using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCoalPit
    {
        private const int WorkerLayerOffset = 96;

        public Vector3 GetInteriorWorkWorld(StrategyResidentAgent worker)
        {
            Bounds bounds = FootprintBounds;
            int slot = GetWorkerSlotIndex(worker);
            bool split = workers.Count > 1;
            float side = split ? (slot == 0 ? -0.27f : 0.27f) : 0f;
            float depth = split && slot == 1 ? 0.43f : 0.38f;
            return new Vector3(bounds.center.x + side, bounds.min.y + bounds.size.y * depth, -0.08f);
        }

        public int GetInteriorWorkerSortingOffset(StrategyResidentAgent worker)
        {
            return WorkerLayerOffset + GetWorkerSlotIndex(worker) * 2;
        }

        public void PlayMiningWorkEffect(StrategyResidentAgent worker, int seed)
        {
            Vector3 world = GetInteriorWorkWorld(worker) + new Vector3(0.12f, 0.02f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.CoalChips,
                world,
                StrategyWorldSorting.ForPosition(world, WorkerLayerOffset + 4),
                seed,
                0.82f);
            if (Mathf.Abs(seed) % 2 == 0)
            {
                StrategyWorldEffectAnimator.Spawn(
                    StrategyWorldEffectKind.Dust,
                    world + new Vector3(-0.05f, 0.01f, -0.01f),
                    StrategyWorldSorting.ForPosition(world, WorkerLayerOffset + 3),
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
