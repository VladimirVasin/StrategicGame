using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void StoreReturnedMaterialAtYard(
            StrategyStorageYard yard,
            StrategyConstructionResourceKind resource,
            int amount)
        {
            if (yard == null || amount <= 0)
            {
                return;
            }

            int reservedAmount = GetRestorableCarriedConstructionReservationAmount(resource, amount, out StrategyConstructionSite site);
            if (reservedAmount > 0)
            {
                yard.ReturnReservedConstructionResource(site, resource, reservedAmount);
            }

            int regularAmount = amount - reservedAmount;
            if (regularAmount > 0)
            {
                if (resource == StrategyConstructionResourceKind.Logs)
                {
                    yard.AddLogs(regularAmount);
                }
                else if (resource == StrategyConstructionResourceKind.Stone)
                {
                    yard.AddResource(StrategyResourceType.Stone, regularAmount);
                }
                else if (resource == StrategyConstructionResourceKind.Planks)
                {
                    yard.AddResource(StrategyResourceType.Planks, regularAmount);
                }
            }

            ClearCarriedConstructionReturnReservation();
        }

        private void RestoreReturnedMaterialReservationOnPile(
            StrategyLooseConstructionResourcePile pile,
            StrategyConstructionResourceKind resource,
            int amount)
        {
            if (pile == null || amount <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return;
            }

            int reservedAmount = GetRestorableCarriedConstructionReservationAmount(resource, amount, out StrategyConstructionSite site);
            if (reservedAmount > 0)
            {
                pile.TryRestoreConstructionReservation(site, resource, reservedAmount);
            }

            ClearCarriedConstructionReturnReservation();
        }

        private bool StoreCarriedMaterialImmediately(
            StrategyConstructionResourceKind resource,
            string reason,
            string fallbackReason)
        {
            int amount = resource == StrategyConstructionResourceKind.Logs
                ? carriedLogAmount
                : resource == StrategyConstructionResourceKind.Stone ? carriedStoneAmount : carriedPlanksAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyStorageYard.TryFindNearestStorageYard(transform.position, out StrategyStorageYard yard))
            {
                StoreReturnedMaterialAtYard(yard, resource, amount);
                if (resource == StrategyConstructionResourceKind.Logs)
                {
                    carriedLogAmount = 0;
                    SetCarriedLogsVisible(false);
                }
                else
                {
                    carriedStoneAmount = 0;
                    SetCarriedStoneVisible(false);
                }

                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                int logs = resource == StrategyConstructionResourceKind.Logs ? amount : 0;
                int stone = resource == StrategyConstructionResourceKind.Stone ? amount : 0;
                int planks = resource == StrategyConstructionResourceKind.Planks ? amount : 0;
                StrategyLooseConstructionResourcePile pile = StrategyLooseConstructionResourcePile.Create(map, cell, transform.position, logs, stone, planks);
                RestoreReturnedMaterialReservationOnPile(pile, resource, amount);
                if (resource == StrategyConstructionResourceKind.Logs)
                {
                    carriedLogAmount = 0;
                    SetCarriedLogsVisible(false);
                }
                else if (resource == StrategyConstructionResourceKind.Stone)
                {
                    carriedStoneAmount = 0;
                    SetCarriedStoneVisible(false);
                }
                else if (resource == StrategyConstructionResourceKind.Planks)
                {
                    carriedPlanksAmount = 0;
                    SetCarriedPlanksVisible(false);
                }

                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Warn(
                    "Logistics",
                    "CarriedResourceDroppedAsLoosePile",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "no_storage_yard"),
                    StrategyDebugLogger.F("origin", cell));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private bool StoreCarriedFoodImmediately(
            StrategyResourceType resource,
            string reason,
            string fallbackReason)
        {
            int amount = resource == StrategyResourceType.Game ? carriedGameAmount : carriedFishAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (StrategyGranary.TryFindNearestGranary(transform.position, out StrategyGranary granary))
            {
                if (resource == StrategyResourceType.Game)
                {
                    granary.AddGame(amount);
                    carriedGameAmount = 0;
                    SetCarriedGameVisible(false);
                }
                else
                {
                    granary.AddFish(amount);
                    carriedFishAmount = 0;
                    SetCarriedFishVisible(false);
                }

                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Granary",
                    "CarriedFoodStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", fallbackReason),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (resource == StrategyResourceType.Game && hunterWorkplace != null)
            {
                hunterWorkplace.AddGame(amount);
                carriedGameAmount = 0;
                SetCarriedGameVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Hunting",
                    "CarriedGameStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "hunter_camp"),
                    StrategyDebugLogger.F("campOrigin", hunterWorkplace.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            if (resource == StrategyResourceType.Fish && fisherWorkplace != null)
            {
                fisherWorkplace.AddFish(amount);
                carriedFishAmount = 0;
                SetCarriedFishVisible(false);
                SetFishingLineVisible(false);
                ResetAfterImmediateCarriedResourceStore();
                StrategyDebugLogger.Info(
                    "Fishing",
                    "CarriedFishStoredImmediately",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("fallback", "fisher_hut"),
                    StrategyDebugLogger.F("hutOrigin", fisherWorkplace.Origin));
                TryStartCarriedResourceReturn("remaining_carried_resource");
                return true;
            }

            return false;
        }

        private void ResetAfterImmediateCarriedResourceStore()
        {
            returnStorageYard = null;
            returnGranary = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.25f, 0.70f);
        }

        private void ScheduleCarriedResourceReturnRetry()
        {
            if (!HasAnyCarriedResource())
            {
                ClearEmptyCarriedResourceReturn("retry_without_resource");
                return;
            }

            returnStorageYard = null;
            returnGranary = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.65f, 1.35f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Warn(
                "Logistics",
                "CarriedResourceReturnRetryScheduled",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("logs", carriedLogAmount),
                StrategyDebugLogger.F("stone", carriedStoneAmount),
                StrategyDebugLogger.F("iron", carriedIronAmount),
                StrategyDebugLogger.F("coal", carriedCoalAmount),
                StrategyDebugLogger.F("clay", carriedClayAmount),
                StrategyDebugLogger.F("planks", carriedPlanksAmount),
                StrategyDebugLogger.F("game", carriedGameAmount),
                StrategyDebugLogger.F("fish", carriedFishAmount),
                StrategyDebugLogger.F("forage", carriedForageAmount),
                StrategyDebugLogger.F("forageResource", carriedForageResource));
        }

        private void ResetLumberWorkToIdle()
        {
            activeTree = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (carriedLogAmount > 0 && TryStartCarriedResourceReturn("lumber_work_reset"))
            {
                return;
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetStoneWorkToIdle()
        {
            if (activeStoneDeposit != null)
            {
                activeStoneDeposit.Release(this);
            }

            activeStoneDeposit = null;
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (carriedStoneAmount > 0 && TryStartCarriedResourceReturn("stone_work_reset"))
            {
                return;
            }

            carriedStoneAmount = 0;
            SetCarriedStoneVisible(false);
            stoneWorkCooldown = Random.Range(2.0f, 4.0f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetStorageWorkToIdle(bool storeCarriedLogs = false)
        {
            ReleaseActiveStorageWorkReservations();
            ClearActiveStorageSources();
            if (storeCarriedLogs && TryStartStorageCarriedReturn("storage_work_cancelled"))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedCoalAmount = 0;
            carriedClayAmount = 0;
            carriedPlanksAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedCoalVisible(false);
            SetCarriedClayVisible(false);
            SetCarriedPlanksVisible(false);
            logisticsWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetGranaryWorkToIdle(bool storeCarriedFood = false)
        {
            if (activeGameSource != null)
            {
                activeGameSource.ReleaseStoredGameReservation(this);
            }

            if (activeFishSource != null)
            {
                activeFishSource.ReleaseStoredFishReservation(this);
            }

            if (activeLooseFoodSource != null)
            {
                activeLooseFoodSource.ReleaseReservation(this);
            }

            if (activeForageFoodSource != null)
            {
                activeForageFoodSource.ReleaseStoredForageReservation(this);
            }

            activeGameSource = null;
            activeFishSource = null;
            activeForageFoodSource = null;
            activeLooseFoodSource = null;
            activeGranaryDeliveryTarget = null;
            if (storeCarriedFood
                && (carriedGameAmount > 0 || carriedFishAmount > 0 || carriedForageAmount > 0)
                && TryStartCarriedResourceReturn("granary_work_cancelled"))
            {
                return;
            }

            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            logisticsWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ResetHouseholdFoodWorkToIdle(bool storeCarriedFood, string reason = "reset")
        {
            ReleaseActiveHouseholdFoodReservation();

            StoreCarriedHouseholdPotteryOnCancel(storeCarriedFood, reason);

            int carriedAmount = GetCarriedHouseholdFoodAmount();
            if (storeCarriedFood
                && carriedHouseholdFoodResource != StrategyResourceType.None
                && carriedAmount > 0
                && home != null
                && home.Resources != null)
            {
                home.Resources.AddResource(carriedHouseholdFoodResource, carriedAmount);
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholderFoodStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedHouseholdFoodResource),
                    StrategyDebugLogger.F("amount", carriedAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            ClearCarriedHouseholdFood();
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            householdFoodWorkCooldown = Random.Range(
                HouseholdFoodPickupRetryCooldownMin,
                HouseholdFoodPickupRetryCooldownMax);
            waitTimer = Random.Range(0.30f, 0.85f);
        }

        private void ResetConstructionWorkToIdle()
        {
            CaptureCarriedConstructionReturnReservation();
            ReleaseActiveConstructionPickupReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            if ((carriedLogAmount > 0 || carriedStoneAmount > 0 || carriedPlanksAmount > 0)
                && TryStartCarriedResourceReturn("construction_work_cancelled"))
            {
                return;
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedPlanksAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedPlanksVisible(false);
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void ReleaseActiveConstructionPickupReservation()
        {
            if (activeConstructionSource != null)
            {
                activeConstructionSource.ReleaseConstructionPickupReservation(this);
            }
        }

        private void ResetHunterWorkToIdle(bool releaseReservation)
        {
            if (releaseReservation && activeHuntTarget != null)
            {
                activeHuntTarget.ReleaseHuntReservation(this);
            }

            activeHuntTarget = null;
            bowShotReleased = false;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (carriedGameAmount > 0 && TryStartCarriedResourceReturn("hunter_work_cancelled"))
            {
                return;
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
            UseIdleSprite();
            huntingWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }
    }
}
