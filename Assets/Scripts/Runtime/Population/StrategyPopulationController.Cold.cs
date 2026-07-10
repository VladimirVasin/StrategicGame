using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private int lastHomelessColdResolutionDayIndex = -1;

        public void UpdateHomelessColdExposure(StrategyCalendarSnapshot calendar)
        {
            if (calendar.Phase != StrategyTimeOfDayPhase.Dawn
                || calendar.DayIndex == lastHomelessColdResolutionDayIndex)
            {
                return;
            }

            lastHomelessColdResolutionDayIndex = calendar.DayIndex;
            StrategyTemperatureSnapshot temperature = StrategyTemperatureModel.Evaluate(calendar, StrategyWeatherController.Active);
            float overnightMinimum = calendar.Season == StrategySeason.Winter
                ? temperature.Celsius - 3f
                : 12f;
            for (int i = residents.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.Home == null && !resident.IsPendingRefugee)
                {
                    resident.ApplyOvernightCold(overnightMinimum, calendar.DayIndex);
                }
            }
        }

        public bool TryKillResidentFromCold(
            StrategyResidentAgent resident,
            float minimumCelsius,
            float exposure,
            float mortalityChance)
        {
            bool killed = HandleResidentDeath(
                resident,
                "cold_exposure",
                mortalityChance,
                0f,
                1f,
                resident != null ? resident.NutritionSeverityLevel : 0,
                resident != null && resident.Home != null ? resident.Home.Origin : Vector2Int.zero);
            if (killed)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "ResidentDiedFromCold",
                    StrategyDebugLogger.F("minimumCelsius", minimumCelsius),
                    StrategyDebugLogger.F("exposure", exposure),
                    StrategyDebugLogger.F("chance", mortalityChance));
            }

            return killed;
        }
    }
}
