using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const float MigrationAbortLogCooldownSeconds = 30f;
        private const float MigrationFailedTargetCooldownSeconds = 90f;
        private const int MigrationFailedTargetCooldownRadius = 6;

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

        private static void LogMigrationAbort(
            string kind,
            int id,
            Vector2Int center,
            Vector2Int target,
            bool requireWalkableConnection)
        {
            StrategyDebugLogger.Warn(
                "Wildlife",
                "MigrationAborted",
                StrategyDebugLogger.F("kind", kind),
                StrategyDebugLogger.F("id", id),
                StrategyDebugLogger.F("center", center),
                StrategyDebugLogger.F("target", target),
                StrategyDebugLogger.F("reason", "step_candidates_unreachable"),
                StrategyDebugLogger.F("requireWalkableConnection", requireWalkableConnection));
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
            float nextTime = Time.realtimeSinceStartup + MigrationFailedTargetCooldownSeconds;
            int radiusSqr = MigrationFailedTargetCooldownRadius * MigrationFailedTargetCooldownRadius;
            for (int y = -MigrationFailedTargetCooldownRadius; y <= MigrationFailedTargetCooldownRadius; y++)
            {
                for (int x = -MigrationFailedTargetCooldownRadius; x <= MigrationFailedTargetCooldownRadius; x++)
                {
                    if (x * x + y * y > radiusSqr)
                    {
                        continue;
                    }

                    Vector2Int cell = target + new Vector2Int(x, y);
                    if (map != null
                        && (cell.x < 0 || cell.x >= map.Width || cell.y < 0 || cell.y >= map.Height))
                    {
                        continue;
                    }

                    migrationFailedTargetTimes[cell] = nextTime;
                }
            }
        }

        private bool IsViableMigrationTarget(
            Vector2Int currentCenter,
            Vector2Int candidate,
            int step,
            bool requireWalkableConnection,
            System.Func<Vector2Int, bool> isCandidate,
            System.Func<Vector2Int, float> scoreCandidate)
        {
            return TryPickMigrationStep(
                currentCenter,
                candidate,
                step,
                isCandidate,
                scoreCandidate,
                requireWalkableConnection,
                out _);
        }
    }
}
