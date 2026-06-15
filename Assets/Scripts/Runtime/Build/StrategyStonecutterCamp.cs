using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStonecutterCamp : MonoBehaviour, IStrategyConstructionResourceSource
    {
        public const int MaxWorkers = 2;
        public const int WorkRadius = 9;

        private readonly List<StrategyResidentAgent> workers = new();
        private readonly Dictionary<object, int> constructionStoneReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();
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
        public int AvailableStone => Mathf.Max(0, stoneStored - reservedStone - CountReservations(constructionStoneReservations));
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        private sealed class ConstructionPickupReservation
        {
            public object Owner;
            public int Amount;
        }

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

        public static int GetTotalAvailableConstructionStone()
        {
            int total = 0;
            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyStonecutterCamp camp = camps[i];
                if (camp != null)
                {
                    total += camp.AvailableStone;
                }
            }

            return total;
        }

        public static int ReserveConstructionStone(object owner, int requested, Vector3 nearWorld)
        {
            if (owner == null || requested <= 0)
            {
                return 0;
            }

            int remaining = requested;
            StrategyStonecutterCamp[] camps = GetCampsSortedByDistance(nearWorld);
            for (int i = 0; i < camps.Length && remaining > 0; i++)
            {
                StrategyStonecutterCamp camp = camps[i];
                if (camp == null)
                {
                    continue;
                }

                remaining -= camp.ReserveConstructionStone(owner, remaining);
            }

            return requested - remaining;
        }

        public static int SpendAvailableConstructionStone(int requested, Vector3 nearWorld)
        {
            if (requested <= 0)
            {
                return 0;
            }

            int remaining = requested;
            StrategyStonecutterCamp[] camps = GetCampsSortedByDistance(nearWorld);
            for (int i = 0; i < camps.Length && remaining > 0; i++)
            {
                StrategyStonecutterCamp camp = camps[i];
                if (camp == null)
                {
                    continue;
                }

                remaining -= camp.SpendAvailableStone(remaining);
            }

            return requested - remaining;
        }

        public static bool TryFindConstructionPickup(
            object owner,
            Vector3 nearWorld,
            out StrategyStonecutterCamp camp,
            out Vector2Int pickupCell)
        {
            camp = null;
            pickupCell = default;
            if (owner == null)
            {
                return false;
            }

            StrategyStonecutterCamp[] camps = GetCampsSortedByDistance(nearWorld);
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyStonecutterCamp candidate = camps[i];
                if (candidate == null || !candidate.HasAvailableConstructionReservation(owner))
                {
                    continue;
                }

                if (candidate.TryFindDropoffCell(out pickupCell))
                {
                    camp = candidate;
                    return true;
                }
            }

            return false;
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                camps[i]?.ReleaseConstructionReservation(owner);
            }
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

        public bool TryReserveConstructionPickup(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null
                || builder == null
                || kind != StrategyConstructionResourceKind.Stone
                || amount <= 0)
            {
                return false;
            }

            ReleaseConstructionPickupReservation(builder);
            int available = GetAvailableConstructionReservationAmount(owner);
            if (available < amount)
            {
                return false;
            }

            constructionPickupReservations[builder] = new ConstructionPickupReservation
            {
                Owner = owner,
                Amount = amount
            };

            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStonePickupReserved",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("unclaimed", GetAvailableConstructionReservationAmount(owner)));
            return true;
        }

        public void ReleaseConstructionPickupReservation(StrategyResidentAgent builder)
        {
            if (builder == null || !constructionPickupReservations.Remove(builder))
            {
                return;
            }

            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStonePickupReleased",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("builder", builder.FullName));
        }

        public bool TryTakeReservedConstructionResource(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int maxAmount,
            out int amount)
        {
            amount = 0;
            if (owner == null
                || builder == null
                || kind != StrategyConstructionResourceKind.Stone
                || maxAmount <= 0
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Amount <= 0)
            {
                return false;
            }

            if (!constructionStoneReservations.TryGetValue(owner, out int reserved)
                || reserved <= 0
                || stoneStored <= 0)
            {
                constructionPickupReservations.Remove(builder);
                return false;
            }

            amount = Mathf.Min(maxAmount, pickup.Amount, reserved, stoneStored);
            stoneStored -= amount;
            reserved -= amount;
            pickup.Amount -= amount;
            if (reserved <= 0)
            {
                constructionStoneReservations.Remove(owner);
            }
            else
            {
                constructionStoneReservations[owner] = reserved;
            }

            if (pickup.Amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStoneTaken",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored));
            return amount > 0;
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
            return "Workers: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "Stone: "
                + stoneStored
                + (reservedStone > 0 ? " (" + reservedStone + " reserved)" : string.Empty)
                + "\n"
                + "Deposits: "
                + deposits;
        }

        private int ReserveConstructionStone(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableStone);
            if (owner == null || amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionStoneReservations, owner, amount);
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStoneReserved",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored),
                StrategyDebugLogger.F("available", AvailableStone));
            return amount;
        }

        private int SpendAvailableStone(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableStone);
            if (amount <= 0)
            {
                return 0;
            }

            stoneStored -= amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStoneSpent",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored));
            return amount;
        }

        private bool HasAvailableConstructionReservation(object owner)
        {
            return GetAvailableConstructionReservationAmount(owner) > 0;
        }

        private int GetAvailableConstructionReservationAmount(object owner)
        {
            if (owner == null
                || !constructionStoneReservations.TryGetValue(owner, out int amount)
                || amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, amount - CountPickupReservations(owner));
        }

        private void ReleaseConstructionReservation(object owner)
        {
            if (owner == null)
            {
                return;
            }

            constructionStoneReservations.Remove(owner);
            if (constructionPickupReservations.Count <= 0)
            {
                return;
            }

            List<StrategyResidentAgent> buildersToRelease = new();
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                if (pair.Value != null && ReferenceEquals(pair.Value.Owner, owner))
                {
                    buildersToRelease.Add(pair.Key);
                }
            }

            for (int i = 0; i < buildersToRelease.Count; i++)
            {
                constructionPickupReservations.Remove(buildersToRelease[i]);
            }
        }

        private int CountPickupReservations(object owner)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                ConstructionPickupReservation reservation = pair.Value;
                if (pair.Key != null
                    && reservation != null
                    && ReferenceEquals(reservation.Owner, owner)
                    && reservation.Amount > 0)
                {
                    total += reservation.Amount;
                }
            }

            return total;
        }

        private static void AddReservation(Dictionary<object, int> reservations, object owner, int amount)
        {
            if (reservations.TryGetValue(owner, out int current))
            {
                reservations[owner] = current + amount;
            }
            else
            {
                reservations.Add(owner, amount);
            }
        }

        private static int CountReservations(Dictionary<object, int> reservations)
        {
            int total = 0;
            foreach (KeyValuePair<object, int> pair in reservations)
            {
                if (pair.Key != null && pair.Value > 0)
                {
                    total += pair.Value;
                }
            }

            return total;
        }

        private static StrategyStonecutterCamp[] GetCampsSortedByDistance(Vector3 nearWorld)
        {
            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            System.Array.Sort(
                camps,
                (left, right) =>
                {
                    if (left == null && right == null)
                    {
                        return 0;
                    }

                    if (left == null)
                    {
                        return 1;
                    }

                    if (right == null)
                    {
                        return -1;
                    }

                    float leftDistance = (left.FootprintBounds.center - nearWorld).sqrMagnitude;
                    float rightDistance = (right.FootprintBounds.center - nearWorld).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });
            return camps;
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
            constructionStoneReservations.Clear();
            constructionPickupReservations.Clear();
        }

        private static string GetOwnerName(object owner)
        {
            return owner is StrategyResidentAgent resident ? resident.FullName : owner?.ToString() ?? "none";
        }
    }
}
