using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private int CountAvailableFood(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyHunterCamp camp in cachedHunterCamps)
            {
                if (camp != null && camp.AvailableGame > 0)
                {
                    total += camp.AvailableGame;
                    world = camp.FootprintBounds.center;
                }
            }

            foreach (StrategyFisherHut hut in cachedFisherHuts)
            {
                if (hut != null && hut.AvailableFish > 0)
                {
                    total += hut.AvailableFish;
                    world = hut.FootprintBounds.center;
                }
            }

            foreach (StrategyForagerCamp camp in cachedForagerCamps)
            {
                if (camp != null && camp.AvailableForage > 0)
                {
                    total += camp.AvailableForage;
                    world = camp.FootprintBounds.center;
                }
            }

            foreach (StrategyChickenCoop coop in cachedChickenCoops)
            {
                if (coop != null && coop.AvailableEggs > 0)
                {
                    total += coop.AvailableEggs;
                    world = coop.FootprintBounds.center;
                }
            }

            return total;
        }
    }
}
