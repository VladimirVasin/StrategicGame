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

            if (level == 1 && !HasVisibleWeakRouteSupport(cell, cardinalRouteNeighbors))
            {
                return 0;
            }

            return level;
        }

        private bool HasVisibleWeakRouteSupport(Vector2Int cell, int cardinalRouteNeighbors)
        {
            return CountCardinalStrongRouteTrailNeighbors(cell) > 0
                || HasOpposingRouteTrailNeighbors(cell)
                || cardinalRouteNeighbors >= 3;
        }

        private bool IsStableRouteTrailCell(Vector2Int cell)
        {
            return GetRawRouteTrailLevel(cell) >= 2
                && CountCardinalRouteTrailNeighbors(cell) > 0;
        }

        private bool HasOpposingRouteTrailNeighbors(Vector2Int cell)
        {
            return HasRawRouteTrailNeighbor(cell, Vector2Int.up) && HasRawRouteTrailNeighbor(cell, Vector2Int.down)
                || HasRawRouteTrailNeighbor(cell, Vector2Int.left) && HasRawRouteTrailNeighbor(cell, Vector2Int.right);
        }

        private int CountCardinalStrongRouteTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.up) >= 2 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.right) >= 2 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.down) >= 2 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.left) >= 2 ? 1 : 0;
            return count;
        }

        private bool HasRawRouteTrailNeighbor(Vector2Int cell, Vector2Int offset)
        {
            return GetRawRouteTrailLevel(cell + offset) > 0;
        }

        private byte GetFunctionalTrailLevel(Vector2Int cell)
        {
            byte routeLevel = GetRawRouteTrailLevel(cell);
            byte footfallLevel = GetTrailLevel(cell);
            byte functionalFootfallLevel = footfallLevel >= 2 || (footfallLevel > 0 && HasFunctionalFaintSupport(cell))
                ? footfallLevel
                : (byte)0;

            return routeLevel > functionalFootfallLevel ? routeLevel : functionalFootfallLevel;
        }

        private bool HasFunctionalFaintSupport(Vector2Int cell)
        {
            return HasOpposingStrongCardinalNeighbors(cell) || CountCardinalStrongTrailNeighbors(cell) >= 3;
        }

        private bool HasOpposingStrongCardinalNeighbors(Vector2Int cell)
        {
            return HasStrongTrailNeighbor(cell, Vector2Int.up) && HasStrongTrailNeighbor(cell, Vector2Int.down)
                || HasStrongTrailNeighbor(cell, Vector2Int.left) && HasStrongTrailNeighbor(cell, Vector2Int.right);
        }

        private int CountCardinalStrongTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            count += HasStrongTrailNeighbor(cell, Vector2Int.up) ? 1 : 0;
            count += HasStrongTrailNeighbor(cell, Vector2Int.right) ? 1 : 0;
            count += HasStrongTrailNeighbor(cell, Vector2Int.down) ? 1 : 0;
            count += HasStrongTrailNeighbor(cell, Vector2Int.left) ? 1 : 0;
            return count;
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

        private int CountStrongTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            for (int i = 0; i < NeighborCells.Length; i++)
            {
                count += GetTrailLevel(cell + NeighborCells[i]) >= 2 ? 1 : 0;
            }

            return count;
        }

        private bool HasStrongTrailNeighbor(Vector2Int cell, Vector2Int offset)
        {
            return GetTrailLevel(cell + offset) >= 2;
        }

        private bool HasRawTrailNeighbor(Vector2Int cell, Vector2Int offset)
        {
            return GetTrailLevel(cell + offset) > 0;
        }
    }
}
