using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyHouseWarmthLevel
    {
        Warm,
        Cooling,
        Cold,
        Freezing
    }

    [DisallowMultipleComponent]
    public sealed class StrategyHouseWarmthState : MonoBehaviour
    {
        public const int WinterNightlyLogNeed = 1;
        public const int WinterHouseLogReserveTarget = 3;

        private const float WarmTargetCelsius = 17f;
        private const float EmptyHouseDriftCelsiusPerSecond = 0.16f;
        private const float HeatedWarmupCelsiusPerSecond = 1.15f;
        private const float PassiveDriftCelsiusPerSecond = 0.18f;
        private const float TickSeconds = 1f;

        private StrategyPlacedBuilding house;
        private float indoorCelsius = 15f;
        private float tickTimer;
        private int lastFuelAttemptDayIndex = -1;
        private int lastFuelConsumedDayIndex = -1;

        public float IndoorCelsius => indoorCelsius;
        public int RoundedIndoorCelsius => Mathf.RoundToInt(indoorCelsius);
        public StrategyHouseWarmthLevel WarmthLevel => EvaluateWarmthLevel(indoorCelsius);
        public bool IsOccupied => house != null && house.ResidentCount > 0;
        public bool HasHeatedTonight => lastFuelConsumedDayIndex == StrategyDayNightCycleController.CurrentDayIndex;
        public int LogsStored => house != null && house.Resources != null ? house.Resources.GetLogsAmount() : 0;
        public int CurrentWinterLogDemand => GetWinterLogDemandForHouse(house);
        public string StatusText => GetWarmthLabel(WarmthLevel) + " " + StrategyTemperatureModel.FormatCelsius(indoorCelsius);

        public void Configure(StrategyPlacedBuilding placedHouse)
        {
            house = placedHouse;
            StrategyTemperatureSnapshot outdoor = StrategyTemperatureModel.Evaluate(
                StrategyDayNightCycleController.CurrentCalendarSnapshot,
                StrategyWeatherController.Active);
            indoorCelsius = Mathf.Max(12f, outdoor.Celsius + 8f);
            tickTimer = Random.Range(0f, TickSeconds);
        }

        private void Update()
        {
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f)
            {
                return;
            }

            float dt = TickSeconds - tickTimer;
            tickTimer = TickSeconds;
            TickWarmth(Mathf.Clamp(dt, 0.01f, 2.5f));
        }

        private void TickWarmth(float dt)
        {
            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            StrategyTemperatureSnapshot outdoor = StrategyTemperatureModel.Evaluate(calendar, StrategyWeatherController.Active);
            if (house == null || house.Tool != StrategyBuildTool.House)
            {
                indoorCelsius = Mathf.MoveTowards(
                    indoorCelsius,
                    outdoor.Celsius,
                    EmptyHouseDriftCelsiusPerSecond * dt);
                return;
            }

            bool occupied = IsOccupied;
            if (occupied && calendar.Season == StrategySeason.Winter && calendar.Phase == StrategyTimeOfDayPhase.Night)
            {
                TryConsumeNightFuel(calendar);
            }

            float target = GetTargetIndoorCelsius(calendar, outdoor.Celsius, occupied);
            float speed = target > indoorCelsius
                ? HeatedWarmupCelsiusPerSecond
                : PassiveDriftCelsiusPerSecond;
            indoorCelsius = Mathf.MoveTowards(indoorCelsius, target, speed * dt);
        }

        private float GetTargetIndoorCelsius(
            StrategyCalendarSnapshot calendar,
            float outdoorCelsius,
            bool occupied)
        {
            if (!occupied)
            {
                return outdoorCelsius;
            }

            if (calendar.Season == StrategySeason.Winter
                && calendar.Phase == StrategyTimeOfDayPhase.Night
                && lastFuelConsumedDayIndex == calendar.DayIndex)
            {
                return WarmTargetCelsius;
            }

            float passiveShelter = Mathf.Max(outdoorCelsius + 5f, Mathf.Lerp(outdoorCelsius, 8f, 0.55f));
            return Mathf.Clamp(passiveShelter, outdoorCelsius, 12f);
        }

        private void TryConsumeNightFuel(StrategyCalendarSnapshot calendar)
        {
            if (lastFuelAttemptDayIndex == calendar.DayIndex)
            {
                return;
            }

            lastFuelAttemptDayIndex = calendar.DayIndex;
            int consumed = house.Resources != null
                ? house.Resources.ConsumeResource(StrategyResourceType.Logs, WinterNightlyLogNeed)
                : 0;
            if (consumed >= WinterNightlyLogNeed)
            {
                lastFuelConsumedDayIndex = calendar.DayIndex;
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseFuelConsumed",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("logs", consumed),
                    StrategyDebugLogger.F("remainingLogs", LogsStored),
                    StrategyDebugLogger.F("indoorCelsius", indoorCelsius));
                return;
            }

            StrategyDebugLogger.Warn(
                "Household",
                "HouseFuelMissing",
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("requiredLogs", WinterNightlyLogNeed),
                StrategyDebugLogger.F("storedLogs", LogsStored),
                StrategyDebugLogger.F("outdoorCelsius", StrategyTemperatureModel.FormatCelsius(
                    StrategyTemperatureModel.Evaluate(calendar, StrategyWeatherController.Active).Celsius)));
        }

        public static int GetWinterLogDemandForHouse(StrategyPlacedBuilding targetHouse)
        {
            if (targetHouse == null
                || targetHouse.Tool != StrategyBuildTool.House
                || targetHouse.ResidentCount <= 0
                || StrategyDayNightCycleController.CurrentCalendarSnapshot.Season != StrategySeason.Winter)
            {
                return 0;
            }

            int stored = targetHouse.Resources != null ? targetHouse.Resources.GetLogsAmount() : 0;
            return Mathf.Max(0, WinterHouseLogReserveTarget - stored);
        }

        public static StrategyHouseWarmthLevel EvaluateWarmthLevel(float celsius)
        {
            if (celsius >= 13f)
            {
                return StrategyHouseWarmthLevel.Warm;
            }

            if (celsius >= 7f)
            {
                return StrategyHouseWarmthLevel.Cooling;
            }

            if (celsius >= 1f)
            {
                return StrategyHouseWarmthLevel.Cold;
            }

            return StrategyHouseWarmthLevel.Freezing;
        }

        public static string GetWarmthLabel(StrategyHouseWarmthLevel level)
        {
            return level switch
            {
                StrategyHouseWarmthLevel.Warm => "Warm",
                StrategyHouseWarmthLevel.Cooling => "Cooling",
                StrategyHouseWarmthLevel.Cold => "Cold",
                StrategyHouseWarmthLevel.Freezing => "Freezing",
                _ => "Unknown"
            };
        }
    }
}
