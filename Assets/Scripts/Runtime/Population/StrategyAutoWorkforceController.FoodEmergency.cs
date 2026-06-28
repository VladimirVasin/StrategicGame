namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private bool HasHouseholdFoodEmergency()
        {
            for (int i = 0; i < cachedPlacedBuildings.Length; i++)
            {
                StrategyPlacedBuilding house = cachedPlacedBuildings[i];
                if (house == null || house.Tool != StrategyBuildTool.House)
                {
                    continue;
                }

                StrategyHouseholdFoodState food = house.GetComponent<StrategyHouseholdFoodState>();
                if (food != null && (food.IsStarving || food.IsBirthBlocked))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
