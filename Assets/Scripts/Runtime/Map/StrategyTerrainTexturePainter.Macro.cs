using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyTerrainTexturePainter
    {
        private static float GetMacroStrength(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Water => 0.035f,
                CityMapCellKind.Shore => 0.050f,
                CityMapCellKind.Forest => 0.060f,
                CityMapCellKind.Dirt => 0.055f,
                _ => 0.045f
            };
        }

        private static float SampleMacroVariation(
            int seed,
            int cellX,
            int cellY,
            CityMapCellKind kind)
        {
            const int scale = 8;
            int gridX = cellX / scale;
            int gridY = cellY / scale;
            float tx = SmoothMacro((cellX % scale) / (float)scale);
            float ty = SmoothMacro((cellY % scale) / (float)scale);
            int kindSalt = (int)kind * 17 + 97;
            float bottomLeft = Hash01(seed, gridX, gridY, kindSalt, 0, 101);
            float bottomRight = Hash01(seed, gridX + 1, gridY, kindSalt, 0, 101);
            float topLeft = Hash01(seed, gridX, gridY + 1, kindSalt, 0, 101);
            float topRight = Hash01(seed, gridX + 1, gridY + 1, kindSalt, 0, 101);
            float bottom = Mathf.Lerp(bottomLeft, bottomRight, tx);
            float top = Mathf.Lerp(topLeft, topRight, tx);
            return Mathf.Lerp(bottom, top, ty);
        }

        private static float SmoothMacro(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
    }
}
