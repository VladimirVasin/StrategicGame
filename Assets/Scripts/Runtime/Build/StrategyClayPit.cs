using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyClayPit : MonoBehaviour, IStrategyResourceStoreOwner
    {
        public const int MaxWorkers = 2;

        private readonly List<StrategyResidentAgent> workers = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyClayResourceController clay;
        private StrategyPopulationController population;
        private StrategyClayDeposit activeDeposit;
        private SpriteRenderer stockRenderer;
        private object clayReservationOwner;
        private int reservedClay;
        private readonly StrategyResourceStore resourceStore = new();
        private ref int clayStored => ref resourceStore.GetAmountRef(StrategyResourceType.Clay);

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int ClayStored => clayStored;
        public int AvailableClay => Mathf.Max(0, clayStored - reservedClay);
        public bool HasStorageSpace => HasStorageSpaceFor(1);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyClayResourceController clayController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            resourceStore.Bind(this, StrategyResourceStoreScope.Production, StrategyProductionStorage.LocalCapacity);
            map = mapController;
            clay = clayController;
            population = populationController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Clay",
                "ClayPitConfigured",
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
            resident.AssignClayPitWorkplace(this);
            StrategyDebugLogger.Info(
                "Clay",
                "ClayPitWorkerAssigned",
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
                    "Clay",
                    "ClayPitWorkerUnassigned",
                    StrategyDebugLogger.F("pitOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearClayPitWorkplace(this);
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

        public bool TryMineClay(int amount, out int minedAmount)
        {
            minedAmount = 0;
            if (amount <= 0 || !HasStorageSpaceFor(amount) || !EnsureActiveDeposit())
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

        public bool TryCollectEntranceCells(List<Vector2Int> candidates)
        {
            if (candidates == null)
            {
                return false;
            }

            candidates.Clear();
            if (map == null || building == null)
            {
                return false;
            }

            Vector2Int blockOrigin = building.Origin;
            Vector2Int blockFootprint = new Vector2Int(building.Footprint.x, building.Footprint.y + 1);
            for (int radius = 1; radius <= 3; radius++)
            {
                for (int y = -radius; y < blockFootprint.y + radius; y++)
                {
                    for (int x = -radius; x < blockFootprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == blockFootprint.x + radius - 1
                            || y == blockFootprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = blockOrigin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !candidates.Contains(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }
            }

            return candidates.Count > 0;
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            cell = default;
            List<Vector2Int> candidates = new();
            if (!TryCollectEntranceCells(candidates))
            {
                return false;
            }

            cell = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        public bool TryReserveStoredClay(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || clayStored <= 0 || clayReservationOwner != null && clayReservationOwner != owner)
            {
                return false;
            }

            if (clayReservationOwner == owner && reservedClay > 0)
            {
                amount = reservedClay;
                return true;
            }

            int available = AvailableClay;
            if (available <= 0)
            {
                return false;
            }

            reservedClay = Mathf.Min(StrategyProductionStorage.HaulerCarryLimit, available);
            clayReservationOwner = owner;
            amount = reservedClay;
            return true;
        }

        public bool TryTakeReservedClay(object owner, out int amount)
        {
            amount = 0;
            if (owner == null || clayReservationOwner != owner || reservedClay <= 0 || clayStored <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservedClay, clayStored);
            clayStored -= amount;
            reservedClay = 0;
            clayReservationOwner = null;
            UpdateStockVisual();
            return amount > 0;
        }

        public void ReleaseStoredClayReservation(object owner)
        {
            if (owner == null || clayReservationOwner != owner)
            {
                return;
            }

            clayReservationOwner = null;
            reservedClay = 0;
        }

        public void AddClay(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            clayStored = StrategyProductionStorage.AddCapped(clayStored, clayStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            PlayClayStoredEffect(accepted);
            StrategyDebugLogger.Info(
                "Clay",
                "ClayStoredAtPit",
                StrategyDebugLogger.F("pitOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", clayStored));
        }

        public bool HasStorageSpaceFor(int amount)
        {
            return StrategyProductionStorage.CanAccept(clayStored, amount);
        }

        public string GetHudStatusText()
        {
            int deposits = clay != null && building != null
                ? clay.CountAvailableDepositsInFootprint(Origin, building.Footprint)
                : 0;
            if (activeDeposit != null && !activeDeposit.IsDepleted)
            {
                deposits++;
            }

            return "Clay diggers: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\nClay: "
                + StrategyProductionStorage.Format(clayStored)
                + (reservedClay > 0 ? " (" + reservedClay + " reserved)" : string.Empty)
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

            if (clay == null
                || building == null
                || !clay.TryFindClayDepositInFootprint(Origin, building.Footprint, out StrategyClayDeposit deposit)
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

            GameObject stockObject = new GameObject("Clay Stock");
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

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetClayPitStockSprite(clayStored);
            stockRenderer.gameObject.SetActive(clayStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = GetClayStockWorld(bounds);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private void PlayClayStoredEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = GetClayStockWorld(building.FootprintBounds) + new Vector3(0f, 0.08f, -0.02f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                StrategyResourceType.Clay,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                clayStored + amount * 31);
        }

        private static Vector3 GetClayStockWorld(Bounds bounds)
        {
            return StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                StrategyBuildTool.ClayPit,
                StrategyResourceType.Clay,
                bounds);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearClayPitWorkplace(this);
                }
            }

            workers.Clear();
            activeDeposit?.Release(this);
            activeDeposit = null;
            clayReservationOwner = null;
            reservedClay = 0;
        }
    }
}
