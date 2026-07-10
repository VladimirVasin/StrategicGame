using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyFirstYearBalance
    {
        public const int WinterPreparationDays = StrategySeasonCalendar.DaysPerSeason;
        public const float MinimumRefugeeFoodDays = 2f;
        public const float ComfortableRefugeeFoodDays = 4f;

        public static float GetRefugeeSeasonMultiplier(StrategySeason season)
        {
            return season switch
            {
                StrategySeason.Spring => 1.10f,
                StrategySeason.Autumn => 0.72f,
                StrategySeason.Winter => 0.18f,
                _ => 1f
            };
        }

        public static float GetRefugeeHousingMultiplier(int availableHousingSlots)
        {
            if (availableHousingSlots <= 0)
            {
                return 0.22f;
            }

            return availableHousingSlots < 3 ? 0.62f : 1f;
        }

        public static float GetRefugeeFoodMultiplier(float availableFoodDays)
        {
            if (availableFoodDays < MinimumRefugeeFoodDays)
            {
                return 0.20f;
            }

            return availableFoodDays < ComfortableRefugeeFoodDays
                ? Mathf.Lerp(0.45f, 1f, (availableFoodDays - MinimumRefugeeFoodDays)
                    / (ComfortableRefugeeFoodDays - MinimumRefugeeFoodDays))
                : 1f;
        }
    }
}
