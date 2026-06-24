using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private bool TryFindFallbackCampCell(out Vector2Int cell)
        {
            Vector2Int bestCell = default;
            int bestWaterDistance = -1;
            bool found = false;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (!map.TryGetCell(x, y, out CityMapCell mapCell)
                        || mapCell.IsWater
                        || mapCell.IsShore
                        || !map.IsCellWalkable(candidate))
                    {
                        continue;
                    }

                    int distance = GetNearestWaterDistance(candidate, CampMinWaterDistance);
                    if (distance >= CampMinWaterDistance)
                    {
                        cell = candidate;
                        return true;
                    }

                    if (!found || distance > bestWaterDistance)
                    {
                        found = true;
                        bestWaterDistance = distance;
                        bestCell = candidate;
                    }
                }
            }

            cell = bestCell;
            if (found)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "StarterCampWaterClearanceRelaxed",
                    StrategyDebugLogger.F("cell", cell),
                    StrategyDebugLogger.F("nearestWaterDistance", bestWaterDistance),
                    StrategyDebugLogger.F("minWaterDistance", CampMinWaterDistance));
            }

            return found;
        }

        private bool HasWaterNearCell(Vector2Int cell, int radius)
        {
            return GetNearestWaterDistance(cell, radius) < radius;
        }

        private int GetNearestWaterDistance(Vector2Int cell, int maxDistance)
        {
            int best = maxDistance;
            for (int y = -maxDistance; y <= maxDistance; y++)
            {
                for (int x = -maxDistance; x <= maxDistance; x++)
                {
                    int distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (distance >= best)
                    {
                        continue;
                    }

                    Vector2Int candidate = cell + new Vector2Int(x, y);
                    if (map.TryGetCell(candidate.x, candidate.y, out CityMapCell mapCell)
                        && (mapCell.IsWater || mapCell.IsShore))
                    {
                        best = distance;
                    }
                }
            }

            return best;
        }
    }
}
