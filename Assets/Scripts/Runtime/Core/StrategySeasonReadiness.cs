using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategySeasonReadinessSnapshot
    {
        public StrategySeasonReadinessSnapshot(
            int daysUntilWinter,
            int winterDaysToCover,
            float availableFoodRations,
            float dailyRationNeed,
            float foodDays,
            int availableFuelLogs,
            int occupiedHouseCount,
            int dailyLogNeed,
            float fuelDays)
        {
            DaysUntilWinter = daysUntilWinter;
            WinterDaysToCover = winterDaysToCover;
            AvailableFoodRations = availableFoodRations;
            DailyRationNeed = dailyRationNeed;
            FoodDays = foodDays;
            AvailableFuelLogs = availableFuelLogs;
            OccupiedHouseCount = occupiedHouseCount;
            DailyLogNeed = dailyLogNeed;
            FuelDays = fuelDays;
        }

        public int DaysUntilWinter { get; }
        public int WinterDaysToCover { get; }
        public float AvailableFoodRations { get; }
        public float DailyRationNeed { get; }
        public float FoodDays { get; }
        public int AvailableFuelLogs { get; }
        public int OccupiedHouseCount { get; }
        public int DailyLogNeed { get; }
        public float FuelDays { get; }
        public bool HasPopulationNeed => DailyRationNeed > 0.01f;
        public bool HasFuelNeed => DailyLogNeed > 0;
        public bool CoversFood => !HasPopulationNeed || FoodDays + 0.01f >= WinterDaysToCover;
        public bool CoversFuel => !HasFuelNeed || FuelDays + 0.01f >= WinterDaysToCover;
        public bool CoversWinter => CoversFood && CoversFuel;
    }

    public static class StrategySeasonReadiness
    {
        public static StrategySeasonReadinessSnapshot Evaluate(StrategyCalendarSnapshot snapshot, StrategyPopulationController population)
        {
            float dailyNeed = population != null ? population.GetTotalDailyRationNeed() : 0f;
            float storedRations = StrategyResourceQueryService.GetFoodRations(
                StrategyResourceStoreScope.Settlement
                | StrategyResourceStoreScope.TemporarySettlement
                | StrategyResourceStoreScope.Household);
            float foodDays = dailyNeed > 0.01f ? storedRations / dailyNeed : 0f;
            int occupiedHouses = population != null ? population.GetOccupiedHouseCount() : 0;
            int dailyLogNeed = occupiedHouses * StrategyHouseWarmthState.WinterNightlyLogNeed;
            int storedLogs = StrategyResourceQueryService.GetAvailable(StrategyResourceType.Logs);
            float fuelDays = dailyLogNeed > 0 ? storedLogs / (float)dailyLogNeed : 0f;
            int daysUntilWinter = StrategySeasonCalendar.GetDaysUntilSeason(snapshot.DayIndex, StrategySeason.Winter);
            int winterDaysToCover = snapshot.Season == StrategySeason.Winter
                ? StrategySeasonCalendar.GetRemainingSeasonDays(snapshot.DayIndex)
                : StrategySeasonCalendar.DaysPerSeason;
            return new StrategySeasonReadinessSnapshot(
                daysUntilWinter,
                Mathf.Max(1, winterDaysToCover),
                storedRations,
                dailyNeed,
                foodDays,
                storedLogs,
                occupiedHouses,
                dailyLogNeed,
                fuelDays);
        }
    }
}
