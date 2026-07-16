using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal readonly struct StrategyPointOfInterestPlan
    {
        public StrategyPointOfInterestPlan(Vector2Int cell)
        {
            Cell = cell;
            ResourceKind = StrategyPointOfInterestResourceKind.None;
            MineralOrigin = default;
        }

        public StrategyPointOfInterestPlan(
            Vector2Int cell,
            StrategyPointOfInterestResourceKind resourceKind,
            Vector2Int mineralOrigin)
        {
            Cell = cell;
            ResourceKind = resourceKind;
            MineralOrigin = mineralOrigin;
        }

        public Vector2Int Cell { get; }
        public StrategyPointOfInterestResourceKind ResourceKind { get; }
        public Vector2Int MineralOrigin { get; }
        public bool HasMineralSite => ResourceKind != StrategyPointOfInterestResourceKind.None;
    }

    internal static partial class StrategyPointOfInterestPlacement
    {
        internal const int MineralFreeRadius = 6;
        internal const int MineralPointMinDistance = 3;
        internal const int MineralPointMaxDistance = 5;
        internal const int CampMineralExclusionRadius = 24;

        internal static readonly Vector2Int MineralFootprint = new(2, 2);
        internal static readonly Vector2Int ExtractionBlockFootprint = new(2, 3);

        private const int MineralDistributionSalt = 9917;
        private const int MaximumResourceCandidates = 768;

        public static List<StrategyPointOfInterestPlan> SelectMineralPlans(
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

            List<StrategyPointOfInterestPlan> result = new();
            if (width <= 0 || height <= 0 || targetCount <= 0)
            {
                return result;
            }

            bool[,] reachable = BuildReachableCells(width, height, campCell, isWalkable);
            if (reachable == null
                || !TrySelectFirstPoint(
                    width,
                    height,
                    seed,
                    campCell,
                    reachable,
                    isWalkable,
                    isBuildable,
                    out Vector2Int firstCell))
            {
                return result;
            }

            result.Add(new StrategyPointOfInterestPlan(firstCell));
            if (targetCount == 1)
            {
                return result;
            }

            List<ResourceCandidate> candidates = BuildResourceCandidates(
                width,
                height,
                seed,
                campCell,
                firstCell,
                reachable,
                isWalkable,
                isBuildable);
            AppendResourcePlans(result, candidates, seed, targetCount - 1);
            return result;
        }

        private static bool TrySelectFirstPoint(
            int width,
            int height,
            int seed,
            Vector2Int campCell,
            bool[,] reachable,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isBuildable,
            out Vector2Int selected)
        {
            selected = default;
            bool found = false;
            int bestDistance = int.MaxValue;
            int bestRank = int.MaxValue;
            int totalCells = width * height;
            for (int i = 0; i < totalCells; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(
                    Mathf.Max(1, seed),
                    i,
                    totalCells,
                    MineralDistributionSalt);
                Vector2Int candidate = new(cellIndex % width, cellIndex / width);
                if (!IsBaseCandidate(
                        candidate,
                        width,
                        height,
                        campCell,
                        reachable,
                        isWalkable,
                        isBuildable))
                {
                    continue;
                }

                int distance = SquaredDistance(candidate, campCell);
                int rank = StableHash(seed, candidate.x, candidate.y, MineralDistributionSalt);
                if (found
                    && (distance > bestDistance
                        || distance == bestDistance && rank > bestRank
                        || distance == bestDistance
                        && rank == bestRank
                        && CompareCells(candidate, selected) >= 0))
                {
                    continue;
                }

                found = true;
                selected = candidate;
                bestDistance = distance;
                bestRank = rank;
            }

            return found;
        }

        private static List<ResourceCandidate> BuildResourceCandidates(
            int width,
            int height,
            int seed,
            Vector2Int campCell,
            Vector2Int firstCell,
            bool[,] reachable,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isBuildable)
        {
            List<ResourceCandidate> candidates = new();
            int totalCells = width * height;
            for (int i = 0; i < totalCells && candidates.Count < MaximumResourceCandidates; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(
                    Mathf.Max(1, seed),
                    i,
                    totalCells,
                    MineralDistributionSalt + 101);
                Vector2Int origin = new(cellIndex % width, cellIndex / width);
                if (!CanUseExtractionSite(
                        origin,
                        width,
                        height,
                        campCell,
                        reachable,
                        isWalkable,
                        isBuildable))
                {
                    continue;
                }

                TryAppendMarkerCandidates(
                    candidates,
                    origin,
                    width,
                    height,
                    seed,
                    campCell,
                    firstCell,
                    reachable,
                    isWalkable,
                    isBuildable);
            }

            candidates.Sort(CompareResourceCandidates);
            return candidates;
        }

        private static void TryAppendMarkerCandidates(
            List<ResourceCandidate> target,
            Vector2Int mineralOrigin,
            int width,
            int height,
            int seed,
            Vector2Int campCell,
            Vector2Int firstCell,
            bool[,] reachable,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isBuildable)
        {
            int spacingSquared = MinimumSpacing * MinimumSpacing;
            int minX = mineralOrigin.x - MineralPointMaxDistance;
            int maxX = mineralOrigin.x + MineralFootprint.x - 1 + MineralPointMaxDistance;
            int minY = mineralOrigin.y - MineralPointMaxDistance;
            int maxY = mineralOrigin.y + MineralFootprint.y - 1 + MineralPointMaxDistance;
            ResourceCandidate best = default;
            bool found = false;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2Int cell = new(x, y);
                    int distance = DistanceToFootprint(cell, mineralOrigin, MineralFootprint);
                    if (distance < MineralPointMinDistance
                        || distance > MineralPointMaxDistance
                        || SquaredDistance(cell, firstCell) < spacingSquared
                        || !IsBaseCandidate(
                            cell,
                            width,
                            height,
                            campCell,
                            reachable,
                            isWalkable,
                            isBuildable))
                    {
                        continue;
                    }

                    int rank = StableHash(
                        seed,
                        cell.x,
                        cell.y,
                        MineralDistributionSalt + mineralOrigin.x * 31 + mineralOrigin.y * 17);
                    ResourceCandidate candidate = new(cell, mineralOrigin, rank);
                    if (!found || CompareResourceCandidates(candidate, best) < 0)
                    {
                        found = true;
                        best = candidate;
                    }
                }
            }

            if (found)
            {
                target.Add(best);
            }
        }

        private static bool CanUseExtractionSite(
            Vector2Int origin,
            int width,
            int height,
            Vector2Int campCell,
            bool[,] reachable,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isBuildable)
        {
            if (DistanceToFootprint(campCell, origin, MineralFootprint) <= CampMineralExclusionRadius)
            {
                return false;
            }

            for (int y = 0; y < ExtractionBlockFootprint.y; y++)
            {
                for (int x = 0; x < ExtractionBlockFootprint.x; x++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    if (!IsInside(cell, width, height)
                        || !reachable[cell.x, cell.y]
                        || !isWalkable(cell)
                        || !isBuildable(cell))
                    {
                        return false;
                    }
                }
            }

            return HasBuilderAccessOutsideExtractionBlock(
                origin,
                width,
                height,
                reachable,
                isWalkable);
        }

        private static bool HasBuilderAccessOutsideExtractionBlock(
            Vector2Int origin,
            int width,
            int height,
            bool[,] reachable,
            Func<Vector2Int, bool> isWalkable)
        {
            for (int radius = 1; radius <= 2; radius++)
            {
                for (int y = -radius; y < ExtractionBlockFootprint.y + radius; y++)
                {
                    for (int x = -radius; x < ExtractionBlockFootprint.x + radius; x++)
                    {
                        bool edge = x == -radius
                            || y == -radius
                            || x == ExtractionBlockFootprint.x + radius - 1
                            || y == ExtractionBlockFootprint.y + radius - 1;
                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (edge
                            && IsInside(candidate, width, height)
                            && !Contains(candidate, origin, ExtractionBlockFootprint)
                            && reachable[candidate.x, candidate.y]
                            && isWalkable(candidate))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsBaseCandidate(
            Vector2Int cell,
            int width,
            int height,
            Vector2Int campCell,
            bool[,] reachable,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isBuildable)
        {
            int campRadiusSquared = CampExclusionRadius * CampExclusionRadius;
            return IsInsideEdgeMargin(cell, width, height)
                && reachable[cell.x, cell.y]
                && isWalkable(cell)
                && isBuildable(cell)
                && SquaredDistance(cell, campCell) >= campRadiusSquared;
        }

        internal static int DistanceToFootprint(
            Vector2Int cell,
            Vector2Int origin,
            Vector2Int footprint)
        {
            int maxX = origin.x + Mathf.Max(1, footprint.x) - 1;
            int maxY = origin.y + Mathf.Max(1, footprint.y) - 1;
            int deltaX = cell.x < origin.x
                ? origin.x - cell.x
                : cell.x > maxX ? cell.x - maxX : 0;
            int deltaY = cell.y < origin.y
                ? origin.y - cell.y
                : cell.y > maxY ? cell.y - maxY : 0;
            return Mathf.Max(deltaX, deltaY);
        }

        private static bool Contains(
            Vector2Int cell,
            Vector2Int origin,
            Vector2Int footprint)
        {
            return cell.x >= origin.x
                && cell.y >= origin.y
                && cell.x < origin.x + footprint.x
                && cell.y < origin.y + footprint.y;
        }

        private static int StableHash(int seed, int x, int y, int salt)
        {
            unchecked
            {
                int hash = seed;
                hash = hash * 374761393 + x * 668265263;
                hash = hash * 1274126177 + y * 461845907;
                hash = hash * 1103515245 + salt * 12345;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & int.MaxValue;
            }
        }
    }
}
