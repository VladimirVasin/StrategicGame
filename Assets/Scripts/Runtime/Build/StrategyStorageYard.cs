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
        private SpriteRenderer planksStockRenderer;
        private readonly Dictionary<object, int> constructionLogReservations = new();
        private readonly Dictionary<object, int> constructionStoneReservations = new();
        private readonly Dictionary<object, int> constructionPlankReservations = new();
        private readonly Dictionary<StrategyResourceType, Dictionary<object, int>> productionInputReservations = new();
        private readonly Dictionary<StrategyResidentAgent, ConstructionPickupReservation> constructionPickupReservations = new();
        private object logisticsLogsReservationOwner;
        private int reservedLogisticsLogs;
        private int logsStored;
        private int stoneStored;
        private int ironStored;
        private int coalStored;
        private int planksStored;

        public IReadOnlyList<StrategyResidentAgent> Workers => workers;
        public IReadOnlyList<StrategyResidentAgent> Builders => builders;
        public int WorkerCount => workers.Count;
        public int BuilderCount => builders.Count;
        public int LogsStored => logsStored;
        public int StoneStored => stoneStored;
        public int IronStored => ironStored;
        public int CoalStored => coalStored;
        public int PlanksStored => planksStored;
        public int AvailableConstructionLogs => Mathf.Max(0, logsStored - CountReservations(constructionLogReservations) - reservedLogisticsLogs - CountProductionInputReservations(StrategyResourceType.Logs));
        public int AvailableLogisticsLogs => Mathf.Max(0, logsStored - CountReservations(constructionLogReservations) - reservedLogisticsLogs - CountProductionInputReservations(StrategyResourceType.Logs));
        public int AvailableConstructionStone => Mathf.Max(0, stoneStored - CountReservations(constructionStoneReservations) - CountProductionInputReservations(StrategyResourceType.Stone));
        public int AvailableConstructionPlanks => Mathf.Max(0, planksStored - CountReservations(constructionPlankReservations) - CountProductionInputReservations(StrategyResourceType.Planks));
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
            return TryAssignBuildersAcrossSites(site);
        }

        public static StrategyConstructionResourceCost GetTotalConstructionResources()
        {
            int logs = 0;
            int stone = 0;
            int planks = 0;
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
                planks += yard.AvailableConstructionPlanks;
            }

            StrategyConstructionResourceCost loose = StrategyLooseConstructionResourcePile.GetTotalAvailableResources();
            return new StrategyConstructionResourceCost(
                logs + loose.Logs,
                stone + loose.Stone,
                planks + loose.Planks);
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
                    StrategyDebugLogger.F("costPlanks", cost.Planks),
                    StrategyDebugLogger.F("availableLogs", available.Logs),
                    StrategyDebugLogger.F("availableStone", available.Stone),
                    StrategyDebugLogger.F("availablePlanks", available.Planks));
                return false;
            }

            StrategyStorageYard[] yards = GetYardsSortedByDistance(nearWorld);
            int remainingLogs = cost.Logs;
            remainingLogs -= StrategyLooseConstructionResourcePile.SpendAvailableResources(
                StrategyConstructionResourceKind.Logs,
                remainingLogs,
                nearWorld);
            for (int i = 0; i < yards.Length && remainingLogs > 0; i++)
            {
                remainingLogs -= yards[i].SpendAvailableLogs(remainingLogs);
            }

            int remainingStone = cost.Stone;
            remainingStone -= StrategyLooseConstructionResourcePile.SpendAvailableResources(
                StrategyConstructionResourceKind.Stone,
                remainingStone,
                nearWorld);
            for (int i = 0; i < yards.Length && remainingStone > 0; i++)
            {
                remainingStone -= yards[i].SpendAvailableStone(remainingStone);
            }

            int remainingPlanks = cost.Planks;
            remainingPlanks -= StrategyLooseConstructionResourcePile.SpendAvailableResources(
                StrategyConstructionResourceKind.Planks,
                remainingPlanks,
                nearWorld);
            for (int i = 0; i < yards.Length && remainingPlanks > 0; i++)
            {
                remainingPlanks -= yards[i].SpendAvailablePlanks(remainingPlanks);
            }

            if (remainingLogs > 0 || remainingStone > 0 || remainingPlanks > 0)
            {
                StrategyDebugLogger.Warn(
                    "StorageYard",
                    "ConstructionSpendShort",
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("remainingLogs", remainingLogs),
                    StrategyDebugLogger.F("remainingStone", remainingStone),
                    StrategyDebugLogger.F("remainingPlanks", remainingPlanks));
                return false;
            }

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionResourcesSpent",
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("logs", cost.Logs),
                StrategyDebugLogger.F("stone", cost.Stone),
                StrategyDebugLogger.F("planks", cost.Planks));
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

            int remainingPlanks = cost.Planks;
            remainingPlanks -= StrategyLooseConstructionResourcePile.ReserveConstructionResources(
                owner,
                StrategyConstructionResourceKind.Planks,
                remainingPlanks,
                nearWorld);
            for (int i = 0; i < yards.Length && remainingPlanks > 0; i++)
            {
                int reserved = yards[i].ReserveConstructionPlanks(owner, remainingPlanks);
                remainingPlanks -= reserved;
            }

            if (remainingLogs > 0 || remainingStone > 0 || remainingPlanks > 0)
            {
                ReleaseConstructionReservations(owner);
                return false;
            }

            StrategyDebugLogger.Info(
                "StorageYard",
                "ConstructionReserved",
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("logs", cost.Logs),
                StrategyDebugLogger.F("stone", cost.Stone),
                StrategyDebugLogger.F("planks", cost.Planks));
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

    }
}
