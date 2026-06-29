using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const float MigrationAbortLogCooldownSeconds = 30f;
        private const float MigrationFailedTargetCooldownSeconds = 90f;

        private readonly Dictionary<string, float> migrationAbortLogTimes = new();
        private readonly Dictionary<Vector2Int, float> migrationFailedTargetTimes = new();

        private int GetMigrationTargetMaxVisited(Vector2Int start, Vector2Int target)
        {
            int distance = Mathf.CeilToInt(Vector2Int.Distance(start, target));
            int localLimit = Mathf.Max(128, distance * distance * 2);
            return map != null ? Mathf.Min(map.Width * map.Height, localLimit) : localLimit;
        }

        private bool ShouldLogMigrationAbort(string kind, int id)
        {
            string key = kind + ":" + id;
            float now = Time.realtimeSinceStartup;
            if (migrationAbortLogTimes.TryGetValue(key, out float nextTime) && now < nextTime)
            {
                return false;
            }

            migrationAbortLogTimes[key] = now + MigrationAbortLogCooldownSeconds;
            return true;
        }

        private bool IsMigrationTargetCoolingDown(Vector2Int target)
        {
            float now = Time.realtimeSinceStartup;
            if (!migrationFailedTargetTimes.TryGetValue(target, out float nextTime))
            {
                return false;
            }

            if (now < nextTime)
            {
                return true;
            }

            migrationFailedTargetTimes.Remove(target);
            return false;
        }

        private void RegisterMigrationTargetFailure(Vector2Int target)
        {
            migrationFailedTargetTimes[target] = Time.realtimeSinceStartup + MigrationFailedTargetCooldownSeconds;
        }
    }
}
