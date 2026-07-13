using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private void BuildFishWaterRegions()
        {
            lakeFishRegions.Clear();
            lakeRegionByCell.Clear();
            riverRouteCells.Clear();
            BuildLakeFishRegions();
            BuildRiverFishRoute();

            StrategyDebugLogger.Info(
                "Wildlife",
                "FishWaterRegionsBuilt",
                StrategyDebugLogger.F("lakeRegions", lakeFishRegions.Count),
                StrategyDebugLogger.F("lakeCapacity", GetTotalLakeFishCapacity()),
                StrategyDebugLogger.F("riverRouteCells", riverRouteCells.Count),
                StrategyDebugLogger.F("riverFlow", map != null ? map.RiverFlowDirection : Vector2Int.zero));
        }

        private void BuildLakeFishRegions()
        {
            if (map == null)
            {
                return;
            }

            bool[,] visited = new bool[map.Width, map.Height];
            int nextRegionId = 0;
            Queue<Vector2Int> open = new();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2Int origin = new(x, y);
                    if (visited[x, y] || !IsWaterCellOfKind(origin, CityMapWaterKind.Lake))
                    {
                        continue;
                    }

                    FishWaterRegion region = new() { Id = nextRegionId++ };
                    open.Clear();
                    open.Enqueue(origin);
                    visited[x, y] = true;
                    while (open.Count > 0)
                    {
                        Vector2Int current = open.Dequeue();
                        region.Cells.Add(current);
                        lakeRegionByCell[current] = region.Id;
                        for (int i = 0; i < CardinalDirections.Length; i++)
                        {
                            Vector2Int next = current + CardinalDirections[i];
                            if (next.x < 0
                                || next.x >= map.Width
                                || next.y < 0
                                || next.y >= map.Height
                                || visited[next.x, next.y]
                                || !IsWaterCellOfKind(next, CityMapWaterKind.Lake))
                            {
                                continue;
                            }

                            visited[next.x, next.y] = true;
                            open.Enqueue(next);
                        }
                    }

                    region.Center = PickWaterRegionCenter(region.Cells);
                    region.Capacity = Mathf.Clamp(
                        Mathf.CeilToInt(region.Cells.Count / (float)LakeFishCellsPerCapacity),
                        LakeFishRegionMinCap,
                        LakeFishRegionMaxCap);
                    lakeFishRegions.Add(region);
                }
            }
        }
    }
}
