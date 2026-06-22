namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyDishCookingSummary
    {
        public StrategyDishCookingSummary(
            string recipesText,
            StrategyDishQuality bestQuality,
            float producedRations)
        {
            RecipesText = recipesText ?? string.Empty;
            BestQuality = bestQuality;
            ProducedRations = producedRations;
        }

        public string RecipesText { get; }
        public StrategyDishQuality BestQuality { get; }
        public float ProducedRations { get; }

        public static StrategyDishCookingSummary Empty { get; } = new(
            string.Empty,
            StrategyDishQuality.Poor,
            0f);
    }
}
