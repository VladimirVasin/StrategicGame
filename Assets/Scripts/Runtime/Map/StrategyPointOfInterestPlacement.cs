using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyPointOfInterestPlacement
    {
        internal const int DefaultPointCount = 10;
        internal const int CampExclusionRadius = 16;
        internal const int EdgeMargin = 6;
        internal const int MinimumSpacing = 18;

        private const int DistributionSalt = 8801;
        private const int SeedSearchRadius = 8;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1)
        };

        public static List<Vector2Int> SelectCells(
            int width,
            int height,
            int seed,
            Vector2Int campCell,
            int targetCount,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isBuildable)
        {
            if (isWalkable == null)
            {
                throw new ArgumentNullException(nameof(isWalkable));
            }

            if (isBuildable == null)
            {
                throw new ArgumentNullException(nameof(isBuildable));
            }

            List<Vector2Int> selected = new();
            if (width <= 0 || height <= 0 || targetCount <= 0)
            {
                return selected;
            }

            bool[,] reachable = BuildReachableCells(
                width,
                height,
                campCell,
                isWalkable);
            if (reachable == null)
            {
                return selected;
            }

            int totalCells = width * height;
            int desiredCount = Mathf.Min(targetCount, totalCells);
            int campDistanceSquared = CampExclusionRadius * CampExclusionRadius;
            int spacingSquared = MinimumSpacing * MinimumSpacing;
            for (int iteration = 0; iteration < totalCells && selected.Count < desiredCount; iteration++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(
                    Mathf.Max(1, seed),
                    iteration,
                    totalCells,
                    DistributionSalt);
                Vector2Int candidate = new(cellIndex % width, cellIndex / width);
                if (!IsInsideEdgeMargin(candidate, width, height)
                    || !reachable[candidate.x, candidate.y]
                    || !isWalkable(candidate)
                    || !isBuildable(candidate)
                    || SquaredDistance(candidate, campCell) < campDistanceSquared
                    || IsTooCloseToSelected(candidate, selected, spacingSquared))
                {
                    continue;
                }

                selected.Add(candidate);
            }

            return selected;
        }

        private static bool[,] BuildReachableCells(
            int width,
            int height,
            Vector2Int campCell,
            Func<Vector2Int, bool> isWalkable)
        {
            if (!TryFindReachableSeed(
                    width,
                    height,
                    campCell,
                    isWalkable,
                    out Vector2Int seedCell))
            {
                return null;
            }

            bool[,] reachable = new bool[width, height];
            Queue<Vector2Int> open = new();
            reachable[seedCell.x, seedCell.y] = true;
            open.Enqueue(seedCell);
            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();
                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int next = current + Directions[i];
                    if (!IsInside(next, width, height)
                        || reachable[next.x, next.y]
                        || !isWalkable(next)
                        || IsBlockedDiagonal(current, next, isWalkable))
                    {
                        continue;
                    }

                    reachable[next.x, next.y] = true;
                    open.Enqueue(next);
                }
            }

            return reachable;
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
                        if (!IsInside(candidate, width, height) || !isWalkable(candidate))
                        {
                            continue;
                        }

                        int distanceSquared = x * x + y * y;
                        if (found
                            && (distanceSquared > bestDistanceSquared
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
            return deltaX != 0
                && deltaY != 0
                && (!isWalkable(new Vector2Int(from.x + deltaX, from.y))
                    || !isWalkable(new Vector2Int(from.x, from.y + deltaY)));
        }

        private static bool IsInsideEdgeMargin(Vector2Int cell, int width, int height)
        {
            return cell.x >= EdgeMargin
                && cell.y >= EdgeMargin
                && cell.x < width - EdgeMargin
                && cell.y < height - EdgeMargin;
        }

        private static bool IsTooCloseToSelected(
            Vector2Int candidate,
            IReadOnlyList<Vector2Int> selected,
            int spacingSquared)
        {
            for (int i = 0; i < selected.Count; i++)
            {
                if (SquaredDistance(candidate, selected[i]) < spacingSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private static int SquaredDistance(Vector2Int left, Vector2Int right)
        {
            int deltaX = left.x - right.x;
            int deltaY = left.y - right.y;
            return deltaX * deltaX + deltaY * deltaY;
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
    }
}
