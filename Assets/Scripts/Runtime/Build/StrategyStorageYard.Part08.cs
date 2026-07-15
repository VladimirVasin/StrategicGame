using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private void PlayResourceStoredEffect(StrategyResourceType resource, int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Bounds bounds = FootprintBounds;
            Vector3 world = GetStoredResourceEffectWorld(resource, bounds);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                resource,
                world + new Vector3(0f, 0.09f, -0.02f),
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                GetAmount(resource) + amount * 23);
        }

        private static Vector3 GetStoredResourceEffectWorld(StrategyResourceType resource, Bounds bounds)
        {
            return StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                StrategyBuildTool.StorageYard,
                resource,
                bounds);
        }
    }
}
