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
                    "Deposits");
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
                    "Deposits");
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
                    "Deposits");
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
                    "Deposits");
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
            snapshot.AddChip("state", "State", state, GetBuildingIcon(StrategyBuildTool.LumberjackCamp), tone);
            AddCapacityChip(snapshot, camp.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", "Local Stock");
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Logs);
            StrategyBuildingHudSection area = snapshot.AddSection("sources", "Work Area");
            AddMetricRow(area, "mature", "Mature trees", camp.HudMatureTreeCount, StrategyResourceType.Logs);
            AddMetricRow(area, "trunks", "Trunks", camp.HudProcessableWoodCount, StrategyResourceType.Logs);
            AddMetricRow(area, "saplings", "Saplings", camp.HudGrowingTreeCount, StrategyResourceType.Logs);
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
            snapshot.AddChip("state", "State", state, StrategyResourceIconFactory.GetSprite(resource), tone);
            AddCapacityChip(snapshot, store);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", "Local Stock");
            AddResourceRow(stock, store, resource);
            StrategyBuildingHudSection source = snapshot.AddSection("sources", "Work Area");
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
            snapshot.AddChip("state", "State", state, GetBuildingIcon(StrategyBuildTool.HunterCamp), tone);
            AddCapacityChip(snapshot, camp.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", "Local Stock");
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Game);
            StrategyBuildingHudSection targets = snapshot.AddSection("targets", "Hunting Grounds");
            AddMetricRow(targets, "rabbits", "Rabbits", camp.HudHuntableRabbitCount, StrategyResourceType.Game);
            targets.AddRow(
                "deer",
                "Deer",
                camp.CanHuntDeer ? camp.HudHuntableDeerCount.ToString() : "Locked",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game),
                camp.CanHuntDeer ? StrategyBuildingHudTone.Neutral : StrategyBuildingHudTone.Info,
                camp.CanHuntDeer ? string.Empty : "Requires Deer Hunting Kit");
            SetWorkStatus(snapshot, state, tone, camp.WorkerCount, sources, camp.HasStorageSpace);
        }

        private static void FillFisher(
            StrategyFisherHut hut,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool frozen = StrategySeasonalSurfaceController.IsWaterFrozenForGameplay;
            int sources = frozen ? 0 : hut.HudCatchableFishCount;
            string state = frozen ? "Frozen" : GetWorkState(hut.WorkerCount, sources, hut.HasStorageSpace);
            StrategyBuildingHudTone tone = frozen
                ? StrategyBuildingHudTone.Critical
                : GetWorkTone(hut.WorkerCount, sources, hut.HasStorageSpace);
            AddWorkerChip(snapshot, hut.WorkerCount, StrategyFisherHut.MaxWorkers, StrategyProfessionType.Fisher);
            snapshot.AddChip("state", "State", state, GetBuildingIcon(StrategyBuildTool.FisherHut), tone);
            AddCapacityChip(snapshot, hut.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", "Local Stock");
            AddResourceRow(stock, hut.ResourceStore, StrategyResourceType.Fish);
            StrategyBuildingHudSection water = snapshot.AddSection("water", "Fishing Water");
            AddMetricRow(water, "fish", "Fish nearby", hut.HudCatchableFishCount, StrategyResourceType.Fish);
            water.AddRow(
                "water_state",
                "Water",
                frozen ? "Frozen" : "Open",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Fish),
                frozen ? StrategyBuildingHudTone.Critical : StrategyBuildingHudTone.Positive);
            snapshot.SetStatus(
                state,
                frozen
                    ? "Fishing is blocked until the water thaws."
                    : GetWorkStatusBody(hut.WorkerCount, sources, hut.HasStorageSpace),
                tone);
        }

        private static void FillForager(
            StrategyForagerCamp camp,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool hasSpace = camp.HasStorageSpace;
            string state = camp.WorkerCount <= 0 ? "Idle" : hasSpace ? "Gathering" : "Stock full";
            StrategyBuildingHudTone tone = camp.WorkerCount <= 0 || !hasSpace
                ? StrategyBuildingHudTone.Warning
                : StrategyBuildingHudTone.Positive;
            AddWorkerChip(snapshot, camp.WorkerCount, StrategyForagerCamp.MaxWorkers, StrategyProfessionType.Forager);
            snapshot.AddChip("state", "State", state, GetBuildingIcon(StrategyBuildTool.ForagerCamp), tone);
            AddCapacityChip(snapshot, camp.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", "Forage Stock");
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Berries);
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Roots);
            AddResourceRow(stock, camp.ResourceStore, StrategyResourceType.Mushrooms);
            stock.AddRow(
                "rations",
                "Ration value",
                camp.HudStoredRations.ToString("0.#") + "r",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Dish));
            snapshot.SetStatus(
                state,
                camp.WorkerCount <= 0
                    ? "Assign Foragers in the Professions HUD."
                    : hasSpace
                        ? "Nearby food is gathered into local stock for Haulers."
                        : "Haulers need to clear the local stock.",
                tone);
        }

        private static void FillChickenCoop(
            StrategyChickenCoop coop,
            StrategyBuildingHudSnapshot snapshot)
        {
            bool hasSpace = coop.HasStorageSpace;
            snapshot.AddChip(
                "labor",
                "Labor",
                "Autonomous",
                GetBuildingIcon(StrategyBuildTool.ChickenCoop),
                StrategyBuildingHudTone.Info);
            snapshot.AddChip(
                "state",
                "State",
                hasSpace ? "Producing" : "Stock full",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Eggs),
                hasSpace ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            AddCapacityChip(snapshot, coop.ResourceStore);

            StrategyBuildingHudSection stock = snapshot.AddSection("stock", "Local Stock");
            AddResourceRow(stock, coop.ResourceStore, StrategyResourceType.Eggs);
            StrategyBuildingHudSection production = snapshot.AddSection("production", "Production");
            production.AddRow(
                "next_egg",
                "Next egg",
                Mathf.CeilToInt(coop.NextProductionSeconds) + "s",
                StrategyResourceIconFactory.GetSprite(StrategyResourceType.Eggs),
                hasSpace ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning,
                hasSpace ? "Automatic cycle" : "Waiting for free storage",
                coop.ProductionProgress);
            snapshot.SetStatus(
                hasSpace ? "Automatic production" : "Local stock is full",
                hasSpace
                    ? "Eggs are produced without an assigned worker."
                    : "Haulers need to collect eggs before production continues.",
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
            StrategyBuildingHudSection input = snapshot.AddSection("input", "Input");
            AddProcessorInputRow(input, mill.ResourceStore, StrategyResourceType.Logs, mill.HudIncomingLogs);
            StrategyBuildingHudSection output = snapshot.AddSection("output", "Output");
            AddProcessorOutputRow(output, mill.ResourceStore, StrategyResourceType.Planks, mill.PendingPlanksForDemolition);
            SetProcessorStatus(snapshot, mill.WorkerCount, mill.HasInputLogs, mill.OutputStorageUsed);
        }

        private static void FillKiln(StrategyKiln kiln, StrategyBuildingHudSnapshot snapshot)
        {
            FillProcessorHeader(snapshot, kiln.WorkerCount, StrategyKiln.MaxWorkers, StrategyProfessionType.Potter, kiln.HasInputMaterials, kiln.OutputStorageUsed, StrategyBuildTool.Kiln);
            StrategyBuildingHudSection input = snapshot.AddSection("input", "Input");
            AddProcessorInputRow(input, kiln.ResourceStore, StrategyResourceType.Clay, kiln.HudIncomingClay);
            AddProcessorInputRow(input, kiln.ResourceStore, StrategyResourceType.Coal, kiln.HudIncomingCoal);
            StrategyBuildingHudSection output = snapshot.AddSection("output", "Output");
            AddProcessorOutputRow(output, kiln.ResourceStore, StrategyResourceType.Pottery, kiln.PendingPotteryForDemolition);
            SetProcessorStatus(snapshot, kiln.WorkerCount, kiln.HasInputMaterials, kiln.OutputStorageUsed);
        }

        private static void FillForge(StrategyForge forge, StrategyBuildingHudSnapshot snapshot)
        {
            FillProcessorHeader(snapshot, forge.WorkerCount, StrategyForge.MaxWorkers, StrategyProfessionType.Blacksmith, forge.HasInputMaterials, forge.OutputStorageUsed, StrategyBuildTool.Forge);
            StrategyBuildingHudSection input = snapshot.AddSection("input", "Input");
            AddProcessorInputRow(input, forge.ResourceStore, StrategyResourceType.Iron, forge.HudIncomingIron);
            AddProcessorInputRow(input, forge.ResourceStore, StrategyResourceType.Coal, forge.HudIncomingCoal);
            AddProcessorInputRow(input, forge.ResourceStore, StrategyResourceType.Logs, forge.HudIncomingLogs);
            StrategyBuildingHudSection output = snapshot.AddSection("output", "Output");
            AddProcessorOutputRow(output, forge.ResourceStore, StrategyResourceType.Tools, forge.PendingToolsForDemolition);
            SetProcessorStatus(snapshot, forge.WorkerCount, forge.HasInputMaterials, forge.OutputStorageUsed);
        }

        private static void FillStorageYard(StrategyStorageYard yard, StrategyBuildingHudSnapshot snapshot)
        {
            int haulers = StrategyPopulationController.CountActiveSettlementHaulers();
            int builders = StrategyPopulationController.CountActiveSettlementBuilders();
            int sources = yard.GetAvailableSourceCount();
            snapshot.AddChip("haulers", "Haulers", haulers.ToString(), StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.StorageWorker), haulers > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            snapshot.AddChip("builders", "Builders", builders.ToString(), StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder), StrategyBuildingHudTone.Neutral);
            snapshot.AddChip("sources", "Sources", sources.ToString(), GetBuildingIcon(StrategyBuildTool.StorageYard), sources > 0 ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Neutral);

            StrategyBuildingHudSection construction = snapshot.AddSection("construction", "Construction Stock");
            AddResourceRow(construction, yard.ResourceStore, StrategyResourceType.Logs);
            AddResourceRow(construction, yard.ResourceStore, StrategyResourceType.Stone);
            AddResourceRow(construction, yard.ResourceStore, StrategyResourceType.Planks);
            StrategyBuildingHudSection materials = snapshot.AddSection("materials", "Materials");
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Iron);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Coal);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Clay);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Pottery);
            AddResourceRow(materials, yard.ResourceStore, StrategyResourceType.Tools);
            snapshot.SetStatus(
                haulers <= 0 ? "No Haulers assigned" : sources > 0 ? "Ready for hauling" : "Logistics idle",
                haulers <= 0
                    ? "Assign Haulers in the Professions HUD to move resources."
                    : sources > 0
                        ? sources + " source" + (sources == 1 ? " has" : "s have") + " stock waiting."
                        : "No source stock is currently waiting for pickup.",
                haulers <= 0 ? StrategyBuildingHudTone.Warning : sources > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Neutral);
        }

        private static void FillGranary(StrategyGranary granary, StrategyBuildingHudSnapshot snapshot)
        {
            granary.GetHudSourceCounts(out int game, out int fish, out int eggs, out int forage);
            int sources = game + fish + eggs + forage;
            int haulers = StrategyPopulationController.CountActiveSettlementHaulers();
            snapshot.AddChip("haulers", "Haulers", haulers.ToString(), StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.StorageWorker), haulers > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            snapshot.AddChip("sources", "Sources", sources.ToString(), GetBuildingIcon(StrategyBuildTool.Granary), sources > 0 ? StrategyBuildingHudTone.Info : StrategyBuildingHudTone.Neutral);
            snapshot.AddChip("rations", "Rations", granary.TotalRationValue.ToString("0.#") + "r", StrategyResourceIconFactory.GetSprite(StrategyResourceType.Dish), granary.TotalRationValue > 0f ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            StrategyBuildingHudSection stock = snapshot.AddSection("food", "Food Stock");
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Game);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Fish);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Eggs);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Berries);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Roots);
            AddResourceRow(stock, granary.ResourceStore, StrategyResourceType.Mushrooms);
            snapshot.SetStatus(
                haulers <= 0 ? "No Haulers assigned" : "Food storage ready",
                haulers <= 0 ? "Assign Haulers in the Professions HUD." : sources > 0 ? "Food is waiting at production sources." : "Stored food is ready for households.",
                haulers <= 0 ? StrategyBuildingHudTone.Warning : StrategyBuildingHudTone.Positive);
        }

        private static void FillStarterCart(StrategyStarterCaravanCart cart, StrategyBuildingHudSnapshot snapshot)
        {
            snapshot.AddChip("state", "State", "Temporary", GetBuildingIcon(StrategyBuildTool.StarterCaravanCart), StrategyBuildingHudTone.Info);
            snapshot.AddChip("construction", "Build stock", (cart.LogsStored + cart.StoneStored + cart.PlanksStored).ToString(), StrategyResourceIconFactory.GetSprite(StrategyResourceType.Logs));
            snapshot.AddChip("rations", "Rations", cart.AvailableHouseholdRationValue.ToString("0.#") + "r", StrategyResourceIconFactory.GetSprite(StrategyResourceType.Dish), cart.AvailableHouseholdRationValue > 0f ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
            StrategyBuildingHudSection construction = snapshot.AddSection("construction", "Construction Stock");
            AddResourceRow(construction, cart.ResourceStore, StrategyResourceType.Logs);
            AddResourceRow(construction, cart.ResourceStore, StrategyResourceType.Stone);
            AddResourceRow(construction, cart.ResourceStore, StrategyResourceType.Planks);
            StrategyBuildingHudSection food = snapshot.AddSection("food", "Food Stock");
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Game);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Fish);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Eggs);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Berries);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Roots);
            AddResourceRow(food, cart.ResourceStore, StrategyResourceType.Mushrooms);
            snapshot.SetStatus("Starter supplies", "Construction materials and food remain available until transferred.", StrategyBuildingHudTone.Info);
        }

    }
}
