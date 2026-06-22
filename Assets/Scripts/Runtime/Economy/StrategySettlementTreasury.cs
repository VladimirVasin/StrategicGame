using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategySettlementTreasury : MonoBehaviour
    {
        public static StrategySettlementTreasury Active { get; private set; }

        public int Coins { get; private set; }

        public void Configure(int initialCoins)
        {
            Active = this;
            Coins = Mathf.Max(0, initialCoins);
            StrategyDebugLogger.Info(
                "Trade",
                "TreasuryConfigured",
                StrategyDebugLogger.F("coins", Coins));
        }

        public void AddCoins(int amount, string reason)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins += amount;
            StrategyDebugLogger.Info(
                "Trade",
                "CoinsAdded",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("coins", Coins));
        }

        public bool TrySpendCoins(int amount, string reason)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (Coins < amount)
            {
                StrategyDebugLogger.Warn(
                    "Trade",
                    "CoinsSpendRejected",
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("cost", amount),
                    StrategyDebugLogger.F("coins", Coins));
                return false;
            }

            Coins -= amount;
            StrategyDebugLogger.Info(
                "Trade",
                "CoinsSpent",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("coins", Coins));
            return true;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
