using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {

        private void HideUnusedBridgePreviewRenderers(int usedCount)
        {
            for (int i = usedCount; i < bridgePreviewRenderers.Count; i++)
            {
                if (bridgePreviewRenderers[i] != null)
                {
                    bridgePreviewRenderers[i].gameObject.SetActive(false);
                }
            }
        }

        private void HideBridgePreview()
        {
            HideUnusedBridgePreviewRenderers(0);
        }

        private void ResetBridgePlacement()
        {
            hasBridgeStart = false;
            bridgeStartCell = default;
            bridgeCandidates.Clear();
            HideBridgePreview();
        }

        private void UpdatePreview(StrategyBuildToolInfo toolInfo)
        {
            if (!TryGetMouseWorld(out Vector3 world) || !map.TryWorldToCell(world, out Vector2Int cell))
            {
                HidePreview();
                return;
            }

            hoveredOrigin = ClampOriginToMap(cell, toolInfo.Footprint);
            Bounds bounds = map.GetCellRectWorld(hoveredOrigin, toolInfo.Footprint);
            bool canPlace = buildMenu.CanAffordActiveTool() && CanPlace(hoveredOrigin, toolInfo);

            previewRenderer.gameObject.SetActive(true);
            bool hasSprite = StrategyBuildingSpriteFactory.TryGetBuildSprite(toolInfo.Tool, out Sprite sprite);
            previewRenderer.sprite = hasSprite ? sprite : whiteSprite;

            if (hasSprite)
            {
                previewRenderer.transform.position = GetSpriteAnchor(bounds, -0.25f);
                previewRenderer.transform.localScale = Vector3.one;
            }
            else
            {
                previewRenderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.25f);
                previewRenderer.transform.localScale = new Vector3(
                    Mathf.Max(0.05f, bounds.size.x - map.CellSize * 0.08f),
                    Mathf.Max(0.05f, bounds.size.y - map.CellSize * 0.08f),
                    1f);
            }

            Color previewColor = hasSprite && canPlace ? Color.white : canPlace ? toolInfo.Color : new Color(0.90f, 0.16f, 0.12f);
            previewColor.a = hasSprite ? (canPlace ? 0.72f : 0.58f) : (canPlace ? 0.52f : 0.42f);
            previewRenderer.color = previewColor;
            hasValidHover = canPlace;
        }

        public StrategyPlacedBuilding CompleteConstructionSite(StrategyConstructionSite site)
        {
            if (site == null || map == null)
            {
                return null;
            }

            ReleaseConstructionSiteMapState(site);
            StrategyBuildToolInfo toolInfo = new StrategyBuildToolInfo(
                site.Tool,
                site.Title,
                site.Cost,
                site.Color,
                site.Footprint);
            StrategyPlacedBuilding building = site.Tool == StrategyBuildTool.Bridge
                ? PlaceTool(
                    toolInfo,
                    site.Origin,
                    site.VisualVariant,
                    false,
                    site.BridgeCells,
                    site.BridgeStartCell,
                    site.BridgeEndCell)
                : PlaceTool(toolInfo, site.Origin, site.VisualVariant, false);
            if (building != null && site.Tool == StrategyBuildTool.House)
            {
                population?.CompleteHouseConstruction(site, building);
            }

            StrategyDebugLogger.Info(
                "Build",
                "ConstructionFinalized",
                StrategyDebugLogger.F("tool", site.Tool),
                StrategyDebugLogger.F("origin", site.Origin),
                StrategyDebugLogger.F("buildingCreated", building != null));

            Destroy(site.gameObject);
            return building;
        }

        public bool CancelConstructionSite(StrategyConstructionSite site)
        {
            if (site == null || map == null || site.IsCompleted)
            {
                return false;
            }

            int looseLogs = site.DeliveredLogs;
            int looseStone = site.DeliveredStone;
            IReadOnlyList<StrategyResidentAgent> builders = site.Builders;
            for (int i = 0; i < builders.Count; i++)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder == null)
                {
                    continue;
                }

                builder.ExtractCarriedConstructionResources(site, out int carriedLogs, out int carriedStone);
                looseLogs += carriedLogs;
                looseStone += carriedStone;
            }

            ReleaseConstructionSiteMapState(site);
            SpawnLooseConstructionResources(site, looseLogs, looseStone);
            StrategyDebugLogger.Info(
                "Build",
                "ConstructionCancelled",
                StrategyDebugLogger.F("tool", site.Tool),
                StrategyDebugLogger.F("origin", site.Origin),
                StrategyDebugLogger.F("looseLogs", looseLogs),
                StrategyDebugLogger.F("looseStone", looseStone));
            Destroy(site.gameObject);
            fog?.RequestRefresh();
            return true;
        }

        public bool DemolishBuilding(StrategyPlacedBuilding building)
        {
            if (building == null || map == null)
            {
                return false;
            }

            StrategyBuildTool tool = building.Tool;
            Vector2Int origin = building.Origin;
            if (tool == StrategyBuildTool.House)
            {
                population?.UnregisterHouse(building);
                building.DetachResidentsForDemolition();
            }

            if (tool == StrategyBuildTool.Bridge && building.BridgeCells.Count > 0)
            {
                UnmarkOccupiedCells(building.BridgeCells);
                map.SetBridgeCellsWalkable(building.BridgeCells, false);
            }
            else
            {
                GetWalkBlockFootprint(tool, building.Origin, building.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
                UnmarkOccupied(blockOrigin, blockFootprint);
                map.SetCellsWalkable(blockOrigin, blockFootprint, true);
            }

            placedBuildings.Remove(building);
            Destroy(building.gameObject);
            fog?.RequestRefresh();
            StrategyDebugLogger.Info(
                "Build",
                "BuildingDemolished",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("placedCount", placedBuildings.Count));
            return true;
        }

        private StrategyConstructionSite PlaceConstructionSite(StrategyBuildToolInfo toolInfo, Vector2Int origin)
        {
            Bounds bounds = map.GetCellRectWorld(origin, toolInfo.Footprint);
            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int finalBlockOrigin, out Vector2Int finalBlockFootprint);
            Vector2Int constructionBlockOrigin = origin;
            Vector2Int constructionBlockFootprint = toolInfo.Footprint;

            GameObject siteObject = new GameObject("Construction: " + toolInfo.Title);
            siteObject.transform.SetParent(placedRoot, false);

            SpriteRenderer renderer = siteObject.AddComponent<SpriteRenderer>();
            int visualVariant = Random.Range(0, StrategyBuildingSpriteFactory.GetVariantCount(toolInfo.Tool));
            renderer.sprite = StrategyConstructionSpriteFactory.GetConstructionSprite(toolInfo.Tool, visualVariant, 0);
            renderer.color = Color.white;
            siteObject.transform.position = GetSpriteAnchor(bounds, -0.14f);
            StrategyWorldSorting.Apply(renderer, siteObject.transform.position);

            StrategyConstructionSite site = siteObject.AddComponent<StrategyConstructionSite>();
            site.Configure(
                this,
                map,
                toolInfo,
                origin,
                bounds,
                constructionBlockOrigin,
                constructionBlockFootprint,
                visualVariant,
                renderer);

            if (!StrategyStorageYard.TryReserveConstructionResources(toolInfo.Cost, site, bounds.center))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "ConstructionSiteRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("reason", "resources_reserve_failed"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                Destroy(siteObject);
                return null;
            }

            MarkOccupied(finalBlockOrigin, finalBlockFootprint);
            map.SetCellsWalkable(constructionBlockOrigin, constructionBlockFootprint, false);
            fog?.RequestRefresh();
            site.Begin();
            bool buildersAssigned = StrategyStorageYard.TryAssignBuildersToSite(site);

            StrategyDebugLogger.Info(
                "Build",
                "ConstructionSitePlaced",
                StrategyDebugLogger.F("tool", toolInfo.Tool),
                StrategyDebugLogger.F("title", toolInfo.Title),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("footprint", toolInfo.Footprint),
                StrategyDebugLogger.F("constructionBlockOrigin", constructionBlockOrigin),
                StrategyDebugLogger.F("constructionBlockFootprint", constructionBlockFootprint),
                StrategyDebugLogger.F("reservedBlockOrigin", finalBlockOrigin),
                StrategyDebugLogger.F("reservedBlockFootprint", finalBlockFootprint),
                StrategyDebugLogger.F("visualVariant", visualVariant),
                StrategyDebugLogger.F("buildersAssigned", buildersAssigned));
            return site;
        }

        private StrategyConstructionSite PlaceBridgeConstructionSite(
            StrategyBuildToolInfo toolInfo,
            Vector2Int startCell,
            BridgeCandidate bridgeCandidate)
        {
            string reason = "invalid_span_bounds";
            if (!TryGetCellBounds(bridgeCandidate.Cells, out Vector2Int origin, out Vector2Int footprint)
                || !CanPlaceBridgeSpan(startCell, bridgeCandidate, out reason))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "BridgeConstructionSiteRejected",
                    StrategyDebugLogger.F("start", startCell),
                    StrategyDebugLogger.F("end", bridgeCandidate.EndCell),
                    StrategyDebugLogger.F("reason", reason));
                return null;
            }

            StrategyBuildToolInfo bridgeInfo = new StrategyBuildToolInfo(
                toolInfo.Tool,
                toolInfo.Title,
                toolInfo.Cost,
                toolInfo.Color,
                footprint);
            Bounds bounds = map.GetCellRectWorld(origin, footprint);

            GameObject siteObject = new GameObject("Construction: " + toolInfo.Title);
            siteObject.transform.SetParent(placedRoot, false);

            SpriteRenderer renderer = siteObject.AddComponent<SpriteRenderer>();
            int visualVariant = 0;
            renderer.sprite = StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(footprint, 0);
            renderer.color = Color.white;
            siteObject.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.14f);
            StrategyWorldSorting.Apply(renderer, siteObject.transform.position);

            StrategyConstructionSite site = siteObject.AddComponent<StrategyConstructionSite>();
            site.Configure(this, map, bridgeInfo, origin, bounds, origin, footprint, visualVariant, renderer);
            site.ConfigureBridgeSpan(bridgeCandidate.Cells, startCell, bridgeCandidate.EndCell);

            if (!StrategyStorageYard.TryReserveConstructionResources(toolInfo.Cost, site, bounds.center))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "BridgeConstructionSiteRejected",
                    StrategyDebugLogger.F("start", startCell),
                    StrategyDebugLogger.F("end", bridgeCandidate.EndCell),
                    StrategyDebugLogger.F("reason", "resources_reserve_failed"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                Destroy(siteObject);
                return null;
            }

            MarkOccupiedCells(bridgeCandidate.Cells);
            fog?.RequestRefresh();
            site.Begin();
            bool buildersAssigned = StrategyStorageYard.TryAssignBuildersToSite(site);

            StrategyDebugLogger.Info(
                "Build",
                "BridgeConstructionSitePlaced",
                StrategyDebugLogger.F("start", startCell),
                StrategyDebugLogger.F("end", bridgeCandidate.EndCell),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("footprint", footprint),
                StrategyDebugLogger.F("cells", bridgeCandidate.Cells.Count),
                StrategyDebugLogger.F("buildersAssigned", buildersAssigned));
            return site;
        }

        private StrategyPlacedBuilding PlaceTool(
            StrategyBuildToolInfo toolInfo,
            Vector2Int origin,
            int visualVariantOverride = -1,
            bool autoAssignHouseResidents = true,
            IReadOnlyList<Vector2Int> bridgeCells = null,
            Vector2Int bridgeStartCell = default,
            Vector2Int bridgeEndCell = default)
        {
            Bounds bounds = map.GetCellRectWorld(origin, toolInfo.Footprint);
            GameObject placed = new GameObject(toolInfo.Title);
            placed.transform.SetParent(placedRoot, false);

            SpriteRenderer renderer = placed.AddComponent<SpriteRenderer>();
            int visualVariant = 0;
            bool isBridge = toolInfo.Tool == StrategyBuildTool.Bridge;
            Sprite sprite = null;
            bool hasSprite = isBridge || StrategyBuildingSpriteFactory.TryGetBuildSprite(toolInfo.Tool, out sprite);
            if (isBridge)
            {
                sprite = StrategyBuildingSpriteFactory.GetBridgeSprite(toolInfo.Footprint);
            }

            if (hasSprite)
            {
                if (isBridge)
                {
                    visualVariant = 0;
                }
                else
                {
                    int variantCount = StrategyBuildingSpriteFactory.GetVariantCount(toolInfo.Tool);
                    visualVariant = visualVariantOverride >= 0
                        ? Mathf.Abs(visualVariantOverride) % Mathf.Max(1, variantCount)
                        : Random.Range(0, variantCount);
                    StrategyBuildingSpriteFactory.TryGetBuildSprite(toolInfo.Tool, visualVariant, out sprite);
                }
            }

            if (hasSprite)
            {
                placed.transform.position = isBridge
                    ? new Vector3(bounds.center.x, bounds.center.y, -0.15f)
                    : GetSpriteAnchor(bounds, -0.15f);
                placed.transform.localScale = Vector3.one;
                renderer.sprite = sprite;
                renderer.color = Color.white;
            }
            else
            {
                placed.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.15f);
                placed.transform.localScale = new Vector3(
                    Mathf.Max(0.05f, bounds.size.x - map.CellSize * 0.12f),
                    Mathf.Max(0.05f, bounds.size.y - map.CellSize * 0.12f),
                    1f);
                renderer.sprite = whiteSprite;
                renderer.color = GetPlacedColor(toolInfo);
            }

            StrategyWorldSorting.Apply(renderer, placed.transform.position);

            if (!hasSprite)
            {
                AddLabel(placed.transform, toolInfo);
            }

            StrategyPlacedBuilding building = placed.AddComponent<StrategyPlacedBuilding>();
            building.Configure(toolInfo.Tool, origin, toolInfo.Footprint, bounds, renderer, visualVariant);
            if (isBridge)
            {
                building.ConfigureBridgeSpan(bridgeCells, bridgeStartCell, bridgeEndCell);
            }

            placedBuildings.Add(building);

            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
            if (isBridge)
            {
                if (bridgeCells != null && bridgeCells.Count > 0)
                {
                    MarkOccupiedCells(bridgeCells);
                    map.SetBridgeCellsWalkable(bridgeCells, true);
                }
                else
                {
                    MarkOccupied(blockOrigin, blockFootprint);
                }
            }
            else
            {
                MarkOccupied(blockOrigin, blockFootprint);
                map.SetCellsWalkable(blockOrigin, blockFootprint, false);
            }

            fog?.RequestRefresh();

            if (toolInfo.Tool == StrategyBuildTool.House)
            {
                StrategyHouseAmbientAnimator ambient = placed.AddComponent<StrategyHouseAmbientAnimator>();
                ambient.Configure(renderer, visualVariant);
                TryInstallDefaultGardenBeds(building);
                population?.RegisterHouse(building);
                if (autoAssignHouseResidents)
                {
                    bool assigned = population != null && population.AssignResidentsToHouse(building);
                    if (!assigned)
                    {
                        population?.TryPopulateFreeHouse(building);
                    }
                }
            }
            else if (toolInfo.Tool == StrategyBuildTool.LumberjackCamp)
            {
                StrategyLumberjackCamp camp = placed.AddComponent<StrategyLumberjackCamp>();
                camp.Configure(building, map, forestry, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.StonecutterCamp)
            {
                StrategyStonecutterCamp camp = placed.AddComponent<StrategyStonecutterCamp>();
                camp.Configure(building, map, stone, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.HunterCamp)
            {
                StrategyHunterCamp camp = placed.AddComponent<StrategyHunterCamp>();
                camp.Configure(building, map, population, StrategyWildlifeController.Active);
            }
            else if (toolInfo.Tool == StrategyBuildTool.FisherHut)
            {
                StrategyFisherHut hut = placed.AddComponent<StrategyFisherHut>();
                hut.Configure(building, map, population, StrategyWildlifeController.Active);
            }
            else if (toolInfo.Tool == StrategyBuildTool.StorageYard)
            {
                StrategyStorageYard yard = placed.AddComponent<StrategyStorageYard>();
                yard.Configure(building, map, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.Granary)
            {
                StrategyGranary granary = placed.AddComponent<StrategyGranary>();
                granary.Configure(building, map, population);
            }

            StrategyDebugLogger.Info(
                "Build",
                "Placed",
                StrategyDebugLogger.F("tool", toolInfo.Tool),
                StrategyDebugLogger.F("title", toolInfo.Title),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("footprint", toolInfo.Footprint),
                StrategyDebugLogger.F("blockOrigin", blockOrigin),
                StrategyDebugLogger.F("blockFootprint", blockFootprint),
                StrategyDebugLogger.F("visualVariant", visualVariant),
                StrategyDebugLogger.F("placedCount", placedBuildings.Count));
            return building;
        }
    }
}
