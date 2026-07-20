using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class CityMapController
    {
        private const float DefaultGenerationFrameBudgetMs = 4f;

        private int generationToken;
        private float generationFrameBudgetMs = DefaultGenerationFrameBudgetMs;
        private CancellationTokenSource generationCancellation;

        public bool IsGenerated { get; private set; }
        public bool IsGenerating { get; private set; }
        public bool GenerationFailed { get; private set; }
        private string generationStageKey = "map.generation.waiting";

        public float GenerationProgress { get; private set; }
        public string GenerationStage => StrategyLocalization.Get(StrategyLocalizationTables.Menu, generationStageKey);

        public void SetGenerationFrameBudget(float milliseconds)
        {
            generationFrameBudgetMs = Mathf.Clamp(milliseconds, 1f, 24f);
        }

        public void CancelIncrementalGeneration()
        {
            generationToken++;
            generationCancellation?.Cancel();
            IsGenerating = false;
            if (!IsGenerated)
            {
                generationStageKey = "map.generation.cancelled";
            }
        }

        public void SetPresentationVisible(bool visible)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        public IEnumerator GenerateMapIncremental(int requestedSeed)
        {
            int token = ++generationToken;
            PrepareIncrementalGeneration(requestedSeed);
            yield return null;
            if (token != generationToken)
            {
                yield break;
            }

            Stopwatch totalTimer = Stopwatch.StartNew();
            Stopwatch frameTimer = Stopwatch.StartNew();
            MapGenerationProfile profile = CreateGenerationProfile(activeSeed);
            riverFlowDirection = profile.RiverHorizontal ? Vector2Int.right : Vector2Int.up;

            generationStageKey = "map.generation.shaping_terrain";
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CityMapCellKind kind = PickCellKind(x, y, profile, out CityMapWaterKind waterKind);
                    cells[x, y] = new CityMapCell(x, y, kind, waterKind);
                }

                GenerationProgress = Mathf.Lerp(0f, 0.08f, (y + 1f) / height);
                if (ShouldYield(frameTimer))
                {
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    yield return null;
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    frameTimer.Restart();
                }
            }

            generationStageKey = "map.generation.raising_terrain";
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CityMapCell cell = cells[x, y];
                    float reliefHeight = PickReliefHeight(x, y, profile, cell.Kind, cell.WaterKind);
                    cells[x, y] = new CityMapCell(x, y, cell.Kind, cell.WaterKind, reliefHeight);
                }

                GenerationProgress = Mathf.Lerp(0.08f, 0.12f, (y + 1f) / height);
                if (ShouldYield(frameTimer))
                {
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    yield return null;
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    frameTimer.Restart();
                }
            }

            generationStageKey = "map.generation.smoothing_terrain";
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
                    smoothed[x, y] = new CityMapCell(x, y, smoothedKind, waterKind, currentCell.ReliefHeight);
                }

                GenerationProgress = Mathf.Lerp(0.12f, 0.145f, (y + 1f) / height);
                if (ShouldYield(frameTimer))
                {
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    yield return null;
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    frameTimer.Restart();
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[x, y] = smoothed[x, y];
                }

                GenerationProgress = Mathf.Lerp(0.145f, 0.16f, (y + 1f) / height);
                if (ShouldYield(frameTimer))
                {
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    yield return null;
                    if (token != generationToken)
                    {
                        yield break;
                    }

                    frameTimer.Restart();
                }
            }

            EnsureMinimumForestCoverage(profile);
            if (token != generationToken)
            {
                yield break;
            }

            generationStageKey = "map.generation.painting_terrain";
            CityMapCell[,] paintCells = cells;
            int paintWidth = width;
            int paintHeight = height;
            int paintTilePixels = tilePixels;
            int paintSeed = activeSeed;
            bool paintGrid = drawGrid;
            int textureWidth = paintWidth * paintTilePixels;
            int textureHeight = paintHeight * paintTilePixels;
            CancellationToken cancellationToken = generationCancellation.Token;
            int paintedRows = 0;
            Task<Color32[]> paintTask = Task.Run(() =>
            {
                using var profilerScope = StrategyPerformanceMarkers.TerrainPaint.Auto();
                Color32[] result = new Color32[textureWidth * textureHeight];
                Parallel.For(
                    0,
                    paintHeight,
                    new ParallelOptions { CancellationToken = cancellationToken },
                    y =>
                    {
                        for (int x = 0; x < paintWidth; x++)
                        {
                            StrategyTerrainTexturePainter.PaintTile(
                                result,
                                textureWidth,
                                paintCells,
                                x,
                                y,
                                paintTilePixels,
                                paintSeed,
                                paintGrid);
                        }

                        Interlocked.Increment(ref paintedRows);
                    });
                return result;
            }, cancellationToken);

            while (!paintTask.IsCompleted)
            {
                GenerationProgress = Mathf.Lerp(
                    0.16f,
                    0.96f,
                    Volatile.Read(ref paintedRows) / (float)paintHeight);
                if (token != generationToken || cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return null;
            }

            if (paintTask.IsCanceled || token != generationToken)
            {
                yield break;
            }

            if (paintTask.IsFaulted)
            {
                IsGenerating = false;
                GenerationFailed = true;
                generationStageKey = "map.generation.preload_failed";
                StrategyDebugLogger.Warn(
                    "Map",
                    "PreloadPaintFailed",
                    StrategyDebugLogger.F("error", paintTask.Exception?.GetBaseException().Message ?? "unknown"));
                yield break;
            }

            Color32[] pixels = paintTask.Result;
            generationStageKey = "map.generation.uploading_terrain";
            GenerationProgress = 0.98f;
            ApplyTexturePixels(pixels, textureWidth, textureHeight);
            WorldBounds = new Bounds(Vector3.zero, new Vector3(width * cellSize, height * cellSize, 0f));
            SetPresentationVisible(false);

            IsGenerating = false;
            IsGenerated = true;
            GenerationFailed = false;
            GenerationProgress = 1f;
            generationStageKey = "map.generation.ready";
            StrategyDebugLogger.Info(
                "Map",
                "Preloaded",
                StrategyDebugLogger.F("seed", paintSeed),
                StrategyDebugLogger.F("width", paintWidth),
                StrategyDebugLogger.F("height", paintHeight),
                StrategyDebugLogger.F("durationMs", totalTimer.ElapsedMilliseconds));
        }

        private void PrepareIncrementalGeneration(int requestedSeed)
        {
            StrategyVisualCatalogProvider.Prewarm();
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            cellSize = Mathf.Max(0.25f, cellSize);
            tilePixels = Mathf.Clamp(tilePixels, 8, 32);
            activeSeed = Mathf.Max(1, requestedSeed);
            seed = activeSeed;
            cells = new CityMapCell[width, height];
            blockedWalkCounts = new int[width, height];
            blockedBuildCounts = new int[width, height];
            bridgeWalkableCells = new bool[width, height];
            WalkabilityVersion++;
            IsGenerating = true;
            IsGenerated = false;
            GenerationFailed = false;
            GenerationProgress = 0f;
            generationStageKey = "map.generation.starting";
            generationCancellation?.Cancel();
            generationCancellation = new CancellationTokenSource();
        }

        private bool ShouldYield(Stopwatch frameTimer)
        {
            return frameTimer.Elapsed.TotalMilliseconds >= generationFrameBudgetMs;
        }
    }
}
