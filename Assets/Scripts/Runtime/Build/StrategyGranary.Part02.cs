using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyGranary
    {
        private void PlayFoodStoredEffect(StrategyResourceType resource, int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = GetFoodStockWorld(resource, building.FootprintBounds) + new Vector3(0f, 0.08f, -0.02f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                resource,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                GetStoredFood(resource) + amount * 19);
        }

        private static Vector3 GetFoodStockWorld(StrategyResourceType resource, Bounds bounds)
        {
            return StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                StrategyBuildTool.Granary,
                resource,
                bounds);
        }
    }
}
