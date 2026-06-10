using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStonecutterCamp : MonoBehaviour
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 9;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyStoneResourceController stone;
        private StrategyPopulationController population;
        private SpriteRenderer stockRenderer;
        private object stoneReservationOwner;
        private int reservedStone;
        private int stoneStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int StoneStored => stoneStored;
        public int AvailableStone => Mathf.Max(0, stoneStored - reservedStone);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyStoneResourceController stoneController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            stone = stoneController;
            population = populationController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
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
                if (resident != null && !resident.HasWorkplace && !resident.HasConstructionAssignment && !workers.Contains(resident))
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
                if (resident != null && !resident.HasWorkplace && !resident.HasConstructionAssignment && !workers.Contains(resident))
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
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignStoneWorkplace(this);
            StrategyDebugLogger.Info(
                "StonecutterCamp",
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
                    "StonecutterCamp",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("campOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearStoneWorkplace(this);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool TryReserveStoneDeposit(object owner, out StrategyStoneDeposit deposit)
        {
            deposit = null;
            if (stone == null)
            {
                return false;
            }

            if (!stone.TryFindStoneDeposit(Origin, WorkRadius, out StrategyStoneDeposit candidate)
                || !candidate.TryReserve(owner))
            {
                return false;
            }

            deposit = candidate;
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

        public bool TryReserveStoredStone(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || stoneStored <= 0)
            {
                return false;
            }

            if (stoneReservationOwner != null && stoneReservationOwner != owner)
            {
                return false;
            }

            if (stoneReservationOwner == owner && reservedStone > 0)
            {
                amount = reservedStone;
                return true;
            }

            int available = AvailableStone;
            if (available <= 0)
            {
                return false;
            }

            reservedStone = Mathf.Min(4, available);
            stoneReservationOwner = owner;
            amount = reservedStone;
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "StoneReserved",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored),
                StrategyDebugLogger.F("available", AvailableStone),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            return true;
        }

        public bool TryTakeReservedStone(object owner, out int amount)
        {
            amount = 0;
            if (owner == null
                || stoneReservationOwner != owner
                || reservedStone <= 0
                || stoneStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedStone, stoneStored);
            stoneStored -= amount;
            reservedStone = 0;
            stoneReservationOwner = null;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "StoneTakenFromStock",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            return amount > 0;
        }

        public void ReleaseStoredStoneReservation(object owner)
        {
            if (owner == null || stoneReservationOwner != owner)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "StoneReservationReleased",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", reservedStone),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            stoneReservationOwner = null;
            reservedStone = 0;
        }

        public void AddStone(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stoneStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "StoneStored",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", stoneStored));
        }

        public string GetHudStatusText()
        {
            int deposits = stone != null ? stone.CountAvailableDeposits(Origin, WorkRadius) : 0;
            return "\u0420\u0430\u0431\u043e\u0447\u0438\u0435: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "\u041a\u0430\u043c\u0435\u043d\u044c: "
                + stoneStored
                + (reservedStone > 0 ? " (" + reservedStone + " reserved)" : string.Empty)
                + "\n"
                + "\u0417\u0430\u043b\u0435\u0436\u0438: "
                + deposits;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Stone Stock");
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

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetStonecutterCampStockSprite(stoneStored);
            stockRenderer.gameObject.SetActive(stoneStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.34f, -0.13f);
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
                    worker.ClearStoneWorkplace(this);
                }
            }

            workers.Clear();
            stoneReservationOwner = null;
            reservedStone = 0;
        }

        private static string GetOwnerName(object owner)
        {
            return owner is StrategyResidentAgent resident ? resident.FullName : owner?.ToString() ?? "none";
        }
    }
}
