using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private byte GetVisibleTrailLevel(Vector2Int cell)
        {
            byte level = GetRawRouteTrailLevel(cell);
            if (level <= 0)
            {
                return 0;
            }

            int cardinalRouteNeighbors = CountCardinalRouteTrailNeighbors(cell);
            if (cardinalRouteNeighbors <= 0)
            {
                return 0;
            }

            return level;
        }

        private byte GetFunctionalTrailLevel(Vector2Int cell)
        {
            return GetRawRouteTrailLevel(cell);
        }

        private int CountCardinalTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            count += HasRawTrailNeighbor(cell, Vector2Int.up) ? 1 : 0;
            count += HasRawTrailNeighbor(cell, Vector2Int.right) ? 1 : 0;
            count += HasRawTrailNeighbor(cell, Vector2Int.down) ? 1 : 0;
            count += HasRawTrailNeighbor(cell, Vector2Int.left) ? 1 : 0;
            return count;
        }

        private int CountRawTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < NeighborCells.Length; i++)
            {
                count += HasRawTrailNeighbor(cell, NeighborCells[i]) ? 1 : 0;
            }

            return count;
        }

        private bool HasRawTrailNeighbor(Vector2Int cell, Vector2Int offset)
        {
            return GetTrailLevel(cell + offset) > 0;
        }
    }
}
