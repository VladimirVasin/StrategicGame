using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyResidentHouseholdCookingTask
    {
        private float remainingSeconds;

        public bool IsRunning => remainingSeconds > 0f;

        public void Begin(float durationSeconds)
        {
            remainingSeconds = Mathf.Max(0.01f, durationSeconds);
        }

        public bool Tick(float deltaSeconds)
        {
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Mathf.Max(0f, deltaSeconds));
            return remainingSeconds <= 0f;
        }

        public void Cancel()
        {
            remainingSeconds = 0f;
        }

        public int CalculateRequestedDishes(float dailyNeed, float preparedRations)
        {
            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            float missingRations = Mathf.Max(0f, dailyNeed - preparedRations);
            return dishRation > 0f ? Mathf.CeilToInt(missingRations / dishRation) : 0;
        }

        public bool CanCook(
            StrategyHouseResourceStore resources,
            float dailyNeed,
            out int requestedDishes)
        {
            requestedDishes = resources != null
                ? CalculateRequestedDishes(dailyNeed, resources.GetPreparedDishRations())
                : 0;
            return requestedDishes > 0
                && resources.GetCookableDishCountByIngredients() > 0
                && resources.GetPotteryAmount() > 0;
        }

        public bool TryComplete(
            StrategyHouseResourceStore resources,
            float dailyNeed,
            out CookingResult result)
        {
            result = default;
            int requestedDishes = resources != null
                ? CalculateRequestedDishes(dailyNeed, resources.GetPreparedDishRations())
                : 0;
            result.RequestedDishes = requestedDishes;
            if (resources == null
                || !resources.TryCookDishes(
                    requestedDishes,
                    out result.DishesCooked,
                    out result.ConsumedIngredients,
                    out result.ConsumedIngredientRations,
                    out result.ConsumedPottery,
                    out result.Summary))
            {
                return false;
            }

            return true;
        }

        internal struct CookingResult
        {
            public int RequestedDishes;
            public int DishesCooked;
            public int ConsumedIngredients;
            public float ConsumedIngredientRations;
            public int ConsumedPottery;
            public StrategyDishCookingSummary Summary;
        }
    }
}
