using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyTemperatureSnapshot
    {
        public StrategyTemperatureSnapshot(float celsius)
        {
            Celsius = celsius;
        }

        public float Celsius { get; }
        public int RoundedCelsius => Mathf.RoundToInt(Celsius);
        public string CelsiusText => StrategyTemperatureModel.FormatCelsius(Celsius);
    }

    public static class StrategyTemperatureModel
    {
        public static StrategyTemperatureSnapshot Evaluate(
            StrategyCalendarSnapshot snapshot,
            StrategyWeatherController weather = null)
        {
            GetSeasonTemperatureProfile(
                snapshot.Season,
                out float seasonalMean,
                out float diurnalSwing,
                out float dailyVariance);

            float dailyOffset = Mathf.Lerp(
                -dailyVariance,
                dailyVariance,
                Stable01(snapshot.DayIndex, 0x45A31));
            float hour = snapshot.Hour + snapshot.Minute / 60f;
            float dayCurve = Mathf.Sin((hour - 9f) / 24f * Mathf.PI * 2f);
            float temperature = seasonalMean + dailyOffset + dayCurve * diurnalSwing;
            temperature += GetWeatherOffset(temperature, weather);

            return new StrategyTemperatureSnapshot(Mathf.Round(temperature * 10f) / 10f);
        }

        public static string FormatCelsius(float celsius)
        {
            int rounded = Mathf.RoundToInt(celsius);
            return rounded > 0 ? "+" + rounded + "C" : rounded + "C";
        }

        public static Color GetTemperatureColor(float celsius)
        {
            if (celsius <= -8f)
            {
                return new Color(0.58f, 0.78f, 1f);
            }

            if (celsius <= 2f)
            {
                return new Color(0.70f, 0.90f, 1f);
            }

            if (celsius >= 24f)
            {
                return new Color(1f, 0.72f, 0.44f);
            }

            if (celsius >= 16f)
            {
                return new Color(1f, 0.90f, 0.58f);
            }

            return new Color(0.84f, 0.92f, 0.84f);
        }

        private static float GetWeatherOffset(float currentCelsius, StrategyWeatherController weather)
        {
            if (weather == null)
            {
                return 0f;
            }

            float offset = -weather.CloudIntensity * 0.6f
                - weather.RainIntensity * 1.8f
                - weather.HeavyRainIntensity * 1.0f
                - weather.FogIntensity * 0.8f
                - weather.StormIntensity * 2.4f
                - weather.SnowIntensity * 1.8f
                - weather.HeavySnowIntensity * 3.2f;

            if (weather.CurrentWeather == StrategyWeatherKind.Snow)
            {
                offset += Mathf.Min(0f, 1.5f - currentCelsius);
            }
            else if (weather.CurrentWeather == StrategyWeatherKind.Blizzard)
            {
                offset += Mathf.Min(0f, -3f - currentCelsius);
            }

            return offset;
        }

        private static void GetSeasonTemperatureProfile(
            StrategySeason season,
            out float seasonalMean,
            out float diurnalSwing,
            out float dailyVariance)
        {
            switch (season)
            {
                case StrategySeason.Spring:
                    seasonalMean = 11.5f;
                    diurnalSwing = 4.5f;
                    dailyVariance = 2.5f;
                    break;
                case StrategySeason.Autumn:
                    seasonalMean = 8.5f;
                    diurnalSwing = 4.0f;
                    dailyVariance = 2.5f;
                    break;
                case StrategySeason.Winter:
                    seasonalMean = -4.5f;
                    diurnalSwing = 3.0f;
                    dailyVariance = 2.3f;
                    break;
                default:
                    seasonalMean = 22f;
                    diurnalSwing = 4.5f;
                    dailyVariance = 2.0f;
                    break;
            }
        }

        private static float Stable01(int dayIndex, int salt)
        {
            unchecked
            {
                uint n = (uint)(Mathf.Max(0, dayIndex) * 747796405) ^ (uint)salt;
                n = (n ^ (n >> 15)) * 2891336453u;
                n ^= n >> 16;
                return (n & 1023u) / 1023f;
            }
        }
    }
}
