using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHouseholdState : MonoBehaviour
    {
        private const float BirthCooldownMin = 90f;
        private const float BirthCooldownMax = 180f;
        private const float FullHouseRetryMin = 24f;
        private const float FullHouseRetryMax = 45f;
        private const float FoodShortageRetryMin = 36f;
        private const float FoodShortageRetryMax = 68f;

        private StrategyPopulationController population;
        private StrategyPlacedBuilding house;
        private float birthTimer;

        public float BirthTimer => birthTimer;

        public void Configure(StrategyPopulationController populationController, StrategyPlacedBuilding homeBuilding)
        {
            population = populationController;
            house = homeBuilding;
            ResetBirthTimer();
        }

        private void Update()
        {
            if (population == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            birthTimer -= Time.deltaTime;
            if (birthTimer > 0f)
            {
                return;
            }

            if (!house.HasFreeResidentSlot)
            {
                birthTimer = Random.Range(FullHouseRetryMin, FullHouseRetryMax);
                return;
            }

            StrategyHouseholdFoodState food = house.GetComponent<StrategyHouseholdFoodState>();
            if (food != null && food.IsBirthBlocked)
            {
                birthTimer = Random.Range(FoodShortageRetryMin, FoodShortageRetryMax);
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholdBirthBlockedFoodShortage",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("starvation", food.StarvationLevel),
                    StrategyDebugLogger.F("requiredFood", food.LastRequiredFood),
                    StrategyDebugLogger.F("lastConsumedFood", food.LastConsumedFood));
                return;
            }

            if (TryFindParents(out StrategyResidentAgent father, out StrategyResidentAgent mother)
                && population.TrySpawnChildForHouse(house, father, mother, out StrategyResidentAgent child))
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholdChildBorn",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("father", father.FullName),
                    StrategyDebugLogger.F("mother", mother.FullName),
                    StrategyDebugLogger.F("child", child != null ? child.FullName : string.Empty));
            }

            ResetBirthTimer();
        }

        private bool TryFindParents(out StrategyResidentAgent father, out StrategyResidentAgent mother)
        {
            father = null;
            mother = null;

            IReadOnlyList<StrategyResidentAgent> residents = house.Residents;
            List<StrategyResidentAgent> males = new();
            List<StrategyResidentAgent> females = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || !resident.IsAdult || resident.Home != house)
                {
                    continue;
                }

                if (resident.Gender == StrategyResidentGender.Male)
                {
                    males.Add(resident);
                }
                else
                {
                    females.Add(resident);
                }
            }

            int maleStart = males.Count > 0 ? Random.Range(0, males.Count) : 0;
            int femaleStart = females.Count > 0 ? Random.Range(0, females.Count) : 0;
            for (int maleOffset = 0; maleOffset < males.Count; maleOffset++)
            {
                StrategyResidentAgent male = males[(maleStart + maleOffset) % males.Count];
                for (int femaleOffset = 0; femaleOffset < females.Count; femaleOffset++)
                {
                    StrategyResidentAgent female = females[(femaleStart + femaleOffset) % females.Count];
                    if (StrategyKinshipUtility.CanFormCouple(male, female, population))
                    {
                        father = male;
                        mother = female;
                        return true;
                    }
                }
            }

            return false;
        }

        private void ResetBirthTimer()
        {
            birthTimer = Random.Range(BirthCooldownMin, BirthCooldownMax);
        }
    }
}
