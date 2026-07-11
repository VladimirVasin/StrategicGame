using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySeasonalSurfaceController
    {
        private void PaintSnowCell(int cellX, int cellY, CityMapCell cell)
        {
            float strength = GetSnowTerrainStrength(cell.Kind);
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    int worldPixelX = cellX * PixelsPerCell + px;
                    int worldPixelY = cellY * PixelsPerCell + py;
                    float patch = SampleSurfaceNoise(map.ActiveSeed, worldPixelX, worldPixelY, 12, 991);
                    float detail = SampleSurfaceNoise(map.ActiveSeed, worldPixelX, worldPixelY, 5, 1171);
                    float alpha = snowCoverFactor * strength * Mathf.Lerp(0.46f, 0.76f, patch);
                    if (detail > 0.82f && snowCoverFactor < 0.86f)
                    {
                        alpha *= Mathf.Lerp(0.35f, 0.72f, patch);
                    }

                    float tint = Mathf.Lerp(patch, detail, 0.20f);
                    Color color = Color.Lerp(
                        new Color(0.72f, 0.82f, 0.86f, 1f),
                        new Color(0.95f, 0.98f, 0.98f, 1f),
                        tint);
                    color.a = Mathf.Clamp01(alpha);
                    SetSurfacePixel(snowPixels, worldPixelX, worldPixelY, color);
                }
            }
        }

        private void PaintIceCell(int cellX, int cellY, CityMapCell cell)
        {
            bool river = cell.IsRiver;
            float strength = river ? 0.54f : 0.78f;
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    int worldPixelX = cellX * PixelsPerCell + px;
                    int worldPixelY = cellY * PixelsPerCell + py;
                    float patch = SampleSurfaceNoise(map.ActiveSeed, worldPixelX, worldPixelY, 10, 1601);
                    float crack = SampleSurfaceNoise(map.ActiveSeed, worldPixelX, worldPixelY, 3, 1867);
                    float alpha = iceCoverFactor * strength * Mathf.Lerp(0.52f, 0.80f, patch);
                    if ((river && crack > 0.72f) || (!river && crack > 0.90f))
                    {
                        alpha *= river ? 0.42f : 0.58f;
                    }

                    Color color = Color.Lerp(
                        new Color(0.62f, 0.79f, 0.87f, 1f),
                        new Color(0.90f, 0.96f, 0.97f, 1f),
                        patch);
                    color.a = Mathf.Clamp01(alpha);
                    SetSurfacePixel(icePixels, worldPixelX, worldPixelY, color);
                }
            }
        }

        private void SetSurfacePixel(Color[] pixels, int x, int y, Color color)
        {
            pixels[y * map.Width * PixelsPerCell + x] = color;
        }

        private static float GetSnowTerrainStrength(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Meadow => 0.88f,
                CityMapCellKind.Grass => 0.80f,
                CityMapCellKind.Forest => 0.58f,
                CityMapCellKind.Dirt => 0.48f,
                CityMapCellKind.Shore => 0.64f,
                _ => 0.70f
            };
        }

        private static float SampleSurfaceNoise(int seed, int x, int y, int scale, int salt)
        {
            int gridX = x / scale;
            int gridY = y / scale;
            float tx = SmoothSurface((x % scale) / (float)scale);
            float ty = SmoothSurface((y % scale) / (float)scale);
            float bottomLeft = Hash01(seed, gridX, gridY, 0, 0, salt);
            float bottomRight = Hash01(seed, gridX + 1, gridY, 0, 0, salt);
            float topLeft = Hash01(seed, gridX, gridY + 1, 0, 0, salt);
            float topRight = Hash01(seed, gridX + 1, gridY + 1, 0, 0, salt);
            return Mathf.Lerp(
                Mathf.Lerp(bottomLeft, bottomRight, tx),
                Mathf.Lerp(topLeft, topRight, tx),
                ty);
        }

        private static float SmoothSurface(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static float Hash01(int seed, int x, int y, int px, int py, int salt)
        {
            unchecked
            {
                int n = seed;
                n = n * 73856093 ^ x * 19349663 ^ y * 83492791 ^ px * 265443576 ^ py * 1597334677 ^ salt;
                n = (n << 13) ^ n;
                int positive = (n * (n * n * 15731 + 789221) + 1376312589) & int.MaxValue;
                return positive / (float)int.MaxValue;
            }
        }
    }
}
