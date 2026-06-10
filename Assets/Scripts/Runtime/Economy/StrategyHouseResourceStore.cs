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
            StrategyResourceType.Potato
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

        public int GetAmount(StrategyResourceType type)
        {
            return amounts.TryGetValue(type, out int amount) ? amount : 0;
        }
    }
}
