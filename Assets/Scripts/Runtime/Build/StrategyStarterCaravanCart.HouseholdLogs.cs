using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStarterCaravanCart
    {
        private const int HouseholdLogPickupStackLimit = StrategyProductionStorage.HaulerCarryLimit;

        public static int GetTotalAvailableHouseholdLogs()
        {
            int total = 0;
            List<StrategyStarterCaravanCart> carts = GetActiveCarts();
            for (int i = 0; i < carts.Count; i++)
            {
                total += carts[i] != null ? carts[i].AvailableConstructionLogs : 0;
            }

            return total;
        }

        public static bool TryReserveNearestHouseholdLogs(
            StrategyPlacedBuilding targetHouse,
            object owner,
            out StrategyStarterCaravanCart cart,
            out int amount,
            out Vector2Int pickupCell)
        {
            cart = null;
            amount = 0;
            pickupCell = default;
            if (targetHouse == null || owner == null)
            {
                return false;
            }

            int demand = StrategyHouseWarmthState.GetWinterLogDemandForHouse(targetHouse);
            if (demand <= 0)
            {
                return false;
            }

            List<StrategyStarterCaravanCart> carts = GetCartsSortedByDistance(targetHouse.FootprintBounds.center);
            for (int i = 0; i < carts.Count; i++)
            {
                StrategyStarterCaravanCart candidate = carts[i];
                if (candidate == null
                    || candidate.AvailableConstructionLogs <= 0
                    || !candidate.TryFindDropoffCell(out Vector2Int candidatePickupCell)
                    || !candidate.TryReserveHouseholdLogs(owner, demand, out int reservedAmount))
                {
                    continue;
                }

                cart = candidate;
                amount = reservedAmount;
                pickupCell = candidatePickupCell;
                return true;
            }

            return false;
        }

        public bool TryTakeReservedHouseholdLogs(object owner, out int amount)
        {
            amount = 0;
            if (owner == null
                || !householdLogReservations.TryGetValue(owner, out int reserved)
                || reserved <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reserved, logsStored);
            householdLogReservations.Remove(owner);
            if (amount <= 0)
            {
                TryTransferConstructionResourcesToNearestStorageYard();
                TryDespawnIfEmpty();
                return false;
            }

            logsStored = Mathf.Max(0, logsStored - amount);
            StrategyDebugLogger.Info(
                "StarterCaravan",
                "HouseholdLogsTaken",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("logsRemaining", logsStored));
            TryTransferConstructionResourcesToNearestStorageYard();
            TryDespawnIfEmpty();
            return true;
        }

        public void ReleaseHouseholdLogsReservation(object owner)
        {
            if (owner != null && householdLogReservations.Remove(owner))
            {
                TryTransferConstructionResourcesToNearestStorageYard();
                TryDespawnIfEmpty();
            }
        }

        private bool TryReserveHouseholdLogs(object owner, int requestedDemand, out int amount)
        {
            amount = 0;
            if (owner == null || requestedDemand <= 0)
            {
                return false;
            }

            if (householdLogReservations.TryGetValue(owner, out int existing) && existing > 0)
            {
                amount = existing;
                return true;
            }

            amount = Mathf.Min(AvailableConstructionLogs, requestedDemand, HouseholdLogPickupStackLimit);
            if (amount <= 0)
            {
                return false;
            }

            householdLogReservations[owner] = amount;
            StrategyDebugLogger.Info(
                "StarterCaravan",
                "HouseholdLogsReserved",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }
    }
}
