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
            StrategyProfessionType.Sawyer,
            StrategyProfessionType.Hunter,
            StrategyProfessionType.Fisher,
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

        private void AddCappedCampDemands<T>(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            int shortage,
            string reason,
            Func<T, int> getWorkers,
            Func<T, int> getCapacity,
            Func<T, bool> canWork,
            Func<T, Vector3> getWorld,
            float extraUrgency = 0f)
            where T : Component
        {
            if (shortage <= 0)
            {
                return;
            }

            int priority = settings.GetPriority(category);
            if (priority <= 0)
            {
                return;
            }

            T[] sites = UnityEngine.Object.FindObjectsByType<T>();
            Array.Sort(sites, (left, right) => getWorkers(left).CompareTo(getWorkers(right)));
            int desired = Mathf.Clamp(Mathf.CeilToInt(shortage / 4f), 1, Mathf.Max(1, priority));
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
            for (int i = 0; i < sites.Length && remaining > 0; i++)
            {
                T site = sites[i];
                if (site == null || !canWork(site))
                {
                    continue;
                }

                int open = Mathf.Max(0, getCapacity(site) - getWorkers(site));
                if (open <= 0)
                {
                    continue;
                }

                int needed = Mathf.Min(open, remaining);
                remaining -= needed;
                AddDemand(
                    profession,
                    category,
                    site,
                    getWorld(site),
                    needed,
                    extraUrgency + shortage * 6f - getWorkers(site) * 10f,
                    reason);
            }
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
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "AutoWorkforceDemand",
                StrategyDebugLogger.F("profession", profession),
                StrategyDebugLogger.F("category", category),
                StrategyDebugLogger.F("needed", needed),
                StrategyDebugLogger.F("score", demand.Score),
                StrategyDebugLogger.F("reason", reason));
        }

        private int AssignDemand(StrategyAutoWorkforceDemand demand)
        {
            int assigned = 0;
            while (demand.Needed > 0 && candidates.Count > 0)
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
                StrategySawmill sawmill => sawmill.AssignWorker(resident),
                StrategyHunterCamp camp => camp.AssignWorker(resident),
                StrategyFisherHut hut => hut.AssignWorker(resident),
                StrategyStorageYard yard when demand.Profession == StrategyProfessionType.Builder => yard.AssignBuilder(resident),
                StrategyStorageYard yard => yard.AssignWorker(resident),
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
            StrategyConstructionSite[] sites = UnityEngine.Object.FindObjectsByType<StrategyConstructionSite>();
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
                StrategyStorageYard.TryAssignBuildersToSite(nearest);
            }
        }

        private bool TryFindStorageTarget(Vector3 world, out StrategyStorageYard yard)
        {
            return StrategyStorageYard.TryFindNearestStorageYard(world, out yard);
        }

        private int CountAssignedProfession(StrategyProfessionType profession)
        {
            return profession switch
            {
                StrategyProfessionType.Lumberjack => CountSiteWorkers<StrategyLumberjackCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.Stonecutter => CountSiteWorkers<StrategyStonecutterCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.Miner => CountSiteWorkers<StrategyMine>(mine => mine.WorkerCount),
                StrategyProfessionType.CoalMiner => CountSiteWorkers<StrategyCoalPit>(pit => pit.WorkerCount),
                StrategyProfessionType.Sawyer => CountSiteWorkers<StrategySawmill>(sawmill => sawmill.WorkerCount),
                StrategyProfessionType.Hunter => CountSiteWorkers<StrategyHunterCamp>(camp => camp.WorkerCount),
                StrategyProfessionType.Fisher => CountSiteWorkers<StrategyFisherHut>(hut => hut.WorkerCount),
                StrategyProfessionType.StorageWorker => CountSiteWorkers<StrategyStorageYard>(yard => yard.WorkerCount),
                StrategyProfessionType.Builder => CountSiteWorkers<StrategyStorageYard>(yard => yard.BuilderCount),
                _ => 0
            };
        }

        private static int CountSiteWorkers<T>(Func<T, int> getWorkers)
            where T : Component
        {
            int total = 0;
            foreach (T site in UnityEngine.Object.FindObjectsByType<T>())
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

        private int RebalanceOverstaffedProfessions()
        {
            int released = 0;
            for (int i = 0; i < AutoManagedProfessions.Length; i++)
            {
                StrategyProfessionType profession = AutoManagedProfessions[i];
                if (IsProfessionManualLocked(profession))
                {
                    continue;
                }

                int target = desiredProfessionTargets.TryGetValue(profession, out int value) ? value : 0;
                int current = CountAssignedProfession(profession);
                while (current > target)
                {
                    if (!TryReleaseProfessionWorker(profession, out StrategyResidentAgent resident))
                    {
                        break;
                    }

                    current--;
                    released++;
                    TryAddReleasedCandidate(resident);
                    StrategyDebugLogger.Info(
                        "AutoWorkforce",
                        "AutoWorkforceReleasedSurplus",
                        StrategyDebugLogger.F("profession", profession),
                        StrategyDebugLogger.F("resident", resident != null ? resident.FullName : string.Empty),
                        StrategyDebugLogger.F("target", target),
                        StrategyDebugLogger.F("remaining", current));
                }
            }

            return released;
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

        private bool TryReleaseProfessionWorker(StrategyProfessionType profession, out StrategyResidentAgent worker)
        {
            worker = null;
            return profession switch
            {
                StrategyProfessionType.Lumberjack => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyLumberjackCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyLumberjackCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found),
                    (camp, index) => camp.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.Stonecutter => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStonecutterCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyStonecutterCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found),
                    (camp, index) => camp.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.Miner => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyMine>(),
                    mine => mine.WorkerCount,
                    (StrategyMine mine, int index, out StrategyResidentAgent found) => mine.TryGetWorker(index, out found),
                    (mine, index) => mine.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.CoalMiner => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyCoalPit>(),
                    pit => pit.WorkerCount,
                    (StrategyCoalPit pit, int index, out StrategyResidentAgent found) => pit.TryGetWorker(index, out found),
                    (pit, index) => pit.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.Sawyer => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategySawmill>(),
                    sawmill => sawmill.WorkerCount,
                    (StrategySawmill sawmill, int index, out StrategyResidentAgent found) => sawmill.TryGetWorker(index, out found),
                    (sawmill, index) => sawmill.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.Hunter => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyHunterCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyHunterCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found),
                    (camp, index) => camp.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.Fisher => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyFisherHut>(),
                    hut => hut.WorkerCount,
                    (StrategyFisherHut hut, int index, out StrategyResidentAgent found) => hut.TryGetWorker(index, out found),
                    (hut, index) => hut.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.StorageWorker => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStorageYard>(),
                    yard => yard.WorkerCount,
                    (StrategyStorageYard yard, int index, out StrategyResidentAgent found) => yard.TryGetWorker(index, out found),
                    (yard, index) => yard.UnassignWorkerAt(index),
                    out worker),
                StrategyProfessionType.Builder => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStorageYard>(),
                    yard => yard.BuilderCount,
                    (StrategyStorageYard yard, int index, out StrategyResidentAgent found) => yard.TryGetBuilder(index, out found),
                    (yard, index) => yard.UnassignBuilderAt(index),
                    out worker),
                _ => false
            };
        }

        private static bool TryReleaseFromSites<T>(
            T[] sites,
            Func<T, int> getWorkers,
            TryGetWorkerAt<T> tryGetWorker,
            UnassignWorkerAt<T> unassignWorker,
            out StrategyResidentAgent worker)
            where T : Component
        {
            worker = null;
            Array.Sort(sites, (left, right) => GetWorkerCount(right, getWorkers).CompareTo(GetWorkerCount(left, getWorkers)));
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                int count = GetWorkerCount(site, getWorkers);
                if (count <= 0)
                {
                    continue;
                }

                int index = count - 1;
                tryGetWorker(site, index, out worker);
                unassignWorker(site, index);
                return true;
            }

            return false;
        }

        private static int GetWorkerCount<T>(T site, Func<T, int> getWorkers)
            where T : Component
        {
            return site != null ? getWorkers(site) : 0;
        }
    }
}
