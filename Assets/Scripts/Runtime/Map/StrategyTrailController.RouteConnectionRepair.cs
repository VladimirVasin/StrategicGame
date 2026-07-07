using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private const int RouteRepairSourceLookahead = 10;
        private const int RouteRepairBoundsPadding = 6;
        private const int RouteRepairMaxVisited = 128;

        private readonly List<RouteRepairNode> routeRepairNodes = new();
        private readonly List<int> routeRepairOpen = new();
        private readonly Dictionary<Vector2Int, int> routeRepairBestCosts = new();
        private readonly HashSet<Vector2Int> routeRepairClosed = new();
        private readonly List<Vector2Int> routeRepairPathCells = new();
        private readonly List<Vector2Int> routeRepairParentCells = new();

        private bool TryAppendRouteRepair(
            List<Vector2Int> targetCells,
            IReadOnlyList<Vector2Int> sourceCells,
            int rejectedIndex,
            string reason,
            out int repairedIndex)
        {
            repairedIndex = rejectedIndex;
            if (targetCells.Count <= 0 || sourceCells == null || rejectedIndex + 1 >= sourceCells.Count)
            {
                return false;
            }

            Vector2Int start = targetCells[targetCells.Count - 1];
            int maxIndex = Mathf.Min(sourceCells.Count - 1, rejectedIndex + RouteRepairSourceLookahead);
            for (int targetIndex = rejectedIndex + 1; targetIndex <= maxIndex; targetIndex++)
            {
                Vector2Int target = sourceCells[targetIndex];
                if (GetWearRejectReason(target) != null || ContainsRouteConnectionCell(targetCells, target))
                {
                    continue;
                }

                if (!TryFindRouteRepairPath(start, target, targetCells))
                {
                    continue;
                }

                for (int i = 0; i < routeRepairPathCells.Count; i++)
                {
                    targetCells.Add(routeRepairPathCells[i]);
                }

                repairedIndex = targetIndex;
                StrategyDebugLogger.Info(
                    "Map",
                    "TrailRouteConnectorRepaired",
                    StrategyDebugLogger.F("from", start),
                    StrategyDebugLogger.F("to", target),
                    StrategyDebugLogger.F("skippedCell", sourceCells[rejectedIndex]),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("cells", routeRepairPathCells.Count));
                return true;
            }

            return false;
        }

        private bool TryFindRouteRepairPath(
            Vector2Int start,
            Vector2Int target,
            IReadOnlyList<Vector2Int> stagedCells)
        {
            ClearRouteRepairState();
            int minX = Mathf.Min(start.x, target.x) - RouteRepairBoundsPadding;
            int maxX = Mathf.Max(start.x, target.x) + RouteRepairBoundsPadding;
            int minY = Mathf.Min(start.y, target.y) - RouteRepairBoundsPadding;
            int maxY = Mathf.Max(start.y, target.y) + RouteRepairBoundsPadding;

            routeRepairNodes.Add(new RouteRepairNode(start, -1, 0, GetRouteRepairHeuristic(start, target)));
            routeRepairOpen.Add(0);
            routeRepairBestCosts[start] = 0;

            while (routeRepairOpen.Count > 0 && routeRepairClosed.Count < RouteRepairMaxVisited)
            {
                int nodeIndex = PopBestRouteRepairOpenNode();
                RouteRepairNode node = routeRepairNodes[nodeIndex];
                if (routeRepairClosed.Contains(node.Cell))
                {
                    continue;
                }

                if (node.Cell == target)
                {
                    ReconstructRouteRepairPath(nodeIndex);
                    return routeRepairPathCells.Count > 0;
                }

                routeRepairClosed.Add(node.Cell);
                for (int i = 0; i < NeighborCells.Length; i++)
                {
                    Vector2Int next = node.Cell + NeighborCells[i];
                    if (next.x < minX
                        || next.x > maxX
                        || next.y < minY
                        || next.y > maxY
                        || routeRepairClosed.Contains(next)
                        || !CanUseRouteRepairCell(stagedCells, nodeIndex, next))
                    {
                        continue;
                    }

                    int nextCost = node.Cost + 1 + GetRouteRepairStepPenalty(next);
                    if (routeRepairBestCosts.TryGetValue(next, out int oldCost) && nextCost >= oldCost)
                    {
                        continue;
                    }

                    routeRepairBestCosts[next] = nextCost;
                    int priority = nextCost + GetRouteRepairHeuristic(next, target);
                    routeRepairNodes.Add(new RouteRepairNode(next, nodeIndex, nextCost, priority));
                    routeRepairOpen.Add(routeRepairNodes.Count - 1);
                }
            }

            return false;
        }

        private void ClearRouteRepairState()
        {
            routeRepairNodes.Clear();
            routeRepairOpen.Clear();
            routeRepairBestCosts.Clear();
            routeRepairClosed.Clear();
            routeRepairPathCells.Clear();
            routeRepairParentCells.Clear();
        }

        private bool CanUseRouteRepairCell(
            IReadOnlyList<Vector2Int> stagedCells,
            int parentNodeIndex,
            Vector2Int candidate)
        {
            if (GetWearRejectReason(candidate) != null || ContainsRouteConnectionCell(stagedCells, candidate))
            {
                return false;
            }

            BuildRouteRepairParentCells(parentNodeIndex);
            if (ContainsRouteConnectionCell(routeRepairParentCells, candidate))
            {
                return false;
            }

            return GetRawRouteTrailLevel(candidate) > 0
                || !WouldCompleteRouteSquare(candidate, stagedCells, routeRepairParentCells);
        }

        private int PopBestRouteRepairOpenNode()
        {
            int bestSlot = 0;
            int bestIndex = routeRepairOpen[0];
            RouteRepairNode best = routeRepairNodes[bestIndex];
            for (int i = 1; i < routeRepairOpen.Count; i++)
            {
                int candidateIndex = routeRepairOpen[i];
                RouteRepairNode candidate = routeRepairNodes[candidateIndex];
                if (candidate.Priority > best.Priority
                    || (candidate.Priority == best.Priority && candidate.Cost >= best.Cost))
                {
                    continue;
                }

                bestSlot = i;
                bestIndex = candidateIndex;
                best = candidate;
            }

            routeRepairOpen.RemoveAt(bestSlot);
            return bestIndex;
        }

        private void ReconstructRouteRepairPath(int targetNodeIndex)
        {
            routeRepairPathCells.Clear();
            int currentIndex = targetNodeIndex;
            while (currentIndex >= 0)
            {
                RouteRepairNode node = routeRepairNodes[currentIndex];
                if (node.ParentIndex < 0)
                {
                    break;
                }

                routeRepairPathCells.Add(node.Cell);
                currentIndex = node.ParentIndex;
            }

            routeRepairPathCells.Reverse();
        }

        private void BuildRouteRepairParentCells(int nodeIndex)
        {
            routeRepairParentCells.Clear();
            int currentIndex = nodeIndex;
            while (currentIndex >= 0)
            {
                RouteRepairNode node = routeRepairNodes[currentIndex];
                if (node.ParentIndex < 0)
                {
                    break;
                }

                routeRepairParentCells.Add(node.Cell);
                currentIndex = node.ParentIndex;
            }

            routeRepairParentCells.Reverse();
        }

        private bool WouldCompleteRouteSquare(
            Vector2Int cell,
            IReadOnlyList<Vector2Int> stagedCells,
            IReadOnlyList<Vector2Int> repairCells)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    Vector2Int corner = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (HasRouteSquareCell(corner, cell, stagedCells, repairCells)
                        && HasRouteSquareCell(corner + Vector2Int.right, cell, stagedCells, repairCells)
                        && HasRouteSquareCell(corner + Vector2Int.up, cell, stagedCells, repairCells)
                        && HasRouteSquareCell(corner + Vector2Int.one, cell, stagedCells, repairCells))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasRouteSquareCell(
            Vector2Int squareCell,
            Vector2Int candidate,
            IReadOnlyList<Vector2Int> stagedCells,
            IReadOnlyList<Vector2Int> repairCells)
        {
            return squareCell == candidate
                || GetRawRouteTrailLevel(squareCell) > 0
                || ContainsRouteConnectionCell(stagedCells, squareCell)
                || ContainsRouteConnectionCell(repairCells, squareCell);
        }

        private static int GetRouteRepairHeuristic(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        private int GetRouteRepairStepPenalty(Vector2Int cell)
        {
            if (GetRawRouteTrailLevel(cell) > 0)
            {
                return 0;
            }

            return CountExistingRouteTrailNeighbors(cell);
        }

        private readonly struct RouteRepairNode
        {
            public RouteRepairNode(Vector2Int cell, int parentIndex, int cost, int priority)
            {
                Cell = cell;
                ParentIndex = parentIndex;
                Cost = cost;
                Priority = priority;
            }

            public Vector2Int Cell { get; }
            public int ParentIndex { get; }
            public int Cost { get; }
            public int Priority { get; }
        }
    }
}
