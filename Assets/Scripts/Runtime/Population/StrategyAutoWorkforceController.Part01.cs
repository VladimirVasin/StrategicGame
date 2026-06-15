using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private const float BasePriorityScore = 100f;
        private const float FoodReserveTargetDays = 4f;
        private const float FoodEmergencyDays = 1.5f;

        private void CollectDemands()
        {
            demands.Clear();
            desiredProfessionTargets.Clear();
            AddConstructionDemands();
            AddLogisticsDemands();
            AddFoodDemands();
            AddMaterialDemands();
        }

        private void AddConstructionDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Construction);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Builder))
            {
                return;
            }

            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            int activeSites = 0;
            int readySites = 0;
            int desiredBuilders = 0;
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

                int siteTarget = priority >= 5 ? 4 : priority >= 3 ? 3 : 1;
                if (!site.ResourcesComplete)
                {
                    siteTarget = Mathf.Max(1, siteTarget - 1);
                }

                desiredBuilders += siteTarget;
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

            desiredBuilders = Mathf.Clamp(desiredBuilders, 0, priority);
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
            if (backlog <= 0 && activeConstructionNeeds <= 0)
            {
                return;
            }

            int desired = Mathf.Clamp(Mathf.CeilToInt(backlog / 4f) + Mathf.Min(2, activeConstructionNeeds), 1, priority);
            SetDesiredProfessionTarget(StrategyProfessionType.StorageWorker, desired);
            int current = CountAssignedProfession(StrategyProfessionType.StorageWorker);
            int needed = Mathf.Max(0, desired - current);
            if (needed <= 0 || !TryFindStorageTarget(focus, out StrategyStorageYard yard))
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
            if (reserveDays >= FoodReserveTargetDays)
            {
                return;
            }

            int desiredFoodWorkers = reserveDays <= FoodEmergencyDays ? 4 : reserveDays <= 2.5f ? 3 : 2;
            desiredFoodWorkers = Mathf.Min(desiredFoodWorkers, priority);
            bool hasHunterCamp = Object.FindObjectsByType<StrategyHunterCamp>().Length > 0;
            bool hasFisherHut = Object.FindObjectsByType<StrategyFisherHut>().Length > 0;
            int hunterWorkers = hasFisherHut ? Mathf.CeilToInt(desiredFoodWorkers * 0.5f) : desiredFoodWorkers;
            int fisherWorkers = hasHunterCamp ? desiredFoodWorkers - hunterWorkers : desiredFoodWorkers;
            float urgency = (FoodReserveTargetDays - reserveDays) * 25f;
            AddHunterDemands(hunterWorkers * 4, urgency);
            AddFisherDemands(fisherWorkers * 4, urgency);
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

            int target = 10 + CountNeededConstruction(StrategyConstructionResourceKind.Logs);
            int shortage = Mathf.Max(0, target - CountTotalLogs());
            AddCappedCampDemands<StrategyLumberjackCamp>(
                StrategyProfessionType.Lumberjack,
                StrategyAutoWorkforceCategory.Wood,
                shortage,
                "logs_shortage",
                camp => camp.WorkerCount,
                camp => StrategyLumberjackCamp.MaxWorkers,
                camp => camp.HasStorageSpace,
                camp => camp.FootprintBounds.center);
        }

        private void AddStoneDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Stone);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Stonecutter))
            {
                return;
            }

            int target = 8 + CountNeededConstruction(StrategyConstructionResourceKind.Stone);
            int shortage = Mathf.Max(0, target - CountTotalStone());
            AddCappedCampDemands<StrategyStonecutterCamp>(
                StrategyProfessionType.Stonecutter,
                StrategyAutoWorkforceCategory.Stone,
                shortage,
                "stone_shortage",
                camp => camp.WorkerCount,
                camp => StrategyStonecutterCamp.MaxWorkers,
                camp => camp.HasStorageSpace,
                camp => camp.FootprintBounds.center);
        }

        private void AddPlankDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Planks);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Sawyer))
            {
                return;
            }

            int target = 4 + CountNeededConstruction(StrategyConstructionResourceKind.Planks);
            int shortage = Mathf.Max(0, target - CountTotalPlanks());
            if (shortage <= 0 || CountTotalLogs() <= 0)
            {
                return;
            }

            AddCappedCampDemands<StrategySawmill>(
                StrategyProfessionType.Sawyer,
                StrategyAutoWorkforceCategory.Planks,
                shortage,
                "planks_shortage",
                sawmill => sawmill.WorkerCount,
                sawmill => StrategySawmill.MaxWorkers,
                sawmill => sawmill.CanStartWorkCycle() || sawmill.CanAcceptInputLogs(1),
                sawmill => sawmill.FootprintBounds.center);
        }

        private void AddIronDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Iron);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Miner))
            {
                return;
            }

            int shortage = Mathf.Max(0, priority * 2 - CountTotalIron());
            AddCappedCampDemands<StrategyMine>(
                StrategyProfessionType.Miner,
                StrategyAutoWorkforceCategory.Iron,
                shortage,
                "iron_low_stock",
                mine => mine.WorkerCount,
                mine => StrategyMine.MaxWorkers,
                mine => mine.HasStorageSpace,
                mine => mine.FootprintBounds.center);
        }

        private void AddCoalDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Coal);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.CoalMiner))
            {
                return;
            }

            int shortage = Mathf.Max(0, priority * 2 - CountTotalCoal());
            AddCappedCampDemands<StrategyCoalPit>(
                StrategyProfessionType.CoalMiner,
                StrategyAutoWorkforceCategory.Coal,
                shortage,
                "coal_low_stock",
                pit => pit.WorkerCount,
                pit => StrategyCoalPit.MaxWorkers,
                pit => pit.HasStorageSpace,
                pit => pit.FootprintBounds.center);
        }
    }
}
