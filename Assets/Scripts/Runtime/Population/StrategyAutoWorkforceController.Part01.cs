using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private const float BasePriorityScore = 100f;
        private const float FoodReserveTargetDays = 4f;
        private const float FoodEmergencyDays = 1.5f;
        private const float ResourceShortagePriorityBonus = 360f;
        private const float ResourceShortageUrgencyPerUnit = 14f;
        private const float EmptyResourceUrgencyBonus = 80f;

        private void CollectDemands()
        {
            demands.Clear();
            desiredProfessionTargets.Clear();
            coverageProfessionFloors.Clear();
            AddConstructionDemands();
            AddLogisticsDemands();
            AddFoodDemands();
            AddMaterialDemands();
            AddCoverageFloorDemands();
        }

        private void AddConstructionDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Construction);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Builder))
            {
                return;
            }

            StrategyConstructionSite[] sites = cachedConstructionSites;
            int activeSites = 0;
            int readySites = 0;
            Vector3 focus = transform.position;
            float bestScore = float.MinValue;
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                activeSites++;
                if (site.ResourcesComplete)
                {
                    readySites++;
                }

                float siteScore = (site.ResourcesComplete ? 60f : 30f)
                    + site.Cost.Total
                    + site.Progress * 20f
                    - site.BuilderCount * 8f;
                if (siteScore > bestScore)
                {
                    bestScore = siteScore;
                    focus = site.FootprintBounds.center;
                }
            }

            if (activeSites <= 0)
            {
                return;
            }

            int desiredBuilders = priority;
            SetDesiredProfessionTarget(StrategyProfessionType.Builder, desiredBuilders);
            int currentBuilders = CountAssignedProfession(StrategyProfessionType.Builder);
            int needed = Mathf.Max(0, desiredBuilders - currentBuilders);
            if (needed <= 0 || !TryFindStorageTarget(focus, out StrategyStorageYard yard))
            {
                return;
            }

            AddDemand(
                StrategyProfessionType.Builder,
                StrategyAutoWorkforceCategory.Construction,
                yard,
                focus,
                needed,
                35f + activeSites * 12f + readySites * 18f,
                "construction_sites");
        }

        private void AddLogisticsDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Logistics);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.StorageWorker))
            {
                return;
            }

            int backlog = CountLogisticsBacklog(out Vector3 focus);
            int activeConstructionNeeds = CountConstructionMaterialNeeds();
            if (!TryFindStorageTarget(focus, out StrategyStorageYard yard))
            {
                return;
            }

            int desired = priority;
            SetDesiredProfessionTarget(StrategyProfessionType.StorageWorker, desired);
            int current = CountAssignedProfession(StrategyProfessionType.StorageWorker);
            int needed = Mathf.Max(0, desired - current);
            if (needed <= 0)
            {
                return;
            }

            AddDemand(
                StrategyProfessionType.StorageWorker,
                StrategyAutoWorkforceCategory.Logistics,
                yard,
                focus,
                needed,
                20f + backlog * 4f + activeConstructionNeeds * 10f,
                "resource_backlog");
        }

        private void AddFoodDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Food);
            if (priority <= 0)
            {
                return;
            }

            float dailyNeed = GetDailyRationNeed();
            float storedRations = StrategyGranary.GetTotalSettlementFoodRations();
            float reserveDays = dailyNeed <= 0.01f ? FoodReserveTargetDays : storedRations / dailyNeed;
            bool hasHunterCamp = cachedHunterCamps.Length > 0;
            bool hasFisherHut = cachedFisherHuts.Length > 0;
            if (!hasHunterCamp && !hasFisherHut)
            {
                return;
            }

            int desiredFoodWorkers = priority;
            int hunterWorkers = hasFisherHut ? Mathf.CeilToInt(desiredFoodWorkers * 0.5f) : desiredFoodWorkers;
            int fisherWorkers = hasHunterCamp ? desiredFoodWorkers - hunterWorkers : desiredFoodWorkers;
            float urgency = Mathf.Max(0f, FoodReserveTargetDays - reserveDays) * 25f;
            if (reserveDays < FoodReserveTargetDays)
            {
                urgency += ResourceShortagePriorityBonus;
            }

            if (reserveDays <= FoodEmergencyDays)
            {
                urgency += EmptyResourceUrgencyBonus;
            }

            AddHunterDemands(hunterWorkers, urgency);
            AddFisherDemands(fisherWorkers, urgency);
        }

        private void AddMaterialDemands()
        {
            AddLumberDemands();
            AddStoneDemands();
            AddPlankDemands();
            AddIronDemands();
            AddCoalDemands();
        }

        private void AddLumberDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Wood);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Lumberjack))
            {
                return;
            }

            int totalLogs = CountTotalLogs();
            int target = 10 + CountNeededConstruction(StrategyConstructionResourceKind.Logs);
            int shortage = Mathf.Max(0, target - totalLogs);
            float urgency = GetResourceShortageUrgency(shortage, totalLogs);
            AddCappedCampDemands<StrategyLumberjackCamp>(
                StrategyProfessionType.Lumberjack,
                StrategyAutoWorkforceCategory.Wood,
                priority,
                "logs_shortage",
                camp => camp.WorkerCount,
                camp => StrategyLumberjackCamp.MaxWorkers,
                camp => camp.HasStorageSpace,
                camp => camp.FootprintBounds.center,
                urgency);
        }

        private void AddStoneDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Stone);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Stonecutter))
            {
                return;
            }

            int totalStone = CountTotalStone();
            int target = 8 + CountNeededConstruction(StrategyConstructionResourceKind.Stone);
            int shortage = Mathf.Max(0, target - totalStone);
            float urgency = GetResourceShortageUrgency(shortage, totalStone);
            AddCappedCampDemands<StrategyStonecutterCamp>(
                StrategyProfessionType.Stonecutter,
                StrategyAutoWorkforceCategory.Stone,
                priority,
                "stone_shortage",
                camp => camp.WorkerCount,
                camp => StrategyStonecutterCamp.MaxWorkers,
                camp => camp.HasStorageSpace,
                camp => camp.FootprintBounds.center,
                urgency);
        }

        private void AddPlankDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Planks);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Sawyer))
            {
                return;
            }

            int target = 4 + CountNeededConstruction(StrategyConstructionResourceKind.Planks);
            int totalPlanks = CountTotalPlanks();
            int shortage = Mathf.Max(0, target - totalPlanks);
            if (CountTotalLogs() <= 0)
            {
                return;
            }

            AddCappedCampDemands<StrategySawmill>(
                StrategyProfessionType.Sawyer,
                StrategyAutoWorkforceCategory.Planks,
                priority,
                "planks_shortage",
                sawmill => sawmill.WorkerCount,
                sawmill => StrategySawmill.MaxWorkers,
                sawmill => sawmill.CanStartWorkCycle() || sawmill.CanAcceptInputLogs(1),
                sawmill => sawmill.FootprintBounds.center,
                GetResourceShortageUrgency(shortage, totalPlanks));
        }

        private void AddIronDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Iron);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Miner))
            {
                return;
            }

            int totalIron = CountTotalIron();
            int shortage = Mathf.Max(0, priority * 2 - totalIron);
            AddCappedCampDemands<StrategyMine>(
                StrategyProfessionType.Miner,
                StrategyAutoWorkforceCategory.Iron,
                priority,
                "iron_low_stock",
                mine => mine.WorkerCount,
                mine => StrategyMine.MaxWorkers,
                mine => mine.HasStorageSpace,
                mine => mine.FootprintBounds.center,
                GetResourceShortageUrgency(shortage, totalIron));
        }

        private void AddCoalDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Coal);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.CoalMiner))
            {
                return;
            }

            int totalCoal = CountTotalCoal();
            int shortage = Mathf.Max(0, priority * 2 - totalCoal);
            AddCappedCampDemands<StrategyCoalPit>(
                StrategyProfessionType.CoalMiner,
                StrategyAutoWorkforceCategory.Coal,
                priority,
                "coal_low_stock",
                pit => pit.WorkerCount,
                pit => StrategyCoalPit.MaxWorkers,
                pit => pit.HasStorageSpace,
                pit => pit.FootprintBounds.center,
                GetResourceShortageUrgency(shortage, totalCoal));
        }

        private static float GetResourceShortageUrgency(int shortage, int totalStock)
        {
            if (shortage <= 0)
            {
                return 0f;
            }

            return ResourceShortagePriorityBonus
                + shortage * ResourceShortageUrgencyPerUnit
                + (totalStock <= 0 ? EmptyResourceUrgencyBonus : 0f);
        }
    }
}
