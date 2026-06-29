using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyTrailRouteCellBuilder
    {
        public static void BuildRouteCells(
            CityMapController map,
            Vector2Int startCell,
            IReadOnlyList<Vector2Int> pathCells,
            List<Vector2Int> routeCells)
        {
            routeCells.Clear();
            if (map == null || pathCells == null)
            {
                return;
            }

            AddRouteCell(routeCells, startCell);
            Vector2Int previous = startCell;
            for (int i = 0; i < pathCells.Count; i++)
            {
                Vector2Int next = pathCells[i];
                AddRouteLine(map, routeCells, previous, next);
                previous = next;
            }

            RemoveRepeatedLoops(routeCells);
        }

        private static void AddRouteLine(CityMapController map, List<Vector2Int> routeCells, Vector2Int from, Vector2Int to)
        {
            Vector2Int current = from;
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);
            int stepX = from.x < to.x ? 1 : -1;
            int stepY = from.y < to.y ? 1 : -1;
            int error = dx - dy;

            while (current != to)
            {
                int doubledError = error * 2;
                Vector2Int next = current;
                if (doubledError > -dy)
                {
                    error -= dy;
                    next.x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    next.y += stepY;
                }

                AddRouteStep(map, routeCells, current, next);
                current = next;
            }
        }

        private static void AddRouteStep(CityMapController map, List<Vector2Int> routeCells, Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
            {
                Vector2Int first = ChooseDiagonalBridgeCell(map, routeCells, from, to);
                AddRouteCell(routeCells, first);
                AddRouteCell(routeCells, to);
                return;
            }

            AddRouteCell(routeCells, to);
        }

        private static Vector2Int ChooseDiagonalBridgeCell(
            CityMapController map,
            List<Vector2Int> routeCells,
            Vector2Int from,
            Vector2Int to)
        {
            Vector2Int horizontal = new Vector2Int(to.x, from.y);
            Vector2Int vertical = new Vector2Int(from.x, to.y);
            bool horizontalWalkable = map.IsCellWalkable(horizontal);
            bool verticalWalkable = map.IsCellWalkable(vertical);
            if (!horizontalWalkable)
            {
                return vertical;
            }

            if (!verticalWalkable)
            {
                return horizontal;
            }

            bool horizontalCompletesSquare = WouldCompleteSquare(routeCells, horizontal);
            bool verticalCompletesSquare = WouldCompleteSquare(routeCells, vertical);
            if (horizontalCompletesSquare != verticalCompletesSquare)
            {
                return horizontalCompletesSquare ? vertical : horizontal;
            }

            return ((from.x + from.y + to.x + to.y) & 1) == 0
                ? horizontal
                : vertical;
        }

        private static void AddRouteCell(List<Vector2Int> routeCells, Vector2Int cell)
        {
            if (routeCells.Count > 0 && routeCells[routeCells.Count - 1] == cell)
            {
                return;
            }

            routeCells.Add(cell);
        }

        private static void RemoveRepeatedLoops(List<Vector2Int> routeCells)
        {
            Dictionary<Vector2Int, int> seen = new();
            bool removed;
            do
            {
                removed = false;
                seen.Clear();
                for (int i = 0; i < routeCells.Count; i++)
                {
                    Vector2Int cell = routeCells[i];
                    if (!seen.TryGetValue(cell, out int firstIndex))
                    {
                        seen[cell] = i;
                        continue;
                    }

                    routeCells.RemoveRange(firstIndex + 1, i - firstIndex);
                    removed = true;
                    break;
                }
            }
            while (removed);
        }

        private static bool WouldCompleteSquare(List<Vector2Int> routeCells, Vector2Int cell)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    Vector2Int corner = new Vector2Int(cell.x + dx, cell.y + dy);
                    if ((corner != cell && !ContainsCell(routeCells, corner))
                        || (corner + Vector2Int.right != cell && !ContainsCell(routeCells, corner + Vector2Int.right))
                        || (corner + Vector2Int.up != cell && !ContainsCell(routeCells, corner + Vector2Int.up))
                        || (corner + Vector2Int.one != cell && !ContainsCell(routeCells, corner + Vector2Int.one)))
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCell(List<Vector2Int> routeCells, Vector2Int cell)
        {
            for (int i = 0; i < routeCells.Count; i++)
            {
                if (routeCells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
