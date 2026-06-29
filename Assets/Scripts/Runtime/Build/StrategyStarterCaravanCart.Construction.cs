using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStarterCaravanCart
    {
        public static StrategyConstructionResourceCost GetTotalConstructionResources()
        {
            int logs = 0;
            int stone = 0;
            int planks = 0;
            List<StrategyStarterCaravanCart> carts = GetActiveCarts();
            for (int i = 0; i < carts.Count; i++)
            {
                StrategyStarterCaravanCart cart = carts[i];
                if (cart == null)
                {
                    continue;
                }

                logs += cart.AvailableConstructionLogs;
                stone += cart.AvailableConstructionStone;
                planks += cart.AvailableConstructionPlanks;
            }

            return new StrategyConstructionResourceCost(logs, stone, planks);
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
            List<StrategyStarterCaravanCart> carts = GetCartsSortedByDistance(nearWorld);
            for (int i = 0; i < carts.Count && remaining > 0; i++)
            {
                StrategyStarterCaravanCart cart = carts[i];
                if (cart == null)
                {
                    continue;
                }

                remaining -= cart.ReserveConstruction(owner, kind, remaining);
            }

            return requested - remaining;
        }

        public static int SpendAvailableResources(
            StrategyConstructionResourceKind kind,
            int requested,
            Vector3 nearWorld)
        {
            if (requested <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return 0;
            }

            int remaining = requested;
            List<StrategyStarterCaravanCart> carts = GetCartsSortedByDistance(nearWorld);
            for (int i = 0; i < carts.Count && remaining > 0; i++)
            {
                remaining -= carts[i] != null ? carts[i].SpendAvailable(kind, remaining) : 0;
            }

            return requested - remaining;
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            List<StrategyStarterCaravanCart> carts = GetActiveCarts();
            for (int i = 0; i < carts.Count; i++)
            {
                carts[i]?.ReleaseConstructionReservation(owner);
            }
        }

        public static bool TryFindConstructionPickup(
            object owner,
            StrategyConstructionResourceKind kind,
            Vector3 nearWorld,
            int maxAmount,
            out StrategyStarterCaravanCart cart,
            out Vector2Int pickupCell,
            out int amount)
        {
            cart = null;
            pickupCell = default;
            amount = 0;
            if (owner == null || kind == StrategyConstructionResourceKind.None || maxAmount <= 0)
            {
                return false;
            }

            List<StrategyStarterCaravanCart> carts = GetCartsSortedByDistance(nearWorld);
            for (int i = 0; i < carts.Count; i++)
            {
                StrategyStarterCaravanCart candidate = carts[i];
                int available = candidate != null ? candidate.GetAvailableReservationAmount(owner, kind) : 0;
                if (available <= 0 || !candidate.TryFindDropoffCell(out pickupCell))
                {
                    continue;
                }

                cart = candidate;
                amount = Mathf.Min(maxAmount, available);
                return true;
            }

            return false;
        }

        public static int TransferConstructionResourcesToStorageYard(StrategyStorageYard yard)
        {
            if (yard == null)
            {
                return 0;
            }

            int total = 0;
            List<StrategyStarterCaravanCart> carts = GetCartsSortedByDistance(yard.FootprintBounds.center);
            for (int i = 0; i < carts.Count; i++)
            {
                total += carts[i] != null ? carts[i].TransferAvailableConstructionResourcesTo(yard) : 0;
            }

            return total;
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

            constructionPickupReservations[builder] = new ConstructionPickupReservation
            {
                Owner = owner,
                Kind = kind,
                Amount = amount
            };
            return true;
        }

        public void ReleaseConstructionPickupReservation(StrategyResidentAgent builder)
        {
            if (builder != null && constructionPickupReservations.Remove(builder))
            {
                TryTransferConstructionResourcesToNearestStorageYard();
                TryDespawnIfEmpty();
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
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
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
                constructionPickupReservations.Remove(builder);
            }

            TryTransferConstructionResourcesToNearestStorageYard();
            TryDespawnIfEmpty();
            return amount > 0;
        }

        private int ReserveConstruction(object owner, StrategyConstructionResourceKind kind, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), GetAvailableConstruction(kind));
            if (amount <= 0)
            {
                return 0;
            }

            AddReservation(GetConstructionReservations(kind), owner, amount);
            return amount;
        }

        private int SpendAvailable(StrategyConstructionResourceKind kind, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), GetAvailableConstruction(kind));
            if (amount <= 0)
            {
                return 0;
            }

            RemoveConstructionStock(kind, amount);
            TryTransferConstructionResourcesToNearestStorageYard();
            TryDespawnIfEmpty();
            return amount;
        }

        private int TransferAvailableConstructionResourcesTo(StrategyStorageYard yard)
        {
            if (yard == null)
            {
                return 0;
            }

            int logs = AvailableConstructionLogs;
            int stone = AvailableConstructionStone;
            int planks = AvailableConstructionPlanks;
            logsStored -= logs;
            stoneStored -= stone;
            planksStored -= planks;
            yard.AddResource(StrategyResourceType.Logs, logs);
            yard.AddResource(StrategyResourceType.Stone, stone);
            yard.AddResource(StrategyResourceType.Planks, planks);
            int total = logs + stone + planks;
            if (total > 0)
            {
                StrategyDebugLogger.Info(
                    "StarterCaravan",
                    "ConstructionResourcesTransferredToStorage",
                    StrategyDebugLogger.F("cartOrigin", Origin),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("logs", logs),
                    StrategyDebugLogger.F("stone", stone),
                    StrategyDebugLogger.F("planks", planks));
            }

            TryDespawnIfEmpty();
            return total;
        }

        private void TryTransferConstructionResourcesToNearestStorageYard()
        {
            if (StrategyStorageYard.TryFindNearestStorageYard(FootprintBounds.center, out StrategyStorageYard yard))
            {
                TransferAvailableConstructionResourcesTo(yard);
            }
        }

        private int TakeReservedConstruction(object owner, StrategyConstructionResourceKind kind, int maxAmount)
        {
            Dictionary<object, int> reservations = GetConstructionReservations(kind);
            if (reservations == null
                || !reservations.TryGetValue(owner, out int reserved)
                || reserved <= 0
                || maxAmount <= 0)
            {
                return 0;
            }

            int amount = Mathf.Min(maxAmount, reserved, GetStoredConstruction(kind));
            if (amount <= 0)
            {
                return 0;
            }

            RemoveConstructionStock(kind, amount);
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

        private void ReleaseConstructionReservation(object owner)
        {
            constructionLogReservations.Remove(owner);
            constructionStoneReservations.Remove(owner);
            constructionPlankReservations.Remove(owner);
            ReleaseConstructionPickupReservations(owner);
            TryTransferConstructionResourcesToNearestStorageYard();
            TryDespawnIfEmpty();
        }

        private void ReleaseConstructionPickupReservations(object owner)
        {
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

        private int GetAvailableReservationAmount(object owner, StrategyConstructionResourceKind kind)
        {
            Dictionary<object, int> reservations = GetConstructionReservations(kind);
            if (reservations == null || !reservations.TryGetValue(owner, out int amount) || amount <= 0)
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
                if (pair.Key != null
                    && pair.Value != null
                    && ReferenceEquals(pair.Value.Owner, owner)
                    && pair.Value.Kind == kind
                    && pair.Value.Amount > 0)
                {
                    total += pair.Value.Amount;
                }
            }

            return total;
        }

        private int GetAvailableConstruction(StrategyConstructionResourceKind kind)
        {
            return kind switch
            {
                StrategyConstructionResourceKind.Logs => AvailableConstructionLogs,
                StrategyConstructionResourceKind.Stone => AvailableConstructionStone,
                StrategyConstructionResourceKind.Planks => AvailableConstructionPlanks,
                _ => 0
            };
        }

        private int GetStoredConstruction(StrategyConstructionResourceKind kind)
        {
            return kind switch
            {
                StrategyConstructionResourceKind.Logs => logsStored,
                StrategyConstructionResourceKind.Stone => stoneStored,
                StrategyConstructionResourceKind.Planks => planksStored,
                _ => 0
            };
        }

        private Dictionary<object, int> GetConstructionReservations(StrategyConstructionResourceKind kind)
        {
            return kind switch
            {
                StrategyConstructionResourceKind.Logs => constructionLogReservations,
                StrategyConstructionResourceKind.Stone => constructionStoneReservations,
                StrategyConstructionResourceKind.Planks => constructionPlankReservations,
                _ => null
            };
        }

        private void RemoveConstructionStock(StrategyConstructionResourceKind kind, int amount)
        {
            if (kind == StrategyConstructionResourceKind.Logs)
            {
                logsStored = Mathf.Max(0, logsStored - amount);
            }
            else if (kind == StrategyConstructionResourceKind.Stone)
            {
                stoneStored = Mathf.Max(0, stoneStored - amount);
            }
            else if (kind == StrategyConstructionResourceKind.Planks)
            {
                planksStored = Mathf.Max(0, planksStored - amount);
            }
        }

        private static void AddReservation(Dictionary<object, int> reservations, object owner, int amount)
        {
            if (reservations == null || owner == null || amount <= 0)
            {
                return;
            }

            if (reservations.TryGetValue(owner, out int current))
            {
                reservations[owner] = current + amount;
            }
            else
            {
                reservations.Add(owner, amount);
            }
        }
    }
}
