using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyRefugeeArrivalController : MonoBehaviour
    {
        private const int FirstArrivalDayIndex = 1;
        private const float RepeatArrivalMinSeconds = 420f;
        private const float RepeatArrivalMaxSeconds = 720f;
        private const int PopulationSlowdownStart = 40;
        private const int PopulationHardCap = 50;
        private const int MaxRouteAttempts = 24;
        private const int MaxCampGatherRadius = 5;

        private readonly List<StrategyResidentAgent> activeFamily = new();
        private readonly List<Vector3> exitTargets = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyTimeScaleController timeScale;
        private StrategyRefugeeDialogController dialog;
        private StrategyFogOfWarController fog;
        private RefugeeArrivalState state;
        private Vector2Int activeEntryCell;
        private Vector3 activeOutsideBaseWorld;
        private Vector2 activeFormationAxis = Vector2.right;
        private float arrivalTimer;
        private int familySequence;
        private bool firstArrivalTriggered;
        private bool loggedWaitingForFirstArrival;
        private bool pauseHeld;

        public bool HasActiveArrivalFamily => activeFamily.Count > 0
            && state == RefugeeArrivalState.WalkingToCamp;

        private enum RefugeeArrivalState
        {
            Waiting,
            PreparingRoutes,
            WalkingToCamp,
            WaitingForDecision,
            Leaving
        }

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyTimeScaleController timeScaleController,
            StrategyRefugeeDialogController dialogController,
            StrategyFogOfWarController fogController)
        {
            map = mapController;
            population = populationController;
            timeScale = timeScaleController;
            dialog = dialogController;
            fog = fogController;
            dialog?.Configure();
            if (state == RefugeeArrivalState.Waiting && !firstArrivalTriggered && !loggedWaitingForFirstArrival)
            {
                LogFirstArrivalWaiting();
            }
            else if (state == RefugeeArrivalState.Waiting && firstArrivalTriggered && arrivalTimer <= 0f)
            {
                ScheduleNextArrival();
            }
        }

        private void Update()
        {
            if (map == null || population == null)
            {
                return;
            }

            if (state == RefugeeArrivalState.Waiting)
            {
                if (!firstArrivalTriggered)
                {
                    UpdateFirstArrivalGate();
                    return;
                }

                UpdateRecurringArrivalGate();

                return;
            }

            if (state == RefugeeArrivalState.WalkingToCamp)
            {
                if (HasFamilyFinishedTravel(StrategyResidentAgent.ResidentActivity.ArrivingAsRefugee))
                {
                    OpenDecisionDialog();
                }

                return;
            }

            if (state == RefugeeArrivalState.Leaving
                && HasFamilyFinishedTravel(StrategyResidentAgent.ResidentActivity.LeavingSettlement))
            {
                FinishRejectedDeparture();
            }
        }

        public bool DebugStartArrival()
        {
            if (map == null || population == null)
            {
                StrategyDebugLogger.Warn(
                    "Refugees",
                    "DebugArrivalRejected",
                    StrategyDebugLogger.F("reason", "not_configured"));
                return false;
            }

            if (state != RefugeeArrivalState.Waiting)
            {
                StrategyDebugLogger.Warn(
                    "Refugees",
                    "DebugArrivalRejected",
                    StrategyDebugLogger.F("reason", "arrival_already_active"),
                    StrategyDebugLogger.F("state", state));
                return false;
            }

            bool started = TryStartArrival(!firstArrivalTriggered);
            StrategyDebugLogger.Info(
                "Refugees",
                "DebugArrivalRequested",
                StrategyDebugLogger.F("started", started),
                StrategyDebugLogger.F("state", state));
            return started;
        }

        private bool TryStartArrival(bool firstArrival)
        {
            int populationCount = population.TotalResidentCount;
            int availableSlots = PopulationHardCap - populationCount;
            if (availableSlots <= 0)
            {
                arrivalTimer = 60f;
                StrategyDebugLogger.Info(
                    "Refugees",
                    "ArrivalSuppressedByPopulation",
                    StrategyDebugLogger.F("population", populationCount),
                    StrategyDebugLogger.F("cap", PopulationHardCap));
                return false;
            }

            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            int housingSlots = population.GetAvailableHousingSlots();
            if (!firstArrival && calendar.Season == StrategySeason.Winter && housingSlots <= 0)
            {
                arrivalTimer = 120f;
                StrategyDebugLogger.Info(
                    "Refugees",
                    "ArrivalSuppressedByWinterHousing",
                    StrategyDebugLogger.F("housingSlots", housingSlots));
                return false;
            }

            if (!population.TryGetCampCell(out Vector2Int campCell)
                || !population.TryGetCampWorld(out _))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "no_camp"));
                arrivalTimer = 45f;
                return false;
            }

            int supportedFamilySize = housingSlots > 0
                ? Mathf.Min(3, availableSlots, housingSlots)
                : 1;
            int memberCount = Random.Range(1, supportedFamilySize + 1);
            int parentCount = GetRefugeeParentCount(memberCount);
            int childCount = memberCount - parentCount;
            if (!BeginPrepareArrival(
                    firstArrival,
                    campCell,
                    memberCount,
                    parentCount,
                    childCount))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "route_prepare_busy"));
                arrivalTimer = 60f;
                return false;
            }
            return true;
        }

        private void OpenDecisionDialog()
        {
            state = RefugeeArrivalState.WaitingForDecision;
            PushPause();
            if (dialog == null)
            {
                StrategyDebugLogger.Warn("Refugees", "DecisionAutoAccepted", StrategyDebugLogger.F("reason", "no_dialog"));
                HandleDecision(true);
                return;
            }

            dialog.Show(activeFamily, HandleDecision);
            StrategyDebugLogger.Info(
                "Refugees",
                "ArrivedAtCamp",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("members", activeFamily.Count));
        }

        private void HandleDecision(bool accepted)
        {
            if (state != RefugeeArrivalState.WaitingForDecision)
            {
                return;
            }

            if (accepted)
            {
                population.AcceptRefugeeFamily(activeFamily);
                activeFamily.Clear();
                exitTargets.Clear();
                ReleasePause();
                ScheduleNextArrival();
                return;
            }

            ReleasePause();
            StartRejectedDeparture();
        }

        private void StartRejectedDeparture()
        {
            exitTargets.Clear();
            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident == null)
                {
                    continue;
                }

                List<Vector3> route = BuildDepartureRoute(resident, i);
                resident.FollowRefugeePath(route, true);
            }

            state = RefugeeArrivalState.Leaving;
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyRejected",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("members", activeFamily.Count));
        }

        private void FinishRejectedDeparture()
        {
            population.DestroyTemporaryResidents(activeFamily);
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyLeftMap",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("members", activeFamily.Count));
            activeFamily.Clear();
            exitTargets.Clear();
            ScheduleNextArrival();
        }

        private static int GetRefugeeParentCount(int memberCount)
        {
            int clampedMembers = Mathf.Clamp(memberCount, 1, 3);
            if (clampedMembers <= 1)
            {
                return 1;
            }

            int maxParents = Mathf.Min(2, clampedMembers);
            return Random.Range(1, maxParents + 1);
        }

        private bool HasFamilyFinishedTravel(StrategyResidentAgent.ResidentActivity travelActivity)
        {
            bool hasLivingMember = false;
            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident == null)
                {
                    continue;
                }

                hasLivingMember = true;
                if (resident.Activity == travelActivity)
                {
                    return false;
                }
            }

            return hasLivingMember;
        }

    }
}
