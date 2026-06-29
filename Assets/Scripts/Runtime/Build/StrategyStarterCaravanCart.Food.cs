using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStarterCaravanCart
    {
        private const int MinimumStarterFoodKinds = 3;

        private static readonly StrategyResourceType[] StarterFoodCandidates =
        {
            StrategyResourceType.Fish,
            StrategyResourceType.Game,
            StrategyResourceType.Eggs,
            StrategyResourceType.Berries,
            StrategyResourceType.Roots,
            StrategyResourceType.Mushrooms
        };

        public static float GetTotalAvailableHouseholdRations()
        {
            float total = 0f;
            List<StrategyStarterCaravanCart> carts = GetActiveCarts();
            for (int i = 0; i < carts.Count; i++)
            {
                if (carts[i] != null)
                {
                    total += carts[i].AvailableHouseholdRationValue;
                }
            }

            return total;
        }

        public static bool TryReserveNearestHouseholdFood(
            Vector3 requesterWorld,
            object owner,
            out StrategyStarterCaravanCart cart,
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int pickupCell)
        {
            cart = null;
            resource = StrategyResourceType.None;
            amount = 0;
            pickupCell = default;
            if (owner == null)
            {
                return false;
            }

            List<StrategyStarterCaravanCart> carts = GetCartsSortedByDistance(requesterWorld);
            for (int i = 0; i < carts.Count; i++)
            {
                StrategyStarterCaravanCart candidate = carts[i];
                if (candidate == null
                    || candidate.AvailableHouseholdRationValue <= 0f
                    || !candidate.TryFindDropoffCell(out Vector2Int candidatePickupCell)
                    || !candidate.TryReserveHouseholdFood(owner, out StrategyResourceType reservedResource, out int reservedAmount))
                {
                    continue;
                }

                cart = candidate;
                resource = reservedResource;
                amount = reservedAmount;
                pickupCell = candidatePickupCell;
                return true;
            }

            return false;
        }

        public bool TryReserveHouseholdFood(object owner, out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null)
            {
                return false;
            }

            if (householdFoodReservations.TryGetValue(owner, out HouseholdFoodReservation existing)
                && existing != null
                && existing.Amount > 0)
            {
                resource = existing.Resource;
                amount = existing.Amount;
                return true;
            }

            if (!TryChooseHouseholdFoodResource(out resource))
            {
                return false;
            }

            amount = 1;
            householdFoodReservations[owner] = new HouseholdFoodReservation
            {
                Resource = resource,
                Amount = amount
            };

            StrategyDebugLogger.Info(
                "StarterCaravan",
                "HouseholdFoodReserved",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }

        public bool TryTakeReservedHouseholdFood(object owner, out StrategyResourceType resource, out int amount)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null
                || !householdFoodReservations.TryGetValue(owner, out HouseholdFoodReservation reservation)
                || reservation == null
                || reservation.Amount <= 0
                || reservation.Resource == StrategyResourceType.None)
            {
                return false;
            }

            int stored = GetFoodStock(reservation.Resource);
            amount = Mathf.Min(reservation.Amount, stored);
            if (amount <= 0)
            {
                householdFoodReservations.Remove(owner);
                TryDespawnIfEmpty();
                return false;
            }

            resource = reservation.Resource;
            RemoveFood(resource, amount);
            householdFoodReservations.Remove(owner);
            StrategyDebugLogger.Info(
                "StarterCaravan",
                "HouseholdFoodTaken",
                StrategyDebugLogger.F("origin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("food", GetFoodStockText()));
            TryDespawnIfEmpty();
            return true;
        }

        public void ReleaseHouseholdFoodReservation(object owner)
        {
            if (owner != null && householdFoodReservations.Remove(owner))
            {
                TryDespawnIfEmpty();
            }
        }

        private void AddStarterFoodRations(float targetRations)
        {
            if (targetRations <= 0f)
            {
                return;
            }

            List<StrategyResourceType> foods = new(StarterFoodCandidates);
            ShuffleFoods(foods);
            float stockedRations = 0f;
            int seededKinds = Mathf.Min(MinimumStarterFoodKinds, foods.Count);
            for (int i = 0; i < seededKinds; i++)
            {
                AddFoodUnit(foods[i], 1);
                stockedRations += StrategyFoodNutrition.GetRationValue(foods[i]);
            }

            while (stockedRations + 0.01f < targetRations && foods.Count > 0)
            {
                StrategyResourceType resource = foods[Random.Range(0, foods.Count)];
                AddFoodUnit(resource, 1);
                stockedRations += StrategyFoodNutrition.GetRationValue(resource);
            }
        }

        private static void ShuffleFoods(List<StrategyResourceType> foods)
        {
            for (int i = foods.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                StrategyResourceType current = foods[i];
                foods[i] = foods[swap];
                foods[swap] = current;
            }
        }

        private bool TryChooseHouseholdFoodResource(out StrategyResourceType resource)
        {
            List<StrategyResourceType> available = new();
            for (int i = 0; i < StarterFoodCandidates.Length; i++)
            {
                StrategyResourceType candidate = StarterFoodCandidates[i];
                if (GetAvailableFoodForHouseholds(candidate) > 0)
                {
                    available.Add(candidate);
                }
            }

            if (available.Count <= 0)
            {
                resource = StrategyResourceType.None;
                return false;
            }

            resource = available[Random.Range(0, available.Count)];
            return true;
        }

        private float GetTotalFoodRations(bool availableOnly)
        {
            float total = 0f;
            for (int i = 0; i < StarterFoodCandidates.Length; i++)
            {
                StrategyResourceType resource = StarterFoodCandidates[i];
                int stock = availableOnly ? GetAvailableFoodForHouseholds(resource) : GetFoodStock(resource);
                total += stock * StrategyFoodNutrition.GetRationValue(resource);
            }

            return total;
        }

        private int GetAvailableFoodForHouseholds(StrategyResourceType resource)
        {
            return Mathf.Max(0, GetFoodStock(resource) - GetReservedFoodAmount(resource));
        }

        private int GetReservedFoodAmount(StrategyResourceType resource)
        {
            int total = 0;
            foreach (KeyValuePair<object, HouseholdFoodReservation> pair in householdFoodReservations)
            {
                if (pair.Key != null
                    && pair.Value != null
                    && pair.Value.Resource == resource
                    && pair.Value.Amount > 0)
                {
                    total += pair.Value.Amount;
                }
            }

            return total;
        }

        private int GetFoodStock(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Game => gameStored,
                StrategyResourceType.Fish => fishStored,
                StrategyResourceType.Eggs => eggsStored,
                StrategyResourceType.Berries => berriesStored,
                StrategyResourceType.Roots => rootsStored,
                StrategyResourceType.Mushrooms => mushroomsStored,
                _ => 0
            };
        }

        private void AddFoodUnit(StrategyResourceType resource, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            switch (resource)
            {
                case StrategyResourceType.Game:
                    gameStored += amount;
                    break;
                case StrategyResourceType.Fish:
                    fishStored += amount;
                    break;
                case StrategyResourceType.Eggs:
                    eggsStored += amount;
                    break;
                case StrategyResourceType.Berries:
                    berriesStored += amount;
                    break;
                case StrategyResourceType.Roots:
                    rootsStored += amount;
                    break;
                case StrategyResourceType.Mushrooms:
                    mushroomsStored += amount;
                    break;
            }
        }

        private void RemoveFood(StrategyResourceType resource, int amount)
        {
            int taken = Mathf.Max(0, amount);
            switch (resource)
            {
                case StrategyResourceType.Game:
                    gameStored = Mathf.Max(0, gameStored - taken);
                    break;
                case StrategyResourceType.Fish:
                    fishStored = Mathf.Max(0, fishStored - taken);
                    break;
                case StrategyResourceType.Eggs:
                    eggsStored = Mathf.Max(0, eggsStored - taken);
                    break;
                case StrategyResourceType.Berries:
                    berriesStored = Mathf.Max(0, berriesStored - taken);
                    break;
                case StrategyResourceType.Roots:
                    rootsStored = Mathf.Max(0, rootsStored - taken);
                    break;
                case StrategyResourceType.Mushrooms:
                    mushroomsStored = Mathf.Max(0, mushroomsStored - taken);
                    break;
            }
        }

        private void ClearFoodStock()
        {
            gameStored = 0;
            fishStored = 0;
            eggsStored = 0;
            berriesStored = 0;
            rootsStored = 0;
            mushroomsStored = 0;
        }

        private bool HasFoodStock()
        {
            return gameStored > 0
                || fishStored > 0
                || eggsStored > 0
                || berriesStored > 0
                || rootsStored > 0
                || mushroomsStored > 0;
        }

        private string GetFoodStockText()
        {
            return "Fish "
                + fishStored
                + " / Game "
                + gameStored
                + " / Eggs "
                + eggsStored
                + " / Berries "
                + berriesStored
                + " / Roots "
                + rootsStored
                + " / Mushrooms "
                + mushroomsStored;
        }
    }
}
