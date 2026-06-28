using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const int TrailRouteEndpointRadius = 1;

        private readonly List<Vector2Int> pendingTrailRouteCells = new();
        private StrategyPlacedBuilding pendingTrailRouteStartBuilding;
        private StrategyPlacedBuilding pendingTrailRouteEndBuilding;
        private bool suppressTrailRouteCapture;

        private void PrepareTrailRouteForBuiltPath(Vector2Int startCell, Vector2Int targetCell, IReadOnlyList<Vector2Int> rawPathCells)
        {
            if (suppressTrailRouteCapture)
            {
                return;
            }

            ClearPendingTrailRoute();
            if (map == null
                || rawPathCells == null
                || rawPathCells.Count <= 0
                || !TryFindTrailRouteEndpointBuilding(startCell, out StrategyPlacedBuilding startBuilding)
                || !TryFindTrailRouteEndpointBuilding(targetCell, out StrategyPlacedBuilding endBuilding)
                || startBuilding == endBuilding)
            {
                return;
            }

            pendingTrailRouteStartBuilding = startBuilding;
            pendingTrailRouteEndBuilding = endBuilding;
            BuildPendingTrailRouteCells(startCell, rawPathCells);
            if (pendingTrailRouteCells.Count < 2)
            {
                ClearPendingTrailRoute();
            }
        }

        private void CompletePendingTrailRouteTraversal()
        {
            if (pendingTrailRouteStartBuilding != null
                && pendingTrailRouteEndBuilding != null
                && pendingTrailRouteCells.Count >= 2)
            {
                StrategyTrailController.Active?.RecordBuildingRouteTraversal(
                    pendingTrailRouteStartBuilding,
                    pendingTrailRouteEndBuilding,
                    pendingTrailRouteCells);
            }

            ClearPendingTrailRoute();
        }

        private void ClearPendingTrailRoute()
        {
            pendingTrailRouteStartBuilding = null;
            pendingTrailRouteEndBuilding = null;
            pendingTrailRouteCells.Clear();
        }

        private TrailRouteState CaptureTrailRouteState()
        {
            return new TrailRouteState(
                pendingTrailRouteStartBuilding,
                pendingTrailRouteEndBuilding,
                new List<Vector2Int>(pendingTrailRouteCells),
                suppressTrailRouteCapture);
        }

        private void RestoreTrailRouteState(TrailRouteState state)
        {
            pendingTrailRouteStartBuilding = state.StartBuilding;
            pendingTrailRouteEndBuilding = state.EndBuilding;
            pendingTrailRouteCells.Clear();
            pendingTrailRouteCells.AddRange(state.Cells);
            suppressTrailRouteCapture = state.SuppressCapture;
        }

        private void BuildPendingTrailRouteCells(Vector2Int startCell, IReadOnlyList<Vector2Int> rawPathCells)
        {
            AddPendingTrailRouteCell(startCell);
            Vector2Int previous = startCell;
            for (int i = 0; i < rawPathCells.Count; i++)
            {
                Vector2Int next = rawPathCells[i];
                AddPendingTrailRouteSegment(previous, next);
                previous = next;
            }
        }

        private void AddPendingTrailRouteSegment(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
            {
                bool horizontalFirst = ((from.x + from.y + to.x + to.y) & 1) == 0;
                Vector2Int first = horizontalFirst
                    ? new Vector2Int(to.x, from.y)
                    : new Vector2Int(from.x, to.y);
                Vector2Int fallback = horizontalFirst
                    ? new Vector2Int(from.x, to.y)
                    : new Vector2Int(to.x, from.y);
                AddPendingTrailRouteCell(map.IsCellWalkable(first) ? first : fallback);
                AddPendingTrailRouteCell(to);
                return;
            }

            AddPendingTrailRouteCell(to);
        }

        private void AddPendingTrailRouteCell(Vector2Int cell)
        {
            if (pendingTrailRouteCells.Count > 0 && pendingTrailRouteCells[pendingTrailRouteCells.Count - 1] == cell)
            {
                return;
            }

            pendingTrailRouteCells.Add(cell);
        }

        private bool TryFindTrailRouteEndpointBuilding(Vector2Int cell, out StrategyPlacedBuilding building)
        {
            building = null;
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            float bestScore = float.MaxValue;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding candidate = buildings[i];
                if (candidate == null
                    || candidate.Tool == StrategyBuildTool.Bridge
                    || !IsTrailRouteEndpointNearBuilding(cell, candidate))
                {
                    continue;
                }

                Vector3 cellWorld = map.GetCellCenterWorld(cell.x, cell.y);
                float score = (candidate.FootprintBounds.center - cellWorld).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    building = candidate;
                }
            }

            return building != null;
        }

        private static bool IsTrailRouteEndpointNearBuilding(Vector2Int cell, StrategyPlacedBuilding building)
        {
            Vector2Int origin = building.Origin;
            Vector2Int footprint = building.Footprint;
            return cell.x >= origin.x - TrailRouteEndpointRadius
                && cell.x < origin.x + footprint.x + TrailRouteEndpointRadius
                && cell.y >= origin.y - TrailRouteEndpointRadius
                && cell.y < origin.y + footprint.y + TrailRouteEndpointRadius;
        }

        private readonly struct TrailRouteState
        {
            public TrailRouteState(
                StrategyPlacedBuilding startBuilding,
                StrategyPlacedBuilding endBuilding,
                List<Vector2Int> cells,
                bool suppressCapture)
            {
                StartBuilding = startBuilding;
                EndBuilding = endBuilding;
                Cells = cells;
                SuppressCapture = suppressCapture;
            }

            public StrategyPlacedBuilding StartBuilding { get; }
            public StrategyPlacedBuilding EndBuilding { get; }
            public List<Vector2Int> Cells { get; }
            public bool SuppressCapture { get; }
        }
    }
}
