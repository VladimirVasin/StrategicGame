using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategySawmill : MonoBehaviour
    {
        public const int MaxWorkers = 2;
        private const int LogsPerWorkCycle = 1;
        private const int PlanksPerLog = 2;

        private readonly List<StrategyResidentAgent> workers = new();
        private readonly List<StrategyResidentAgent> activeSawyers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer logStockRenderer;
        private SpriteRenderer plankStockRenderer;
        private SpriteRenderer workRenderer;
        private object planksReservationOwner;
        private int reservedPlanks;
        private int logsStored;
        private int planksStored;
        private int pendingPlanks;
        private float workFrameTimer;
        private int workFrame;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int LogsStored => logsStored;
        public int PlanksStored => planksStored;
        public int AvailablePlanks => Mathf.Max(0, planksStored - reservedPlanks);
        public int StorageUsed => logsStored + planksStored;
        public int ReservedStorageUsed => logsStored + planksStored + pendingPlanks;
        public bool HasInputLogs => logsStored >= LogsPerWorkCycle;
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            EnsureStockRenderers();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Sawmill",
                "SawmillConfigured",
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
            resident.AssignSawmillWorkplace(this);
            StrategyDebugLogger.Info(
                "Sawmill",
                "SawmillWorkerAssigned",
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
            if (worker != null)
            {
                worker.ClearSawmillWorkplace(this);
            }
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

        public bool TryReserveInputLogs(
            object owner,
            out StrategyStorageYard yardSource,
            out StrategyLumberjackCamp campSource,
            out Vector2Int pickupCell)
        {
            yardSource = null;
            campSource = null;
            pickupCell = default;
            if (!CanAcceptInputLogs(3))
            {
                return false;
            }

            return TryReserveNearestStorageLogs(owner, out yardSource, out pickupCell)
                || TryReserveNearestCampLogs(owner, out campSource, out pickupCell);
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
            Bounds bounds = FootprintBounds;
            int index = Mathf.Max(0, activeSawyers.IndexOf(worker));
            float side = activeSawyers.Count > 1 && index == 1 ? 0.34f : -0.34f;
            return new Vector3(bounds.center.x + side, bounds.min.y + bounds.size.y * 0.44f, -0.08f);
        }

        public Vector3 GetSawFocusWorld()
        {
            Bounds bounds = FootprintBounds;
            return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.45f, -0.08f);
        }

        public bool TryConsumeLogForWork(out int planksExpected)
        {
            planksExpected = 0;
            if (!CanStartWorkCycle())
            {
                return false;
            }

            logsStored -= LogsPerWorkCycle;
            planksExpected = LogsPerWorkCycle * PlanksPerLog;
            pendingPlanks += planksExpected;
            UpdateStockVisual();
            return true;
        }

        public void AddLogs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            logsStored = StrategyProductionStorage.AddCapped(
                logsStored,
                ReservedStorageUsed,
                amount,
                out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
        }

        public void AddPlanks(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            pendingPlanks = Mathf.Max(0, pendingPlanks - amount);
            planksStored = StrategyProductionStorage.AddCapped(
                planksStored,
                ReservedStorageUsed,
                amount,
                out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Sawmill",
                "PlanksStoredAtSawmill",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", planksStored));
        }

        public bool CanAcceptInputLogs(int amount)
        {
            return StrategyProductionStorage.CanAccept(ReservedStorageUsed, amount);
        }

        public bool CanStartWorkCycle()
        {
            return logsStored >= LogsPerWorkCycle
                && ReservedStorageUsed - LogsPerWorkCycle + PlanksPerLog <= StrategyProductionStorage.LocalCapacity;
        }

        public void ReleasePendingPlanks(int amount)
        {
            pendingPlanks = Mathf.Max(0, pendingPlanks - Mathf.Max(0, amount));
        }

        public bool TryReserveStoredPlanks(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || planksStored <= 0 || planksReservationOwner != null && planksReservationOwner != owner)
            {
                return false;
            }

            if (planksReservationOwner == owner && reservedPlanks > 0)
            {
                amount = reservedPlanks;
                return true;
            }

            int available = AvailablePlanks;
            if (available <= 0)
            {
                return false;
            }

            reservedPlanks = Mathf.Min(4, available);
            planksReservationOwner = owner;
            amount = reservedPlanks;
            return true;
        }

        public bool TryTakeReservedPlanks(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || planksReservationOwner != owner || reservedPlanks <= 0 || planksStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedPlanks, planksStored);
            planksStored -= amount;
            reservedPlanks = 0;
            planksReservationOwner = null;
            UpdateStockVisual();
            return amount > 0;
        }

        public void ReleaseStoredPlanksReservation(object owner)
        {
            if (owner != null && planksReservationOwner == owner)
            {
                planksReservationOwner = null;
                reservedPlanks = 0;
            }
        }

        public void BeginSawing(StrategyResidentAgent worker)
        {
            if (worker != null && !activeSawyers.Contains(worker))
            {
                activeSawyers.Add(worker);
            }
        }

        public void EndSawing(StrategyResidentAgent worker)
        {
            activeSawyers.Remove(worker);
        }

        public string GetHudStatusText()
        {
            return "Sawyers: " + workers.Count + "/" + MaxWorkers
                + "\nStorage: " + StorageUsed + "/" + StrategyProductionStorage.LocalCapacity
                + (pendingPlanks > 0 ? " (" + pendingPlanks + " pending)" : string.Empty)
                + "\nLogs: " + logsStored
                + "\nPlanks: " + planksStored
                + (reservedPlanks > 0 ? " (" + reservedPlanks + " reserved)" : string.Empty);
        }

        private void Update()
        {
            UpdateWorkAnimation();
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearSawmillWorkplace(this);
                }
            }

            workers.Clear();
            activeSawyers.Clear();
            planksReservationOwner = null;
            reservedPlanks = 0;
            pendingPlanks = 0;
        }
    }
}
