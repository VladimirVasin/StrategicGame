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
                buildMenu.SetPlacementFeedback(false, "Move the pointer over the map.");
                HidePreview();
                return;
            }

            hoveredOrigin = ClampOriginToMap(cell, toolInfo.Footprint);
            Bounds bounds = map.GetCellRectWorld(hoveredOrigin, toolInfo.Footprint);
            bool affordable = buildMenu.CanAffordActiveTool();
            bool canPlace = affordable && CanPlace(hoveredOrigin, toolInfo);
            string feedback = canPlace
                ? "Valid site. Click to place the construction plan."
                : !affordable
                    ? StrategyBuildPlacementFeedbackText.FormatFailureReason("not_affordable")
                    : StrategyBuildPlacementFeedbackText.FormatFailureReason(
                        GetPlacementFailureReason(hoveredOrigin, toolInfo));
            buildMenu.SetPlacementFeedback(canPlace, feedback);

            previewRenderer.gameObject.SetActive(true);
            bool hasSprite = StrategyBuildingSpriteFactory.TryGetBuildSprite(toolInfo.Tool, out Sprite sprite);
            previewRenderer.sprite = hasSprite ? sprite : whiteSprite;

            if (hasSprite)
            {
                previewRenderer.transform.position = GetSpriteAnchor(toolInfo.Tool, bounds, -0.25f);
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

            NotifyBuildingCompleted(building);
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
            int loosePlanks = site.DeliveredPlanks;
            IReadOnlyList<StrategyResidentAgent> builders = site.Builders;
            for (int i = 0; i < builders.Count; i++)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder == null)
                {
                    continue;
                }

                builder.ExtractCarriedConstructionResources(site, out int carriedLogs, out int carriedStone, out int carriedPlanks);
                looseLogs += carriedLogs;
                looseStone += carriedStone;
                loosePlanks += carriedPlanks;
            }

            ReleaseConstructionSiteMapState(site);
            SpawnLooseConstructionResources(site, looseLogs, looseStone, loosePlanks);
            StrategyDebugLogger.Info(
                "Build",
                "ConstructionCancelled",
                StrategyDebugLogger.F("tool", site.Tool),
                StrategyDebugLogger.F("origin", site.Origin),
                StrategyDebugLogger.F("looseLogs", looseLogs),
                StrategyDebugLogger.F("looseStone", looseStone),
                StrategyDebugLogger.F("loosePlanks", loosePlanks));
            Destroy(site.gameObject);
            fog?.RequestRefresh();
            return true;
        }

        public bool DemolishBuilding(StrategyPlacedBuilding building)
        {
            return QueueBuildingDemolition(building);
        }

        private StrategyConstructionSite PlaceConstructionSite(StrategyBuildToolInfo toolInfo, Vector2Int origin)
        {
            Bounds bounds = map.GetCellRectWorld(origin, toolInfo.Footprint);
            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int finalBlockOrigin, out Vector2Int finalBlockFootprint);
            Vector2Int constructionBlockOrigin = finalBlockOrigin;
            Vector2Int constructionBlockFootprint = finalBlockFootprint;

            GameObject siteObject = new GameObject("Construction: " + toolInfo.Title);
            siteObject.transform.SetParent(placedRoot, false);

            SpriteRenderer renderer = siteObject.AddComponent<SpriteRenderer>();
            int visualVariant = Random.Range(0, StrategyBuildingSpriteFactory.GetVariantCount(toolInfo.Tool));
            renderer.sprite = StrategyConstructionSpriteFactory.GetConstructionSprite(toolInfo.Tool, visualVariant, 0);
            renderer.color = Color.white;
            siteObject.transform.position = GetSpriteAnchor(toolInfo.Tool, bounds, -0.14f);
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

            bool instantConstruction = StrategyDebugOptions.InstantConstructionEnabled;
            if (!instantConstruction && !StrategyStorageYard.TryReserveConstructionResources(toolInfo.Cost, site, bounds.center))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "ConstructionSiteRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("reason", "resources_reserve_failed"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                Destroy(siteObject);
                return null;
            }

            MarkOccupied(finalBlockOrigin, finalBlockFootprint);
            map.SetCellsWalkable(constructionBlockOrigin, constructionBlockFootprint, false);
            fog?.RequestRefresh();
            site.Begin();
            bool buildersAssigned = false;
            if (!instantConstruction)
            {
                buildersAssigned = StrategyStorageYard.TryAssignBuildersToSite(site);
            }

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
                StrategyDebugLogger.F("instantConstruction", instantConstruction),
                StrategyDebugLogger.F("buildersAssigned", buildersAssigned));
            if (instantConstruction)
            {
                site.DebugCompleteInstantly();
            }

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

            bool instantConstruction = StrategyDebugOptions.InstantConstructionEnabled;
            if (!instantConstruction && !StrategyStorageYard.TryReserveConstructionResources(toolInfo.Cost, site, bounds.center))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "BridgeConstructionSiteRejected",
                    StrategyDebugLogger.F("start", startCell),
                    StrategyDebugLogger.F("end", bridgeCandidate.EndCell),
                    StrategyDebugLogger.F("reason", "resources_reserve_failed"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("costPlanks", toolInfo.Cost.Planks),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                Destroy(siteObject);
                return null;
            }

            MarkOccupiedCells(bridgeCandidate.Cells);
            fog?.RequestRefresh();
            site.Begin();
            bool buildersAssigned = false;
            if (!instantConstruction)
            {
                buildersAssigned = StrategyStorageYard.TryAssignBuildersToSite(site);
            }

            StrategyDebugLogger.Info(
                "Build",
                "BridgeConstructionSitePlaced",
                StrategyDebugLogger.F("start", startCell),
                StrategyDebugLogger.F("end", bridgeCandidate.EndCell),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("footprint", footprint),
                StrategyDebugLogger.F("cells", bridgeCandidate.Cells.Count),
                StrategyDebugLogger.F("instantConstruction", instantConstruction),
                StrategyDebugLogger.F("buildersAssigned", buildersAssigned));
            if (instantConstruction)
            {
                site.DebugCompleteInstantly();
            }

            return site;
        }

    }
}
