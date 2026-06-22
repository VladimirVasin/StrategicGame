namespace ProjectUnknown.Strategy
{
    public readonly struct StrategyPreparedDishStack
    {
        public StrategyPreparedDishStack(StrategyDishRecipe recipe, int amount)
        {
            Recipe = recipe;
            Amount = amount;
        }

        public StrategyDishRecipe Recipe { get; }
        public int Amount { get; }
        public float Rations => Recipe != null ? Recipe.RationValue * Amount : 0f;
    }
}
