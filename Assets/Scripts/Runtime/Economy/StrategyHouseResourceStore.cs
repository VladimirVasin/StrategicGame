using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyHouseResourceStore : MonoBehaviour
    {
        public static readonly StrategyResourceType[] DisplayOrder =
        {
            StrategyResourceType.Dish,
            StrategyResourceType.Pottery,
            StrategyResourceType.Eggs,
            StrategyResourceType.Turnip,
            StrategyResourceType.Cabbage,
            StrategyResourceType.Onion,
            StrategyResourceType.Carrot,
            StrategyResourceType.Potato,
            StrategyResourceType.Berries,
            StrategyResourceType.Roots,
            StrategyResourceType.Mushrooms,
            StrategyResourceType.Fish,
            StrategyResourceType.Game
        };

        private static readonly StrategyResourceType[] IngredientConsumptionOrder =
        {
            StrategyResourceType.Onion,
            StrategyResourceType.Berries,
            StrategyResourceType.Cabbage,
            StrategyResourceType.Carrot,
            StrategyResourceType.Mushrooms,
            StrategyResourceType.Turnip,
            StrategyResourceType.Roots,
            StrategyResourceType.Eggs,
            StrategyResourceType.Potato,
            StrategyResourceType.Fish,
            StrategyResourceType.Game
        };

        private readonly Dictionary<StrategyResourceType, int> amounts = new();
        private readonly Dictionary<string, int> preparedDishAmounts = new();
        private float leftoverRations;

        public float LeftoverRations => leftoverRations;

        public bool HasAny
        {
            get
            {
                if (leftoverRations > 0.01f)
                {
                    return true;
                }

                for (int i = 0; i < DisplayOrder.Length; i++)
                {
                    if (GetAmount(DisplayOrder[i]) > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void AddResource(StrategyResourceType type, int amount)
        {
            if (type == StrategyResourceType.None || amount <= 0)
            {
                return;
            }

            if (type == StrategyResourceType.Dish)
            {
                AddPreparedDish(StrategyDishRecipeCatalog.FallbackRecipe, amount);
                return;
            }

            amounts.TryGetValue(type, out int current);
            amounts[type] = current + amount;
        }

        public int GetTotalFoodAmount()
        {
            int total = 0;
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                total += GetAmount(DisplayOrder[i]);
            }

            return total;
        }

        public float GetTotalRationValue()
        {
            float total = 0f;
            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                StrategyResourceType type = DisplayOrder[i];
                total += type == StrategyResourceType.Dish
                    ? GetPreparedDishRations()
                    : GetAmount(type) * StrategyFoodNutrition.GetRationValue(type);
            }

            return total;
        }

        public int GetTotalIngredientAmount()
        {
            int total = 0;
            for (int i = 0; i < IngredientConsumptionOrder.Length; i++)
            {
                total += GetAmount(IngredientConsumptionOrder[i]);
            }

            return total;
        }

        public float GetTotalIngredientRationValue()
        {
            float total = 0f;
            for (int i = 0; i < IngredientConsumptionOrder.Length; i++)
            {
                StrategyResourceType type = IngredientConsumptionOrder[i];
                total += GetAmount(type) * StrategyFoodNutrition.GetRationValue(type);
            }

            return total;
        }

        public int GetPreparedDishAmount()
        {
            int total = 0;
            foreach (int amount in preparedDishAmounts.Values)
            {
                total += Mathf.Max(0, amount);
            }

            return total;
        }

        public float GetPreparedDishRations()
        {
            float total = leftoverRations;
            foreach (KeyValuePair<string, int> stack in preparedDishAmounts)
            {
                StrategyDishRecipe recipe = StrategyDishRecipeCatalog.FindById(stack.Key);
                if (recipe != null)
                {
                    total += Mathf.Max(0, stack.Value) * recipe.RationValue;
                }
            }

            return total;
        }

        public int GetPotteryAmount()
        {
            return GetAmount(StrategyResourceType.Pottery);
        }

        public int GetLogsAmount()
        {
            return GetAmount(StrategyResourceType.Logs);
        }

        public int GetCookableDishCountByIngredients()
        {
            return CountCookableDishUnits(0f);
        }

        public int GetPotteryDemandForCooking(float targetRations)
        {
            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            if (dishRation <= 0f || targetRations <= 0.01f)
            {
                return 0;
            }

            float missingRations = Mathf.Max(0f, targetRations - GetPreparedDishRations());
            int desiredDishes = CountCookableDishUnits(missingRations);
            return Mathf.Max(0, desiredDishes - GetPotteryAmount());
        }

        public int ConsumeFood(int requested)
        {
            int remaining = Mathf.Max(0, requested);
            if (remaining <= 0)
            {
                return 0;
            }

            for (int i = 0; i < IngredientConsumptionOrder.Length && remaining > 0; i++)
            {
                StrategyResourceType type = IngredientConsumptionOrder[i];
                int available = GetAmount(type);
                if (available <= 0)
                {
                    continue;
                }

                int taken = Mathf.Min(available, remaining);
                remaining -= taken;
                int nextAmount = available - taken;
                if (nextAmount > 0)
                {
                    amounts[type] = nextAmount;
                }
                else
                {
                    amounts.Remove(type);
                }
            }

            return requested - remaining;
        }

        public int ConsumeRations(float requestedRations, out float suppliedRations)
        {
            return ConsumeIngredientRations(requestedRations, out suppliedRations);
        }

        public int ConsumeIngredientRations(float requestedRations, out float suppliedRations)
        {
            suppliedRations = 0f;
            float remaining = Mathf.Max(0f, requestedRations);
            ConsumeLeftoverRations(ref remaining, ref suppliedRations);
            if (remaining <= 0.01f)
            {
                return 0;
            }

            int consumedUnits = 0;
            for (int i = 0; i < IngredientConsumptionOrder.Length && remaining > 0.01f; i++)
            {
                StrategyResourceType type = IngredientConsumptionOrder[i];
                float rationValue = StrategyFoodNutrition.GetRationValue(type);
                if (rationValue <= 0f)
                {
                    continue;
                }

                int available = GetAmount(type);
                if (available <= 0)
                {
                    continue;
                }

                int requestedUnits = Mathf.CeilToInt(remaining / rationValue);
                int taken = Mathf.Min(available, requestedUnits);
                if (taken <= 0)
                {
                    continue;
                }

                consumedUnits += taken;
                float supplied = taken * rationValue;
                float used = Mathf.Min(remaining, supplied);
                suppliedRations += used;
                AddLeftoverRations(supplied - used);
                remaining = Mathf.Max(0f, remaining - used);

                int nextAmount = available - taken;
                if (nextAmount > 0)
                {
                    amounts[type] = nextAmount;
                }
                else
                {
                    amounts.Remove(type);
                }
            }

            return consumedUnits;
        }

        public int GetAmount(StrategyResourceType type)
        {
            if (type == StrategyResourceType.Dish)
            {
                return GetPreparedDishAmount();
            }

            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }

        public int ConsumeResource(StrategyResourceType type, int requested)
        {
            if (type == StrategyResourceType.None || type == StrategyResourceType.Dish || requested <= 0)
            {
                return 0;
            }

            int available = GetAmount(type);
            int taken = Mathf.Min(available, requested);
            if (taken <= 0)
            {
                return 0;
            }

            int nextAmount = available - taken;
            if (nextAmount > 0)
            {
                amounts[type] = nextAmount;
            }
            else
            {
                amounts.Remove(type);
            }

            return taken;
        }

        private void ConsumeLeftoverRations(ref float remainingRations, ref float suppliedRations)
        {
            if (leftoverRations <= 0.01f || remainingRations <= 0.01f)
            {
                return;
            }

            float taken = Mathf.Min(leftoverRations, remainingRations);
            leftoverRations = Mathf.Max(0f, leftoverRations - taken);
            suppliedRations += taken;
            remainingRations = Mathf.Max(0f, remainingRations - taken);
        }

        private void AddLeftoverRations(float rations)
        {
            if (rations <= 0.01f)
            {
                return;
            }

            leftoverRations += rations;
        }
    }
}
