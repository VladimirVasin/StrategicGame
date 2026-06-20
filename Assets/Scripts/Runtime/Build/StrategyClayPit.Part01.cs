using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyClayPit
    {
        private const int WorkerLayerOffset = 96;

        public Vector3 GetInteriorWorkWorld(StrategyResidentAgent worker)
        {
            Bounds bounds = FootprintBounds;
            int slot = GetWorkerSlotIndex(worker);
            bool split = workers.Count > 1;
            float side = split ? (slot == 0 ? -0.25f : 0.25f) : 0f;
            float depth = split && slot == 1 ? 0.44f : 0.39f;
            return new Vector3(bounds.center.x + side, bounds.min.y + bounds.size.y * depth, -0.08f);
        }

        public int GetInteriorWorkerSortingOffset(StrategyResidentAgent worker)
        {
            return WorkerLayerOffset + GetWorkerSlotIndex(worker) * 2;
        }

        public void PlayDiggingWorkEffect(StrategyResidentAgent worker, int seed)
        {
            Vector3 world = GetInteriorWorkWorld(worker) + new Vector3(0.11f, 0.02f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.Dust,
                world,
                StrategyWorldSorting.ForPosition(world, WorkerLayerOffset + 4),
                seed,
                0.78f);
            if (Mathf.Abs(seed) % 2 == 0)
            {
                StrategyWorldEffectAnimator.SpawnResourcePlaced(
                    StrategyResourceType.Clay,
                    world + new Vector3(-0.04f, 0.01f, -0.01f),
                    StrategyWorldSorting.ForPosition(world, WorkerLayerOffset + 5),
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
