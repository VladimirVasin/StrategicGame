namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private static bool CanAffordBuildCost(StrategyConstructionResourceCost cost)
        {
            return StrategyDebugOptions.InstantConstructionEnabled
                || cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources());
        }

        private static string GetBuildItemBadgeText(StrategyConstructionResourceCost cost)
        {
            return cost.ToBadgeText();
        }
    }
}
