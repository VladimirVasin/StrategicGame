using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyFisherHut : MonoBehaviour, IStrategyResourceStoreOwner
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
        private float nextFishAvailabilityCheckTime;
        private float nextReservationFailureLogTime;
        private readonly StrategyResourceStore resourceStore = new();
        private ref int fishStored => ref resourceStore.GetAmountRef(StrategyResourceType.Fish);
        private static readonly Vector2Int[] CardinalFishingDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int FishStored => fishStored;
        public int AvailableFish => Mathf.Max(0, fishStored - reservedFish);
        public bool HasStorageSpace => HasStorageSpaceFor(1);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyWildlifeController wildlifeController)
        {
            building = placedBuilding;
            resourceStore.Bind(this, StrategyResourceStoreScope.Production, StrategyProductionStorage.LocalCapacity);
            map = mapController;
            population = populationController;
            wildlife = wildlifeController;
            EnsureStockRenderer();
            UpdateStockVisual();
            wildlife?.EnsureCatchableFishNearFisherHut(Origin, WorkRadius);
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

        public bool TryReserveFishTarget(object owner, out StrategyFishAgent fish)
        {
            fish = null;
            if (StrategySeasonalSurfaceController.IsWaterFrozenForGameplay)
            {
                return false;
            }

            if (wildlife == null)
            {
                wildlife = StrategyWildlifeController.Active;
            }

            if (!HasStorageSpace || wildlife == null)
            {
                LogReservationFailure(!HasStorageSpace ? "hut_storage_full" : "wildlife_unavailable");
                return false;
            }

            if (wildlife.TryReserveFishForFishing(
                Origin,
                WorkRadius,
                owner,
                candidate => CanReserveFishTarget(owner, candidate),
                out fish))
            {
                return true;
            }

            if (Time.time >= nextFishAvailabilityCheckTime)
            {
                nextFishAvailabilityCheckTime = Time.time + 2f;
                wildlife.EnsureCatchableFishNearFisherHut(
                    Origin,
                    WorkRadius,
                    wildlife.CountCatchableFish(Origin, WorkRadius) > 0);
            }

            LogReservationFailure(wildlife.CountCatchableFish(Origin, WorkRadius) > 0
                ? "no_reachable_shore"
                : "no_fish_in_radius");
            return false;
        }

        private bool CanReserveFishTarget(object owner, StrategyFishAgent fish)
        {
            if (!TryFindFishingCell(fish, out Vector2Int fishingCell))
            {
                return false;
            }

            StrategyResidentAgent resident = owner as StrategyResidentAgent;
            return resident == null || resident.CanReachCellForReservation(fishingCell);
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

                if (candidates.Count <= 0)
                {
                    continue;
                }

                cell = candidates[0];
                float bestSqr = (cell - Origin).sqrMagnitude;
                for (int i = 1; i < candidates.Count; i++)
                {
                    float sqr = (candidates[i] - Origin).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        cell = candidates[i];
                        bestSqr = sqr;
                    }
                }

                return true;
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

            int carryLimit = owner is StrategyResidentAgent { IsHouseholder: true } ? 1 : StrategyProductionStorage.HaulerCarryLimit;
            reservedFish = Mathf.Min(carryLimit, available);
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

            fishStored = StrategyProductionStorage.AddCapped(fishStored, fishStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "FisherHut",
                "FishStored",
                StrategyDebugLogger.F("hutOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", fishStored));
        }

        public bool HasStorageSpaceFor(int amount)
        {
            return StrategyProductionStorage.CanAccept(fishStored, amount);
        }

        public string GetHudStatusText()
        {
            int availableFish = wildlife != null ? wildlife.CountCatchableFish(Origin, WorkRadius) : 0;
            string waterStatus = StrategySeasonalSurfaceController.IsWaterFrozenForGameplay
                ? "\nWater: Frozen"
                : string.Empty;
            return "Workers: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "Fish: "
                + StrategyProductionStorage.Format(fishStored)
                + (reservedFish > 0 ? " (reserved: " + reservedFish + ")" : string.Empty)
                + "\n"
                + "Fish nearby: "
                + availableFish
                + waterStatus;
        }

        public bool IsValidFishingStandCell(Vector2Int cell)
        {
            return IsFishingStandCell(cell);
        }

        private bool IsFishingStandCell(Vector2Int cell)
        {
            if (map == null
                || !map.IsCellWalkable(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell standCell)
                || standCell.Kind == CityMapCellKind.Water)
            {
                return false;
            }

            for (int i = 0; i < CardinalFishingDirections.Length; i++)
            {
                Vector2Int neighborCell = cell + CardinalFishingDirections[i];
                if (map.TryGetCell(neighborCell.x, neighborCell.y, out CityMapCell neighbor)
                    && neighbor.Kind == CityMapCellKind.Water)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
