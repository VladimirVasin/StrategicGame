using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {

        private ProfessionSnapshot BuildSnapshot(StrategyProfessionType type, int freeWorkers)
        {
            ProfessionSnapshot snapshot = CreateBaseSnapshot(type);
            snapshot.FreeWorkers = freeWorkers;

            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    StrategyLumberjackCamp[] lumberCamps = FindSorted<StrategyLumberjackCamp>();
                    snapshot.Assigned = CountAssigned(lumberCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = lumberCamps.Length * StrategyLumberjackCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Stonecutter:
                    StrategyStonecutterCamp[] stoneCamps = FindSorted<StrategyStonecutterCamp>();
                    snapshot.Assigned = CountAssigned(stoneCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = stoneCamps.Length * StrategyStonecutterCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Miner:
                    StrategyMine[] mines = FindSorted<StrategyMine>();
                    snapshot.Assigned = CountAssigned(mines, mine => mine.WorkerCount);
                    snapshot.Capacity = mines.Length * StrategyMine.MaxWorkers;
                    break;
                case StrategyProfessionType.CoalMiner:
                    StrategyCoalPit[] coalPits = FindSorted<StrategyCoalPit>();
                    snapshot.Assigned = CountAssigned(coalPits, pit => pit.WorkerCount);
                    snapshot.Capacity = coalPits.Length * StrategyCoalPit.MaxWorkers;
                    break;
                case StrategyProfessionType.Hunter:
                    StrategyHunterCamp[] hunterCamps = FindSorted<StrategyHunterCamp>();
                    snapshot.Assigned = CountAssigned(hunterCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = hunterCamps.Length * StrategyHunterCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Fisher:
                    StrategyFisherHut[] fisherHuts = FindSorted<StrategyFisherHut>();
                    snapshot.Assigned = CountAssigned(fisherHuts, hut => hut.WorkerCount);
                    snapshot.Capacity = fisherHuts.Length * StrategyFisherHut.MaxWorkers;
                    break;
                case StrategyProfessionType.StorageWorker:
                    StrategyStorageYard[] storageYards = FindSorted<StrategyStorageYard>();
                    snapshot.Assigned = CountAssigned(storageYards, yard => yard.WorkerCount);
                    snapshot.Capacity = storageYards.Length > 0 ? int.MaxValue : 0;
                    snapshot.IsUnlimited = storageYards.Length > 0;
                    break;
                case StrategyProfessionType.Builder:
                    StrategyStorageYard[] builderYards = FindSorted<StrategyStorageYard>();
                    snapshot.Assigned = CountAssigned(builderYards, yard => yard.BuilderCount);
                    snapshot.Capacity = builderYards.Length > 0 ? int.MaxValue : 0;
                    snapshot.IsUnlimited = builderYards.Length > 0;
                    break;
                case StrategyProfessionType.GranaryWorker:
                    StrategyGranary[] granaries = FindSorted<StrategyGranary>();
                    snapshot.Assigned = CountAssigned(granaries, granary => granary.WorkerCount);
                    snapshot.Capacity = granaries.Length * StrategyGranary.MaxWorkers;
                    break;
            }

            return snapshot;
        }

        private ProfessionSnapshot CreateBaseSnapshot(StrategyProfessionType type)
        {
            return type switch
            {
                StrategyProfessionType.Lumberjack => new ProfessionSnapshot(type, "Lumberjacks", "chop trees and stockpile Logs", new Color(0.45f, 0.62f, 0.32f)),
                StrategyProfessionType.Stonecutter => new ProfessionSnapshot(type, "Stonecutters", "mine Stone with pickaxes", new Color(0.47f, 0.53f, 0.55f)),
                StrategyProfessionType.Miner => new ProfessionSnapshot(type, "Miners", "work underground for Iron", new Color(0.61f, 0.42f, 0.30f)),
                StrategyProfessionType.CoalMiner => new ProfessionSnapshot(type, "Coal Miners", "dig Coal inside pits", new Color(0.33f, 0.37f, 0.38f)),
                StrategyProfessionType.Hunter => new ProfessionSnapshot(type, "Hunters", "hunt rabbits", new Color(0.56f, 0.43f, 0.26f)),
                StrategyProfessionType.Fisher => new ProfessionSnapshot(type, "Fishers", "catch fish near water", new Color(0.32f, 0.54f, 0.63f)),
                StrategyProfessionType.StorageWorker => new ProfessionSnapshot(type, "Storekeepers", "haul Logs, Stone, and Iron", new Color(0.58f, 0.49f, 0.37f)),
                StrategyProfessionType.Builder => new ProfessionSnapshot(type, "Builders", "build structures", new Color(0.75f, 0.55f, 0.27f)),
                StrategyProfessionType.GranaryWorker => new ProfessionSnapshot(type, "Granary Workers", "haul food to the granary", new Color(0.62f, 0.51f, 0.28f)),
                _ => new ProfessionSnapshot(type, "Profession", string.Empty, Color.white)
            };
        }

        private void ChangeProfession(StrategyProfessionType type, bool assign)
        {
            bool success = assign
                ? TryAssign(type, out StrategyResidentAgent worker)
                : TryRemove(type, out worker);

            actionStatusText.text = GetActionMessage(type, assign, success, worker);
            StrategyDebugLogger.Info(
                "ProfessionHud",
                "ProfessionChanged",
                StrategyDebugLogger.F("profession", type),
                StrategyDebugLogger.F("action", assign ? "assign" : "remove"),
                StrategyDebugLogger.F("success", success),
                StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty));
            isDirty = true;
            RefreshUi();
        }

        private bool TryAssign(StrategyProfessionType type, out StrategyResidentAgent worker)
        {
            worker = null;
            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    foreach (StrategyLumberjackCamp camp in FindSorted<StrategyLumberjackCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Stonecutter:
                    foreach (StrategyStonecutterCamp camp in FindSorted<StrategyStonecutterCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Miner:
                    foreach (StrategyMine mine in FindSorted<StrategyMine>())
                    {
                        if (mine != null && mine.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.CoalMiner:
                    foreach (StrategyCoalPit pit in FindSorted<StrategyCoalPit>())
                    {
                        if (pit != null && pit.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Hunter:
                    foreach (StrategyHunterCamp camp in FindSorted<StrategyHunterCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Fisher:
                    foreach (StrategyFisherHut hut in FindSorted<StrategyFisherHut>())
                    {
                        if (hut != null && hut.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    foreach (StrategyStorageYard yard in FindSorted<StrategyStorageYard>())
                    {
                        if (yard != null && yard.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Builder:
                    foreach (StrategyStorageYard yard in FindSorted<StrategyStorageYard>())
                    {
                        if (yard != null && yard.TryAssignNextAvailableBuilder(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.GranaryWorker:
                    foreach (StrategyGranary granary in FindSorted<StrategyGranary>())
                    {
                        if (granary != null && granary.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private bool TryRemove(StrategyProfessionType type, out StrategyResidentAgent worker)
        {
            worker = null;
            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    StrategyLumberjackCamp[] lumberCamps = FindSorted<StrategyLumberjackCamp>();
                    for (int i = lumberCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(lumberCamps[i], lumberCamps[i].WorkerCount, out worker, index => lumberCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Stonecutter:
                    StrategyStonecutterCamp[] stoneCamps = FindSorted<StrategyStonecutterCamp>();
                    for (int i = stoneCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(stoneCamps[i], stoneCamps[i].WorkerCount, out worker, index => stoneCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Miner:
                    StrategyMine[] mines = FindSorted<StrategyMine>();
                    for (int i = mines.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(mines[i], mines[i].WorkerCount, out worker, index => mines[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.CoalMiner:
                    StrategyCoalPit[] coalPits = FindSorted<StrategyCoalPit>();
                    for (int i = coalPits.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(coalPits[i], coalPits[i].WorkerCount, out worker, index => coalPits[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Hunter:
                    StrategyHunterCamp[] hunterCamps = FindSorted<StrategyHunterCamp>();
                    for (int i = hunterCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(hunterCamps[i], hunterCamps[i].WorkerCount, out worker, index => hunterCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Fisher:
                    StrategyFisherHut[] fisherHuts = FindSorted<StrategyFisherHut>();
                    for (int i = fisherHuts.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(fisherHuts[i], fisherHuts[i].WorkerCount, out worker, index => fisherHuts[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    StrategyStorageYard[] storageYards = FindSorted<StrategyStorageYard>();
                    for (int i = storageYards.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(storageYards[i], storageYards[i].WorkerCount, out worker, index => storageYards[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Builder:
                    StrategyStorageYard[] builderYards = FindSorted<StrategyStorageYard>();
                    for (int i = builderYards.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveBuilder(builderYards[i], out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.GranaryWorker:
                    StrategyGranary[] granaries = FindSorted<StrategyGranary>();
                    for (int i = granaries.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(granaries[i], granaries[i].WorkerCount, out worker, index => granaries[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool TryRemoveWorker<T>(T site, int workerCount, out StrategyResidentAgent worker, Action<int> unassignAt)
            where T : Component
        {
            worker = null;
            if (site == null || workerCount <= 0)
            {
                return false;
            }

            int index = workerCount - 1;
            switch (site)
            {
                case StrategyLumberjackCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyStonecutterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyMine mine:
                    mine.TryGetWorker(index, out worker);
                    break;
                case StrategyCoalPit pit:
                    pit.TryGetWorker(index, out worker);
                    break;
                case StrategyHunterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyFisherHut hut:
                    hut.TryGetWorker(index, out worker);
                    break;
                case StrategyStorageYard yard:
                    yard.TryGetWorker(index, out worker);
                    break;
                case StrategyGranary granary:
                    granary.TryGetWorker(index, out worker);
                    break;
            }

            unassignAt(index);
            return true;
        }

        private static bool TryRemoveBuilder(StrategyStorageYard yard, out StrategyResidentAgent worker)
        {
            worker = null;
            if (yard == null || yard.BuilderCount <= 0)
            {
                return false;
            }

            int index = yard.BuilderCount - 1;
            yard.TryGetBuilder(index, out worker);
            yard.UnassignBuilderAt(index);
            return true;
        }

        private string GetActionMessage(StrategyProfessionType type, bool assign, bool success, StrategyResidentAgent worker)
        {
            string title = CreateBaseSnapshot(type).Title;
            if (!success)
            {
                return assign
                    ? title + ": no free residents or workplaces"
                    : title + ": nobody to remove";
            }

            return worker != null
                ? worker.FullName
                : assign
                    ? title + ": assigned"
                    : title + ": removed";
        }

        private int CountFreeWorkers()
        {
            if (population == null)
            {
                return 0;
            }

            int count = 0;
            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAssigned<T>(T[] items, Func<T, int> getCount)
            where T : Component
        {
            int total = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    total += getCount(items[i]);
                }
            }

            return total;
        }

        private static T[] FindSorted<T>()
            where T : Component
        {
            T[] items = UnityEngine.Object.FindObjectsByType<T>();
            Array.Sort(items, (left, right) => CompareOrigin(GetOrigin(left), GetOrigin(right)));
            return items;
        }

        private static int CompareOrigin(Vector2Int left, Vector2Int right)
        {
            int y = left.y.CompareTo(right.y);
            return y != 0 ? y : left.x.CompareTo(right.x);
        }

        private static Vector2Int GetOrigin(Component component)
        {
            return component switch
            {
                StrategyLumberjackCamp camp => camp.Origin,
                StrategyStonecutterCamp camp => camp.Origin,
                StrategyMine mine => mine.Origin,
                StrategyCoalPit pit => pit.Origin,
                StrategyHunterCamp camp => camp.Origin,
                StrategyFisherHut hut => hut.Origin,
                StrategyStorageYard yard => yard.Origin,
                StrategyGranary granary => granary.Origin,
                _ => Vector2Int.zero
            };
        }

    }
}
