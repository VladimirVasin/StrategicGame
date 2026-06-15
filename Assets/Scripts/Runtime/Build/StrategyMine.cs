using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyMine : MonoBehaviour
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 9;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyIronResourceController iron;
        private StrategyPopulationController population;
        private SpriteRenderer stockRenderer;
        private object ironReservationOwner;
        private int reservedIron;
        private int ironStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int IronStored => ironStored;
        public int AvailableIron => Mathf.Max(0, ironStored - reservedIron);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyIronResourceController ironController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            iron = ironController;
            population = populationController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Mine",
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
            resident.AssignMineWorkplace(this);
            StrategyDebugLogger.Info(
                "Mine",
                "WorkerAssigned",
                StrategyDebugLogger.F("mineOrigin", Origin),
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
                    "Mine",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("mineOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearMineWorkplace(this);
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

        public bool TryReserveIronDeposit(object owner, out StrategyIronDeposit deposit)
        {
            deposit = null;
            if (iron == null || building == null)
            {
                return false;
            }

            if (!iron.TryFindIronDepositInFootprint(Origin, building.Footprint, out StrategyIronDeposit candidate)
                || !candidate.TryReserve(owner))
            {
                return false;
            }

            deposit = candidate;
            return true;
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

        public bool TryReserveStoredIron(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || ironStored <= 0 || ironReservationOwner != null && ironReservationOwner != owner)
            {
                return false;
            }

            if (ironReservationOwner == owner && reservedIron > 0)
            {
                amount = reservedIron;
                return true;
            }

            int available = AvailableIron;
            if (available <= 0)
            {
                return false;
            }

            reservedIron = Mathf.Min(4, available);
            ironReservationOwner = owner;
            amount = reservedIron;
            StrategyDebugLogger.Info(
                "Mine",
                "IronReserved",
                StrategyDebugLogger.F("mineOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", ironStored),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            return true;
        }

        public bool TryTakeReservedIron(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || ironReservationOwner != owner || reservedIron <= 0 || ironStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedIron, ironStored);
            ironStored -= amount;
            reservedIron = 0;
            ironReservationOwner = null;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Mine",
                "IronTakenFromStock",
                StrategyDebugLogger.F("mineOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", ironStored),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            return amount > 0;
        }

        public void ReleaseStoredIronReservation(object owner)
        {
            if (owner == null || ironReservationOwner != owner)
            {
                return;
            }

            ironReservationOwner = null;
            reservedIron = 0;
        }

        public void AddIron(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ironStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Mine",
                "IronStored",
                StrategyDebugLogger.F("mineOrigin", Origin),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", ironStored));
        }

        public string GetHudStatusText()
        {
            int deposits = iron != null && building != null
                ? iron.CountAvailableDepositsInFootprint(Origin, building.Footprint)
                : 0;
            return "Miners: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\nIron: "
                + ironStored
                + (reservedIron > 0 ? " (" + reservedIron + " reserved)" : string.Empty)
                + "\nDeposits: "
                + deposits;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Iron Stock");
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

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetMineIronStockSprite(ironStored);
            stockRenderer.gameObject.SetActive(ironStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.26f, bounds.min.y + 0.32f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearMineWorkplace(this);
                }
            }

            workers.Clear();
            ironReservationOwner = null;
            reservedIron = 0;
        }

        private static string GetOwnerName(object owner)
        {
            return owner is StrategyResidentAgent resident ? resident.FullName : owner?.ToString() ?? "none";
        }
    }
}
