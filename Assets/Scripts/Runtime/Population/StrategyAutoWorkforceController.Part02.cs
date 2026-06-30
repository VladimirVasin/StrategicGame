using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private delegate bool TryGetWorkerAt<T>(T site, int index, out StrategyResidentAgent worker);
        private delegate void UnassignWorkerAt<T>(T site, int index);

        private static readonly StrategyProfessionType[] AutoManagedProfessions =
        {
            StrategyProfessionType.Lumberjack,
            StrategyProfessionType.Stonecutter,
            StrategyProfessionType.Miner,
            StrategyProfessionType.CoalMiner,
            StrategyProfessionType.ClayDigger,
            StrategyProfessionType.Sawyer,
            StrategyProfessionType.Potter,
            StrategyProfessionType.Blacksmith,
            StrategyProfessionType.Hunter,
            StrategyProfessionType.Fisher,
            StrategyProfessionType.Forager,
            StrategyProfessionType.StorageWorker,
            StrategyProfessionType.Builder
        };

        private void AddHunterDemands(int desiredFoodWorkers, float urgency)
        {
            if (IsProfessionManualLocked(StrategyProfessionType.Hunter))
            {
                return;
            }

            AddCappedCampDemands<StrategyHunterCamp>(
                StrategyProfessionType.Hunter,
                StrategyAutoWorkforceCategory.Food,
                desiredFoodWorkers,
                "low_food",
                camp => camp.WorkerCount,
                camp => StrategyHunterCamp.MaxWorkers,
                camp => camp.HasStorageSpace,
                camp => camp.FootprintBounds.center,
                urgency);
        }

        private void AddFisherDemands(int desiredFoodWorkers, float urgency)
        {
            if (IsProfessionManualLocked(StrategyProfessionType.Fisher))
            {
                return;
            }

            AddCappedCampDemands<StrategyFisherHut>(
                StrategyProfessionType.Fisher,
                StrategyAutoWorkforceCategory.Food,
                desiredFoodWorkers,
                "low_food",
                hut => hut.WorkerCount,
                hut => StrategyFisherHut.MaxWorkers,
                hut => hut.HasStorageSpace,
                hut => hut.FootprintBounds.center,
                urgency);
        }

        private void AddForagerDemands(int desiredFoodWorkers, float urgency)
        {
            if (IsProfessionManualLocked(StrategyProfessionType.Forager))
            {
                return;
            }

            AddCappedCampDemands<StrategyForagerCamp>(
                StrategyProfessionType.Forager,
                StrategyAutoWorkforceCategory.Food,
                desiredFoodWorkers,
                "low_food",
                camp => camp.WorkerCount,
                camp => StrategyForagerCamp.MaxWorkers,
                camp => camp.HasStorageSpace,
                camp => camp.FootprintBounds.center,
                urgency);
        }

        private void AddCappedCampDemands<T>(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            int desiredWorkers,
            string reason,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            Func<T, bool> canWork,
            Func<T, Vector3> getWorld,
            float extraUrgency = 0f)
            where T : Component
        {
            if (desiredWorkers <= 0)
            {
                return;
            }

            int priority = settings.GetPriority(category);
            if (priority <= 0)
            {
                return;
            }

            T[] sites = GetCachedSites<T>();
            demandSiteScratch.Clear();
            int desired = Mathf.Min(desiredWorkers, priority);
            int capacity = 0;
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                if (site != null && canWork(site))
                {
                    capacity += Mathf.Max(0, getCapacity(site));
                }
            }

            int target = Mathf.Min(desired, capacity);
            SetDesiredProfessionTarget(profession, target);
            int remaining = Mathf.Max(0, target - CountAssignedProfession(profession));
            while (remaining > 0)
            {
                T site = FindLeastStaffedOpenSite(sites, getWorkers, getCapacity, canWork);
                if (site == null)
                {
                    break;
                }

                demandSiteScratch.Add(site);
                int open = Mathf.Max(0, getCapacity(site) - getWorkers(site));
                int needed = Mathf.Min(open, remaining);
                remaining -= needed;
                AddDemand(
                    profession,
                    category,
                    site,
                    getWorld(site),
                    needed,
                    extraUrgency - getWorkers(site) * 10f,
                    reason);
            }
        }

        private T FindLeastStaffedOpenSite<T>(
            T[] sites,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            Func<T, bool> canWork)
            where T : Component
        {
            T best = null;
            int bestWorkers = int.MaxValue;
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                if (site == null || demandSiteScratch.Contains(site) || !canWork(site))
                {
                    continue;
                }

                int workers = getWorkers(site);
                if (workers >= getCapacity(site) || workers >= bestWorkers)
                {
                    continue;
                }

                best = site;
                bestWorkers = workers;
            }

            return best;
        }

        private void AddDemand(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Component target,
            Vector3 world,
            int needed,
            float urgency,
            string reason)
        {
            if (needed <= 0 || target == null || IsProfessionManualLocked(profession))
            {
                return;
            }

            int priority = settings.GetPriority(category);
            if (priority <= 0)
            {
                return;
            }

            StrategyAutoWorkforceDemand demand = new(
                profession,
                category,
                target,
                world,
                needed,
                priority * BasePriorityScore + urgency,
                reason);
            demands.Add(demand);
            if (ShouldLogDemand(profession, reason))
            {
                StrategyDebugLogger.Info(
                    "AutoWorkforce",
                    "AutoWorkforceDemand",
                    StrategyDebugLogger.F("profession", profession),
                    StrategyDebugLogger.F("category", category),
                    StrategyDebugLogger.F("needed", needed),
                    StrategyDebugLogger.F("score", demand.Score),
                    StrategyDebugLogger.F("reason", reason));
            }
        }

        private bool ShouldLogDemand(StrategyProfessionType profession, string reason)
        {
            string key = profession + ":" + reason;
            float now = Time.unscaledTime;
            if (demandLogTimes.TryGetValue(key, out float nextAllowedTime) && now < nextAllowedTime)
            {
                return false;
            }

            demandLogTimes[key] = now + DemandLogInterval;
            return true;
        }

        private int AssignDemand(StrategyAutoWorkforceDemand demand, ref int assignmentBudget)
        {
            int assigned = 0;
            while (assignmentBudget > 0 && demand.Needed > 0 && candidates.Count > 0)
            {
                StrategyResidentAgent resident = TakeNearestCandidate(demand.World);
                if (resident == null)
                {
                    break;
                }

                if (!TryAssignResident(demand, resident))
                {
                    continue;
                }

                demand.Needed--;
                assignmentBudget--;
                assigned++;
                StrategyDebugLogger.Info(
                    "AutoWorkforce",
                    "AutoWorkforceAssigned",
                    StrategyDebugLogger.F("profession", demand.Profession),
                    StrategyDebugLogger.F("category", demand.Category),
                    StrategyDebugLogger.F("resident", resident.FullName),
                    StrategyDebugLogger.F("reason", demand.Reason));
            }

            return assigned;
        }

        private StrategyResidentAgent TakeNearestCandidate(Vector3 world)
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                StrategyResidentAgent candidate = candidates[i];
                if (candidate == null
                    || !candidate.CanAcceptWorkAssignment
                    || candidate.HasWorkplace
                    || candidate.HasConstructionAssignment)
                {
                    continue;
                }

                float distance = (candidate.transform.position - world).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                candidates.Clear();
                return null;
            }

            StrategyResidentAgent resident = candidates[bestIndex];
            candidates.RemoveAt(bestIndex);
            return resident;
        }

        private bool TryAssignResident(StrategyAutoWorkforceDemand demand, StrategyResidentAgent resident)
        {
            bool assigned = demand.Target switch
            {
                StrategyLumberjackCamp camp => camp.AssignWorker(resident),
                StrategyStonecutterCamp camp => camp.AssignWorker(resident),
                StrategyMine mine => mine.AssignWorker(resident),
                StrategyCoalPit pit => pit.AssignWorker(resident),
                StrategyClayPit pit => pit.AssignWorker(resident),
                StrategySawmill sawmill => sawmill.AssignWorker(resident),
                StrategyKiln kiln => kiln.AssignWorker(resident),
                StrategyForge forge => forge.AssignWorker(resident),
                StrategyHunterCamp camp => camp.AssignWorker(resident),
                StrategyFisherHut hut => hut.AssignWorker(resident),
                StrategyForagerCamp camp => camp.AssignWorker(resident),
                StrategyConstructionSite _ when demand.Profession == StrategyProfessionType.Builder => population != null && population.TryAssignSettlementBuilder(resident),
                StrategyConstructionSite _ when demand.Profession == StrategyProfessionType.StorageWorker => population != null && population.TryAssignSettlementHauler(resident),
                StrategyStorageYard _ when demand.Profession == StrategyProfessionType.Builder => population != null && population.TryAssignSettlementBuilder(resident),
                StrategyStorageYard _ when demand.Profession == StrategyProfessionType.StorageWorker => population != null && population.TryAssignSettlementHauler(resident),
                StrategyGranary _ when demand.Profession == StrategyProfessionType.StorageWorker => population != null && population.TryAssignSettlementHauler(resident),
                StrategyAutoWorkforceController _ when demand.Profession == StrategyProfessionType.Builder => population != null && population.TryAssignSettlementBuilder(resident),
                StrategyAutoWorkforceController _ when demand.Profession == StrategyProfessionType.StorageWorker => population != null && population.TryAssignSettlementHauler(resident),
                _ => false
            };

            if (assigned && demand.Profession == StrategyProfessionType.Builder)
            {
                DispatchBuildersNear(demand.World);
            }

            if (!assigned)
            {
                StrategyDebugLogger.Info(
                    "AutoWorkforce",
                    "AutoWorkforceSkipped",
                    StrategyDebugLogger.F("profession", demand.Profession),
                    StrategyDebugLogger.F("resident", resident.FullName),
                    StrategyDebugLogger.F("reason", "assign_rejected"));
            }

            return assigned;
        }

        private void DispatchBuildersNear(Vector3 world)
        {
            StrategyConstructionSite[] sites = cachedConstructionSites;
            StrategyConstructionSite nearest = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                float distance = (site.FootprintBounds.center - world).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = site;
                }
            }

            if (nearest != null)
            {
                population?.TryDispatchSettlementBuildersToSite(nearest, false);
            }
        }

        private bool TryFindStorageTarget(Vector3 world, out StrategyStorageYard yard)
        {
            yard = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < cachedStorageYards.Length; i++)
            {
                StrategyStorageYard candidate = cachedStorageYards[i];
                if (candidate == null)
                {
                    continue;
                }

                float distance = (candidate.FootprintBounds.center - world).sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                yard = candidate;
            }

            return yard != null;
        }

        private int CountAssignedProfession(StrategyProfessionType profession)
        {
            return profession switch
            {
                StrategyProfessionType.Lumberjack => CountSiteWorkers<StrategyLumberjackCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.Stonecutter => CountSiteWorkers<StrategyStonecutterCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.Miner => CountSiteWorkers<StrategyMine>(mine => mine.WorkerCount),
                StrategyProfessionType.CoalMiner => CountSiteWorkers<StrategyCoalPit>(pit => pit.WorkerCount),
                StrategyProfessionType.ClayDigger => CountSiteWorkers<StrategyClayPit>(pit => pit.WorkerCount),
                StrategyProfessionType.Sawyer => CountSiteWorkers<StrategySawmill>(sawmill => sawmill.WorkerCount),
                StrategyProfessionType.Potter => CountSiteWorkers<StrategyKiln>(kiln => kiln.WorkerCount),
                StrategyProfessionType.Blacksmith => CountSiteWorkers<StrategyForge>(forge => forge.WorkerCount),
                StrategyProfessionType.Hunter => CountSiteWorkers<StrategyHunterCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.Fisher => CountSiteWorkers<StrategyFisherHut>(hut => hut.WorkerCount),
                StrategyProfessionType.Forager => CountSiteWorkers<StrategyForagerCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.StorageWorker => population != null ? population.CountSettlementHaulers() : 0,
                StrategyProfessionType.Builder => population != null ? population.CountSettlementBuilders() : 0,
                _ => 0
            };
        }

        private int CountSiteWorkers<T>(Func<T, int> getWorkers)
            where T : Component
        {
            int total = 0;
            foreach (T site in GetCachedSites<T>())
            {
                total += site != null ? getWorkers(site) : 0;
            }

            return total;
        }

        private void SetDesiredProfessionTarget(StrategyProfessionType profession, int target)
        {
            if (target <= 0)
            {
                return;
            }

            if (!desiredProfessionTargets.TryGetValue(profession, out int current) || target > current)
            {
                desiredProfessionTargets[profession] = target;
            }
        }

        private void TryAddReleasedCandidate(StrategyResidentAgent resident)
        {
            if (resident == null
                || candidates.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment
                || resident.Activity != StrategyResidentAgent.ResidentActivity.Idle)
            {
                return;
            }

            candidates.Add(resident);
        }
    }
}
