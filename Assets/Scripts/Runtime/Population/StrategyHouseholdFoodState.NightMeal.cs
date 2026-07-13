using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyHouseholdFoodState
    {
        private const float NightMealFallbackSeconds = 42f;

        private int nightMealDayIndex = -1;
        private float nightMealWaitSeconds;
        private int nightMealPresentResidentCount;
        private int nightMealExpectedResidentCount;
        private bool nightMealWaitingLogged;

        public bool IsNightMealWaiting => nightMealDayIndex == StrategyDayNightCycleController.CurrentDayIndex
            && IsNightMealTime()
            && CanResolveNightMealForDay(nightMealDayIndex);
        public int NightMealPresentResidentCount => nightMealPresentResidentCount;
        public int NightMealExpectedResidentCount => nightMealExpectedResidentCount;
        public float NightMealFallbackSecondsRemaining => IsNightMealWaiting
            ? Mathf.Max(0f, NightMealFallbackSeconds - nightMealWaitSeconds)
            : 0f;

        public void NotifyResidentEnteredHomeForNight(StrategyResidentAgent resident)
        {
            if (resident == null || house == null || resident.Home != house)
            {
                return;
            }

            TryResolveNightMealFromResidentArrival();
        }

        private void UpdateNightMeal()
        {
            int currentDay = StrategyDayNightCycleController.CurrentDayIndex;
            if (!CanResolveNightMealForDay(currentDay))
            {
                ResetNightMealWait();
                return;
            }

            if (!IsNightMealTime())
            {
                ResetNightMealWait();
                return;
            }

            EnsureNightMealWaitStarted(currentDay);
            nightMealWaitSeconds += Time.deltaTime;
            if (RefreshNightMealPresenceCounts()
                || nightMealWaitSeconds >= NightMealFallbackSeconds)
            {
                ResolveNightMeal(currentDay, nightMealWaitSeconds >= NightMealFallbackSeconds);
            }
        }

        private void TryResolveNightMealFromResidentArrival()
        {
            int currentDay = StrategyDayNightCycleController.CurrentDayIndex;
            if (!CanResolveNightMealForDay(currentDay) || !IsNightMealTime())
            {
                return;
            }

            EnsureNightMealWaitStarted(currentDay);
            if (RefreshNightMealPresenceCounts())
            {
                ResolveNightMeal(currentDay, false);
            }
        }

        private bool CanResolveNightMealForDay(int dayIndex)
        {
            return dayIndex >= configuredDayIndex + SettlingGraceDays
                && lastResolvedDayIndex != dayIndex;
        }

        private static bool IsNightMealTime()
        {
            return StrategyDayNightCycleController.CurrentCalendarSnapshot.Phase
                == StrategyTimeOfDayPhase.Night;
        }

        private void EnsureNightMealWaitStarted(int dayIndex)
        {
            if (nightMealDayIndex == dayIndex)
            {
                return;
            }

            nightMealDayIndex = dayIndex;
            nightMealWaitSeconds = 0f;
            nightMealWaitingLogged = false;
            RefreshNightMealPresenceCounts();
            LogNightMealWaiting(dayIndex);
        }

        private bool RefreshNightMealPresenceCounts()
        {
            nightMealPresentResidentCount = 0;
            nightMealExpectedResidentCount = 0;
            if (house == null)
            {
                return true;
            }

            System.Collections.Generic.IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || resident.Home != house || resident.IsPendingRefugee)
                {
                    continue;
                }

                nightMealExpectedResidentCount++;
                if (IsResidentPresentForNightMeal(resident))
                {
                    nightMealPresentResidentCount++;
                }
            }

            return nightMealPresentResidentCount >= nightMealExpectedResidentCount;
        }

        private static bool IsResidentPresentForNightMeal(StrategyResidentAgent resident)
        {
            return resident.IsSleepingInsideHome
                || resident.IsHomeboundYoungChild
                || resident.Activity == StrategyResidentAgent.ResidentActivity.StayingInsideHome;
        }

        private void ResolveNightMeal(int dayIndex, bool forced)
        {
            if (forced)
            {
                StrategyDebugLogger.Warn(
                    "Food",
                    "HouseholdDinnerForcedByDeadline",
                    StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("day", dayIndex),
                    StrategyDebugLogger.F("present", nightMealPresentResidentCount),
                    StrategyDebugLogger.F("expected", nightMealExpectedResidentCount),
                    StrategyDebugLogger.F("missingResidents", DescribeMissingNightMealResidents()),
                    StrategyDebugLogger.F("waitSeconds", nightMealWaitSeconds));
            }
            else
            {
                StrategyDebugLogger.Info(
                    "Food",
                    "HouseholdDinnerServed",
                    StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("day", dayIndex),
                    StrategyDebugLogger.F("present", nightMealPresentResidentCount),
                    StrategyDebugLogger.F("expected", nightMealExpectedResidentCount));
            }

            ResolveDailyRation(dayIndex);
            ResetNightMealWait();
        }

        private void LogNightMealWaiting(int dayIndex)
        {
            if (nightMealWaitingLogged)
            {
                return;
            }

            nightMealWaitingLogged = true;
            StrategyDebugLogger.Info(
                "Food",
                "HouseholdDinnerWaiting",
                StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("day", dayIndex),
                StrategyDebugLogger.F("present", nightMealPresentResidentCount),
                StrategyDebugLogger.F("expected", nightMealExpectedResidentCount),
                StrategyDebugLogger.F("fallbackSeconds", NightMealFallbackSeconds));
        }

        private string DescribeMissingNightMealResidents()
        {
            if (house == null)
            {
                return "none";
            }

            string result = string.Empty;
            System.Collections.Generic.IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null
                    || resident.Home != house
                    || resident.IsPendingRefugee
                    || IsResidentPresentForNightMeal(resident))
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result += ",";
                }

                result += resident.FullName + ":" + resident.Activity;
            }

            return result.Length > 0 ? result : "none";
        }

        private void ResetNightMealWait()
        {
            nightMealDayIndex = -1;
            nightMealWaitSeconds = 0f;
            nightMealPresentResidentCount = 0;
            nightMealExpectedResidentCount = 0;
            nightMealWaitingLogged = false;
        }

        private static float GetSecondsUntilNightMeal(int targetDay)
        {
            int currentDay = StrategyDayNightCycleController.CurrentDayIndex;
            float currentPhase = StrategyDayNightCycleController.CurrentDayPhase;
            float target = targetDay + StrategyDayNightCycleController.NightStartPhase;
            float current = currentDay + currentPhase;
            if (target < current)
            {
                target += 1f;
            }

            return Mathf.Max(0f, (target - current) * StrategyDayNightCycleController.DayLengthSeconds);
        }
    }
}
