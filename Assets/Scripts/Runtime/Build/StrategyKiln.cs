using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyKiln : MonoBehaviour, IStrategyResourceStoreOwner
    {
        public const int MaxWorkers = 1;
        private const int MaxInputClay = 4;
        private const int MaxInputCoal = 2;
        private const int ClayPerWorkCycle = 2;
        private const int CoalPerWorkCycle = 1;
        private const int PotteryPerWorkCycle = 1;

        private readonly List<StrategyResidentAgent> workers = new();
        private readonly List<StrategyResidentAgent> activePotters = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer clayStockRenderer;
        private SpriteRenderer coalStockRenderer;
        private SpriteRenderer potteryStockRenderer;
        private SpriteRenderer workRenderer;
        private object inputClayReservationOwner;
        private object inputCoalReservationOwner;
        private object potteryReservationOwner;
        private int reservedInputClay;
        private int reservedInputCoal;
        private int reservedPottery;
        private readonly StrategyResourceStore resourceStore = new();
        private ref int clayStored => ref resourceStore.GetAmountRef(StrategyResourceType.Clay);
        private ref int coalStored => ref resourceStore.GetAmountRef(StrategyResourceType.Coal);
        private ref int potteryStored => ref resourceStore.GetAmountRef(StrategyResourceType.Pottery);
        private int pendingPottery;
        private float workFrameTimer;
        private int workFrame;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int ClayStored => clayStored;
        public int CoalStored => coalStored;
        public int PotteryStored => potteryStored;
        public int AvailablePottery => Mathf.Max(0, potteryStored - reservedPottery);
        public int InputStorageUsed => clayStored + coalStored;
        public int ReservedInputStorageUsed => InputStorageUsed + reservedInputClay + reservedInputCoal;
        public int OutputStorageUsed => potteryStored;
        public int ReservedOutputStorageUsed => OutputStorageUsed + pendingPottery;
        public int StorageUsed => InputStorageUsed + OutputStorageUsed;
        public int ReservedStorageUsed => ReservedInputStorageUsed + ReservedOutputStorageUsed;
        public int PendingPotteryForDemolition => Mathf.Max(0, pendingPottery);
        public bool HasInputMaterials => clayStored >= ClayPerWorkCycle && coalStored >= CoalPerWorkCycle;
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            resourceStore.Bind(this, StrategyResourceStoreScope.Production, StrategyProductionStorage.ProcessingTotalCapacity);
            map = mapController;
            population = populationController;
            EnsureStockRenderers();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Kiln",
                "KilnConfigured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("maxWorkers", MaxWorkers));
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
            resident.AssignKilnWorkplace(this);
            StrategyDebugLogger.Info(
                "Kiln",
                "KilnWorkerAssigned",
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
            worker?.ClearKilnWorkplace(this);
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
                StrategyBuildTool.Kiln,
                FootprintBounds,
                0,
                1);
        }

        public Vector3 GetKilnFocusWorld()
        {
            return StrategyBuildingVisualAnchorProfile.GetWorkFocusWorld(
                StrategyBuildTool.Kiln,
                FootprintBounds);
        }
    }
}
