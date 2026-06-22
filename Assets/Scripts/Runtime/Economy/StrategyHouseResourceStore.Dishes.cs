using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyHouseResourceStore
    {
        public int ConsumePreparedDishes(float requestedRations, out float suppliedRations)
        {
            suppliedRations = 0f;
            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            if (dishRation <= 0f || requestedRations <= 0.01f)
            {
                return 0;
            }

            float remaining = Mathf.Max(0f, requestedRations);
            int consumed = 0;
            while (remaining > 0.01f)
            {
                string stackId = FindBestPreparedDishStackId();
                if (string.IsNullOrEmpty(stackId))
                {
                    break;
                }

                StrategyDishRecipe recipe = StrategyDishRecipeCatalog.FindById(stackId);
                if (recipe == null || recipe.RationValue <= 0f)
                {
                    preparedDishAmounts.Remove(stackId);
                    continue;
                }

                int available = preparedDishAmounts.TryGetValue(stackId, out int amount) ? amount : 0;
                int requestedDishes = Mathf.CeilToInt(remaining / recipe.RationValue);
                int taken = Mathf.Min(available, requestedDishes);
                if (taken <= 0)
                {
                    preparedDishAmounts.Remove(stackId);
                    continue;
                }

                consumed += taken;
                suppliedRations += taken * recipe.RationValue;
                remaining = Mathf.Max(0f, remaining - taken * recipe.RationValue);
                SetPreparedDishAmount(stackId, available - taken);
            }

            return consumed;
        }

        public bool TryCookDishes(
            int requestedDishes,
            out int dishesCooked,
            out int consumedIngredients,
            out float consumedIngredientRations,
            out int consumedPottery)
        {
            return TryCookDishes(
                requestedDishes,
                out dishesCooked,
                out consumedIngredients,
                out consumedIngredientRations,
                out consumedPottery,
                out _);
        }

        public bool TryCookDishes(
            int requestedDishes,
            out int dishesCooked,
            out int consumedIngredients,
            out float consumedIngredientRations,
            out int consumedPottery,
            out StrategyDishCookingSummary cookingSummary)
        {
            dishesCooked = 0;
            consumedIngredients = 0;
            consumedIngredientRations = 0f;
            consumedPottery = 0;
            cookingSummary = StrategyDishCookingSummary.Empty;
            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            int targetDishes = Mathf.Min(Mathf.Max(0, requestedDishes), GetPotteryAmount());
            if (targetDishes <= 0 || dishRation <= 0f)
            {
                return false;
            }

            float targetRations = targetDishes * dishRation;
            float producedRations = 0f;
            StrategyDishQuality bestQuality = StrategyDishQuality.Poor;
            Dictionary<string, int> cookedCounts = new();
            while (dishesCooked < targetDishes && producedRations < targetRations - 0.01f)
            {
                StrategyDishRecipe recipe = StrategyDishRecipeCatalog.ChooseBestAvailable(
                    amounts,
                    targetRations - producedRations);
                if (recipe == null)
                {
                    break;
                }

                int pottery = ConsumePottery(1);
                if (pottery <= 0)
                {
                    break;
                }

                if (!ConsumeRecipeIngredients(amounts, recipe))
                {
                    AddResource(StrategyResourceType.Pottery, pottery);
                    break;
                }

                AddPreparedDish(recipe, 1);
                cookedCounts.TryGetValue(recipe.Id, out int cooked);
                cookedCounts[recipe.Id] = cooked + 1;
                dishesCooked++;
                consumedPottery += pottery;
                consumedIngredients += recipe.IngredientUnitCount;
                consumedIngredientRations += recipe.IngredientRationValue;
                producedRations += recipe.RationValue;
                if (recipe.Quality > bestQuality)
                {
                    bestQuality = recipe.Quality;
                }
            }

            if (dishesCooked <= 0)
            {
                return false;
            }

            cookingSummary = new StrategyDishCookingSummary(
                FormatRecipeCounts(cookedCounts),
                bestQuality,
                producedRations);
            return true;
        }

        public string GetPreparedDishSummary(int maxStacks)
        {
            List<StrategyPreparedDishStack> stacks = new();
            CopyPreparedDishStacks(stacks);
            if (stacks.Count <= 0)
            {
                return "none";
            }

            int limit = Mathf.Clamp(maxStacks, 1, stacks.Count);
            StringBuilder builder = new();
            for (int i = 0; i < limit; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                StrategyPreparedDishStack stack = stacks[i];
                builder.Append(stack.Recipe.DisplayName);
                builder.Append(" x");
                builder.Append(stack.Amount);
            }

            if (stacks.Count > limit)
            {
                builder.Append(", +");
                builder.Append(stacks.Count - limit);
            }

            return builder.ToString();
        }

        public bool TryGetNextPreparedDish(out StrategyDishRecipe recipe, out int amount)
        {
            recipe = null;
            amount = 0;
            string stackId = FindBestPreparedDishStackId();
            if (string.IsNullOrEmpty(stackId))
            {
                return false;
            }

            recipe = StrategyDishRecipeCatalog.FindById(stackId);
            amount = preparedDishAmounts.TryGetValue(stackId, out int stackAmount)
                ? Mathf.Max(0, stackAmount)
                : 0;
            return recipe != null && amount > 0;
        }

        public bool TryGetBestCookableRecipe(float targetRations, out StrategyDishRecipe recipe)
        {
            recipe = StrategyDishRecipeCatalog.ChooseBestAvailable(
                amounts,
                Mathf.Max(0f, targetRations));
            return recipe != null;
        }

        public int CopyPreparedDishStacks(List<StrategyPreparedDishStack> target)
        {
            if (target == null)
            {
                return 0;
            }

            target.Clear();
            foreach (KeyValuePair<string, int> stack in preparedDishAmounts)
            {
                StrategyDishRecipe recipe = StrategyDishRecipeCatalog.FindById(stack.Key);
                if (recipe != null && stack.Value > 0)
                {
                    target.Add(new StrategyPreparedDishStack(recipe, stack.Value));
                }
            }

            target.Sort((left, right) => StrategyDishRecipeCatalog.CompareServingPriority(left.Recipe, right.Recipe));
            return target.Count;
        }

        private void AddPreparedDish(StrategyDishRecipe recipe, int amount)
        {
            if (recipe == null || amount <= 0)
            {
                return;
            }

            preparedDishAmounts.TryGetValue(recipe.Id, out int current);
            preparedDishAmounts[recipe.Id] = current + amount;
        }

        private int CountCookableDishUnits(float targetRations)
        {
            Dictionary<StrategyResourceType, int> available = CopyIngredientAmounts();
            float producedRations = 0f;
            int count = 0;
            while (count < 128 && (targetRations <= 0.01f || producedRations < targetRations - 0.01f))
            {
                StrategyDishRecipe recipe = StrategyDishRecipeCatalog.ChooseBestAvailable(
                    available,
                    targetRations > 0.01f ? targetRations - producedRations : 0f);
                if (recipe == null || !ConsumeRecipeIngredients(available, recipe))
                {
                    break;
                }

                count++;
                producedRations += recipe.RationValue;
            }

            return count;
        }

        private int ConsumePottery(int requested)
        {
            int available = GetPotteryAmount();
            int taken = Mathf.Min(available, Mathf.Max(0, requested));
            if (taken <= 0)
            {
                return 0;
            }

            int nextAmount = available - taken;
            if (nextAmount > 0)
            {
                amounts[StrategyResourceType.Pottery] = nextAmount;
            }
            else
            {
                amounts.Remove(StrategyResourceType.Pottery);
            }

            return taken;
        }

        private string FindBestPreparedDishStackId()
        {
            string bestId = null;
            StrategyDishRecipe bestRecipe = null;
            foreach (KeyValuePair<string, int> stack in preparedDishAmounts)
            {
                StrategyDishRecipe recipe = stack.Value > 0
                    ? StrategyDishRecipeCatalog.FindById(stack.Key)
                    : null;
                if (recipe == null)
                {
                    continue;
                }

                if (bestRecipe == null
                    || StrategyDishRecipeCatalog.CompareServingPriority(recipe, bestRecipe) < 0)
                {
                    bestId = stack.Key;
                    bestRecipe = recipe;
                }
            }

            return bestId;
        }

        private Dictionary<StrategyResourceType, int> CopyIngredientAmounts()
        {
            Dictionary<StrategyResourceType, int> copy = new();
            for (int i = 0; i < IngredientConsumptionOrder.Length; i++)
            {
                StrategyResourceType type = IngredientConsumptionOrder[i];
                int amount = GetAmount(type);
                if (amount > 0)
                {
                    copy[type] = amount;
                }
            }

            return copy;
        }

        private static bool ConsumeRecipeIngredients(
            Dictionary<StrategyResourceType, int> source,
            StrategyDishRecipe recipe)
        {
            if (source == null || !StrategyDishRecipeCatalog.HasIngredients(recipe, source))
            {
                return false;
            }

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                StrategyDishIngredient ingredient = recipe.Ingredients[i];
                source.TryGetValue(ingredient.Resource, out int current);
                int next = current - ingredient.Amount;
                if (next > 0)
                {
                    source[ingredient.Resource] = next;
                }
                else
                {
                    source.Remove(ingredient.Resource);
                }
            }

            return true;
        }

        private void SetPreparedDishAmount(string stackId, int amount)
        {
            if (amount > 0)
            {
                preparedDishAmounts[stackId] = amount;
            }
            else
            {
                preparedDishAmounts.Remove(stackId);
            }
        }

        private static string FormatRecipeCounts(Dictionary<string, int> recipeCounts)
        {
            if (recipeCounts == null || recipeCounts.Count <= 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            foreach (KeyValuePair<string, int> cooked in recipeCounts)
            {
                StrategyDishRecipe recipe = StrategyDishRecipeCatalog.FindById(cooked.Key);
                if (recipe == null || cooked.Value <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(recipe.DisplayName);
                builder.Append(" x");
                builder.Append(cooked.Value);
            }

            return builder.ToString();
        }
    }
}
