using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBuildPlacementController : MonoBehaviour
    {
        private readonly HashSet<Vector2Int> occupiedCells = new();
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
        private bool hasValidHover;

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
            StrategyPlacedBuilding building = PlaceTool(toolInfo, site.Origin, site.VisualVariant, false);
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

            if (population == null || !population.TryAssignConstructionBuilders(site))
            {
                StrategyDebugLogger.Warn(
                    "Build",
                    "ConstructionSiteRejected",
                    StrategyDebugLogger.F("tool", toolInfo.Tool),
                    StrategyDebugLogger.F("origin", origin),
                    StrategyDebugLogger.F("reason", population == null ? "population_missing" : "no_builders"));
                Destroy(siteObject);
                return null;
            }

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

            StrategyDebugLogger.Info(
                "Build",
                "ConstructionSitePlaced",
                StrategyDebugLogger.F("tool", toolInfo.Tool),
                StrategyDebugLogger.F("title", toolInfo.Title),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("footprint", toolInfo.Footprint),
                StrategyDebugLogger.F("blockOrigin", blockOrigin),
                StrategyDebugLogger.F("blockFootprint", blockFootprint),
                StrategyDebugLogger.F("visualVariant", visualVariant));
            return site;
        }

        private StrategyPlacedBuilding PlaceTool(
            StrategyBuildToolInfo toolInfo,
            Vector2Int origin,
            int visualVariantOverride = -1,
            bool autoAssignHouseResidents = true)
        {
            Bounds bounds = map.GetCellRectWorld(origin, toolInfo.Footprint);
            GameObject placed = new GameObject(toolInfo.Title);
            placed.transform.SetParent(placedRoot, false);

            SpriteRenderer renderer = placed.AddComponent<SpriteRenderer>();
            int visualVariant = 0;
            bool hasSprite = StrategyBuildingSpriteFactory.TryGetBuildSprite(toolInfo.Tool, out Sprite sprite);
            if (hasSprite)
            {
                int variantCount = StrategyBuildingSpriteFactory.GetVariantCount(toolInfo.Tool);
                visualVariant = visualVariantOverride >= 0
                    ? Mathf.Abs(visualVariantOverride) % Mathf.Max(1, variantCount)
                    : Random.Range(0, variantCount);
                StrategyBuildingSpriteFactory.TryGetBuildSprite(toolInfo.Tool, visualVariant, out sprite);
            }

            if (hasSprite)
            {
                placed.transform.position = GetSpriteAnchor(bounds, -0.15f);
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
            placedBuildings.Add(building);

            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
            MarkOccupied(blockOrigin, blockFootprint);
            map.SetCellsWalkable(blockOrigin, blockFootprint, false);
            fog?.RequestRefresh();

            if (toolInfo.Tool == StrategyBuildTool.House)
            {
                StrategyHouseAmbientAnimator ambient = placed.AddComponent<StrategyHouseAmbientAnimator>();
                ambient.Configure(renderer, visualVariant);
                if (autoAssignHouseResidents)
                {
                    population?.AssignResidentsToHouse(building);
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
            else if (toolInfo.Tool == StrategyBuildTool.StorageYard)
            {
                StrategyStorageYard yard = placed.AddComponent<StrategyStorageYard>();
                yard.Configure(building, map, population);
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
            GetWalkBlockFootprint(toolInfo.Tool, origin, toolInfo.Footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);
            return CanPlaceFootprint(blockOrigin, blockFootprint);
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
            else if (tool == StrategyBuildTool.StorageYard)
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
                StrategyBuildTool.StorageYard => "ST",
                _ => "?"
            };
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
