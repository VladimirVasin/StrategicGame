using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        private void PlayDeliveredResourceEffect(StrategyConstructionResourceKind resource, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Vector3 world = resource switch
            {
                StrategyConstructionResourceKind.Logs => new Vector3(footprintBounds.center.x - 0.82f, footprintBounds.min.y + 0.42f, -0.14f),
                StrategyConstructionResourceKind.Stone => new Vector3(footprintBounds.center.x + 0.78f, footprintBounds.min.y + 0.40f, -0.14f),
                StrategyConstructionResourceKind.Planks => new Vector3(footprintBounds.center.x, footprintBounds.min.y + 0.58f, -0.15f),
                _ => footprintBounds.center
            };
            StrategyWorldEffectAnimator.SpawnConstructionResourcePlaced(
                resource,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                deliveredLogs + deliveredStone * 7 + deliveredPlanks * 13);
        }

        private void PlayBuildHitEffect(Vector3 builderWorld)
        {
            Vector3 world = Vector3.Lerp(builderWorld, footprintBounds.center, 0.58f);
            world.z = -0.14f;
            StrategyWorldEffectAnimator.SpawnConstructionHit(
                world,
                StrategyWorldSorting.ForPosition(world, 5),
                buildHits + BuilderCount * 19);
        }
    }
}
