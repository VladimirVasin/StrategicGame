using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyScoutLodge
    {
        private const float RationEpsilon = 0.01f;

        private StrategyScoutExpeditionState expeditionState = StrategyScoutExpeditionState.Ready;
        private int plannedExpeditionDays;
        private float expeditionStartedElapsedSeconds;
        private float expeditionEndsElapsedSeconds;
        private float remainingFieldRations;
        private float provisionRationCredit;
        private int lastProvisionedDayIndex = -1;

        public StrategyScoutExpeditionState ExpeditionState => expeditionState;
        public int PlannedExpeditionDays => plannedExpeditionDays;
        public float ExpeditionStartedElapsedSeconds => expeditionStartedElapsedSeconds;
        public float ExpeditionEndsElapsedSeconds => expeditionEndsElapsedSeconds;
        public float RemainingFieldRations => Mathf.Max(0f, remainingFieldRations);
        public float ProvisionRationCredit => Mathf.Max(0f, provisionRationCredit);
        public int LastProvisionedDayIndex => lastProvisionedDayIndex;
        public bool IsExploring => WorkerCount > 0
            && expeditionState == StrategyScoutExpeditionState.Exploring;
        public bool IsReturning => WorkerCount > 0
            && expeditionState == StrategyScoutExpeditionState.Returning;
        public float RemainingExpeditionSeconds => IsExploring
            ? Mathf.Max(0f, expeditionEndsElapsedSeconds
                - StrategyDayNightCycleController.CurrentElapsedSeconds)
            : 0f;

        public float GetAvailableExpeditionRations() =>
            ProvisionRationCredit + StrategyScoutProvisionService.GetAvailableRations();

        public bool CanDispatchScout(StrategyResidentAgent resident)
        {
            return resident != null
                && expeditionState == StrategyScoutExpeditionState.Ready
                && resident.CanAcceptWorkAssignment
                && TryGetWorker(0, out StrategyResidentAgent current)
                && current == resident;
        }

        public bool TryAppointAndStartExpedition(
            StrategyResidentAgent resident,
            int days)
        {
            if (!StrategyScoutExpeditionPolicy.IsSupportedDuration(days)
                || !CanAppointWorker(resident)
                || GetAvailableExpeditionRations() + RationEpsilon
                    < StrategyScoutExpeditionPolicy.GetRequiredRations(days)
                || !TryAppointWorker(resident))
            {
                return false;
            }

            if (TryStartExpedition(days))
            {
                return true;
            }

            UnassignWorker(resident);
            return false;
        }

        public bool TryStartExpedition(int days)
        {
            if (!StrategyScoutExpeditionPolicy.IsSupportedDuration(days)
                || expeditionState != StrategyScoutExpeditionState.Ready
                || !TryGetWorker(0, out StrategyResidentAgent worker)
                || !CanDispatchScout(worker))
            {
                return false;
            }

            float requiredRations = StrategyScoutExpeditionPolicy.GetRequiredRations(days);
            if (GetAvailableExpeditionRations() + RationEpsilon < requiredRations)
            {
                return false;
            }

            float creditUsed = Mathf.Min(ProvisionRationCredit, requiredRations);
            float rationRequest = Mathf.Max(0f, requiredRations - creditUsed);
            float suppliedRations = 0f;
            if (rationRequest > RationEpsilon
                && !StrategyScoutProvisionService.TryTakeRations(
                    rationRequest,
                    this,
                    out suppliedRations))
            {
                return false;
            }

            provisionRationCredit = Mathf.Max(
                0f,
                ProvisionRationCredit - creditUsed + Mathf.Max(0f, suppliedRations - rationRequest));
            plannedExpeditionDays = days;
            expeditionStartedElapsedSeconds = StrategyDayNightCycleController.CurrentElapsedSeconds;
            expeditionEndsElapsedSeconds = expeditionStartedElapsedSeconds
                + StrategyScoutExpeditionPolicy.GetDurationSeconds(days);
            remainingFieldRations = requiredRations;
            lastProvisionedDayIndex = worker.LastNutritionDayIndex;
            expeditionState = StrategyScoutExpeditionState.Exploring;
            missionStatus = "Preparing the first route";
            worker.BeginScoutExpedition(this);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ExpeditionStarted",
                StrategyDebugLogger.F("lodgeOrigin", Origin),
                StrategyDebugLogger.F("worker", worker.FullName),
                StrategyDebugLogger.F("days", days),
                StrategyDebugLogger.F("endsAt", expeditionEndsElapsedSeconds),
                StrategyDebugLogger.F("fieldRations", remainingFieldRations),
                StrategyDebugLogger.F("rationCredit", provisionRationCredit));
            return true;
        }

        public bool RequestRecall()
        {
            if (IsReturning)
            {
                return true;
            }

            return BeginScoutReturn("Recalled to Lodge", "recall");
        }

        public bool RestorePersistentState(
            StrategyResidentAgent resident,
            StrategyScoutExpeditionState state,
            int plannedDays,
            float startedAt,
            float endsAt,
            float savedRemainingFieldRations,
            float provisionCredit,
            int savedLastProvisionedDayIndex)
        {
            if (!IsValidRestoreState(
                    resident,
                    state,
                    plannedDays,
                    startedAt,
                    endsAt,
                    savedRemainingFieldRations,
                    provisionCredit,
                    savedLastProvisionedDayIndex)
                || WorkerCount > 0)
            {
                return false;
            }

            ResetExpeditionState(false);
            provisionRationCredit = provisionCredit;
            if (resident == null)
            {
                return true;
            }

            if (!AssignWorker(resident))
            {
                provisionRationCredit = provisionCredit;
                return false;
            }

            provisionRationCredit = provisionCredit;
            if (state == StrategyScoutExpeditionState.Ready)
            {
                return true;
            }

            plannedExpeditionDays = plannedDays;
            expeditionStartedElapsedSeconds = startedAt;
            expeditionEndsElapsedSeconds = endsAt;
            remainingFieldRations = savedRemainingFieldRations;
            lastProvisionedDayIndex = savedLastProvisionedDayIndex;
            expeditionState = state;
            if (state == StrategyScoutExpeditionState.Returning
                || endsAt <= StrategyDayNightCycleController.CurrentElapsedSeconds + RationEpsilon)
            {
                expeditionState = StrategyScoutExpeditionState.Exploring;
                return BeginScoutReturn("Returning to Lodge", "restore");
            }

            missionStatus = "Resuming expedition";
            resident.BeginScoutExpedition(this);
            return true;
        }

        internal bool TryFindReturnCell(Vector3 residentWorld, out Vector2Int returnCell)
        {
            returnCell = default;
            if (map == null || building == null)
            {
                return false;
            }

            bool found = false;
            float bestDistance = float.MaxValue;
            for (int radius = 1; radius <= 3; radius++)
            {
                for (int y = -radius; y < building.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < building.Footprint.x + radius; x++)
                    {
                        bool edge = x == -radius
                            || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        if (!edge)
                        {
                            continue;
                        }

                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                        float distance = (candidateWorld - residentWorld).sqrMagnitude;
                        if (!found
                            || distance < bestDistance - 0.001f
                            || Mathf.Abs(distance - bestDistance) <= 0.001f
                            && IsStableCellBefore(candidate, returnCell))
                        {
                            found = true;
                            bestDistance = distance;
                            returnCell = candidate;
                        }
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        internal void NotifyScoutReturnTravelStarted(StrategyResidentAgent resident)
        {
            if (IsReturning && IsAssignedWorker(resident))
            {
                missionStatus = "Returning to Lodge";
            }
        }

        internal void NotifyScoutReturnBlocked(StrategyResidentAgent resident)
        {
            if (IsReturning && IsAssignedWorker(resident))
            {
                missionStatus = "Return route blocked - retrying";
            }
        }

        internal void NotifyScoutReturned(StrategyResidentAgent resident)
        {
            if (!IsReturning || !IsAssignedWorker(resident))
            {
                return;
            }

            string workerName = resident.FullName;
            ResetExpeditionState(true);
            missionStatus = "Ready for expedition";
            StrategyEventLogHudController.Notify(
                workerName + " returned to the Scout Lodge.",
                new Color(0.48f, 0.76f, 0.72f));
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ExpeditionReturned",
                StrategyDebugLogger.F("lodgeOrigin", Origin),
                StrategyDebugLogger.F("worker", workerName),
                StrategyDebugLogger.F("rationCredit", provisionRationCredit));
        }

        private void Update()
        {
            if (WorkerCount <= 0)
            {
                if (expeditionState != StrategyScoutExpeditionState.Ready)
                {
                    ResetExpeditionState(true);
                }

                return;
            }

            if (IsExploring)
            {
                if (StrategyDayNightCycleController.CurrentElapsedSeconds + RationEpsilon
                    >= expeditionEndsElapsedSeconds)
                {
                    BeginScoutReturn("Expedition complete - returning", "duration_complete");
                    return;
                }

                TryApplyNightFieldRation();
            }
            else if (IsReturning && TryGetWorker(0, out StrategyResidentAgent worker))
            {
                worker.EnsureScoutReturn(this);
            }
        }

        private void TryApplyNightFieldRation()
        {
            StrategyCalendarSnapshot calendar =
                StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (calendar.Phase != StrategyTimeOfDayPhase.Night
                || lastProvisionedDayIndex >= calendar.DayIndex
                || !TryGetWorker(0, out StrategyResidentAgent worker))
            {
                return;
            }

            if (worker.LastNutritionDayIndex >= calendar.DayIndex)
            {
                lastProvisionedDayIndex = calendar.DayIndex;
                return;
            }

            if (remainingFieldRations + RationEpsilon
                < StrategyScoutExpeditionPolicy.RationsPerDay)
            {
                StrategyDebugLogger.Warn(
                    "ScoutLodge",
                    "FieldRationBalanceShort",
                    StrategyDebugLogger.F("lodgeOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("remaining", remainingFieldRations));
            }

            worker.ApplyDailyRation(
                worker.DailyRationNeed,
                worker.DailyRationNeed,
                calendar.DayIndex);
            remainingFieldRations = Mathf.Max(
                0f,
                remainingFieldRations - StrategyScoutExpeditionPolicy.RationsPerDay);
            lastProvisionedDayIndex = calendar.DayIndex;
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "FieldRationConsumed",
                StrategyDebugLogger.F("lodgeOrigin", Origin),
                StrategyDebugLogger.F("worker", worker.FullName),
                StrategyDebugLogger.F("day", calendar.DisplayDay),
                StrategyDebugLogger.F("remaining", remainingFieldRations));
        }

        private bool BeginScoutReturn(string status, string reason)
        {
            if (!IsExploring || !TryGetWorker(0, out StrategyResidentAgent worker))
            {
                return false;
            }

            expeditionState = StrategyScoutExpeditionState.Returning;
            remainingFieldRations = 0f;
            missionStatus = status;
            worker.BeginScoutReturn(this);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ExpeditionReturning",
                StrategyDebugLogger.F("lodgeOrigin", Origin),
                StrategyDebugLogger.F("worker", worker.FullName),
                StrategyDebugLogger.F("reason", reason));
            return true;
        }

        private void HandleScoutWorkerAssigned()
        {
            ResetExpeditionState(true);
            missionStatus = "Ready for expedition";
        }

        private void HandleScoutWorkerRemoved(StrategyResidentAgent worker)
        {
            if (worker != null && expeditionState != StrategyScoutExpeditionState.Ready)
            {
                StrategyDebugLogger.Info(
                    "ScoutLodge",
                    "ExpeditionCancelled",
                    StrategyDebugLogger.F("lodgeOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("state", expeditionState),
                    StrategyDebugLogger.F("refund", false));
            }

            ResetExpeditionState(true);
        }

        private void HandleScoutLodgeDestroyed()
        {
            ResetExpeditionState(true);
        }

        private void ResetExpeditionState(bool preserveProvisionCredit)
        {
            expeditionState = StrategyScoutExpeditionState.Ready;
            plannedExpeditionDays = 0;
            expeditionStartedElapsedSeconds = 0f;
            expeditionEndsElapsedSeconds = 0f;
            remainingFieldRations = 0f;
            lastProvisionedDayIndex = -1;
            if (!preserveProvisionCredit)
            {
                provisionRationCredit = 0f;
            }
        }

        private string BuildExpeditionHudStatusText()
        {
            string text = "Scouts: " + WorkerCount + "/" + MaxWorkers;
            if (WorkerCount <= 0)
            {
                return text
                    + "\nMission: Awaiting assignment"
                    + "\nAvailable provisions: "
                    + GetAvailableExpeditionRations().ToString("0.#")
                    + " rations";
            }

            text += "\nMission: " + missionStatus;
            if (IsExploring)
            {
                text += "\nPlanned: " + plannedExpeditionDays + " day"
                    + (plannedExpeditionDays == 1 ? string.Empty : "s")
                    + "\nReturns in: " + FormatRemainingExpeditionTime(RemainingExpeditionSeconds)
                    + "\nField rations: " + RemainingFieldRations.ToString("0.#");
            }
            else if (IsReturning)
            {
                text += "\nReturning from a " + plannedExpeditionDays + "-day expedition";
            }
            else
            {
                text += "\nAvailable provisions: "
                    + GetAvailableExpeditionRations().ToString("0.#")
                    + " rations";
            }

            return text + "\nExploration expands the known map";
        }

        private bool IsAssignedWorker(StrategyResidentAgent resident)
        {
            return resident != null && workers.Contains(resident);
        }

        private static bool IsValidRestoreState(
            StrategyResidentAgent resident,
            StrategyScoutExpeditionState state,
            int plannedDays,
            float startedAt,
            float endsAt,
            float savedRemainingFieldRations,
            float provisionCredit,
            int savedLastProvisionedDayIndex)
        {
            bool active = state != StrategyScoutExpeditionState.Ready;
            return System.Enum.IsDefined(typeof(StrategyScoutExpeditionState), state)
                && (!active || resident != null)
                && (!active || StrategyScoutExpeditionPolicy.IsSupportedDuration(plannedDays))
                && (!active || endsAt >= startedAt)
                && IsFiniteNonNegative(startedAt)
                && IsFiniteNonNegative(endsAt)
                && IsFiniteNonNegative(savedRemainingFieldRations)
                && IsFiniteNonNegative(provisionCredit)
                && savedLastProvisionedDayIndex >= -1;
        }

        private static bool IsFiniteNonNegative(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

        private static bool IsStableCellBefore(Vector2Int left, Vector2Int right) =>
            left.y < right.y || left.y == right.y && left.x < right.x;

        private static string FormatRemainingExpeditionTime(float seconds)
        {
            float dayLength = Mathf.Max(1f, StrategyDayNightCycleController.DayLengthSeconds);
            int days = Mathf.FloorToInt(Mathf.Max(0f, seconds) / dayLength);
            int hours = Mathf.CeilToInt(Mathf.Repeat(Mathf.Max(0f, seconds), dayLength)
                / dayLength * 24f);
            if (hours >= 24)
            {
                days++;
                hours = 0;
            }

            return days > 0
                ? days + "d " + hours + "h"
                : Mathf.Max(1, hours) + "h";
        }
    }
}
