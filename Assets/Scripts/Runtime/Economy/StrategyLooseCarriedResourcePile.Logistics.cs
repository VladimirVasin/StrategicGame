using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyLooseCarriedResourcePile
    {
        public static bool TryReserveNearestForStorage(
            StrategyStorageYard yard,
            StrategyResidentAgent worker,
            StrategyResourceType requestedResource,
            out StrategyLooseCarriedResourcePile pile,
            out Vector2Int pickupCell)
        {
            pile = null;
            pickupCell = default;
            if (yard == null || worker == null || !IsStorageResource(requestedResource))
            {
                return false;
            }

            StrategyLooseCarriedResourcePile[] piles = GetPilesSortedByDistance(yard.FootprintBounds.center);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseCarriedResourcePile candidate = piles[i];
                if (candidate == null
                    || candidate.resource != requestedResource
                    || candidate.amount <= 0
                    || candidate.reservedBy != null
                    || !candidate.TryFindPickupCell(out pickupCell)
                    || !candidate.TryReserve(worker, StrategyProductionStorage.HaulerCarryLimit))
                {
                    continue;
                }

                pile = candidate;
                StrategyDebugLogger.Info(
                    "Logistics",
                    "LooseResourceReservedForStorage",
                    StrategyDebugLogger.F("origin", candidate.origin),
                    StrategyDebugLogger.F("resource", candidate.resource),
                    StrategyDebugLogger.F("amount", candidate.reservedAmount),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                return true;
            }

            return false;
        }

        public static bool TryReserveNearestHouseholdFood(
            Vector3 requesterWorld,
            StrategyResidentAgent resident,
            out StrategyLooseCarriedResourcePile pile,
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int pickupCell)
        {
            pile = null;
            resource = StrategyResourceType.None;
            amount = 0;
            pickupCell = default;
            if (resident == null)
            {
                return false;
            }

            StrategyLooseCarriedResourcePile[] piles = GetPilesSortedByDistance(requesterWorld);
            for (int i = 0; i < piles.Length; i++)
            {
                StrategyLooseCarriedResourcePile candidate = piles[i];
                if (candidate == null
                    || !StrategyFoodNutrition.IsFood(candidate.resource)
                    || candidate.amount <= 0
                    || candidate.reservedBy != null
                    || !candidate.TryFindPickupCell(out pickupCell)
                    || !candidate.TryReserve(resident, StrategyProductionStorage.HaulerCarryLimit))
                {
                    continue;
                }

                pile = candidate;
                resource = candidate.resource;
                amount = candidate.reservedAmount;
                StrategyDebugLogger.Info(
                    "Household",
                    "LooseFoodReservedForHousehold",
                    StrategyDebugLogger.F("origin", candidate.origin),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("resident", resident.FullName));
                return true;
            }

            return false;
        }

        private static bool IsStorageResource(StrategyResourceType type)
        {
            return type == StrategyResourceType.Iron
                || type == StrategyResourceType.Coal
                || type == StrategyResourceType.Clay
                || type == StrategyResourceType.Pottery
                || type == StrategyResourceType.Tools;
        }
    }
}
