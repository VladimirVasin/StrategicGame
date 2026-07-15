using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySawmill
    {
        private void TrySpawnSawdustEffect()
        {
            if (building == null || activeSawyers.Count <= 0 || workFrame % 3 != 0)
            {
                return;
            }

            Vector3 world = GetSawFocusWorld() + new Vector3(0.28f, -0.05f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.Sawdust,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                workFrame + activeSawyers.Count * 23,
                0.76f);
        }

        private void PlayInputDeliveredEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Sawmill,
                    StrategyResourceType.Logs,
                    FootprintBounds)
                + new Vector3(0f, 0.10f, -0.02f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                StrategyResourceType.Logs,
                world,
                StrategyWorldSorting.ForPosition(world, 3),
                amount,
                logsStored + amount * 17);
        }

        private void PlayPlanksProducedEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                    StrategyBuildTool.Sawmill,
                    StrategyResourceType.Planks,
                    FootprintBounds)
                + new Vector3(0f, 0.10f, -0.03f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                StrategyResourceType.Planks,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                planksStored + amount * 31);
        }
    }
}
