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

            if (!householdCookingTask.CanCook(home.Resources, dailyNeed, out int requestedDishes)
                || !TryBuildPathToHomeDropoff())
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
                StrategyDebugLogger.F("ingredientRations", home.Resources.GetTotalIngredientRationValue()),
                StrategyDebugLogger.F("pottery", home.Resources.GetPotteryAmount()),
                StrategyDebugLogger.F("requestedDishes", requestedDishes));
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
            householdCookingTask.Begin(Random.Range(4.2f, 6.8f));
            FaceWorldPoint(home.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderCookingAtHome",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin));
        }

        private void UpdateHouseholdCooking()
        {
            AnimateGardenWork();
            if (!householdCookingTask.Tick(Time.deltaTime))
            {
                return;
            }

            float dailyNeed = CalculateHomeDailyRationNeed();
            StrategyHouseResourceStore resources = home != null ? home.Resources : null;
            if (householdCookingTask.TryComplete(
                resources,
                dailyNeed,
                out StrategyResidentHouseholdCookingTask.CookingResult result))
            {
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholdDishesCooked",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("dishes", result.DishesCooked),
                    StrategyDebugLogger.F("recipes", result.Summary.RecipesText),
                    StrategyDebugLogger.F("bestQuality", StrategyDishRecipeCatalog.GetQualityLabel(result.Summary.BestQuality)),
                    StrategyDebugLogger.F("producedRations", result.Summary.ProducedRations),
                    StrategyDebugLogger.F("ingredientsConsumed", result.ConsumedIngredients),
                    StrategyDebugLogger.F("potteryConsumed", result.ConsumedPottery),
                    StrategyDebugLogger.F("ingredientRations", result.ConsumedIngredientRations),
                    StrategyDebugLogger.F("potteryRemaining", home.Resources.GetPotteryAmount()),
                    StrategyDebugLogger.F("preparedDishes", home.Resources.GetPreparedDishAmount()));
            }
            else
            {
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholdCookingFailed",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("requestedDishes", result.RequestedDishes),
                    StrategyDebugLogger.F("pottery", home != null && home.Resources != null ? home.Resources.GetPotteryAmount() : 0),
                    StrategyDebugLogger.F("cookableByIngredients", home != null && home.Resources != null ? home.Resources.GetCookableDishCountByIngredients() : 0));
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

    }
}
