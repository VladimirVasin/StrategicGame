namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private static bool CanAffordBuildCost(StrategyConstructionResourceCost cost)
        {
            return StrategyDebugOptions.InstantConstructionEnabled
                || cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources());
        }

        private static string GetBuildItemBadgeText(
            StrategyConstructionResourceCost cost,
            bool allowed,
            bool active)
        {
            if (!allowed)
            {
                return "Locked";
            }

            if (active)
            {
                return "Active";
            }

            return StrategyDebugOptions.InstantConstructionEnabled ? "Free" : cost.ToBadgeText();
        }
    }
}
