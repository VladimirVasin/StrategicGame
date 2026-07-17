using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        internal void BeginScoutExpedition(StrategyScoutLodge lodge)
        {
            if (lodge == null || scoutWorkplace != lodge || !lodge.IsExploring)
            {
                return;
            }

            bool preserveResourceReturn = IsCompletingResourceReturn();
            CancelScoutWork();
            LeaveNightRestForScoutDuty();
            if (!preserveResourceReturn)
            {
                ResetScoutMovementToIdle();
            }

            waitTimer = 0f;
            scoutWorkCooldown = Random.Range(0.05f, 0.15f);
        }

        internal void BeginScoutReturn(StrategyScoutLodge lodge)
        {
            if (lodge == null || scoutWorkplace != lodge || !lodge.IsReturning)
            {
                return;
            }

            bool preserveResourceReturn = IsCompletingResourceReturn();
            CancelScoutWork();
            LeaveNightRestForScoutDuty();
            if (preserveResourceReturn)
            {
                waitTimer = 0f;
                return;
            }

            ResetScoutMovementToIdle();
            scoutWorkCooldown = 0f;
            waitTimer = 0f;
            TryStartScoutReturnTask();
        }

        internal void EnsureScoutReturn(StrategyScoutLodge lodge)
        {
            if (lodge == null || scoutWorkplace != lodge || !lodge.IsReturning)
            {
                return;
            }

            if (activity == ResidentActivity.ReturningToScoutLodge
                && hasTarget
                && pathIndex < path.Count)
            {
                return;
            }

            if (IsCompletingResourceReturn() || scoutWorkCooldown > 0f)
            {
                return;
            }

            if (activity == ResidentActivity.ReturningToScoutLodge)
            {
                ResetScoutMovementToIdle();
            }

            if (activity == ResidentActivity.Idle)
            {
                TryStartScoutReturnTask();
            }
        }

        private bool TryStartScoutReturnTask()
        {
            StrategyScoutLodge lodge = scoutWorkplace;
            if (lodge == null
                || !lodge.IsReturning
                || deathRequested
                || map == null
                || activity != ResidentActivity.Idle)
            {
                return false;
            }

            if (!lodge.TryFindReturnCell(transform.position, out Vector2Int returnCell)
                || !map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                ScheduleScoutReturnRetry(lodge, "no_return_cell");
                return false;
            }

            if (currentCell == returnCell)
            {
                CompleteScoutReturn();
                return false;
            }

            activity = ResidentActivity.ReturningToScoutLodge;
            if (!TryBuildPathTo(returnCell))
            {
                bool deferred = WasLastPathBuildDeferred;
                ResetScoutMovementToIdle();
                scoutWorkCooldown = deferred
                    ? Random.Range(0.12f, 0.24f)
                    : Random.Range(0.55f, 1.1f);
                waitTimer = Mathf.Clamp(scoutWorkCooldown, 0.08f, 0.35f);
                lodge.NotifyScoutReturnBlocked(this);
                if (!deferred)
                {
                    StrategyDebugLogger.Warn(
                        "ScoutLodge",
                        "ScoutReturnMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("lodgeOrigin", lodge.Origin),
                        StrategyDebugLogger.F("target", returnCell),
                        StrategyDebugLogger.F("reason", "no_path"));
                }

                return false;
            }

            hasTarget = true;
            waitTimer = 0f;
            lodge.NotifyScoutReturnTravelStarted(this);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ScoutReturnMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("lodgeOrigin", lodge.Origin),
                StrategyDebugLogger.F("target", returnCell));
            return true;
        }

        private void ScheduleScoutReturnRetry(StrategyScoutLodge lodge, string reason)
        {
            scoutWorkCooldown = Random.Range(0.55f, 1.1f);
            waitTimer = Mathf.Clamp(scoutWorkCooldown, 0.08f, 0.35f);
            lodge?.NotifyScoutReturnBlocked(this);
            StrategyDebugLogger.Warn(
                "ScoutLodge",
                "ScoutReturnDeferred",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("lodgeOrigin", lodge != null ? lodge.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("reason", reason));
        }

        private void CompleteScoutReturn()
        {
            StrategyScoutLodge lodge = scoutWorkplace;
            ResetScoutMovementToIdle();
            scoutWorkCooldown = Random.Range(0.10f, 0.25f);
            waitTimer = 0f;
            lodge?.NotifyScoutReturned(this);
        }
    }
}
