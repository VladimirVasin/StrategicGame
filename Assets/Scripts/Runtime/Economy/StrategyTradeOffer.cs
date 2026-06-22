namespace ProjectUnknown.Strategy
{
    public enum StrategyTradeDirection
    {
        PlayerSells,
        PlayerBuys
    }

    public readonly struct StrategyTradeOffer
    {
        public StrategyTradeOffer(
            StrategyTradeDirection direction,
            StrategyResourceType resource,
            int amount,
            int totalCoins)
        {
            Direction = direction;
            Resource = resource;
            Amount = amount;
            TotalCoins = totalCoins;
        }

        public StrategyTradeDirection Direction { get; }
        public StrategyResourceType Resource { get; }
        public int Amount { get; }
        public int TotalCoins { get; }
        public bool IsValid => Resource != StrategyResourceType.None && Amount > 0 && TotalCoins > 0;
    }
}
