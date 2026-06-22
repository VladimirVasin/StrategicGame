using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private const int HouseholdPotteryPickupStackLimit = StrategyProductionStorage.HaulerCarryLimit;

        public static bool TryReserveNearestHouseholdPottery(
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

            StrategyStorageYard[] yards = GetYardsSortedByDistance(targetHouse.FootprintBounds.center);
            int requestedDemand = targetHouse.Resources != null
                ? targetHouse.Resources.GetPotteryDemandForCooking(CalculateHouseDailyRationNeed(targetHouse))
                : 0;
            for (int i = 0; i < yards.Length; i++)
            {
                if (yards[i] != null
                    && yards[i].TryReserveHouseholdPotteryForHouse(owner, targetHouse, out amount, out pickupCell))
                {
                    yard = yards[i];
                    return true;
                }
            }

            LogHouseholdPotteryReserveFailed(targetHouse, yards, requestedDemand);
            return false;
        }

        private bool TryReserveHouseholdPotteryForHouse(
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

            if (householdPotteryReservations.TryGetValue(owner, out HouseholdPotteryReservation existing)
                && existing.House == targetHouse
                && existing.Amount > 0)
            {
                amount = existing.Amount;
                return true;
            }

            int available = GetAvailableLogisticsAmount(StrategyResourceType.Pottery);
            if (available <= 0)
            {
                return false;
            }

            int demand = GetHouseholdPotteryDemand(targetHouse);
            if (demand <= 0)
            {
                return false;
            }

            amount = Mathf.Min(available, demand, HouseholdPotteryPickupStackLimit);
            if (amount <= 0)
            {
                return false;
            }

            householdPotteryReservations[owner] = new HouseholdPotteryReservation
            {
                House = targetHouse,
                Amount = amount
            };
            StrategyDebugLogger.Info(
                "Household",
                "HouseholdPotteryReserved",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("houseOrigin", targetHouse.Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("houseDemand", demand));
            return true;
        }

        public bool TryTakeReservedHouseholdPottery(
            object owner,
            out StrategyPlacedBuilding house,
            out int amount)
        {
            house = null;
            amount = 0;
            if (owner == null
                || !householdPotteryReservations.TryGetValue(owner, out HouseholdPotteryReservation reservation)
                || reservation.House == null
                || reservation.Amount <= 0)
            {
                return false;
            }

            amount = Mathf.Min(reservation.Amount, potteryStored);
            house = reservation.House;
            householdPotteryReservations.Remove(owner);
            if (amount <= 0)
            {
                return false;
            }

            SpendLogisticsAmount(StrategyResourceType.Pottery, amount);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholdPotteryTaken",
                StrategyDebugLogger.F("yardOrigin", Origin),
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("amount", amount));
            return true;
        }

        public void ReleaseHouseholdPotteryReservation(object owner)
        {
            if (owner != null)
            {
                householdPotteryReservations.Remove(owner);
            }
        }

        public static int CountAvailableHouseholdPottery()
        {
            int available = 0;
            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            for (int i = 0; i < yards.Length; i++)
            {
                available += yards[i] != null ? yards[i].GetAvailableLogisticsAmount(StrategyResourceType.Pottery) : 0;
            }

            return available;
        }

        public static int CountHouseholdPotteryDemand(out Vector3 focus)
        {
            focus = Vector3.zero;
            int available = 0;
            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            for (int i = 0; i < yards.Length; i++)
            {
                available += yards[i] != null ? yards[i].GetAvailableLogisticsAmount(StrategyResourceType.Pottery) : 0;
            }

            if (available <= 0)
            {
                return 0;
            }

            int demand = 0;
            Vector3 weighted = Vector3.zero;
            StrategyPlacedBuilding[] buildings = Object.FindObjectsByType<StrategyPlacedBuilding>();
            for (int i = 0; i < buildings.Length; i++)
            {
                StrategyPlacedBuilding house = buildings[i];
                if (house == null || house.Tool != StrategyBuildTool.House || house.Resources == null)
                {
                    continue;
                }

                int houseDemand = house.Resources.GetPotteryDemandForCooking(CalculateHouseDailyRationNeed(house));
                if (houseDemand <= 0)
                {
                    continue;
                }

                demand += houseDemand;
                weighted += house.FootprintBounds.center * houseDemand;
            }

            if (demand > 0)
            {
                focus = weighted / demand;
            }

            return Mathf.Min(demand, available);
        }

        public static int CountRawHouseholdPotteryDemand()
        {
            int demand = 0;
            StrategyPlacedBuilding[] buildings = Object.FindObjectsByType<StrategyPlacedBuilding>();
            for (int i = 0; i < buildings.Length; i++)
            {
                StrategyPlacedBuilding house = buildings[i];
                if (house != null && house.Tool == StrategyBuildTool.House && house.Resources != null)
                {
                    demand += house.Resources.GetPotteryDemandForCooking(CalculateHouseDailyRationNeed(house));
                }
            }

            return demand;
        }

        private int CountHouseholdPotteryReservations()
        {
            int total = 0;
            foreach (HouseholdPotteryReservation reservation in householdPotteryReservations.Values)
            {
                total += reservation != null ? Mathf.Max(0, reservation.Amount) : 0;
            }

            return total;
        }

        private int GetHouseholdPotteryDemand(StrategyPlacedBuilding house)
        {
            if (house == null || house.Tool != StrategyBuildTool.House || house.Resources == null)
            {
                return 0;
            }

            float dailyNeed = CalculateHouseDailyRationNeed(house);
            int demand = house.Resources.GetPotteryDemandForCooking(dailyNeed);
            return Mathf.Max(0, demand - CountHouseholdPotteryReservationsForHouse(house));
        }

        private int CountHouseholdPotteryReservationsForHouse(StrategyPlacedBuilding house)
        {
            int total = 0;
            foreach (HouseholdPotteryReservation reservation in householdPotteryReservations.Values)
            {
                if (reservation != null && reservation.House == house)
                {
                    total += Mathf.Max(0, reservation.Amount);
                }
            }

            return total;
        }

        private static void LogHouseholdPotteryReserveFailed(
            StrategyPlacedBuilding house,
            StrategyStorageYard[] yards,
            int requestedDemand)
        {
            int yardsChecked = 0;
            int storagePottery = 0;
            int available = 0;
            int reserved = 0;
            bool anyDropoff = false;
            if (yards != null)
            {
                for (int i = 0; i < yards.Length; i++)
                {
                    StrategyStorageYard yard = yards[i];
                    if (yard == null)
                    {
                        continue;
                    }

                    yardsChecked++;
                    storagePottery += yard.potteryStored;
                    available += yard.GetAvailableLogisticsAmount(StrategyResourceType.Pottery);
                    reserved += yard.CountHouseholdPotteryReservations();
                    if (!anyDropoff && yard.TryFindDropoffCell(out _))
                    {
                        anyDropoff = true;
                    }
                }
            }

            int housePottery = house != null && house.Resources != null ? house.Resources.GetPotteryAmount() : 0;
            string reason = DetermineHouseholdPotteryReserveFailureReason(
                yardsChecked,
                storagePottery,
                available,
                requestedDemand,
                anyDropoff);
            StrategyDebugLogger.Warn(
                "Household",
                "HouseholdPotteryReserveFailed",
                StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("requestedDemand", requestedDemand),
                StrategyDebugLogger.F("housePottery", housePottery),
                StrategyDebugLogger.F("storagePottery", storagePottery),
                StrategyDebugLogger.F("storageAvailable", available),
                StrategyDebugLogger.F("reserved", reserved),
                StrategyDebugLogger.F("yardsChecked", yardsChecked),
                StrategyDebugLogger.F("anyDropoff", anyDropoff));
        }

        private static string DetermineHouseholdPotteryReserveFailureReason(
            int yardsChecked,
            int storagePottery,
            int available,
            int requestedDemand,
            bool anyDropoff)
        {
            if (yardsChecked <= 0)
            {
                return "no_storage_yards";
            }

            if (!anyDropoff)
            {
                return "no_yard_dropoff";
            }

            if (requestedDemand <= 0)
            {
                return "no_house_demand";
            }

            if (storagePottery <= 0)
            {
                return "no_storage_pottery";
            }

            if (available <= 0)
            {
                return "pottery_reserved_or_committed";
            }

            return "no_reservable_yard";
        }

        private static float CalculateHouseDailyRationNeed(StrategyPlacedBuilding house)
        {
            float total = 0f;
            if (house == null)
            {
                return total;
            }

            for (int i = 0; i < house.Residents.Count; i++)
            {
                StrategyResidentAgent resident = house.Residents[i];
                if (resident != null && resident.Home == house && !resident.IsPendingRefugee)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }
    }
}
