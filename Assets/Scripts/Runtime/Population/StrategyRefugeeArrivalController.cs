using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyRefugeeArrivalController : MonoBehaviour
    {
        private const int FirstArrivalHouseRequirement = 3;
        private const float RepeatArrivalMinSeconds = 420f;
        private const float RepeatArrivalMaxSeconds = 720f;
        private const int PopulationSlowdownStart = 40;
        private const int PopulationHardCap = 50;
        private const int MaxRouteAttempts = 96;
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

        private enum RefugeeArrivalState
        {
            Waiting,
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

                arrivalTimer -= Time.deltaTime * GetArrivalIntensity();
                if (arrivalTimer <= 0f)
                {
                    TryStartArrival(false);
                }

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

        private void UpdateFirstArrivalGate()
        {
            if (population.CompletedHouseCount < FirstArrivalHouseRequirement)
            {
                return;
            }

            arrivalTimer -= Time.deltaTime * GetArrivalIntensity();
            if (arrivalTimer > 0f)
            {
                return;
            }

            TryStartArrival(true);
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

            if (!population.TryGetCampCell(out Vector2Int campCell)
                || !population.TryGetCampWorld(out _))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "no_camp"));
                arrivalTimer = 45f;
                return false;
            }

            int memberCount = Random.Range(1, Mathf.Min(3, availableSlots) + 1);
            int parentCount = GetRefugeeParentCount(memberCount);
            int childCount = memberCount - parentCount;
            if (!TryPrepareArrivalRoutes(memberCount, campCell, out List<List<Vector3>> routes))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "no_route"));
                arrivalTimer = 60f;
                return false;
            }

            if (!population.TryCreateRefugeeFamily(
                    activeOutsideBaseWorld,
                    activeFormationAxis,
                    activeEntryCell,
                    parentCount,
                    childCount,
                    out List<StrategyResidentAgent> family))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "family_create_failed"));
                arrivalTimer = 60f;
                return false;
            }

            activeFamily.Clear();
            activeFamily.AddRange(family);
            familySequence++;
            if (firstArrival)
            {
                firstArrivalTriggered = true;
            }

            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident == null)
                {
                    continue;
                }

                List<Vector3> route = routes[Mathf.Min(i, routes.Count - 1)];
                resident.FollowRefugeePath(route, false);
            }

            state = RefugeeArrivalState.WalkingToCamp;
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilySpawned",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("firstArrival", firstArrival),
                StrategyDebugLogger.F("completedHouses", population.CompletedHouseCount),
                StrategyDebugLogger.F("members", activeFamily.Count),
                StrategyDebugLogger.F("parents", parentCount),
                StrategyDebugLogger.F("children", childCount),
                StrategyDebugLogger.F("entryCell", activeEntryCell),
                StrategyDebugLogger.F("spawnWorld", activeOutsideBaseWorld));
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

        private void ScheduleNextArrival()
        {
            arrivalTimer = Random.Range(RepeatArrivalMinSeconds, RepeatArrivalMaxSeconds);
            state = RefugeeArrivalState.Waiting;
            StrategyDebugLogger.Info(
                "Refugees",
                "ArrivalScheduled",
                StrategyDebugLogger.F("initial", false),
                StrategyDebugLogger.F("population", population != null ? population.TotalResidentCount : 0),
                StrategyDebugLogger.F("intensity", GetArrivalIntensity()),
                StrategyDebugLogger.F("seconds", arrivalTimer));
        }

        private float GetArrivalIntensity()
        {
            int count = population != null ? population.TotalResidentCount : 0;
            if (count >= PopulationHardCap)
            {
                return 0f;
            }

            if (count <= PopulationSlowdownStart)
            {
                return 1f;
            }

            return Mathf.Clamp01((PopulationHardCap - count) / (float)(PopulationHardCap - PopulationSlowdownStart));
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

        private void LogFirstArrivalWaiting()
        {
            loggedWaitingForFirstArrival = true;
            arrivalTimer = 0f;
            StrategyDebugLogger.Info(
                "Refugees",
                "FirstArrivalWaitingForHouses",
                StrategyDebugLogger.F("requiredHouses", FirstArrivalHouseRequirement),
                StrategyDebugLogger.F("completedHouses", population != null ? population.CompletedHouseCount : 0));
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

        private bool TryPrepareArrivalRoutes(
            int memberCount,
            Vector2Int campCell,
            out List<List<Vector3>> routes)
        {
            routes = null;
            if (map == null || memberCount <= 0)
            {
                return false;
            }

            if (!TryBuildCampArrivalTargets(campCell, out HashSet<Vector2Int> campArrivalTargets))
            {
                StrategyDebugLogger.Warn(
                    "Refugees",
                    "ArrivalRouteTargetsRejected",
                    StrategyDebugLogger.F("reason", "no_reachable_camp_targets"),
                    StrategyDebugLogger.F("campCell", campCell));
                return false;
            }

            for (int attempt = 0; attempt < MaxRouteAttempts; attempt++)
            {
                if (!TryGetRandomArrivalEntryCell(out Vector2Int entryCell, out Vector2 outward))
                {
                    continue;
                }

                HashSet<Vector2Int> usedTargets = new();
                List<List<Vector3>> preparedRoutes = new();
                bool allReady = true;
                for (int i = 0; i < memberCount; i++)
                {
                    if (!TryFindCampRoute(entryCell, campCell, campArrivalTargets, usedTargets, i, out List<Vector2Int> cellPath))
                    {
                        allReady = false;
                        break;
                    }

                    preparedRoutes.Add(ToWorldRoute(cellPath));
                }

                if (!allReady || preparedRoutes.Count <= 0)
                {
                    continue;
                }

                activeEntryCell = entryCell;
                activeFormationAxis = -outward;
                Vector3 entryWorld = map.GetCellCenterWorld(entryCell.x, entryCell.y);
                activeOutsideBaseWorld = new Vector3(entryWorld.x, entryWorld.y, -0.08f);
                routes = preparedRoutes;
                return true;
            }

            return false;
        }

        private bool TryFindCampRoute(
            Vector2Int entryCell,
            Vector2Int campCell,
            HashSet<Vector2Int> campArrivalTargets,
            HashSet<Vector2Int> usedTargets,
            int salt,
            out List<Vector2Int> cellPath)
        {
            cellPath = null;
            for (int radius = 1; radius <= MaxCampGatherRadius; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        if (usedTargets.Contains(candidate)
                            || campArrivalTargets == null
                            || !campArrivalTargets.Contains(candidate)
                            || !map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        candidates.Add(candidate);
                    }
                }

                while (candidates.Count > 0)
                {
                    int index = Mathf.Abs(Random.Range(0, candidates.Count) + salt) % candidates.Count;
                    Vector2Int target = candidates[index];
                    candidates.RemoveAt(index);
                    if (TryBuildCellPath(entryCell, target, out cellPath))
                    {
                        usedTargets.Add(target);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
