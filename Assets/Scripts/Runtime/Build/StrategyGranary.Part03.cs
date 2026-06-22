using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyGranary
    {
        public static bool CanReceiveTradeFood(StrategyResourceType resource, Vector3 nearWorld)
        {
            return IsTradeFood(resource) && TryFindNearestGranary(nearWorld, out _);
        }

        public static bool TryAddTradeFood(StrategyResourceType resource, int amount, Vector3 nearWorld)
        {
            if (!IsTradeFood(resource) || amount <= 0)
            {
                return false;
            }

            if (!TryFindNearestGranary(nearWorld, out StrategyGranary granary))
            {
                return false;
            }

            if (resource == StrategyResourceType.Game)
            {
                granary.AddGame(amount);
            }
            else
            {
                granary.AddFish(amount);
            }

            StrategyDebugLogger.Info(
                "Trade",
                "GranaryTradeFoodAdded",
                StrategyDebugLogger.F("granaryOrigin", granary.Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount));
            return true;
        }

        public static bool TrySpendTradeFood(
            StrategyResourceType resource,
            int amount,
            Vector3 nearWorld,
            out int spent)
        {
            spent = 0;
            if (!IsTradeFood(resource) || amount <= 0)
            {
                return false;
            }

            StrategyGranary[] granaries = GetGranariesSortedByDistance(nearWorld);
            int available = 0;
            for (int i = 0; i < granaries.Length; i++)
            {
                StrategyGranary granary = granaries[i];
                available += granary != null ? granary.GetAvailableTradeFood(resource) : 0;
            }

            if (available < amount)
            {
                return false;
            }

            int remaining = amount;
            for (int i = 0; i < granaries.Length && remaining > 0; i++)
            {
                StrategyGranary granary = granaries[i];
                if (granary == null)
                {
                    continue;
                }

                int taken = Mathf.Min(remaining, granary.GetAvailableTradeFood(resource));
                if (taken <= 0)
                {
                    continue;
                }

                granary.SpendTradeFood(resource, taken);
                spent += taken;
                remaining -= taken;
            }

            StrategyDebugLogger.Info(
                "Trade",
                spent == amount ? "GranaryTradeFoodSpent" : "GranaryTradeFoodSpendShort",
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("requested", amount),
                StrategyDebugLogger.F("spent", spent));
            return spent == amount;
        }

        public static int GetTotalAvailableTradeFood(StrategyResourceType resource)
        {
            if (!IsTradeFood(resource))
            {
                return 0;
            }

            int total = 0;
            StrategyGranary[] granaries = Object.FindObjectsByType<StrategyGranary>();
            for (int i = 0; i < granaries.Length; i++)
            {
                total += granaries[i] != null ? granaries[i].GetAvailableTradeFood(resource) : 0;
            }

            return total;
        }

        private int GetAvailableTradeFood(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Game
                ? GetAvailableGameForHouseholds()
                : GetAvailableFishForHouseholds();
        }

        private void SpendTradeFood(StrategyResourceType resource, int amount)
        {
            if (resource == StrategyResourceType.Game)
            {
                gameStored = Mathf.Max(0, gameStored - amount);
            }
            else
            {
                fishStored = Mathf.Max(0, fishStored - amount);
            }

            UpdateStockVisual();
        }

        private static bool IsTradeFood(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Game || resource == StrategyResourceType.Fish;
        }
    }
}
