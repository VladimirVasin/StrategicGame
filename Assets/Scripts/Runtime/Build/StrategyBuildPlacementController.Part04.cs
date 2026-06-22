using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
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
            else if (toolInfo.Tool == StrategyBuildTool.Sawmill)
            {
                StrategySawmill sawmill = placed.AddComponent<StrategySawmill>();
                sawmill.Configure(building, map, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.Mine)
            {
                StrategyMine mine = placed.AddComponent<StrategyMine>();
                mine.Configure(building, map, StrategyIronResourceController.Active, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.CoalPit)
            {
                StrategyCoalPit pit = placed.AddComponent<StrategyCoalPit>();
                pit.Configure(building, map, StrategyCoalResourceController.Active, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.ClayPit)
            {
                StrategyClayPit pit = placed.AddComponent<StrategyClayPit>();
                pit.Configure(building, map, StrategyClayResourceController.Active, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.Kiln)
            {
                StrategyKiln kiln = placed.AddComponent<StrategyKiln>();
                kiln.Configure(building, map, population);
            }
            else if (toolInfo.Tool == StrategyBuildTool.Forge)
            {
                StrategyForge forge = placed.AddComponent<StrategyForge>();
                forge.Configure(building, map, population);
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
            else if (toolInfo.Tool == StrategyBuildTool.TradingPost)
            {
                StrategyTradingPost post = placed.AddComponent<StrategyTradingPost>();
                post.Configure(building, map);
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
