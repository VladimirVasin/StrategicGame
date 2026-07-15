using System;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private static bool ValidateLooseResources(StrategySaveData data, out string reason)
        {
            for (int i = 0; i < data.looseResources.Count; i++)
            {
                StrategyLooseResourceSaveData resource = data.looseResources[i];
                if (resource == null
                    || !IsCellInside(resource.originX, resource.originY, data.mapWidth, data.mapHeight))
                {
                    reason = "invalid_loose_resource_" + i;
                    return false;
                }

                bool validAmounts = resource.constructionPile
                    ? resource.logs >= 0 && resource.stone >= 0 && resource.planks >= 0
                    : resource.amount > 0
                        && Enum.IsDefined(typeof(StrategyResourceType), resource.resource)
                        && resource.resource != (int)StrategyResourceType.None;
                if (!validAmounts)
                {
                    reason = "invalid_loose_resource_amount_" + i;
                    return false;
                }

                bool hasPreparedDishMetadata = resource.preparedDishPile
                    || !string.IsNullOrEmpty(resource.preparedDishRecipeId)
                    || resource.preparedDishAmount != 0
                    || resource.preparedDishLeftoverRations != 0f;
                if (!hasPreparedDishMetadata)
                {
                    continue;
                }

                bool hasDishes = resource.preparedDishAmount > 0;
                bool hasLeftovers = resource.preparedDishLeftoverRations > 0f;
                bool validRecipe = hasDishes
                    ? !string.IsNullOrWhiteSpace(resource.preparedDishRecipeId)
                        && resource.preparedDishRecipeId.Length <= MaxSavePreparedDishIdLength
                        && StrategyDishRecipeCatalog.FindById(resource.preparedDishRecipeId) != null
                    : string.IsNullOrEmpty(resource.preparedDishRecipeId);
                if (!resource.preparedDishPile
                    || resource.constructionPile
                    || resource.resource != (int)StrategyResourceType.Dish
                    || !IsFinite(resource.preparedDishLeftoverRations)
                    || resource.preparedDishLeftoverRations < 0f
                    || resource.preparedDishAmount < 0
                    || resource.amount != Math.Max(1, resource.preparedDishAmount)
                    || !hasDishes && !hasLeftovers
                    || !validRecipe)
                {
                    reason = "invalid_loose_prepared_dish_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }
    }
}
