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
            return resource switch
            {
                StrategyResourceType.Logs => new Vector3(bounds.center.x + 0.28f, bounds.min.y + 0.45f, -0.16f),
                StrategyResourceType.Stone => new Vector3(bounds.center.x - 0.86f, bounds.min.y + 0.37f, -0.155f),
                StrategyResourceType.Iron => new Vector3(bounds.center.x + 0.82f, bounds.min.y + 0.34f, -0.15f),
                StrategyResourceType.Coal => new Vector3(bounds.center.x + 0.18f, bounds.min.y + 0.28f, -0.145f),
                StrategyResourceType.Clay => new Vector3(bounds.max.x - 0.16f, bounds.min.y + 0.30f, -0.145f),
                StrategyResourceType.Pottery => new Vector3(bounds.max.x - 0.34f, bounds.min.y + 0.58f, -0.146f),
                StrategyResourceType.Planks => new Vector3(bounds.max.x - 0.56f, bounds.min.y + 0.39f, -0.148f),
                StrategyResourceType.Tools => new Vector3(bounds.center.x + 0.64f, bounds.min.y + 0.60f, -0.147f),
                _ => bounds.center
            };
        }
    }
}
