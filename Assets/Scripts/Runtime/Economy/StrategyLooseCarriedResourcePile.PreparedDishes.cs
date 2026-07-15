using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyLooseResourcePickup
    {
        public StrategyLooseResourcePickup(
            StrategyResourceType resource,
            int amount,
            string preparedDishRecipeId,
            int preparedDishAmount,
            float preparedDishLeftoverRations)
        {
            Resource = resource;
            Amount = amount;
            PreparedDishRecipeId = preparedDishRecipeId ?? string.Empty;
            PreparedDishAmount = Mathf.Max(0, preparedDishAmount);
            PreparedDishLeftoverRations = float.IsNaN(preparedDishLeftoverRations)
                || float.IsInfinity(preparedDishLeftoverRations)
                    ? 0f
                    : Mathf.Max(0f, preparedDishLeftoverRations);
        }

        public StrategyResourceType Resource { get; }
        public int Amount { get; }
        public string PreparedDishRecipeId { get; }
        public int PreparedDishAmount { get; }
        public float PreparedDishLeftoverRations { get; }
        public bool HasPreparedDishState => Resource == StrategyResourceType.Dish
            && (PreparedDishAmount > 0 || PreparedDishLeftoverRations > 0f);
    }

    public sealed partial class StrategyLooseCarriedResourcePile
    {
        private string preparedDishRecipeId = string.Empty;
        private int preparedDishAmount;
        private float preparedDishLeftoverRations;

        public string PreparedDishRecipeId => preparedDishRecipeId;
        public int PreparedDishAmount => preparedDishAmount;
        public float PreparedDishLeftoverRations => preparedDishLeftoverRations;
        public bool HasPreparedDishPayload => resource == StrategyResourceType.Dish
            && (preparedDishAmount > 0 || preparedDishLeftoverRations > 0f);

        public float GetPreparedDishRations(bool availableOnly)
        {
            if (!HasPreparedDishPayload)
            {
                return 0f;
            }

            int countedDishes = availableOnly
                ? Mathf.Min(preparedDishAmount, resourceStore.GetAvailable(StrategyResourceType.Dish))
                : preparedDishAmount;
            StrategyDishRecipe recipe = countedDishes > 0
                ? StrategyDishRecipeCatalog.FindById(preparedDishRecipeId)
                : null;
            float rations = recipe != null ? countedDishes * recipe.RationValue : 0f;
            if (!availableOnly || reservedBy == null)
            {
                rations += preparedDishLeftoverRations;
            }

            return rations;
        }

        public static StrategyLooseCarriedResourcePile CreatePreparedDishes(
            CityMapController map,
            Vector2Int origin,
            Vector3 world,
            string recipeId,
            int dishAmount,
            float leftoverRations)
        {
            int safeDishAmount = Mathf.Max(0, dishAmount);
            float safeLeftoverRations = IsFinite(leftoverRations)
                ? Mathf.Max(0f, leftoverRations)
                : 0f;
            StrategyDishRecipe recipe = safeDishAmount > 0
                ? StrategyDishRecipeCatalog.FindById(recipeId)
                : null;
            if ((safeDishAmount > 0 && recipe == null)
                || (safeDishAmount <= 0 && safeLeftoverRations <= 0f))
            {
                return null;
            }

            int transportAmount = Mathf.Max(1, safeDishAmount);
            StrategyLooseCarriedResourcePile pile = Create(
                map,
                origin,
                world,
                StrategyResourceType.Dish,
                transportAmount);
            if (pile == null)
            {
                return null;
            }

            pile.preparedDishRecipeId = recipe != null ? recipe.Id : string.Empty;
            pile.preparedDishAmount = safeDishAmount;
            pile.preparedDishLeftoverRations = safeLeftoverRations;
            return pile;
        }

        public bool TryTakeReserved(object owner, out StrategyLooseResourcePickup pickup)
        {
            pickup = default;
            if (owner == null || reservedBy != owner || amount <= 0)
            {
                return false;
            }

            int takenAmount = Mathf.Min(amount, reservedAmount);
            int takenPreparedDishAmount = 0;
            string takenPreparedDishRecipeId = string.Empty;
            float takenLeftoverRations = 0f;
            if (HasPreparedDishPayload)
            {
                takenPreparedDishAmount = Mathf.Min(preparedDishAmount, takenAmount);
                if (takenPreparedDishAmount > 0)
                {
                    takenPreparedDishRecipeId = preparedDishRecipeId;
                    preparedDishAmount -= takenPreparedDishAmount;
                }

                takenLeftoverRations = preparedDishLeftoverRations;
                preparedDishLeftoverRations = 0f;
            }

            pickup = new StrategyLooseResourcePickup(
                resource,
                takenAmount,
                takenPreparedDishRecipeId,
                takenPreparedDishAmount,
                takenLeftoverRations);
            amount = Mathf.Max(0, amount - takenAmount);
            reservedBy = null;
            reservedAmount = 0;
            StrategyDebugLogger.Info(
                "Logistics",
                "LooseCarriedResourceTaken",
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("resource", pickup.Resource),
                StrategyDebugLogger.F("amount", pickup.Amount),
                StrategyDebugLogger.F("preparedDishRecipe", pickup.PreparedDishRecipeId),
                StrategyDebugLogger.F("preparedDishAmount", pickup.PreparedDishAmount),
                StrategyDebugLogger.F("leftoverRations", pickup.PreparedDishLeftoverRations),
                StrategyDebugLogger.F("owner", owner));
            if (amount <= 0)
            {
                Destroy(gameObject);
            }
            else
            {
                UpdateVisual();
            }

            return takenAmount > 0;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
