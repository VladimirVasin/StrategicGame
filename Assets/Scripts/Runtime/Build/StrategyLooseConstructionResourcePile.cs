using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyLooseConstructionResourcePile : MonoBehaviour, IStrategyConstructionResourceSource, IStrategyWorldInspectable
    {
        private static Transform root;

        private readonly Dictionary<object, int> logReservations = new();
        private readonly Dictionary<object, int> stoneReservations = new();
        private readonly Dictionary<StrategyResidentAgent, PickupReservation> pickupReservations = new();
        private CityMapController map;
        private SpriteRenderer logsRenderer;
        private SpriteRenderer stoneRenderer;
        private Vector2Int origin;
        private Bounds footprintBounds;
        private int logs;
        private int stone;

        public int Logs => logs;
        public int Stone => stone;
        public int AvailableLogs => Mathf.Max(0, logs - CountReservations(logReservations) - CountPickupReservations(StrategyConstructionResourceKind.Logs));
        public int AvailableStone => Mathf.Max(0, stone - CountReservations(stoneReservations) - CountPickupReservations(StrategyConstructionResourceKind.Stone));
        public Vector2Int Origin => origin;
        public Bounds FootprintBounds => footprintBounds;

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            string body = "Logs: "
                + logs
                + " (available "
                + AvailableLogs
                + ")\nStone: "
                + stone
                + " (available "
                + AvailableStone
                + ")";
            info = new StrategyWorldInspectInfo(
                "Loose Building Materials",
                "Construction resource pile",
                body,
                logs > 0 && logsRenderer != null ? logsRenderer.sprite : stoneRenderer != null ? stoneRenderer.sprite : null,
                origin,
                true);
            return true;
        }

        private sealed class PickupReservation
        {
            public object Owner;
            public StrategyConstructionResourceKind Kind;
            public int Amount;
        }

        public static StrategyLooseConstructionResourcePile Create(
            CityMapController map,
            Vector2Int origin,
            Vector3 world,
            int logs,
            int stone)
        {
            if (logs <= 0 && stone <= 0)
            {
                return null;
            }

            EnsureRoot();
            GameObject obj = new GameObject("Loose Construction Resources");
            obj.transform.SetParent(root, false);
            StrategyLooseConstructionResourcePile pile = obj.AddComponent<StrategyLooseConstructionResourcePile>();
            pile.Configure(map, origin, world, logs, stone);
            return pile;
        }

        public static StrategyConstructionResourceCost GetTotalAvailableResources()
        {
            int totalLogs = 0;
            int totalStone = 0;
            StrategyLooseConstructionResourcePile[] piles = Object.FindObjectsByType<StrategyLooseConstructionResourcePile>();
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseConstructionResourcePile pile = piles[i];
                if (pile == null)
                {
                    continue;
                }

                totalLogs += pile.AvailableLogs;
                totalStone += pile.AvailableStone;
            }

            return new StrategyConstructionResourceCost(totalLogs, totalStone);
        }

        public static bool TryReserveNearestForStorage(
            StrategyStorageYard yard,
            StrategyResidentAgent worker,
            StrategyConstructionResourceKind kind,
            out StrategyLooseConstructionResourcePile pile)
        {
            pile = null;
            if (yard == null || worker == null || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            StrategyLooseConstructionResourcePile[] piles = GetPilesSortedByDistance(yard.FootprintBounds.center);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseConstructionResourcePile candidate = piles[i];
                if (candidate == null || !candidate.TryReserveForStorage(worker, kind, 1))
                {
                    continue;
                }

                pile = candidate;
                return true;
            }

            return false;
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            StrategyLooseConstructionResourcePile[] piles = Object.FindObjectsByType<StrategyLooseConstructionResourcePile>();
            for (int i = 0; i < piles.Length; i++)
            {
                piles[i]?.ReleaseConstructionReservation(owner);
            }
        }

        public static int ReserveConstructionResources(
            object owner,
            StrategyConstructionResourceKind kind,
            int requested,
            Vector3 nearWorld)
        {
            if (owner == null || requested <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return 0;
            }

            int remaining = requested;
            StrategyLooseConstructionResourcePile[] piles = GetPilesSortedByDistance(nearWorld);
            for (int i = 0; i < piles.Length && remaining > 0; i++)
            {
                StrategyLooseConstructionResourcePile pile = piles[i];
                if (pile == null)
                {
                    continue;
                }

                remaining -= pile.ReserveConstruction(owner, kind, remaining);
            }

            return requested - remaining;
        }

        public static bool TryFindConstructionPickup(
            object owner,
            StrategyConstructionResourceKind kind,
            Vector3 nearWorld,
            out StrategyLooseConstructionResourcePile pile,
            out Vector2Int pickupCell)
        {
            pile = null;
            pickupCell = default;
            if (owner == null || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            StrategyLooseConstructionResourcePile[] piles = GetPilesSortedByDistance(nearWorld);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseConstructionResourcePile candidate = piles[i];
                if (candidate == null || !candidate.HasAvailableConstructionReservation(owner, kind))
                {
                    continue;
                }

                if (candidate.TryFindPickupCell(out pickupCell))
                {
                    pile = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryReserveNearestAvailableForConstruction(
            object owner,
            StrategyConstructionResourceKind kind,
            Vector3 nearWorld,
            out StrategyLooseConstructionResourcePile pile,
            out Vector2Int pickupCell)
        {
            pile = null;
            pickupCell = default;
            if (owner == null || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            StrategyLooseConstructionResourcePile[] piles = GetPilesSortedByDistance(nearWorld);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseConstructionResourcePile candidate = piles[i];
                if (candidate == null)
                {
                    continue;
                }

                int available = kind == StrategyConstructionResourceKind.Logs
                    ? candidate.AvailableLogs
                    : candidate.AvailableStone;
                if (available <= 0 || !candidate.TryFindPickupCell(out pickupCell))
                {
                    continue;
                }

                if (candidate.ReserveConstruction(owner, kind, 1) <= 0)
                {
                    continue;
                }

                pile = candidate;
                StrategyDebugLogger.Info(
                    "Build",
                    "LooseConstructionResourceReservedForConstruction",
                    StrategyDebugLogger.F("origin", candidate.Origin),
                    StrategyDebugLogger.F("owner", owner),
                    StrategyDebugLogger.F("resource", kind),
                    StrategyDebugLogger.F("amount", 1));
                return true;
            }

            return false;
        }

        public void Configure(CityMapController mapController, Vector2Int pileOrigin, Vector3 world, int initialLogs, int initialStone)
        {
            map = mapController;
            origin = pileOrigin;
            logs = Mathf.Max(0, initialLogs);
            stone = Mathf.Max(0, initialStone);
            footprintBounds = map != null && map.TryGetCell(origin.x, origin.y, out _)
                ? map.GetCellRectWorld(origin, Vector2Int.one)
                : new Bounds(world, Vector3.one);
            transform.position = new Vector3(world.x, world.y, -0.12f);
            EnsureRenderers();
            UpdateVisuals();
            StrategyDebugLogger.Info(
                "Build",
                "LooseConstructionResourcesCreated",
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("logs", logs),
                StrategyDebugLogger.F("stone", stone));
        }

        public bool TryFindPickupCell(out Vector2Int cell)
        {
            if (map != null && map.IsCellWalkable(origin))
            {
                cell = origin;
                return true;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map != null && map.IsCellWalkable(candidate))
                        {
                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            cell = default;
            return false;
        }

        public bool TryReserveForStorage(StrategyResidentAgent worker, StrategyConstructionResourceKind kind, int amount)
        {
            if (worker == null || amount <= 0)
            {
                return false;
            }

            ReleaseStorageReservation(worker);
            int available = kind == StrategyConstructionResourceKind.Logs ? AvailableLogs : AvailableStone;
            if (available < amount)
            {
                return false;
            }

            pickupReservations[worker] = new PickupReservation
            {
                Owner = null,
                Kind = kind,
                Amount = amount
            };
            return true;
        }

        public void ReleaseStorageReservation(StrategyResidentAgent worker)
        {
            if (worker != null && pickupReservations.TryGetValue(worker, out PickupReservation reservation) && reservation.Owner == null)
            {
                pickupReservations.Remove(worker);
            }
        }

        public bool TryTakeReservedForStorage(
            StrategyResidentAgent worker,
            StrategyConstructionResourceKind kind,
            out int amount)
        {
            amount = 0;
            if (worker == null
                || !pickupReservations.TryGetValue(worker, out PickupReservation reservation)
                || reservation == null
                || reservation.Owner != null
                || reservation.Kind != kind
                || reservation.Amount <= 0)
            {
                return false;
            }

            amount = TakeStorageReservation(kind, reservation.Amount);
            pickupReservations.Remove(worker);
            UpdateOrDestroy();
            return amount > 0;
        }

        public bool TryReserveConstructionPickup(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null || builder == null || amount <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            ReleaseConstructionPickupReservation(builder);
            if (GetAvailableReservationAmount(owner, kind) < amount)
            {
                return false;
            }

            pickupReservations[builder] = new PickupReservation
            {
                Owner = owner,
                Kind = kind,
                Amount = amount
            };
            return true;
        }

        public void ReleaseConstructionPickupReservation(StrategyResidentAgent builder)
        {
            if (builder != null && pickupReservations.TryGetValue(builder, out PickupReservation reservation) && reservation.Owner != null)
            {
                pickupReservations.Remove(builder);
            }
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
                || !pickupReservations.TryGetValue(builder, out PickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Kind != kind
                || pickup.Amount <= 0)
            {
                return false;
            }

            amount = TakeReservedConstruction(owner, kind, Mathf.Min(maxAmount, pickup.Amount));
            pickup.Amount -= amount;
            if (pickup.Amount <= 0 || amount <= 0)
            {
                pickupReservations.Remove(builder);
            }

            UpdateOrDestroy();
            return amount > 0;
        }

        public bool TryRestoreConstructionReservation(
            object owner,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null || amount <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            Dictionary<object, int> reservations = kind == StrategyConstructionResourceKind.Logs ? logReservations : stoneReservations;
            int available = kind == StrategyConstructionResourceKind.Logs ? AvailableLogs : AvailableStone;
            int reservedAmount = Mathf.Min(amount, available);
            if (reservedAmount <= 0)
            {
                return false;
            }

            AddReservation(reservations, owner, reservedAmount);
            StrategyDebugLogger.Info(
                "Build",
                "LooseConstructionResourceReservationRestored",
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", reservedAmount));
            return true;
        }

        private int ReserveConstruction(object owner, StrategyConstructionResourceKind kind, int requested)
        {
            Dictionary<object, int> reservations = kind == StrategyConstructionResourceKind.Logs ? logReservations : stoneReservations;
            int available = kind == StrategyConstructionResourceKind.Logs ? AvailableLogs : AvailableStone;
            int amount = Mathf.Min(Mathf.Max(0, requested), available);
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(reservations, owner, amount);
            return amount;
        }

        private bool HasAvailableConstructionReservation(object owner, StrategyConstructionResourceKind kind)
        {
            return GetAvailableReservationAmount(owner, kind) > 0;
        }

        private int GetAvailableReservationAmount(object owner, StrategyConstructionResourceKind kind)
        {
            Dictionary<object, int> reservations = kind == StrategyConstructionResourceKind.Logs ? logReservations : stoneReservations;
            if (!reservations.TryGetValue(owner, out int reserved) || reserved <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, reserved - CountPickupReservations(owner, kind));
        }
    }
}
