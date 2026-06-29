using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyTrailPathfinder
    {
        private const float PathCostEpsilon = 0.0001f;
        private const float DiagonalPathCost = 1.41421356f;

        private static readonly TrailPathDirection[] Directions =
        {
            new TrailPathDirection(new Vector2Int(1, 0), 1f),
            new TrailPathDirection(new Vector2Int(-1, 0), 1f),
            new TrailPathDirection(new Vector2Int(0, 1), 1f),
            new TrailPathDirection(new Vector2Int(0, -1), 1f),
            new TrailPathDirection(new Vector2Int(1, 1), DiagonalPathCost),
            new TrailPathDirection(new Vector2Int(1, -1), DiagonalPathCost),
            new TrailPathDirection(new Vector2Int(-1, 1), DiagonalPathCost),
            new TrailPathDirection(new Vector2Int(-1, -1), DiagonalPathCost)
        };

        private readonly List<PathQueueNode> open = new();
        private readonly Dictionary<Vector2Int, float> costs = new();
        private readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        private readonly HashSet<Vector2Int> closed = new();
        private readonly List<Vector2Int> rawCells = new();
        private readonly List<Vector2Int> smoothedCells = new();

        public IReadOnlyList<Vector2Int> RawCells => rawCells;
        public IReadOnlyList<Vector2Int> SmoothedCells => smoothedCells;

        public bool TryBuildPath(CityMapController map, Vector2Int startCell, Vector2Int targetCell)
        {
            Clear();
            if (map == null || !map.IsCellWalkable(startCell) || !map.IsCellWalkable(targetCell) || startCell == targetCell)
            {
                return false;
            }

            costs[startCell] = 0f;
            PushPathNode(open, new PathQueueNode(startCell, 0f, EstimatePathCost(startCell, targetCell)));

            int visited = 0;
            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && visited < visitLimit)
            {
                PathQueueNode node = PopPathNode(open);
                Vector2Int current = node.Cell;
                if (closed.Contains(current)
                    || !costs.TryGetValue(current, out float currentCost)
                    || node.Cost > currentCost + PathCostEpsilon)
                {
                    continue;
                }

                if (current == targetCell)
                {
                    return TryReconstructCells(startCell, targetCell) && SmoothCells(map, startCell);
                }

                closed.Add(current);
                visited++;
                ExpandNode(map, current, currentCost, targetCell);
            }

            return false;
        }

        private void Clear()
        {
            open.Clear();
            costs.Clear();
            cameFrom.Clear();
            closed.Clear();
            rawCells.Clear();
            smoothedCells.Clear();
        }

        private void ExpandNode(CityMapController map, Vector2Int current, float currentCost, Vector2Int targetCell)
        {
            for (int i = 0; i < Directions.Length; i++)
            {
                TrailPathDirection direction = Directions[i];
                Vector2Int next = current + direction.Delta;
                if (closed.Contains(next) || !CanUseStep(map, current, direction))
                {
                    continue;
                }

                float newCost = currentCost + GetStepCost(next, direction.Cost);
                if (costs.TryGetValue(next, out float oldCost) && newCost >= oldCost - PathCostEpsilon)
                {
                    continue;
                }

                costs[next] = newCost;
                cameFrom[next] = current;
                float priority = newCost + EstimatePathCost(next, targetCell);
                PushPathNode(open, new PathQueueNode(next, newCost, priority));
            }
        }

        private static bool CanUseStep(CityMapController map, Vector2Int current, TrailPathDirection direction)
        {
            Vector2Int next = current + direction.Delta;
            if (!map.IsCellWalkable(next))
            {
                return false;
            }

            if (!direction.IsDiagonal)
            {
                return true;
            }

            Vector2Int horizontal = current + new Vector2Int(direction.Delta.x, 0);
            Vector2Int vertical = current + new Vector2Int(0, direction.Delta.y);
            return map.IsCellWalkable(horizontal) && map.IsCellWalkable(vertical);
        }

        private static float EstimatePathCost(Vector2Int from, Vector2Int to)
        {
            float minStepCost = StrategyTrailController.Active != null
                ? StrategyTrailController.Active.PathCostMultiplier
                : 1f;
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            int diagonal = Mathf.Min(dx, dy);
            int straight = Mathf.Max(dx, dy) - diagonal;
            return (straight + diagonal * DiagonalPathCost) * minStepCost;
        }

        private static float GetStepCost(Vector2Int cell, float baseCost)
        {
            StrategyTrailController trails = StrategyTrailController.Active;
            return baseCost * (trails != null ? trails.GetPathCostMultiplier(cell) : 1f);
        }

        private bool TryReconstructCells(Vector2Int startCell, Vector2Int targetCell)
        {
            Vector2Int current = targetCell;
            while (current != startCell)
            {
                rawCells.Add(current);
                if (!cameFrom.TryGetValue(current, out current))
                {
                    rawCells.Clear();
                    return false;
                }
            }

            rawCells.Reverse();
            return rawCells.Count > 0;
        }

        private bool SmoothCells(CityMapController map, Vector2Int startCell)
        {
            Vector2Int anchor = startCell;
            int index = 0;
            while (index < rawCells.Count)
            {
                int best = index;
                for (int candidate = rawCells.Count - 1; candidate >= index; candidate--)
                {
                    if (HasWalkableLine(map, anchor, rawCells[candidate]))
                    {
                        best = candidate;
                        break;
                    }
                }

                anchor = rawCells[best];
                smoothedCells.Add(anchor);
                index = best + 1;
            }

            return smoothedCells.Count > 0;
        }

        private static bool HasWalkableLine(CityMapController map, Vector2Int from, Vector2Int to)
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

                Vector2Int delta = next - current;
                float cost = delta.x != 0 && delta.y != 0 ? DiagonalPathCost : 1f;
                if (!CanUseStep(map, current, new TrailPathDirection(delta, cost)))
                {
                    return false;
                }

                current = next;
            }

            return true;
        }

        private static void PushPathNode(List<PathQueueNode> heap, PathQueueNode node)
        {
            heap.Add(node);
            int index = heap.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!HasHigherPriority(node, heap[parent]))
                {
                    break;
                }

                heap[index] = heap[parent];
                index = parent;
            }

            heap[index] = node;
        }

        private static PathQueueNode PopPathNode(List<PathQueueNode> heap)
        {
            PathQueueNode result = heap[0];
            PathQueueNode last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count <= 0)
            {
                return result;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                if (left >= heap.Count)
                {
                    break;
                }

                int best = right < heap.Count && HasHigherPriority(heap[right], heap[left]) ? right : left;
                if (!HasHigherPriority(heap[best], last))
                {
                    break;
                }

                heap[index] = heap[best];
                index = best;
            }

            heap[index] = last;
            return result;
        }

        private static bool HasHigherPriority(PathQueueNode a, PathQueueNode b)
        {
            return a.Priority < b.Priority - PathCostEpsilon
                || (Mathf.Abs(a.Priority - b.Priority) <= PathCostEpsilon && a.Cost < b.Cost);
        }

        private readonly struct PathQueueNode
        {
            public PathQueueNode(Vector2Int cell, float cost, float priority)
            {
                Cell = cell;
                Cost = cost;
                Priority = priority;
            }

            public Vector2Int Cell { get; }
            public float Cost { get; }
            public float Priority { get; }
        }

        private readonly struct TrailPathDirection
        {
            public TrailPathDirection(Vector2Int delta, float cost)
            {
                Delta = delta;
                Cost = cost;
                IsDiagonal = delta.x != 0 && delta.y != 0;
            }

            public Vector2Int Delta { get; }
            public float Cost { get; }
            public bool IsDiagonal { get; }
        }
    }
}
