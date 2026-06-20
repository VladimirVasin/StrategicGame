using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHouseResourceStore : MonoBehaviour
    {
        public static readonly StrategyResourceType[] DisplayOrder =
        {
            StrategyResourceType.Dish,
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

        public bool HasAny
        {
            get
            {
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
                total += GetAmount(type) * StrategyFoodNutrition.GetRationValue(type);
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
            return GetAmount(StrategyResourceType.Dish);
        }

        public float GetPreparedDishRations()
        {
            return GetPreparedDishAmount() * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
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

        public int ConsumePreparedDishes(float requestedRations, out float suppliedRations)
        {
            suppliedRations = 0f;
            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            if (dishRation <= 0f || requestedRations <= 0.01f)
            {
                return 0;
            }

            int available = GetPreparedDishAmount();
            int requestedDishes = Mathf.CeilToInt(requestedRations / dishRation);
            int taken = Mathf.Min(available, requestedDishes);
            if (taken <= 0)
            {
                return 0;
            }

            int nextAmount = available - taken;
            if (nextAmount > 0)
            {
                amounts[StrategyResourceType.Dish] = nextAmount;
            }
            else
            {
                amounts.Remove(StrategyResourceType.Dish);
            }

            suppliedRations = taken * dishRation;
            return taken;
        }

        public bool TryCookDishes(
            int requestedDishes,
            out int dishesCooked,
            out int consumedIngredients,
            out float consumedIngredientRations)
        {
            dishesCooked = 0;
            consumedIngredients = 0;
            consumedIngredientRations = 0f;
            float dishRation = StrategyFoodNutrition.GetRationValue(StrategyResourceType.Dish);
            int targetDishes = Mathf.Max(0, requestedDishes);
            if (targetDishes <= 0 || dishRation <= 0f)
            {
                return false;
            }

            int possibleDishes = Mathf.FloorToInt(GetTotalIngredientRationValue() / dishRation);
            targetDishes = Mathf.Min(targetDishes, possibleDishes);
            if (targetDishes <= 0)
            {
                return false;
            }

            consumedIngredients = ConsumeIngredientRations(targetDishes * dishRation, out consumedIngredientRations);
            dishesCooked = Mathf.Min(targetDishes, Mathf.FloorToInt((consumedIngredientRations + 0.001f) / dishRation));
            if (dishesCooked <= 0)
            {
                return false;
            }

            AddResource(StrategyResourceType.Dish, dishesCooked);
            return true;
        }

        public int ConsumeIngredientRations(float requestedRations, out float suppliedRations)
        {
            suppliedRations = 0f;
            float remaining = Mathf.Max(0f, requestedRations);
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
                suppliedRations += supplied;
                remaining = Mathf.Max(0f, remaining - supplied);

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
            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }
    }
}
