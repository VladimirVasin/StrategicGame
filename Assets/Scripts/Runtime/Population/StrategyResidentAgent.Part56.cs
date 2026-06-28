using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void StartPickingUpGranaryForage()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if ((activeForageFoodSource == null && activeLooseFoodSource == null && activeEggFoodSource == null)
                || activeGranaryDeliveryTarget == null)
            {
                ResetGranaryWorkToIdle();
                return;
            }

            activity = ResidentActivity.PickingUpGranaryForage;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            Bounds sourceBounds = activeLooseFoodSource != null
                ? activeLooseFoodSource.FootprintBounds
                : activeForageFoodSource != null
                    ? activeForageFoodSource.FootprintBounds
                    : activeEggFoodSource.FootprintBounds;
            FaceWorldPoint(sourceBounds.center);
            StrategyDebugLogger.Info(
                "Granary",
                "ForagePickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", GetActiveGranaryForageSourceOrigin()),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget.Origin));
        }

        private void UpdatePickingUpGranaryForage()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateForageWork(carriedForageResource, true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeForageFoodSource == null && activeLooseFoodSource == null && activeEggFoodSource == null)
                || activeGranaryDeliveryTarget == null
                || !activeGranaryDeliveryTarget.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                activeForageFoodSource?.ReleaseStoredForageReservation(this);
                activeEggFoodSource?.ReleaseStoredEggsReservation(this);
                activeLooseFoodSource?.ReleaseReservation(this);
                StrategyDebugLogger.Warn(
                    "Granary",
                    "ForagePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_granary_path"),
                    StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
                ResetGranaryWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin;
            StrategyResourceType resource;
            if (activeLooseFoodSource != null)
            {
                sourceOrigin = activeLooseFoodSource.Origin;
                if (!activeLooseFoodSource.TryTakeReserved(this, out resource, out carriedForageAmount)
                    || !IsForageFood(resource))
                {
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "ForagePickupRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "loose_take_failed"),
                        StrategyDebugLogger.F("sourceOrigin", sourceOrigin));
                    activeLooseFoodSource = null;
                    ResetGranaryWorkToIdle();
                    return;
                }

                activeLooseFoodSource = null;
            }
            else if (activeEggFoodSource != null)
            {
                sourceOrigin = activeEggFoodSource.Origin;
                resource = StrategyResourceType.Eggs;
                if (!activeEggFoodSource.TryTakeReservedEggs(this, out carriedForageAmount))
                {
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "ForagePickupRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "take_failed"),
                        StrategyDebugLogger.F("sourceOrigin", sourceOrigin));
                    ResetGranaryWorkToIdle();
                    return;
                }

                activeEggFoodSource = null;
            }
            else if (!activeForageFoodSource.TryTakeReservedForage(this, out resource, out carriedForageAmount))
            {
                StrategyDebugLogger.Warn(
                    "Granary",
                    "ForagePickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeForageFoodSource.Origin));
                ResetGranaryWorkToIdle();
                return;
            }
            else
            {
                sourceOrigin = activeForageFoodSource.Origin;
            }

            carriedForageResource = resource;
            activeForageFoodSource = null;
            activeEggFoodSource = null;
            activity = ResidentActivity.CarryingForageToGranary;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedForageVisible(true);
            StrategyDebugLogger.Info(
                "Granary",
                "ForagePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget.Origin));
        }

        private void StartDepositingGranaryForage()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGranaryForage;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (activeGranaryDeliveryTarget != null)
            {
                FaceWorldPoint(activeGranaryDeliveryTarget.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Granary",
                "ForageDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedForageResource),
                StrategyDebugLogger.F("amount", carriedForageAmount),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingGranaryForage()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateForageWork(carriedForageResource, true);
            SetCarriedForageVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            StrategyResourceType resource = carriedForageResource;
            int depositedAmount = carriedForageAmount;
            if (activeGranaryDeliveryTarget != null)
            {
                activeGranaryDeliveryTarget.AddFood(resource, depositedAmount);
            }

            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            StrategyDebugLogger.Info(
                "Granary",
                "ForageDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
            CompleteGranaryDelivery();
        }

        private bool TryStartForageFoodReturn(string reason)
        {
            if (carriedForageAmount <= 0 || !IsForageFood(carriedForageResource))
            {
                return false;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyGranary.TryFindNearestDropoff(transform.position, out StrategyGranary granary, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = null;
                returnGranary = granary;
                activity = ResidentActivity.ReturningForageToGranary;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedForageVisible(true);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Granary",
                    "CarriedForageReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedForageResource),
                    StrategyDebugLogger.F("amount", carriedForageAmount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedForageFoodImmediately(reason, "no_reachable_granary");
        }

        private bool CompleteForageFoodReturn(out int amount, out object resource, out Vector2Int storageOrigin)
        {
            amount = carriedForageAmount;
            resource = carriedForageResource;
            storageOrigin = Vector2Int.zero;
            if (returnGranary == null)
            {
                if (!StoreCarriedForageFoodImmediately("resource_return_completed", "target_missing"))
                {
                    ScheduleCarriedResourceReturnRetry();
                }

                return false;
            }

            storageOrigin = returnGranary.Origin;
            returnGranary.AddFood(carriedForageResource, amount);
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            SetCarriedForageVisible(false);
            return true;
        }

        private bool StoreCarriedForageFoodImmediately(string reason, string fallbackReason)
        {
            StrategyResourceType resource = carriedForageResource;
            int amount = carriedForageAmount;
            if (amount <= 0 || !IsForageFood(resource))
            {
                return false;
            }

            if (StrategyGranary.TryFindNearestGranary(transform.position, out StrategyGranary granary))
            {
                granary.AddFood(resource, amount);
                carriedForageResource = StrategyResourceType.None;
                carriedForageAmount = 0;
                SetCarriedForageVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Granary",
                    "CarriedForageStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                StrategyLooseCarriedResourcePile.Create(map, cell, transform.position, resource, amount);
                carriedForageResource = StrategyResourceType.None;
                carriedForageAmount = 0;
                SetCarriedForageVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Granary",
                    "CarriedForageDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_granary"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private static bool IsForageFood(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Berries
                || resource == StrategyResourceType.Roots
                || resource == StrategyResourceType.Mushrooms
                || resource == StrategyResourceType.Eggs;
        }

        private Vector2Int GetActiveGranaryForageSourceOrigin()
        {
            if (activeLooseFoodSource != null)
            {
                return activeLooseFoodSource.Origin;
            }

            if (activeForageFoodSource != null)
            {
                return activeForageFoodSource.Origin;
            }

            return activeEggFoodSource != null ? activeEggFoodSource.Origin : Vector2Int.zero;
        }
    }
}
