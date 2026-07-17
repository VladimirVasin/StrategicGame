using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal readonly struct StrategyStoryPointOfInterestCandidatePlan
    {
        public StrategyStoryPointOfInterestCandidatePlan(
            Vector2Int cell,
            StrategyStoryPointOfInterestDistanceTier distanceTier,
            int routeSteps,
            int rank)
        {
            Cell = cell;
            DistanceTier = distanceTier;
            RouteSteps = routeSteps;
            Rank = rank;
        }

        public Vector2Int Cell { get; }
        public StrategyStoryPointOfInterestDistanceTier DistanceTier { get; }
        public int RouteSteps { get; }
        internal int Rank { get; }
    }

    internal static class StrategyStoryPointOfInterestPlacement
    {
        internal const int Tier1MinimumRouteSteps = 18;
        internal const int Tier1MaximumRouteSteps = 30;
        internal const int Tier2MinimumRouteSteps = 45;
        internal const int Tier2MaximumRouteSteps = 70;
        internal const int Tier3MinimumRouteSteps = 85;
        internal const int Tier3MaximumRouteSteps = 120;
        internal const int Tier1MinimumCandidateCount = 24;
        internal const int Tier2MinimumCandidateCount = 12;
        internal const int Tier3MinimumCandidateCount = 8;

        private const int EdgeMargin = 6;
        private const int MinimumCandidateSpacing = 4;
        private const int PlacementSalt = 7349;

        public static List<StrategyStoryPointOfInterestCandidatePlan> SelectCandidates(
            int width,
            int height,
            int seed,
            Vector2Int campCell,
            StrategyStoryPointOfInterestDistanceTier distanceTier,
            int targetCount,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isAvailable)
        {
            if (isWalkable == null)
            {
                throw new ArgumentNullException(nameof(isWalkable));
            }

            if (isAvailable == null)
            {
                throw new ArgumentNullException(nameof(isAvailable));
            }

            List<StrategyStoryPointOfInterestCandidatePlan> selected = new();
            if (width <= 0
                || height <= 0
                || targetCount <= 0
                || !TryGetRouteRange(distanceTier, out int minimumSteps, out int maximumSteps))
            {
                return selected;
            }

            int[,] routeCosts = StrategyStoryPointOfInterestRouteField.Build(
                width,
                height,
                campCell,
                isWalkable);
            if (routeCosts == null)
            {
                return selected;
            }

            List<StrategyStoryPointOfInterestCandidatePlan> pool = new();
            for (int y = EdgeMargin; y < height - EdgeMargin; y++)
            {
                for (int x = EdgeMargin; x < width - EdgeMargin; x++)
                {
                    int routeCost = routeCosts[x, y];
                    int steps = routeCost < 0 ? -1 : (routeCost + 9) / 10;
                    Vector2Int cell = new(x, y);
                    if (steps < minimumSteps
                        || steps > maximumSteps
                        || !isWalkable(cell)
                        || !isAvailable(cell))
                    {
                        continue;
                    }

                    pool.Add(new StrategyStoryPointOfInterestCandidatePlan(
                        cell,
                        distanceTier,
                        steps,
                        StableHash(seed, cell.x, cell.y, PlacementSalt + (int)distanceTier * 101)));
                }
            }

            int sectorCount = Mathf.Min(targetCount, GetDirectionalCandidateCount(distanceTier));
            HashSet<Vector2Int> used = new();
            for (int sector = 0; sector < sectorCount && selected.Count < targetCount; sector++)
            {
                if (TrySelectSectorCandidate(
                        pool,
                        selected,
                        used,
                        campCell,
                        sector,
                        sectorCount,
                        out StrategyStoryPointOfInterestCandidatePlan candidate))
                {
                    selected.Add(candidate);
                    used.Add(candidate.Cell);
                }
            }

            while (selected.Count < targetCount
                && TrySelectFarthestCandidate(
                    pool,
                    selected,
                    used,
                    out StrategyStoryPointOfInterestCandidatePlan candidate))
            {
                selected.Add(candidate);
                used.Add(candidate.Cell);
            }

            return selected;
        }

        public static bool IsInsideTier(
            StrategyStoryPointOfInterestDistanceTier distanceTier,
            int routeSteps)
        {
            return TryGetRouteRange(distanceTier, out int minimum, out int maximum)
                && routeSteps >= minimum
                && routeSteps <= maximum;
        }

        public static int GetMinimumCandidateCount(
            StrategyStoryPointOfInterestDistanceTier distanceTier)
        {
            return distanceTier switch
            {
                StrategyStoryPointOfInterestDistanceTier.Tier1Near => Tier1MinimumCandidateCount,
                StrategyStoryPointOfInterestDistanceTier.Tier2Middle => Tier2MinimumCandidateCount,
                StrategyStoryPointOfInterestDistanceTier.Tier3Far => Tier3MinimumCandidateCount,
                _ => 0
            };
        }

        private static int GetDirectionalCandidateCount(
            StrategyStoryPointOfInterestDistanceTier distanceTier)
        {
            return distanceTier switch
            {
                StrategyStoryPointOfInterestDistanceTier.Tier1Near => 16,
                StrategyStoryPointOfInterestDistanceTier.Tier2Middle => 12,
                StrategyStoryPointOfInterestDistanceTier.Tier3Far => 8,
                _ => 0
            };
        }

        private static bool TrySelectSectorCandidate(
            IReadOnlyList<StrategyStoryPointOfInterestCandidatePlan> pool,
            IReadOnlyList<StrategyStoryPointOfInterestCandidatePlan> selected,
            ISet<Vector2Int> used,
            Vector2Int campCell,
            int sector,
            int sectorCount,
            out StrategyStoryPointOfInterestCandidatePlan result)
        {
            result = default;
            bool found = false;
            for (int i = 0; i < pool.Count; i++)
            {
                StrategyStoryPointOfInterestCandidatePlan candidate = pool[i];
                if (used.Contains(candidate.Cell)
                    || GetSector(candidate.Cell, campCell, sectorCount) != sector
                    || IsTooClose(candidate.Cell, selected))
                {
                    continue;
                }

                if (!found
                    || IsBetterSectorCandidate(
                        candidate,
                        result,
                        campCell,
                        sector,
                        sectorCount))
                {
                    found = true;
                    result = candidate;
                }
            }

            return found;
        }

        private static bool TrySelectFarthestCandidate(
            IReadOnlyList<StrategyStoryPointOfInterestCandidatePlan> pool,
            IReadOnlyList<StrategyStoryPointOfInterestCandidatePlan> selected,
            ISet<Vector2Int> used,
            out StrategyStoryPointOfInterestCandidatePlan result)
        {
            result = default;
            bool found = false;
            int bestSeparation = -1;
            for (int i = 0; i < pool.Count; i++)
            {
                StrategyStoryPointOfInterestCandidatePlan candidate = pool[i];
                if (used.Contains(candidate.Cell) || IsTooClose(candidate.Cell, selected))
                {
                    continue;
                }

                int separation = MinimumSquaredDistance(candidate.Cell, selected);
                if (!found
                    || separation > bestSeparation
                    || separation == bestSeparation && IsBetterInnerCandidate(candidate, result))
                {
                    found = true;
                    result = candidate;
                    bestSeparation = separation;
                }
            }

            return found;
        }

        private static bool TryGetRouteRange(
            StrategyStoryPointOfInterestDistanceTier distanceTier,
            out int minimum,
            out int maximum)
        {
            switch (distanceTier)
            {
                case StrategyStoryPointOfInterestDistanceTier.Tier1Near:
                    minimum = Tier1MinimumRouteSteps;
                    maximum = Tier1MaximumRouteSteps;
                    return true;
                case StrategyStoryPointOfInterestDistanceTier.Tier2Middle:
                    minimum = Tier2MinimumRouteSteps;
                    maximum = Tier2MaximumRouteSteps;
                    return true;
                case StrategyStoryPointOfInterestDistanceTier.Tier3Far:
                    minimum = Tier3MinimumRouteSteps;
                    maximum = Tier3MaximumRouteSteps;
                    return true;
                default:
                    minimum = 0;
                    maximum = -1;
                    return false;
            }
        }

        private static bool IsBetterInnerCandidate(
            StrategyStoryPointOfInterestCandidatePlan candidate,
            StrategyStoryPointOfInterestCandidatePlan current)
        {
            if (candidate.RouteSteps != current.RouteSteps)
            {
                return candidate.RouteSteps < current.RouteSteps;
            }

            if (candidate.Rank != current.Rank)
            {
                return candidate.Rank < current.Rank;
            }

            return CompareCells(candidate.Cell, current.Cell) < 0;
        }

        private static bool IsBetterSectorCandidate(
            StrategyStoryPointOfInterestCandidatePlan candidate,
            StrategyStoryPointOfInterestCandidatePlan current,
            Vector2Int campCell,
            int sector,
            int sectorCount)
        {
            float candidateDeviation = GetSectorCenterDeviation(
                candidate.Cell,
                campCell,
                sector,
                sectorCount);
            float currentDeviation = GetSectorCenterDeviation(
                current.Cell,
                campCell,
                sector,
                sectorCount);
            if (!Mathf.Approximately(candidateDeviation, currentDeviation))
            {
                return candidateDeviation < currentDeviation;
            }

            return IsBetterInnerCandidate(candidate, current);
        }

        private static int GetSector(Vector2Int cell, Vector2Int campCell, int sectorCount)
        {
            float angle = Mathf.Atan2(
                cell.y - campCell.y,
                cell.x - campCell.x) * Mathf.Rad2Deg;
            float sectorWidth = 360f / sectorCount;
            float shifted = Mathf.Repeat(angle + 180f + sectorWidth * 0.5f, 360f);
            return Mathf.Clamp(Mathf.FloorToInt(shifted / sectorWidth), 0, sectorCount - 1);
        }

        private static float GetSectorCenterDeviation(
            Vector2Int cell,
            Vector2Int campCell,
            int sector,
            int sectorCount)
        {
            float candidateAngle = Mathf.Atan2(
                cell.y - campCell.y,
                cell.x - campCell.x) * Mathf.Rad2Deg;
            float sectorWidth = 360f / sectorCount;
            float sectorCenter = -180f + sector * sectorWidth;
            return Mathf.Abs(Mathf.DeltaAngle(candidateAngle, sectorCenter));
        }

        private static bool IsTooClose(
            Vector2Int candidate,
            IReadOnlyList<StrategyStoryPointOfInterestCandidatePlan> selected)
        {
            int minimumSquared = MinimumCandidateSpacing * MinimumCandidateSpacing;
            return MinimumSquaredDistance(candidate, selected) < minimumSquared;
        }

        private static int MinimumSquaredDistance(
            Vector2Int candidate,
            IReadOnlyList<StrategyStoryPointOfInterestCandidatePlan> selected)
        {
            int minimum = int.MaxValue;
            for (int i = 0; i < selected.Count; i++)
            {
                Vector2Int delta = candidate - selected[i].Cell;
                minimum = Mathf.Min(minimum, delta.x * delta.x + delta.y * delta.y);
            }

            return minimum;
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

        private static int CompareCells(Vector2Int left, Vector2Int right)
        {
            int yComparison = left.y.CompareTo(right.y);
            return yComparison != 0 ? yComparison : left.x.CompareTo(right.x);
        }

    }
}
