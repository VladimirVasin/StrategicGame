using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyNavigationPathfinder
    {
        private const float DiagonalCost = 1.41421356f;
        private const float CostEpsilon = 0.0001f;

        private static readonly Direction[] CardinalDirections =
        {
            new(new Vector2Int(1, 0), 1f),
            new(new Vector2Int(-1, 0), 1f),
            new(new Vector2Int(0, 1), 1f),
            new(new Vector2Int(0, -1), 1f)
        };

        private static readonly Direction[] ResidentDirections =
        {
            new(new Vector2Int(1, 0), 1f),
            new(new Vector2Int(-1, 0), 1f),
            new(new Vector2Int(0, 1), 1f),
            new(new Vector2Int(0, -1), 1f),
            new(new Vector2Int(1, 1), DiagonalCost),
            new(new Vector2Int(1, -1), DiagonalCost),
            new(new Vector2Int(-1, 1), DiagonalCost),
            new(new Vector2Int(-1, -1), DiagonalCost)
        };

        private readonly List<HeapNode> open = new();
        private readonly List<Vector2Int> reversePath = new();
        private int[] visitStamps;
        private int[] closedStamps;
        private int[] parents;
        private float[] costs;
        private int stamp;
        private int width;
        private int height;

        public StrategyNavigationStatus TryBuildPath(
            CityMapController map,
            StrategyNavigationQuery query,
            List<Vector2Int> rawCells,
            List<Vector2Int> smoothedCells)
        {
            rawCells.Clear();
            smoothedCells.Clear();
            if (map == null || !IsInside(map, query.Start) || !IsInside(map, query.Target))
            {
                return StrategyNavigationStatus.Invalid;
            }

            EnsureCapacity(map.Width, map.Height);
            AdvanceStamp();
            open.Clear();
            reversePath.Clear();

            if (!CanOccupy(map, query, query.Start, false)
                || !CanOccupy(map, query, query.Target, true))
            {
                return StrategyNavigationStatus.Invalid;
            }

            if (query.Start == query.Target)
            {
                return StrategyNavigationStatus.Success;
            }

            int startIndex = ToIndex(query.Start);
            visitStamps[startIndex] = stamp;
            costs[startIndex] = 0f;
            parents[startIndex] = -1;
            Push(new HeapNode(startIndex, 0f, Estimate(query.Start, query.Target, query.Mode)));

            int visited = 0;
            int visitLimit = query.MaxVisited > 0
                ? query.MaxVisited
                : Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && visited < visitLimit)
            {
                HeapNode node = Pop();
                if (closedStamps[node.Index] == stamp
                    || visitStamps[node.Index] != stamp
                    || node.Cost > costs[node.Index] + CostEpsilon)
                {
                    continue;
                }

                Vector2Int current = ToCell(node.Index);
                if (current == query.Target)
                {
                    if (!Reconstruct(query.Start, query.Target, rawCells))
                    {
                        return StrategyNavigationStatus.Unreachable;
                    }

                    if (query.Mode == StrategyNavigationMode.ResidentTrail)
                    {
                        Smooth(map, query, rawCells, smoothedCells);
                    }
                    else
                    {
                        smoothedCells.AddRange(rawCells);
                    }

                    return StrategyNavigationStatus.Success;
                }

                closedStamps[node.Index] = stamp;
                visited++;
                Expand(map, query, current, node.Index, costs[node.Index]);
            }

            return StrategyNavigationStatus.Unreachable;
        }

        private void Expand(
            CityMapController map,
            StrategyNavigationQuery query,
            Vector2Int current,
            int currentIndex,
            float currentCost)
        {
            Direction[] directions = query.Mode == StrategyNavigationMode.ResidentTrail
                ? ResidentDirections
                : CardinalDirections;
            for (int i = 0; i < directions.Length; i++)
            {
                Direction direction = directions[i];
                Vector2Int next = current + direction.Delta;
                if (!IsInside(map, next)
                    || !CanUseStep(map, query, current, next, direction.IsDiagonal))
                {
                    continue;
                }

                int nextIndex = ToIndex(next);
                if (closedStamps[nextIndex] == stamp)
                {
                    continue;
                }

                float newCost = currentCost + GetStepCost(query.Mode, next, direction.Cost);
                if (visitStamps[nextIndex] == stamp && newCost >= costs[nextIndex] - CostEpsilon)
                {
                    continue;
                }

                visitStamps[nextIndex] = stamp;
                costs[nextIndex] = newCost;
                parents[nextIndex] = currentIndex;
                Push(new HeapNode(
                    nextIndex,
                    newCost,
                    newCost + Estimate(next, query.Target, query.Mode)));
            }
        }

        private bool CanUseStep(
            CityMapController map,
            StrategyNavigationQuery query,
            Vector2Int current,
            Vector2Int next,
            bool diagonal)
        {
            if (!CanOccupy(map, query, next, next == query.Target))
            {
                return false;
            }

            if (!diagonal)
            {
                return true;
            }

            Vector2Int horizontal = new(next.x, current.y);
            Vector2Int vertical = new(current.x, next.y);
            return CanOccupy(map, query, horizontal, false)
                && CanOccupy(map, query, vertical, false);
        }

        private static bool CanOccupy(
            CityMapController map,
            StrategyNavigationQuery query,
            Vector2Int cell,
            bool isTarget)
        {
            if (query.Mode != StrategyNavigationMode.WildlifeLand)
            {
                return map.IsCellWalkable(cell);
            }

            if (query.Wildlife != null)
            {
                return isTarget
                    ? query.Wildlife.IsLandWildlifeTargetCell(cell, query.AllowWildlifeStructureBuffer)
                    : query.Wildlife.IsLandWildlifeTravelCell(cell, query.AllowWildlifeStructureBuffer);
            }

            return isTarget
                ? StrategyWildlifeRiverCrossing.IsLandCell(map, cell)
                : StrategyWildlifeRiverCrossing.IsLandOrRiverCell(map, cell);
        }

        private bool Reconstruct(Vector2Int start, Vector2Int target, List<Vector2Int> result)
        {
            reversePath.Clear();
            int startIndex = ToIndex(start);
            int current = ToIndex(target);
            while (current != startIndex)
            {
                reversePath.Add(ToCell(current));
                if (current < 0 || current >= parents.Length || parents[current] < 0)
                {
                    result.Clear();
                    return false;
                }

                current = parents[current];
            }

            for (int i = reversePath.Count - 1; i >= 0; i--)
            {
                result.Add(reversePath[i]);
            }

            return result.Count > 0;
        }

        private void Smooth(
            CityMapController map,
            StrategyNavigationQuery query,
            List<Vector2Int> rawCells,
            List<Vector2Int> result)
        {
            Vector2Int anchor = query.Start;
            int index = 0;
            while (index < rawCells.Count)
            {
                int best = index;
                for (int candidate = rawCells.Count - 1; candidate >= index; candidate--)
                {
                    if (HasWalkableLine(map, query, anchor, rawCells[candidate]))
                    {
                        best = candidate;
                        break;
                    }
                }

                anchor = rawCells[best];
                result.Add(anchor);
                index = best + 1;
            }
        }

        private bool HasWalkableLine(
            CityMapController map,
            StrategyNavigationQuery query,
            Vector2Int from,
            Vector2Int to)
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

                if (!CanUseStep(map, query, current, next, next.x != current.x && next.y != current.y))
                {
                    return false;
                }

                current = next;
            }

            return true;
        }

        private static float GetStepCost(StrategyNavigationMode mode, Vector2Int cell, float baseCost)
        {
            if (mode != StrategyNavigationMode.ResidentTrail || StrategyTrailController.Active == null)
            {
                return baseCost;
            }

            return baseCost * StrategyTrailController.Active.GetPathCostMultiplier(cell);
        }

        private static float Estimate(Vector2Int from, Vector2Int to, StrategyNavigationMode mode)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);
            if (mode != StrategyNavigationMode.ResidentTrail)
            {
                return dx + dy;
            }

            int diagonal = Mathf.Min(dx, dy);
            int straight = Mathf.Max(dx, dy) - diagonal;
            float multiplier = StrategyTrailController.Active != null
                ? StrategyTrailController.Active.PathCostMultiplier
                : 1f;
            return (straight + diagonal * DiagonalCost) * multiplier;
        }

        private void EnsureCapacity(int mapWidth, int mapHeight)
        {
            int capacity = mapWidth * mapHeight;
            if (visitStamps != null && visitStamps.Length == capacity && width == mapWidth)
            {
                return;
            }

            width = mapWidth;
            height = mapHeight;
            visitStamps = new int[capacity];
            closedStamps = new int[capacity];
            parents = new int[capacity];
            costs = new float[capacity];
            stamp = 0;
        }

        private void AdvanceStamp()
        {
            stamp++;
            if (stamp != int.MaxValue)
            {
                return;
            }

            System.Array.Clear(visitStamps, 0, visitStamps.Length);
            System.Array.Clear(closedStamps, 0, closedStamps.Length);
            stamp = 1;
        }

        private int ToIndex(Vector2Int cell) => cell.y * width + cell.x;
        private Vector2Int ToCell(int index) => new(index % width, index / width);

        private static bool IsInside(CityMapController map, Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < map.Width && cell.y >= 0 && cell.y < map.Height;
        }

        private void Push(HeapNode node)
        {
            open.Add(node);
            int index = open.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!HigherPriority(node, open[parent]))
                {
                    break;
                }

                open[index] = open[parent];
                index = parent;
            }

            open[index] = node;
        }

        private HeapNode Pop()
        {
            HeapNode result = open[0];
            HeapNode last = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            if (open.Count == 0)
            {
                return result;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                if (left >= open.Count)
                {
                    break;
                }

                int best = right < open.Count && HigherPriority(open[right], open[left]) ? right : left;
                if (!HigherPriority(open[best], last))
                {
                    break;
                }

                open[index] = open[best];
                index = best;
            }

            open[index] = last;
            return result;
        }

        private static bool HigherPriority(HeapNode left, HeapNode right)
        {
            return left.Priority < right.Priority - CostEpsilon
                || (Mathf.Abs(left.Priority - right.Priority) <= CostEpsilon && left.Cost < right.Cost);
        }

        private readonly struct HeapNode
        {
            public HeapNode(int index, float cost, float priority)
            {
                Index = index;
                Cost = cost;
                Priority = priority;
            }

            public int Index { get; }
            public float Cost { get; }
            public float Priority { get; }
        }

        private readonly struct Direction
        {
            public Direction(Vector2Int delta, float cost)
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
