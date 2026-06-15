using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyLumberjackCamp : MonoBehaviour
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 9;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyForestryController forestry;
        private StrategyPopulationController population;
        private SpriteRenderer stockRenderer;
        private object logsReservationOwner;
        private int reservedLogs;
        private int logsStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int LogsStored => logsStored;
        public int AvailableLogs => Mathf.Max(0, logsStored - reservedLogs);
        public bool HasStorageSpace => HasStorageSpaceFor(1);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyForestryController forestryController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            forestry = forestryController;
            population = populationController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "LumberjackCamp",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("workRadius", WorkRadius),
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
            resident.AssignWorkplace(this);
            StrategyDebugLogger.Info(
                "LumberjackCamp",
                "WorkerAssigned",
                StrategyDebugLogger.F("campOrigin", Origin),
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
                StrategyDebugLogger.Info(
                    "LumberjackCamp",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("campOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearWorkplace(this);
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

        public bool TryReserveMatureTree(object owner, out StrategyForestryTree tree)
        {
            tree = null;
            if (forestry == null)
            {
                return false;
            }

            if (!forestry.TryFindMatureTree(Origin, WorkRadius, out StrategyForestryTree candidate)
                || !HasStorageSpaceFor(candidate.LogYield)
                || !candidate.TryReserve(owner))
            {
                return false;
            }

            tree = candidate;
            return true;
        }

        public bool TryReserveProcessableWood(object owner, out StrategyForestryTree tree)
        {
            tree = null;
            if (forestry == null)
            {
                return false;
            }

            if (!forestry.TryFindProcessableWood(Origin, WorkRadius, out StrategyForestryTree candidate)
                || !HasStorageSpaceFor(candidate.LogYield)
                || !candidate.TryReserve(owner))
            {
                return false;
            }

            tree = candidate;
            return true;
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
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
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

        public bool TryReserveStoredLogs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || logsStored <= 0)
            {
                return false;
            }

            if (logsReservationOwner != null && logsReservationOwner != owner)
            {
                return false;
            }

            if (logsReservationOwner == owner && reservedLogs > 0)
            {
                amount = reservedLogs;
                return true;
            }

            int available = AvailableLogs;
            if (available <= 0)
            {
                return false;
            }

            reservedLogs = Mathf.Min(3, available);
            logsReservationOwner = owner;
            amount = reservedLogs;
            StrategyDebugLogger.Info(
                "LumberjackCamp",
                "LogsReserved",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", logsStored),
                StrategyDebugLogger.F("available", AvailableLogs),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            return true;
        }

        public bool TryTakeReservedLogs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null
                || logsReservationOwner != owner
                || reservedLogs <= 0
                || logsStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedLogs, logsStored);
            logsStored -= amount;
            reservedLogs = 0;
            logsReservationOwner = null;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "LumberjackCamp",
                "LogsTakenFromStock",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", logsStored),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            return amount > 0;
        }

        public void ReleaseStoredLogsReservation(object owner)
        {
            if (owner == null || logsReservationOwner != owner)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "LumberjackCamp",
                "LogsReservationReleased",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", reservedLogs),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            logsReservationOwner = null;
            reservedLogs = 0;
        }

        public void AddLogs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            logsStored = StrategyProductionStorage.AddCapped(logsStored, logsStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "LumberjackCamp",
                "LogsStored",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", logsStored));
        }

        public bool HasStorageSpaceFor(int amount)
        {
            return StrategyProductionStorage.CanAccept(logsStored, amount);
        }

        public bool TryFindPlantingCell(out Vector2Int cell)
        {
            cell = default;
            return forestry != null && forestry.TryFindPlantingCell(Origin, WorkRadius, out cell);
        }

        public bool TryPlantTree(Vector2Int cell)
        {
            return forestry != null && forestry.TryPlantTree(cell);
        }

        public string GetHudStatusText()
        {
            int mature = forestry != null ? forestry.CountMatureTrees(Origin, WorkRadius) : 0;
            int growing = forestry != null ? forestry.CountGrowingTrees(Origin, WorkRadius) : 0;
            int processable = forestry != null ? forestry.CountProcessableWood(Origin, WorkRadius) : 0;
            return "Workers: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "Logs: "
                + StrategyProductionStorage.Format(logsStored)
                + (reservedLogs > 0 ? " (" + reservedLogs + " reserved)" : string.Empty)
                + "\n"
                + "Trees: "
                + mature
                + "\n"
                + "Trunks: "
                + processable
                + "\n"
                + "Saplings: "
                + growing;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Logs Stock");
            stockObject.transform.SetParent(transform, false);
            stockRenderer = stockObject.AddComponent<SpriteRenderer>();
            stockRenderer.color = Color.white;
            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (stockRenderer == null)
            {
                return;
            }

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetLumberjackCampStockSprite(logsStored);
            stockRenderer.gameObject.SetActive(logsStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.28f, bounds.min.y + 0.34f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
            stockRenderer.transform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearWorkplace(this);
                }
            }

            workers.Clear();
            logsReservationOwner = null;
            reservedLogs = 0;
        }

        private static string GetOwnerName(object owner)
        {
            return owner is StrategyResidentAgent resident ? resident.FullName : owner?.ToString() ?? "none";
        }
    }
}
