using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategySeason
    {
        Summer,
        Spring,
        Autumn,
        Winter
    }

    public readonly struct StrategySeasonPostProcessProfile
    {
        public StrategySeasonPostProcessProfile(
            Color tint,
            float tintStrength,
            float exposureOffset,
            float contrastOffset,
            float saturationOffset)
        {
            Tint = tint;
            TintStrength = tintStrength;
            ExposureOffset = exposureOffset;
            ContrastOffset = contrastOffset;
            SaturationOffset = saturationOffset;
        }

        public Color Tint { get; }
        public float TintStrength { get; }
        public float ExposureOffset { get; }
        public float ContrastOffset { get; }
        public float SaturationOffset { get; }
    }

    public readonly struct StrategySeasonGameplayProfile
    {
        public StrategySeasonGameplayProfile(
            float initialForageMultiplier,
            float forageChanceMultiplier,
            float forageRespawnDelayMultiplier,
            float campSupportDelayMultiplier,
            float campSupportTargetMultiplier,
            float berriesWeight,
            float rootsWeight,
            float mushroomsWeight)
        {
            InitialForageMultiplier = initialForageMultiplier;
            ForageChanceMultiplier = forageChanceMultiplier;
            ForageRespawnDelayMultiplier = forageRespawnDelayMultiplier;
            CampSupportDelayMultiplier = campSupportDelayMultiplier;
            CampSupportTargetMultiplier = campSupportTargetMultiplier;
            BerriesWeight = berriesWeight;
            RootsWeight = rootsWeight;
            MushroomsWeight = mushroomsWeight;
        }

        public float InitialForageMultiplier { get; }
        public float ForageChanceMultiplier { get; }
        public float ForageRespawnDelayMultiplier { get; }
        public float CampSupportDelayMultiplier { get; }
        public float CampSupportTargetMultiplier { get; }
        public float BerriesWeight { get; }
        public float RootsWeight { get; }
        public float MushroomsWeight { get; }
    }

    public static class StrategySeasonCalendar
    {
        public const int DaysPerSeason = 7;
        public const int SeasonsPerYear = 4;
        public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;

        private static readonly StrategySeason[] SeasonCycle =
        {
            StrategySeason.Spring,
            StrategySeason.Summer,
            StrategySeason.Autumn,
            StrategySeason.Winter
        };

        public static StrategySeason GetSeason(int dayIndex)
        {
            int seasonIndex = Mathf.FloorToInt(Mathf.Max(0, dayIndex) / (float)DaysPerSeason) % SeasonsPerYear;
            return SeasonCycle[seasonIndex];
        }

        public static int GetSeasonDay(int dayIndex)
        {
            return Mathf.Max(0, dayIndex) % DaysPerSeason + 1;
        }

        public static int GetYear(int dayIndex)
        {
            return Mathf.Max(0, dayIndex) / DaysPerYear + 1;
        }

        public static float GetSeasonProgress(int dayIndex, float dayPhase)
        {
            float seasonDay = Mathf.Max(0, dayIndex) % DaysPerSeason;
            return Mathf.Clamp01((seasonDay + Mathf.Clamp01(dayPhase)) / DaysPerSeason);
        }

        public static int GetDaysUntilSeason(int dayIndex, StrategySeason targetSeason)
        {
            int safeDay = Mathf.Max(0, dayIndex);
            if (GetSeason(safeDay) == targetSeason)
            {
                return 0;
            }

            int currentDayInYear = safeDay % DaysPerYear;
            int targetStart = GetCycleIndex(targetSeason) * DaysPerSeason;
            int daysUntil = targetStart - currentDayInYear;
            return daysUntil > 0 ? daysUntil : daysUntil + DaysPerYear;
        }

        public static int GetRemainingSeasonDays(int dayIndex)
        {
            return DaysPerSeason - GetSeasonDay(dayIndex) + 1;
        }

        public static string GetSeasonLabel(StrategySeason season)
        {
            switch (season)
            {
                case StrategySeason.Spring:
                    return "Spring";
                case StrategySeason.Autumn:
                    return "Autumn";
                case StrategySeason.Winter:
                    return "Winter";
                default:
                    return "Summer";
            }
        }

        public static Color GetSeasonAccentColor(StrategySeason season)
        {
            switch (season)
            {
                case StrategySeason.Spring:
                    return new Color(0.58f, 0.94f, 0.58f);
                case StrategySeason.Autumn:
                    return new Color(1f, 0.62f, 0.32f);
                case StrategySeason.Winter:
                    return new Color(0.72f, 0.88f, 1f);
                default:
                    return new Color(1f, 0.86f, 0.46f);
            }
        }

        public static StrategySeasonPostProcessProfile GetPostProcessProfile(StrategySeason season)
        {
            switch (season)
            {
                case StrategySeason.Spring:
                    return new StrategySeasonPostProcessProfile(new Color(0.82f, 1f, 0.78f, 1f), 0.10f, 0.006f, 0f, 3f);
                case StrategySeason.Autumn:
                    return new StrategySeasonPostProcessProfile(new Color(1f, 0.78f, 0.54f, 1f), 0.14f, -0.004f, 1f, -1f);
                case StrategySeason.Winter:
                    return new StrategySeasonPostProcessProfile(new Color(0.76f, 0.88f, 1f, 1f), 0.18f, -0.012f, -2f, -7f);
                default:
                    return new StrategySeasonPostProcessProfile(new Color(1f, 0.91f, 0.66f, 1f), 0.06f, 0.008f, 1f, 2f);
            }
        }

        public static StrategySeasonGameplayProfile GetGameplayProfile(StrategySeason season)
        {
            switch (season)
            {
                case StrategySeason.Spring:
                    return new StrategySeasonGameplayProfile(1.05f, 1.15f, 0.86f, 0.88f, 1.12f, 1.00f, 1.08f, 1.20f);
                case StrategySeason.Autumn:
                    return new StrategySeasonGameplayProfile(0.95f, 1.00f, 1.04f, 1.00f, 1.00f, 0.46f, 1.45f, 1.65f);
                case StrategySeason.Winter:
                    return new StrategySeasonGameplayProfile(0.35f, 0.28f, 3.20f, 2.80f, 0.36f, 0.04f, 0.85f, 0.18f);
                default:
                    return new StrategySeasonGameplayProfile(1.15f, 1.18f, 0.76f, 0.78f, 1.18f, 1.45f, 0.82f, 0.82f);
            }
        }

        private static int GetCycleIndex(StrategySeason season)
        {
            return season switch
            {
                StrategySeason.Spring => 0,
                StrategySeason.Summer => 1,
                StrategySeason.Autumn => 2,
                StrategySeason.Winter => 3,
                _ => 0
            };
        }
    }
}
