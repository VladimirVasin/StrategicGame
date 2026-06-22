using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyTradeTransactionService
    {
        public static bool CanExecute(StrategyTradeOffer offer, Vector3 nearWorld)
        {
            if (!offer.IsValid)
            {
                return false;
            }

            return offer.Direction == StrategyTradeDirection.PlayerSells
                ? GetAvailableStock(offer.Resource) >= offer.Amount
                : HasCoins(offer.TotalCoins) && CanReceive(offer.Resource, nearWorld);
        }

        public static int GetAvailableStock(StrategyResourceType resource)
        {
            if (StrategyFoodNutrition.IsFood(resource))
            {
                return StrategyGranary.GetTotalAvailableTradeFood(resource);
            }

            return StrategyStorageYard.GetTotalAvailableLogisticsAmount(resource);
        }

        public static bool TryExecute(
            StrategyTradeOffer offer,
            Vector3 nearWorld,
            out string result)
        {
            result = string.Empty;
            if (!offer.IsValid)
            {
                result = "Invalid offer.";
                return false;
            }

            if (offer.Direction == StrategyTradeDirection.PlayerSells)
            {
                return TrySellToCaravan(offer, nearWorld, out result);
            }

            return TryBuyFromCaravan(offer, nearWorld, out result);
        }

        private static bool TrySellToCaravan(
            StrategyTradeOffer offer,
            Vector3 nearWorld,
            out string result)
        {
            if (!TrySpendStock(offer.Resource, offer.Amount, nearWorld, out int spent))
            {
                result = "Not enough stock.";
                return false;
            }

            StrategySettlementTreasury.Active?.AddCoins(offer.TotalCoins, "trade_sell");
            result = "Sold " + spent + " " + offer.Resource + ".";
            LogTrade("TradeSoldToCaravan", offer, spent);
            return true;
        }

        private static bool TryBuyFromCaravan(
            StrategyTradeOffer offer,
            Vector3 nearWorld,
            out string result)
        {
            StrategySettlementTreasury treasury = StrategySettlementTreasury.Active;
            if (treasury == null || !treasury.TrySpendCoins(offer.TotalCoins, "trade_buy"))
            {
                result = "Not enough coins.";
                return false;
            }

            if (!TryAddStock(offer.Resource, offer.Amount, nearWorld))
            {
                treasury.AddCoins(offer.TotalCoins, "trade_buy_refund");
                result = StrategyFoodNutrition.IsFood(offer.Resource)
                    ? "No Granary can receive this."
                    : "No Storage Yard can receive this.";
                return false;
            }

            result = "Bought " + offer.Amount + " " + offer.Resource + ".";
            LogTrade("TradeBoughtFromCaravan", offer, offer.Amount);
            return true;
        }

        private static bool TrySpendStock(
            StrategyResourceType resource,
            int amount,
            Vector3 nearWorld,
            out int spent)
        {
            return StrategyFoodNutrition.IsFood(resource)
                ? StrategyGranary.TrySpendTradeFood(resource, amount, nearWorld, out spent)
                : StrategyStorageYard.TrySpendTradeResource(resource, amount, nearWorld, out spent);
        }

        private static bool TryAddStock(StrategyResourceType resource, int amount, Vector3 nearWorld)
        {
            return StrategyFoodNutrition.IsFood(resource)
                ? StrategyGranary.TryAddTradeFood(resource, amount, nearWorld)
                : StrategyStorageYard.TryAddTradeResource(resource, amount, nearWorld);
        }

        private static bool CanReceive(StrategyResourceType resource, Vector3 nearWorld)
        {
            return StrategyFoodNutrition.IsFood(resource)
                ? StrategyGranary.CanReceiveTradeFood(resource, nearWorld)
                : StrategyStorageYard.CanReceiveTradeResource(resource, nearWorld);
        }

        private static bool HasCoins(int amount)
        {
            StrategySettlementTreasury treasury = StrategySettlementTreasury.Active;
            return treasury != null && treasury.Coins >= amount;
        }

        private static void LogTrade(string eventName, StrategyTradeOffer offer, int moved)
        {
            StrategyDebugLogger.Info(
                "Trade",
                eventName,
                StrategyDebugLogger.F("resource", offer.Resource),
                StrategyDebugLogger.F("amount", offer.Amount),
                StrategyDebugLogger.F("moved", moved),
                StrategyDebugLogger.F("coins", offer.TotalCoins),
                StrategyDebugLogger.F("direction", offer.Direction));
        }
    }
}
