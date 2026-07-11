using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryStartHouseholdFoodPickupTask()
        {
            if (!CanStartGardenDuty()
                || home == null
                || home.Resources == null
                || !CanStartHouseholdFoodPickupAsHomeCarrier()
                || householdFoodWorkCooldown > 0f
                || !StrategyDayNightCycleController.IsHouseholdOutdoorWorkTime
                || carriedGameAmount > 0
                || carriedFishAmount > 0
                || carriedForageAmount > 0
                || carriedHouseholdFoodResource != StrategyResourceType.None)
            {
                return false;
            }

            float dailyNeed = CalculateHomeDailyRationNeed();
            if (dailyNeed <= 0f)
            {
                return false;
            }

            float homeRations = home.Resources.GetPreparedDishRations() + home.Resources.GetTotalIngredientRationValue();
            float desiredReserve = Mathf.Max(1f, dailyNeed * HouseholdFoodReserveDays);
            if (homeRations >= desiredReserve)
            {
                return false;
            }

            if (!TryReserveHouseholdFoodPickupSource(
                    home.FootprintBounds.center,
                    out StrategyResourceType resource,
                    out int amount,
                    out Vector2Int pickupCell))
            {
                householdFoodWorkCooldown = Random.Range(
                    HouseholdFoodPickupRetryCooldownMin,
                    HouseholdFoodPickupRetryCooldownMax);
                return false;
            }

            if (!TryBuildPathTo(pickupCell))
            {
                Vector2Int sourceOrigin = GetActiveHouseholdFoodSourceOrigin();
                string sourceKind = GetActiveHouseholdFoodSourceKind();
                ReleaseActiveHouseholdFoodReservation();
                if (WasLastPathBuildDeferred)
                {
                    householdFoodWorkCooldown = Random.Range(0.18f, 0.38f);
                    return false;
                }

                householdFoodWorkCooldown = Random.Range(
                    HouseholdFoodPickupRetryCooldownMin,
                    HouseholdFoodPickupRetryCooldownMax);
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderFoodPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("source", sourceKind),
                    StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            carriedHouseholdFoodResource = resource;
            activity = ResidentActivity.MovingToHouseholdFoodPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            householdFoodWorkCooldown = Random.Range(
                HouseholdFoodPickupCooldownMin,
                HouseholdFoodPickupCooldownMax);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderFoodPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("homeRations", homeRations),
                StrategyDebugLogger.F("desiredRations", desiredReserve),
                StrategyDebugLogger.F("source", GetActiveHouseholdFoodSourceKind()),
                StrategyDebugLogger.F("sourceOrigin", GetActiveHouseholdFoodSourceOrigin()),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("pickupCell", pickupCell));
            return true;
        }
    }
}
