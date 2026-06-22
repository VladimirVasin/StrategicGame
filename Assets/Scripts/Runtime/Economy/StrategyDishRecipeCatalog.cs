using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public static class StrategyDishRecipeCatalog
    {
        private static readonly StrategyDishRecipe[] recipes =
        {
            R("mushroom_broth", "Mushroom Broth", StrategyDishQuality.Poor, 0.90f,
                I(StrategyResourceType.Mushrooms, 1), I(StrategyResourceType.Onion, 1)),
            R("root_soup", "Root Soup", StrategyDishQuality.Poor, 1.00f,
                I(StrategyResourceType.Roots, 1), I(StrategyResourceType.Onion, 1)),
            R("turnip_soup", "Turnip Soup", StrategyDishQuality.Poor, 0.95f,
                I(StrategyResourceType.Turnip, 1), I(StrategyResourceType.Onion, 1)),
            R("berry_mash", "Berry Mash", StrategyDishQuality.Poor, 0.95f,
                I(StrategyResourceType.Berries, 1), I(StrategyResourceType.Roots, 1)),
            R("potato_broth", "Potato Broth", StrategyDishQuality.Common, 1.30f,
                I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1)),
            R("cabbage_soup", "Cabbage Soup", StrategyDishQuality.Common, 1.10f,
                I(StrategyResourceType.Cabbage, 1), I(StrategyResourceType.Carrot, 1)),
            R("roasted_roots", "Roasted Roots", StrategyDishQuality.Common, 1.60f,
                I(StrategyResourceType.Roots, 1), I(StrategyResourceType.Potato, 1)),
            R("egg_mash", "Egg Mash", StrategyDishQuality.Common, 1.75f,
                I(StrategyResourceType.Eggs, 1), I(StrategyResourceType.Potato, 1)),
            R("egg_and_greens", "Egg and Greens", StrategyDishQuality.Common, 1.35f,
                I(StrategyResourceType.Eggs, 1), I(StrategyResourceType.Cabbage, 1)),
            R("vegetable_hash", "Vegetable Hash", StrategyDishQuality.Common, 1.80f,
                I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1), I(StrategyResourceType.Carrot, 1)),
            R("foragers_bowl", "Forager's Bowl", StrategyDishQuality.Common, 1.55f,
                I(StrategyResourceType.Berries, 1), I(StrategyResourceType.Mushrooms, 1), I(StrategyResourceType.Roots, 1)),
            R("fish_soup", "Fish Soup", StrategyDishQuality.Common, 1.60f,
                I(StrategyResourceType.Fish, 1), I(StrategyResourceType.Onion, 1)),
            R("garden_stew", "Garden Stew", StrategyDishQuality.Hearty, 1.75f,
                I(StrategyResourceType.Turnip, 1), I(StrategyResourceType.Cabbage, 1), I(StrategyResourceType.Carrot, 1)),
            R("mushroom_pottage", "Mushroom Pottage", StrategyDishQuality.Hearty, 2.00f,
                I(StrategyResourceType.Mushrooms, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1)),
            R("fish_and_roots", "Fish and Roots", StrategyDishQuality.Hearty, 2.00f,
                I(StrategyResourceType.Fish, 1), I(StrategyResourceType.Roots, 1)),
            R("fish_stew", "Fish Stew", StrategyDishQuality.Hearty, 2.40f,
                I(StrategyResourceType.Fish, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1)),
            R("fishers_pottage", "Fisher's Pottage", StrategyDishQuality.Hearty, 2.20f,
                I(StrategyResourceType.Fish, 1), I(StrategyResourceType.Cabbage, 1), I(StrategyResourceType.Onion, 1)),
            R("game_roast", "Game Roast", StrategyDishQuality.Hearty, 2.50f,
                I(StrategyResourceType.Game, 1), I(StrategyResourceType.Potato, 1)),
            R("game_stew", "Game Stew", StrategyDishQuality.Hearty, 2.50f,
                I(StrategyResourceType.Game, 1), I(StrategyResourceType.Carrot, 1), I(StrategyResourceType.Onion, 1)),
            R("hunters_pot", "Hunter's Pot", StrategyDishQuality.Fine, 2.70f,
                I(StrategyResourceType.Game, 1), I(StrategyResourceType.Mushrooms, 1), I(StrategyResourceType.Onion, 1)),
            R("hearty_pottage", "Hearty Pottage", StrategyDishQuality.Fine, 3.10f,
                I(StrategyResourceType.Game, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Cabbage, 1)),
            R("fine_fish_dish", "Fine Fish Dish", StrategyDishQuality.Fine, 3.00f,
                I(StrategyResourceType.Fish, 1), I(StrategyResourceType.Eggs, 1), I(StrategyResourceType.Onion, 1), I(StrategyResourceType.Carrot, 1)),
            R("fine_game_dish", "Fine Game Dish", StrategyDishQuality.Fine, 3.60f,
                I(StrategyResourceType.Game, 1), I(StrategyResourceType.Eggs, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1)),
            R("fishers_feast", "Fisher's Feast", StrategyDishQuality.Feast, 4.80f,
                I(StrategyResourceType.Fish, 2), I(StrategyResourceType.Eggs, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1)),
            R("hunters_feast", "Hunter's Feast", StrategyDishQuality.Feast, 5.00f,
                I(StrategyResourceType.Game, 2), I(StrategyResourceType.Mushrooms, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Onion, 1)),
            R("settlement_feast", "Settlement Feast", StrategyDishQuality.Feast, 5.40f,
                I(StrategyResourceType.Game, 1), I(StrategyResourceType.Fish, 1), I(StrategyResourceType.Eggs, 1), I(StrategyResourceType.Potato, 1), I(StrategyResourceType.Cabbage, 1))
        };

        public static IReadOnlyList<StrategyDishRecipe> Recipes => recipes;
        public static StrategyDishRecipe FallbackRecipe => recipes[0];

        public static StrategyDishRecipe FindById(string id)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                if (string.Equals(recipes[i].Id, id, StringComparison.Ordinal))
                {
                    return recipes[i];
                }
            }

            return null;
        }

        public static StrategyDishRecipe ChooseBestAvailable(
            IReadOnlyDictionary<StrategyResourceType, int> available,
            float remainingTargetRations)
        {
            StrategyDishRecipe best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < recipes.Length; i++)
            {
                StrategyDishRecipe recipe = recipes[i];
                if (!HasIngredients(recipe, available))
                {
                    continue;
                }

                float score = GetCookingScore(recipe, remainingTargetRations);
                if (best == null || score > bestScore)
                {
                    best = recipe;
                    bestScore = score;
                }
            }

            return best;
        }

        public static int CompareServingPriority(StrategyDishRecipe left, StrategyDishRecipe right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int quality = right.Quality.CompareTo(left.Quality);
            if (quality != 0)
            {
                return quality;
            }

            return right.RationValue.CompareTo(left.RationValue);
        }

        public static string GetQualityLabel(StrategyDishQuality quality)
        {
            return quality switch
            {
                StrategyDishQuality.Poor => "Poor",
                StrategyDishQuality.Common => "Common",
                StrategyDishQuality.Hearty => "Hearty",
                StrategyDishQuality.Fine => "Fine",
                StrategyDishQuality.Feast => "Feast",
                _ => "Unknown"
            };
        }

        public static bool HasIngredients(
            StrategyDishRecipe recipe,
            IReadOnlyDictionary<StrategyResourceType, int> available)
        {
            if (recipe == null || available == null)
            {
                return false;
            }

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                StrategyDishIngredient ingredient = recipe.Ingredients[i];
                available.TryGetValue(ingredient.Resource, out int amount);
                if (amount < ingredient.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GetCookingScore(StrategyDishRecipe recipe, float remainingTargetRations)
        {
            float score = ((int)recipe.Quality + 1) * 1000f
                + recipe.RationValue * 50f
                + recipe.IngredientUnitCount;
            if (remainingTargetRations > 0.01f && recipe.RationValue > remainingTargetRations + 0.25f)
            {
                score -= (recipe.RationValue - remainingTargetRations) * 900f;
            }

            return score;
        }

        private static StrategyDishRecipe R(
            string id,
            string displayName,
            StrategyDishQuality quality,
            float rationValue,
            params StrategyDishIngredient[] ingredients)
        {
            return new StrategyDishRecipe(id, displayName, quality, rationValue, ingredients);
        }

        private static StrategyDishIngredient I(StrategyResourceType resource, int amount)
        {
            return new StrategyDishIngredient(resource, amount);
        }
    }
}
