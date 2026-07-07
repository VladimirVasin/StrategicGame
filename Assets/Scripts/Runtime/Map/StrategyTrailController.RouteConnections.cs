using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private readonly List<Vector2Int> routeConnectionCells = new();

        private void BuildSingleSidedRouteCells(IReadOnlyList<Vector2Int> sourceCells, List<Vector2Int> targetCells)
        {
            targetCells.Clear();
            if (sourceCells == null)
            {
                return;
            }

            for (int i = 0; i < sourceCells.Count; i++)
            {
                Vector2Int cell = sourceCells[i];
                if (GetWearRejectReason(cell) != null)
                {
                    continue;
                }

                bool added = TryAppendRouteConnectionCell(targetCells, cell, out string rejectReason);
                if (!added)
                {
                    if (!TryAppendSingleSidedConnector(targetCells, cell, rejectReason))
                    {
                        if (TryAppendRouteRepair(targetCells, sourceCells, i, rejectReason, out int repairedIndex))
                        {
                            i = repairedIndex;
                            continue;
                        }

                        StrategyDebugLogger.Info(
                            "Map",
                            "TrailRouteConnectorSkipped",
                            StrategyDebugLogger.F("cell", cell),
                            StrategyDebugLogger.F("reason", rejectReason));
                    }

                    continue;
                }

                TryAppendSingleSidedConnector(targetCells, cell, "near_existing");
            }
        }

        private bool TryAppendRouteConnectionCell(
            List<Vector2Int> targetCells,
            Vector2Int cell,
            out string rejectReason)
        {
            rejectReason = null;
            if (targetCells.Count > 0 && targetCells[targetCells.Count - 1] == cell)
            {
                return true;
            }

            if (ContainsRouteConnectionCell(targetCells, cell))
            {
                return true;
            }

            if (!CanReachRouteConnectionCell(targetCells, cell))
            {
                rejectReason = "disconnected_candidate";
                return false;
            }

            if (GetRawRouteTrailLevel(cell) <= 0 && WouldCompleteRouteSquare(cell, targetCells))
            {
                rejectReason = "square_candidate";
                return false;
            }

            targetCells.Add(cell);
            return true;
        }

        private bool CanReachRouteConnectionCell(List<Vector2Int> targetCells, Vector2Int cell)
        {
            if (targetCells.Count <= 0)
            {
                return true;
            }

            Vector2Int previous = targetCells[targetCells.Count - 1];
            if (IsCardinalNeighbor(previous, cell))
            {
                return true;
            }

            int existingNeighbors = CountExistingRouteTrailNeighbors(cell);
            if (GetRawRouteTrailLevel(cell) > 0)
            {
                return existingNeighbors > 0;
            }

            return existingNeighbors > 0 && HasRecentRouteNetworkContact(targetCells);
        }

        private bool TryAppendSingleSidedConnector(
            List<Vector2Int> targetCells,
            Vector2Int anchor,
            string reason)
        {
            if (targetCells.Count <= 0 || HasRecentRouteNetworkContact(targetCells))
            {
                return false;
            }

            if (TryAppendExistingRouteConnector(targetCells, anchor, reason))
            {
                return true;
            }

            Vector2Int best = default;
            int bestScore = int.MaxValue;
            for (int i = 0; i < NeighborCells.Length; i++)
            {
                Vector2Int candidate = anchor + NeighborCells[i];
                if (!CanUseSingleSidedConnector(targetCells, candidate))
                {
                    continue;
                }

                int score = GetSingleSidedConnectorScore(targetCells[targetCells.Count - 1], candidate);
                if (score >= bestScore)
                {
                    continue;
                }

                best = candidate;
                bestScore = score;
            }

            if (bestScore == int.MaxValue)
            {
                return false;
            }

            targetCells.Add(best);
            StrategyDebugLogger.Info(
                "Map",
                "TrailRouteConnectorAdded",
                StrategyDebugLogger.F("cell", best),
                StrategyDebugLogger.F("anchor", anchor),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("existingNeighbors", CountExistingRouteTrailNeighbors(best)));
            return true;
        }

        private bool TryAppendExistingRouteConnector(
            List<Vector2Int> targetCells,
            Vector2Int anchor,
            string reason)
        {
            Vector2Int previous = targetCells[targetCells.Count - 1];
            Vector2Int best = default;
            int bestScore = int.MaxValue;
            for (int i = 0; i < NeighborCells.Length; i++)
            {
                Vector2Int candidate = anchor + NeighborCells[i];
                if (GetRawRouteTrailLevel(candidate) <= 0
                    || ContainsRouteConnectionCell(targetCells, candidate)
                    || !IsCardinalNeighbor(previous, candidate))
                {
                    continue;
                }

                int score = GetSingleSidedConnectorScore(previous, candidate);
                if (score >= bestScore)
                {
                    continue;
                }

                best = candidate;
                bestScore = score;
            }

            if (bestScore == int.MaxValue)
            {
                return false;
            }

            targetCells.Add(best);
            StrategyDebugLogger.Info(
                "Map",
                "TrailRouteConnectorAdded",
                StrategyDebugLogger.F("cell", best),
                StrategyDebugLogger.F("anchor", anchor),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("existingRoute", true),
                StrategyDebugLogger.F("existingNeighbors", CountExistingRouteTrailNeighbors(best)));
            return true;
        }

        private bool CanUseSingleSidedConnector(List<Vector2Int> targetCells, Vector2Int candidate)
        {
            if (GetWearRejectReason(candidate) != null
                || GetRawRouteTrailLevel(candidate) > 0
                || ContainsRouteConnectionCell(targetCells, candidate)
                || CountExistingRouteTrailNeighbors(candidate) != 1
                || !IsCardinalNeighbor(targetCells[targetCells.Count - 1], candidate)
                || WouldCompleteRouteSquare(candidate, targetCells))
            {
                return false;
            }

            return true;
        }

        private int GetSingleSidedConnectorScore(Vector2Int previous, Vector2Int candidate)
        {
            int score = Mathf.Abs(previous.x - candidate.x) + Mathf.Abs(previous.y - candidate.y);
            score += CountCardinalRouteTrailNeighbors(candidate);
            return score;
        }

        private bool HasRecentRouteNetworkContact(List<Vector2Int> targetCells)
        {
            int start = Mathf.Max(0, targetCells.Count - 4);
            for (int i = targetCells.Count - 1; i >= start; i--)
            {
                if (CountExistingRouteTrailNeighbors(targetCells[i]) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountExistingRouteTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.up) > 0 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.right) > 0 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.down) > 0 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.left) > 0 ? 1 : 0;
            return count;
        }

        private static bool IsCardinalNeighbor(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx + dy == 1;
        }

        private bool WouldCompleteRouteSquare(Vector2Int cell, IReadOnlyList<Vector2Int> stagedCells)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    Vector2Int corner = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (HasRouteSquareCell(corner, cell, stagedCells)
                        && HasRouteSquareCell(corner + Vector2Int.right, cell, stagedCells)
                        && HasRouteSquareCell(corner + Vector2Int.up, cell, stagedCells)
                        && HasRouteSquareCell(corner + Vector2Int.one, cell, stagedCells))
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
            IReadOnlyList<Vector2Int> stagedCells)
        {
            return squareCell == candidate
                || GetRawRouteTrailLevel(squareCell) > 0
                || ContainsRouteConnectionCell(stagedCells, squareCell);
        }

        private static bool ContainsRouteConnectionCell(IReadOnlyList<Vector2Int> cells, Vector2Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
