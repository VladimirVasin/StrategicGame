using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private const int HouseholdLogPickupStackLimit = StrategyProductionStorage.HaulerCarryLimit;

        public static bool TryReserveNearestHouseholdLogs(
            StrategyPlacedBuilding targetHouse,
            object owner,
            out StrategyStorageYard yard,
            out int amount,
            out Vector2Int pickupCell)
        {
            yard = null;
            amount = 0;
            pickupCell = default;
            if (targetHouse == null || owner == null)
            {
                return false;
            }

            List<StrategyStorageYard> yards = GetYardsSortedByDistance(targetHouse.FootprintBounds.center);
            for (int i = 0; i < yards.Count; i++)
            {
                if (yards[i] != null
                    && yards[i].TryReserveHouseholdLogsForHouse(owner, targetHouse, out amount, out pickupCell))
                {
                    yard = yards[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryTakeReservedHouseholdLogs(
            object owner,
            out StrategyPlacedBuilding house,
            out int amount)
        {
            house = null;
            amount = 0;
            if (owner == null
                || !householdLogReservations.TryGetValue(owner, out HouseholdLogReservation reservation)
                || reservation == null
                || reservation.House == null
                || reservation.Amount <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservation.Amount, logsStored);
            house = reservation.House;
            householdLogReservations.Remove(owner);
            if (amount <= 0)
            {
                return false;
            }

            SpendLogisticsAmount(StrategyResourceType.Logs, amount);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholdLogsTaken",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("amount", amount));
            return true;
        }

        public void ReleaseHouseholdLogsReservation(object owner)
        {
            if (owner != null)
            {
                householdLogReservations.Remove(owner);
            }
        }

        public static int CountAvailableHouseholdLogs()
        {
            return GetTotalAvailableLogisticsAmount(StrategyResourceType.Logs);
        }

        public static int CountRawHouseholdLogDemand()
        {
            int demand = 0;
            IReadOnlyList<StrategyPlacedBuilding> buildings = GetActiveBuildings();
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding house = buildings[i];
                if (house != null && house.Tool == StrategyBuildTool.House)
                {
                    demand += StrategyHouseWarmthState.GetWinterLogDemandForHouse(house);
                }
            }

            return demand;
        }

        private bool TryReserveHouseholdLogsForHouse(
            object owner,
            StrategyPlacedBuilding targetHouse,
            out int amount,
            out Vector2Int pickupCell)
        {
            amount = 0;
            pickupCell = default;
            if (owner == null || targetHouse == null || !TryFindDropoffCell(out pickupCell))
            {
                return false;
            }

            if (householdLogReservations.TryGetValue(owner, out HouseholdLogReservation existing)
                && existing != null
                && existing.House == targetHouse
                && existing.Amount > 0)
            {
                amount = existing.Amount;
                return true;
            }

            int available = GetAvailableLogisticsAmount(StrategyResourceType.Logs);
            int demand = GetHouseholdLogDemand(targetHouse);
            amount = Mathf.Min(available, demand, HouseholdLogPickupStackLimit);
            if (amount <= 0)
            {
                return false;
            }

            householdLogReservations[owner] = new HouseholdLogReservation
            {
                House = targetHouse,
                Amount = amount
            };
            StrategyDebugLogger.Info(
                "Household",
                "HouseholdLogsReserved",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("houseOrigin", targetHouse.Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("houseDemand", demand));
            return true;
        }

        private int GetHouseholdLogDemand(StrategyPlacedBuilding house)
        {
            int demand = StrategyHouseWarmthState.GetWinterLogDemandForHouse(house);
            return Mathf.Max(0, demand - CountHouseholdLogReservationsForHouse(house));
        }

        private int CountHouseholdLogReservations()
        {
            int total = 0;
            foreach (HouseholdLogReservation reservation in householdLogReservations.Values)
            {
                total += reservation != null ? Mathf.Max(0, reservation.Amount) : 0;
            }

            return total;
        }

        private int CountHouseholdLogReservationsForHouse(StrategyPlacedBuilding house)
        {
            int total = 0;
            foreach (HouseholdLogReservation reservation in householdLogReservations.Values)
            {
                if (reservation != null && reservation.House == house)
                {
                    total += Mathf.Max(0, reservation.Amount);
                }
            }

            return total;
        }
    }
}
