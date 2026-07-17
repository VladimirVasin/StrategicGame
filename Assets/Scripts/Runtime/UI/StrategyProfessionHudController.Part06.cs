using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {
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
                case StrategyClayPit pit:
                    pit.TryGetWorker(index, out worker);
                    break;
                case StrategySawmill sawmill:
                    sawmill.TryGetWorker(index, out worker);
                    break;
                case StrategyKiln kiln:
                    kiln.TryGetWorker(index, out worker);
                    break;
                case StrategyForge forge:
                    forge.TryGetWorker(index, out worker);
                    break;
                case StrategyHunterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyFisherHut hut:
                    hut.TryGetWorker(index, out worker);
                    break;
                case StrategyForagerCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyScoutLodge lodge:
                    lodge.TryGetWorker(index, out worker);
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

        private int CountAppointableScoutCandidates(StrategyScoutLodge[] lodges)
        {
            if (population == null || lodges == null || lodges.Length == 0)
            {
                return 0;
            }

            int count = 0;
            foreach (StrategyResidentAgent resident in population.Residents)
            {
                for (int i = 0; i < lodges.Length; i++)
                {
                    if (lodges[i] != null && lodges[i].CanAppointWorker(resident))
                    {
                        count++;
                        break;
                    }
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
                StrategyClayPit pit => pit.Origin,
                StrategySawmill sawmill => sawmill.Origin,
                StrategyKiln kiln => kiln.Origin,
                StrategyForge forge => forge.Origin,
                StrategyHunterCamp camp => camp.Origin,
                StrategyFisherHut hut => hut.Origin,
                StrategyForagerCamp camp => camp.Origin,
                StrategyScoutLodge lodge => lodge.Origin,
                StrategyStorageYard yard => yard.Origin,
                StrategyGranary granary => granary.Origin,
                _ => Vector2Int.zero
            };
        }
    }
}
