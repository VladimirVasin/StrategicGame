using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyTerrainTexturePainter
    {
        private const float HillReliefThreshold = 0.48f;
        private const float MountainReliefThreshold = 0.72f;

        private static Color ApplyReliefShading(
            Color color,
            in ReliefPaintContext context,
            int cellX,
            int cellY,
            int px,
            int py,
            int tilePixels,
            int seed)
        {
            if (!context.IsEnabled)
            {
                return color;
            }

            float pxn = tilePixels <= 1 ? 0f : px / (float)(tilePixels - 1);
            float pyn = tilePixels <= 1 ? 0f : py / (float)(tilePixels - 1);
            color = Shift(color, context.Light + context.Hill * 0.035f);
            color = ApplyReliefBodyTint(color, context.Height, context.Hill, context.Mountain);
            color = ApplyHillFacePattern(
                color,
                context.Height,
                cellX,
                cellY,
                px,
                py,
                seed,
                context.Hill,
                pxn,
                pyn);
            color = ApplySlopeEdge(
                color,
                context.DropEast,
                context.DropSouth,
                context.RiseWest,
                context.RiseNorth,
                pxn,
                pyn);
            color = ApplyContourBands(
                color,
                context.Height,
                cellX,
                cellY,
                px,
                py,
                tilePixels,
                seed,
                context.Hill);
            color = ApplyMountainRock(
                color,
                context.Height,
                cellX,
                cellY,
                px,
                py,
                seed,
                context.Mountain);
            return color;
        }

        private static Color ApplyReliefBodyTint(Color color, float height, float hill, float mountain)
        {
            if (height > 0.22f)
            {
                color = Color.Lerp(color, Rgb(92, 132, 66), Mathf.Clamp01(hill * 0.10f));
            }

            if (height > MountainReliefThreshold)
            {
                color = Color.Lerp(color, Rgb(111, 111, 88), Mathf.Clamp01(mountain * 0.35f));
            }

            return color;
        }

        private static Color ApplyHillFacePattern(
            Color color,
            float height,
            int cellX,
            int cellY,
            int px,
            int py,
            int seed,
            float hill,
            float pxn,
            float pyn)
        {
            if (hill <= 0.08f)
            {
                return color;
            }

            float roll = Hash01(seed, cellX, cellY, px, py, 67);
            float shoulder = SmoothStep01(0.54f, 1f, pyn) * (1f - SmoothStep01(0.0f, 0.26f, pxn));
            float foot = (1f - SmoothStep01(0.0f, 0.38f, pyn)) * SmoothStep01(0.46f, 1f, pxn);
            color = Color.Lerp(color, Rgb(150, 170, 94), shoulder * hill * 0.16f);
            color = Color.Lerp(color, Color.black, foot * hill * 0.18f);

            bool ridgePixel = Mathf.Abs((px + py * 2 + cellX * 3 + cellY) % 19) <= 1 && roll > 0.36f;
            if (ridgePixel && height > 0.36f)
            {
                color = Color.Lerp(color, Rgb(73, 93, 58), 0.20f + hill * 0.14f);
            }

            return color;
        }

        private static Color ApplySlopeEdge(
            Color color,
            float dropEast,
            float dropSouth,
            float riseWest,
            float riseNorth,
            float pxn,
            float pyn)
        {
            float eastShadow = SmoothStep01(0.56f, 1f, pxn) * dropEast;
            float southShadow = (1f - SmoothStep01(0f, 0.42f, pyn)) * dropSouth;
            float westLight = (1f - SmoothStep01(0f, 0.42f, pxn)) * riseWest;
            float northLight = SmoothStep01(0.58f, 1f, pyn) * riseNorth;
            color = Color.Lerp(color, Color.black, Mathf.Clamp01((eastShadow + southShadow) * 0.30f));
            color = Color.Lerp(color, Rgb(184, 194, 122), Mathf.Clamp01((westLight + northLight) * 0.20f));
            return color;
        }

        private static Color ApplyContourBands(
            Color color,
            float height,
            int cellX,
            int cellY,
            int px,
            int py,
            int tilePixels,
            int seed,
            float hill)
        {
            if (hill <= 0.03f)
            {
                return color;
            }

            float diagonal = (px * 0.62f + py * 0.42f) / Mathf.Max(1f, tilePixels);
            float jitter = Hash01(seed, cellX, cellY, px, py, 71) * 0.16f;
            float band = Mathf.Abs(Mathf.Repeat(height * 8.8f + diagonal + jitter, 1f) - 0.5f);
            if (band < 0.055f)
            {
                color = Color.Lerp(color, Rgb(45, 72, 40), 0.12f + hill * 0.16f);
            }

            return color;
        }

        private static Color ApplyMountainRock(
            Color color,
            float height,
            int cellX,
            int cellY,
            int px,
            int py,
            int seed,
            float mountain)
        {
            if (mountain <= 0.01f)
            {
                return color;
            }

            float stoneNoise = Hash01(seed, cellX, cellY, px, py, 83);
            color = Color.Lerp(color, Rgb(100, 107, 82), mountain * 0.38f);
            bool vein = Mathf.Abs((px - py + (cellX + cellY) % 7) % 9) <= 1 && stoneNoise > 0.34f;
            bool chip = stoneNoise > 0.88f || stoneNoise < 0.075f;
            if (vein)
            {
                color = Color.Lerp(color, Rgb(48, 52, 48), 0.34f + mountain * 0.26f);
            }
            else if (chip)
            {
                Color fleck = stoneNoise > 0.5f ? Rgb(151, 150, 121) : Rgb(63, 68, 61);
                color = Color.Lerp(color, fleck, 0.40f + mountain * 0.24f);
            }

            if (height > 0.84f && (px + py + cellX) % 13 == 0)
            {
                color = Color.Lerp(color, Rgb(196, 196, 166), 0.38f);
            }

            return color;
        }

        private static ReliefPaintContext CreateReliefContext(
            CityMapCell[,] cells,
            CityMapCell cell,
            int cellX,
            int cellY,
            int mapWidth,
            int mapHeight)
        {
            if (cell.IsWater)
            {
                return default;
            }

            float height = cell.ReliefHeight;
            if (height < 0.12f)
            {
                return default;
            }

            float west = GetRelief(cells, cellX - 1, cellY, mapWidth, mapHeight, height);
            float east = GetRelief(cells, cellX + 1, cellY, mapWidth, mapHeight, height);
            float north = GetRelief(cells, cellX, cellY + 1, mapWidth, mapHeight, height);
            float south = GetRelief(cells, cellX, cellY - 1, mapWidth, mapHeight, height);
            float hill = SmoothStep01(0.24f, HillReliefThreshold, height);
            float mountain = SmoothStep01(MountainReliefThreshold - 0.10f, 0.92f, height);
            float light = (west - east) * 0.18f + (north - south) * 0.14f;
            return new ReliefPaintContext(
                height,
                hill,
                mountain,
                light,
                Mathf.Clamp01((height - east) * 4.6f),
                Mathf.Clamp01((height - south) * 4.6f),
                Mathf.Clamp01((height - west) * 3.4f),
                Mathf.Clamp01((height - north) * 3.4f));
        }

        private static float GetRelief(
            CityMapCell[,] cells,
            int x,
            int y,
            int mapWidth,
            int mapHeight,
            float fallback)
        {
            if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight)
            {
                return fallback;
            }

            CityMapCell cell = cells[x, y];
            return cell.IsWater ? 0.02f : cell.ReliefHeight;
        }

        private readonly struct ReliefPaintContext
        {
            public ReliefPaintContext(
                float height,
                float hill,
                float mountain,
                float light,
                float dropEast,
                float dropSouth,
                float riseWest,
                float riseNorth)
            {
                Height = height;
                Hill = hill;
                Mountain = mountain;
                Light = light;
                DropEast = dropEast;
                DropSouth = dropSouth;
                RiseWest = riseWest;
                RiseNorth = riseNorth;
                IsEnabled = true;
            }

            public float Height { get; }
            public float Hill { get; }
            public float Mountain { get; }
            public float Light { get; }
            public float DropEast { get; }
            public float DropSouth { get; }
            public float RiseWest { get; }
            public float RiseNorth { get; }
            public bool IsEnabled { get; }
        }

        private static float SmoothStep01(float min, float max, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
        }
    }
}
