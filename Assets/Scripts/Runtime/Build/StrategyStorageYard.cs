using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyStorageYard : MonoBehaviour, IStrategyConstructionResourceSource
    {
        private readonly List<StrategyResidentAgent> workers = new();
        private readonly List<StrategyResidentAgent> builders = new();
        private StrategyPlacedBuilding building;
        private CityMapController map;
        private StrategyPopulationController population;
        private SpriteRenderer logsStockRenderer;
        private SpriteRenderer stoneStockRenderer;
        private SpriteRenderer ironStockRenderer;
        private SpriteRenderer coalStockRenderer;
        private readonly Dictionary<object, int> constructionLogReservations = new();
        private readonly Dictionary<object, int> constructionStoneReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();
        private int logsStored;
        private int stoneStored;
        private int ironStored;
        private int coalStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public IReadOnlyList<StrategyResidentAgent> Builders => builders;
        public int WorkerCount => workers.Count;
        public int BuilderCount => builders.Count;
        public int LogsStored => logsStored;
        public int StoneStored => stoneStored;
        public int IronStored => ironStored;
        public int CoalStored => coalStored;
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
            int productionStone = StrategyStonecutterCamp.GetTotalAvailableConstructionStone();
            return new StrategyConstructionResourceCost(logs + loose.Logs, stone + loose.Stone + productionStone);
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

            if (remainingStone > 0)
            {
                remainingStone -= StrategyStonecutterCamp.SpendAvailableConstructionStone(remainingStone, nearWorld);
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

            if (remainingStone > 0)
            {
                remainingStone -= StrategyStonecutterCamp.ReserveConstructionStone(owner, remainingStone, nearWorld);
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
            StrategyStonecutterCamp.ReleaseConstructionReservations(owner);
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

            if (kind == StrategyConstructionResourceKind.Stone
                && StrategyStonecutterCamp.TryFindConstructionPickup(owner, nearWorld, out StrategyStonecutterCamp camp, out pickupCell))
            {
                source = camp;
                return true;
            }

            if (StrategyLooseConstructionResourcePile.TryReserveNearestAvailableForConstruction(owner, kind, nearWorld, out StrategyLooseConstructionResourcePile availablePile, out pickupCell))
            {
                source = availablePile;
                return true;
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
    }
}
