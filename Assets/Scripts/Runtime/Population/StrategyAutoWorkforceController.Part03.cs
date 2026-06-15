using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private float GetDailyRationNeed()
        {
            if (population == null)
            {
                return 0f;
            }

            float total = 0f;
            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (resident != null && !resident.IsPendingRefugee)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }

        private int CountNeededConstruction(StrategyConstructionResourceKind kind)
        {
            int total = 0;
            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                if (kind == StrategyConstructionResourceKind.Logs)
                {
                    total += site.NeededLogs;
                }
                else if (kind == StrategyConstructionResourceKind.Stone)
                {
                    total += site.NeededStone;
                }
                else if (kind == StrategyConstructionResourceKind.Planks)
                {
                    total += site.NeededPlanks;
                }
            }

            return total;
        }

        private int CountConstructionMaterialNeeds()
        {
            int count = 0;
            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site != null && !site.IsCompleted && !site.ResourcesComplete)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountLogisticsBacklog(out Vector3 focus)
        {
            int backlog = 0;
            Vector3 weighted = Vector3.zero;
            AddBacklog(CountAvailableLogs(out Vector3 logsWorld), logsWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailableStone(out Vector3 stoneWorld), stoneWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailableIron(out Vector3 ironWorld), ironWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailableCoal(out Vector3 coalWorld), coalWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailablePlanks(out Vector3 planksWorld), planksWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailableFood(out Vector3 foodWorld), foodWorld, ref backlog, ref weighted);
            focus = backlog > 0 ? weighted / backlog : transform.position;
            return backlog;
        }

        private static void AddBacklog(int amount, Vector3 world, ref int backlog, ref Vector3 weighted)
        {
            if (amount <= 0)
            {
                return;
            }

            backlog += amount;
            weighted += world * amount;
        }

        private int CountTotalLogs()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in Object.FindObjectsByType<StrategyStorageYard>())
            {
                total += yard != null ? yard.LogsStored : 0;
            }

            total += CountAvailableLogs(out _);
            return total;
        }

        private int CountTotalStone()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in Object.FindObjectsByType<StrategyStorageYard>())
            {
                total += yard != null ? yard.StoneStored : 0;
            }

            total += CountAvailableStone(out _);
            return total;
        }

        private int CountTotalPlanks()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in Object.FindObjectsByType<StrategyStorageYard>())
            {
                total += yard != null ? yard.PlanksStored : 0;
            }

            total += CountAvailablePlanks(out _);
            return total;
        }

        private int CountTotalIron()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in Object.FindObjectsByType<StrategyStorageYard>())
            {
                total += yard != null ? yard.IronStored : 0;
            }

            total += CountAvailableIron(out _);
            return total;
        }

        private int CountTotalCoal()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in Object.FindObjectsByType<StrategyStorageYard>())
            {
                total += yard != null ? yard.CoalStored : 0;
            }

            total += CountAvailableCoal(out _);
            return total;
        }

        private int CountAvailableLogs(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyLumberjackCamp camp in Object.FindObjectsByType<StrategyLumberjackCamp>())
            {
                if (camp != null && camp.AvailableLogs > 0)
                {
                    total += camp.AvailableLogs;
                    world = camp.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailableStone(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyStonecutterCamp camp in Object.FindObjectsByType<StrategyStonecutterCamp>())
            {
                if (camp != null && camp.AvailableStone > 0)
                {
                    total += camp.AvailableStone;
                    world = camp.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailableIron(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyMine mine in Object.FindObjectsByType<StrategyMine>())
            {
                if (mine != null && mine.AvailableIron > 0)
                {
                    total += mine.AvailableIron;
                    world = mine.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailableCoal(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyCoalPit pit in Object.FindObjectsByType<StrategyCoalPit>())
            {
                if (pit != null && pit.AvailableCoal > 0)
                {
                    total += pit.AvailableCoal;
                    world = pit.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailablePlanks(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategySawmill sawmill in Object.FindObjectsByType<StrategySawmill>())
            {
                if (sawmill != null && sawmill.AvailablePlanks > 0)
                {
                    total += sawmill.AvailablePlanks;
                    world = sawmill.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailableFood(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyHunterCamp camp in Object.FindObjectsByType<StrategyHunterCamp>())
            {
                if (camp != null && camp.AvailableGame > 0)
                {
                    total += camp.AvailableGame;
                    world = camp.FootprintBounds.center;
                }
            }

            foreach (StrategyFisherHut hut in Object.FindObjectsByType<StrategyFisherHut>())
            {
                if (hut != null && hut.AvailableFish > 0)
                {
                    total += hut.AvailableFish;
                    world = hut.FootprintBounds.center;
                }
            }

            return total;
        }
    }
}
