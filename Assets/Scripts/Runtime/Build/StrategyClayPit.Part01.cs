using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyClayPit
    {
        public Vector3 GetInteriorWorkWorld(StrategyResidentAgent worker)
        {
            int slot = GetWorkerSlotIndex(worker);
            return StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(
                StrategyBuildTool.ClayPit,
                FootprintBounds,
                slot,
                workers.Count);
        }

        public int GetInteriorWorkerSortingOffset(StrategyResidentAgent worker)
        {
            return StrategyBuildingVisualAnchorProfile.GetInteriorWorkerSortingOffset(
                GetWorkerSlotIndex(worker));
        }

        public void PlayDiggingWorkEffect(StrategyResidentAgent worker, int seed)
        {
            Vector3 world = GetInteriorWorkWorld(worker) + new Vector3(0.11f, 0.02f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.Dust,
                world,
                StrategyWorldSorting.ForPosition(
                    world,
                    StrategyBuildingVisualAnchorProfile.InteriorWorkerLayerOffset + 4),
                seed,
                0.78f);
            if (Mathf.Abs(seed) % 2 == 0)
            {
                StrategyWorldEffectAnimator.SpawnResourcePlaced(
                    StrategyResourceType.Clay,
                    world + new Vector3(-0.04f, 0.01f, -0.01f),
                    StrategyWorldSorting.ForPosition(
                        world,
                        StrategyBuildingVisualAnchorProfile.InteriorWorkerLayerOffset + 5),
                    1,
                    seed + 23);
            }
        }

        private int GetWorkerSlotIndex(StrategyResidentAgent worker)
        {
            int index = worker != null ? workers.IndexOf(worker) : -1;
            return index <= 0 ? 0 : 1;
        }
    }
}
