using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private void AddSettlementCoverageFloor(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category)
        {
            if (!IsCoverageAllowed(profession, category)
                || !TryGetSettlementRoleAnchor(profession, out Component target, out Vector3 world))
            {
                return;
            }

            SetCoverageFloorTarget(profession, 1);
            SetDesiredProfessionTarget(profession, 1);
            AddCoverageDemandIfNeeded(profession, category, target, world, CountAssignedProfession(profession));
        }

        private StrategyAutoWorkforceDemand TryCreateSettlementFallbackDemand(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            bool allowOverTarget)
        {
            if (!IsCoverageAllowed(profession, category)
                || !TryGetSettlementRoleAnchor(profession, out Component target, out Vector3 world))
            {
                return null;
            }

            int current = CountAssignedProfession(profession);
            int targetCount = GetDesiredOrCoverageTarget(profession);
            if (!allowOverTarget && current >= targetCount)
            {
                return null;
            }

            return CreateFallbackDemand(profession, category, target, world, current, targetCount, allowOverTarget);
        }

        private bool TryGetSettlementRoleAnchor(
            StrategyProfessionType profession,
            out Component target,
            out Vector3 world)
        {
            target = this;
            world = transform.position;
            if (profession == StrategyProfessionType.Builder)
            {
                return TryFindConstructionAnchor(out target, out world);
            }

            if (profession != StrategyProfessionType.StorageWorker)
            {
                return false;
            }

            if (TryFindStorageAnchor(out target, out world)
                || TryFindConstructionAnchor(out target, out world)
                || TryFindGranaryAnchor(out target, out world))
            {
                return true;
            }

            return false;
        }

        private bool TryFindConstructionAnchor(out Component target, out Vector3 world)
        {
            target = null;
            world = transform.position;
            StrategyConstructionSite best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < cachedConstructionSites.Length; i++)
            {
                StrategyConstructionSite site = cachedConstructionSites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                float score = site.NeededLogs + site.NeededStone + site.NeededPlanks
                    + (site.CanBuildWithDeliveredResources ? 8f : 0f)
                    - site.BuilderCount * 4f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = site;
                }
            }

            if (best == null)
            {
                return false;
            }

            target = best;
            world = best.FootprintBounds.center;
            return true;
        }

        private bool TryFindStorageAnchor(out Component target, out Vector3 world)
        {
            target = null;
            world = transform.position;
            StrategyStorageYard best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < cachedStorageYards.Length; i++)
            {
                StrategyStorageYard yard = cachedStorageYards[i];
                if (yard == null)
                {
                    continue;
                }

                float distance = (yard.FootprintBounds.center - transform.position).sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = yard;
            }

            if (best == null)
            {
                return false;
            }

            target = best;
            world = best.FootprintBounds.center;
            return true;
        }

        private bool TryFindGranaryAnchor(out Component target, out Vector3 world)
        {
            target = null;
            world = transform.position;
            for (int i = 0; i < cachedGranaries.Length; i++)
            {
                StrategyGranary granary = cachedGranaries[i];
                if (granary == null)
                {
                    continue;
                }

                target = granary;
                world = granary.FootprintBounds.center;
                return true;
            }

            return false;
        }

        private int CountReleasableSettlementHaulers(bool allowActiveRelease)
        {
            return population != null ? population.CountReleasableSettlementHaulers(allowActiveRelease) : 0;
        }

        private int CountReleasableSettlementBuilders(bool allowActiveRelease)
        {
            return population != null ? population.CountReleasableSettlementBuilders(allowActiveRelease) : 0;
        }

        private bool TryReleaseSettlementHauler(out StrategyResidentAgent worker, bool allowActiveRelease)
        {
            worker = null;
            return population != null && population.TryRemoveSettlementHauler(out worker, allowActiveRelease);
        }

        private bool TryReleaseSettlementBuilder(out StrategyResidentAgent worker, bool allowActiveRelease)
        {
            worker = null;
            return population != null && population.TryRemoveSettlementBuilder(out worker, allowActiveRelease);
        }
    }
}
