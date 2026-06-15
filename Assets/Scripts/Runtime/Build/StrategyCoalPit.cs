using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCoalPit : MonoBehaviour
    {
        public const int MaxWorkers = 2;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyCoalResourceController coal;
        private StrategyPopulationController population;
        private StrategyCoalDeposit activeDeposit;
        private SpriteRenderer stockRenderer;
        private object coalReservationOwner;
        private int reservedCoal;
        private int coalStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int CoalStored => coalStored;
        public int AvailableCoal => Mathf.Max(0, coalStored - reservedCoal);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyCoalResourceController coalController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            coal = coalController;
            population = populationController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Coal",
                "CoalPitConfigured",
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
            resident.AssignCoalPitWorkplace(this);
            StrategyDebugLogger.Info(
                "Coal",
                "CoalPitWorkerAssigned",
                StrategyDebugLogger.F("pitOrigin", Origin),
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
                    "Coal",
                    "CoalPitWorkerUnassigned",
                    StrategyDebugLogger.F("pitOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearCoalPitWorkplace(this);
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

        public bool TryMineCoal(int amount, out int minedAmount)
        {
            minedAmount = 0;
            if (amount <= 0 || !EnsureActiveDeposit())
            {
                return false;
            }

            bool mined = activeDeposit.TryMine(this, amount, out minedAmount);
            if (activeDeposit == null || activeDeposit.IsDepleted)
            {
                activeDeposit = null;
            }

            return mined;
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

        public Vector3 GetInteriorWorkWorld()
        {
            Bounds bounds = FootprintBounds;
            return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.38f, -0.08f);
        }

        public bool TryReserveStoredCoal(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || coalStored <= 0 || coalReservationOwner != null && coalReservationOwner != owner)
            {
                return false;
            }

            if (coalReservationOwner == owner && reservedCoal > 0)
            {
                amount = reservedCoal;
                return true;
            }

            int available = AvailableCoal;
            if (available <= 0)
            {
                return false;
            }

            reservedCoal = Mathf.Min(4, available);
            coalReservationOwner = owner;
            amount = reservedCoal;
            return true;
        }

        public bool TryTakeReservedCoal(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || coalReservationOwner != owner || reservedCoal <= 0 || coalStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedCoal, coalStored);
            coalStored -= amount;
            reservedCoal = 0;
            coalReservationOwner = null;
            UpdateStockVisual();
            return amount > 0;
        }

        public void ReleaseStoredCoalReservation(object owner)
        {
            if (owner == null || coalReservationOwner != owner)
            {
                return;
            }

            coalReservationOwner = null;
            reservedCoal = 0;
        }

        public void AddCoal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coalStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Coal",
                "CoalStoredAtPit",
                StrategyDebugLogger.F("pitOrigin", Origin),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", coalStored));
        }

        public string GetHudStatusText()
        {
            int deposits = coal != null && building != null
                ? coal.CountAvailableDepositsInFootprint(Origin, building.Footprint)
                : 0;
            if (activeDeposit != null && !activeDeposit.IsDepleted)
            {
                deposits++;
            }

            return "Coal miners: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\nCoal: "
                + coalStored
                + (reservedCoal > 0 ? " (" + reservedCoal + " reserved)" : string.Empty)
                + "\nDeposits: "
                + deposits;
        }

        private bool EnsureActiveDeposit()
        {
            if (activeDeposit != null && !activeDeposit.IsDepleted)
            {
                return activeDeposit.TryReserve(this);
            }

            if (activeDeposit != null)
            {
                activeDeposit.Release(this);
                activeDeposit = null;
            }

            if (coal == null
                || building == null
                || !coal.TryFindCoalDepositInFootprint(Origin, building.Footprint, out StrategyCoalDeposit deposit)
                || !deposit.TryReserve(this))
            {
                return false;
            }

            activeDeposit = deposit;
            return true;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Coal Stock");
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

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetCoalPitStockSprite(coalStored);
            stockRenderer.gameObject.SetActive(coalStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.26f, bounds.min.y + 0.30f, -0.13f);
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
                    worker.ClearCoalPitWorkplace(this);
                }
            }

            workers.Clear();
            activeDeposit?.Release(this);
            activeDeposit = null;
            coalReservationOwner = null;
            reservedCoal = 0;
        }
    }
}
