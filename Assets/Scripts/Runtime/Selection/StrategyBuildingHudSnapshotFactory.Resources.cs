using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingHudSnapshotFactory
    {
        private static bool TryFillResourceBuilding(
            StrategyPlacedBuilding building,
            StrategyBuildingHudSnapshot snapshot)
        {
            if (TryGet(building, out StrategyLumberjackCamp lumberjack))
            {
                FillLumberjack(lumberjack, snapshot);
            }
            else if (TryGet(building, out StrategyStonecutterCamp stonecutter))
            {
                FillExtractor(
                    snapshot,
                    stonecutter.ResourceStore,
                    StrategyResourceType.Stone,
                    stonecutter.WorkerCount,
                    StrategyStonecutterCamp.MaxWorkers,
                    StrategyProfessionType.Stonecutter,
                    stonecutter.HudAvailableDepositCount,
                    L("label.deposits"));
            }
            else if (TryGet(building, out StrategyMine mine))
            {
                FillExtractor(
                    snapshot,
                    mine.ResourceStore,
                    StrategyResourceType.Iron,
                    mine.WorkerCount,
                    StrategyMine.MaxWorkers,
                    StrategyProfessionType.Miner,
                    mine.HudAvailableDepositCount,
                    L("label.deposits"));
            }
            else if (TryGet(building, out StrategyCoalPit coalPit))
            {
                FillExtractor(
                    snapshot,
                    coalPit.ResourceStore,
                    StrategyResourceType.Coal,
                    coalPit.WorkerCount,
                    StrategyCoalPit.MaxWorkers,
                    StrategyProfessionType.CoalMiner,
                    coalPit.HudAvailableDepositCount,
                    L("label.deposits"));
            }
            else if (TryGet(building, out StrategyClayPit clayPit))
            {
                FillExtractor(
                    snapshot,
                    clayPit.ResourceStore,
                    StrategyResourceType.Clay,
                    clayPit.WorkerCount,
                    StrategyClayPit.MaxWorkers,
                    StrategyProfessionType.ClayDigger,
                    clayPit.HudAvailableDepositCount,
                    L("label.deposits"));
            }
            else if (TryGet(building, out StrategyHunterCamp hunter))
            {
                FillHunter(hunter, snapshot);
            }
            else if (TryGet(building, out StrategyFisherHut fisher))
            {
                FillFisher(fisher, snapshot);
            }
            else if (TryGet(building, out StrategyForagerCamp forager))
            {
                FillForager(forager, snapshot);
            }
            else if (TryGet(building, out StrategyChickenCoop coop))
            {
                FillChickenCoop(coop, snapshot);
            }
            else if (TryGet(building, out StrategySawmill sawmill))
            {
                FillSawmill(sawmill, snapshot);
            }
            else if (TryGet(building, out StrategyKiln kiln))
            {
                FillKiln(kiln, snapshot);
            }
            else if (TryGet(building, out StrategyForge forge))
            {
                FillForge(forge, snapshot);
            }
            else if (TryGet(building, out StrategyStorageYard yard))
            {
                FillStorageYard(yard, snapshot);
            }
            else if (TryGet(building, out StrategyGranary granary))
            {
                FillGranary(granary, snapshot);
            }
            else if (TryGet(building, out StrategyStarterCaravanCart cart))
            {
                FillStarterCart(cart, snapshot);
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool TryGet<T>(StrategyPlacedBuilding building, out T component)
            where T : Component
        {
            component = building != null ? building.GetComponent<T>() : null;
            return component != null;
        }

        private static void FillLumberjack(
            StrategyLumberjackCamp camp,
            StrategyBuildingHudSnapshot snapshot)
        {
            int sources = camp.HudMatureTreeCount + camp.HudProcessableWoodCount;
            string state = GetWorkState(camp.WorkerCount, sources, camp.HasStorageSpace);
            StrategyBuildingHudTone tone = GetWorkTone(camp.WorkerCount, sources, camp.HasStorageSpace);
            AddWorkerChip(snapshot, camp.WorkerCount, StrategyLumberjackCamp.MaxWorkers, StrategyProfessionType.Lumberjack);
            snapshot.AddChip("state", L("label.state"), state, GetBuildingIcon(StrategyBuildTool.LumberjackCamp), tone);
            AddCapacityChip(snapshot, camp.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", L("section.local_stock"));
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Logs);
            StrategyBuildingHudSection area = snapshot.AddSection("sources", L("section.work_area"));
            AddMetricRow(area, "mature", L("label.mature_trees"), camp.HudMatureTreeCount, StrategyResourceType.Logs);
            AddMetricRow(area, "trunks", L("label.trunks"), camp.HudProcessableWoodCount, StrategyResourceType.Logs);
            AddMetricRow(area, "saplings", L("label.saplings"), camp.HudGrowingTreeCount, StrategyResourceType.Logs);
            SetWorkStatus(snapshot, state, tone, camp.WorkerCount, sources, camp.HasStorageSpace);
        }

        private static void FillExtractor(
            StrategyBuildingHudSnapshot snapshot,
            StrategyResourceStore store,
            StrategyResourceType resource,
            int workers,
            int maxWorkers,
            StrategyProfessionType profession,
            int sources,
            string sourceLabel)
        {
            bool hasSpace = store == null || store.Capacity <= 0 || store.TotalStored < store.Capacity;
            string state = GetWorkState(workers, sources, hasSpace);
            StrategyBuildingHudTone tone = GetWorkTone(workers, sources, hasSpace);
            AddWorkerChip(snapshot, workers, maxWorkers, profession);
            snapshot.AddChip("state", L("label.state"), state, StrategyResourceIconFactory.GetSprite(resource), tone);
            AddCapacityChip(snapshot, store);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", L("section.local_stock"));
            AddResourceRow(stock, store, resource);
            StrategyBuildingHudSection source = snapshot.AddSection("sources", L("section.work_area"));
            AddMetricRow(source, "sources", sourceLabel, sources, resource);
            SetWorkStatus(snapshot, state, tone, workers, sources, hasSpace);
        }

        private static void FillHunter(
            StrategyHunterCamp camp,
            StrategyBuildingHudSnapshot snapshot)
        {
            int sources = camp.HudHuntableRabbitCount + camp.HudHuntableDeerCount;
            string state = GetWorkState(camp.WorkerCount, sources, camp.HasStorageSpace);
            StrategyBuildingHudTone tone = GetWorkTone(camp.WorkerCount, sources, camp.HasStorageSpace);
            AddWorkerChip(snapshot, camp.WorkerCount, StrategyHunterCamp.MaxWorkers, StrategyProfessionType.Hunter);
            snapshot.AddChip("state", L("label.state"), state, GetBuildingIcon(StrategyBuildTool.HunterCamp), tone);
            AddCapacityChip(snapshot, camp.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", L("section.local_stock"));
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Game);
            StrategyBuildingHudSection targets = snapshot.AddSection("targets", L("section.hunting_grounds"));
            AddMetricRow(targets, "rabbits", L("label.rabbits"), camp.HudHuntableRabbitCount, StrategyResourceType.Game);
            targets.AddRow(
                "deer",
                L("label.deer"),
                camp.CanHuntDeer ? camp.HudHuntableDeerCount.ToString() : LocalizedValue("Locked"),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game),
                camp.CanHuntDeer ? StrategyBuildingHudTone.Neutral : StrategyBuildingHudTone.Info,
                camp.CanHuntDeer ? string.Empty : L("hunter.requires_deer_kit"));
            SetWorkStatus(snapshot, state, tone, camp.WorkerCount, sources, camp.HasStorageSpace);
        }

        private static void FillFisher(
            StrategyFisherHut hut,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool frozen = StrategySeasonalSurfaceController.IsWaterFrozenForGameplay;
            int sources = frozen ? 0 : hut.HudCatchableFishCount;
            string state = frozen
                ? LocalizedValue("Frozen")
                : GetWorkState(hut.WorkerCount, sources, hut.HasStorageSpace);
            StrategyBuildingHudTone tone = frozen
                ? StrategyBuildingHudTone.Critical
                : GetWorkTone(hut.WorkerCount, sources, hut.HasStorageSpace);
            AddWorkerChip(snapshot, hut.WorkerCount, StrategyFisherHut.MaxWorkers, StrategyProfessionType.Fisher);
            snapshot.AddChip("state", L("label.state"), state, GetBuildingIcon(StrategyBuildTool.FisherHut), tone);
            AddCapacityChip(snapshot, hut.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", L("section.local_stock"));
            AddResourceRow(stock, hut.ResourceStore, StrategyResourceType.Fish);
            StrategyBuildingHudSection water = snapshot.AddSection("water", L("section.fishing_water"));
            AddMetricRow(water, "fish", L("label.fish_nearby"), hut.HudCatchableFishCount, StrategyResourceType.Fish);
            water.AddRow(
                "water_state",
                L("label.water"),
                LocalizedValue(frozen ? "Frozen" : "Open"),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Fish),
                frozen ? StrategyBuildingHudTone.Critical : StrategyBuildingHudTone.Positive);
            snapshot.SetStatus(
                state,
                frozen
                    ? L("fisher.water_frozen")
                    : GetWorkStatusBody(hut.WorkerCount, sources, hut.HasStorageSpace),
                tone);
        }

        private static void FillForager(
            StrategyForagerCamp camp,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool hasSpace = camp.HasStorageSpace;
            string state = LocalizedValue(
                camp.WorkerCount <= 0 ? "Idle" : hasSpace ? "Gathering" : "Stock full");
            StrategyBuildingHudTone tone = camp.WorkerCount <= 0 || !hasSpace
                ? StrategyBuildingHudTone.Warning
                : StrategyBuildingHudTone.Positive;
            AddWorkerChip(snapshot, camp.WorkerCount, StrategyForagerCamp.MaxWorkers, StrategyProfessionType.Forager);
            snapshot.AddChip("state", L("label.state"), state, GetBuildingIcon(StrategyBuildTool.ForagerCamp), tone);
            AddCapacityChip(snapshot, camp.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", L("section.forage_stock"));
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Berries);
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Roots);
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Mushrooms);
            stock.AddRow(
                "rations",
                L("label.ration_value"),
                StrategySelectionLocalization.Rations(camp.HudStoredRations),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Dish));
            snapshot.SetStatus(
                state,
                camp.WorkerCount <= 0
                    ? L("forager.assign_workers")
                    : hasSpace
                        ? L("forager.gathering_body")
                        : L("status.clear_local_stock"),
                tone);
        }

        private static void FillChickenCoop(
            StrategyChickenCoop coop,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool hasSpace = coop.HasStorageSpace;
            snapshot.AddChip(
                "labor",
                L("label.labor"),
                LocalizedValue("Autonomous"),
                GetBuildingIcon(StrategyBuildTool.ChickenCoop),
                StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "state",
                L("label.state"),
                LocalizedValue(hasSpace ? "Producing" : "Stock full"),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Eggs),
                hasSpace ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            AddCapacityChip(snapshot, coop.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", L("section.local_stock"));
            AddResourceRow(stock, coop.ResourceStore, StrategyResourceType.Eggs);
            StrategyBuildingHudSection production = snapshot.AddSection("production", L("section.production"));
            production.AddRow(
                "next_egg",
                L("label.next_egg"),
                L("format.seconds_short", Mathf.CeilToInt(coop.NextProductionSeconds)),
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Eggs),
                hasSpace ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning,
                hasSpace ? L("coop.automatic_cycle") : L("coop.waiting_storage"),
                coop.ProductionProgress);
            snapshot.SetStatus(
                hasSpace ? L("coop.automatic_title") : L("coop.stock_full_title"),
                hasSpace
                    ? L("coop.automatic_body")
                    : L("coop.collect_eggs_body"),
                hasSpace ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
        }

        private static void FillSawmill(
            StrategySawmill mill,
            StrategyBuildingHudSnapshot snapshot)
        {
            FillProcessorHeader(
                snapshot,
                mill.WorkerCount,
                StrategySawmill.MaxWorkers,
                StrategyProfessionType.Sawyer,
                mill.HasInputLogs,
                mill.OutputStorageUsed,
                StrategyBuildTool.Sawmill);
            StrategyBuildingHudSection input = snapshot.AddSection("input", L("section.input"));
            AddProcessorInputRow(input, mill.ResourceStore, StrategyResourceType.Logs, mill.HudIncomingLogs);
            StrategyBuildingHudSection output = snapshot.AddSection("output", L("section.output"));
            AddProcessorOutputRow(output, mill.ResourceStore, StrategyResourceType.Planks, mill.PendingPlanksForDemolition);
            SetProcessorStatus(snapshot, mill.WorkerCount, mill.HasInputLogs, mill.OutputStorageUsed);
        }

        private static void FillKiln(StrategyKiln kiln, StrategyBuildingHudSnapshot snapshot)
        {
            FillProcessorHeader(snapshot, kiln.WorkerCount, StrategyKiln.MaxWorkers, StrategyProfessionType.Potter, kiln.HasInputMaterials, kiln.OutputStorageUsed, StrategyBuildTool.Kiln);
            StrategyBuildingHudSection input = snapshot.AddSection("input", L("section.input"));
            AddProcessorInputRow(input, kiln.ResourceStore, StrategyResourceType.Clay, kiln.HudIncomingClay);
            AddProcessorInputRow(input, kiln.ResourceStore, StrategyResourceType.Coal, kiln.HudIncomingCoal);
            StrategyBuildingHudSection output = snapshot.AddSection("output", L("section.output"));
            AddProcessorOutputRow(output, kiln.ResourceStore, StrategyResourceType.Pottery, kiln.PendingPotteryForDemolition);
            SetProcessorStatus(snapshot, kiln.WorkerCount, kiln.HasInputMaterials, kiln.OutputStorageUsed);
        }

        private static void FillForge(StrategyForge forge, StrategyBuildingHudSnapshot snapshot)
        {
            FillProcessorHeader(snapshot, forge.WorkerCount, StrategyForge.MaxWorkers, StrategyProfessionType.Blacksmith, forge.HasInputMaterials, forge.OutputStorageUsed, StrategyBuildTool.Forge);
            StrategyBuildingHudSection input = snapshot.AddSection("input", L("section.input"));
            AddProcessorInputRow(input, forge.ResourceStore, StrategyResourceType.Iron, forge.HudIncomingIron);
            AddProcessorInputRow(input, forge.ResourceStore, StrategyResourceType.Coal, forge.HudIncomingCoal);
            AddProcessorInputRow(input, forge.ResourceStore, StrategyResourceType.Logs, forge.HudIncomingLogs);
            StrategyBuildingHudSection output = snapshot.AddSection("output", L("section.output"));
            AddProcessorOutputRow(output, forge.ResourceStore, StrategyResourceType.Tools, forge.PendingToolsForDemolition);
            SetProcessorStatus(snapshot, forge.WorkerCount, forge.HasInputMaterials, forge.OutputStorageUsed);
        }

        private static void FillStorageYard(StrategyStorageYard yard, StrategyBuildingHudSnapshot snapshot)
        {
            int haulers = StrategyPopulationController.CountActiveSettlementHaulers();
            int builders = StrategyPopulationController.CountActiveSettlementBuilders();
            int sources = yard.GetAvailableSourceCount();
            snapshot.AddChip("haulers", L("label.haulers"), haulers.ToString(), StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.StorageWorker), haulers > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            snapshot.AddChip("builders", L("label.builders"), builders.ToString(), StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder), StrategyBuildingHudTone.Neutral);
            snapshot.AddChip("sources", L("label.sources"), sources.ToString(), GetBuildingIcon(StrategyBuildTool.StorageYard), sources > 0 ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Neutral);

            StrategyBuildingHudSection construction = snapshot.AddSection("construction", L("section.construction_stock"));
            AddResourceRow(construction, yard.ResourceStore, StrategyResourceType.Logs);
            AddResourceRow(construction, yard.ResourceStore, StrategyResourceType.Stone);
            AddResourceRow(construction, yard.ResourceStore, StrategyResourceType.Planks);
            StrategyBuildingHudSection materials = snapshot.AddSection("materials", L("section.materials"));
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Iron);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Coal);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Clay);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Pottery);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Tools);
            snapshot.SetStatus(
                haulers <= 0
                    ? L("storage.no_haulers_title")
                    : sources > 0
                        ? L("storage.ready_title")
                        : L("storage.idle_title"),
                haulers <= 0
                    ? L("storage.assign_haulers")
                    : sources > 0
                        ? L("storage.sources_waiting", sources)
                        : L("storage.no_sources_waiting"),
                haulers <= 0 ? StrategyBuildingHudTone.Warning : sources > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Neutral);
        }

        private static void FillGranary(StrategyGranary granary, StrategyBuildingHudSnapshot snapshot)
        {
            granary.GetHudSourceCounts(out int game, out int fish, out int eggs, out int forage);
            int sources = game + fish + eggs + forage;
            int haulers = StrategyPopulationController.CountActiveSettlementHaulers();
            snapshot.AddChip("haulers", L("label.haulers"), haulers.ToString(), StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.StorageWorker), haulers > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            snapshot.AddChip("sources", L("label.sources"), sources.ToString(), GetBuildingIcon(StrategyBuildTool.Granary), sources > 0 ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Neutral);
            snapshot.AddChip("rations", L("label.rations"), StrategySelectionLocalization.Rations(granary.TotalRationValue), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Dish), granary.TotalRationValue > 0f ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            StrategyBuildingHudSection stock = snapshot.AddSection("food", L("section.food_stock"));
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Game);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Fish);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Eggs);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Berries);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Roots);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Mushrooms);
            snapshot.SetStatus(
                haulers <= 0 ? L("storage.no_haulers_title") : L("granary.ready_title"),
                haulers <= 0
                    ? L("status.no_worker.body")
                    : sources > 0
                        ? L("granary.food_waiting")
                        : L("granary.food_ready"),
                haulers <= 0 ? StrategyBuildingHudTone.Warning : StrategyBuildingHudTone.Positive);
        }

        private static void FillStarterCart(StrategyStarterCaravanCart cart, StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.AddChip("state", L("label.state"), LocalizedValue("Temporary"), GetBuildingIcon(StrategyBuildTool.StarterCaravanCart), StrategyBuildingHudTone.Info);
            snapshot.AddChip("construction", L("label.build_stock"), (cart.LogsStored + cart.StoneStored + cart.PlanksStored).ToString(), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs));
            snapshot.AddChip("rations", L("label.rations"), StrategySelectionLocalization.Rations(cart.AvailableHouseholdRationValue), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Dish), cart.AvailableHouseholdRationValue > 0f ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            StrategyBuildingHudSection construction = snapshot.AddSection("construction", L("section.construction_stock"));
            AddResourceRow(construction, cart.ResourceStore, StrategyResourceType.Logs);
            AddResourceRow(construction, cart.ResourceStore, StrategyResourceType.Stone);
            AddResourceRow(construction, cart.ResourceStore, StrategyResourceType.Planks);
            StrategyBuildingHudSection food = snapshot.AddSection("food", L("section.food_stock"));
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Game);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Fish);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Eggs);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Berries);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Roots);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Mushrooms);
            snapshot.SetStatus(L("cart.supplies_title"), L("cart.supplies_body"), StrategyBuildingHudTone.Info);
        }

    }
}
