using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private StrategyHunterCamp activeHouseholdFoodHunterCamp;
        private StrategyFisherHut activeHouseholdFoodFisherHut;
        private StrategyForagerCamp activeHouseholdFoodForagerCamp;
        private StrategyChickenCoop activeHouseholdFoodChickenCoop;
        private StrategyStarterCaravanCart activeHouseholdFoodCaravanCart;

        private bool TryReserveHouseholdFoodPickupSource(
            Vector3 requesterWorld,
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int pickupCell)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            pickupCell = default;
            ReleaseActiveHouseholdFoodReservation();

            if (StrategyGranary.TryReserveNearestHouseholdFood(
                    requesterWorld,
                    this,
                    out StrategyGranary granary,
                    out resource,
                    out amount,
                    out pickupCell))
            {
                activeHouseholdFoodGranary = granary;
                return true;
            }

            if (StrategyStarterCaravanCart.TryReserveNearestHouseholdFood(
                    requesterWorld,
                    this,
                    out StrategyStarterCaravanCart cart,
                    out resource,
                    out amount,
                    out pickupCell))
            {
                activeHouseholdFoodCaravanCart = cart;
                return true;
            }

            return StrategyGranary.GetTotalSettlementFoodRations() <= 0.01f
                && StrategyStarterCaravanCart.GetTotalAvailableHouseholdRations() <= 0.01f
                && TryReserveNearestProductionHouseholdFood(requesterWorld, out resource, out amount, out pickupCell);
        }

        private bool TryReserveNearestProductionHouseholdFood(
            Vector3 requesterWorld,
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int pickupCell)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            pickupCell = default;

            StrategyHunterCamp gameSource = null;
            StrategyFisherHut fishSource = null;
            StrategyForagerCamp forageSource = null;
            StrategyChickenCoop eggSource = null;
            float bestDistance = float.MaxValue;
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null
                    || !building.TryGetComponent(out StrategyHunterCamp camp)
                    || camp == null
                    || camp.AvailableGame <= 0
                    || !camp.TryFindDropoffCell(out Vector2Int cell))
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - requesterWorld).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = camp;
                    fishSource = null;
                    forageSource = null;
                    eggSource = null;
                    pickupCell = cell;
                }
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null
                    || !building.TryGetComponent(out StrategyFisherHut hut)
                    || hut == null
                    || hut.AvailableFish <= 0
                    || !hut.TryFindDropoffCell(out Vector2Int cell))
                {
                    continue;
                }

                float distance = (hut.FootprintBounds.center - requesterWorld).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = null;
                    fishSource = hut;
                    forageSource = null;
                    eggSource = null;
                    pickupCell = cell;
                }
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null
                    || !building.TryGetComponent(out StrategyForagerCamp camp)
                    || camp == null
                    || camp.AvailableForage <= 0
                    || !camp.TryFindDropoffCell(out Vector2Int cell))
                {
                    continue;
                }

                float distance = (camp.FootprintBounds.center - requesterWorld).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = null;
                    fishSource = null;
                    forageSource = camp;
                    eggSource = null;
                    pickupCell = cell;
                }
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null
                    || !building.TryGetComponent(out StrategyChickenCoop coop)
                    || coop == null
                    || coop.AvailableEggs <= 0
                    || !coop.TryFindDropoffCell(out Vector2Int cell))
                {
                    continue;
                }

                float distance = (coop.FootprintBounds.center - requesterWorld).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    gameSource = null;
                    fishSource = null;
                    forageSource = null;
                    eggSource = coop;
                    pickupCell = cell;
                }
            }

            if (gameSource != null && gameSource.TryReserveStoredGame(this, out amount))
            {
                activeHouseholdFoodHunterCamp = gameSource;
                resource = StrategyResourceType.Game;
                return amount > 0;
            }

            if (fishSource != null && fishSource.TryReserveStoredFish(this, out amount))
            {
                activeHouseholdFoodFisherHut = fishSource;
                resource = StrategyResourceType.Fish;
                return amount > 0;
            }

            if (forageSource != null && forageSource.TryReserveStoredForage(this, out resource, out amount))
            {
                activeHouseholdFoodForagerCamp = forageSource;
                return amount > 0;
            }

            if (eggSource != null && eggSource.TryReserveStoredEggs(this, out amount))
            {
                activeHouseholdFoodChickenCoop = eggSource;
                resource = StrategyResourceType.Eggs;
                return amount > 0;
            }

            pickupCell = default;
            return false;
        }

        private bool HasActiveHouseholdFoodSource()
        {
            return activeHouseholdFoodGranary != null
                || activeHouseholdFoodHunterCamp != null
                || activeHouseholdFoodFisherHut != null
                || activeHouseholdFoodForagerCamp != null
                || activeHouseholdFoodChickenCoop != null
                || activeHouseholdFoodCaravanCart != null;
        }

        private Bounds GetActiveHouseholdFoodSourceBounds()
        {
            if (activeHouseholdFoodGranary != null)
            {
                return activeHouseholdFoodGranary.FootprintBounds;
            }

            if (activeHouseholdFoodHunterCamp != null)
            {
                return activeHouseholdFoodHunterCamp.FootprintBounds;
            }

            if (activeHouseholdFoodFisherHut != null)
            {
                return activeHouseholdFoodFisherHut.FootprintBounds;
            }

            if (activeHouseholdFoodChickenCoop != null)
            {
                return activeHouseholdFoodChickenCoop.FootprintBounds;
            }

            if (activeHouseholdFoodCaravanCart != null)
            {
                return activeHouseholdFoodCaravanCart.FootprintBounds;
            }

            return activeHouseholdFoodForagerCamp != null
                ? activeHouseholdFoodForagerCamp.FootprintBounds
                : new Bounds(transform.position, Vector3.one);
        }

        private Vector2Int GetActiveHouseholdFoodSourceOrigin()
        {
            if (activeHouseholdFoodGranary != null)
            {
                return activeHouseholdFoodGranary.Origin;
            }

            if (activeHouseholdFoodHunterCamp != null)
            {
                return activeHouseholdFoodHunterCamp.Origin;
            }

            if (activeHouseholdFoodFisherHut != null)
            {
                return activeHouseholdFoodFisherHut.Origin;
            }

            if (activeHouseholdFoodChickenCoop != null)
            {
                return activeHouseholdFoodChickenCoop.Origin;
            }

            if (activeHouseholdFoodCaravanCart != null)
            {
                return activeHouseholdFoodCaravanCart.Origin;
            }

            return activeHouseholdFoodForagerCamp != null ? activeHouseholdFoodForagerCamp.Origin : Vector2Int.zero;
        }

        private string GetActiveHouseholdFoodSourceKind()
        {
            if (activeHouseholdFoodGranary != null)
            {
                return "Granary";
            }

            if (activeHouseholdFoodHunterCamp != null)
            {
                return "HunterCamp";
            }

            if (activeHouseholdFoodFisherHut != null)
            {
                return "FisherHut";
            }

            if (activeHouseholdFoodChickenCoop != null)
            {
                return "ChickenCoop";
            }

            if (activeHouseholdFoodCaravanCart != null)
            {
                return "CaravanCart";
            }

            return activeHouseholdFoodForagerCamp != null ? "ForagerCamp" : "None";
        }

        private bool TryTakeActiveHouseholdFoodReservation(
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int sourceOrigin)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            sourceOrigin = GetActiveHouseholdFoodSourceOrigin();

            if (activeHouseholdFoodGranary != null)
            {
                bool taken = activeHouseholdFoodGranary.TryTakeReservedHouseholdFood(this, out resource, out amount);
                if (!taken)
                {
                    activeHouseholdFoodGranary.ReleaseHouseholdFoodReservation(this);
                }

                activeHouseholdFoodGranary = null;
                return taken;
            }

            if (activeHouseholdFoodHunterCamp != null)
            {
                resource = StrategyResourceType.Game;
                bool taken = activeHouseholdFoodHunterCamp.TryTakeReservedGame(this, out amount);
                if (!taken)
                {
                    activeHouseholdFoodHunterCamp.ReleaseStoredGameReservation(this);
                }

                activeHouseholdFoodHunterCamp = null;
                return taken;
            }

            if (activeHouseholdFoodFisherHut != null)
            {
                resource = StrategyResourceType.Fish;
                bool taken = activeHouseholdFoodFisherHut.TryTakeReservedFish(this, out amount);
                if (!taken)
                {
                    activeHouseholdFoodFisherHut.ReleaseStoredFishReservation(this);
                }

                activeHouseholdFoodFisherHut = null;
                return taken;
            }

            if (activeHouseholdFoodForagerCamp != null)
            {
                bool taken = activeHouseholdFoodForagerCamp.TryTakeReservedForage(this, out resource, out amount);
                if (!taken)
                {
                    activeHouseholdFoodForagerCamp.ReleaseStoredForageReservation(this);
                }

                activeHouseholdFoodForagerCamp = null;
                return taken;
            }

            if (activeHouseholdFoodChickenCoop != null)
            {
                resource = StrategyResourceType.Eggs;
                bool taken = activeHouseholdFoodChickenCoop.TryTakeReservedEggs(this, out amount);
                if (!taken)
                {
                    activeHouseholdFoodChickenCoop.ReleaseStoredEggsReservation(this);
                }

                activeHouseholdFoodChickenCoop = null;
                return taken;
            }

            if (activeHouseholdFoodCaravanCart != null)
            {
                bool taken = activeHouseholdFoodCaravanCart.TryTakeReservedHouseholdFood(this, out resource, out amount);
                if (!taken)
                {
                    activeHouseholdFoodCaravanCart.ReleaseHouseholdFoodReservation(this);
                }

                activeHouseholdFoodCaravanCart = null;
                return taken;
            }

            return false;
        }

        private void ApplyCarriedHouseholdFood(StrategyResourceType resource, int amount)
        {
            carriedHouseholdFoodResource = resource;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;

            if (resource == StrategyResourceType.Game)
            {
                carriedGameAmount = amount;
            }
            else if (resource == StrategyResourceType.Fish)
            {
                carriedFishAmount = amount;
            }
            else
            {
                carriedForageResource = resource;
                carriedForageAmount = amount;
            }

            SetCarriedHouseholdFoodVisible(true);
        }

        private void ReleaseActiveHouseholdFoodReservation()
        {
            if (activeHouseholdFoodGranary != null)
            {
                activeHouseholdFoodGranary.ReleaseHouseholdFoodReservation(this);
                activeHouseholdFoodGranary = null;
            }

            if (activeHouseholdFoodHunterCamp != null)
            {
                activeHouseholdFoodHunterCamp.ReleaseStoredGameReservation(this);
                activeHouseholdFoodHunterCamp = null;
            }

            if (activeHouseholdFoodFisherHut != null)
            {
                activeHouseholdFoodFisherHut.ReleaseStoredFishReservation(this);
                activeHouseholdFoodFisherHut = null;
            }

            if (activeHouseholdFoodForagerCamp != null)
            {
                activeHouseholdFoodForagerCamp.ReleaseStoredForageReservation(this);
                activeHouseholdFoodForagerCamp = null;
            }

            if (activeHouseholdFoodChickenCoop != null)
            {
                activeHouseholdFoodChickenCoop.ReleaseStoredEggsReservation(this);
                activeHouseholdFoodChickenCoop = null;
            }

            if (activeHouseholdFoodCaravanCart != null)
            {
                activeHouseholdFoodCaravanCart.ReleaseHouseholdFoodReservation(this);
                activeHouseholdFoodCaravanCart = null;
            }
        }
    }
}
