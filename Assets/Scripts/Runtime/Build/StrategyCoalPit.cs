using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyCoalPit : MonoBehaviour, IStrategyResourceStoreOwner
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
        private readonly StrategyResourceStore resourceStore = new();
        private ref int coalStored => ref resourceStore.GetAmountRef(StrategyResourceType.Coal);

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public int WorkerCount => workers.Count;
        public StrategyResourceStore ResourceStore => resourceStore;
        public int CoalStored => coalStored;
        public int AvailableCoal => Mathf.Max(0, coalStored - reservedCoal);
        public bool HasStorageSpace => HasStorageSpaceFor(1);
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyCoalResourceController coalController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            resourceStore.Bind(this, StrategyResourceStoreScope.Production, StrategyProductionStorage.LocalCapacity);
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

        public Vector3 GetInteriorWorkWorld()
        {
            return StrategyBuildingVisualAnchorProfile.GetInteriorWorkWorld(
                StrategyBuildTool.CoalPit,
                FootprintBounds,
                0,
                1);
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

            reservedCoal = Mathf.Min(StrategyProductionStorage.HaulerCarryLimit, available);
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

            coalStored = StrategyProductionStorage.AddCapped(coalStored, coalStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            PlayCoalStoredEffect(accepted);
            StrategyDebugLogger.Info(
                "Coal",
                "CoalStoredAtPit",
                StrategyDebugLogger.F("pitOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", coalStored));
        }

        public bool HasStorageSpaceFor(int amount)
        {
            return StrategyProductionStorage.CanAccept(coalStored, amount);
        }

        public void PlayMiningWorkEffect(int seed)
        {
            Vector3 world = GetInteriorWorkWorld() + new Vector3(0.16f, 0.04f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.CoalChips,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                seed,
                0.82f);
            if (Mathf.Abs(seed) % 2 == 0)
            {
                StrategyWorldEffectAnimator.Spawn(
                    StrategyWorldEffectKind.Dust,
                    world + new Vector3(-0.06f, 0.01f, -0.01f),
                    StrategyWorldSorting.ForPosition(world, 3),
                    seed + 17,
                    0.66f);
            }
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
                + StrategyProductionStorage.Format(coalStored)
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
            Vector3 world = GetCoalStockWorld(bounds);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private void PlayCoalStoredEffect(int amount)
        {
            if (amount <= 0 || building == null)
            {
                return;
            }

            Vector3 world = GetCoalStockWorld(building.FootprintBounds) + new Vector3(0f, 0.08f, -0.02f);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                StrategyResourceType.Coal,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                coalStored + amount * 29);
        }

        private static Vector3 GetCoalStockWorld(Bounds bounds)
        {
            return StrategyBuildingVisualAnchorProfile.GetStockAnchorWorld(
                StrategyBuildTool.CoalPit,
                StrategyResourceType.Coal,
                bounds);
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
