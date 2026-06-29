using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyTrailRouteCellBuilder
    {
        public static void BuildRouteCells(
            CityMapController map,
            Vector2Int startCell,
            IReadOnlyList<Vector2Int> rawPathCells,
            List<Vector2Int> routeCells)
        {
            routeCells.Clear();
            if (map == null || rawPathCells == null)
            {
                return;
            }

            AddRouteCell(routeCells, startCell);
            Vector2Int previous = startCell;
            for (int i = 0; i < rawPathCells.Count; i++)
            {
                Vector2Int next = rawPathCells[i];
                AddRouteSegment(map, routeCells, previous, next);
                previous = next;
            }
        }

        private static void AddRouteSegment(CityMapController map, List<Vector2Int> routeCells, Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
            {
                bool horizontalFirst = ((from.x + from.y + to.x + to.y) & 1) == 0;
                Vector2Int first = horizontalFirst
                    ? new Vector2Int(to.x, from.y)
                    : new Vector2Int(from.x, to.y);
                Vector2Int fallback = horizontalFirst
                    ? new Vector2Int(from.x, to.y)
                    : new Vector2Int(to.x, from.y);
                AddRouteCell(routeCells, map.IsCellWalkable(first) ? first : fallback);
                AddRouteCell(routeCells, to);
                return;
            }

            AddRouteCell(routeCells, to);
        }

        private static void AddRouteCell(List<Vector2Int> routeCells, Vector2Int cell)
        {
            if (routeCells.Count > 0 && routeCells[routeCells.Count - 1] == cell)
            {
                return;
            }

            routeCells.Add(cell);
        }
    }
}
