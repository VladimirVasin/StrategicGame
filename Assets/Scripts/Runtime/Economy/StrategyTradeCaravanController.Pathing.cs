using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTradeCaravanController
    {
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private List<Vector2Int> CollectEdgeCandidates(Vector2Int target)
        {
            List<Vector2Int> candidates = new();
            if (map == null)
            {
                return candidates;
            }

            for (int x = 0; x < map.Width; x++)
            {
                AddEdgeCandidate(candidates, new Vector2Int(x, 0));
                AddEdgeCandidate(candidates, new Vector2Int(x, map.Height - 1));
            }

            for (int y = 1; y < map.Height - 1; y++)
            {
                AddEdgeCandidate(candidates, new Vector2Int(0, y));
                AddEdgeCandidate(candidates, new Vector2Int(map.Width - 1, y));
            }

            candidates.Sort((left, right) =>
            {
                int leftDistance = Mathf.Abs(left.x - target.x) + Mathf.Abs(left.y - target.y);
                int rightDistance = Mathf.Abs(right.x - target.x) + Mathf.Abs(right.y - target.y);
                return leftDistance.CompareTo(rightDistance);
            });
            return candidates;
        }

        private void AddEdgeCandidate(List<Vector2Int> candidates, Vector2Int cell)
        {
            if (map != null && map.IsCellWalkable(cell))
            {
                candidates.Add(cell);
            }
        }

        private bool TryBuildCellPath(
            Vector2Int start,
            Vector2Int target,
            out List<Vector2Int> cellPath)
        {
            cellPath = null;
            if (map == null || !map.IsCellWalkable(start) || !map.IsCellWalkable(target))
            {
                return false;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();
            open.Enqueue(start);
            visited.Add(start);
            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            int visitedCount = 0;

            while (open.Count > 0 && visitedCount < visitLimit)
            {
                Vector2Int current = open.Dequeue();
                visitedCount++;
                if (current == target)
                {
                    cellPath = ReconstructPath(start, target, cameFrom);
                    return cellPath.Count > 0;
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (visited.Contains(next) || !map.IsCellWalkable(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    open.Enqueue(next);
                }
            }

            return false;
        }

        private static List<Vector2Int> ReconstructPath(
            Vector2Int start,
            Vector2Int target,
            Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            List<Vector2Int> cells = new() { target };
            Vector2Int current = target;
            while (current != start)
            {
                if (!cameFrom.TryGetValue(current, out current))
                {
                    cells.Clear();
                    return cells;
                }

                cells.Add(current);
            }

            cells.Reverse();
            return cells;
        }

        private List<Vector3> ToWorldPath(List<Vector2Int> cellPath)
        {
            List<Vector3> worldPath = new();
            if (map == null || cellPath == null)
            {
                return worldPath;
            }

            for (int i = 0; i < cellPath.Count; i++)
            {
                Vector3 center = map.GetCellCenterWorld(cellPath[i].x, cellPath[i].y);
                center.z = -0.10f;
                worldPath.Add(center);
            }

            return worldPath;
        }
    }
}
