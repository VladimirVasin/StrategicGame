using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyFishingAccessUtility
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        public static bool TryFindFishingWaterCell(
            CityMapController map,
            Vector2Int center,
            int radius,
            out Vector2Int waterCell,
            out CityMapWaterKind waterKind)
        {
            waterCell = default;
            waterKind = CityMapWaterKind.None;
            if (map == null)
            {
                return false;
            }

            float bestSqr = float.MaxValue;
            float radiusSqr = radius * radius;
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    float sqr = (candidate - center).sqrMagnitude;
                    if (sqr > radiusSqr || sqr >= bestSqr
                        || !map.TryGetCell(x, y, out CityMapCell cell)
                        || cell.Kind != CityMapCellKind.Water
                        || !HasCardinalWalkableShore(map, candidate))
                    {
                        continue;
                    }

                    waterCell = candidate;
                    waterKind = cell.WaterKind;
                    bestSqr = sqr;
                }
            }

            return bestSqr < float.MaxValue;
        }

        private static bool HasCardinalWalkableShore(CityMapController map, Vector2Int waterCell)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                if (map.IsCellWalkable(waterCell + CardinalDirections[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
