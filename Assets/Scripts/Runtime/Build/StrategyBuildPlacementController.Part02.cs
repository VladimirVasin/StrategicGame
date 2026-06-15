using System.Collections.Generic;
using UnityEngine;
namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
        private void TryInstallDefaultGardenBeds(StrategyPlacedBuilding building)
        {
            if (building == null || building.Tool != StrategyBuildTool.House || building.HasUpgrade(StrategyBuildingUpgradeType.GardenBeds))
            {
                return;
            }

            if (upgrades == null)
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "DefaultGardenBedsSkipped",
                    StrategyDebugLogger.F("houseOrigin", building.Origin),
                    StrategyDebugLogger.F("reason", "upgrades_not_ready"));
                return;
            }

            if (!upgrades.TryInstallDefaultGardenBeds(
                    building,
                    out _,
                    out StrategyBuildingUpgradeInstallFailureReason failureReason))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "DefaultGardenBedsSkipped",
                    StrategyDebugLogger.F("houseOrigin", building.Origin),
                    StrategyDebugLogger.F("reason", failureReason));
            }
        }

        private void AddLabel(Transform parent, StrategyBuildToolInfo toolInfo)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            labelObject.transform.localScale = Vector3.one * 0.14f;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = GetAbbreviation(toolInfo.Tool);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = 1f;
            label.color = Color.white;

            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sortingOrder = StrategyWorldSorting.ForPosition(parent.position, 1);
            }
        }

        private bool CanPlace(Vector2Int origin, StrategyBuildToolInfo toolInfo)
        {
            if (toolInfo.Tool == StrategyBuildTool.Bridge)
            {
                return false;
            }

            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int finalBlockOrigin, out Vector2Int finalBlockFootprint);
            return CanPlaceFoundation(origin, toolInfo.Footprint, toolInfo.Tool)
                && CanReserveFinalBlock(finalBlockOrigin, finalBlockFootprint, toolInfo.Tool)
                && HasBuilderWorkAccess(origin, toolInfo.Footprint)
                && (toolInfo.Tool != StrategyBuildTool.FisherHut || HasFishingWaterAccess(origin))
                && HasRequiredDepositAccess(toolInfo.Tool, origin, toolInfo.Footprint, out _);
        }

        private bool TryGetBridgeCandidate(Vector2Int endCell, out BridgeCandidate bridgeCandidate)
        {
            for (int i = 0; i < bridgeCandidates.Count; i++)
            {
                BridgeCandidate candidate = bridgeCandidates[i];
                if (candidate.EndCell == endCell)
                {
                    bridgeCandidate = candidate;
                    return true;
                }
            }

            bridgeCandidate = default;
            return false;
        }

        private void BuildBridgeCandidates(Vector2Int startCell, List<BridgeCandidate> results)
        {
            results.Clear();
            Vector2Int[] directions = GetBridgeCrossingDirections();
            for (int i = 0; i < directions.Length; i++)
            {
                if (TryBuildBridgeCandidate(startCell, directions[i], out BridgeCandidate candidate)
                    && !ContainsBridgeCandidate(results, candidate.EndCell))
                {
                    results.Add(candidate);
                }
            }
        }

        private bool TryBuildBridgeCandidate(Vector2Int startCell, Vector2Int direction, out BridgeCandidate candidate)
        {
            candidate = default;
            if (direction == Vector2Int.zero)
            {
                return false;
            }

            List<Vector2Int> cells = new() { startCell };
            Vector2Int current = startCell + direction;
            int riverCells = 0;
            while (riverCells < MaxBridgeRiverCells)
            {
                if (!map.TryGetCell(current.x, current.y, out CityMapCell cell))
                {
                    return false;
                }

                if (cell.Kind != CityMapCellKind.Water || !cell.IsRiver)
                {
                    break;
                }

                if (!CanUseBridgeSpanCell(current, out _))
                {
                    return false;
                }

                cells.Add(current);
                riverCells++;
                current += direction;
            }

            if (riverCells <= 0 || !IsValidBridgeBankCell(current, out _))
            {
                return false;
            }

            cells.Add(current);
            candidate = new BridgeCandidate(current, direction, cells);
            return true;
        }

        private bool CanPlaceBridgeSpan(Vector2Int startCell, BridgeCandidate bridgeCandidate, out string reason)
        {
            if (!IsValidBridgeBankCell(startCell, out reason))
            {
                return false;
            }

            if (!IsValidBridgeBankCell(bridgeCandidate.EndCell, out reason))
            {
                return false;
            }

            int waterCells = 0;
            for (int i = 0; i < bridgeCandidate.Cells.Count; i++)
            {
                Vector2Int cell = bridgeCandidate.Cells[i];
                if (cell == startCell || cell == bridgeCandidate.EndCell)
                {
                    continue;
                }

                if (!CanUseBridgeSpanCell(cell, out reason))
                {
                    return false;
                }

                waterCells++;
            }

            if (waterCells <= 0)
            {
                reason = "no_river_water_span";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool IsValidBridgeBankCell(Vector2Int cell, out string reason)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                reason = "out_of_bounds";
                return false;
            }

            if (mapCell.Kind == CityMapCellKind.Water)
            {
                reason = "bank_is_water";
                return false;
            }

            if (!map.IsCellWalkable(cell))
            {
                reason = "bank_not_walkable";
                return false;
            }

            if (!map.IsCellBuildable(cell))
            {
                reason = "bank_not_buildable";
                return false;
            }

            if (occupiedCells.Contains(cell))
            {
                reason = "bank_occupied";
                return false;
            }

            if (fog != null && !fog.IsCellExplored(cell))
            {
                reason = "bank_unexplored";
                return false;
            }

            if (!HasAdjacentRiverWater(cell))
            {
                reason = "no_adjacent_river";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool CanUseBridgeSpanCell(Vector2Int cell, out string reason)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                reason = "span_out_of_bounds";
                return false;
            }

            if (mapCell.Kind != CityMapCellKind.Water || !mapCell.IsRiver)
            {
                reason = "span_not_river";
                return false;
            }

            if (occupiedCells.Contains(cell))
            {
                reason = "span_occupied";
                return false;
            }

            if (fog != null && !fog.IsCellExplored(cell))
            {
                reason = "span_unexplored";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool HasAdjacentRiverWater(Vector2Int cell)
        {
            Vector2Int[] directions = GetBridgeCrossingDirections();
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int candidate = cell + directions[i];
                if (map.TryGetCell(candidate.x, candidate.y, out CityMapCell mapCell)
                    && mapCell.Kind == CityMapCellKind.Water
                    && mapCell.IsRiver)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int[] GetBridgeCrossingDirections()
        {
            Vector2Int flow = map != null ? map.RiverFlowDirection : Vector2Int.right;
            if (Mathf.Abs(flow.x) >= Mathf.Abs(flow.y))
            {
                return new[] { Vector2Int.up, Vector2Int.down };
            }

            return new[] { Vector2Int.right, Vector2Int.left };
        }

        private static bool ContainsBridgeCandidate(List<BridgeCandidate> candidates, Vector2Int endCell)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].EndCell == endCell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindStarterStorageOrigin(Vector2Int nearCell, StrategyBuildToolInfo toolInfo, out Vector2Int origin)
        {
            Vector2Int[] preferredOffsets =
            {
                new Vector2Int(2, -1),
                new Vector2Int(-4, -1),
                new Vector2Int(-1, 2),
                new Vector2Int(-1, -4),
                new Vector2Int(3, 1),
                new Vector2Int(-5, 1),
                new Vector2Int(1, 3),
                new Vector2Int(1, -5)
            };

            for (int i = 0; i < preferredOffsets.Length; i++)
            {
                Vector2Int candidate = ClampOriginToMap(nearCell + preferredOffsets[i], toolInfo.Footprint);
                if (CanPlaceStarterStorageOrigin(candidate, nearCell, toolInfo))
                {
                    origin = candidate;
                    return true;
                }
            }

            for (int radius = 2; radius <= 7; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = ClampOriginToMap(nearCell + new Vector2Int(x, y), toolInfo.Footprint);
                        if (CanPlaceStarterStorageOrigin(candidate, nearCell, toolInfo))
                        {
                            origin = candidate;
                            return true;
                        }
                    }
                }
            }

            origin = default;
            return false;
        }

        private bool CanPlaceStarterStorageOrigin(Vector2Int origin, Vector2Int avoidCell, StrategyBuildToolInfo toolInfo)
        {
            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
            return !ContainsCell(blockOrigin, blockFootprint, avoidCell) && CanPlaceFootprint(blockOrigin, blockFootprint);
        }

        private string GetPlacementFailureReason(Vector2Int origin, StrategyBuildToolInfo toolInfo)
        {
            if (map == null)
            {
                return "map_missing";
            }

            string reason = GetFoundationFailureReason(origin, toolInfo.Footprint, toolInfo.Tool);
            if (!string.IsNullOrEmpty(reason))
            {
                return reason;
            }

            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int finalBlockOrigin, out Vector2Int finalBlockFootprint);
            reason = GetFinalBlockFailureReason(finalBlockOrigin, finalBlockFootprint, toolInfo.Tool);
            if (!string.IsNullOrEmpty(reason))
            {
                return reason;
            }

            if (!HasBuilderWorkAccess(origin, toolInfo.Footprint))
            {
                return "no_builder_access";
            }

            if (toolInfo.Tool == StrategyBuildTool.FisherHut && !HasFishingWaterAccess(origin))
            {
                return "no_water_access";
            }

            if (!HasRequiredDepositAccess(toolInfo.Tool, origin, toolInfo.Footprint, out string depositReason))
            {
                return depositReason;
            }

            return hasValidHover ? "unknown" : "invalid_hover";
        }

        private bool CanPlaceFoundation(Vector2Int origin, Vector2Int footprint, StrategyBuildTool tool)
        {
            return string.IsNullOrEmpty(GetFoundationFailureReason(origin, footprint, tool));
        }

        private string GetFoundationFailureReason(Vector2Int origin, Vector2Int footprint, StrategyBuildTool tool)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    Vector2Int cell = new Vector2Int(origin.x + x, origin.y + y);
                    if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
                    {
                        return "foundation_out_of_bounds@" + cell.x + "," + cell.y;
                    }

                    if (!mapCell.IsBuildable)
                    {
                        return "foundation_terrain_" + mapCell.Kind + "@" + cell.x + "," + cell.y;
                    }

                    if (!map.IsCellBuildable(cell) && !CanUseMineralBuildBlock(tool, cell))
                    {
                        return "foundation_not_buildable@" + cell.x + "," + cell.y;
                    }

                    if (fog != null && !fog.IsCellExplored(cell))
                    {
                        return "foundation_unexplored@" + cell.x + "," + cell.y;
                    }

                    if (occupiedCells.Contains(cell))
                    {
                        return "foundation_occupied@" + cell.x + "," + cell.y;
                    }

                    if (!map.IsCellWalkable(cell))
                    {
                        return "foundation_not_walkable@" + cell.x + "," + cell.y;
                    }
                }
            }

            return string.Empty;
        }

        private bool CanReserveFinalBlock(Vector2Int origin, Vector2Int footprint, StrategyBuildTool tool)
        {
            return string.IsNullOrEmpty(GetFinalBlockFailureReason(origin, footprint, tool));
        }

        private string GetFinalBlockFailureReason(
            Vector2Int origin,
            Vector2Int footprint,
            StrategyBuildTool tool)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    Vector2Int cell = new Vector2Int(origin.x + x, origin.y + y);
                    if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
                    {
                        return "final_block_out_of_bounds@" + cell.x + "," + cell.y;
                    }

                    if (!mapCell.IsBuildable)
                    {
                        return "final_block_terrain_" + mapCell.Kind + "@" + cell.x + "," + cell.y;
                    }

                    if (!map.IsCellBuildable(cell) && !CanUseMineralBuildBlock(tool, cell))
                    {
                        return "final_block_not_buildable@" + cell.x + "," + cell.y;
                    }

                    if (fog != null && !fog.IsCellExplored(cell))
                    {
                        return "final_block_unexplored@" + cell.x + "," + cell.y;
                    }

                    if (occupiedCells.Contains(cell))
                    {
                        return "final_block_occupied@" + cell.x + "," + cell.y;
                    }
                }
            }

            return string.Empty;
        }

    }
}
