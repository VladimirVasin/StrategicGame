using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {
        private bool restoringPersistentWorld;

        public void ClearWorldForLoad()
        {
            IReadOnlyList<StrategyConstructionSite> activeSites = StrategyConstructionSite.ActiveSites;
            List<StrategyConstructionSite> sites = new(activeSites);
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null)
                {
                    sites[i].gameObject.SetActive(false);
                    CancelConstructionSite(sites[i]);
                }
            }

            for (int i = placedBuildings.Count - 1; i >= 0; i--)
            {
                StrategyPlacedBuilding building = placedBuildings[i];
                if (building != null)
                {
                    ClearBuildingStores(building);
                    building.gameObject.SetActive(false);
                    DemolishBuilding(building);
                }
            }
        }

        private static void ClearBuildingStores(StrategyPlacedBuilding building)
        {
            MonoBehaviour[] components = building.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IStrategyResourceStoreOwner owner)
                {
                    owner.ResourceStore?.RestoreAmounts(null);
                }
            }
        }

        public StrategyPlacedBuilding RestoreBuilding(StrategyBuildingSaveData data)
        {
            if (data == null || map == null)
            {
                return null;
            }

            StrategyBuildTool tool = (StrategyBuildTool)data.tool;
            int visualVariant = StrategyBuildingVariantProfile.NormalizeVariant(tool, data.visualVariant);
            Vector2Int footprint = new(Mathf.Max(1, data.footprintX), Mathf.Max(1, data.footprintY));
            StrategyBuildToolInfo info = CreatePersistenceToolInfo(tool, footprint, default);
            List<Vector2Int> bridgeCells = CopyCells(data.bridgeCells);
            restoringPersistentWorld = true;
            StrategyPlacedBuilding building;
            try
            {
                building = PlaceTool(
                    info,
                    new Vector2Int(data.originX, data.originY),
                    visualVariant,
                    false,
                    bridgeCells,
                    new Vector2Int(data.bridgeStartX, data.bridgeStartY),
                    new Vector2Int(data.bridgeEndX, data.bridgeEndY));
            }
            finally
            {
                restoringPersistentWorld = false;
            }

            building?.RestoreStableId(data.stableId);
            return building;
        }

        public StrategyConstructionSite RestoreConstructionSite(StrategyConstructionSiteSaveData data)
        {
            if (data == null || map == null)
            {
                return null;
            }

            StrategyBuildTool tool = (StrategyBuildTool)data.tool;
            int visualVariant = StrategyBuildingVariantProfile.NormalizeVariant(tool, data.visualVariant);
            Vector2Int origin = new(data.originX, data.originY);
            Vector2Int footprint = new(Mathf.Max(1, data.footprintX), Mathf.Max(1, data.footprintY));
            StrategyConstructionResourceCost cost = new(data.costLogs, data.costStone, data.costPlanks);
            StrategyBuildToolInfo info = CreatePersistenceToolInfo(tool, footprint, cost);
            Bounds bounds = map.GetCellRectWorld(origin, footprint);
            GetWalkBlockFootprint(tool, origin, footprint, out Vector2Int blockOrigin, out Vector2Int blockFootprint);

            GameObject siteObject = new GameObject("Construction: " + info.Title);
            siteObject.transform.SetParent(placedRoot, false);
            SpriteRenderer renderer = siteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = tool == StrategyBuildTool.Bridge
                ? StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(footprint, 0)
                : StrategyConstructionSpriteFactory.GetConstructionSprite(tool, visualVariant, 0);
            renderer.color = Color.white;
            siteObject.transform.position = tool == StrategyBuildTool.Bridge
                ? new Vector3(bounds.center.x, bounds.center.y, -0.14f)
                : GetSpriteAnchor(tool, bounds, -0.14f);
            StrategyWorldSorting.Apply(renderer, siteObject.transform.position);

            StrategyConstructionSite site = siteObject.AddComponent<StrategyConstructionSite>();
            site.Configure(this, map, info, origin, bounds, blockOrigin, blockFootprint, visualVariant, renderer);
            List<Vector2Int> bridgeCells = CopyCells(data.bridgeCells);
            if (data.hasBridgeSpan && bridgeCells.Count > 0)
            {
                site.ConfigureBridgeSpan(
                    bridgeCells,
                    new Vector2Int(data.bridgeStartX, data.bridgeStartY),
                    new Vector2Int(data.bridgeEndX, data.bridgeEndY));
                MarkOccupiedCells(bridgeCells);
            }
            else
            {
                MarkOccupied(blockOrigin, blockFootprint);
                map.SetCellsWalkable(blockOrigin, blockFootprint, false);
            }

            site.RestorePersistentProgress(
                data.deliveredLogs,
                data.deliveredStone,
                data.deliveredPlanks,
                data.progress);
            StrategyConstructionResourceCost remaining = new(site.NeededLogs, site.NeededStone, site.NeededPlanks);
            if (!remaining.IsFree)
            {
                StrategyStorageYard.TryReserveConstructionResources(remaining, site, bounds.center);
            }

            StrategyStorageYard.TryAssignBuildersToSite(site);
            fog?.RequestRefresh();
            return site;
        }

        private static StrategyBuildToolInfo CreatePersistenceToolInfo(
            StrategyBuildTool tool,
            Vector2Int footprint,
            StrategyConstructionResourceCost cost)
        {
            return new StrategyBuildToolInfo(tool, GetPersistenceTitle(tool), cost, Color.white, footprint);
        }

        private static string GetPersistenceTitle(StrategyBuildTool tool)
        {
            string name = tool.ToString();
            System.Text.StringBuilder title = new();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    title.Append(' ');
                }

                title.Append(name[i]);
            }

            return title.ToString();
        }

        private static List<Vector2Int> CopyCells(IReadOnlyList<StrategyCellSaveData> source)
        {
            List<Vector2Int> cells = new();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    cells.Add(new Vector2Int(source[i].x, source[i].y));
                }
            }

            return cells;
        }
    }
}
