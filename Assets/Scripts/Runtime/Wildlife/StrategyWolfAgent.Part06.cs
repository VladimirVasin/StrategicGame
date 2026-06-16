using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private const float WolfEscapeRetryCooldownMin = 1.2f;
        private const float WolfEscapeRetryCooldownMax = 2.6f;
        private const int WolfEscapeSearchVisitLimit = 1800;

        private float nextWolfEscapeAttemptTime;
        private float NextWolfEscapeAttemptTime => nextWolfEscapeAttemptTime;

        private bool TryStartRiverEscape()
        {
            if (!StrategyWildlifeRiverCrossing.IsRiverCell(map, transform.position)
                || Time.time < nextWolfEscapeAttemptTime)
            {
                return false;
            }

            if (TryStartRoaming(true))
            {
                return true;
            }

            ScheduleWolfEscapeRetry();
            return false;
        }

        private void ScheduleWolfEscapeRetry()
        {
            nextWolfEscapeAttemptTime = Time.time + Random.Range(WolfEscapeRetryCooldownMin, WolfEscapeRetryCooldownMax);
        }

        private bool IsWolfUrgentEscapeCell(Vector2Int cell)
        {
            return StrategyWildlifeRiverCrossing.IsRiverCell(map, cell)
                || wildlife != null && wildlife.IsWolfUnsafeSettlementCell(cell);
        }

        private bool TryBuildNearbyEscapePath(out Vector2Int escapeCell)
        {
            escapeCell = default;
            if (map == null || !TryGetPathStartCell(out Vector2Int startCell))
            {
                return false;
            }

            bool startIsRiver = StrategyWildlifeRiverCrossing.IsRiverCell(map, startCell);
            bool allowStructureBuffer = startIsRiver
                || wildlife != null && wildlife.IsLandWildlifeStructureBufferCell(startCell);
            bool allowLandFallback = startIsRiver;
            int visitLimit = Mathf.Min(WolfEscapeSearchVisitLimit, Mathf.Max(128, map.Width * map.Height));

            Queue<Vector2Int> frontier = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();
            frontier.Enqueue(startCell);
            visited.Add(startCell);

            Vector2Int fallbackCell = default;
            bool hasFallback = false;
            while (frontier.Count > 0 && visited.Count < visitLimit)
            {
                Vector2Int current = frontier.Dequeue();
                if (current != startCell)
                {
                    if (IsWolfTargetCell(current))
                    {
                        return TryCompleteNearbyEscapePath(startCell, current, cameFrom, out escapeCell);
                    }

                    if (allowLandFallback && !hasFallback && IsWolfTargetCell(current, true))
                    {
                        fallbackCell = current;
                        hasFallback = true;
                    }
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (visited.Contains(next) || !IsWolfTravelCell(next, allowStructureBuffer))
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            return hasFallback
                && TryCompleteNearbyEscapePath(startCell, fallbackCell, cameFrom, out escapeCell);
        }

        private bool TryCompleteNearbyEscapePath(
            Vector2Int startCell,
            Vector2Int targetCell,
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            out Vector2Int escapeCell)
        {
            escapeCell = targetCell;
            BuildWorldPath(startCell, targetCell, cameFrom);
            return path.Count > 0;
        }
    }
}
