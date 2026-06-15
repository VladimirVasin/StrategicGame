using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHouseResourceStore : MonoBehaviour
    {
        public static readonly StrategyResourceType[] DisplayOrder =
        {
            StrategyResourceType.Eggs,
            StrategyResourceType.Turnip,
            StrategyResourceType.Cabbage,
            StrategyResourceType.Onion,
            StrategyResourceType.Carrot,
            StrategyResourceType.Potato,
            StrategyResourceType.Berries,
            StrategyResourceType.Roots,
            StrategyResourceType.Mushrooms
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

        public int ConsumeFood(int requested)
        {
            int remaining = Mathf.Max(0, requested);
            if (remaining <= 0)
            {
                return 0;
            }

            for (int i = 0; i < DisplayOrder.Length && remaining > 0; i++)
            {
                StrategyResourceType type = DisplayOrder[i];
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

        public int GetAmount(StrategyResourceType type)
        {
            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }
    }
}
