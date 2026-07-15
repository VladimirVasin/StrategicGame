using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private string carriedPreparedDishRecipeId = string.Empty;
        private int carriedPreparedDishAmount;
        private float carriedPreparedDishLeftoverRations;

        private bool TryTakeActiveHouseholdFoodReservation(
            out StrategyResourceType resource,
            out int amount,
            out Vector2Int sourceOrigin)
        {
            resource = StrategyResourceType.None;
            amount = 0;
            sourceOrigin = GetActiveHouseholdFoodSourceOrigin();
            ClearCarriedPreparedDishPayload();

            if (activeHouseholdFoodLoosePile != null)
            {
                bool taken = activeHouseholdFoodLoosePile.TryTakeReserved(
                    this,
                    out StrategyLooseResourcePickup pickup);
                if (!taken)
                {
                    activeHouseholdFoodLoosePile.ReleaseReservation(this);
                }
                else
                {
                    resource = pickup.Resource;
                    amount = pickup.Amount;
                    CaptureCarriedPreparedDishPayload(pickup);
                }

                activeHouseholdFoodLoosePile = null;
                return taken;
            }

            if (activeHouseholdFoodGranary != null)
            {
                bool taken = activeHouseholdFoodGranary.TryTakeReservedHouseholdFood(
                    this,
                    out resource,
                    out amount);
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
                bool taken = activeHouseholdFoodForagerCamp.TryTakeReservedForage(
                    this,
                    out resource,
                    out amount);
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
                bool taken = activeHouseholdFoodCaravanCart.TryTakeReservedHouseholdFood(
                    this,
                    out resource,
                    out amount);
                if (!taken)
                {
                    activeHouseholdFoodCaravanCart.ReleaseHouseholdFoodReservation(this);
                }

                activeHouseholdFoodCaravanCart = null;
                return taken;
            }

            return false;
        }

        private void CaptureCarriedPreparedDishPayload(StrategyLooseResourcePickup pickup)
        {
            if (!pickup.HasPreparedDishState)
            {
                return;
            }

            carriedPreparedDishRecipeId = pickup.PreparedDishRecipeId;
            carriedPreparedDishAmount = pickup.PreparedDishAmount;
            carriedPreparedDishLeftoverRations = pickup.PreparedDishLeftoverRations;
        }

        private void StoreCarriedHouseholdFood(
            StrategyHouseResourceStore target,
            StrategyResourceType resource,
            int transportAmount)
        {
            if (target == null || resource == StrategyResourceType.None || transportAmount <= 0)
            {
                return;
            }

            if (resource == StrategyResourceType.Dish
                && (carriedPreparedDishAmount > 0 || carriedPreparedDishLeftoverRations > 0f))
            {
                target.AddRecoveredPreparedDishes(
                    carriedPreparedDishRecipeId,
                    carriedPreparedDishAmount,
                    carriedPreparedDishLeftoverRations);
                return;
            }

            target.AddResource(resource, transportAmount);
        }

        private void ClearCarriedPreparedDishPayload()
        {
            carriedPreparedDishRecipeId = string.Empty;
            carriedPreparedDishAmount = 0;
            carriedPreparedDishLeftoverRations = 0f;
        }
    }
}
