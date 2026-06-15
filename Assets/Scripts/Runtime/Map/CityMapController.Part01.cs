using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class CityMapController
    {

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
