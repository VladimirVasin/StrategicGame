using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRefugeeArrivalController
    {
        private const int FogArrivalEntryOffsetCells = 4;
        private const int ArrivalEntryFormationPadding = 1;
        private const int FogEntrySideAttempts = 4;

        private bool TryGetRandomArrivalEntryCell(out Vector2Int entryCell, out Vector2 outward)
        {
            if (TryGetRandomFogPerimeterEntryCell(out entryCell, out outward))
            {
                return true;
            }

            return TryGetRandomMapEdgeFallbackEntryCell(out entryCell, out outward);
        }

        private bool TryGetRandomFogPerimeterEntryCell(out Vector2Int entryCell, out Vector2 outward)
        {
            entryCell = default;
            outward = Vector2.zero;
            if (map == null || fog == null)
            {
                return false;
            }

            for (int attempt = 0; attempt < FogEntrySideAttempts; attempt++)
            {
                int side = Random.Range(0, 4);
                if (TryGetFogPerimeterEntryForSide(side, out entryCell, out outward))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetFogPerimeterEntryForSide(int side, out Vector2Int entryCell, out Vector2 outward)
        {
            entryCell = default;
            outward = GetSideOutwardDirection(side);
            List<Vector2Int> candidates = new();

            if (side == 0 || side == 1)
            {
                for (int y = ArrivalEntryFormationPadding; y < map.Height - ArrivalEntryFormationPadding; y++)
                {
                    TryAddHorizontalFogEntryCandidate(side, y, candidates);
                }
            }
            else
            {
                for (int x = ArrivalEntryFormationPadding; x < map.Width - ArrivalEntryFormationPadding; x++)
                {
                    TryAddVerticalFogEntryCandidate(side, x, candidates);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            entryCell = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        private void TryAddHorizontalFogEntryCandidate(int side, int y, List<Vector2Int> candidates)
        {
            int start = side == 0 ? 0 : map.Width - 1;
            int end = side == 0 ? map.Width : -1;
            int step = side == 0 ? 1 : -1;
            for (int x = start; x != end; x += step)
            {
                if (!fog.IsCellVisibleAtDaylightRange(new Vector2Int(x, y)))
                {
                    continue;
                }

                int entryX = side == 0 ? x - FogArrivalEntryOffsetCells : x + FogArrivalEntryOffsetCells;
                TryAddArrivalEntryCandidate(new Vector2Int(entryX, y), side, true, candidates);
                return;
            }
        }

        private void TryAddVerticalFogEntryCandidate(int side, int x, List<Vector2Int> candidates)
        {
            int start = side == 2 ? 0 : map.Height - 1;
            int end = side == 2 ? map.Height : -1;
            int step = side == 2 ? 1 : -1;
            for (int y = start; y != end; y += step)
            {
                if (!fog.IsCellVisibleAtDaylightRange(new Vector2Int(x, y)))
                {
                    continue;
                }

                int entryY = side == 2 ? y - FogArrivalEntryOffsetCells : y + FogArrivalEntryOffsetCells;
                TryAddArrivalEntryCandidate(new Vector2Int(x, entryY), side, true, candidates);
                return;
            }
        }

        private void TryAddArrivalEntryCandidate(
            Vector2Int candidate,
            int side,
            bool requireHidden,
            List<Vector2Int> candidates)
        {
            if (!IsArrivalEntryCellCandidate(candidate, side, requireHidden)
                || candidates.Contains(candidate))
            {
                return;
            }

            candidates.Add(candidate);
        }

        private bool IsArrivalEntryCellCandidate(Vector2Int candidate, int side, bool requireHidden)
        {
            return IsCellInsideMap(candidate)
                && IsFormationInsideMap(candidate, side)
                && map.IsCellWalkable(candidate)
                && (!requireHidden || fog == null || !fog.IsCellVisibleAtDaylightRange(candidate));
        }

        private bool TryGetRandomMapEdgeFallbackEntryCell(out Vector2Int entryCell, out Vector2 outward)
        {
            entryCell = default;
            outward = Vector2.zero;
            for (int attempt = 0; attempt < MaxRouteAttempts; attempt++)
            {
                int side = Random.Range(0, 4);
                entryCell = GetRandomMapEdgeCell(side);
                outward = GetSideOutwardDirection(side);
                if (IsArrivalEntryCellCandidate(entryCell, side, false))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int GetRandomMapEdgeCell(int side)
        {
            return side switch
            {
                0 => new Vector2Int(0, GetRandomInnerCoordinate(map.Height)),
                1 => new Vector2Int(map.Width - 1, GetRandomInnerCoordinate(map.Height)),
                2 => new Vector2Int(GetRandomInnerCoordinate(map.Width), 0),
                _ => new Vector2Int(GetRandomInnerCoordinate(map.Width), map.Height - 1)
            };
        }

        private static Vector2 GetSideOutwardDirection(int side)
        {
            return side switch
            {
                0 => Vector2.left,
                1 => Vector2.right,
                2 => Vector2.down,
                _ => Vector2.up
            };
        }

        private static int GetRandomInnerCoordinate(int size)
        {
            if (size <= ArrivalEntryFormationPadding * 2)
            {
                return Random.Range(0, Mathf.Max(1, size));
            }

            return Random.Range(ArrivalEntryFormationPadding, size - ArrivalEntryFormationPadding);
        }

        private bool IsFormationInsideMap(Vector2Int cell, int side)
        {
            if (side == 0 || side == 1)
            {
                return cell.y >= ArrivalEntryFormationPadding
                    && cell.y < map.Height - ArrivalEntryFormationPadding;
            }

            return cell.x >= ArrivalEntryFormationPadding
                && cell.x < map.Width - ArrivalEntryFormationPadding;
        }

        private bool IsCellInsideMap(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < map.Width && cell.y >= 0 && cell.y < map.Height;
        }
    }
}
