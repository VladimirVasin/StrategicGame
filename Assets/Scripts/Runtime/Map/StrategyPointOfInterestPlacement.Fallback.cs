using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyPointOfInterestPlacement
    {
        private static void AppendResourcePlans(
            List<StrategyPointOfInterestPlan> result,
            IReadOnlyList<ResourceCandidate> candidates,
            int seed,
            int targetCount)
        {
            if (targetCount <= 0 || candidates == null || candidates.Count <= 0)
            {
                return;
            }

            StrategyPointOfInterestResourceKind firstKind = StableHash(
                    seed,
                    candidates.Count,
                    targetCount,
                    10037) % 2 == 0
                ? StrategyPointOfInterestResourceKind.Coal
                : StrategyPointOfInterestResourceKind.Iron;
            List<ResourceCandidate> selected = new();
            List<Vector2Int> selectedCells = new(result.Count);
            for (int i = 0; i < result.Count; i++)
            {
                selectedCells.Add(result[i].Cell);
            }

            int spacingSquared = MinimumSpacing * MinimumSpacing;
            for (int index = 0; index < targetCount; index++)
            {
                bool found = false;
                ResourceCandidate best = default;
                int bestSeparation = -1;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    ResourceCandidate candidate = candidates[candidateIndex];
                    if (IsTooCloseToSelected(candidate.Cell, selectedCells, spacingSquared)
                        || HasConflictingMineralSite(candidate.MineralOrigin, selected))
                    {
                        continue;
                    }

                    int separation = MinimumSquaredDistance(candidate.Cell, selectedCells);
                    if (found
                        && (separation < bestSeparation
                            || separation == bestSeparation && CompareResourceCandidates(candidate, best) >= 0))
                    {
                        continue;
                    }

                    found = true;
                    best = candidate;
                    bestSeparation = separation;
                }

                if (!found)
                {
                    return;
                }

                StrategyPointOfInterestResourceKind kind = index % 2 == 0
                    ? firstKind
                    : Opposite(firstKind);
                result.Add(new StrategyPointOfInterestPlan(best.Cell, kind, best.MineralOrigin));
                selected.Add(best);
                selectedCells.Add(best.Cell);
            }
        }

        private static bool HasConflictingMineralSite(
            Vector2Int origin,
            IReadOnlyList<ResourceCandidate> selected)
        {
            for (int i = 0; i < selected.Count; i++)
            {
                if (FootprintsTouchOrOverlap(
                        origin,
                        MineralFootprint,
                        selected[i].MineralOrigin,
                        MineralFootprint))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FootprintsTouchOrOverlap(
            Vector2Int leftOrigin,
            Vector2Int leftFootprint,
            Vector2Int rightOrigin,
            Vector2Int rightFootprint)
        {
            return leftOrigin.x <= rightOrigin.x + rightFootprint.x
                && leftOrigin.x + leftFootprint.x >= rightOrigin.x
                && leftOrigin.y <= rightOrigin.y + rightFootprint.y
                && leftOrigin.y + leftFootprint.y >= rightOrigin.y;
        }

        private static int MinimumSquaredDistance(
            Vector2Int candidate,
            IReadOnlyList<Vector2Int> selectedCells)
        {
            int minimum = int.MaxValue;
            for (int i = 0; i < selectedCells.Count; i++)
            {
                minimum = Mathf.Min(minimum, SquaredDistance(candidate, selectedCells[i]));
            }

            return minimum;
        }

        private static StrategyPointOfInterestResourceKind Opposite(
            StrategyPointOfInterestResourceKind kind)
        {
            return kind == StrategyPointOfInterestResourceKind.Coal
                ? StrategyPointOfInterestResourceKind.Iron
                : StrategyPointOfInterestResourceKind.Coal;
        }

        private static int CompareResourceCandidates(ResourceCandidate left, ResourceCandidate right)
        {
            int comparison = left.Rank.CompareTo(right.Rank);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareCells(left.Cell, right.Cell);
            return comparison != 0
                ? comparison
                : CompareCells(left.MineralOrigin, right.MineralOrigin);
        }

        private readonly struct ResourceCandidate
        {
            public ResourceCandidate(Vector2Int cell, Vector2Int mineralOrigin, int rank)
            {
                Cell = cell;
                MineralOrigin = mineralOrigin;
                Rank = rank;
            }

            public Vector2Int Cell { get; }
            public Vector2Int MineralOrigin { get; }
            public int Rank { get; }
        }
    }
}
