using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum CityMapCellKind
    {
        Grass,
        Meadow,
        Forest,
        Dirt,
        Water,
        Shore
    }

    public enum CityMapWaterKind
    {
        None,
        River,
        Lake
    }

    public readonly struct CityMapCell
    {
        public CityMapCell(
            int x,
            int y,
            CityMapCellKind kind,
            CityMapWaterKind waterKind = CityMapWaterKind.None,
            float reliefHeight = 0f)
        {
            X = x;
            Y = y;
            Kind = kind;
            WaterKind = kind == CityMapCellKind.Water || kind == CityMapCellKind.Shore
                ? waterKind
                : CityMapWaterKind.None;
            ReliefHeight = Mathf.Clamp01(reliefHeight);
        }

        public int X { get; }
        public int Y { get; }
        public CityMapCellKind Kind { get; }
        public CityMapWaterKind WaterKind { get; }
        public float ReliefHeight { get; }
        public bool IsWater => Kind == CityMapCellKind.Water;
        public bool IsShore => Kind == CityMapCellKind.Shore;
        public bool IsRiver => WaterKind == CityMapWaterKind.River;
        public bool IsLake => WaterKind == CityMapWaterKind.Lake;
        public bool IsBuildable => Kind != CityMapCellKind.Water;
    }

    [DisallowMultipleComponent]
    public sealed partial class CityMapController : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private int width = 192;
        [SerializeField] private int height = 192;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int seed = 24701;
        [SerializeField] private bool randomizeSeedOnGenerate = true;

        [Header("Visuals")]
        [SerializeField] private int tilePixels = 16;
        [SerializeField] private bool drawGrid = true;

        private SpriteRenderer spriteRenderer;
        private Texture2D mapTexture;
        private CityMapCell[,] cells;
        private int[,] blockedWalkCounts;
        private int[,] blockedBuildCounts;
        private bool[,] bridgeWalkableCells;
        private int activeSeed;
        private Vector2Int riverFlowDirection = Vector2Int.right;

        public Bounds WorldBounds { get; private set; }
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int ActiveSeed => activeSeed;
        public int WalkabilityVersion { get; private set; }
        public Vector2Int RiverFlowDirection => riverFlowDirection == Vector2Int.zero
            ? Vector2Int.right
            : riverFlowDirection;

        public void GenerateMap()
        {
            IsGenerating = true;
            IsGenerated = false;
            GenerationFailed = false;
            GenerationProgress = 0f;
            GenerationStage = "Generating terrain";
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            cellSize = Mathf.Max(0.25f, cellSize);
            tilePixels = Mathf.Clamp(tilePixels, 8, 32);
            activeSeed = ResolveActiveSeed();

            cells = new CityMapCell[width, height];
            blockedWalkCounts = new int[width, height];
            blockedBuildCounts = new int[width, height];
            bridgeWalkableCells = new bool[width, height];
            WalkabilityVersion++;
            System.Diagnostics.Stopwatch generationTimer = System.Diagnostics.Stopwatch.StartNew();
            BuildCells();
            long buildCellsMs = generationTimer.ElapsedMilliseconds;
            BuildTexture();
            long buildTextureMs = generationTimer.ElapsedMilliseconds - buildCellsMs;

            WorldBounds = new Bounds(
                Vector3.zero,
                new Vector3(width * cellSize, height * cellSize, 0f));

            long countStartedMs = generationTimer.ElapsedMilliseconds;
            CountTerrain(
                out int grass,
                out int meadow,
                out int forest,
                out int dirt,
                out int water,
                out int shore,
                out int riverWater,
                out int riverShore,
                out int lakeWater,
                out int lakeShore);
            long countTerrainMs = generationTimer.ElapsedMilliseconds - countStartedMs;
            countStartedMs = generationTimer.ElapsedMilliseconds;
            CountRelief(out int hillCells, out int mountainCells);
            long countReliefMs = generationTimer.ElapsedMilliseconds - countStartedMs;
            StrategyDebugLogger.Info(
                "Map",
                "Generated",
                StrategyDebugLogger.F("seed", activeSeed),
                StrategyDebugLogger.F("width", width),
                StrategyDebugLogger.F("height", height),
                StrategyDebugLogger.F("cellSize", cellSize),
                StrategyDebugLogger.F("grass", grass),
                StrategyDebugLogger.F("meadow", meadow),
                StrategyDebugLogger.F("forest", forest),
                StrategyDebugLogger.F("dirt", dirt),
                StrategyDebugLogger.F("water", water),
                StrategyDebugLogger.F("shore", shore),
                StrategyDebugLogger.F("riverWater", riverWater),
                StrategyDebugLogger.F("riverShore", riverShore),
                StrategyDebugLogger.F("lakeWater", lakeWater),
                StrategyDebugLogger.F("lakeShore", lakeShore),
                StrategyDebugLogger.F("hillCells", hillCells),
                StrategyDebugLogger.F("mountainCells", mountainCells),
                StrategyDebugLogger.F("riverFlow", RiverFlowDirection),
                StrategyDebugLogger.F("durationMs", generationTimer.ElapsedMilliseconds),
                StrategyDebugLogger.F("buildCellsMs", buildCellsMs),
                StrategyDebugLogger.F("buildTextureMs", buildTextureMs),
                StrategyDebugLogger.F("countTerrainMs", countTerrainMs),
                StrategyDebugLogger.F("countReliefMs", countReliefMs));
            IsGenerating = false;
            IsGenerated = true;
            GenerationProgress = 1f;
            GenerationStage = "Ready";
        }

        public bool TryGetCell(int x, int y, out CityMapCell cell)
        {
            if (cells != null && x >= 0 && x < width && y >= 0 && y < height)
            {
                cell = cells[x, y];
                return true;
            }

            cell = default;
            return false;
        }

        public bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cell)
        {
            Vector3 min = WorldBounds.min;
            int x = Mathf.FloorToInt((worldPosition.x - min.x) / cellSize);
            int y = Mathf.FloorToInt((worldPosition.y - min.y) / cellSize);
            cell = new Vector2Int(x, y);
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool TryGetWaterKind(Vector2Int cell, out CityMapWaterKind waterKind)
        {
            return TryGetWaterKind(cell.x, cell.y, out waterKind);
        }

        public bool TryGetWaterKind(int x, int y, out CityMapWaterKind waterKind)
        {
            if (TryGetCell(x, y, out CityMapCell cell)
                && (cell.Kind == CityMapCellKind.Water || cell.Kind == CityMapCellKind.Shore))
            {
                waterKind = cell.WaterKind;
                return waterKind != CityMapWaterKind.None;
            }

            waterKind = CityMapWaterKind.None;
            return false;
        }

        public bool IsRiverCell(Vector2Int cell)
        {
            return IsRiverCell(cell.x, cell.y);
        }

        public bool IsRiverCell(int x, int y)
        {
            return TryGetCell(x, y, out CityMapCell cell) && cell.IsRiver;
        }

        public bool IsLakeCell(Vector2Int cell)
        {
            return IsLakeCell(cell.x, cell.y);
        }

        public bool IsLakeCell(int x, int y)
        {
            return TryGetCell(x, y, out CityMapCell cell) && cell.IsLake;
        }

        public Vector3 GetCellCenterWorld(int x, int y)
        {
            Vector3 min = WorldBounds.min;
            return new Vector3(
                min.x + (x + 0.5f) * cellSize,
                min.y + (y + 0.5f) * cellSize,
                0f);
        }

        public Bounds GetCellRectWorld(Vector2Int origin, Vector2Int size)
        {
            Vector3 min = WorldBounds.min + new Vector3(origin.x * cellSize, origin.y * cellSize, 0f);
            Vector3 worldSize = new Vector3(size.x * cellSize, size.y * cellSize, 0f);
            return new Bounds(min + worldSize * 0.5f, worldSize);
        }

        private void CountTerrain(
            out int grass,
            out int meadow,
            out int forest,
            out int dirt,
            out int water,
            out int shore,
            out int riverWater,
            out int riverShore,
            out int lakeWater,
            out int lakeShore)
        {
            grass = 0;
            meadow = 0;
            forest = 0;
            dirt = 0;
            water = 0;
            shore = 0;
            riverWater = 0;
            riverShore = 0;
            lakeWater = 0;
            lakeShore = 0;

            if (cells == null)
            {
                return;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    switch (cells[x, y].Kind)
                    {
                        case CityMapCellKind.Grass:
                            grass++;
                            break;
                        case CityMapCellKind.Meadow:
                            meadow++;
                            break;
                        case CityMapCellKind.Forest:
                            forest++;
                            break;
                        case CityMapCellKind.Dirt:
                            dirt++;
                            break;
                        case CityMapCellKind.Water:
                            water++;
                            if (cells[x, y].WaterKind == CityMapWaterKind.River)
                            {
                                riverWater++;
                            }
                            else if (cells[x, y].WaterKind == CityMapWaterKind.Lake)
                            {
                                lakeWater++;
                            }

                            break;
                        case CityMapCellKind.Shore:
                            shore++;
                            if (cells[x, y].WaterKind == CityMapWaterKind.River)
                            {
                                riverShore++;
                            }
                            else if (cells[x, y].WaterKind == CityMapWaterKind.Lake)
                            {
                                lakeShore++;
                            }

                            break;
                    }
                }
            }
        }

        private void BuildCells()
        {
            MapGenerationProfile profile = CreateGenerationProfile(activeSeed);
            riverFlowDirection = profile.RiverHorizontal ? Vector2Int.right : Vector2Int.up;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CityMapCellKind kind = PickCellKind(x, y, profile, out CityMapWaterKind waterKind);
                    float reliefHeight = PickReliefHeight(x, y, profile, kind, waterKind);
                    cells[x, y] = new CityMapCell(x, y, kind, waterKind, reliefHeight);
                }
            }

            SmoothLandCells();
            EnsureMinimumForestCoverage(profile);
        }

        private CityMapCellKind PickCellKind(
            int x,
            int y,
            MapGenerationProfile profile,
            out CityMapWaterKind waterKind)
        {
            if (TryPickWaterKind(x, y, profile, out CityMapCellKind kind, out waterKind))
            {
                return kind;
            }

            waterKind = CityMapWaterKind.None;

            float broadNoise = FractalNoise(x, y, profile.BroadOffset, profile.BroadScale, 3, 0.54f);
            float detailNoise = FractalNoise(x, y, profile.DetailOffset, profile.DetailScale, 2, 0.48f);
            float moistureNoise = FractalNoise(x, y, profile.MoistureOffset, profile.MoistureScale, 3, 0.58f);
            float forestNoise = FractalNoise(x, y, profile.ForestOffset, profile.ForestScale, 3, 0.52f);

            float forestScore = CalculateForestScore(forestNoise, moistureNoise, broadNoise);
            if (forestScore > profile.ForestThreshold)
            {
                return CityMapCellKind.Forest;
            }

            if (moistureNoise > profile.MeadowMoistureThreshold
                && broadNoise < profile.MeadowBroadThreshold
                && detailNoise > 0.24f)
            {
                return CityMapCellKind.Meadow;
            }

            if (moistureNoise < profile.DirtMoistureThreshold
                && broadNoise > 0.30f
                && (detailNoise < profile.DirtDetailThreshold || broadNoise > profile.DirtBroadThreshold))
            {
                return CityMapCellKind.Dirt;
            }

            if (moistureNoise > profile.MeadowMoistureThreshold + 0.08f && detailNoise > 0.38f)
            {
                return CityMapCellKind.Meadow;
            }

            return CityMapCellKind.Grass;
        }

        private bool TryPickWaterKind(
            int x,
            int y,
            MapGenerationProfile profile,
            out CityMapCellKind kind,
            out CityMapWaterKind waterKind)
        {
            float along = profile.RiverHorizontal ? x : y;
            float across = profile.RiverHorizontal ? y : x;
            float alongMax = Mathf.Max(1f, (profile.RiverHorizontal ? width : height) - 1f);
            float acrossMax = profile.RiverHorizontal ? height : width;
            float normalizedAlong = along / alongMax;
            float riverNoise = Mathf.PerlinNoise(
                profile.RiverNoiseOffset.x + normalizedAlong * profile.RiverNoiseScale,
                profile.RiverNoiseOffset.y);
            float riverCenter = acrossMax * profile.RiverBaseOffset
                + Mathf.Sin(normalizedAlong * profile.RiverFrequency + profile.RiverPhase) * profile.RiverCurveAmplitude
                + (riverNoise - 0.5f) * profile.RiverNoiseAmplitude;
            float riverDistance = Mathf.Abs(across - riverCenter);

            if (riverDistance <= profile.WaterHalfWidth)
            {
                kind = CityMapCellKind.Water;
                waterKind = CityMapWaterKind.River;
                return true;
            }

            if (riverDistance <= profile.WaterHalfWidth + profile.ShoreWidth)
            {
                kind = CityMapCellKind.Shore;
                waterKind = CityMapWaterKind.River;
                return true;
            }

            for (int i = 0; i < profile.WaterBlobs.Length; i++)
            {
                WaterBlob blob = profile.WaterBlobs[i];
                float dx = (x - blob.Center.x) / blob.Radius.x;
                float dy = (y - blob.Center.y) / blob.Radius.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float ragged = Mathf.PerlinNoise(
                    blob.NoiseOffset.x + x * blob.NoiseScale,
                    blob.NoiseOffset.y + y * blob.NoiseScale);
                float edge = distance + (ragged - 0.5f) * blob.Raggedness;

                if (edge <= 1f)
                {
                    kind = CityMapCellKind.Water;
                    waterKind = CityMapWaterKind.Lake;
                    return true;
                }

                if (edge <= 1f + blob.ShoreWidth)
                {
                    kind = CityMapCellKind.Shore;
                    waterKind = CityMapWaterKind.Lake;
                    return true;
                }
            }

            kind = default;
            waterKind = CityMapWaterKind.None;
            return false;
        }
    }
}
