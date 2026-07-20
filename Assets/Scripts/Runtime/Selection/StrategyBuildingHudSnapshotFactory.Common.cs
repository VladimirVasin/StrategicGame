using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingHudSnapshotFactory
    {
        private static void FillProcessorHeader(StrategyBuildingHudSnapshot snapshot, int workers, int maxWorkers, StrategyProfessionType profession, bool hasInput, int outputStored, StrategyBuildTool tool)
        {
            bool outputFull = outputStored >= StrategyProductionStorage.ProcessingOutputCapacity;
            string state = LocalizedValue(
                workers <= 0 ? "Idle" : outputFull ? "Output full" : hasInput ? "Working" : "Needs input");
            StrategyBuildingHudTone tone = workers > 0 && hasInput && !outputFull ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning;
            AddWorkerChip(snapshot, workers, maxWorkers, profession);
            snapshot.AddChip("state", L("label.state"), state, GetBuildingIcon(tool), tone);
            snapshot.AddChip("output", L("label.output"), outputStored + "/" + StrategyProductionStorage.ProcessingOutputCapacity, GetBuildingIcon(tool), outputFull ? StrategyBuildingHudTone.Warning : StrategyBuildingHudTone.Info);
        }

        private static void SetProcessorStatus(StrategyBuildingHudSnapshot snapshot, int workers, bool hasInput, int outputStored)
        {
            bool outputFull = outputStored >= StrategyProductionStorage.ProcessingOutputCapacity;
            if (workers <= 0)
            {
                snapshot.SetStatus(L("status.no_worker.title"), L("status.no_worker.body"), StrategyBuildingHudTone.Warning);
            }
            else if (outputFull)
            {
                snapshot.SetStatus(L("status.output_full.title"), L("status.output_full.body"), StrategyBuildingHudTone.Warning);
            }
            else if (!hasInput)
            {
                snapshot.SetStatus(L("status.waiting_inputs.title"), L("status.waiting_inputs.body"), StrategyBuildingHudTone.Warning);
            }
            else
            {
                snapshot.SetStatus(L("status.production_active.title"), L("status.production_active.body"), StrategyBuildingHudTone.Positive);
            }
        }

        private static void AddProcessorInputRow(StrategyBuildingHudSection section, StrategyResourceStore store, StrategyResourceType type, int incoming)
        {
            string detail = incoming > 0
                ? L("format.incoming", incoming)
                : L("detail.input_material");
            AddResourceRow(section, store, type, StrategyProductionStorage.ProcessingInputCapacity, detail);
        }

        private static void AddProcessorOutputRow(StrategyBuildingHudSection section, StrategyResourceStore store, StrategyResourceType type, int pending)
        {
            string detail = pending > 0
                ? L("format.pending", pending)
                : L("detail.finished_output");
            AddResourceRow(section, store, type, StrategyProductionStorage.ProcessingOutputCapacity, detail);
        }

        private static void AddWorkerChip(StrategyBuildingHudSnapshot snapshot, int workers, int maxWorkers, StrategyProfessionType profession)
        {
            snapshot.AddChip("workers", L("label.workers"), workers + "/" + maxWorkers, StrategyProfessionIconFactory.GetIcon(profession), workers > 0 ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning);
        }

        private static void AddCapacityChip(StrategyBuildingHudSnapshot snapshot, StrategyResourceStore store)
        {
            int stored = store != null ? store.TotalStored : 0;
            int capacity = store != null && store.Capacity > 0 ? store.Capacity : StrategyProductionStorage.LocalCapacity;
            snapshot.AddChip("capacity", L("label.stock"), stored + "/" + capacity, null, stored >= capacity ? StrategyBuildingHudTone.Warning : StrategyBuildingHudTone.Info);
        }

        private static void AddResourceRow(StrategyBuildingHudSection section, StrategyResourceStore store, StrategyResourceType resource, int capacity = 0, string detail = "")
        {
            if (section == null || store == null)
            {
                return;
            }

            int stored = store.GetStored(resource);
            int reserved = store.GetReserved(resource);
            int shownCapacity = capacity > 0 ? capacity : store.Capacity;
            string value = shownCapacity > 0 ? stored + "/" + shownCapacity : stored.ToString();
            string reservation = reserved > 0 ? L("format.reserved", reserved) : string.Empty;
            string rowDetail = string.IsNullOrEmpty(detail)
                ? reservation
                : string.IsNullOrEmpty(reservation)
                    ? detail
                    : L("format.join_detail", detail, reservation);
            section.AddRow(resource.ToString().ToLowerInvariant(), GetResourceTitle(resource), value, StrategyResourceIconFactory.GetSprite(resource), stored > 0 ? StrategyBuildingHudTone.Neutral : StrategyBuildingHudTone.Info, rowDetail, shownCapacity > 0 ? Mathf.Clamp01(stored / (float)shownCapacity) : -1f);
        }

        private static void AddMetricRow(StrategyBuildingHudSection section, string key, string label, int value, StrategyResourceType icon)
        {
            section?.AddRow(key, label, value.ToString(), StrategyResourceIconFactory.GetSprite(icon), value > 0 ? StrategyBuildingHudTone.Neutral : StrategyBuildingHudTone.Info);
        }

        private static string GetWorkState(int workers, int sources, bool hasSpace) =>
            LocalizedValue(workers <= 0 ? "Idle" : !hasSpace ? "Stock full" : sources <= 0 ? "No sources" : "Working");

        private static StrategyBuildingHudTone GetWorkTone(int workers, int sources, bool hasSpace) =>
            workers > 0 && sources > 0 && hasSpace ? StrategyBuildingHudTone.Positive : StrategyBuildingHudTone.Warning;

        private static void SetWorkStatus(StrategyBuildingHudSnapshot snapshot, string state, StrategyBuildingHudTone tone, int workers, int sources, bool hasSpace)
        {
            snapshot.SetStatus(state, GetWorkStatusBody(workers, sources, hasSpace), tone);
        }

        private static string GetWorkStatusBody(int workers, int sources, bool hasSpace) =>
            workers <= 0
                ? L("status.no_worker.body")
                : !hasSpace
                    ? L("status.clear_local_stock")
                    : sources <= 0
                        ? L("status.no_usable_sources")
                        : L("status.sources_and_space_ready");
    }
}
