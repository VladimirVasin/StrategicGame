using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldChunkRegistry
    {
        public int CopyActiveChunkIndices(
            List<int> results,
            StrategyWorldChunkDirtyFlags dirtyFlags = StrategyWorldChunkDirtyFlags.None,
            bool consumeDirty = false)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (!IsConfigured)
            {
                return 0;
            }

            EnsureSpatialIndexFresh();
            RefreshCameraChunks();
            RefreshActiveSettlementChunks();
            for (int i = 0; i < ChunkCount; i++)
            {
                bool isDirty = dirtyFlags != StrategyWorldChunkDirtyFlags.None
                    && (dirtyChunks[i] & dirtyFlags) != StrategyWorldChunkDirtyFlags.None;
                if (!cameraChunks[i] && !activeSettlementChunks[i] && !isDirty)
                {
                    continue;
                }

                results.Add(i);
                if (isDirty && consumeDirty)
                {
                    dirtyChunks[i] &= ~dirtyFlags;
                }
            }

            return results.Count;
        }

        public int CopyAllChunkIndices(List<int> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (!IsConfigured)
            {
                return 0;
            }

            for (int i = 0; i < ChunkCount; i++)
            {
                results.Add(i);
            }

            return results.Count;
        }

        public int CopyActiveBuildings(List<StrategyPlacedBuilding> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (!IsConfigured)
            {
                return 0;
            }

            EnsureSpatialIndexFresh();
            for (int i = 0; i < activeBuildingOrder.Count; i++)
            {
                StrategyPlacedBuilding building = activeBuildingOrder[i];
                if (building != null)
                {
                    results.Add(building);
                }
            }

            return results.Count;
        }

        public int CopyActiveConstructionSites(List<StrategyConstructionSite> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (!IsConfigured)
            {
                return 0;
            }

            EnsureSpatialIndexFresh();
            for (int i = 0; i < activeConstructionSiteOrder.Count; i++)
            {
                StrategyConstructionSite site = activeConstructionSiteOrder[i];
                if (site != null && !site.IsCompleted)
                {
                    results.Add(site);
                }
            }

            return results.Count;
        }

        public int CopyActiveBuildingComponents<T>(List<T> results)
            where T : Component
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (!IsConfigured)
            {
                return 0;
            }

            EnsureSpatialIndexFresh();
            List<Component> cached = GetCachedActiveBuildingComponents(typeof(T));
            for (int i = 0; i < cached.Count; i++)
            {
                if (cached[i] is T component && component != null)
                {
                    results.Add(component);
                }
            }

            return results.Count;
        }

        private List<Component> GetCachedActiveBuildingComponents(Type componentType)
        {
            if (activeBuildingComponentCache.TryGetValue(componentType, out List<Component> cached))
            {
                return cached;
            }

            cached = new List<Component>();
            for (int i = 0; i < activeBuildingOrder.Count; i++)
            {
                StrategyPlacedBuilding building = activeBuildingOrder[i];
                if (building != null
                    && building.TryGetComponent(componentType, out Component component)
                    && component != null)
                {
                    cached.Add(component);
                }
            }

            activeBuildingComponentCache[componentType] = cached;
            return cached;
        }

        public int CopyNearbyBuildingComponents<T>(
            Vector3 nearWorld,
            List<T> results,
            int radiusChunks = 2,
            bool fallbackToAll = true)
            where T : Component
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();
            if (!IsConfigured
                || !map.TryWorldToCell(nearWorld, out Vector2Int cell)
                || !TryGetChunkCoordForCell(cell, out Vector2Int centerChunk))
            {
                return fallbackToAll ? CopyActiveBuildingComponents(results) : 0;
            }

            EnsureSpatialIndexFresh();
            int maxRadius = Mathf.Max(0, radiusChunks);
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                AddBuildingComponentsFromRing(centerChunk, radius, results);
                if (results.Count > 0)
                {
                    return results.Count;
                }
            }

            return fallbackToAll ? CopyActiveBuildingComponents(results) : 0;
        }

        private void AddBuildingComponentsFromRing<T>(Vector2Int centerChunk, int radius, List<T> results)
            where T : Component
        {
            for (int y = centerChunk.y - radius; y <= centerChunk.y + radius; y++)
            {
                for (int x = centerChunk.x - radius; x <= centerChunk.x + radius; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x - centerChunk.x), Mathf.Abs(y - centerChunk.y)) != radius
                        || !TryGetChunkIndex(new Vector2Int(x, y), out int index))
                    {
                        continue;
                    }

                    AddBuildingComponentsFromChunk(buildingsByChunk[index], results);
                }
            }
        }

        private static void AddBuildingComponentsFromChunk<T>(
            IReadOnlyList<StrategyPlacedBuilding> buildings,
            List<T> results)
            where T : Component
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null && building.TryGetComponent(out T component) && component != null)
                {
                    results.Add(component);
                }
            }
        }
    }
}
