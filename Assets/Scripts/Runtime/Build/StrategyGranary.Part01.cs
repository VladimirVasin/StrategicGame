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
            gameTaken = ConsumeStoredResourceRations(
                ref gameStored,
                StrategyResourceType.Game,
                ref remaining,
                out float gameRations);

            float suppliedRations = fishRations + gameRations;
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
                StrategyDebugLogger.F("gameRations", gameRations),
                StrategyDebugLogger.F("fishRations", fishRations),
                StrategyDebugLogger.F("gameStock", gameStored),
                StrategyDebugLogger.F("fishStock", fishStored));
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
            CountAvailableSources(out int gameSources, out int fishSources);
            int forageSources = CountAvailableForagerSources();
            return "Serviced by Haulers"
                + "\n"
                + "Game: "
                + gameStored
                + "\n"
                + "Fish: "
                + fishStored
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
                + "forage "
                + forageSources;
        }

        private void CountAvailableSources(out int gameSources, out int fishSources)
        {
            gameSources = 0;
            fishSources = 0;
            StrategyHunterCamp[] camps = Object.FindObjectsByType<StrategyHunterCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                if (camps[i] != null && camps[i].AvailableGame > 0)
                {
                    gameSources++;
                }
            }

            StrategyFisherHut[] huts = Object.FindObjectsByType<StrategyFisherHut>();
            for (int i = 0; i < huts.Length; i++)
            {
                if (huts[i] != null && huts[i].AvailableFish > 0)
                {
                    fishSources++;
                }
            }
        }

        private static StrategyGranary[] GetGranariesSortedByDistance(Vector3 nearWorld)
        {
            StrategyGranary[] granaries = Object.FindObjectsByType<StrategyGranary>();
            System.Array.Sort(
                granaries,
                (left, right) =>
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

                    float leftDistance = (left.FootprintBounds.center - nearWorld).sqrMagnitude;
                    float rightDistance = (right.FootprintBounds.center - nearWorld).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });
            return granaries;
        }

        private void EnsureStockRenderers()
        {
            if (gameStockRenderer == null)
            {
                GameObject gameObject = new GameObject("Game Stock");
                gameObject.transform.SetParent(transform, false);
                gameStockRenderer = gameObject.AddComponent<SpriteRenderer>();
                gameStockRenderer.color = Color.white;
            }

            if (fishStockRenderer == null)
            {
                GameObject fishObject = new GameObject("Fish Stock");
                fishObject.transform.SetParent(transform, false);
                fishStockRenderer = fishObject.AddComponent<SpriteRenderer>();
                fishStockRenderer.color = Color.white;
            }

            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderers();
            if (gameStockRenderer != null)
            {
                gameStockRenderer.sprite = StrategyBuildingSpriteFactory.GetGranaryGameStockSprite(gameStored);
                gameStockRenderer.gameObject.SetActive(gameStored > 0 && gameStockRenderer.sprite != null);
            }

            if (fishStockRenderer != null)
            {
                fishStockRenderer.sprite = StrategyBuildingSpriteFactory.GetGranaryFishStockSprite(fishStored);
                fishStockRenderer.gameObject.SetActive(fishStored > 0 && fishStockRenderer.sprite != null);
            }

            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            if (gameStockRenderer != null)
            {
                Vector3 gameWorld = new Vector3(bounds.min.x + 0.42f, bounds.min.y + 0.35f, -0.13f);
                gameStockRenderer.transform.localPosition = transform.InverseTransformPoint(gameWorld);
                gameStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(gameStockRenderer, gameWorld, 1);
            }

            if (fishStockRenderer != null)
            {
                Vector3 fishWorld = new Vector3(bounds.max.x - 0.42f, bounds.min.y + 0.37f, -0.13f);
                fishStockRenderer.transform.localPosition = transform.InverseTransformPoint(fishWorld);
                fishStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(fishStockRenderer, fishWorld, 1);
            }
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
