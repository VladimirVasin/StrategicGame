namespace ProjectUnknown.Strategy
{
    public static class StrategyTradeOfferCatalog
    {
        public static StrategyTradeOffer[] CreateDefaultOffers()
        {
            return new[]
            {
                new StrategyTradeOffer(StrategyTradeDirection.PlayerSells, StrategyResourceType.Logs, 4, 8),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerSells, StrategyResourceType.Stone, 4, 10),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerSells, StrategyResourceType.Planks, 2, 14),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerSells, StrategyResourceType.Pottery, 2, 18),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerSells, StrategyResourceType.Tools, 1, 24),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerBuys, StrategyResourceType.Iron, 2, 18),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerBuys, StrategyResourceType.Coal, 3, 18),
                new StrategyTradeOffer(StrategyTradeDirection.PlayerBuys, StrategyResourceType.Fish, 2, 14)
            };
        }
    }
}
