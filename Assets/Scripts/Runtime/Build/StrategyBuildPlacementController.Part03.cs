using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyBuildPlacementController
    {

        private bool CanPlaceFootprint(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    Vector2Int cell = new Vector2Int(origin.x + x, origin.y + y);
                    if (!map.IsCellWalkable(cell)
                        || !map.IsCellBuildable(cell)
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
            else if (tool == StrategyBuildTool.Sawmill)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.Mine)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.CoalPit)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.ClayPit)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.Kiln)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.Forge)
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
            else if (tool == StrategyBuildTool.ChickenCoop)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.StorageYard)
            {
                blockFootprint = new Vector2Int(footprint.x, footprint.y + 1);
            }
            else if (tool == StrategyBuildTool.StarterCaravanCart)
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

        private void UnmarkOccupied(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    occupiedCells.Remove(new Vector2Int(origin.x + x, origin.y + y));
                }
            }
        }

        private void UnmarkOccupiedCells(IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                occupiedCells.Remove(cells[i]);
            }
        }

        private void ReleaseConstructionSiteMapState(StrategyConstructionSite site)
        {
            if (site == null)
            {
                return;
            }

            if (site.HasBridgeSpan)
            {
                UnmarkOccupiedCells(site.BridgeCells);
            }
            else
            {
                GetWalkBlockFootprint(site.Tool, site.Origin, site.Footprint, out Vector2Int finalBlockOrigin, out Vector2Int finalBlockFootprint);
                UnmarkOccupied(finalBlockOrigin, finalBlockFootprint);
                map.SetCellsWalkable(site.BlockOrigin, site.BlockFootprint, true);
            }
        }

        private void SpawnLooseConstructionResources(StrategyConstructionSite site, int logs, int stone, int planks)
        {
            if (site == null || (logs <= 0 && stone <= 0 && planks <= 0))
            {
                return;
            }

            Vector2Int resourceCell = site.HasBridgeSpan ? site.BridgeStartCell : site.Origin;
            Vector3 world = site.HasBridgeSpan
                ? map.GetCellRectWorld(resourceCell, Vector2Int.one).center
                : site.FootprintBounds.center;
            StrategyLooseConstructionResourcePile.Create(map, resourceCell, world, logs, stone, planks);
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

        private Vector3 GetSpriteAnchor(StrategyBuildTool tool, Bounds bounds, float z)
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
                StrategyBuildTool.Sawmill => "SW",
                StrategyBuildTool.Mine => "MN",
                StrategyBuildTool.CoalPit => "CP",
                StrategyBuildTool.ClayPit => "CL",
                StrategyBuildTool.Kiln => "KI",
                StrategyBuildTool.Forge => "FG",
                StrategyBuildTool.HunterCamp => "HC",
                StrategyBuildTool.FisherHut => "FH",
                StrategyBuildTool.ForagerCamp => "FC",
                StrategyBuildTool.ChickenCoop => "CC",
                StrategyBuildTool.StarterCaravanCart => "CV",
                StrategyBuildTool.StorageYard => "ST",
                StrategyBuildTool.Granary => "GR",
                StrategyBuildTool.Bridge => "BR",
                _ => "?"
            };
        }

        private bool HasFishingWaterAccess(Vector2Int origin)
        {
            return StrategyFishingAccessUtility.TryFindFishingWaterCell(
                map,
                origin,
                StrategyFisherHut.WorkRadius,
                out _,
                out _);
        }

        private bool HasBuilderWorkAccess(Vector2Int origin, Vector2Int footprint)
        {
            for (int radius = 1; radius <= 2; radius++)
            {
                for (int y = -radius; y < footprint.y + radius; y++)
                {
                    for (int x = -radius; x < footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == footprint.x + radius - 1
                            || y == footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !occupiedCells.Contains(candidate))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool HasMineIronAccess(Vector2Int origin, Vector2Int footprint)
        {
            StrategyIronResourceController iron = StrategyIronResourceController.Active;
            if (iron == null)
            {
                return false;
            }

            return iron.CountAvailableDepositsInFootprint(origin, footprint) > 0;
        }

        private static bool HasCoalPitCoalAccess(Vector2Int origin, Vector2Int footprint)
        {
            StrategyCoalResourceController coal = StrategyCoalResourceController.Active;
            return coal != null && coal.CountAvailableDepositsInFootprint(origin, footprint) > 0;
        }

        private static bool HasClayPitClayAccess(Vector2Int origin, Vector2Int footprint)
        {
            StrategyClayResourceController clay = StrategyClayResourceController.Active;
            return clay != null && clay.CountAvailableDepositsInFootprint(origin, footprint) > 0;
        }

        private static bool HasRequiredDepositAccess(
            StrategyBuildTool tool,
            Vector2Int origin,
            Vector2Int footprint,
            out string reason)
        {
            reason = string.Empty;
            if (tool == StrategyBuildTool.Mine && !HasMineIronAccess(origin, footprint))
            {
                reason = "no_iron_deposit_under_mine";
                return false;
            }

            if (tool == StrategyBuildTool.CoalPit && !HasCoalPitCoalAccess(origin, footprint))
            {
                reason = "no_coal_deposit_under_pit";
                return false;
            }

            if (tool == StrategyBuildTool.ClayPit && !HasClayPitClayAccess(origin, footprint))
            {
                reason = "no_clay_deposit_under_pit";
                return false;
            }

            return true;
        }

        private static bool CanUseMineralBuildBlock(StrategyBuildTool tool, Vector2Int cell)
        {
            if (tool == StrategyBuildTool.Mine)
            {
                StrategyIronResourceController iron = StrategyIronResourceController.Active;
                return iron != null && iron.HasAvailableDepositAtCell(cell);
            }

            if (tool == StrategyBuildTool.CoalPit)
            {
                StrategyCoalResourceController coal = StrategyCoalResourceController.Active;
                return coal != null && coal.HasAvailableDepositAtCell(cell);
            }

            if (tool == StrategyBuildTool.ClayPit)
            {
                StrategyClayResourceController clay = StrategyClayResourceController.Active;
                return clay != null && clay.HasAvailableDepositAtCell(cell);
            }

            return false;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
