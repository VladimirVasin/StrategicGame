using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyForge : MonoBehaviour, IStrategyResourceStoreOwner
    {
        public const int MaxWorkers = 1;
        private const int MaxInputIron = 2;
        private const int MaxInputCoal = 2;
        private const int MaxInputLogs = 2;
        private const int IronPerWorkCycle = 1;
        private const int CoalPerWorkCycle = 1;
        private const int LogsPerWorkCycle = 1;
        private const int ToolsPerWorkCycle = 1;

        private readonly List<StrategyResidentAgent> workers = new();
        private readonly List<StrategyResidentAgent> activeBlacksmiths = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer ironStockRenderer;
        private SpriteRenderer coalStockRenderer;
        private SpriteRenderer logStockRenderer;
        private SpriteRenderer toolsStockRenderer;
        private SpriteRenderer workRenderer;
        private object inputIronReservationOwner;
        private object inputCoalReservationOwner;
        private object inputLogsReservationOwner;
        private object toolsReservationOwner;
        private int reservedInputIron;
        private int reservedInputCoal;
        private int reservedInputLogs;
        private int reservedTools;
        private readonly StrategyResourceStore resourceStore = new();
        private ref int ironStored => ref resourceStore.GetAmountRef(StrategyResourceType.Iron);
        private ref int coalStored => ref resourceStore.GetAmountRef(StrategyResourceType.Coal);
        private ref int logsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Logs);
        private ref int toolsStored => ref resourceStore.GetAmountRef(StrategyResourceType.Tools);
        private int pendingTools;
        private float workFrameTimer;
        private int workFrame;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int IronStored => ironStored;
        public int CoalStored => coalStored;
        public int LogsStored => logsStored;
        public int ToolsStored => toolsStored;
        public int AvailableTools => Mathf.Max(0, toolsStored - reservedTools);
        public int StorageUsed => ironStored + coalStored + logsStored + toolsStored;
        public int ReservedStorageUsed => StorageUsed + pendingTools + reservedInputIron + reservedInputCoal + reservedInputLogs;
        public bool HasInputMaterials => ironStored >= IronPerWorkCycle
            && coalStored >= CoalPerWorkCycle
            && logsStored >= LogsPerWorkCycle;
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            resourceStore.Bind(this, StrategyResourceStoreScope.Production, StrategyProductionStorage.LocalCapacity);
            map = mapController;
            population = populationController;
            EnsureStockRenderers();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Forge",
                "ForgeConfigured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("maxWorkers", MaxWorkers),
                StrategyDebugLogger.F("recipe", "Iron 1 + Coal 1 + Logs 1 = Tools 1"));
        }

        public bool CanAssignNextAvailableWorker()
        {
            if (workers.Count >= MaxWorkers || population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableWorker(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (workers.Count >= MaxWorkers || population == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment
                    && !workers.Contains(resident))
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            return AssignWorker(assigned);
        }

        public bool AssignWorker(StrategyResidentAgent resident)
        {
            if (resident == null
                || workers.Count >= MaxWorkers
                || workers.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignForgeWorkplace(this);
            StrategyDebugLogger.Info(
                "Forge",
                "ForgeWorkerAssigned",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("worker", resident.FullName),
                StrategyDebugLogger.F("workerCount", workers.Count));
            return true;
        }

        public void UnassignWorkerAt(int index)
        {
            if (index < 0 || index >= workers.Count)
            {
                return;
            }

            StrategyResidentAgent worker = workers[index];
            workers.RemoveAt(index);
            worker?.ClearForgeWorkplace(this);
        }

        public void UnassignWorker(StrategyResidentAgent worker)
        {
            int index = workers.IndexOf(worker);
            if (index >= 0)
            {
                UnassignWorkerAt(index);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool TryFindEntranceCell(out Vector2Int cell)
        {
            return TryFindDropoffCell(out cell);
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            cell = default;
            if (map == null || building == null)
            {
                return false;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y < building.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < building.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (isEdge && map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetInteriorWorkWorld(StrategyResidentAgent worker)
        {
            return StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(
                StrategyBuildTool.Forge,
                FootprintBounds,
                0,
                1);
        }

        public Vector3 GetForgeFocusWorld()
        {
            return StrategyBuildingVisualAnchorProfile.GetWorkFocusWorld(
                StrategyBuildTool.Forge,
                FootprintBounds);
        }
    }
}
