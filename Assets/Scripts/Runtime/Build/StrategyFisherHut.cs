using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFisherHut : MonoBehaviour
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 12;
        public const float CastRange = 4.8f;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyWildlifeController wildlife;
        private SpriteRenderer stockRenderer;
        private object fishReservationOwner;
        private int reservedFish;
        private int fishStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public int FishStored => fishStored;
        public int AvailableFish => Mathf.Max(0, fishStored - reservedFish);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyWildlifeController wildlifeController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            wildlife = wildlifeController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "FisherHut",
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
                    && resident.CanWork
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
                    && resident.CanWork
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
                || !resident.CanWork
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignFisherWorkplace(this);
            StrategyDebugLogger.Info(
                "FisherHut",
                "WorkerAssigned",
                StrategyDebugLogger.F("hutOrigin", Origin),
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
                    "FisherHut",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("hutOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearFisherWorkplace(this);
            }
        }

        public bool TryGetWorker(int index, out StrategyResidentAgent worker)
        {
            worker = index >= 0 && index < workers.Count ? workers[index] : null;
            return worker != null;
        }

        public bool TryReserveFishTarget(object owner, out StrategyFishAgent fish)
        {
            fish = null;
            if (wildlife == null)
            {
                wildlife = StrategyWildlifeController.Active;
            }

            return wildlife != null && wildlife.TryReserveFishForFishing(Origin, WorkRadius, owner, out fish);
        }

        public bool TryFindFishingCell(StrategyFishAgent fish, out Vector2Int cell)
        {
            cell = default;
            if (map == null || fish == null || !fish.TryGetCurrentCell(out Vector2Int fishCell))
            {
                return false;
            }

            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 6; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = fishCell + new Vector2Int(x, y);
                        if (!IsFishingStandCell(candidate))
                        {
                            continue;
                        }

                        float distance = Vector2.Distance(map.GetCellCenterWorld(candidate.x, candidate.y), fish.transform.position);
                        if (distance <= CastRange)
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

        public bool TryReserveStoredFish(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || fishStored <= 0)
            {
                return false;
            }

            if (fishReservationOwner != null && fishReservationOwner != owner)
            {
                return false;
            }

            if (fishReservationOwner == owner && reservedFish > 0)
            {
                amount = reservedFish;
                return true;
            }

            int available = AvailableFish;
            if (available <= 0)
            {
                return false;
            }

            reservedFish = Mathf.Min(2, available);
            fishReservationOwner = owner;
            amount = reservedFish;
            StrategyDebugLogger.Info(
                "FisherHut",
                "FishReserved",
                StrategyDebugLogger.F("hutOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", fishStored),
                StrategyDebugLogger.F("available", AvailableFish),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }

        public bool TryTakeReservedFish(object owner, out int amount)
        {
            amount = 0;
            if (owner == null
                || fishReservationOwner != owner
                || reservedFish <= 0
                || fishStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedFish, fishStored);
            fishStored -= amount;
            reservedFish = 0;
            fishReservationOwner = null;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "FisherHut",
                "FishTakenFromStock",
                StrategyDebugLogger.F("hutOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", fishStored),
                StrategyDebugLogger.F("owner", owner));
            return amount > 0;
        }

        public void ReleaseStoredFishReservation(object owner)
        {
            if (owner == null || fishReservationOwner != owner)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "FisherHut",
                "FishReservationReleased",
                StrategyDebugLogger.F("hutOrigin", Origin),
                StrategyDebugLogger.F("amount", reservedFish),
                StrategyDebugLogger.F("owner", owner));
            fishReservationOwner = null;
            reservedFish = 0;
        }

        public void AddFish(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            fishStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "FisherHut",
                "FishStored",
                StrategyDebugLogger.F("hutOrigin", Origin),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", fishStored));
        }

        public string GetHudStatusText()
        {
            int availableFish = wildlife != null ? wildlife.CountCatchableFish(Origin, WorkRadius) : 0;
            return "\u0420\u0430\u0431\u043e\u0447\u0438\u0435: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "\u0420\u044b\u0431\u0430: "
                + fishStored
                + (reservedFish > 0 ? " (\u0431\u0440\u043e\u043d\u044c: " + reservedFish + ")" : string.Empty)
                + "\n"
                + "\u0420\u044b\u0431\u044b \u0440\u044f\u0434\u043e\u043c: "
                + availableFish;
        }

        private bool IsFishingStandCell(Vector2Int cell)
        {
            if (map == null || !map.IsCellWalkable(cell))
            {
                return false;
            }

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Water)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Fish Stock");
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

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetFisherHutStockSprite(fishStored);
            stockRenderer.gameObject.SetActive(fishStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.25f, bounds.min.y + 0.38f, -0.13f);
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
                    worker.ClearFisherWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
