using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        public static bool CanReceiveTradeResource(StrategyResourceType resource, Vector3 nearWorld)
        {
            return IsTradeStorageResource(resource) && TryFindNearestStorageYard(nearWorld, out _);
        }

        public static bool TryAddTradeResource(StrategyResourceType resource, int amount, Vector3 nearWorld)
        {
            if (!IsTradeStorageResource(resource) || amount <= 0)
            {
                return false;
            }

            if (!TryFindNearestStorageYard(nearWorld, out StrategyStorageYard yard))
            {
                return false;
            }

            yard.AddResource(resource, amount);
            StrategyDebugLogger.Info(
                "Trade",
                "StorageTradeResourceAdded",
                StrategyDebugLogger.F("yardOrigin", yard.Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount));
            return true;
        }

        public static bool TrySpendTradeResource(
            StrategyResourceType resource,
            int amount,
            Vector3 nearWorld,
            out int spent)
        {
            spent = 0;
            if (!IsTradeStorageResource(resource) || amount <= 0)
            {
                return false;
            }

            int available = GetTotalAvailableLogisticsAmount(resource);
            if (available < amount)
            {
                return false;
            }

            List<StrategyStorageYard> yards = GetYardsSortedByDistance(nearWorld);
            int remaining = amount;
            for (int i = 0; i < yards.Count && remaining > 0; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null)
                {
                    continue;
                }

                int taken = Mathf.Min(remaining, yard.GetAvailableLogisticsAmount(resource));
                if (taken <= 0)
                {
                    continue;
                }

                yard.SpendLogisticsAmount(resource, taken);
                remaining -= taken;
                spent += taken;
            }

            bool success = spent == amount;
            StrategyDebugLogger.Info(
                "Trade",
                success ? "StorageTradeResourceSpent" : "StorageTradeResourceSpendShort",
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("requested", amount),
                StrategyDebugLogger.F("spent", spent),
                StrategyDebugLogger.F("remaining", remaining));
            return success;
        }

        private static bool IsTradeStorageResource(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Logs
                || resource == StrategyResourceType.Stone
                || resource == StrategyResourceType.Iron
                || resource == StrategyResourceType.Coal
                || resource == StrategyResourceType.Clay
                || resource == StrategyResourceType.Pottery
                || resource == StrategyResourceType.Planks
                || resource == StrategyResourceType.Tools;
        }
    }
}
