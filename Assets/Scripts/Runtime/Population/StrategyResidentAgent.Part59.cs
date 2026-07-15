using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private static readonly List<StrategyHunterCamp> householdFoodHunterCampQuery = new();
        private static readonly List<StrategyFisherHut> householdFoodFisherHutQuery = new();
        private static readonly List<StrategyForagerCamp> householdFoodForagerCampQuery = new();
        private static readonly List<StrategyChickenCoop> householdFoodChickenCoopQuery = new();
        private StrategyHunterCamp activeHouseholdFoodHunterCamp;
        private StrategyFisherHut activeHouseholdFoodFisherHut;
        private StrategyForagerCamp activeHouseholdFoodForagerCamp;
        private StrategyChickenCoop activeHouseholdFoodChickenCoop;
        private StrategyStarterCaravanCart activeHouseholdFoodCaravanCart;
        private StrategyLooseCarriedResourcePile activeHouseholdFoodLoosePile;

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

            if (StrategyLooseCarriedResourcePile.TryReserveNearestHouseholdFood(
                    requesterWorld, this, out StrategyLooseCarriedResourcePile loosePile,
                    out resource, out amount, out pickupCell))
            {
                activeHouseholdFoodLoosePile = loosePile;
                return true;
            }

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

            List<StrategyHunterCamp> hunterCamps = GetActiveHouseholdFoodComponents(householdFoodHunterCampQuery);
            for (int i = 0; i < hunterCamps.Count; i++)
            {
                StrategyHunterCamp camp = hunterCamps[i];
                if (camp == null
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

            List<StrategyFisherHut> fisherHuts = GetActiveHouseholdFoodComponents(householdFoodFisherHutQuery);
            for (int i = 0; i < fisherHuts.Count; i++)
            {
                StrategyFisherHut hut = fisherHuts[i];
                if (hut == null
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

            List<StrategyForagerCamp> foragerCamps = GetActiveHouseholdFoodComponents(householdFoodForagerCampQuery);
            for (int i = 0; i < foragerCamps.Count; i++)
            {
                StrategyForagerCamp camp = foragerCamps[i];
                if (camp == null
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

            List<StrategyChickenCoop> coops = GetActiveHouseholdFoodComponents(householdFoodChickenCoopQuery);
            for (int i = 0; i < coops.Count; i++)
            {
                StrategyChickenCoop coop = coops[i];
                if (coop == null
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

        private static List<T> GetActiveHouseholdFoodComponents<T>(List<T> results)
            where T : Component
        {
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks != null && chunks.IsConfigured)
            {
                chunks.CopyActiveBuildingComponents(results);
            }
            else
            {
                StrategyPlacedBuilding.CopyActiveComponents(results);
            }

            return results;
        }

        private bool HasActiveHouseholdFoodSource()
        {
            return activeHouseholdFoodGranary != null
                || activeHouseholdFoodHunterCamp != null
                || activeHouseholdFoodFisherHut != null
                || activeHouseholdFoodForagerCamp != null
                || activeHouseholdFoodChickenCoop != null
                || activeHouseholdFoodCaravanCart != null
                || activeHouseholdFoodLoosePile != null;
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

            return activeHouseholdFoodLoosePile != null
                ? activeHouseholdFoodLoosePile.FootprintBounds
                : activeHouseholdFoodForagerCamp != null
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

            return activeHouseholdFoodLoosePile != null
                ? activeHouseholdFoodLoosePile.Origin
                : activeHouseholdFoodForagerCamp != null ? activeHouseholdFoodForagerCamp.Origin : Vector2Int.zero;
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

            return activeHouseholdFoodLoosePile != null
                ? "LooseFood"
                : activeHouseholdFoodForagerCamp != null ? "ForagerCamp" : "None";
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

            if (activeHouseholdFoodLoosePile != null)
            {
                activeHouseholdFoodLoosePile.ReleaseReservation(this);
                activeHouseholdFoodLoosePile = null;
            }
        }
    }
}
