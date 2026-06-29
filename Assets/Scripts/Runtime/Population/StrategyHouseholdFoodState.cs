using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyHouseholdFoodStatus
    {
        Settling,
        Fed,
        ShortRations,
        Hungry,
        Starving
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyHouseholdFoodState : MonoBehaviour
    {
        private const int SettlingGraceDays = 1;
        private const int BirthBlockShortageDays = 2;

        private readonly List<StrategyResidentAgent> activeResidents = new();

        private StrategyPopulationController population;
        private StrategyPlacedBuilding house;
        private int configuredDayIndex;
        private int lastResolvedDayIndex = -1;
        private int shortageStreakDays;
        private int hungryResidentCount;
        private int starvingResidentCount;
        private float lastRequiredRations;
        private float lastSuppliedRations;
        private float lastMissingRations;
        private float lastHouseRationsSupplied;
        private float lastIngredientRationsSupplied;
        private float lastGranaryRationsSupplied;
        private int lastResidentCount;
        private int lastConsumedFood;
        private int lastHouseFoodConsumed;
        private int lastIngredientFoodConsumed;
        private int lastGameConsumed;
        private int lastFishConsumed;
        private bool hasResolvedDailyRation;

        public int NutritionSeverityLevel => GetHouseholdNutritionSeverityLevel();
        public int ShortageStreakDays => shortageStreakDays;
        public int HungryResidentCount => hungryResidentCount;
        public int StarvingResidentCount => starvingResidentCount;
        public int LastResidentCount => lastResidentCount;
        public int LastConsumedFood => lastConsumedFood;
        public int LastHouseFoodConsumed => lastHouseFoodConsumed;
        public int LastIngredientFoodConsumed => lastIngredientFoodConsumed;
        public int LastGameConsumed => lastGameConsumed;
        public int LastFishConsumed => lastFishConsumed;
        public float LastRequiredRations => lastRequiredRations;
        public float LastSuppliedRations => lastSuppliedRations;
        public float LastMissingRations => lastMissingRations;
        public float LastHouseRationsSupplied => lastHouseRationsSupplied;
        public float LastIngredientRationsSupplied => lastIngredientRationsSupplied;
        public float LastGranaryRationsSupplied => lastGranaryRationsSupplied;
        public float NextFoodTickSeconds => GetSecondsUntilNextRation();
        public float FoodGraceSecondsRemaining => GetSecondsUntilFirstRation();
        public float FoodGraceDurationSeconds => StrategyDayNightCycleController.DayLengthSeconds * SettlingGraceDays;
        public bool IsFoodSupplyActivated => hasResolvedDailyRation;
        public bool IsStarving => GetHouseholdNutritionSeverityLevel() >= 3;
        public bool IsBirthBlocked => shortageStreakDays >= BirthBlockShortageDays || HasBirthBlockedResident();
        public StrategyHouseholdFoodStatus Status => GetStatus();
        public float MortalityMultiplier => GetMaxResidentMortalityMultiplier();

        public void Configure(StrategyPopulationController populationController, StrategyPlacedBuilding homeBuilding)
        {
            bool houseChanged = house != homeBuilding;
            population = populationController;
            house = homeBuilding;
            if (houseChanged)
            {
                configuredDayIndex = StrategyDayNightCycleController.CurrentDayIndex;
                lastResolvedDayIndex = -1;
                shortageStreakDays = 0;
                hungryResidentCount = 0;
                starvingResidentCount = 0;
                hasResolvedDailyRation = false;
                ClearLastRation();
            }
        }

        private void Update()
        {
            if (population == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            UpdateNightMeal();
        }

        public string GetCompactHudText()
        {
            return "Food: "
                + GetStatusText()
                + " | Need "
                + FormatRation(lastRequiredRations)
                + " | Last "
                + FormatRation(lastSuppliedRations)
                + "/"
                + FormatRation(lastRequiredRations);
        }

        private void ResolveDailyRation(int dayIndex)
        {
            CollectActiveResidents();
            lastResidentCount = activeResidents.Count;
            if (lastResidentCount <= 0)
            {
                ClearLastRation();
                shortageStreakDays = Mathf.Max(0, shortageStreakDays - 1);
                lastResolvedDayIndex = dayIndex;
                hasResolvedDailyRation = true;
                return;
            }

            lastRequiredRations = CalculateRequiredRations();
            int preparedDishes = house.Resources != null ? house.Resources.GetPreparedDishAmount() : 0;
            float preparedDishRations = house.Resources != null ? house.Resources.GetPreparedDishRations() : 0f;
            string preparedDishSummary = house.Resources != null ? house.Resources.GetPreparedDishSummary(3) : string.Empty;
            int ingredientFood = house.Resources != null ? house.Resources.GetTotalIngredientAmount() : 0;
            float ingredientRations = house.Resources != null ? house.Resources.GetTotalIngredientRationValue() : 0f;
            float leftoverRationsBefore = house.Resources != null ? house.Resources.LeftoverRations : 0f;
            int granaryFood = StrategyGranary.GetTotalSettlementFood();
            float granaryFoodRations = StrategyGranary.GetTotalSettlementFoodRations();
            lastHouseRationsSupplied = 0f;
            lastHouseFoodConsumed = house.Resources != null
                ? house.Resources.ConsumePreparedDishes(lastRequiredRations, out lastHouseRationsSupplied)
                : 0;
            float remainingRations = Mathf.Max(0f, lastRequiredRations - lastHouseRationsSupplied);
            lastIngredientRationsSupplied = 0f;
            lastIngredientFoodConsumed = house.Resources != null && remainingRations > 0.01f
                ? house.Resources.ConsumeIngredientRations(remainingRations, out lastIngredientRationsSupplied)
                : 0;
            lastGranaryRationsSupplied = 0f;
            lastGameConsumed = 0;
            lastFishConsumed = 0;

            lastConsumedFood = lastHouseFoodConsumed + lastIngredientFoodConsumed + lastGameConsumed + lastFishConsumed;
            lastSuppliedRations = Mathf.Min(
                lastRequiredRations,
                lastHouseRationsSupplied + lastIngredientRationsSupplied + lastGranaryRationsSupplied);
            lastMissingRations = Mathf.Max(0f, lastRequiredRations - lastSuppliedRations);
            float leftoverRationsAfter = house.Resources != null ? house.Resources.LeftoverRations : 0f;
            ApplyRationToResidents(dayIndex);
            RefreshNutritionCounts();

            int previousShortage = shortageStreakDays;
            if (lastMissingRations <= 0.01f)
            {
                shortageStreakDays = Mathf.Max(0, shortageStreakDays - 1);
                StrategyDebugLogger.Info(
                    "Food",
                    "HouseholdDinnerMet",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("day", dayIndex),
                    StrategyDebugLogger.F("residents", lastResidentCount),
                    StrategyDebugLogger.F("requiredRations", lastRequiredRations),
                    StrategyDebugLogger.F("suppliedRations", lastSuppliedRations),
                    StrategyDebugLogger.F("dishesConsumed", lastHouseFoodConsumed),
                    StrategyDebugLogger.F("dishRations", lastHouseRationsSupplied),
                    StrategyDebugLogger.F("fallbackIngredientsConsumed", lastIngredientFoodConsumed),
                    StrategyDebugLogger.F("fallbackIngredientRations", lastIngredientRationsSupplied),
                    StrategyDebugLogger.F("previousShortageDays", previousShortage),
                    StrategyDebugLogger.F("shortageDays", shortageStreakDays),
                    StrategyDebugLogger.F("dishesBefore", preparedDishes),
                    StrategyDebugLogger.F("dishRationsBefore", preparedDishRations),
                    StrategyDebugLogger.F("dishSummaryBefore", preparedDishSummary),
                    StrategyDebugLogger.F("leftoverRationsBefore", leftoverRationsBefore),
                    StrategyDebugLogger.F("leftoverRationsAfter", leftoverRationsAfter),
                    StrategyDebugLogger.F("ingredientsBefore", ingredientFood),
                    StrategyDebugLogger.F("ingredientRationsBefore", ingredientRations),
                    StrategyDebugLogger.F("granaryFoodBefore", granaryFood),
                    StrategyDebugLogger.F("granaryRationsBefore", granaryFoodRations));
            }
            else
            {
                shortageStreakDays++;
                StrategyDebugLogger.Warn(
                    "Food",
                    "HouseholdDinnerShortRations",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("day", dayIndex),
                    StrategyDebugLogger.F("residents", lastResidentCount),
                    StrategyDebugLogger.F("requiredRations", lastRequiredRations),
                    StrategyDebugLogger.F("suppliedRations", lastSuppliedRations),
                    StrategyDebugLogger.F("missingRations", lastMissingRations),
                    StrategyDebugLogger.F("dishesConsumed", lastHouseFoodConsumed),
                    StrategyDebugLogger.F("dishRations", lastHouseRationsSupplied),
                    StrategyDebugLogger.F("fallbackIngredientsConsumed", lastIngredientFoodConsumed),
                    StrategyDebugLogger.F("fallbackIngredientRations", lastIngredientRationsSupplied),
                    StrategyDebugLogger.F("previousShortageDays", previousShortage),
                    StrategyDebugLogger.F("shortageDays", shortageStreakDays),
                    StrategyDebugLogger.F("hungryResidents", hungryResidentCount),
                    StrategyDebugLogger.F("starvingResidents", starvingResidentCount),
                    StrategyDebugLogger.F("mortalityMultiplier", MortalityMultiplier),
                    StrategyDebugLogger.F("dishesBefore", preparedDishes),
                    StrategyDebugLogger.F("dishRationsBefore", preparedDishRations),
                    StrategyDebugLogger.F("dishSummaryBefore", preparedDishSummary),
                    StrategyDebugLogger.F("leftoverRationsBefore", leftoverRationsBefore),
                    StrategyDebugLogger.F("leftoverRationsAfter", leftoverRationsAfter),
                    StrategyDebugLogger.F("ingredientsBefore", ingredientFood),
                    StrategyDebugLogger.F("ingredientRationsBefore", ingredientRations),
                    StrategyDebugLogger.F("granaryFoodBefore", granaryFood),
                    StrategyDebugLogger.F("granaryRationsBefore", granaryFoodRations));
            }

            lastResolvedDayIndex = dayIndex;
            hasResolvedDailyRation = true;
        }

        private void ApplyRationToResidents(int dayIndex)
        {
            float required = Mathf.Max(0.01f, lastRequiredRations);
            float suppliedShare = Mathf.Clamp01(lastSuppliedRations / required);
            for (int i = 0; i < activeResidents.Count; i++)
            {
                StrategyResidentAgent resident = activeResidents[i];
                if (resident == null)
                {
                    continue;
                }

                float residentNeed = resident.DailyRationNeed;
                resident.ApplyDailyRation(residentNeed, residentNeed * suppliedShare, dayIndex);
            }
        }

        private void CollectActiveResidents()
        {
            activeResidents.Clear();
            if (house == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.Home == house && !resident.IsPendingRefugee)
                {
                    activeResidents.Add(resident);
                }
            }
        }

        private float CalculateRequiredRations()
        {
            float total = 0f;
            for (int i = 0; i < activeResidents.Count; i++)
            {
                StrategyResidentAgent resident = activeResidents[i];
                if (resident != null)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }

        private void RefreshNutritionCounts()
        {
            hungryResidentCount = 0;
            starvingResidentCount = 0;
            if (house == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || resident.Home != house || resident.IsPendingRefugee)
                {
                    continue;
                }

                if (resident.IsHungry)
                {
                    hungryResidentCount++;
                }

                if (resident.IsStarving)
                {
                    starvingResidentCount++;
                }
            }
        }

        private StrategyHouseholdFoodStatus GetStatus()
        {
            RefreshNutritionCounts();
            if (!hasResolvedDailyRation && GetSecondsUntilFirstRation() > 0.01f)
            {
                return StrategyHouseholdFoodStatus.Settling;
            }

            if (starvingResidentCount > 0)
            {
                return StrategyHouseholdFoodStatus.Starving;
            }

            if (hungryResidentCount > 0 || shortageStreakDays >= BirthBlockShortageDays)
            {
                return StrategyHouseholdFoodStatus.Hungry;
            }

            return lastMissingRations > 0.01f
                ? StrategyHouseholdFoodStatus.ShortRations
                : StrategyHouseholdFoodStatus.Fed;
        }

        private bool HasBirthBlockedResident()
        {
            if (house == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.Home == house
                    && resident.IsBirthBlockedByHunger)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetHouseholdNutritionSeverityLevel()
        {
            int maxSeverity = 0;
            if (house != null)
            {
                IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
                for (int i = 0; i < residents.Count; i++)
                {
                    StrategyResidentAgent resident = residents[i];
                    if (resident != null && resident.Home == house)
                    {
                        maxSeverity = Mathf.Max(maxSeverity, resident.NutritionSeverityLevel);
                    }
                }
            }

            return Mathf.Clamp(Mathf.Max(shortageStreakDays, maxSeverity), 0, 5);
        }

        private float GetMaxResidentMortalityMultiplier()
        {
            float multiplier = 1f;
            if (house == null)
            {
                return multiplier;
            }

            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.Home == house)
                {
                    multiplier = Mathf.Max(multiplier, resident.NutritionMortalityMultiplier);
                }
            }

            return multiplier;
        }

        private float GetSecondsUntilNextRation()
        {
            int currentDay = StrategyDayNightCycleController.CurrentDayIndex;
            int targetDay = Mathf.Max(currentDay, lastResolvedDayIndex + 1, configuredDayIndex + SettlingGraceDays);
            if (IsNightMealTime() && CanResolveNightMealForDay(currentDay))
            {
                return 0f;
            }

            return GetSecondsUntilNightMeal(targetDay);
        }

        private float GetSecondsUntilFirstRation()
        {
            if (hasResolvedDailyRation)
            {
                return 0f;
            }

            int currentDay = StrategyDayNightCycleController.CurrentDayIndex;
            int firstDay = configuredDayIndex + SettlingGraceDays;
            if (IsNightMealTime() && CanResolveNightMealForDay(currentDay))
            {
                return 0f;
            }

            return GetSecondsUntilNightMeal(firstDay);
        }

        private string GetStatusText()
        {
            return Status switch
            {
                StrategyHouseholdFoodStatus.Settling => "settling",
                StrategyHouseholdFoodStatus.ShortRations => "short rations",
                StrategyHouseholdFoodStatus.Hungry => "hungry",
                StrategyHouseholdFoodStatus.Starving => "starving",
                _ => "fed"
            };
        }

        private void ClearLastRation()
        {
            lastResidentCount = 0;
            lastConsumedFood = 0;
            lastHouseFoodConsumed = 0;
            lastIngredientFoodConsumed = 0;
            lastGameConsumed = 0;
            lastFishConsumed = 0;
            lastRequiredRations = 0f;
            lastSuppliedRations = 0f;
            lastMissingRations = 0f;
            lastHouseRationsSupplied = 0f;
            lastIngredientRationsSupplied = 0f;
            lastGranaryRationsSupplied = 0f;
        }

        private static string FormatRation(float value)
        {
            return value.ToString("0.#");
        }
    }
}
