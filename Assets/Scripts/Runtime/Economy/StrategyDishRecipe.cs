using System;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyDishIngredient
    {
        public StrategyDishIngredient(StrategyResourceType resource, int amount)
        {
            Resource = resource;
            Amount = amount;
        }

        public StrategyResourceType Resource { get; }
        public int Amount { get; }
    }

    public sealed class StrategyDishRecipe
    {
        public StrategyDishRecipe(
            string id,
            string displayName,
            StrategyDishQuality quality,
            float rationValue,
            params StrategyDishIngredient[] ingredients)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Quality = quality;
            RationValue = Math.Max(0f, rationValue);
            Ingredients = ingredients ?? Array.Empty<StrategyDishIngredient>();
            IngredientUnitCount = CalculateIngredientUnitCount(Ingredients);
            IngredientRationValue = CalculateIngredientRationValue(Ingredients);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public StrategyDishQuality Quality { get; }
        public float RationValue { get; }
        public StrategyDishIngredient[] Ingredients { get; }
        public int IngredientUnitCount { get; }
        public float IngredientRationValue { get; }

        public string QualityLabel => StrategyDishRecipeCatalog.GetQualityLabel(Quality);

        private static int CalculateIngredientUnitCount(StrategyDishIngredient[] ingredients)
        {
            int total = 0;
            for (int i = 0; i < ingredients.Length; i++)
            {
                total += Math.Max(0, ingredients[i].Amount);
            }

            return total;
        }

        private static float CalculateIngredientRationValue(StrategyDishIngredient[] ingredients)
        {
            float total = 0f;
            for (int i = 0; i < ingredients.Length; i++)
            {
                StrategyDishIngredient ingredient = ingredients[i];
                total += Math.Max(0, ingredient.Amount)
                    * StrategyFoodNutrition.GetRationValue(ingredient.Resource);
            }

            return total;
        }
    }
}
