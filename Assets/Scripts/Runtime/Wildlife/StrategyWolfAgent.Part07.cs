using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private const int WolfRoamPathAttemptLimit = 4;
        private const float WolfRoamPathFailureCooldownSeconds = 16f;

        private readonly Dictionary<Vector2Int, float> blockedRoamTargets = new();

        internal bool IsWolfRoamTargetBlocked(Vector2Int cell)
        {
            if (!blockedRoamTargets.TryGetValue(cell, out float until))
            {
                return false;
            }

            if (until > Time.time)
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
                    return triedAny
                        ? LogWolfPathFailed("roam_candidates_blocked", lastFailedCell)
                        : LogWolfRoamFailed("no_roam_cell", currentCell, preferSafety);
                }

                triedAny = true;
                lastFailedCell = roamCell;
                if (TryBuildPathTo(roamCell))
                {
                    blockedRoamTargets.Remove(roamCell);
                    LogWolfPathReady(preferSafety ? "avoid_roam" : "roam", roamCell, roamCell);
                    SetWolfState(preferSafety ? StrategyWolfBehaviorState.AvoidingSettlement : StrategyWolfBehaviorState.Roaming, "roam_path_ready");
                    stateTimer = Random.Range(1.0f, 2.2f);
                    return true;
                }

                blockedRoamTargets[roamCell] = Time.time + WolfRoamPathFailureCooldownSeconds;
            }

            return LogWolfPathFailed("roam_path_failed", lastFailedCell);
        }
    }
}
