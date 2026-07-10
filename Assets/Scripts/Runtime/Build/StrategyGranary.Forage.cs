using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyGranary
    {

        public int BerriesStored => berriesStored;
        public int RootsStored => rootsStored;
        public int MushroomsStored => mushroomsStored;
        public int ForageStored => berriesStored + rootsStored + mushroomsStored;

        public void AddFood(StrategyResourceType resource, int amount)
        {
            if (resource == StrategyResourceType.Game)
            {
                AddGame(amount);
                return;
            }

            if (resource == StrategyResourceType.Fish)
            {
                AddFish(amount);
                return;
            }

            if (resource == StrategyResourceType.Eggs)
            {
                AddEggs(amount);
                return;
            }

            AddForageFood(resource, amount);
        }

        private bool TryReserveNearestFoodSource(
            object owner,
            out StrategyResourceType resource,
            out StrategyHunterCamp gameSource,
            out StrategyFisherHut fishSource,
            out StrategyForagerCamp forageSource,
            out StrategyChickenCoop eggSource)
        {
            resource = StrategyResourceType.None;
            gameSource = null;
            fishSource = null;
            forageSource = null;
            eggSource = null;
            float bestDistance = float.MaxValue;

            List<StrategyHunterCamp> hunterCamps = GetActiveBuildingComponents(hunterCampQuery);
            for (int i = 0; i < hunterCamps.Count; i++)
            {
                StrategyHunterCamp camp = hunterCamps[i];
                if (camp == null || camp.AvailableGame <= 0)
                {
                    continue;
                }

                float distance = GetFoodSourceDistance(camp.FootprintBounds);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = camp;
                    fishSource = null;
                    forageSource = null;
                    resource = StrategyResourceType.Game;
                }
            }

            List<StrategyFisherHut> fisherHuts = GetActiveBuildingComponents(fisherHutQuery);
            for (int i = 0; i < fisherHuts.Count; i++)
            {
                StrategyFisherHut hut = fisherHuts[i];
                if (hut == null || hut.AvailableFish <= 0)
                {
                    continue;
                }

                float distance = GetFoodSourceDistance(hut.FootprintBounds);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = null;
                    fishSource = hut;
                    forageSource = null;
                    resource = StrategyResourceType.Fish;
                }
            }

            List<StrategyForagerCamp> foragerCamps = GetActiveBuildingComponents(foragerCampQuery);
            for (int i = 0; i < foragerCamps.Count; i++)
            {
                StrategyForagerCamp camp = foragerCamps[i];
                if (camp == null || camp.AvailableForage <= 0)
                {
                    continue;
                }

                float distance = GetFoodSourceDistance(camp.FootprintBounds);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = null;
                    fishSource = null;
                    forageSource = camp;
                    eggSource = null;
                }
            }

            List<StrategyChickenCoop> coops = GetActiveBuildingComponents(chickenCoopQuery);
            for (int i = 0; i < coops.Count; i++)
            {
                StrategyChickenCoop coop = coops[i];
                if (coop == null || coop.AvailableEggs <= 0)
                {
                    continue;
                }

                float distance = GetFoodSourceDistance(coop.FootprintBounds);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = null;
                    fishSource = null;
                    forageSource = null;
                    eggSource = coop;
                    resource = StrategyResourceType.Eggs;
                }
            }

            if (gameSource != null)
            {
                return gameSource.TryReserveStoredGame(owner, out _)
                    && SetReservedFoodSource(StrategyResourceType.Game, ref resource);
            }

            if (fishSource != null)
            {
                return fishSource.TryReserveStoredFish(owner, out _)
                    && SetReservedFoodSource(StrategyResourceType.Fish, ref resource);
            }

            if (forageSource != null
                && forageSource.TryReserveStoredForage(owner, out StrategyResourceType forageResource, out _))
            {
                resource = forageResource;
                return true;
            }

            if (eggSource != null)
            {
                return eggSource.TryReserveStoredEggs(owner, out _)
                    && SetReservedFoodSource(StrategyResourceType.Eggs, ref resource);
            }

            return false;
        }

        private float GetFoodSourceDistance(Bounds sourceBounds)
        {
            return (sourceBounds.center - FootprintBounds.center).sqrMagnitude;
        }

        private static bool SetReservedFoodSource(StrategyResourceType value, ref StrategyResourceType resource)
        {
            resource = value;
            return true;
        }

        private bool TryChooseHouseholdFoodResource(out StrategyResourceType resource)
        {
            if (GetAvailableFishForHouseholds() > 0)
            {
                resource = StrategyResourceType.Fish;
                return true;
            }

            if (GetAvailableGameForHouseholds() > 0)
            {
                resource = StrategyResourceType.Game;
                return true;
            }

            if (GetAvailableEggsForHouseholds() > 0)
            {
                resource = StrategyResourceType.Eggs;
                return true;
            }

            if (GetAvailableForHouseholds(StrategyResourceType.Berries) > 0)
            {
                resource = StrategyResourceType.Berries;
                return true;
            }

            if (GetAvailableForHouseholds(StrategyResourceType.Roots) > 0)
            {
                resource = StrategyResourceType.Roots;
                return true;
            }

            if (GetAvailableForHouseholds(StrategyResourceType.Mushrooms) > 0)
            {
                resource = StrategyResourceType.Mushrooms;
                return true;
            }

            resource = StrategyResourceType.None;
            return false;
        }

        private int GetStoredFood(StrategyResourceType resource)
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

        private int GetAvailableForHouseholds(StrategyResourceType resource)
        {
            return Mathf.Max(0, GetStoredFood(resource) - GetReservedHouseholdAmount(resource));
        }

        private float GetStoredForageRations()
        {
            return berriesStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Berries)
                + rootsStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Roots)
                + mushroomsStored * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Mushrooms);
        }

        private float GetAvailableForageHouseholdRations()
        {
            return GetAvailableForHouseholds(StrategyResourceType.Berries) * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Berries)
                + GetAvailableForHouseholds(StrategyResourceType.Roots) * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Roots)
                + GetAvailableForHouseholds(StrategyResourceType.Mushrooms) * StrategyFoodNutrition.GetRationValue(StrategyResourceType.Mushrooms);
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

        private void AddForageFood(StrategyResourceType resource, int amount)
        {
            if (!IsForageFood(resource) || amount <= 0)
            {
                return;
            }

            switch (resource)
            {
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

            UpdateStockVisual();
            PlayFoodStoredEffect(resource, amount);
            StrategyDebugLogger.Info(
                "Granary",
                "FoodStored",
                StrategyDebugLogger.F("granaryOrigin", Origin),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("added", amount),
                StrategyDebugLogger.F("stock", GetStoredFood(resource)),
                StrategyDebugLogger.F("forageStock", GetForageStockText()));
        }

        private int CountAvailableForagerSources()
        {
            int count = 0;
            List<StrategyForagerCamp> camps = GetActiveBuildingComponents(foragerCampQuery);
            for (int i = 0; i < camps.Count; i++)
            {
                StrategyForagerCamp camp = camps[i];
                if (camp != null && camp.AvailableForage > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private string GetForageStockText()
        {
            return "Berries "
                + berriesStored
                + " / Roots "
                + rootsStored
                + " / Mushrooms "
                + mushroomsStored;
        }

        private static bool IsForageFood(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Berries
                || resource == StrategyResourceType.Roots
                || resource == StrategyResourceType.Mushrooms;
        }
    }
}
