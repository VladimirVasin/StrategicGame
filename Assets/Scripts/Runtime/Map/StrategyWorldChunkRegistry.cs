using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [Flags]
    public enum StrategyWorldChunkDirtyFlags
    {
        None = 0,
        Fog = 1 << 0,
        Weather = 1 << 1,
        Roads = 1 << 2,
        Props = 1 << 3,
        Lights = 1 << 4,
        Resources = 1 << 5,
        Buildings = 1 << 6,
        Residents = 1 << 7,
        All = Fog | Weather | Roads | Props | Lights | Resources | Buildings | Residents
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyWorldChunkRegistry : MonoBehaviour
    {
        public const int ChunkSize = 16;

        private const float RefreshIntervalSeconds = 0.35f;
        private const int CameraPaddingChunks = 1;
        private const int SettlementPaddingChunks = 1;

        private CityMapController map;
        private StrategyPopulationController population;
        private Camera strategyCamera;
        private readonly List<StrategyPlacedBuilding> activeBuildingOrder = new();
        private readonly List<StrategyConstructionSite> activeConstructionSiteOrder = new();
        private readonly Dictionary<Type, List<Component>> activeBuildingComponentCache = new();
        private List<StrategyPlacedBuilding>[] buildingsByChunk;
        private List<StrategyConstructionSite>[] constructionSitesByChunk;
        private List<StrategyResidentAgent>[] residentsByChunk;
        private bool[] cameraChunks;
        private bool[] activeSettlementChunks;
        private StrategyWorldChunkDirtyFlags[] dirtyChunks;
        private float refreshTimer;
        private int chunkColumns;
        private int chunkRows;
        private int observedBuildingCount = -1;
        private int observedConstructionSiteCount = -1;
        private int observedResidentCount = -1;

        public static StrategyWorldChunkRegistry Active { get; private set; }
        public bool IsConfigured => map != null && chunkColumns > 0 && chunkRows > 0;
        public int ChunkColumns => chunkColumns;
        public int ChunkRows => chunkRows;
        public int ChunkCount => chunkColumns * chunkRows;
        public IReadOnlyList<StrategyPlacedBuilding> ActiveBuildingsView
        {
            get
            {
                if (!IsConfigured)
                {
                    return Array.Empty<StrategyPlacedBuilding>();
                }

                EnsureSpatialIndexFresh();
                return activeBuildingOrder;
            }
        }

        public IReadOnlyList<StrategyConstructionSite> ActiveConstructionSitesView
        {
            get
            {
                if (!IsConfigured)
                {
                    return Array.Empty<StrategyConstructionSite>();
                }

                EnsureSpatialIndexFresh();
                return activeConstructionSiteOrder;
            }
        }

        private void Awake()
        {
            Active = this;
        }

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            Camera camera)
        {
            map = mapController;
            population = populationController;
            strategyCamera = camera != null ? camera : Camera.main;
            AllocateChunks();
            RefreshAll();
            StrategyDebugLogger.Info(
                "Map",
                "WorldChunksConfigured",
                StrategyDebugLogger.F("chunkSize", ChunkSize),
                StrategyDebugLogger.F("columns", chunkColumns),
                StrategyDebugLogger.F("rows", chunkRows),
                StrategyDebugLogger.F("chunks", ChunkCount));
        }

        private void Update()
        {
            if (!IsConfigured)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = RefreshIntervalSeconds;
            RefreshAll();
        }

        public bool TryGetChunkCoordForCell(Vector2Int cell, out Vector2Int chunk)
        {
            chunk = default;
            if (!IsConfigured || !IsCellInsideMap(cell))
            {
                return false;
            }

            chunk = new Vector2Int(
                Mathf.Clamp(cell.x / ChunkSize, 0, chunkColumns - 1),
                Mathf.Clamp(cell.y / ChunkSize, 0, chunkRows - 1));
            return true;
        }

        public bool TryGetChunkIndexForCell(Vector2Int cell, out int index)
        {
            if (TryGetChunkCoordForCell(cell, out Vector2Int chunk))
            {
                index = GetChunkIndex(chunk);
                return true;
            }

            index = -1;
            return false;
        }

        public bool IsCellNearCamera(Vector2Int cell)
        {
            return TryGetChunkIndexForCell(cell, out int index) && cameraChunks[index];
        }

        public bool IsCellInActiveSettlementChunk(Vector2Int cell)
        {
            return TryGetChunkIndexForCell(cell, out int index) && activeSettlementChunks[index];
        }

        public bool TryGetChunkCellBounds(int index, out Vector2Int origin, out Vector2Int size)
        {
            origin = default;
            size = default;
            if (!IsValidChunkIndex(index))
            {
                return false;
            }

            int chunkX = index % chunkColumns;
            int chunkY = index / chunkColumns;
            origin = new Vector2Int(chunkX * ChunkSize, chunkY * ChunkSize);
            size = new Vector2Int(
                Mathf.Min(ChunkSize, map.Width - origin.x),
                Mathf.Min(ChunkSize, map.Height - origin.y));
            return true;
        }

        public void MarkDirty(Vector2Int cell, StrategyWorldChunkDirtyFlags flags)
        {
            if (flags == StrategyWorldChunkDirtyFlags.None
                || !TryGetChunkIndexForCell(cell, out int index))
            {
                return;
            }

            dirtyChunks[index] |= flags;
        }

        public void MarkDirty(Bounds worldBounds, StrategyWorldChunkDirtyFlags flags)
        {
            if (flags == StrategyWorldChunkDirtyFlags.None
                || !TryGetCellRange(worldBounds, out Vector2Int minCell, out Vector2Int maxCell))
            {
                return;
            }

            for (int y = minCell.y; y <= maxCell.y; y += ChunkSize)
            {
                for (int x = minCell.x; x <= maxCell.x; x += ChunkSize)
                {
                    MarkDirty(new Vector2Int(x, y), flags);
                }
            }

            MarkDirty(maxCell, flags);
        }

        public bool ConsumeDirty(int index, StrategyWorldChunkDirtyFlags flags)
        {
            if (!IsValidChunkIndex(index) || flags == StrategyWorldChunkDirtyFlags.None)
            {
                return false;
            }

            StrategyWorldChunkDirtyFlags current = dirtyChunks[index] & flags;
            if (current == StrategyWorldChunkDirtyFlags.None)
            {
                return false;
            }

            dirtyChunks[index] &= ~flags;
            return true;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
