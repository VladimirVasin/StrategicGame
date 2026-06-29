using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private const int WolfRoamPathAttemptLimit = 4;
        private const float WolfRoamPathFailureCooldownSeconds = 16f;
        private const float WolfNormalRoamRetryCooldownSeconds = 4.0f;
        private const float WolfSafetyRoamRetryCooldownSeconds = 1.2f;
        private const float WolfTargetPathFailureCooldownSeconds = 2.4f;

        private readonly Dictionary<Vector2Int, float> blockedRoamTargets = new();
        private Vector2Int lastTargetPathFailureCell;
        private float nextRoamPathAttemptTime;
        private float nextTargetPathAttemptTime;
        private bool hasLastTargetPathFailureCell;

        internal bool IsWolfRoamTargetBlocked(Vector2Int cell)
        {
            if (!blockedRoamTargets.TryGetValue(cell, out float until))
            {
                return false;
            }

            if (until > Time.realtimeSinceStartup)
            {
                return true;
            }

            blockedRoamTargets.Remove(cell);
            return false;
        }

        private bool TryStartReachableRoaming(Vector2Int currentCell, bool preferSafety)
        {
            Vector2Int lastFailedCell = currentCell;
            bool triedAny = false;
            for (int attempt = 0; attempt < WolfRoamPathAttemptLimit; attempt++)
            {
                if (!wildlife.TryFindWolfRoamCell(this, currentCell, preferSafety, out Vector2Int roamCell))
                {
                    MarkWolfRoamPathFailure(preferSafety);
                    return triedAny
                        ? LogWolfPathFailed("roam_candidates_blocked", lastFailedCell)
                        : LogWolfRoamFailed("no_roam_cell", currentCell, preferSafety);
                }

                triedAny = true;
                lastFailedCell = roamCell;
                if (TryBuildPathTo(roamCell))
                {
                    blockedRoamTargets.Remove(roamCell);
                    MarkWolfRoamPathSuccess();
                    LogWolfPathReady(preferSafety ? "avoid_roam" : "roam", roamCell, roamCell);
                    SetWolfState(preferSafety ? StrategyWolfBehaviorState.AvoidingSettlement : StrategyWolfBehaviorState.Roaming, "roam_path_ready");
                    stateTimer = Random.Range(1.0f, 2.2f);
                    return true;
                }

                blockedRoamTargets[roamCell] = Time.realtimeSinceStartup + WolfRoamPathFailureCooldownSeconds;
            }

            MarkWolfRoamPathFailure(preferSafety);
            return LogWolfPathFailed("roam_path_failed", lastFailedCell);
        }

        private bool ShouldSkipWolfRoamPathAttempt(bool preferSafety)
        {
            float now = Time.realtimeSinceStartup;
            return now < nextRoamPathAttemptTime
                && (!preferSafety || nextRoamPathAttemptTime - now <= WolfSafetyRoamRetryCooldownSeconds);
        }

        private void MarkWolfRoamPathFailure(bool preferSafety)
        {
            float cooldown = preferSafety ? WolfSafetyRoamRetryCooldownSeconds : WolfNormalRoamRetryCooldownSeconds;
            nextRoamPathAttemptTime = Time.realtimeSinceStartup + cooldown;
        }

        private void MarkWolfRoamPathSuccess()
        {
            nextRoamPathAttemptTime = 0f;
        }

        private bool ShouldSkipWolfTargetPathAttempt(Vector2Int targetCell)
        {
            return hasLastTargetPathFailureCell
                && targetCell == lastTargetPathFailureCell
                && Time.realtimeSinceStartup < nextTargetPathAttemptTime;
        }

        private void MarkWolfTargetPathFailure(Vector2Int targetCell)
        {
            hasLastTargetPathFailureCell = true;
            lastTargetPathFailureCell = targetCell;
            nextTargetPathAttemptTime = Time.realtimeSinceStartup + WolfTargetPathFailureCooldownSeconds;
        }

        private void MarkWolfTargetPathSuccess()
        {
            hasLastTargetPathFailureCell = false;
            nextTargetPathAttemptTime = 0f;
        }
    }
}
