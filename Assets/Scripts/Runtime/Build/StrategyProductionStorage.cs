using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyProductionStorage
    {
        public const int LocalCapacity = 6;
        public const int ProcessingInputCapacity = LocalCapacity;
        public const int ProcessingOutputCapacity = LocalCapacity;
        public const int ProcessingTotalCapacity = ProcessingInputCapacity + ProcessingOutputCapacity;
        public const int HaulerCarryLimit = 2;
        public const int BuilderCarryLimit = 2;

        public static int GetRemaining(int used)
        {
            return Mathf.Max(0, LocalCapacity - Mathf.Max(0, used));
        }

        public static bool CanAccept(int used, int amount)
        {
            return amount > 0 && GetRemaining(used) >= amount;
        }

        public static int AddCapped(int current, int used, int amount, out int accepted)
        {
            accepted = Mathf.Min(Mathf.Max(0, amount), GetRemaining(used));
            return current + accepted;
        }

        public static string Format(int stored)
        {
            return stored + "/" + LocalCapacity;
        }
    }
}
