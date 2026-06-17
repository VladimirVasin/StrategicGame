using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFogOfWarController
    {
        private const float WeatherFogStart = 0.32f;
        private const float WeatherFogFull = 0.72f;
        private const float WeatherFogCampRevealMultiplier = 0.50f;
        private const float WeatherFogResidentRevealMultiplier = 0.38f;
        private const float WeatherFogBuildingRevealMultiplier = 0.45f;
        private const float WeatherFogCampMinimumRadius = 2.8f;
        private const float WeatherFogResidentMinimumRadius = 1.65f;
        private const float WeatherFogBuildingMinimumRadius = 2.20f;
        private const int WeatherFogLightRadiusSqr = 4;
        private const int WeatherFogMediumRadiusSqr = 16;
        private const byte WeatherFogDenseBand = 0;
        private const byte WeatherFogMediumBand = 1;
        private const byte WeatherFogLightBand = 2;
        private const byte WeatherFogClearBand = 3;

        private static readonly Color WeatherFogColor = new Color(0.60f, 0.68f, 0.66f, 1f);

        private void UpdateWeatherFogTuning()
        {
            float rawFog = weather != null ? weather.FogIntensity : 0f;
            weatherFogPressure = Smooth01(Mathf.InverseLerp(WeatherFogStart, WeatherFogFull, rawFog));
        }

        private void BuildWeatherFogBands()
        {
            if (weatherFogPressure <= 0.02f || weatherFogBands == null)
            {
                return;
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!visible[x, y])
                    {
                        continue;
                    }

                    weatherFogBands[x, y] = WeatherFogClearBand;
                    StampWeatherFogBandAroundVisibleCell(x, y);
                }
            }
        }

        private void StampWeatherFogBandAroundVisibleCell(int centerX, int centerY)
        {
            int minX = Mathf.Max(0, centerX - 4);
            int maxX = Mathf.Min(map.Width - 1, centerX + 4);
            int minY = Mathf.Max(0, centerY - 4);
            int maxY = Mathf.Min(map.Height - 1, centerY + 4);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                for (int x = minX; x <= maxX; x++)
                {
                    if (visible[x, y])
                    {
                        weatherFogBands[x, y] = WeatherFogClearBand;
                        continue;
                    }

                    int dx = x - centerX;
                    int distanceSqr = dx * dx + dy * dy;
                    if (distanceSqr > WeatherFogMediumRadiusSqr)
                    {
                        continue;
                    }

                    byte band = distanceSqr <= WeatherFogLightRadiusSqr
                        ? WeatherFogLightBand
                        : WeatherFogMediumBand;
                    if (band > weatherFogBands[x, y])
                    {
                        weatherFogBands[x, y] = band;
                    }
                }
            }
        }

        private Color GetFogCellColor(int x, int y)
        {
            if (!explored[x, y])
            {
                return UnexploredColor;
            }

            float strength = Smooth01(visibilityStrength[x, y]);
            Color normalColor = ExploredColor;
            normalColor.a = exploredAlpha * (1f - strength);
            if (weatherFogPressure <= 0.02f)
            {
                return normalColor;
            }

            if (visible[x, y])
            {
                return Color.clear;
            }

            byte band = weatherFogBands[x, y];
            float targetAlpha = GetWeatherFogBandAlpha(band);
            targetAlpha *= Mathf.Lerp(0.92f, 1.08f, GetWeatherFogCellNoise(x, y));
            Color weatherColor = WeatherFogColor;
            weatherColor.a = Mathf.Lerp(normalColor.a, Mathf.Clamp01(targetAlpha), weatherFogPressure);
            weatherColor = Color.Lerp(normalColor, weatherColor, weatherFogPressure);
            return weatherColor;
        }

        private float GetWeatherFogRevealMultiplier(RevealSourceKind kind)
        {
            switch (kind)
            {
                case RevealSourceKind.Camp:
                    return Mathf.Lerp(1f, WeatherFogCampRevealMultiplier, weatherFogPressure);
                case RevealSourceKind.Resident:
                    return Mathf.Lerp(1f, WeatherFogResidentRevealMultiplier, weatherFogPressure);
                case RevealSourceKind.Building:
                    return Mathf.Lerp(1f, WeatherFogBuildingRevealMultiplier, weatherFogPressure);
                default:
                    return 1f;
            }
        }

        private static float GetWeatherFogMinimumRevealRadius(RevealSourceKind kind)
        {
            switch (kind)
            {
                case RevealSourceKind.Camp:
                    return WeatherFogCampMinimumRadius;
                case RevealSourceKind.Resident:
                    return WeatherFogResidentMinimumRadius;
                case RevealSourceKind.Building:
                    return WeatherFogBuildingMinimumRadius;
                default:
                    return 1f;
            }
        }

        private static float GetWeatherFogBandAlpha(byte band)
        {
            switch (band)
            {
                case WeatherFogClearBand:
                    return 0f;
                case WeatherFogLightBand:
                    return 0.34f;
                case WeatherFogMediumBand:
                    return 0.58f;
                case WeatherFogDenseBand:
                default:
                    return 0.86f;
            }
        }

        private static float GetWeatherFogCellNoise(int x, int y)
        {
            unchecked
            {
                uint n = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 1023u) / 1023f;
            }
        }
    }
}
