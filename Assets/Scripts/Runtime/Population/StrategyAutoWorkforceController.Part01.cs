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
            StrategyConstructionSite bestSite = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                activeSites++;
                if (site.CanBuildWithDeliveredResources)
                {
                    readySites++;
                }

                float siteScore = (site.CanBuildWithDeliveredResources ? 60f : 30f)
                    + site.Cost.Total
                    + site.Progress * 20f
                    - site.BuilderCount * 8f;
                if (siteScore > bestScore)
                {
                    bestScore = siteScore;
                    focus = site.FootprintBounds.center;
                    bestSite = site;
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
            if (needed <= 0)
            {
                return;
            }

            AddDemand(
                StrategyProfessionType.Builder,
                StrategyAutoWorkforceCategory.Construction,
                bestSite != null ? bestSite : this,
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

            int desired = priority;
            SetDesiredProfessionTarget(StrategyProfessionType.StorageWorker, desired);
            int current = CountAssignedProfession(StrategyProfessionType.StorageWorker);
            int needed = Mathf.Max(0, desired - current);
            Component target = null;
            Vector3 demandWorld = focus;
            if (!TryFindStorageTarget(focus, out StrategyStorageYard yard))
            {
                if (!TryFindConstructionAnchor(out target, out demandWorld)
                    && !TryFindGranaryAnchor(out target, out demandWorld))
                {
                    return;
                }
            }
            else
            {
                target = yard;
            }

            if (needed <= 0 || target == null)
            {
                return;
            }

            AddDemand(
                StrategyProfessionType.StorageWorker,
                StrategyAutoWorkforceCategory.Logistics,
                target,
                demandWorld,
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
            float storedRations = CountCachedGranaryHouseholdRations();
            float reserveDays = dailyNeed <= 0.01f ? FoodReserveTargetDays : storedRations / dailyNeed;
            bool hasHunterCamp = cachedHunterCamps.Length > 0;
            bool hasFisherHut = cachedFisherHuts.Length > 0;
            bool hasForagerCamp = cachedForagerCamps.Length > 0;
            if (!hasHunterCamp && !hasFisherHut && !hasForagerCamp)
            {
                return;
            }

            bool foodReserveLow = reserveDays < FoodReserveTargetDays;
            bool foodEmergency = reserveDays <= FoodEmergencyDays || HasHouseholdFoodEmergency();
            int desiredFoodWorkers = priority;
            int sourceKinds = (hasHunterCamp ? 1 : 0) + (hasFisherHut ? 1 : 0) + (hasForagerCamp ? 1 : 0);
            int hunterWorkers = hasHunterCamp ? Mathf.CeilToInt(desiredFoodWorkers / (float)sourceKinds) : 0;
            int remainingFoodWorkers = desiredFoodWorkers - hunterWorkers;
            int remainingKinds = sourceKinds - (hasHunterCamp ? 1 : 0);
            int fisherWorkers = hasFisherHut ? Mathf.CeilToInt(remainingFoodWorkers / (float)Mathf.Max(1, remainingKinds)) : 0;
            int foragerWorkers = hasForagerCamp ? Mathf.Max(0, desiredFoodWorkers - hunterWorkers - fisherWorkers) : 0;
            float urgency = Mathf.Max(0f, FoodReserveTargetDays - reserveDays) * 25f;
            if (foodReserveLow)
            {
                urgency += ResourceShortagePriorityBonus;
            }

            if (foodEmergency)
            {
                urgency += EmptyResourceUrgencyBonus;
            }

            if (foodReserveLow && hasForagerCamp)
            {
                int foragerCapacityFloor = cachedForagerCamps.Length * StrategyForagerCamp.MaxWorkers;
                int protectedForagers = Mathf.Min(priority, foragerCapacityFloor);
                if (protectedForagers > foragerWorkers)
                {
                    foragerWorkers = protectedForagers;
                    int remainingProtectedFoodWorkers = Mathf.Max(0, desiredFoodWorkers - foragerWorkers);
                    int otherSourceKinds = (hasHunterCamp ? 1 : 0) + (hasFisherHut ? 1 : 0);
                    hunterWorkers = hasHunterCamp && otherSourceKinds > 0
                        ? Mathf.CeilToInt(remainingProtectedFoodWorkers / (float)otherSourceKinds)
                        : 0;
                    fisherWorkers = hasFisherHut
                        ? Mathf.Max(0, remainingProtectedFoodWorkers - hunterWorkers)
                        : 0;
                }
            }

            AddHunterDemands(hunterWorkers, urgency);
            AddFisherDemands(fisherWorkers, urgency);
            AddForagerDemands(foragerWorkers, urgency);
        }

        private void AddMaterialDemands()
        {
            AddLumberDemands();
            AddStoneDemands();
            AddPlankDemands();
            AddIronDemands();
            AddCoalDemands();
            AddClayDemands();
            AddPotteryDemands();
            AddToolsDemands();
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

        private void AddClayDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Clay);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.ClayDigger))
            {
                return;
            }

            int totalClay = CountTotalClay();
            int shortage = Mathf.Max(0, priority * 2 - totalClay);
            AddCappedCampDemands<StrategyClayPit>(
                StrategyProfessionType.ClayDigger,
                StrategyAutoWorkforceCategory.Clay,
                priority,
                "clay_low_stock",
                pit => pit.WorkerCount,
                pit => StrategyClayPit.MaxWorkers,
                pit => pit.HasStorageSpace,
                pit => pit.FootprintBounds.center,
                GetResourceShortageUrgency(shortage, totalClay));
        }

        private void AddPotteryDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Pottery);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Potter))
            {
                return;
            }

            int totalPottery = CountTotalPottery();
            int householdDemand = CountCachedRawHouseholdPotteryDemand();
            int shortage = Mathf.Max(0, priority * 2 + householdDemand - totalPottery);
            AddCappedCampDemands<StrategyKiln>(
                StrategyProfessionType.Potter,
                StrategyAutoWorkforceCategory.Pottery,
                priority,
                "pottery_low_stock",
                kiln => kiln.WorkerCount,
                kiln => StrategyKiln.MaxWorkers,
                kiln => kiln.CanStartWorkCycle() || kiln.CanAcceptInputClay(1) || kiln.CanAcceptInputCoal(1),
                kiln => kiln.FootprintBounds.center,
                GetResourceShortageUrgency(shortage, totalPottery));
        }

        private void AddToolsDemands()
        {
            int priority = settings.GetPriority(StrategyAutoWorkforceCategory.Tools);
            if (priority <= 0 || IsProfessionManualLocked(StrategyProfessionType.Blacksmith))
            {
                return;
            }

            int totalTools = CountTotalTools();
            int shortage = Mathf.Max(0, priority * 2 - totalTools);
            AddCappedCampDemands<StrategyForge>(
                StrategyProfessionType.Blacksmith,
                StrategyAutoWorkforceCategory.Tools,
                priority,
                "tools_low_stock",
                forge => forge.WorkerCount,
                forge => StrategyForge.MaxWorkers,
                forge => forge.CanStartWorkCycle()
                    || forge.CanAcceptInputIron(1)
                    || forge.CanAcceptInputCoal(1)
                    || forge.CanAcceptInputLogs(1),
                forge => forge.FootprintBounds.center,
                GetResourceShortageUrgency(shortage, totalTools));
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
