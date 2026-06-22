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
            StrategyConstructionSite[] sites = cachedConstructionSites;
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
            StrategyConstructionSite[] sites = cachedConstructionSites;
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
            AddBacklog(CountAvailableClay(out Vector3 clayWorld), clayWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailablePlanks(out Vector3 planksWorld), planksWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailablePottery(out Vector3 potteryWorld), potteryWorld, ref backlog, ref weighted);
            AddBacklog(CountAvailableTools(out Vector3 toolsWorld), toolsWorld, ref backlog, ref weighted);
            AddBacklog(CountProductionInputBacklog(out Vector3 inputWorld), inputWorld, ref backlog, ref weighted);
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
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.LogsStored : 0;
            }

            total += CountAvailableLogs(out _);
            return total;
        }

        private int CountTotalStone()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.StoneStored : 0;
            }

            total += CountAvailableStone(out _);
            return total;
        }

        private int CountTotalPlanks()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.PlanksStored : 0;
            }

            total += CountAvailablePlanks(out _);
            return total;
        }

        private int CountTotalIron()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.IronStored : 0;
            }

            total += CountAvailableIron(out _);
            return total;
        }

        private int CountTotalCoal()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.CoalStored : 0;
            }

            total += CountAvailableCoal(out _);
            return total;
        }

        private int CountTotalClay()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.ClayStored : 0;
            }

            total += CountAvailableClay(out _);
            return total;
        }

        private int CountTotalPottery()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.PotteryStored : 0;
            }

            total += CountAvailablePottery(out _);
            return total;
        }

        private int CountTotalTools()
        {
            int total = 0;
            foreach (StrategyStorageYard yard in cachedStorageYards)
            {
                total += yard != null ? yard.ToolsStored : 0;
            }

            total += CountAvailableTools(out _);
            return total;
        }

        private float CountCachedGranaryHouseholdRations()
        {
            float total = 0f;
            for (int i = 0; i < cachedGranaries.Length; i++)
            {
                StrategyGranary granary = cachedGranaries[i];
                total += granary != null ? granary.AvailableHouseholdRationValue : 0f;
            }

            return total;
        }

        private int CountCachedRawHouseholdPotteryDemand()
        {
            int demand = 0;
            for (int i = 0; i < cachedPlacedBuildings.Length; i++)
            {
                StrategyPlacedBuilding house = cachedPlacedBuildings[i];
                if (house != null && house.Tool == StrategyBuildTool.House && house.Resources != null)
                {
                    demand += house.Resources.GetPotteryDemandForCooking(CalculateCachedHouseDailyRationNeed(house));
                }
            }

            return demand;
        }

        private static float CalculateCachedHouseDailyRationNeed(StrategyPlacedBuilding house)
        {
            float total = 0f;
            if (house == null)
            {
                return total;
            }

            for (int i = 0; i < house.Residents.Count; i++)
            {
                StrategyResidentAgent resident = house.Residents[i];
                if (resident != null && resident.Home == house && !resident.IsPendingRefugee)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }

        private int CountAvailableLogs(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyLumberjackCamp camp in cachedLumberjackCamps)
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
            foreach (StrategyStonecutterCamp camp in cachedStonecutterCamps)
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
            foreach (StrategyMine mine in cachedMines)
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
            foreach (StrategyCoalPit pit in cachedCoalPits)
            {
                if (pit != null && pit.AvailableCoal > 0)
                {
                    total += pit.AvailableCoal;
                    world = pit.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailableClay(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyClayPit pit in cachedClayPits)
            {
                if (pit != null && pit.AvailableClay > 0)
                {
                    total += pit.AvailableClay;
                    world = pit.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailablePlanks(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategySawmill sawmill in cachedSawmills)
            {
                if (sawmill != null && sawmill.AvailablePlanks > 0)
                {
                    total += sawmill.AvailablePlanks;
                    world = sawmill.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailablePottery(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyKiln kiln in cachedKilns)
            {
                if (kiln != null && kiln.AvailablePottery > 0)
                {
                    total += kiln.AvailablePottery;
                    world = kiln.FootprintBounds.center;
                }
            }

            return total;
        }

        private int CountAvailableTools(out Vector3 world)
        {
            int total = 0;
            world = transform.position;
            foreach (StrategyForge forge in cachedForges)
            {
                if (forge != null && forge.AvailableTools > 0)
                {
                    total += forge.AvailableTools;
                    world = forge.FootprintBounds.center;
                }
            }

            return total;
        }

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

            return total;
        }

        private int CountProductionInputBacklog(out Vector3 focus)
        {
            int backlog = 0;
            Vector3 weighted = Vector3.zero;
            for (int i = 0; i < cachedSawmills.Length; i++)
            {
                StrategySawmill sawmill = cachedSawmills[i];
                if (sawmill == null
                    || !sawmill.TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
                    || resource == StrategyResourceType.None
                    || StrategyFoodNutrition.IsFood(resource)
                    || maxAmount <= 0)
                {
                    continue;
                }

                int available = 0;
                for (int yardIndex = 0; yardIndex < cachedStorageYards.Length; yardIndex++)
                {
                    StrategyStorageYard yard = cachedStorageYards[yardIndex];
                    available += yard != null ? yard.GetAvailableLogisticsAmount(resource) : 0;
                }

                int amount = Mathf.Min(maxAmount, available);
                if (amount <= 0)
                {
                    continue;
                }

                backlog += amount;
                weighted += sawmill.FootprintBounds.center * amount;
            }

            for (int i = 0; i < cachedKilns.Length; i++)
            {
                StrategyKiln kiln = cachedKilns[i];
                if (kiln == null
                    || !kiln.TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
                    || resource == StrategyResourceType.None
                    || StrategyFoodNutrition.IsFood(resource)
                    || maxAmount <= 0)
                {
                    continue;
                }

                int available = 0;
                for (int yardIndex = 0; yardIndex < cachedStorageYards.Length; yardIndex++)
                {
                    StrategyStorageYard yard = cachedStorageYards[yardIndex];
                    available += yard != null ? yard.GetAvailableLogisticsAmount(resource) : 0;
                }

                int amount = Mathf.Min(maxAmount, available);
                if (amount <= 0)
                {
                    continue;
                }

                backlog += amount;
                weighted += kiln.FootprintBounds.center * amount;
            }

            for (int i = 0; i < cachedForges.Length; i++)
            {
                StrategyForge forge = cachedForges[i];
                if (forge == null
                    || !forge.TryGetInputDeliveryRequest(out StrategyResourceType resource, out int maxAmount)
                    || resource == StrategyResourceType.None
                    || StrategyFoodNutrition.IsFood(resource)
                    || maxAmount <= 0)
                {
                    continue;
                }

                int available = 0;
                for (int yardIndex = 0; yardIndex < cachedStorageYards.Length; yardIndex++)
                {
                    StrategyStorageYard yard = cachedStorageYards[yardIndex];
                    available += yard != null ? yard.GetAvailableLogisticsAmount(resource) : 0;
                }

                int amount = Mathf.Min(maxAmount, available);
                if (amount <= 0)
                {
                    continue;
                }

                backlog += amount;
                weighted += forge.FootprintBounds.center * amount;
            }

            focus = backlog > 0 ? weighted / backlog : Vector3.zero;
            return backlog;
        }
    }
}
