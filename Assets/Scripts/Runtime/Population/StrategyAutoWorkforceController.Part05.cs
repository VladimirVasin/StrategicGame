using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private const float CoverageFloorUrgency = 120f;
        private const float FallbackOverTargetPenalty = 260f;
        private const float FallbackCurrentWorkerPenalty = 36f;

        private void AddCoverageFloorDemands()
        {
            AddStorageCoverageFloor(StrategyProfessionType.Builder, StrategyAutoWorkforceCategory.Construction, yard => yard.BuilderCount);
            AddStorageCoverageFloor(StrategyProfessionType.StorageWorker, StrategyAutoWorkforceCategory.Logistics, yard => yard.WorkerCount);
            AddSiteCoverageFloor<StrategyLumberjackCamp>(StrategyProfessionType.Lumberjack, StrategyAutoWorkforceCategory.Wood, camp => camp.WorkerCount, camp => StrategyLumberjackCamp.MaxWorkers, camp => camp.FootprintBounds.center);
            AddSiteCoverageFloor<StrategyStonecutterCamp>(StrategyProfessionType.Stonecutter, StrategyAutoWorkforceCategory.Stone, camp => camp.WorkerCount, camp => StrategyStonecutterCamp.MaxWorkers, camp => camp.FootprintBounds.center);
            AddSiteCoverageFloor<StrategySawmill>(StrategyProfessionType.Sawyer, StrategyAutoWorkforceCategory.Planks, sawmill => sawmill.WorkerCount, sawmill => StrategySawmill.MaxWorkers, sawmill => sawmill.FootprintBounds.center);
            AddSiteCoverageFloor<StrategyMine>(StrategyProfessionType.Miner, StrategyAutoWorkforceCategory.Iron, mine => mine.WorkerCount, mine => StrategyMine.MaxWorkers, mine => mine.FootprintBounds.center);
            AddSiteCoverageFloor<StrategyCoalPit>(StrategyProfessionType.CoalMiner, StrategyAutoWorkforceCategory.Coal, pit => pit.WorkerCount, pit => StrategyCoalPit.MaxWorkers, pit => pit.FootprintBounds.center);
            AddSiteCoverageFloor<StrategyHunterCamp>(StrategyProfessionType.Hunter, StrategyAutoWorkforceCategory.Food, camp => camp.WorkerCount, camp => StrategyHunterCamp.MaxWorkers, camp => camp.FootprintBounds.center);
            AddSiteCoverageFloor<StrategyFisherHut>(StrategyProfessionType.Fisher, StrategyAutoWorkforceCategory.Food, hut => hut.WorkerCount, hut => StrategyFisherHut.MaxWorkers, hut => hut.FootprintBounds.center);
        }

        private void AddSiteCoverageFloor<T>(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            Func<T, Vector3> getWorld)
            where T : Component
        {
            if (!IsCoverageAllowed(profession, category))
            {
                return;
            }

            T[] sites = GetCachedSites<T>();
            if (!TryFindLeastStaffedCapacitySite(sites, getWorkers, getCapacity, out _))
            {
                return;
            }

            SetCoverageFloorTarget(profession, 1);
            SetDesiredProfessionTarget(profession, 1);
            if (TryFindLeastStaffedOpenSite(sites, getWorkers, getCapacity, out T target))
            {
                AddCoverageDemandIfNeeded(profession, category, target, getWorld(target), CountAssignedProfession(profession));
            }
        }

        private void AddStorageCoverageFloor(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Func<StrategyStorageYard, int> getWorkers)
        {
            if (!IsCoverageAllowed(profession, category)
                || !TryFindLeastStaffedStorageYard(getWorkers, out StrategyStorageYard yard))
            {
                return;
            }

            SetCoverageFloorTarget(profession, 1);
            SetDesiredProfessionTarget(profession, 1);
            AddCoverageDemandIfNeeded(profession, category, yard, yard.FootprintBounds.center, CountAssignedProfession(profession));
        }

        private void AddCoverageDemandIfNeeded(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Component target,
            Vector3 world,
            int current)
        {
            int floor = GetCoverageFloorTarget(profession);
            int needed = Mathf.Max(0, floor - current - CountPendingDemand(profession));
            if (needed <= 0)
            {
                return;
            }

            AddDemand(profession, category, target, world, needed, CoverageFloorUrgency, "coverage_floor");
        }

        private int AssignIdleAdultsToBestAvailableRoles(bool allowOverTarget, ref int assignmentBudget)
        {
            int assigned = 0;
            while (assignmentBudget > 0 && candidates.Count > 0)
            {
                if (!TryCreateBestFallbackDemand(false, out StrategyAutoWorkforceDemand demand)
                    && (!allowOverTarget || !TryCreateBestFallbackDemand(true, out demand)))
                {
                    break;
                }

                StrategyResidentAgent resident = TakeNearestCandidate(demand.World);
                if (resident == null)
                {
                    break;
                }

                if (!TryAssignResident(demand, resident))
                {
                    continue;
                }

                assignmentBudget--;
                assigned++;
                StrategyDebugLogger.Info(
                    "AutoWorkforce",
                    "AutoWorkforceFallbackAssigned",
                    StrategyDebugLogger.F("profession", demand.Profession),
                    StrategyDebugLogger.F("category", demand.Category),
                    StrategyDebugLogger.F("resident", resident.FullName),
                    StrategyDebugLogger.F("score", demand.Score),
                    StrategyDebugLogger.F("reason", demand.Reason));
            }

            return assigned;
        }

        private bool TryCreateBestFallbackDemand(bool allowOverTarget, out StrategyAutoWorkforceDemand demand)
        {
            demand = null;
            TryKeepBetterFallbackDemand(ref demand, TryCreateStorageFallbackDemand(StrategyProfessionType.Builder, StrategyAutoWorkforceCategory.Construction, yard => yard.BuilderCount, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateStorageFallbackDemand(StrategyProfessionType.StorageWorker, StrategyAutoWorkforceCategory.Logistics, yard => yard.WorkerCount, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategyLumberjackCamp>(StrategyProfessionType.Lumberjack, StrategyAutoWorkforceCategory.Wood, camp => camp.WorkerCount, camp => StrategyLumberjackCamp.MaxWorkers, camp => camp.FootprintBounds.center, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategyStonecutterCamp>(StrategyProfessionType.Stonecutter, StrategyAutoWorkforceCategory.Stone, camp => camp.WorkerCount, camp => StrategyStonecutterCamp.MaxWorkers, camp => camp.FootprintBounds.center, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategySawmill>(StrategyProfessionType.Sawyer, StrategyAutoWorkforceCategory.Planks, sawmill => sawmill.WorkerCount, sawmill => StrategySawmill.MaxWorkers, sawmill => sawmill.FootprintBounds.center, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategyMine>(StrategyProfessionType.Miner, StrategyAutoWorkforceCategory.Iron, mine => mine.WorkerCount, mine => StrategyMine.MaxWorkers, mine => mine.FootprintBounds.center, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategyCoalPit>(StrategyProfessionType.CoalMiner, StrategyAutoWorkforceCategory.Coal, pit => pit.WorkerCount, pit => StrategyCoalPit.MaxWorkers, pit => pit.FootprintBounds.center, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategyHunterCamp>(StrategyProfessionType.Hunter, StrategyAutoWorkforceCategory.Food, camp => camp.WorkerCount, camp => StrategyHunterCamp.MaxWorkers, camp => camp.FootprintBounds.center, allowOverTarget));
            TryKeepBetterFallbackDemand(ref demand, TryCreateSiteFallbackDemand<StrategyFisherHut>(StrategyProfessionType.Fisher, StrategyAutoWorkforceCategory.Food, hut => hut.WorkerCount, hut => StrategyFisherHut.MaxWorkers, hut => hut.FootprintBounds.center, allowOverTarget));
            return demand != null;
        }

        private StrategyAutoWorkforceDemand TryCreateSiteFallbackDemand<T>(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            Func<T, Vector3> getWorld,
            bool allowOverTarget)
            where T : Component
        {
            if (!IsCoverageAllowed(profession, category))
            {
                return null;
            }

            int current = CountAssignedProfession(profession);
            int targetCount = GetDesiredOrCoverageTarget(profession);
            if (!allowOverTarget && current >= targetCount)
            {
                return null;
            }

            T[] sites = GetCachedSites<T>();
            return TryFindLeastStaffedOpenSite(sites, getWorkers, getCapacity, out T target)
                ? CreateFallbackDemand(profession, category, target, getWorld(target), current, targetCount, allowOverTarget)
                : null;
        }

        private StrategyAutoWorkforceDemand TryCreateStorageFallbackDemand(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Func<StrategyStorageYard, int> getWorkers,
            bool allowOverTarget)
        {
            if (!IsCoverageAllowed(profession, category))
            {
                return null;
            }

            int current = CountAssignedProfession(profession);
            int targetCount = GetDesiredOrCoverageTarget(profession);
            if (!allowOverTarget && current >= targetCount)
            {
                return null;
            }

            return TryFindLeastStaffedStorageYard(getWorkers, out StrategyStorageYard yard)
                ? CreateFallbackDemand(profession, category, yard, yard.FootprintBounds.center, current, targetCount, allowOverTarget)
                : null;
        }

        private StrategyAutoWorkforceDemand CreateFallbackDemand(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Component target,
            Vector3 world,
            int current,
            int targetCount,
            bool allowOverTarget)
        {
            float score = settings.GetPriority(category) * BasePriorityScore
                - current * FallbackCurrentWorkerPenalty
                + (current < targetCount ? CoverageFloorUrgency : 0f)
                - (allowOverTarget ? FallbackOverTargetPenalty : 0f);
            return new StrategyAutoWorkforceDemand(profession, category, target, world, 1, score, "fallback_free_adult");
        }

        private bool IsCoverageAllowed(StrategyProfessionType profession, StrategyAutoWorkforceCategory category)
        {
            return settings.GetPriority(category) > 0 && !IsProfessionManualLocked(profession);
        }

        private void SetCoverageFloorTarget(StrategyProfessionType profession, int floor)
        {
            if (floor <= 0)
            {
                return;
            }

            if (!coverageProfessionFloors.TryGetValue(profession, out int current) || floor > current)
            {
                coverageProfessionFloors[profession] = floor;
            }
        }

        private int GetCoverageFloorTarget(StrategyProfessionType profession)
        {
            return coverageProfessionFloors.TryGetValue(profession, out int floor) ? floor : 0;
        }

        private int GetDesiredOrCoverageTarget(StrategyProfessionType profession)
        {
            int desired = desiredProfessionTargets.TryGetValue(profession, out int target) ? target : 0;
            return Mathf.Max(desired, GetCoverageFloorTarget(profession));
        }

        private int CountPendingDemand(StrategyProfessionType profession)
        {
            int total = 0;
            for (int i = 0; i < demands.Count; i++)
            {
                StrategyAutoWorkforceDemand demand = demands[i];
                if (demand != null && demand.Profession == profession)
                {
                    total += demand.Needed;
                }
            }

            return total;
        }

        private static void TryKeepBetterFallbackDemand(ref StrategyAutoWorkforceDemand best, StrategyAutoWorkforceDemand candidate)
        {
            if (candidate != null && (best == null || candidate.Score > best.Score))
            {
                best = candidate;
            }
        }

        private static bool TryFindLeastStaffedOpenSite<T>(
            T[] sites,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            out T target)
            where T : Component
        {
            target = null;
            int bestWorkers = int.MaxValue;
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                if (site == null)
                {
                    continue;
                }

                int workers = getWorkers(site);
                if (workers >= getCapacity(site) || workers >= bestWorkers)
                {
                    continue;
                }

                bestWorkers = workers;
                target = site;
            }

            return target != null;
        }

        private static bool TryFindLeastStaffedCapacitySite<T>(
            T[] sites,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            out T target)
            where T : Component
        {
            target = null;
            int bestWorkers = int.MaxValue;
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                if (site == null || getCapacity(site) <= 0)
                {
                    continue;
                }

                int workers = getWorkers(site);
                if (workers >= bestWorkers)
                {
                    continue;
                }

                bestWorkers = workers;
                target = site;
            }

            return target != null;
        }

        private bool TryFindLeastStaffedStorageYard(
            Func<StrategyStorageYard, int> getWorkers,
            out StrategyStorageYard target)
        {
            target = null;
            int bestWorkers = int.MaxValue;
            StrategyStorageYard[] yards = cachedStorageYards;
            for (int i = 0; i < yards.Length; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null)
                {
                    continue;
                }

                int workers = getWorkers(yard);
                if (workers >= bestWorkers)
                {
                    continue;
                }

                bestWorkers = workers;
                target = yard;
            }

            return target != null;
        }
    }
}
