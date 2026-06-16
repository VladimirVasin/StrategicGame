using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private byte GetVisibleTrailLevel(Vector2Int cell)
        {
            byte level = GetTrailLevel(cell);
            if (level <= 0)
            {
                return 0;
            }

            return level >= 2 || HasVisibleFaintSupport(cell) ? level : (byte)0;
        }

        private bool HasVisibleFaintSupport(Vector2Int cell)
        {
            if (CountStrongTrailNeighbors(cell) > 0 && CountRawTrailNeighbors(cell) >= 2)
            {
                return true;
            }

            return HasOpposingRawNeighbors(cell) || CountCardinalRawTrailNeighbors(cell) >= 3;
        }

        private bool HasOpposingRawNeighbors(Vector2Int cell)
        {
            return HasRawTrailNeighbor(cell, Vector2Int.up) && HasRawTrailNeighbor(cell, Vector2Int.down)
                || HasRawTrailNeighbor(cell, Vector2Int.left) && HasRawTrailNeighbor(cell, Vector2Int.right)
                || HasRawTrailNeighbor(cell, new Vector2Int(1, 1)) && HasRawTrailNeighbor(cell, new Vector2Int(-1, -1))
                || HasRawTrailNeighbor(cell, new Vector2Int(1, -1)) && HasRawTrailNeighbor(cell, new Vector2Int(-1, 1));
        }

        private int CountCardinalRawTrailNeighbors(Vector2Int cell)
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

        private int CountStrongTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < NeighborCells.Length; i++)
            {
                count += GetTrailLevel(cell + NeighborCells[i]) >= 2 ? 1 : 0;
            }

            return count;
        }

        private bool HasRawTrailNeighbor(Vector2Int cell, Vector2Int offset)
        {
            return GetTrailLevel(cell + offset) > 0;
        }
    }
}
