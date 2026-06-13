using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyHouseholdFoodStatus
    {
        Settling,
        WaitingForFood,
        Fed,
        Shortage
    }

    [DisallowMultipleComponent]
    public sealed class StrategyHouseholdFoodState : MonoBehaviour
    {
        private const float FoodTickSeconds = 10f;
        private const float FoodPerResidentPerTick = 0.5f;
        private const float FoodSystemGraceSeconds = 180f;
        private const float FoodUnavailableLogIntervalSeconds = 35f;
        private const int MaxStarvationLevel = 5;

        private StrategyPopulationController population;
        private StrategyPlacedBuilding house;
        private float foodTimer;
        private float foodGraceTimer;
        private float foodUnavailableLogTimer;
        private int starvationLevel;
        private int lastResidentCount;
        private int lastRequiredFood;
        private int lastConsumedFood;
        private int lastHouseFoodConsumed;
        private int lastGameConsumed;
        private int lastFishConsumed;
        private bool foodSupplyActivated;

        public int StarvationLevel => starvationLevel;
        public int LastResidentCount => lastResidentCount;
        public int LastRequiredFood => lastRequiredFood;
        public int LastConsumedFood => lastConsumedFood;
        public int LastHouseFoodConsumed => lastHouseFoodConsumed;
        public int LastGameConsumed => lastGameConsumed;
        public int LastFishConsumed => lastFishConsumed;
        public float NextFoodTickSeconds => Mathf.Max(0f, foodTimer);
        public float FoodGraceSecondsRemaining => Mathf.Max(0f, foodGraceTimer);
        public float FoodGraceDurationSeconds => FoodSystemGraceSeconds;
        public bool IsFoodSupplyActivated => foodSupplyActivated;
        public bool IsStarving => starvationLevel > 0;
        public bool IsBirthBlocked => starvationLevel > 0;
        public StrategyHouseholdFoodStatus Status => GetStatus();
        public float MortalityMultiplier => GetMortalityMultiplier(starvationLevel);

        public void Configure(StrategyPopulationController populationController, StrategyPlacedBuilding homeBuilding)
        {
            bool houseChanged = house != homeBuilding;
            population = populationController;
            house = homeBuilding;
            if (houseChanged || foodTimer <= 0f)
            {
                foodTimer = Random.Range(FoodTickSeconds * 0.35f, FoodTickSeconds);
            }

            if (houseChanged)
            {
                foodGraceTimer = FoodSystemGraceSeconds;
                foodUnavailableLogTimer = 0f;
                foodSupplyActivated = false;
            }
        }

        private void Update()
        {
            if (population == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            foodTimer -= Time.deltaTime;
            foodGraceTimer = Mathf.Max(0f, foodGraceTimer - Time.deltaTime);
            foodUnavailableLogTimer = Mathf.Max(0f, foodUnavailableLogTimer - Time.deltaTime);
            while (foodTimer <= 0f)
            {
                foodTimer += FoodTickSeconds;
                ConsumeHouseholdFood();
            }
        }

        public string GetCompactHudText()
        {
            string status;
            if (foodGraceTimer > 0f)
            {
                status = "settling " + Mathf.CeilToInt(foodGraceTimer) + "s";
            }
            else if (!foodSupplyActivated)
            {
                status = "waiting for food";
            }
            else
            {
                status = starvationLevel <= 0
                    ? "fed"
                    : "shortage x" + MortalityMultiplier.ToString("0.0");
            }

            return "Food: "
                + status
                + " | Need "
                + lastRequiredFood
                + " | Last "
                + lastConsumedFood
                + "/"
                + lastRequiredFood;
        }

        private void ConsumeHouseholdFood()
        {
            lastResidentCount = CountActiveResidents();
            if (lastResidentCount <= 0)
            {
                lastRequiredFood = 0;
                lastConsumedFood = 0;
                lastHouseFoodConsumed = 0;
                lastGameConsumed = 0;
                lastFishConsumed = 0;
                starvationLevel = Mathf.Max(0, starvationLevel - 1);
                return;
            }

            lastRequiredFood = Mathf.Max(1, Mathf.CeilToInt(lastResidentCount * FoodPerResidentPerTick));
            int homeFood = house.Resources != null ? house.Resources.GetTotalFoodAmount() : 0;
            int granaryFood = StrategyGranary.GetTotalSettlementFood();
            int totalFood = homeFood + granaryFood;
            if (totalFood > 0)
            {
                foodSupplyActivated = true;
            }

            if (foodGraceTimer > 0f || (!foodSupplyActivated && totalFood <= 0))
            {
                int previousLevel = starvationLevel;
                lastConsumedFood = 0;
                lastHouseFoodConsumed = 0;
                lastGameConsumed = 0;
                lastFishConsumed = 0;
                starvationLevel = Mathf.Max(0, starvationLevel - 1);
                LogDeferredFoodTick(previousLevel, homeFood, granaryFood);
                return;
            }

            int remainingFood = lastRequiredFood;
            lastHouseFoodConsumed = house.Resources != null ? house.Resources.ConsumeFood(remainingFood) : 0;
            remainingFood -= lastHouseFoodConsumed;
            int granaryConsumed = 0;
            if (remainingFood > 0)
            {
                granaryConsumed = StrategyGranary.ConsumeSettlementFood(
                    remainingFood,
                    house.FootprintBounds.center,
                    out lastGameConsumed,
                    out lastFishConsumed);
            }
            else
            {
                lastGameConsumed = 0;
                lastFishConsumed = 0;
            }

            lastConsumedFood = lastHouseFoodConsumed + granaryConsumed;

            if (lastConsumedFood >= lastRequiredFood)
            {
                int previousLevel = starvationLevel;
                starvationLevel = Mathf.Max(0, starvationLevel - 1);
                StrategyDebugLogger.Info(
                    "Food",
                    "HouseholdFed",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("residents", lastResidentCount),
                    StrategyDebugLogger.F("required", lastRequiredFood),
                    StrategyDebugLogger.F("consumed", lastConsumedFood),
                    StrategyDebugLogger.F("houseFood", lastHouseFoodConsumed),
                    StrategyDebugLogger.F("game", lastGameConsumed),
                    StrategyDebugLogger.F("fish", lastFishConsumed),
                    StrategyDebugLogger.F("previousStarvation", previousLevel),
                    StrategyDebugLogger.F("starvation", starvationLevel));
                return;
            }

            starvationLevel = Mathf.Min(MaxStarvationLevel, starvationLevel + 1);
            StrategyDebugLogger.Warn(
                "Food",
                "HouseholdFoodShortage",
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("residents", lastResidentCount),
                StrategyDebugLogger.F("required", lastRequiredFood),
                StrategyDebugLogger.F("consumed", lastConsumedFood),
                StrategyDebugLogger.F("houseFood", lastHouseFoodConsumed),
                StrategyDebugLogger.F("game", lastGameConsumed),
                StrategyDebugLogger.F("fish", lastFishConsumed),
                StrategyDebugLogger.F("starvation", starvationLevel),
                StrategyDebugLogger.F("mortalityMultiplier", MortalityMultiplier));
        }

        private void LogDeferredFoodTick(int previousLevel, int homeFood, int granaryFood)
        {
            if (foodUnavailableLogTimer > 0f)
            {
                return;
            }

            foodUnavailableLogTimer = FoodUnavailableLogIntervalSeconds;
            StrategyDebugLogger.Info(
                "Food",
                "HouseholdFoodDeferred",
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("reason", foodGraceTimer > 0f ? "grace" : "no_food_supply"),
                StrategyDebugLogger.F("residents", lastResidentCount),
                StrategyDebugLogger.F("required", lastRequiredFood),
                StrategyDebugLogger.F("homeFood", homeFood),
                StrategyDebugLogger.F("granaryFood", granaryFood),
                StrategyDebugLogger.F("totalFood", homeFood + granaryFood),
                StrategyDebugLogger.F("previousStarvation", previousLevel),
                StrategyDebugLogger.F("starvation", starvationLevel),
                StrategyDebugLogger.F("graceSeconds", FoodGraceSecondsRemaining));
        }

        private StrategyHouseholdFoodStatus GetStatus()
        {
            if (foodGraceTimer > 0f)
            {
                return StrategyHouseholdFoodStatus.Settling;
            }

            if (!foodSupplyActivated)
            {
                return StrategyHouseholdFoodStatus.WaitingForFood;
            }

            return starvationLevel > 0
                ? StrategyHouseholdFoodStatus.Shortage
                : StrategyHouseholdFoodStatus.Fed;
        }

        private static float GetMortalityMultiplier(int level)
        {
            if (level <= 0)
            {
                return 1f;
            }

            return level switch
            {
                1 => 1.25f,
                2 => 1.75f,
                3 => 2.5f,
                4 => 4f,
                _ => 6f
            };
        }

        private int CountActiveResidents()
        {
            int count = 0;
            if (house == null)
            {
                return count;
            }

            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.Home == house && !resident.IsPendingRefugee)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
