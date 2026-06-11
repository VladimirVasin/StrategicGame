using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildPlacementController : MonoBehaviour
    {
        private const int MaxBridgeRiverCells = 10;

        private readonly struct BridgeCandidate
        {
            public BridgeCandidate(Vector2Int endCell, Vector2Int direction, IReadOnlyList<Vector2Int> cells)
            {
                EndCell = endCell;
                Direction = direction;
                Cells = new List<Vector2Int>(cells);
            }

            public Vector2Int EndCell { get; }
            public Vector2Int Direction { get; }
            public IReadOnlyList<Vector2Int> Cells { get; }
        }

        private readonly HashSet<Vector2Int> occupiedCells = new();
        private readonly List<BridgeCandidate> bridgeCandidates = new();
        private readonly List<SpriteRenderer> bridgePreviewRenderers = new();
        private CityMapController map;
        private StrategyBuildMenuController buildMenu;
        private StrategyPopulationController population;
        private StrategyFogOfWarController fog;
        private StrategyForestryController forestry;
        private StrategyStoneResourceController stone;
        private Camera strategyCamera;
        private Sprite whiteSprite;
        private SpriteRenderer previewRenderer;
        private Transform placedRoot;
        private Vector2Int hoveredOrigin;
        private Vector2Int bridgeStartCell;
        private bool hasValidHover;
        private bool hasBridgeStart;

        public IReadOnlyList<StrategyPlacedBuilding> PlacedBuildings => placedBuildings;

        private readonly List<StrategyPlacedBuilding> placedBuildings = new();

        public void Configure(
            CityMapController mapController,
            StrategyBuildMenuController menuController,
            Camera camera,
            StrategyPopulationController populationController)
        {
            Configure(mapController, menuController, camera, populationController, null);
        }

        public void Configure(
            CityMapController mapController,
            StrategyBuildMenuController menuController,
            Camera camera,
            StrategyPopulationController populationController,
            StrategyFogOfWarController fogController)
        {
            Configure(mapController, menuController, camera, populationController, fogController, StrategyForestryController.Active);
        }

        public void Configure(
            CityMapController mapController,
            StrategyBuildMenuController menuController,
            Camera camera,
            StrategyPopulationController populationController,
            StrategyFogOfWarController fogController,
            StrategyForestryController forestryController)
        {
            Configure(
                mapController,
                menuController,
                camera,
                populationController,
                fogController,
                forestryController,
                StrategyStoneResourceController.Active);
        }

        public void Configure(
            CityMapController mapController,
            StrategyBuildMenuController menuController,
            Camera camera,
            StrategyPopulationController populationController,
            StrategyFogOfWarController fogController,
            StrategyForestryController forestryController,
            StrategyStoneResourceController stoneController)
        {
            map = mapController;
            buildMenu = menuController;
            strategyCamera = camera;
            population = populationController;
            fog = fogController;
            forestry = forestryController;
            stone = stoneController;
            EnsureRuntimeObjects();
            StrategyDebugLogger.Info(
                "Build",
                "PlacementConfigured",
                StrategyDebugLogger.F("hasFog", fog != null),
                StrategyDebugLogger.F("hasForestry", forestry != null),
                StrategyDebugLogger.F("hasStone", stone != null));
        }

        public bool TryPlaceStarterStorageYard(Vector2Int nearCell, int initialLogs, int initialStone)
        {
            if (map == null)
            {
                return false;
            }

            for (int i = 0; i < placedBuildings.Count; i++)
            {
                StrategyPlacedBuilding placed = placedBuildings[i];
                if (placed != null && placed.Tool == StrategyBuildTool.StorageYard)
                {
                    StrategyStorageYard existingYard = placed.GetComponent<StrategyStorageYard>();
                    if (existingYard != null)
                    {
                        existingYard.AddLogs(initialLogs);
                        existingYard.AddResource(StrategyResourceType.Stone, initialStone);
                    }

                    return true;
                }
            }

            StrategyBuildToolInfo toolInfo = new StrategyBuildToolInfo(
                StrategyBuildTool.StorageYard,
                "\u0421\u043a\u043b\u0430\u0434",
                new StrategyConstructionResourceCost(0, 0),
                new Color(0.61f, 0.50f, 0.38f),
                new Vector2Int(3, 2));

            if (!TryFindStarterStorageOrigin(nearCell, toolInfo, out Vector2Int origin))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "StarterStorageRejected",
                    StrategyDebugLogger.F("nearCell", nearCell),
                    StrategyDebugLogger.F("reason", "no_valid_origin"));
                return false;
            }

            StrategyPlacedBuilding building = PlaceTool(toolInfo, origin);
            StrategyStorageYard yard = building != null ? building.GetComponent<StrategyStorageYard>() : null;
            if (yard == null)
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "StarterStorageRejected",
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("reason", "yard_missing"));
                return false;
            }

            yard.AddLogs(initialLogs);
            yard.AddResource(StrategyResourceType.Stone, initialStone);
            StrategyDebugLogger.Info(
                "Build",
                "StarterStoragePlaced",
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("nearCell", nearCell),
                StrategyDebugLogger.F("initialLogs", initialLogs),
                StrategyDebugLogger.F("initialStone", initialStone));
            return true;
        }

        private void Update()
        {
            if (map == null || buildMenu == null || strategyCamera == null)
            {
                return;
            }

            HandleCancelInput();

            if (!buildMenu.TryGetActiveToolInfo(out StrategyBuildToolInfo toolInfo))
            {
                HidePreview();
                return;
            }

            if (toolInfo.Tool == StrategyBuildTool.Bridge)
            {
                UpdateBridgePreview(toolInfo);
                HandleBridgePlaceInput(toolInfo);
                return;
            }

            ResetBridgePlacement();
            UpdatePreview(toolInfo);
            HandlePlaceInput(toolInfo);
        }

        private void HandleCancelInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool cancelPressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame && !IsPointerOverUi());

            if (cancelPressed)
            {
                StrategyBuildTool cancelledTool = buildMenu.ActiveTool;
                buildMenu.ClearActiveTool();
                HidePreview();
                ResetBridgePlacement();
                if (cancelledTool != StrategyBuildTool.None)
                {
                    StrategyDebugLogger.Info("Build", "PlacementCancelled", StrategyDebugLogger.F("tool", cancelledTool));
                }
            }
        }

        private void HandlePlaceInput(StrategyBuildToolInfo toolInfo)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || IsPointerOverUi())
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Build",
                "PlacementAttempt",
                StrategyDebugLogger.F("tool", toolInfo.Tool),
                StrategyDebugLogger.F("origin", hoveredOrigin),
                StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));

            if (!buildMenu.CanAffordActiveTool())
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "PlacementRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", hoveredOrigin),
                    StrategyDebugLogger.F("reason", "not_affordable"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                return;
            }

            if (!hasValidHover || !CanPlace(hoveredOrigin, toolInfo))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "PlacementRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", hoveredOrigin),
                    StrategyDebugLogger.F("reason", GetPlacementFailureReason(hoveredOrigin, toolInfo)));
                return;
            }

            StrategyConstructionSite site = PlaceConstructionSite(toolInfo, hoveredOrigin);
            if (site == null)
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "ConstructionSiteFailed",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", hoveredOrigin),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone));
                return;
            }

            buildMenu.CloseAfterPlacement();
            HidePreview();
        }

        private void HandleBridgePlaceInput(StrategyBuildToolInfo toolInfo)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || IsPointerOverUi())
            {
                return;
            }

            if (!TryGetMouseWorld(out Vector3 world) || !map.TryWorldToCell(world, out Vector2Int clickedCell))
            {
                return;
            }

            hoveredOrigin = clickedCell;
            StrategyDebugLogger.Info(
                "Build",
                "BridgePlacementAttempt",
                StrategyDebugLogger.F("cell", clickedCell),
                StrategyDebugLogger.F("hasStart", hasBridgeStart),
                StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));

            if (!buildMenu.CanAffordActiveTool())
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "BridgePlacementRejected",
                    StrategyDebugLogger.F("cell", clickedCell),
                    StrategyDebugLogger.F("reason", "not_affordable"),
                    StrategyDebugLogger.F("costLogs", toolInfo.Cost.Logs),
                    StrategyDebugLogger.F("costStone", toolInfo.Cost.Stone),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                return;
            }

            if (!hasBridgeStart)
            {
                TrySelectBridgeStart(clickedCell);
                return;
            }

            if (TryGetBridgeCandidate(clickedCell, out BridgeCandidate bridgeCandidate))
            {
                StrategyConstructionSite site = PlaceBridgeConstructionSite(toolInfo, bridgeStartCell, bridgeCandidate);
                if (site == null)
                {
                    StrategyDebugLogger.Warn(
                        "Build",
                        "BridgeConstructionSiteFailed",
                        StrategyDebugLogger.F("start", bridgeStartCell),
                        StrategyDebugLogger.F("end", bridgeCandidate.EndCell));
                    return;
                }

                buildMenu.CloseAfterPlacement();
                HidePreview();
                ResetBridgePlacement();
                return;
            }

            if (TrySelectBridgeStart(clickedCell))
            {
                return;
            }

            StrategyDebugLogger.Warn(
                "Build",
                "BridgePlacementRejected",
                StrategyDebugLogger.F("cell", clickedCell),
                StrategyDebugLogger.F("reason", "not_a_suggested_bank"));
        }

        private void UpdateBridgePreview(StrategyBuildToolInfo toolInfo)
        {
            if (previewRenderer != null)
            {
                previewRenderer.gameObject.SetActive(false);
            }

            if (!TryGetMouseWorld(out Vector3 world) || !map.TryWorldToCell(world, out Vector2Int cell))
            {
                hasValidHover = false;
                HideBridgePreview();
                return;
            }

            hoveredOrigin = cell;
            if (!hasBridgeStart)
            {
                bool valid = buildMenu.CanAffordActiveTool()
                    && CanSelectBridgeStart(cell, out _);
                hasValidHover = valid;
                int index = 0;
                DrawBridgePreviewCell(cell, valid ? new Color(0.33f, 0.95f, 0.62f, 0.46f) : new Color(0.95f, 0.18f, 0.14f, 0.42f), ref index);
                HideUnusedBridgePreviewRenderers(index);
                return;
            }

            TryGetBridgeCandidate(cell, out BridgeCandidate hoveredCandidate);
            bool hasHoveredCandidate = hoveredCandidate.Cells != null && hoveredCandidate.Cells.Count > 0;
            hasValidHover = buildMenu.CanAffordActiveTool() && hasHoveredCandidate;
            DrawBridgeCandidatePreview(hasHoveredCandidate ? hoveredCandidate : default);
        }

        private bool TrySelectBridgeStart(Vector2Int startCell)
        {
            if (!CanSelectBridgeStart(startCell, out string reason))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "BridgeStartRejected",
                    StrategyDebugLogger.F("cell", startCell),
                    StrategyDebugLogger.F("reason", reason));
                return false;
            }

            bridgeStartCell = startCell;
            hasBridgeStart = true;
            BuildBridgeCandidates(startCell, bridgeCandidates);
            StrategyDebugLogger.Info(
                "Build",
                "BridgeStartSelected",
                StrategyDebugLogger.F("cell", startCell),
                StrategyDebugLogger.F("candidates", bridgeCandidates.Count));
            return true;
        }

        private bool CanSelectBridgeStart(Vector2Int startCell, out string reason)
        {
            if (!IsValidBridgeBankCell(startCell, out reason))
            {
                return false;
            }

            List<BridgeCandidate> candidates = new();
            BuildBridgeCandidates(startCell, candidates);
            if (candidates.Count <= 0)
            {
                reason = "no_opposite_river_bank";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private void DrawBridgeCandidatePreview(BridgeCandidate hoveredCandidate)
        {
            int index = 0;
            DrawBridgePreviewCell(bridgeStartCell, new Color(1f, 0.82f, 0.28f, 0.58f), ref index);

            for (int i = 0; i < bridgeCandidates.Count; i++)
            {
                BridgeCandidate candidate = bridgeCandidates[i];
                bool isHovered = hoveredCandidate.Cells != null
                    && candidate.EndCell == hoveredCandidate.EndCell;
                DrawBridgePreviewCell(
                    candidate.EndCell,
                    isHovered ? new Color(0.36f, 1f, 0.60f, 0.62f) : new Color(0.25f, 0.76f, 1f, 0.48f),
                    ref index);
            }

            if (hoveredCandidate.Cells != null)
            {
                for (int i = 0; i < hoveredCandidate.Cells.Count; i++)
                {
                    Vector2Int cell = hoveredCandidate.Cells[i];
                    if (cell == bridgeStartCell || cell == hoveredCandidate.EndCell)
                    {
                        continue;
                    }

                    DrawBridgePreviewCell(cell, new Color(0.50f, 0.82f, 1f, 0.34f), ref index);
                }
            }

            HideUnusedBridgePreviewRenderers(index);
        }

        private void DrawBridgePreviewCell(Vector2Int cell, Color color, ref int rendererIndex)
        {
            SpriteRenderer renderer = EnsureBridgePreviewRenderer(rendererIndex);
            rendererIndex++;
            Bounds bounds = map.GetCellRectWorld(cell, Vector2Int.one);
            renderer.sprite = whiteSprite;
            renderer.color = color;
            renderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.24f);
            renderer.transform.localScale = new Vector3(map.CellSize * 0.86f, map.CellSize * 0.86f, 1f);
            renderer.sortingOrder = StrategyWorldSorting.PreviewOrder;
            renderer.gameObject.SetActive(true);
        }

        private SpriteRenderer EnsureBridgePreviewRenderer(int index)
        {
            while (bridgePreviewRenderers.Count <= index)
            {
                GameObject previewObject = new GameObject("Bridge Preview Cell");
                previewObject.transform.SetParent(transform, false);
                SpriteRenderer renderer = previewObject.AddComponent<SpriteRenderer>();
                renderer.sprite = whiteSprite;
                renderer.sortingOrder = StrategyWorldSorting.PreviewOrder;
                renderer.gameObject.SetActive(false);
                bridgePreviewRenderers.Add(renderer);
            }

            return bridgePreviewRenderers[index];
        }

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

        private StrategyConstructionSite PlaceConstructionSite(StrategyBuildToolInfo toolInfo, Vector2Int origin)
        {
            Bounds bounds = map.GetCellRectWorld(origin, toolInfo.Footprint);
            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);

            GameObject siteObject = new GameObject("\u0421\u0442\u0440\u043e\u0439\u043a\u0430: " + toolInfo.Title);
            siteObject.transform.SetParent(placedRoot, false);

            SpriteRenderer renderer = siteObject.AddComponent<SpriteRenderer>();
            int visualVariant = Random.Range(0, StrategyBuildingSpriteFactory.GetVariantCount(toolInfo.Tool));
            renderer.sprite = StrategyConstructionSpriteFactory.GetConstructionSprite(toolInfo.Tool, visualVariant, 0);
            renderer.color = Color.white;
            siteObject.transform.position = GetSpriteAnchor(bounds, -0.14f);
            StrategyWorldSorting.Apply(renderer, siteObject.transform.position);

            StrategyConstructionSite site = siteObject.AddComponent<StrategyConstructionSite>();
            site.Configure(this, map, toolInfo, origin, bounds, blockOrigin, blockFootprint, visualVariant, renderer);

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

            MarkOccupied(blockOrigin, blockFootprint);
            map.SetCellsWalkable(blockOrigin, blockFootprint, false);
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
                StrategyDebugLogger.F("blockOrigin", blockOrigin),
                StrategyDebugLogger.F("blockFootprint", blockFootprint),
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

            GameObject siteObject = new GameObject("\u0421\u0442\u0440\u043e\u0439\u043a\u0430: " + toolInfo.Title);
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

            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
            return CanPlaceFootprint(blockOrigin, blockFootprint)
                && (toolInfo.Tool != StrategyBuildTool.FisherHut || HasFishingWaterAccess(origin));
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

            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
            for (int y = 0; y < blockFootprint.y; y++)
            {
                for (int x = 0; x < blockFootprint.x; x++)
                {
                    Vector2Int cell = new Vector2Int(blockOrigin.x + x, blockOrigin.y + y);
                    if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
                    {
                        return "out_of_bounds@" + cell.x + "," + cell.y;
                    }

                    if (!mapCell.IsBuildable)
                    {
                        return "terrain_" + mapCell.Kind + "@" + cell.x + "," + cell.y;
                    }

                    if (fog != null && !fog.IsCellExplored(cell))
                    {
                        return "unexplored@" + cell.x + "," + cell.y;
                    }

                    if (occupiedCells.Contains(cell))
                    {
                        return "occupied@" + cell.x + "," + cell.y;
                    }

                    if (!map.IsCellWalkable(cell))
                    {
                        return "not_walkable@" + cell.x + "," + cell.y;
                    }
                }
            }

            if (toolInfo.Tool == StrategyBuildTool.FisherHut && !HasFishingWaterAccess(origin))
            {
                return "no_water_access";
            }

            return hasValidHover ? "unknown" : "invalid_hover";
        }

        private bool CanPlaceFootprint(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    Vector2Int cell = new Vector2Int(origin.x + x, origin.y + y);
                    if (!map.IsCellWalkable(cell)
                        || (fog != null && !fog.IsCellExplored(cell))
                        || occupiedCells.Contains(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void GetWalkBlockFootprint(
            StrategyBuildTool tool,
            Vector2Int origin,
            Vector2Int footprint,
            out Vector2Int blockOrigin,
            out Vector2Int blockFootprint)
        {
            blockOrigin = origin;
            blockFootprint = footprint;

            if (tool == StrategyBuildTool.House)
            {
                blockOrigin = new Vector2Int(origin.x - 1, origin.y);
                blockFootprint = new Vector2Int(footprint.x + 2, footprint.y + 2);
            }
            else if (tool == StrategyBuildTool.LumberjackCamp)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.StonecutterCamp)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.HunterCamp)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.FisherHut)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.StorageYard)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.Granary)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
        }

        private void MarkOccupied(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    occupiedCells.Add(new Vector2Int(origin.x + x, origin.y + y));
                }
            }
        }

        private void MarkOccupiedCells(IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                occupiedCells.Add(cells[i]);
            }
        }

        private static bool TryGetCellBounds(
            IReadOnlyList<Vector2Int> cells,
            out Vector2Int origin,
            out Vector2Int footprint)
        {
            if (cells == null || cells.Count <= 0)
            {
                origin = default;
                footprint = default;
                return false;
            }

            int minX = cells[0].x;
            int maxX = cells[0].x;
            int minY = cells[0].y;
            int maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            origin = new Vector2Int(minX, minY);
            footprint = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
            return true;
        }

        private static bool ContainsCell(Vector2Int origin, Vector2Int footprint, Vector2Int cell)
        {
            return cell.x >= origin.x
                && cell.y >= origin.y
                && cell.x < origin.x + footprint.x
                && cell.y < origin.y + footprint.y;
        }

        private Vector2Int ClampOriginToMap(Vector2Int origin, Vector2Int footprint)
        {
            int maxX = Mathf.Max(0, map.Width - footprint.x);
            int maxY = Mathf.Max(0, map.Height - footprint.y);
            return new Vector2Int(
                Mathf.Clamp(origin.x, 0, maxX),
                Mathf.Clamp(origin.y, 0, maxY));
        }

        private bool TryGetMouseWorld(out Vector3 world)
        {
            world = default;
            Mouse mouse = Mouse.current;
            if (mouse == null || IsPointerOverUi())
            {
                return false;
            }

            Vector2 screen = mouse.position.ReadValue();
            if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
            {
                return false;
            }

            world = strategyCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(strategyCamera.transform.position.z)));
            world.z = 0f;
            return true;
        }

        private void EnsureRuntimeObjects()
        {
            if (whiteSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Build Placement White Pixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, false);
                whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            if (placedRoot == null)
            {
                GameObject placedRootObject = new GameObject("Placed Buildings");
                placedRoot = placedRootObject.transform;
            }

            if (previewRenderer == null)
            {
                GameObject previewObject = new GameObject("Build Preview");
                previewObject.transform.SetParent(transform, false);
                previewRenderer = previewObject.AddComponent<SpriteRenderer>();
                previewRenderer.sprite = whiteSprite;
                previewRenderer.sortingOrder = StrategyWorldSorting.PreviewOrder;
                previewRenderer.gameObject.SetActive(false);
            }
        }

        private void HidePreview()
        {
            hasValidHover = false;
            if (previewRenderer != null)
            {
                previewRenderer.gameObject.SetActive(false);
            }
        }

        private Vector3 GetSpriteAnchor(Bounds bounds, float z)
        {
            return new Vector3(bounds.center.x, bounds.min.y + map.CellSize * 0.20f, z);
        }

        private static Color GetPlacedColor(StrategyBuildToolInfo toolInfo)
        {
            Color color = toolInfo.Color;
            color.a = 0.96f;
            return color;
        }

        private static string GetAbbreviation(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => "HM",
                StrategyBuildTool.LumberjackCamp => "LC",
                StrategyBuildTool.StonecutterCamp => "SC",
                StrategyBuildTool.HunterCamp => "HC",
                StrategyBuildTool.FisherHut => "FH",
                StrategyBuildTool.StorageYard => "ST",
                StrategyBuildTool.Granary => "GR",
                StrategyBuildTool.Bridge => "BR",
                _ => "?"
            };
        }

        private bool HasFishingWaterAccess(Vector2Int origin)
        {
            if (map == null)
            {
                return false;
            }

            int radius = StrategyFisherHut.WorkRadius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    if ((cell - origin).sqrMagnitude > radius * radius)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                        && mapCell.Kind == CityMapCellKind.Water
                        && HasAdjacentWalkableCell(cell))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasAdjacentWalkableCell(Vector2Int waterCell)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.IsCellWalkable(waterCell + new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
