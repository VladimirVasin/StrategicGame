using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyStorageYard : MonoBehaviour, IStrategyConstructionResourceSource
    {
        private readonly List<StrategyResidentAgent> workers = new();
        private readonly List<StrategyResidentAgent> builders = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer logsStockRenderer;
        private SpriteRenderer stoneStockRenderer;
        private readonly Dictionary<object, int> constructionLogReservations = new();
        private readonly Dictionary<object, int> constructionStoneReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();
        private int logsStored;
        private int stoneStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public IReadOnlyList<StrategyResidentAgent> Builders => builders;
        public int WorkerCount => workers.Count;
        public int BuilderCount => builders.Count;
        public int LogsStored => logsStored;
        public int StoneStored => stoneStored;
        public int AvailableConstructionLogs => Mathf.Max(0, logsStored - CountReservations(constructionLogReservations));
        public int AvailableConstructionStone => Mathf.Max(0, stoneStored - CountReservations(constructionStoneReservations));
        public Vector2Int Origin => building != null ? building.Origin : Vector2Int.zero;
        public Bounds FootprintBounds => building != null ? building.FootprintBounds : new Bounds(transform.position, Vector3.one);

        private sealed class ConstructionPickupReservation
        {
            public object Owner;
            public StrategyConstructionResourceKind Kind;
            public int Amount;
        }

        public void Configure(
            StrategyPlacedBuilding placedBuilding,
            CityMapController mapController,
            StrategyPopulationController populationController)
        {
            building = placedBuilding;
            map = mapController;
            population = populationController;
            EnsureStockRenderer();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "Configured",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("workerLimit", "unlimited"),
                StrategyDebugLogger.F("builderLimit", "unlimited"));
        }

        public static bool TryAssignBuildersToSite(StrategyConstructionSite site)
        {
            if (site == null || site.IsCompleted || site.BuilderCount >= StrategyConstructionSite.MaxBuilders)
            {
                return false;
            }

            int assignedCount = 0;
            StrategyStorageYard[] yards = GetYardsSortedByDistance(site.FootprintBounds.center);
            for (int i = 0; i < yards.Length && site.BuilderCount < StrategyConstructionSite.MaxBuilders; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null)
                {
                    continue;
                }

                while (site.BuilderCount < StrategyConstructionSite.MaxBuilders
                    && yard.TryGetAvailableBuilder(out StrategyResidentAgent builder))
                {
                    if (!site.RegisterBuilder(builder, false))
                    {
                        break;
                    }

                    builder.AssignConstructionSite(site, false);
                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                StrategyDebugLogger.Info(
                    "Construction",
                    "BuildersDispatched",
                    StrategyDebugLogger.F("tool", site.Tool),
                    StrategyDebugLogger.F("origin", site.Origin),
                    StrategyDebugLogger.F("assigned", assignedCount),
                    StrategyDebugLogger.F("builderCount", site.BuilderCount));
            }

            return assignedCount > 0;
        }

        public static StrategyConstructionResourceCost GetTotalConstructionResources()
        {
            int logs = 0;
            int stone = 0;
            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            for (int i = 0; i < yards.Length; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null)
                {
                    continue;
                }

                logs += yard.AvailableConstructionLogs;
                stone += yard.AvailableConstructionStone;
            }

            StrategyConstructionResourceCost loose = StrategyLooseConstructionResourcePile.GetTotalAvailableResources();
            return new StrategyConstructionResourceCost(logs + loose.Logs, stone + loose.Stone);
        }

        public static bool CanAffordConstruction(StrategyConstructionResourceCost cost)
        {
            return cost.CanAfford(GetTotalConstructionResources());
        }

        public static bool TrySpendConstructionResources(
            StrategyConstructionResourceCost cost,
            Vector3 nearWorld,
            string reason)
        {
            if (cost.IsFree)
            {
                return true;
            }

            StrategyConstructionResourceCost available = GetTotalConstructionResources();
            if (!cost.CanAfford(available))
            {
                StrategyDebugLogger.Warn(
                    "StorageYard",
                    "ConstructionSpendRejected",
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("costLogs", cost.Logs),
                    StrategyDebugLogger.F("costStone", cost.Stone),
                    StrategyDebugLogger.F("availableLogs", available.Logs),
                    StrategyDebugLogger.F("availableStone", available.Stone));
                return false;
            }

            StrategyStorageYard[] yards = GetYardsSortedByDistance(nearWorld);
            int remainingLogs = cost.Logs;
            for (int i = 0; i < yards.Length && remainingLogs > 0; i++)
            {
                remainingLogs -= yards[i].SpendAvailableLogs(remainingLogs);
            }

            int remainingStone = cost.Stone;
            for (int i = 0; i < yards.Length && remainingStone > 0; i++)
            {
                remainingStone -= yards[i].SpendAvailableStone(remainingStone);
            }

            if (remainingLogs > 0 || remainingStone > 0)
            {
                StrategyDebugLogger.Warn(
                    "StorageYard",
                    "ConstructionSpendShort",
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("remainingLogs", remainingLogs),
                    StrategyDebugLogger.F("remainingStone", remainingStone));
                return false;
            }

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionResourcesSpent",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("logs", cost.Logs),
                StrategyDebugLogger.F("stone", cost.Stone));
            return true;
        }

        public static bool TryReserveConstructionResources(
            StrategyConstructionResourceCost cost,
            object owner,
            Vector3 nearWorld)
        {
            if (owner == null)
            {
                return false;
            }

            if (cost.IsFree)
            {
                return true;
            }

            StrategyConstructionResourceCost available = GetTotalConstructionResources();
            if (!cost.CanAfford(available))
            {
                return false;
            }

            int remainingLogs = cost.Logs;
            remainingLogs -= StrategyLooseConstructionResourcePile.ReserveConstructionResources(
                owner,
                StrategyConstructionResourceKind.Logs,
                remainingLogs,
                nearWorld);
            StrategyStorageYard[] yards = GetYardsSortedByDistance(nearWorld);
            for (int i = 0; i < yards.Length && remainingLogs > 0; i++)
            {
                int reserved = yards[i].ReserveConstructionLogs(owner, remainingLogs);
                remainingLogs -= reserved;
            }

            int remainingStone = cost.Stone;
            remainingStone -= StrategyLooseConstructionResourcePile.ReserveConstructionResources(
                owner,
                StrategyConstructionResourceKind.Stone,
                remainingStone,
                nearWorld);
            for (int i = 0; i < yards.Length && remainingStone > 0; i++)
            {
                int reserved = yards[i].ReserveConstructionStone(owner, remainingStone);
                remainingStone -= reserved;
            }

            if (remainingLogs > 0 || remainingStone > 0)
            {
                ReleaseConstructionReservations(owner);
                return false;
            }

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionReserved",
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("logs", cost.Logs),
                StrategyDebugLogger.F("stone", cost.Stone));
            return true;
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            for (int i = 0; i < yards.Length; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard != null)
                {
                    yard.ReleaseConstructionReservation(owner);
                }
            }

            StrategyLooseConstructionResourcePile.ReleaseConstructionReservations(owner);
        }

        public static bool TryFindConstructionPickup(
            object owner,
            StrategyConstructionResourceKind kind,
            Vector3 nearWorld,
            out IStrategyConstructionResourceSource source,
            out Vector2Int pickupCell)
        {
            source = null;
            pickupCell = default;
            if (owner == null || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            if (StrategyLooseConstructionResourcePile.TryFindConstructionPickup(owner, kind, nearWorld, out StrategyLooseConstructionResourcePile pile, out pickupCell))
            {
                source = pile;
                return true;
            }

            StrategyStorageYard[] yards = GetYardsSortedByDistance(nearWorld);
            for (int i = 0; i < yards.Length; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null || !yard.HasAvailableConstructionReservation(owner, kind))
                {
                    continue;
                }

                if (yard.TryFindDropoffCell(out pickupCell))
                {
                    source = yard;
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindNearestStorageYard(Vector3 nearWorld, out StrategyStorageYard yard)
        {
            StrategyStorageYard[] yards = GetYardsSortedByDistance(nearWorld);
            for (int i = 0; i < yards.Length; i++)
            {
                if (yards[i] != null)
                {
                    yard = yards[i];
                    return true;
                }
            }

            yard = null;
            return false;
        }

        public static bool TryFindNearestDropoff(
            Vector3 nearWorld,
            out StrategyStorageYard yard,
            out Vector2Int dropoffCell)
        {
            StrategyStorageYard[] yards = GetYardsSortedByDistance(nearWorld);
            for (int i = 0; i < yards.Length; i++)
            {
                yard = yards[i];
                if (yard != null && yard.TryFindDropoffCell(out dropoffCell))
                {
                    return true;
                }
            }

            yard = null;
            dropoffCell = default;
            return false;
        }

        public bool TryReserveConstructionPickup(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null
                || builder == null
                || amount <= 0
                || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            ReleaseConstructionPickupReservation(builder);
            int available = GetAvailableReservationAmount(owner, kind);
            if (available < amount)
            {
                return false;
            }

            constructionPickupReservations[builder] = new ConstructionPickupReservation
            {
                Owner = owner,
                Kind = kind,
                Amount = amount
            };

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionPickupReserved",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("unclaimed", GetAvailableReservationAmount(owner, kind)));
            return true;
        }

        public void ReleaseConstructionPickupReservation(StrategyResidentAgent builder)
        {
            if (builder == null || !constructionPickupReservations.Remove(builder))
            {
                return;
            }

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionPickupReleased",
                StrategyDebugLogger.F("yardOrigin", Origin),
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
                || maxAmount <= 0
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Kind != kind
                || pickup.Amount <= 0)
            {
                return false;
            }

            int requested = Mathf.Min(maxAmount, pickup.Amount);
            if (kind == StrategyConstructionResourceKind.Logs)
            {
                amount = TakeReservedConstruction(owner, constructionLogReservations, ref logsStored, requested);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                amount = TakeReservedConstruction(owner, constructionStoneReservations, ref stoneStored, requested);
            }

            if (amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
                return false;
            }

            pickup.Amount -= amount;
            if (pickup.Amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionResourceTaken",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("logsStock", logsStored),
                StrategyDebugLogger.F("stoneStock", stoneStored));
            return true;
        }

        public void ReturnReservedConstructionResource(
            object owner,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null || amount <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return;
            }

            if (kind == StrategyConstructionResourceKind.Logs)
            {
                logsStored += amount;
                AddReservation(constructionLogReservations, owner, amount);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                stoneStored += amount;
                AddReservation(constructionStoneReservations, owner, amount);
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionResourceReturnedToReservation",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("logsStock", logsStored),
                StrategyDebugLogger.F("stoneStock", stoneStored),
                StrategyDebugLogger.F("ownerUnclaimed", GetAvailableReservationAmount(owner, kind)));
        }

        public bool CanAssignNextAvailableWorker()
        {
            if (population == null)
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
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableWorker(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (population == null)
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
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
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
                || workers.Contains(resident)
                || builders.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            workers.Add(resident);
            resident.AssignStorageWorkplace(this);
            StrategyDebugLogger.Info(
                "StorageYard",
                "WorkerAssigned",
                StrategyDebugLogger.F("yardOrigin", Origin),
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
                    "StorageYard",
                    "WorkerUnassigned",
                    StrategyDebugLogger.F("yardOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("workerCount", workers.Count));
                worker.ClearStorageWorkplace(this);
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

        public bool CanAssignNextAvailableBuilder()
        {
            if (population == null)
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
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignNextAvailableBuilder(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (population == null)
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
                    && !workers.Contains(resident)
                    && !builders.Contains(resident))
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            assigned = candidates[Random.Range(0, candidates.Count)];
            bool assignedToYard = AssignBuilder(assigned);
            if (assignedToYard)
            {
                TryDispatchBuilder(assigned);
            }

            return assignedToYard;
        }

        public bool AssignBuilder(StrategyResidentAgent resident)
        {
            if (resident == null
                || workers.Contains(resident)
                || builders.Contains(resident)
                || !resident.CanAcceptWorkAssignment
                || resident.HasWorkplace
                || resident.HasConstructionAssignment)
            {
                return false;
            }

            builders.Add(resident);
            resident.AssignBuilderWorkplace(this);
            StrategyDebugLogger.Info(
                "StorageYard",
                "BuilderAssigned",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("builder", resident.FullName),
                StrategyDebugLogger.F("builderCount", builders.Count));
            return true;
        }

        public void UnassignBuilderAt(int index)
        {
            if (index < 0 || index >= builders.Count)
            {
                return;
            }

            StrategyResidentAgent builder = builders[index];
            builders.RemoveAt(index);
            if (builder != null)
            {
                StrategyDebugLogger.Info(
                    "StorageYard",
                    "BuilderUnassigned",
                    StrategyDebugLogger.F("yardOrigin", Origin),
                    StrategyDebugLogger.F("builder", builder.FullName),
                    StrategyDebugLogger.F("builderCount", builders.Count));
                builder.ClearBuilderWorkplace(this);
            }
        }

        public void UnassignBuilder(StrategyResidentAgent builder)
        {
            int index = builders.IndexOf(builder);
            if (index >= 0)
            {
                UnassignBuilderAt(index);
            }
        }

        public bool TryGetBuilder(int index, out StrategyResidentAgent builder)
        {
            builder = index >= 0 && index < builders.Count ? builders[index] : null;
            return builder != null;
        }

        public bool TryReserveLogSource(object owner, out StrategyLumberjackCamp source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyLumberjackCamp[] camps = Object.FindObjectsByType<StrategyLumberjackCamp>();
            StrategyLumberjackCamp bestCamp = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyLumberjackCamp camp = camps[i];
                if (camp == null || camp.AvailableLogs <= 0)
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCamp = camp;
                }
            }

            if (bestCamp == null || !bestCamp.TryReserveStoredLogs(owner, out _))
            {
                return false;
            }

            source = bestCamp;
            return true;
        }

        public bool TryReserveStoneSource(object owner, out StrategyStonecutterCamp source)
        {
            source = null;
            if (owner == null)
            {
                return false;
            }

            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            StrategyStonecutterCamp bestCamp = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyStonecutterCamp camp = camps[i];
                if (camp == null || camp.AvailableStone <= 0)
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCamp = camp;
                }
            }

            if (bestCamp == null || !bestCamp.TryReserveStoredStone(owner, out _))
            {
                return false;
            }

            source = bestCamp;
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

        public void AddLogs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            logsStored += amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StorageYard",
                "LogsStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", logsStored));
        }

        public void AddResource(StrategyResourceType resource, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (resource == StrategyResourceType.Stone)
            {
                AddStone(amount);
            }
        }

        public int GetAmount(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Stone ? stoneStored : 0;
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
                "StorageYard",
                "ResourceStored",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", stoneStored));
        }

        private int ReserveConstructionLogs(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionLogs);
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionLogReservations, owner, amount);
            return amount;
        }

        private int ReserveConstructionStone(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionStone);
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionStoneReservations, owner, amount);
            return amount;
        }

        private int SpendAvailableLogs(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionLogs);
            if (amount <= 0)
            {
                return 0;
            }

            logsStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private int SpendAvailableStone(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionStone);
            if (amount <= 0)
            {
                return 0;
            }

            stoneStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private void ReleaseConstructionReservation(object owner)
        {
            constructionLogReservations.Remove(owner);
            constructionStoneReservations.Remove(owner);
            ReleaseConstructionPickupReservations(owner);
        }

        private bool HasAvailableConstructionReservation(object owner, StrategyConstructionResourceKind kind)
        {
            return GetAvailableReservationAmount(owner, kind) > 0;
        }

        private int GetAvailableReservationAmount(object owner, StrategyConstructionResourceKind kind)
        {
            Dictionary<object, int> source = kind == StrategyConstructionResourceKind.Logs
                ? constructionLogReservations
                : constructionStoneReservations;
            if (!source.TryGetValue(owner, out int amount) || amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, amount - CountPickupReservations(owner, kind));
        }

        private int CountPickupReservations(object owner, StrategyConstructionResourceKind kind)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                ConstructionPickupReservation reservation = pair.Value;
                if (pair.Key != null
                    && reservation != null
                    && ReferenceEquals(reservation.Owner, owner)
                    && reservation.Kind == kind
                    && reservation.Amount > 0)
                {
                    total += reservation.Amount;
                }
            }

            return total;
        }

        private void ReleaseConstructionPickupReservations(object owner)
        {
            if (owner == null || constructionPickupReservations.Count <= 0)
            {
                return;
            }

            List<StrategyResidentAgent> buildersToRelease = new();
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                ConstructionPickupReservation reservation = pair.Value;
                if (reservation != null && ReferenceEquals(reservation.Owner, owner))
                {
                    buildersToRelease.Add(pair.Key);
                }
            }

            for (int i = 0; i < buildersToRelease.Count; i++)
            {
                constructionPickupReservations.Remove(buildersToRelease[i]);
            }
        }

        private static int TakeReservedConstruction(
            object owner,
            Dictionary<object, int> reservations,
            ref int stored,
            int maxAmount)
        {
            if (!reservations.TryGetValue(owner, out int reserved) || reserved <= 0 || stored <= 0)
            {
                return 0;
            }

            int amount = Mathf.Min(maxAmount, reserved, stored);
            stored -= amount;
            reserved -= amount;
            if (reserved <= 0)
            {
                reservations.Remove(owner);
            }
            else
            {
                reservations[owner] = reserved;
            }

            return amount;
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

        private static StrategyStorageYard[] GetYardsSortedByDistance(Vector3 nearWorld)
        {
            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            System.Array.Sort(
                yards,
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
            return yards;
        }

        public string GetHudStatusText()
        {
            int sourceCount = CountAvailableSources();
            return "Storekeepers: "
                + workers.Count
                + "/\u221e"
                + "\n"
                + "Builders: "
                + builders.Count
                + "/\u221e"
                + "\n"
                + "Logs: "
                + logsStored
                + "\n"
                + "Stone: "
                + stoneStored
                + "\n"
                + "Sources: "
                + sourceCount;
        }

        private int CountAvailableSources()
        {
            int count = 0;
            StrategyLumberjackCamp[] camps = Object.FindObjectsByType<StrategyLumberjackCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyLumberjackCamp camp = camps[i];
                if (camp != null && camp.AvailableLogs > 0)
                {
                    count++;
                }
            }

            StrategyStonecutterCamp[] stoneCamps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            for (int i = 0; i < stoneCamps.Length; i++)
            {
                StrategyStonecutterCamp camp = stoneCamps[i];
                if (camp != null && camp.AvailableStone > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetAvailableBuilder(out StrategyResidentAgent builder)
        {
            builder = null;
            for (int i = 0; i < builders.Count; i++)
            {
                StrategyResidentAgent candidate = builders[i];
                if (candidate != null
                    && candidate.CanAcceptWorkAssignment
                    && candidate.BuilderWorkplace == this
                    && !candidate.HasConstructionAssignment)
                {
                    builder = candidate;
                    return true;
                }
            }

            return false;
        }

        private void TryDispatchBuilder(StrategyResidentAgent builder)
        {
            if (builder == null || builder.HasConstructionAssignment || !builder.CanAcceptWorkAssignment)
            {
                return;
            }

            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            System.Array.Sort(
                sites,
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

                    float leftDistance = (left.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                    float rightDistance = (right.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });

            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted || site.BuilderCount >= StrategyConstructionSite.MaxBuilders)
                {
                    continue;
                }

                if (site.RegisterBuilder(builder, false))
                {
                    builder.AssignConstructionSite(site, false);
                    StrategyDebugLogger.Info(
                        "Construction",
                        "BuilderDispatched",
                        StrategyDebugLogger.F("tool", site.Tool),
                        StrategyDebugLogger.F("origin", site.Origin),
                        StrategyDebugLogger.F("builder", builder.FullName));
                    return;
                }
            }
        }

        private void EnsureStockRenderer()
        {
            if (logsStockRenderer != null && stoneStockRenderer != null)
            {
                return;
            }

            if (logsStockRenderer == null)
            {
                GameObject stockObject = new GameObject("Storage Logs Stock");
                stockObject.transform.SetParent(transform, false);
                logsStockRenderer = stockObject.AddComponent<SpriteRenderer>();
                logsStockRenderer.color = Color.white;
            }

            if (stoneStockRenderer == null)
            {
                GameObject stoneObject = new GameObject("Storage Stone Stock");
                stoneObject.transform.SetParent(transform, false);
                stoneStockRenderer = stoneObject.AddComponent<SpriteRenderer>();
                stoneStockRenderer.color = Color.white;
            }

            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (logsStockRenderer != null)
            {
                logsStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardStockSprite(logsStored);
                logsStockRenderer.gameObject.SetActive(logsStored > 0 && logsStockRenderer.sprite != null);
            }

            if (stoneStockRenderer != null)
            {
                stoneStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardStoneStockSprite(stoneStored);
                stoneStockRenderer.gameObject.SetActive(stoneStored > 0 && stoneStockRenderer.sprite != null);
            }

            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            if (logsStockRenderer != null)
            {
                Vector3 logsWorld = new Vector3(bounds.center.x + 0.28f, bounds.min.y + 0.45f, -0.16f);
                logsStockRenderer.transform.localPosition = transform.InverseTransformPoint(logsWorld);
                logsStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(logsStockRenderer, logsWorld, 1);
            }

            if (stoneStockRenderer != null)
            {
                Vector3 stoneWorld = new Vector3(bounds.center.x - 0.86f, bounds.min.y + 0.37f, -0.155f);
                stoneStockRenderer.transform.localPosition = transform.InverseTransformPoint(stoneWorld);
                stoneStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(stoneStockRenderer, stoneWorld, 1);
            }
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearStorageWorkplace(this);
                }
            }

            workers.Clear();

            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.ClearBuilderWorkplace(this);
                }
            }

            builders.Clear();
        }
    }
}
