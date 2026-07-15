using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyScoutTargetSelector
    {
        private const int UnknownCoverageRadius = 5;

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        public static bool TrySelectTarget(
            int width,
            int height,
            Vector2Int origin,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, bool> isExplored,
            Func<Vector2Int, bool> isUnavailable,
            out Vector2Int target)
        {
            target = default;
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            if (isWalkable == null)
            {
                throw new ArgumentNullException(nameof(isWalkable));
            }

            if (isExplored == null)
            {
                throw new ArgumentNullException(nameof(isExplored));
            }

            if (isUnavailable == null)
            {
                throw new ArgumentNullException(nameof(isUnavailable));
            }

            bool found = false;
            long bestDistanceSquared = long.MaxValue;
            int bestUnknownCoverage = int.MinValue;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (isExplored(candidate)
                        || !isWalkable(candidate)
                        || isUnavailable(candidate)
                        || !HasInBoundsExploredNeighbor(
                            candidate,
                            width,
                            height,
                            isExplored))
                    {
                        continue;
                    }

                    long deltaX = (long)x - origin.x;
                    long deltaY = (long)y - origin.y;
                    long distanceSquared = deltaX * deltaX + deltaY * deltaY;
                    int unknownCoverage = CountNearbyUnknownCells(
                        candidate,
                        width,
                        height,
                        isExplored);
                    if (!IsBetterCandidate(
                            found,
                            distanceSquared,
                            unknownCoverage,
                            candidate,
                            bestDistanceSquared,
                            bestUnknownCoverage,
                            target))
                    {
                        continue;
                    }

                    found = true;
                    target = candidate;
                    bestDistanceSquared = distanceSquared;
                    bestUnknownCoverage = unknownCoverage;
                }
            }

            return found;
        }

        private static bool HasInBoundsExploredNeighbor(
            Vector2Int candidate,
            int width,
            int height,
            Func<Vector2Int, bool> isExplored)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int neighbor = candidate + CardinalDirections[i];
                if (IsInBounds(neighbor, width, height) && isExplored(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountNearbyUnknownCells(
            Vector2Int candidate,
            int width,
            int height,
            Func<Vector2Int, bool> isExplored)
        {
            int unknownCount = 0;
            int radiusSquared = UnknownCoverageRadius * UnknownCoverageRadius;
            int minX = Math.Max(0, candidate.x - UnknownCoverageRadius);
            int maxX = Math.Min(width - 1, candidate.x + UnknownCoverageRadius);
            int minY = Math.Max(0, candidate.y - UnknownCoverageRadius);
            int maxY = Math.Min(height - 1, candidate.y + UnknownCoverageRadius);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int deltaX = x - candidate.x;
                    int deltaY = y - candidate.y;
                    if (deltaX * deltaX + deltaY * deltaY <= radiusSquared
                        && !isExplored(new Vector2Int(x, y)))
                    {
                        unknownCount++;
                    }
                }
            }

            return unknownCount;
        }

        private static bool IsBetterCandidate(
            bool found,
            long distanceSquared,
            int unknownCoverage,
            Vector2Int candidate,
            long bestDistanceSquared,
            int bestUnknownCoverage,
            Vector2Int bestCandidate)
        {
            if (!found || distanceSquared < bestDistanceSquared)
            {
                return true;
            }

            if (distanceSquared > bestDistanceSquared)
            {
                return false;
            }

            if (unknownCoverage != bestUnknownCoverage)
            {
                return unknownCoverage > bestUnknownCoverage;
            }

            return candidate.y < bestCandidate.y
                || (candidate.y == bestCandidate.y && candidate.x < bestCandidate.x);
        }

        private static bool IsInBounds(Vector2Int cell, int width, int height)
        {
            return cell.x >= 0
                && cell.x < width
                && cell.y >= 0
                && cell.y < height;
        }
    }
}
