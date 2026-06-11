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
        public CityMapCell(int x, int y, CityMapCellKind kind, CityMapWaterKind waterKind = CityMapWaterKind.None)
        {
            X = x;
            Y = y;
            Kind = kind;
            WaterKind = kind == CityMapCellKind.Water || kind == CityMapCellKind.Shore
                ? waterKind
                : CityMapWaterKind.None;
        }

        public int X { get; }
        public int Y { get; }
        public CityMapCellKind Kind { get; }
        public CityMapWaterKind WaterKind { get; }
        public bool IsWater => Kind == CityMapCellKind.Water;
        public bool IsShore => Kind == CityMapCellKind.Shore;
        public bool IsRiver => WaterKind == CityMapWaterKind.River;
        public bool IsLake => WaterKind == CityMapWaterKind.Lake;
        public bool IsBuildable => Kind != CityMapCellKind.Water;
    }

    [DisallowMultipleComponent]
    public sealed class CityMapController : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private int width = 128;
        [SerializeField] private int height = 128;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int seed = 24701;
        [SerializeField] private bool randomizeSeedOnGenerate = true;

        [Header("Visuals")]
        [SerializeField] private int tilePixels = 16;
        [SerializeField] private bool drawGrid = true;

        private SpriteRenderer spriteRenderer;
        private Texture2D mapTexture;
        private CityMapCell[,] cells;
        private bool[,] blockedWalkCells;
        private bool[,] bridgeWalkableCells;
        private int activeSeed;
        private Vector2Int riverFlowDirection = Vector2Int.right;

        public Bounds WorldBounds { get; private set; }
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int ActiveSeed => activeSeed;
        public Vector2Int RiverFlowDirection => riverFlowDirection == Vector2Int.zero
            ? Vector2Int.right
            : riverFlowDirection;

        public void GenerateMap()
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            cellSize = Mathf.Max(0.25f, cellSize);
            tilePixels = Mathf.Clamp(tilePixels, 8, 32);
            activeSeed = ResolveActiveSeed();

            cells = new CityMapCell[width, height];
            blockedWalkCells = new bool[width, height];
            bridgeWalkableCells = new bool[width, height];
            BuildCells();
            BuildTexture();

            WorldBounds = new Bounds(
                Vector3.zero,
                new Vector3(width * cellSize, height * cellSize, 0f));

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
                StrategyDebugLogger.F("riverFlow", RiverFlowDirection));
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

        public bool IsCellWalkable(Vector2Int cell)
        {
            return IsCellWalkable(cell.x, cell.y);
        }

        public bool IsCellWalkable(int x, int y)
        {
            return TryGetCell(x, y, out CityMapCell cell)
                && (cell.IsBuildable || IsBridgeWalkableCell(x, y))
                && (blockedWalkCells == null || !blockedWalkCells[x, y]);
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

        public void SetCellsWalkable(Vector2Int origin, Vector2Int size, bool isWalkable)
        {
            EnsureWalkabilityLayer();

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    int cellX = origin.x + x;
                    int cellY = origin.y + y;
                    if (cellX < 0 || cellX >= width || cellY < 0 || cellY >= height)
                    {
                        continue;
                    }

                    blockedWalkCells[cellX, cellY] = !isWalkable;
                }
            }
        }

        public void SetBridgeCellsWalkable(IReadOnlyList<Vector2Int> bridgeCells, bool isWalkable)
        {
            if (bridgeCells == null)
            {
                return;
            }

            EnsureBridgeWalkabilityLayer();
            for (int i = 0; i < bridgeCells.Count; i++)
            {
                Vector2Int cell = bridgeCells[i];
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
                {
                    continue;
                }

                bridgeWalkableCells[cell.x, cell.y] = isWalkable;
            }
        }

        public bool IsBridgeWalkableCell(Vector2Int cell)
        {
            return IsBridgeWalkableCell(cell.x, cell.y);
        }

        public bool IsBridgeWalkableCell(int x, int y)
        {
            return bridgeWalkableCells != null
                && x >= 0
                && x < width
                && y >= 0
                && y < height
                && bridgeWalkableCells[x, y];
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

        private void EnsureWalkabilityLayer()
        {
            if (blockedWalkCells == null
                || blockedWalkCells.GetLength(0) != width
                || blockedWalkCells.GetLength(1) != height)
            {
                blockedWalkCells = new bool[width, height];
            }
        }

        private void EnsureBridgeWalkabilityLayer()
        {
            if (bridgeWalkableCells == null
                || bridgeWalkableCells.GetLength(0) != width
                || bridgeWalkableCells.GetLength(1) != height)
            {
                bridgeWalkableCells = new bool[width, height];
            }
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
                    cells[x, y] = new CityMapCell(x, y, kind, waterKind);
                }
            }

            SmoothLandCells();
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

            float forestScore = forestNoise * 0.58f + moistureNoise * 0.28f + broadNoise * 0.14f;
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

        private void SmoothLandCells()
        {
            CityMapCell[,] smoothed = new CityMapCell[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CityMapCell currentCell = cells[x, y];
                    CityMapCellKind current = currentCell.Kind;
                    CityMapCellKind smoothedKind = IsWaterBoundaryKind(current)
                        ? current
                        : PickSmoothedLandKind(x, y, current);
                    CityMapWaterKind waterKind = IsWaterBoundaryKind(smoothedKind)
                        ? currentCell.WaterKind
                        : CityMapWaterKind.None;
                    smoothed[x, y] = new CityMapCell(x, y, smoothedKind, waterKind);
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[x, y] = smoothed[x, y];
                }
            }
        }

        private CityMapCellKind PickSmoothedLandKind(int x, int y, CityMapCellKind current)
        {
            int sameCount = 0;
            int grassCount = 0;
            int meadowCount = 0;
            int forestCount = 0;
            int dirtCount = 0;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    int nx = x + ox;
                    int ny = y + oy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    CityMapCellKind neighbor = cells[nx, ny].Kind;
                    if (neighbor == current)
                    {
                        sameCount++;
                    }

                    switch (neighbor)
                    {
                        case CityMapCellKind.Grass:
                            grassCount++;
                            break;
                        case CityMapCellKind.Meadow:
                            meadowCount++;
                            break;
                        case CityMapCellKind.Forest:
                            forestCount++;
                            break;
                        case CityMapCellKind.Dirt:
                            dirtCount++;
                            break;
                    }
                }
            }

            if (sameCount > 1)
            {
                return current;
            }

            CityMapCellKind replacement = current;
            int bestCount = 1;
            TryUseLandKind(CityMapCellKind.Grass, grassCount, ref replacement, ref bestCount);
            TryUseLandKind(CityMapCellKind.Meadow, meadowCount, ref replacement, ref bestCount);
            TryUseLandKind(CityMapCellKind.Forest, forestCount, ref replacement, ref bestCount);
            TryUseLandKind(CityMapCellKind.Dirt, dirtCount, ref replacement, ref bestCount);
            return replacement;
        }

        private void BuildTexture()
        {
            int textureWidth = width * tilePixels;
            int textureHeight = height * tilePixels;

            if (mapTexture != null)
            {
                Destroy(mapTexture);
            }

            mapTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            {
                name = "Generated City Map",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    StrategyTerrainTexturePainter.PaintTile(mapTexture, cells, x, y, tilePixels, activeSeed, drawGrid);
                }
            }

            mapTexture.Apply(false, false);

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            float pixelsPerUnit = tilePixels / cellSize;
            Sprite mapSprite = Sprite.Create(
                mapTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            mapSprite.name = "Generated City Map Sprite";
            spriteRenderer.sprite = mapSprite;
            spriteRenderer.sortingOrder = StrategyWorldSorting.TerrainOrder;
            transform.position = Vector3.zero;
        }

        private void OnDestroy()
        {
            if (mapTexture != null)
            {
                Destroy(mapTexture);
                mapTexture = null;
            }
        }

        private int ResolveActiveSeed()
        {
            if (randomizeSeedOnGenerate || seed == 0)
            {
                seed = UnityEngine.Random.Range(1, int.MaxValue);
            }

            return Mathf.Max(1, seed);
        }

        private MapGenerationProfile CreateGenerationProfile(int generationSeed)
        {
            System.Random random = new System.Random(generationSeed);
            bool riverHorizontal = random.NextDouble() > 0.35d;
            int blobCount = random.Next(0, 3);
            WaterBlob[] waterBlobs = new WaterBlob[blobCount];
            for (int i = 0; i < blobCount; i++)
            {
                Vector2 center = new Vector2(
                    Range(random, width * 0.14f, width * 0.86f),
                    Range(random, height * 0.14f, height * 0.86f));
                Vector2 radius = new Vector2(
                    Range(random, width * 0.05f, width * 0.12f),
                    Range(random, height * 0.05f, height * 0.12f));
                waterBlobs[i] = new WaterBlob(
                    center,
                    radius,
                    Range(random, 0.22f, 0.38f),
                    Range(random, 0.11f, 0.19f),
                    Range(random, 0.20f, 0.34f),
                    new Vector2(Range(random, 0f, 4096f), Range(random, 0f, 4096f)));
            }

            return new MapGenerationProfile
            {
                RiverHorizontal = riverHorizontal,
                RiverBaseOffset = Range(random, 0.32f, 0.68f),
                RiverFrequency = Range(random, 5.0f, 12.0f),
                RiverPhase = Range(random, 0f, Mathf.PI * 2f),
                RiverCurveAmplitude = Range(random, 3.5f, (riverHorizontal ? height : width) * 0.16f),
                RiverNoiseScale = Range(random, 1.6f, 3.4f),
                RiverNoiseAmplitude = Range(random, 2.0f, 6.5f),
                RiverNoiseOffset = new Vector2(Range(random, 0f, 4096f), Range(random, 0f, 4096f)),
                WaterHalfWidth = Range(random, 1.0f, 2.0f),
                ShoreWidth = Range(random, 0.9f, 1.8f),
                BroadScale = Range(random, 0.022f, 0.052f),
                DetailScale = Range(random, 0.085f, 0.18f),
                MoistureScale = Range(random, 0.024f, 0.056f),
                ForestScale = Range(random, 0.032f, 0.075f),
                BroadOffset = new Vector2(Range(random, 0f, 4096f), Range(random, 0f, 4096f)),
                DetailOffset = new Vector2(Range(random, 0f, 4096f), Range(random, 0f, 4096f)),
                MoistureOffset = new Vector2(Range(random, 0f, 4096f), Range(random, 0f, 4096f)),
                ForestOffset = new Vector2(Range(random, 0f, 4096f), Range(random, 0f, 4096f)),
                ForestThreshold = Range(random, 0.56f, 0.68f),
                MeadowMoistureThreshold = Range(random, 0.50f, 0.62f),
                MeadowBroadThreshold = Range(random, 0.42f, 0.58f),
                DirtMoistureThreshold = Range(random, 0.32f, 0.45f),
                DirtDetailThreshold = Range(random, 0.22f, 0.34f),
                DirtBroadThreshold = Range(random, 0.72f, 0.84f),
                WaterBlobs = waterBlobs
            };
        }

        private static float FractalNoise(int x, int y, Vector2 offset, float scale, int octaves, float persistence)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float amplitudeSum = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(offset.x + x * scale * frequency, offset.y + y * scale * frequency) * amplitude;
                amplitudeSum += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            return amplitudeSum > 0f ? total / amplitudeSum : 0f;
        }

        private static bool IsWaterBoundaryKind(CityMapCellKind kind)
        {
            return kind == CityMapCellKind.Water || kind == CityMapCellKind.Shore;
        }

        private static void TryUseLandKind(CityMapCellKind kind, int count, ref CityMapCellKind bestKind, ref int bestCount)
        {
            if (count > bestCount)
            {
                bestKind = kind;
                bestCount = count;
            }
        }

        private static float Range(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private sealed class MapGenerationProfile
        {
            public bool RiverHorizontal;
            public float RiverBaseOffset;
            public float RiverFrequency;
            public float RiverPhase;
            public float RiverCurveAmplitude;
            public float RiverNoiseScale;
            public float RiverNoiseAmplitude;
            public Vector2 RiverNoiseOffset;
            public float WaterHalfWidth;
            public float ShoreWidth;
            public float BroadScale;
            public float DetailScale;
            public float MoistureScale;
            public float ForestScale;
            public Vector2 BroadOffset;
            public Vector2 DetailOffset;
            public Vector2 MoistureOffset;
            public Vector2 ForestOffset;
            public float ForestThreshold;
            public float MeadowMoistureThreshold;
            public float MeadowBroadThreshold;
            public float DirtMoistureThreshold;
            public float DirtDetailThreshold;
            public float DirtBroadThreshold;
            public WaterBlob[] WaterBlobs;
        }

        private readonly struct WaterBlob
        {
            public WaterBlob(
                Vector2 center,
                Vector2 radius,
                float shoreWidth,
                float noiseScale,
                float raggedness,
                Vector2 noiseOffset)
            {
                Center = center;
                Radius = radius;
                ShoreWidth = shoreWidth;
                NoiseScale = noiseScale;
                Raggedness = raggedness;
                NoiseOffset = noiseOffset;
            }

            public Vector2 Center { get; }
            public Vector2 Radius { get; }
            public float ShoreWidth { get; }
            public float NoiseScale { get; }
            public float Raggedness { get; }
            public Vector2 NoiseOffset { get; }
        }
    }
}
