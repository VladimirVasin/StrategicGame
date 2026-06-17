using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private int ReleaseDisabledProfessionWorkers()
        {
            int released = 0;
            for (int i = 0; i < AutoManagedProfessions.Length; i++)
            {
                StrategyProfessionType profession = AutoManagedProfessions[i];
                if (IsProfessionManualLocked(profession)
                    || settings.GetPriority(GetProfessionCategory(profession)) > 0)
                {
                    continue;
                }

                int current = CountAssignedProfession(profession);
                while (current > 0)
                {
                    if (!TryReleaseProfessionWorker(profession, out StrategyResidentAgent resident, true))
                    {
                        break;
                    }

                    current--;
                    released++;
                    TryAddReleasedCandidate(resident);
                    StrategyDebugLogger.Info(
                        "AutoWorkforce",
                        "AutoWorkforceReleasedDisabled",
                        StrategyDebugLogger.F("profession", profession),
                        StrategyDebugLogger.F("resident", resident != null ? resident.FullName : string.Empty),
                        StrategyDebugLogger.F("remaining", current));
                }
            }

            return released;
        }

        private int RebalanceOverstaffedProfessions(int maxRelease)
        {
            int released = 0;
            if (maxRelease <= 0)
            {
                return released;
            }

            for (int i = 0; i < AutoManagedProfessions.Length; i++)
            {
                StrategyProfessionType profession = AutoManagedProfessions[i];
                if (IsProfessionManualLocked(profession))
                {
                    continue;
                }

                int target = desiredProfessionTargets.TryGetValue(profession, out int value) ? value : 0;
                int current = CountAssignedProfession(profession);
                while (current > target && released < maxRelease)
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

                if (released >= maxRelease)
                {
                    break;
                }
            }

            return released;
        }

        private int CountReleasableProfessionWorkers(StrategyProfessionType profession)
        {
            return profession switch
            {
                StrategyProfessionType.Lumberjack => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyLumberjackCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyLumberjackCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found)),
                StrategyProfessionType.Stonecutter => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStonecutterCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyStonecutterCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found)),
                StrategyProfessionType.Miner => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyMine>(),
                    mine => mine.WorkerCount,
                    (StrategyMine mine, int index, out StrategyResidentAgent found) => mine.TryGetWorker(index, out found)),
                StrategyProfessionType.CoalMiner => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyCoalPit>(),
                    pit => pit.WorkerCount,
                    (StrategyCoalPit pit, int index, out StrategyResidentAgent found) => pit.TryGetWorker(index, out found)),
                StrategyProfessionType.Sawyer => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategySawmill>(),
                    sawmill => sawmill.WorkerCount,
                    (StrategySawmill sawmill, int index, out StrategyResidentAgent found) => sawmill.TryGetWorker(index, out found)),
                StrategyProfessionType.Hunter => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyHunterCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyHunterCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found)),
                StrategyProfessionType.Fisher => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyFisherHut>(),
                    hut => hut.WorkerCount,
                    (StrategyFisherHut hut, int index, out StrategyResidentAgent found) => hut.TryGetWorker(index, out found)),
                StrategyProfessionType.StorageWorker => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStorageYard>(),
                    yard => yard.WorkerCount,
                    (StrategyStorageYard yard, int index, out StrategyResidentAgent found) => yard.TryGetWorker(index, out found)),
                StrategyProfessionType.Builder => CountReleasableFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStorageYard>(),
                    yard => yard.BuilderCount,
                    (StrategyStorageYard yard, int index, out StrategyResidentAgent found) => yard.TryGetBuilder(index, out found)),
                _ => 0
            };
        }

        private bool TryReleaseProfessionWorker(
            StrategyProfessionType profession,
            out StrategyResidentAgent worker,
            bool allowActiveRelease = false)
        {
            worker = null;
            return profession switch
            {
                StrategyProfessionType.Lumberjack => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyLumberjackCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyLumberjackCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found),
                    (camp, index) => camp.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.Stonecutter => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStonecutterCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyStonecutterCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found),
                    (camp, index) => camp.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.Miner => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyMine>(),
                    mine => mine.WorkerCount,
                    (StrategyMine mine, int index, out StrategyResidentAgent found) => mine.TryGetWorker(index, out found),
                    (mine, index) => mine.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.CoalMiner => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyCoalPit>(),
                    pit => pit.WorkerCount,
                    (StrategyCoalPit pit, int index, out StrategyResidentAgent found) => pit.TryGetWorker(index, out found),
                    (pit, index) => pit.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.Sawyer => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategySawmill>(),
                    sawmill => sawmill.WorkerCount,
                    (StrategySawmill sawmill, int index, out StrategyResidentAgent found) => sawmill.TryGetWorker(index, out found),
                    (sawmill, index) => sawmill.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.Hunter => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyHunterCamp>(),
                    camp => camp.WorkerCount,
                    (StrategyHunterCamp camp, int index, out StrategyResidentAgent found) => camp.TryGetWorker(index, out found),
                    (camp, index) => camp.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.Fisher => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyFisherHut>(),
                    hut => hut.WorkerCount,
                    (StrategyFisherHut hut, int index, out StrategyResidentAgent found) => hut.TryGetWorker(index, out found),
                    (hut, index) => hut.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.StorageWorker => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStorageYard>(),
                    yard => yard.WorkerCount,
                    (StrategyStorageYard yard, int index, out StrategyResidentAgent found) => yard.TryGetWorker(index, out found),
                    (yard, index) => yard.UnassignWorkerAt(index),
                    out worker,
                    allowActiveRelease),
                StrategyProfessionType.Builder => TryReleaseFromSites(
                    UnityEngine.Object.FindObjectsByType<StrategyStorageYard>(),
                    yard => yard.BuilderCount,
                    (StrategyStorageYard yard, int index, out StrategyResidentAgent found) => yard.TryGetBuilder(index, out found),
                    (yard, index) => yard.UnassignBuilderAt(index),
                    out worker,
                    allowActiveRelease),
                _ => false
            };
        }

        private static int CountReleasableFromSites<T>(
            T[] sites,
            Func<T, int> getWorkers,
            TryGetWorkerAt<T> tryGetWorker)
            where T : Component
        {
            int total = 0;
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                int count = GetWorkerCount(site, getWorkers);
                for (int index = 0; index < count; index++)
                {
                    if (tryGetWorker(site, index, out StrategyResidentAgent worker)
                        && CanReleaseWorkerForAutoRebalance(worker))
                    {
                        total++;
                    }
                }
            }

            return total;
        }

        private static bool TryReleaseFromSites<T>(
            T[] sites,
            Func<T, int> getWorkers,
            TryGetWorkerAt<T> tryGetWorker,
            UnassignWorkerAt<T> unassignWorker,
            out StrategyResidentAgent worker,
            bool allowActiveRelease)
            where T : Component
        {
            worker = null;
            Array.Sort(sites, (left, right) => GetWorkerCount(right, getWorkers).CompareTo(GetWorkerCount(left, getWorkers)));
            for (int i = 0; i < sites.Length; i++)
            {
                T site = sites[i];
                int count = GetWorkerCount(site, getWorkers);
                for (int index = count - 1; index >= 0; index--)
                {
                    if (!tryGetWorker(site, index, out StrategyResidentAgent candidate)
                        || !allowActiveRelease && !CanReleaseWorkerForAutoRebalance(candidate))
                    {
                        continue;
                    }

                    worker = candidate;
                    unassignWorker(site, index);
                    return true;
                }
            }

            return false;
        }

        private static bool CanReleaseWorkerForAutoRebalance(StrategyResidentAgent worker)
        {
            return worker != null && worker.Activity == StrategyResidentAgent.ResidentActivity.Idle;
        }

        private static int GetWorkerCount<T>(T site, Func<T, int> getWorkers)
            where T : Component
        {
            return site != null ? getWorkers(site) : 0;
        }
    }
}
