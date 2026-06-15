using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyBuildPlacementController : MonoBehaviour
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
        private StrategyBuildingUpgradeController upgrades;
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
            StrategyStoneResourceController stoneController,
            StrategyBuildingUpgradeController upgradeController = null)
        {
            map = mapController;
            buildMenu = menuController;
            strategyCamera = camera;
            population = populationController;
            fog = fogController;
            forestry = forestryController;
            stone = stoneController;
            upgrades = upgradeController;
            EnsureRuntimeObjects();
            StrategyDebugLogger.Info(
                "Build",
                "PlacementConfigured",
                StrategyDebugLogger.F("hasFog", fog != null),
                StrategyDebugLogger.F("hasForestry", forestry != null),
                StrategyDebugLogger.F("hasStone", stone != null),
                StrategyDebugLogger.F("hasUpgrades", upgrades != null));
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
                "Storage Yard",
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
    }
}
