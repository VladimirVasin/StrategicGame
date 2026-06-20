namespace ProjectUnknown.Strategy
{
    public enum StrategyResourceType
    {
        None,
        Dish,
        Eggs,
        Turnip,
        Cabbage,
        Onion,
        Carrot,
        Potato,
        Berries,
        Roots,
        Mushrooms,
        Game,
        Fish,
        Stone,
        Iron,
        Coal,
        Clay,
        Pottery,
        Planks,
        Logs
    }

    public static class StrategyFoodNutrition
    {
        public static bool IsFood(StrategyResourceType type)
        {
            return GetRationValue(type) > 0f;
        }

        public static bool IsPreparedFood(StrategyResourceType type)
        {
            return type == StrategyResourceType.Dish;
        }

        public static bool IsIngredientFood(StrategyResourceType type)
        {
            return IsFood(type) && !IsPreparedFood(type);
        }

        public static float GetRationValue(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Dish => 1.00f,
                StrategyResourceType.Onion => 0.15f,
                StrategyResourceType.Berries => 0.25f,
                StrategyResourceType.Cabbage => 0.35f,
                StrategyResourceType.Carrot => 0.40f,
                StrategyResourceType.Mushrooms => 0.45f,
                StrategyResourceType.Turnip => 0.55f,
                StrategyResourceType.Roots => 0.65f,
                StrategyResourceType.Eggs => 0.70f,
                StrategyResourceType.Potato => 0.85f,
                StrategyResourceType.Fish => 1.10f,
                StrategyResourceType.Game => 1.50f,
                _ => 0f
            };
        }
    }
}
