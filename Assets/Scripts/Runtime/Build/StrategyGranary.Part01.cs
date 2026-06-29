using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyGranary
    {

        public bool TryTakeReservedHouseholdFood(
            object owner,
            out StrategyResourceType resource,
            out int amount)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            if (owner == null
                || householdFoodReservationOwner != owner
                || householdFoodReservedAmount <= 0
                || householdFoodReservedResource == StrategyResourceType.None)
            {
                return false;
            }

            int stock = GetStoredFood(householdFoodReservedResource);
            amount = Mathf.Min(householdFoodReservedAmount, stock);
            if (amount <= 0)
            {
                ClearHouseholdFoodReservation();
                return false;
            }

            resource = householdFoodReservedResource;
            RemoveFood(resource, amount);

            ClearHouseholdFoodReservation();
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "HouseholdFoodTaken",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("gameStock", gameStored),
                StrategyDebugLogger.F("fishStock", fishStored),
                StrategyDebugLogger.F("eggStock", eggsStored),
                StrategyDebugLogger.F("forageStock", GetForageStockText()),
                StrategyDebugLogger.F("owner", owner));
            return true;
        }

        public void ReleaseHouseholdFoodReservation(object owner)
        {
            if (owner == null || householdFoodReservationOwner != owner)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Granary",
                "HouseholdFoodReservationReleased",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", householdFoodReservedResource),
                StrategyDebugLogger.F("amount", householdFoodReservedAmount),
                StrategyDebugLogger.F("owner", owner));
            ClearHouseholdFoodReservation();
        }

        public bool TryFindDropoffCell(out Vector2Int cell)
        {
            cell = default;
            if (map == null || building == null)
            {
                return false;
            }

            for (int radius = 1; radius <= 3; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y < building.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < building.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == building.Footprint.x + radius - 1
                            || y == building.Footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = building.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            return false;
        }

        public void AddGame(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            gameStored += amount;
            UpdateStockVisual();
            PlayFoodStoredEffect(StrategyResourceType.Game, amount);
            StrategyDebugLogger.Info(
                "Granary",
                "FoodStored",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Game),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", gameStored));
        }

        public void AddFish(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            fishStored += amount;
            UpdateStockVisual();
            PlayFoodStoredEffect(StrategyResourceType.Fish, amount);
            StrategyDebugLogger.Info(
                "Granary",
                "FoodStored",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Fish),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", fishStored));
        }

        public void AddEggs(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            eggsStored += amount;
            UpdateStockVisual();
            PlayFoodStoredEffect(StrategyResourceType.Eggs, amount);
            StrategyDebugLogger.Info(
                "Granary",
                "FoodStored",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", StrategyResourceType.Eggs),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", eggsStored));
        }

        public int ConsumeFood(int requested, out int gameTaken, out int fishTaken)
        {
            gameTaken = 0;
            fishTaken = 0;
            int remaining = Mathf.Max(0, requested);
            if (remaining <= 0)
            {
                return 0;
            }

            gameTaken = Mathf.Min(GetAvailableGameForHouseholds(), remaining);
            gameStored -= gameTaken;
            remaining -= gameTaken;

            fishTaken = Mathf.Min(GetAvailableFishForHouseholds(), remaining);
            fishStored -= fishTaken;
            remaining -= fishTaken;

            int consumed = gameTaken + fishTaken;
            if (consumed <= 0)
            {
                return 0;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "FoodConsumed",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("requested", requested),
                StrategyDebugLogger.F("consumed", consumed),
                StrategyDebugLogger.F("game", gameTaken),
                StrategyDebugLogger.F("fish", fishTaken),
                StrategyDebugLogger.F("gameStock", gameStored),
                StrategyDebugLogger.F("fishStock", fishStored));
            return consumed;
        }

        public float ConsumeFoodRations(float requestedRations, out int gameTaken, out int fishTaken)
        {
            gameTaken = 0;
            fishTaken = 0;
            float remaining = Mathf.Max(0f, requestedRations);
            if (remaining <= 0.01f)
            {
                return 0f;
            }

            fishTaken = ConsumeStoredResourceRations(
                ref fishStored,
                StrategyResourceType.Fish,
                ref remaining,
                out float fishRations);
            int eggsTaken = ConsumeStoredResourceRations(
                ref eggsStored,
                StrategyResourceType.Eggs,
                ref remaining,
                out float eggRations);
            gameTaken = ConsumeStoredResourceRations(
                ref gameStored,
                StrategyResourceType.Game,
                ref remaining,
                out float gameRations);

            float suppliedRations = fishRations + eggRations + gameRations;
            if (suppliedRations <= 0f)
            {
                return 0f;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "Granary",
                "FoodRationsConsumed",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("requestedRations", requestedRations),
                StrategyDebugLogger.F("suppliedRations", suppliedRations),
                StrategyDebugLogger.F("game", gameTaken),
                StrategyDebugLogger.F("fish", fishTaken),
                StrategyDebugLogger.F("eggs", eggsTaken),
                StrategyDebugLogger.F("gameRations", gameRations),
                StrategyDebugLogger.F("fishRations", fishRations),
                StrategyDebugLogger.F("eggRations", eggRations),
                StrategyDebugLogger.F("gameStock", gameStored),
                StrategyDebugLogger.F("fishStock", fishStored),
                StrategyDebugLogger.F("eggStock", eggsStored));
            return suppliedRations;
        }

        private int ConsumeStoredResourceRations(
            ref int stored,
            StrategyResourceType resource,
            ref float remainingRations,
            out float suppliedRations)
        {
            suppliedRations = 0f;
            float rationValue = StrategyFoodNutrition.GetRationValue(resource);
            int available = stored;
            if (resource == StrategyResourceType.Game)
            {
                available = Mathf.Max(0, stored - GetReservedHouseholdAmount(StrategyResourceType.Game));
            }
            else if (resource == StrategyResourceType.Fish)
            {
                available = Mathf.Max(0, stored - GetReservedHouseholdAmount(StrategyResourceType.Fish));
            }
            else if (resource == StrategyResourceType.Eggs)
            {
                available = Mathf.Max(0, stored - GetReservedHouseholdAmount(StrategyResourceType.Eggs));
            }

            if (available <= 0 || rationValue <= 0f || remainingRations <= 0.01f)
            {
                return 0;
            }

            int requestedUnits = Mathf.CeilToInt(remainingRations / rationValue);
            int taken = Mathf.Min(available, requestedUnits);
            if (taken <= 0)
            {
                return 0;
            }

            stored -= taken;
            suppliedRations = taken * rationValue;
            remainingRations = Mathf.Max(0f, remainingRations - suppliedRations);
            return taken;
        }

        private int GetAvailableGameForHouseholds()
        {
            return Mathf.Max(0, gameStored - GetReservedHouseholdAmount(StrategyResourceType.Game));
        }

        private int GetAvailableFishForHouseholds()
        {
            return Mathf.Max(0, fishStored - GetReservedHouseholdAmount(StrategyResourceType.Fish));
        }

        private int GetAvailableEggsForHouseholds()
        {
            return Mathf.Max(0, eggsStored - GetReservedHouseholdAmount(StrategyResourceType.Eggs));
        }

        private int GetReservedHouseholdAmount(StrategyResourceType resource)
        {
            return householdFoodReservedResource == resource ? householdFoodReservedAmount : 0;
        }
        private void ClearHouseholdFoodReservation()
        {
            householdFoodReservationOwner = null;
            householdFoodReservedResource = StrategyResourceType.None;
            householdFoodReservedAmount = 0;
        }

        public string GetHudStatusText()
        {
            CountAvailableSources(out int gameSources, out int fishSources, out int eggSources);
            int forageSources = CountAvailableForagerSources();
            return "Serviced by Haulers"
                + "\n"
                + "Game: "
                + gameStored
                + "\n"
                + "Fish: "
                + fishStored
                + "\n"
                + "Eggs: "
                + eggsStored
                + "\n"
                + "Forage: "
                + GetForageStockText()
                + "\n"
                + "Rations: "
                + TotalRationValue.ToString("0.#")
                + "\n"
                + "Sources: "
                + "game "
                + gameSources
                + " / "
                + "fish "
                + fishSources
                + " / "
                + "eggs "
                + eggSources
                + " / "
                + "forage "
                + forageSources;
        }

        private void CountAvailableSources(out int gameSources, out int fishSources, out int eggSources)
        {
            gameSources = 0;
            fishSources = 0;
            eggSources = 0;
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null
                    && building.TryGetComponent(out StrategyHunterCamp camp)
                    && camp != null
                    && camp.AvailableGame > 0)
                {
                    gameSources++;
                }
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null
                    && building.TryGetComponent(out StrategyFisherHut hut)
                    && hut != null
                    && hut.AvailableFish > 0)
                {
                    fishSources++;
                }
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null
                    && building.TryGetComponent(out StrategyChickenCoop coop)
                    && coop != null
                    && coop.AvailableEggs > 0)
                {
                    eggSources++;
                }
            }
        }

        private static List<StrategyGranary> GetActiveGranaries()
        {
            StrategyPlacedBuilding.CopyActiveComponents(granaryQuery);
            return granaryQuery;
        }

        private static List<StrategyGranary> GetGranariesSortedByDistance(Vector3 nearWorld)
        {
            StrategyPlacedBuilding.CopyActiveComponents(granaryQuery);
            granarySortWorld = nearWorld;
            granaryQuery.Sort(CompareGranariesByDistance);
            return granaryQuery;
        }

        private static int CompareGranariesByDistance(StrategyGranary left, StrategyGranary right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            float leftDistance = (left.FootprintBounds.center - granarySortWorld).sqrMagnitude;
            float rightDistance = (right.FootprintBounds.center - granarySortWorld).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearGranaryWorkplace(this);
                }
            }

            workers.Clear();
        }
    }
}
