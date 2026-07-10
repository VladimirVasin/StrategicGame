using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyHouseWarmthState
    {
        private readonly List<StrategyResidentAgent> coldResolutionResidents = new();
        private int lastColdResolutionDayIndex = -1;
        private int trackedWinterNightDayIndex = -1;
        private float minimumWinterNightCelsius = 20f;

        private void ConfigureColdConsequences()
        {
            lastColdResolutionDayIndex = StrategyDayNightCycleController.CurrentDayIndex;
            trackedWinterNightDayIndex = -1;
            minimumWinterNightCelsius = indoorCelsius;
        }

        private void UpdateColdConsequences(StrategyCalendarSnapshot calendar)
        {
            if (calendar.Season == StrategySeason.Winter && calendar.Phase == StrategyTimeOfDayPhase.Night)
            {
                if (trackedWinterNightDayIndex != calendar.DayIndex)
                {
                    trackedWinterNightDayIndex = calendar.DayIndex;
                    minimumWinterNightCelsius = indoorCelsius;
                }

                minimumWinterNightCelsius = Mathf.Min(minimumWinterNightCelsius, indoorCelsius);
                return;
            }

            if (calendar.Phase != StrategyTimeOfDayPhase.Dawn
                || calendar.DayIndex == lastColdResolutionDayIndex)
            {
                return;
            }

            lastColdResolutionDayIndex = calendar.DayIndex;
            float resolvedTemperature = trackedWinterNightDayIndex >= 0
                ? minimumWinterNightCelsius
                : 12f;
            trackedWinterNightDayIndex = -1;
            minimumWinterNightCelsius = indoorCelsius;
            if (house == null)
            {
                return;
            }

            coldResolutionResidents.Clear();
            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                if (residents[i] != null && residents[i].Home == house)
                {
                    coldResolutionResidents.Add(residents[i]);
                }
            }

            for (int i = coldResolutionResidents.Count - 1; i >= 0; i--)
            {
                coldResolutionResidents[i]?.ApplyOvernightCold(resolvedTemperature, calendar.DayIndex);
            }
        }
    }
}
