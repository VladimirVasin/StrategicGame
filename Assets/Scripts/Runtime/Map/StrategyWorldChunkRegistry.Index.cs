using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldChunkRegistry
    {
        private void AllocateChunks()
        {
            if (map == null)
            {
                chunkColumns = 0;
                chunkRows = 0;
                buildingsByChunk = null;
                constructionSitesByChunk = null;
                residentsByChunk = null;
                cameraChunks = null;
                activeSettlementChunks = null;
                dirtyChunks = null;
                return;
            }

            chunkColumns = Mathf.CeilToInt(map.Width / (float)ChunkSize);
            chunkRows = Mathf.CeilToInt(map.Height / (float)ChunkSize);
            int count = ChunkCount;
            buildingsByChunk = CreateChunkLists<StrategyPlacedBuilding>(count);
            constructionSitesByChunk = CreateChunkLists<StrategyConstructionSite>(count);
            residentsByChunk = CreateChunkLists<StrategyResidentAgent>(count);
            cameraChunks = new bool[count];
            activeSettlementChunks = new bool[count];
            dirtyChunks = new StrategyWorldChunkDirtyFlags[count];
        }

        private void RefreshAll()
        {
            if (!IsConfigured)
            {
                return;
            }

            RefreshSpatialIndex();
            RefreshCameraChunks();
            RefreshActiveSettlementChunks();
        }

        private void EnsureSpatialIndexFresh()
        {
            if (!IsConfigured)
            {
                return;
            }

            int buildingCount = StrategyPlacedBuilding.ActiveBuildings.Count;
            int constructionSiteCount = StrategyConstructionSite.ActiveSites.Count;
            int residentCount = population != null && population.Residents != null ? population.Residents.Count : 0;
            if (buildingCount != observedBuildingCount
                || constructionSiteCount != observedConstructionSiteCount
                || residentCount != observedResidentCount)
            {
                RefreshSpatialIndex();
            }
        }

        private void RefreshSpatialIndex()
        {
            activeBuildingOrder.Clear();
            activeConstructionSiteOrder.Clear();
            activeBuildingComponentCache.Clear();
            ClearChunkLists(buildingsByChunk);
            ClearChunkLists(constructionSitesByChunk);
            ClearChunkLists(residentsByChunk);
            IndexBuildings();
            IndexConstructionSites();
            IndexResidents();
            observedBuildingCount = StrategyPlacedBuilding.ActiveBuildings.Count;
            observedConstructionSiteCount = StrategyConstructionSite.ActiveSites.Count;
            observedResidentCount = population != null && population.Residents != null ? population.Residents.Count : 0;
        }

        private void IndexBuildings()
        {
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null)
                {
                    continue;
                }

                activeBuildingOrder.Add(building);
                if (TryGetChunkIndexForCell(building.Origin, out int index))
                {
                    buildingsByChunk[index].Add(building);
                }
            }
        }

        private void IndexConstructionSites()
        {
            IReadOnlyList<StrategyConstructionSite> sites = StrategyConstructionSite.ActiveSites;
            for (int i = 0; i < sites.Count; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                activeConstructionSiteOrder.Add(site);
                if (TryGetChunkIndexForCell(site.Origin, out int index))
                {
                    constructionSitesByChunk[index].Add(site);
                }
            }
        }

        private void IndexResidents()
        {
            if (population == null || population.Residents == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && map.TryWorldToCell(resident.transform.position, out Vector2Int cell)
                    && TryGetChunkIndexForCell(cell, out int index))
                {
                    residentsByChunk[index].Add(resident);
                }
            }
        }

        private void RefreshCameraChunks()
        {
            Array.Clear(cameraChunks, 0, cameraChunks.Length);
            Camera camera = strategyCamera != null ? strategyCamera : Camera.main;
            if (camera == null)
            {
                return;
            }

            float height = Mathf.Max(0.1f, camera.orthographicSize * 2f);
            float width = height * Mathf.Max(0.1f, camera.aspect);
            Bounds bounds = new Bounds(
                new Vector3(camera.transform.position.x, camera.transform.position.y, 0f),
                new Vector3(width, height, 0f));
            MarkChunkRange(bounds, CameraPaddingChunks, cameraChunks);
        }

        private void RefreshActiveSettlementChunks()
        {
            Array.Clear(activeSettlementChunks, 0, activeSettlementChunks.Length);
            for (int i = 0; i < buildingsByChunk.Length; i++)
            {
                if (buildingsByChunk[i].Count > 0
                    || constructionSitesByChunk[i].Count > 0
                    || residentsByChunk[i].Count > 0)
                {
                    MarkChunkAndNeighbors(i, SettlementPaddingChunks, activeSettlementChunks);
                }
            }

            if (population != null && population.TryGetCampCell(out Vector2Int campCell))
            {
                MarkCellChunkAndNeighbors(campCell, SettlementPaddingChunks, activeSettlementChunks);
            }
        }

        private void MarkChunkRange(Bounds bounds, int paddingChunks, bool[] target)
        {
            if (!TryGetCellRange(bounds, out Vector2Int minCell, out Vector2Int maxCell))
            {
                return;
            }

            TryGetChunkCoordForCell(minCell, out Vector2Int minChunk);
            TryGetChunkCoordForCell(maxCell, out Vector2Int maxChunk);
            for (int y = minChunk.y - paddingChunks; y <= maxChunk.y + paddingChunks; y++)
            {
                for (int x = minChunk.x - paddingChunks; x <= maxChunk.x + paddingChunks; x++)
                {
                    if (TryGetChunkIndex(new Vector2Int(x, y), out int index))
                    {
                        target[index] = true;
                    }
                }
            }
        }

        private bool TryGetCellRange(Bounds bounds, out Vector2Int minCell, out Vector2Int maxCell)
        {
            minCell = default;
            maxCell = default;
            if (!IsConfigured)
            {
                return false;
            }

            minCell = WorldToClampedCell(bounds.min);
            maxCell = WorldToClampedCell(bounds.max);
            return true;
        }

        private Vector2Int WorldToClampedCell(Vector3 world)
        {
            Vector3 origin = map.WorldBounds.min;
            int x = Mathf.FloorToInt((world.x - origin.x) / map.CellSize);
            int y = Mathf.FloorToInt((world.y - origin.y) / map.CellSize);
            return new Vector2Int(Mathf.Clamp(x, 0, map.Width - 1), Mathf.Clamp(y, 0, map.Height - 1));
        }

        private void MarkCellChunkAndNeighbors(Vector2Int cell, int radius, bool[] target)
        {
            if (TryGetChunkIndexForCell(cell, out int index))
            {
                MarkChunkAndNeighbors(index, radius, target);
            }
        }

        private void MarkChunkAndNeighbors(int index, int radius, bool[] target)
        {
            int chunkX = index % chunkColumns;
            int chunkY = index / chunkColumns;
            for (int y = chunkY - radius; y <= chunkY + radius; y++)
            {
                for (int x = chunkX - radius; x <= chunkX + radius; x++)
                {
                    if (TryGetChunkIndex(new Vector2Int(x, y), out int nearbyIndex))
                    {
                        target[nearbyIndex] = true;
                    }
                }
            }
        }

        private bool TryGetChunkIndex(Vector2Int chunk, out int index)
        {
            if (chunk.x >= 0 && chunk.x < chunkColumns && chunk.y >= 0 && chunk.y < chunkRows)
            {
                index = GetChunkIndex(chunk);
                return true;
            }

            index = -1;
            return false;
        }

        private int GetChunkIndex(Vector2Int chunk)
        {
            return chunk.y * chunkColumns + chunk.x;
        }

        private bool IsValidChunkIndex(int index)
        {
            return IsConfigured && index >= 0 && index < ChunkCount;
        }

        private bool IsCellInsideMap(Vector2Int cell)
        {
            return map != null && cell.x >= 0 && cell.x < map.Width && cell.y >= 0 && cell.y < map.Height;
        }

        private static List<T>[] CreateChunkLists<T>(int count)
        {
            List<T>[] lists = new List<T>[count];
            for (int i = 0; i < lists.Length; i++)
            {
                lists[i] = new List<T>();
            }

            return lists;
        }

        private static void ClearChunkLists<T>(IReadOnlyList<List<T>> lists)
        {
            if (lists == null)
            {
                return;
            }

            for (int i = 0; i < lists.Count; i++)
            {
                lists[i].Clear();
            }
        }
    }
}
