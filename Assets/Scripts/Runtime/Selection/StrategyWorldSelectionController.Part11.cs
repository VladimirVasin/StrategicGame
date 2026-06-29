using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private void RefreshHouseFoodRows(StrategyPlacedBuilding building)
        {
            StrategyHouseholdFoodState food = building != null
                ? building.GetComponent<StrategyHouseholdFoodState>()
                : null;
            StrategyHouseResourceStore store = building != null ? building.Resources : null;
            float dinnerNeed = CalculateHouseDinnerNeed(building);
            float availableRations = store != null ? store.GetTotalRationValue() : 0f;
            float fill = dinnerNeed > 0.01f ? availableRations / dinnerNeed : availableRations > 0f ? 1f : 0f;

            string summaryLabels = "Need\nAvailable\nMeal check";
            string summaryValues = FormatRations(dinnerNeed)
                + "r\n"
                + FormatRations(availableRations)
                + "r\n"
                + FormatMealCheckTimer(food);
            GetDinnerColors(food, dinnerNeed, availableRations, out Color rowColor, out Color fillColor);

            ApplyFoodStatus(
                summaryLabels,
                summaryValues,
                "Food                         Qty   Nutrition",
                fill,
                rowColor,
                fillColor);
        }

        private static string FormatMealCheckTimer(StrategyHouseholdFoodState food)
        {
            if (food == null)
            {
                return "--";
            }

            float seconds = food.IsNightMealWaiting
                ? food.NightMealFallbackSecondsRemaining
                : food.NextFoodTickSeconds;
            return seconds <= 0.5f ? "now" : FormatDuration(seconds);
        }

        private static string GetDinnerDetailLine(
            StrategyHouseholdFoodState food,
            StrategyHouseResourceStore store,
            float dinnerNeed,
            float readyRations)
        {
            if (food != null && food.IsNightMealWaiting)
            {
                return "Family home: "
                    + food.NightMealPresentResidentCount
                    + "/"
                    + food.NightMealExpectedResidentCount;
            }

            if (store != null && store.LeftoverRations > 0.01f)
            {
                return "Next: Leftovers";
            }

            if (store != null
                && store.TryGetNextPreparedDish(out StrategyDishRecipe preparedRecipe, out _))
            {
                return "Next: " + preparedRecipe.DisplayName;
            }

            float missingRations = Mathf.Max(0f, dinnerNeed - readyRations);
            if (store != null
                && store.TryGetBestCookableRecipe(missingRations, out StrategyDishRecipe recipe)
                && store.GetPotteryAmount() > 0)
            {
                return "Next: " + recipe.DisplayName;
            }

            if (food != null
                && food.Status == StrategyHouseholdFoodStatus.Settling
                && food.FoodGraceSecondsRemaining > 0.01f)
            {
                return "Dinner in " + FormatDuration(food.FoodGraceSecondsRemaining);
            }

            if (store != null && store.GetTotalIngredientRationValue() > 0.01f)
            {
                return "No dishes ready";
            }

            return "No food ready";
        }

        private static string GetDinnerStateLine(
            StrategyHouseholdFoodState food,
            StrategyHouseResourceStore store,
            float dinnerNeed,
            float readyRations)
        {
            if (food != null && food.Status == StrategyHouseholdFoodStatus.Starving)
            {
                return "Starving";
            }

            if (food != null && food.Status == StrategyHouseholdFoodStatus.Hungry)
            {
                return "Hungry";
            }

            if (dinnerNeed > 0.01f && readyRations >= dinnerNeed - 0.01f)
            {
                return "Ready";
            }

            if (store == null)
            {
                return "Pending";
            }

            bool hasIngredients = store.GetTotalIngredientRationValue() > 0.01f;
            bool canCookByIngredients = store.GetCookableDishCountByIngredients() > 0;
            if (canCookByIngredients && store.GetPotteryAmount() <= 0)
            {
                return "Missing: Pottery";
            }

            if (!canCookByIngredients && readyRations <= 0.01f)
            {
                return hasIngredients ? "Raw fallback" : "Missing: Food";
            }

            if (readyRations <= 0.01f && hasIngredients)
            {
                return "Raw fallback";
            }

            return "Short: " + FormatRations(Mathf.Max(0f, dinnerNeed - readyRations)) + "r";
        }

        private static void GetDinnerColors(
            StrategyHouseholdFoodState food,
            float dinnerNeed,
            float readyRations,
            out Color rowColor,
            out Color fillColor)
        {
            if (food != null && food.Status == StrategyHouseholdFoodStatus.Starving)
            {
                rowColor = new Color(0.36f, 0.16f, 0.13f, 0.96f);
                fillColor = new Color(0.86f, 0.34f, 0.24f, 0.95f);
                return;
            }

            if (food != null && food.Status == StrategyHouseholdFoodStatus.Hungry)
            {
                rowColor = new Color(0.35f, 0.22f, 0.13f, 0.96f);
                fillColor = new Color(0.86f, 0.50f, 0.24f, 0.95f);
                return;
            }

            if (dinnerNeed > 0.01f && readyRations < dinnerNeed - 0.01f)
            {
                rowColor = new Color(0.32f, 0.26f, 0.16f, 0.94f);
                fillColor = new Color(0.77f, 0.60f, 0.31f, 0.95f);
                return;
            }

            rowColor = new Color(0.15f, 0.30f, 0.22f, 0.94f);
            fillColor = new Color(0.56f, 0.76f, 0.38f, 0.95f);
        }

        private static string FormatDinnerStockLine(StrategyHouseResourceStore store)
        {
            if (store == null || !store.HasAny)
            {
                return "Stock: none";
            }

            return "Stock: Dishes "
                + store.GetPreparedDishAmount()
                + " | Leftovers "
                + FormatRations(store.LeftoverRations)
                + "r"
                + " | Pottery "
                + store.GetPotteryAmount()
                + " | Raw "
                + FormatRations(store.GetTotalIngredientRationValue())
                + "r";
        }

        private static float CalculateHouseDinnerNeed(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < building.Residents.Count; i++)
            {
                StrategyResidentAgent resident = building.Residents[i];
                if (resident != null
                    && resident.Home == building
                    && !resident.IsPendingRefugee)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }

        private static string FormatRations(float rations)
        {
            return rations.ToString("0.#");
        }

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return minutes > 0
                ? minutes + "m " + remainder.ToString("00") + "s"
                : remainder + "s";
        }

        private void ApplyFoodStatus(
            string summaryLabels,
            string summaryValues,
            string tableHeader,
            float rationFill,
            Color statusColor,
            Color fillColor)
        {
            if (foodStatusRowImage != null)
            {
                foodStatusRowImage.color = statusColor;
            }

            if (foodStatusText != null)
            {
                foodStatusText.text = summaryLabels;
            }

            if (foodMealText != null)
            {
                foodMealText.text = summaryValues;
            }

            if (foodGranaryText != null)
            {
                foodGranaryText.text = tableHeader;
            }

            if (foodMealFillRect != null)
            {
                foodMealFillRect.anchorMax = new Vector2(Mathf.Clamp01(rationFill), 1f);
                foodMealFillRect.offsetMin = Vector2.zero;
                foodMealFillRect.offsetMax = Vector2.zero;
            }

            if (foodMealFillImage != null)
            {
                foodMealFillImage.color = fillColor;
            }
        }
    }
}
