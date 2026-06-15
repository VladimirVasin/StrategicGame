using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyAutoWorkforceCategory
    {
        Construction,
        Food,
        Logistics,
        Wood,
        Stone,
        Planks,
        Iron,
        Coal
    }

    public sealed class StrategyAutoWorkforceSettings
    {
        public const int MinPriority = 0;
        public const int MaxPriority = 5;

        private readonly int[] priorities =
        {
            4,
            4,
            4,
            3,
            3,
            2,
            1,
            1
        };

        public bool Enabled { get; private set; } = true;

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public int GetPriority(StrategyAutoWorkforceCategory category)
        {
            int index = (int)category;
            return index >= 0 && index < priorities.Length ? priorities[index] : 0;
        }

        public int SetPriority(StrategyAutoWorkforceCategory category, int value)
        {
            int index = (int)category;
            if (index < 0 || index >= priorities.Length)
            {
                return 0;
            }

            priorities[index] = Mathf.Clamp(value, MinPriority, MaxPriority);
            return priorities[index];
        }

        public int AdjustPriority(StrategyAutoWorkforceCategory category, int delta)
        {
            return SetPriority(category, GetPriority(category) + delta);
        }

        public static string GetLabel(StrategyAutoWorkforceCategory category)
        {
            return category switch
            {
                StrategyAutoWorkforceCategory.Construction => "Construction",
                StrategyAutoWorkforceCategory.Food => "Food",
                StrategyAutoWorkforceCategory.Logistics => "Logistics",
                StrategyAutoWorkforceCategory.Wood => "Wood",
                StrategyAutoWorkforceCategory.Stone => "Stone",
                StrategyAutoWorkforceCategory.Planks => "Planks",
                StrategyAutoWorkforceCategory.Iron => "Iron",
                StrategyAutoWorkforceCategory.Coal => "Coal",
                _ => "Priority"
            };
        }
    }
}
