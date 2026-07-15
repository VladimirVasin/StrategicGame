using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCoalPit
    {
        public Vector3 GetInteriorWorkWorld(StrategyResidentAgent worker)
        {
            int slot = GetWorkerSlotIndex(worker);
            return StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(
                StrategyBuildTool.CoalPit,
                FootprintBounds,
                slot,
                workers.Count);
        }

        public int GetInteriorWorkerSortingOffset(StrategyResidentAgent worker)
        {
            return StrategyBuildingVisualAnchorProfile.GetInteriorWorkerSortingOffset(
                GetWorkerSlotIndex(worker));
        }

        public void PlayMiningWorkEffect(StrategyResidentAgent worker, int seed)
        {
            Vector3 world = GetInteriorWorkWorld(worker) + new Vector3(0.12f, 0.02f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.CoalChips,
                world,
                StrategyWorldSorting.ForPosition(
                    world,
                    StrategyBuildingVisualAnchorProfile.InteriorWorkerLayerOffset + 4),
                seed,
                0.82f);
            if (Mathf.Abs(seed) % 2 == 0)
            {
                StrategyWorldEffectAnimator.Spawn(
                    StrategyWorldEffectKind.Dust,
                    world + new Vector3(-0.05f, 0.01f, -0.01f),
                    StrategyWorldSorting.ForPosition(
                        world,
                        StrategyBuildingVisualAnchorProfile.InteriorWorkerLayerOffset + 3),
                    seed + 17,
                    0.66f);
            }
        }

        private int GetWorkerSlotIndex(StrategyResidentAgent worker)
        {
            int index = worker != null ? workers.IndexOf(worker) : -1;
            return index <= 0 ? 0 : 1;
        }
    }
}
