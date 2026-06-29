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
            StrategyTrailRouteCellBuilder.BuildRouteCells(map, startCell, rawPathCells, pendingTrailRouteCells);
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
