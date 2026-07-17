using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyStoryPointOfInterestRouteField
    {
        private const int CardinalCost = 10;
        private const int DiagonalCost = 14;
        private const int SeedSearchRadius = 8;

        private static readonly RouteDirection[] Directions =
        {
            new(Vector2Int.up, CardinalCost),
            new(Vector2Int.right, CardinalCost),
            new(Vector2Int.down, CardinalCost),
            new(Vector2Int.left, CardinalCost),
            new(new Vector2Int(1, 1), DiagonalCost),
            new(new Vector2Int(1, -1), DiagonalCost),
            new(new Vector2Int(-1, -1), DiagonalCost),
            new(new Vector2Int(-1, 1), DiagonalCost)
        };

        public static int[,] Build(
            int width,
            int height,
            Vector2Int campCell,
            Func<Vector2Int, bool> isWalkable)
        {
            if (width <= 0
                || height <= 0
                || isWalkable == null
                || !TryFindReachableSeed(
                    width,
                    height,
                    campCell,
                    isWalkable,
                    out Vector2Int seedCell))
            {
                return null;
            }

            int[,] costs = new int[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    costs[x, y] = -1;
                }
            }

            SortedSet<RouteNode> open = new(RouteNodeComparer.Instance);
            costs[seedCell.x, seedCell.y] = 0;
            open.Add(new RouteNode(seedCell, 0));
            while (open.Count > 0)
            {
                RouteNode current = open.Min;
                open.Remove(current);
                if (costs[current.Cell.x, current.Cell.y] != current.Cost)
                {
                    continue;
                }

                for (int i = 0; i < Directions.Length; i++)
                {
                    RouteDirection direction = Directions[i];
                    Vector2Int next = current.Cell + direction.Offset;
                    if (!IsInside(next, width, height)
                        || !isWalkable(next)
                        || direction.Cost == DiagonalCost
                        && IsBlockedDiagonal(current.Cell, next, isWalkable))
                    {
                        continue;
                    }

                    int nextCost = current.Cost + direction.Cost;
                    int knownCost = costs[next.x, next.y];
                    if (knownCost >= 0 && knownCost <= nextCost)
                    {
                        continue;
                    }

                    costs[next.x, next.y] = nextCost;
                    open.Add(new RouteNode(next, nextCost));
                }
            }

            return costs;
        }

        private static bool TryFindReachableSeed(
            int width,
            int height,
            Vector2Int campCell,
            Func<Vector2Int, bool> isWalkable,
            out Vector2Int seedCell)
        {
            for (int radius = 0; radius <= SeedSearchRadius; radius++)
            {
                bool found = false;
                int bestDistanceSquared = int.MaxValue;
                Vector2Int best = default;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        int distanceSquared = x * x + y * y;
                        if (!IsInside(candidate, width, height)
                            || !isWalkable(candidate)
                            || found && (distanceSquared > bestDistanceSquared
                                || distanceSquared == bestDistanceSquared
                                && CompareCells(candidate, best) >= 0))
                        {
                            continue;
                        }

                        found = true;
                        best = candidate;
                        bestDistanceSquared = distanceSquared;
                    }
                }

                if (found)
                {
                    seedCell = best;
                    return true;
                }
            }

            seedCell = default;
            return false;
        }

        private static bool IsBlockedDiagonal(
            Vector2Int from,
            Vector2Int to,
            Func<Vector2Int, bool> isWalkable)
        {
            int deltaX = to.x - from.x;
            int deltaY = to.y - from.y;
            return !isWalkable(new Vector2Int(from.x + deltaX, from.y))
                || !isWalkable(new Vector2Int(from.x, from.y + deltaY));
        }

        private static int CompareCells(Vector2Int left, Vector2Int right)
        {
            int yComparison = left.y.CompareTo(right.y);
            return yComparison != 0 ? yComparison : left.x.CompareTo(right.x);
        }

        private static bool IsInside(Vector2Int cell, int width, int height)
        {
            return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
        }

        private readonly struct RouteDirection
        {
            public RouteDirection(Vector2Int offset, int cost)
            {
                Offset = offset;
                Cost = cost;
            }

            public Vector2Int Offset { get; }
            public int Cost { get; }
        }

        private readonly struct RouteNode
        {
            public RouteNode(Vector2Int cell, int cost)
            {
                Cell = cell;
                Cost = cost;
            }

            public Vector2Int Cell { get; }
            public int Cost { get; }
        }

        private sealed class RouteNodeComparer : IComparer<RouteNode>
        {
            public static RouteNodeComparer Instance { get; } = new();

            public int Compare(RouteNode left, RouteNode right)
            {
                int comparison = left.Cost.CompareTo(right.Cost);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = left.Cell.y.CompareTo(right.Cell.y);
                return comparison != 0 ? comparison : left.Cell.x.CompareTo(right.Cell.x);
            }
        }
    }
}
