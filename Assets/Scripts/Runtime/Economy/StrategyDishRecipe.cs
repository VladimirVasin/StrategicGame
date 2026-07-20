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
        private readonly string displayName;
        private readonly string localizationTable;
        private readonly string displayNameKey;

        public StrategyDishRecipe(
            string id,
            string displayName,
            StrategyDishQuality quality,
            float rationValue,
            params StrategyDishIngredient[] ingredients)
        {
            Id = id ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            localizationTable = string.Empty;
            displayNameKey = string.Empty;
            Quality = quality;
            RationValue = Math.Max(0f, rationValue);
            Ingredients = ingredients ?? Array.Empty<StrategyDishIngredient>();
            IngredientUnitCount = CalculateIngredientUnitCount(Ingredients);
            IngredientRationValue = CalculateIngredientRationValue(Ingredients);
        }

        internal StrategyDishRecipe(
            string id,
            string displayName,
            StrategyDishQuality quality,
            float rationValue,
            string localizationTable,
            string displayNameKey,
            params StrategyDishIngredient[] ingredients)
            : this(id, displayName, quality, rationValue, ingredients)
        {
            this.localizationTable = localizationTable ?? string.Empty;
            this.displayNameKey = displayNameKey ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(localizationTable) || string.IsNullOrEmpty(displayNameKey))
                {
                    return displayName;
                }

                string localized = StrategyLocalization.Get(localizationTable, displayNameKey);
                return localized == displayNameKey ? displayName : localized;
            }
        }
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
