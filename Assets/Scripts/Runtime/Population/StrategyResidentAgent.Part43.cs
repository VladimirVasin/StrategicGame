using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryStartHouseholdCookingTask()
        {
            if (!CanStartGardenDuty()
                || home == null
                || home.Resources == null
                || !IsHouseholder
                || constructionSite != null
                || !CanWork
                || HasExternalWorkplace
                || !StrategyDayNightCycleController.IsHouseholdCookingTime
                || HasAnyCarriedResource())
            {
                return false;
            }

            float dailyNeed = CalculateHomeDailyRationNeed();
            float preparedRations = home.Resources.GetPreparedDishRations();
            if (dailyNeed <= 0f || preparedRations >= dailyNeed)
            {
                return false;
            }

            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            if (home.Resources.GetTotalIngredientRationValue() < dishRation || !TryBuildPathToHomeDropoff())
            {
                return false;
            }

            activity = ResidentActivity.MovingToHouseCooking;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderCookingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("requiredRations", dailyNeed),
                StrategyDebugLogger.F("preparedRations", preparedRations),
                StrategyDebugLogger.F("ingredientRations", home.Resources.GetTotalIngredientRationValue()));
            return true;
        }

        private void StartHouseholdCooking()
        {
            if (home == null || home.Resources == null)
            {
                ResetHouseholdFoodWorkToIdle(false);
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.CookingHouseMeal;
            lumberWorkTimer = Random.Range(4.2f, 6.8f);
            FaceWorldPoint(home.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderCookingAtHome",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin));
        }

        private void UpdateHouseholdCooking()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateGardenWork();
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int dishesRequested = CalculateRequestedDinnerDishes();
            if (home != null
                && home.Resources != null
                && home.Resources.TryCookDishes(
                    dishesRequested,
                    out int dishesCooked,
                    out int consumedIngredients,
                    out float consumedIngredientRations))
            {
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholdDishesCooked",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("dishes", dishesCooked),
                    StrategyDebugLogger.F("ingredientsConsumed", consumedIngredients),
                    StrategyDebugLogger.F("ingredientRations", consumedIngredientRations),
                    StrategyDebugLogger.F("preparedDishes", home.Resources.GetPreparedDishAmount()));
            }
            else
            {
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholdCookingFailed",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("requestedDishes", dishesRequested));
            }

            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.25f, 0.75f);
            if (IsNightSleepTime())
            {
                TryStartNightSleep();
            }
        }

        private int CalculateRequestedDinnerDishes()
        {
            if (home == null || home.Resources == null)
            {
                return 0;
            }

            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            float missingRations = Mathf.Max(0f, CalculateHomeDailyRationNeed() - home.Resources.GetPreparedDishRations());
            return dishRation > 0f ? Mathf.CeilToInt(missingRations / dishRation) : 0;
        }
    }
}
