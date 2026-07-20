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
            StrategyHouseWarmthState warmth = building != null ? building.Warmth : null;

            string summaryLabels = L("house.food.summary_labels");
            string summaryValues = L(
                "house.food.summary_values",
                StrategySelectionLocalization.Rations(dinnerNeed),
                StrategySelectionLocalization.Rations(availableRations),
                FormatMealCheckTimer(food),
                FormatHouseWarmthLine(warmth, store));
            GetDinnerColors(food, dinnerNeed, availableRations, out Color rowColor, out Color fillColor);
            ApplyHouseWarmthColors(warmth, ref rowColor, ref fillColor);

            ApplyFoodStatus(
                summaryLabels,
                summaryValues,
                L("house.food.table_header"),
                fill,
                rowColor,
                fillColor);
        }

        private static string FormatHouseWarmthLine(StrategyHouseWarmthState warmth, StrategyHouseResourceStore store)
        {
            int logs = store != null ? store.GetLogsAmount() : 0;
            if (warmth == null)
            {
                return L("house.food.logs", logs);
            }

            string warmthText = L(
                "warmth.status",
                LocalizedValue(warmth.WarmthLevel.ToString()),
                StrategyTemperatureModel.FormatCelsius(warmth.IndoorCelsius));
            return L("house.food.warmth_logs", warmthText, logs);
        }

        private static void ApplyHouseWarmthColors(
            StrategyHouseWarmthState warmth,
            ref Color rowColor,
            ref Color fillColor)
        {
            if (warmth == null
                || StrategyDayNightCycleController.CurrentCalendarSnapshot.Season != StrategySeason.Winter
                || warmth.WarmthLevel == StrategyHouseWarmthLevel.Warm)
            {
                return;
            }

            if (warmth.WarmthLevel == StrategyHouseWarmthLevel.Freezing)
            {
                rowColor = new Color(0.33f, 0.15f, 0.20f, 0.96f);
                fillColor = new Color(0.80f, 0.31f, 0.42f, 0.95f);
                return;
            }

            if (warmth.WarmthLevel == StrategyHouseWarmthLevel.Cold)
            {
                rowColor = new Color(0.27f, 0.22f, 0.32f, 0.96f);
                fillColor = new Color(0.58f, 0.60f, 0.92f, 0.95f);
                return;
            }

            rowColor = new Color(0.19f, 0.27f, 0.31f, 0.96f);
            fillColor = new Color(0.58f, 0.78f, 0.95f, 0.95f);
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
            return seconds <= 0.5f ? LocalizedValue("now") : FormatDuration(seconds);
        }

        private static string GetDinnerDetailLine(
            StrategyHouseholdFoodState food,
            StrategyHouseResourceStore store,
            float dinnerNeed,
            float readyRations)
        {
            if (food != null && food.IsNightMealWaiting)
            {
                return L(
                    "house.food.family_home",
                    food.NightMealPresentResidentCount,
                    food.NightMealExpectedResidentCount);
            }

            if (store != null && store.LeftoverRations > 0.01f)
            {
                return L("house.food.next", L("house.food.leftovers"));
            }

            if (store != null
                && store.TryGetNextPreparedDish(out StrategyDishRecipe preparedRecipe, out _))
            {
                return L("house.food.next", preparedRecipe.DisplayName);
            }

            float missingRations = Mathf.Max(0f, dinnerNeed - readyRations);
            if (store != null
                && store.TryGetBestCookableRecipe(missingRations, out StrategyDishRecipe recipe)
                && store.GetPotteryAmount() > 0)
            {
                return L("house.food.next", recipe.DisplayName);
            }

            if (food != null
                && food.Status == StrategyHouseholdFoodStatus.Settling
                && food.FoodGraceSecondsRemaining > 0.01f)
            {
                return L("house.food.dinner_in", FormatDuration(food.FoodGraceSecondsRemaining));
            }

            if (store != null && store.GetTotalIngredientRationValue() > 0.01f)
            {
                return L("house.food.no_dishes");
            }

            return L("house.food.no_food");
        }

        private static string GetDinnerStateLine(
            StrategyHouseholdFoodState food,
            StrategyHouseResourceStore store,
            float dinnerNeed,
            float readyRations)
        {
            if (food != null && food.Status == StrategyHouseholdFoodStatus.Starving)
            {
                return LocalizedValue("Starving");
            }

            if (food != null && food.Status == StrategyHouseholdFoodStatus.Hungry)
            {
                return LocalizedValue("Hungry");
            }

            if (dinnerNeed > 0.01f && readyRations >= dinnerNeed - 0.01f)
            {
                return LocalizedValue("Ready");
            }

            if (store == null)
            {
                return LocalizedValue("Pending");
            }

            bool hasIngredients = store.GetTotalIngredientRationValue() > 0.01f;
            bool canCookByIngredients = store.GetCookableDishCountByIngredients() > 0;
            if (canCookByIngredients && store.GetPotteryAmount() <= 0)
            {
                return L("house.food.missing", GetResourceTitle(StrategyResourceType.Pottery));
            }

            if (!canCookByIngredients && readyRations <= 0.01f)
            {
                return hasIngredients
                    ? L("house.food.raw_fallback")
                    : L("house.food.missing", L("label.food"));
            }

            if (readyRations <= 0.01f && hasIngredients)
            {
                return L("house.food.raw_fallback");
            }

            return L(
                "house.food.short",
                StrategySelectionLocalization.Rations(
                    Mathf.Max(0f, dinnerNeed - readyRations)));
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
                return L("house.food.stock_none");
            }

            return L(
                "house.food.stock",
                store.GetPreparedDishAmount(),
                StrategySelectionLocalization.Rations(store.LeftoverRations),
                store.GetPotteryAmount(),
                StrategySelectionLocalization.Rations(
                    store.GetTotalIngredientRationValue()));
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

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return minutes > 0
                ? L("format.minutes_seconds_short", minutes, remainder.ToString("00"))
                : L("format.seconds_short", remainder);
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
