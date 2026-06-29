using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyLooseConstructionResourcePile
    {
        public bool TryRestoreConstructionReservation(
            object owner,
            StrategyConstructionResourceKind kind,
            int amount)
        {
            if (owner == null || amount <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return false;
            }

            Dictionary<object, int> reservations = GetReservations(kind);
            int available = GetAvailable(kind);
            if (reservations == null)
            {
                return false;
            }

            int reservedAmount = Mathf.Min(amount, available);
            if (reservedAmount <= 0)
            {
                return false;
            }

            ReleaseStorageReservations(kind);
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
            Dictionary<object, int> reservations = GetReservations(kind);
            int available = GetAvailable(kind);
            if (reservations == null)
            {
                return 0;
            }

            int amount = Mathf.Min(Mathf.Max(0, requested), available);
            if (amount <= 0)
            {
                return 0;
            }

            ReleaseStorageReservations(kind);
            AddReservation(reservations, owner, amount);
            return amount;
        }

        private bool HasAvailableConstructionReservation(object owner, StrategyConstructionResourceKind kind)
        {
            return GetAvailableReservationAmount(owner, kind) > 0;
        }

        private int GetAvailableReservationAmount(object owner, StrategyConstructionResourceKind kind)
        {
            Dictionary<object, int> reservations = GetReservations(kind);
            if (reservations == null)
            {
                return 0;
            }

            if (!reservations.TryGetValue(owner, out int reserved) || reserved <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, reserved - CountPickupReservations(owner, kind));
        }
    }
}
