using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void CompleteStoneDelivery()
        {
            activeStoneDeposit = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            stoneWorkCooldown = Random.Range(3.4f, 6.8f);
            waitTimer = Random.Range(0.45f, 1.1f);
        }

        private void CompleteStorageDelivery()
        {
            activeLogSource = null;
            activeStoneSource = null;
            activeIronSource = null;
            activePlanksSource = null;
            activePotterySource = null;
            activeHouseholdPotteryYard = null;
            activeHouseholdPotteryHome = null;
            activeClaySource = null;
            ClearProductionInputDelivery();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            logisticsWorkCooldown = Random.Range(2.2f, 4.8f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void CompleteGranaryDelivery()
        {
            activeGameSource = null;
            activeFishSource = null;
            activeGranaryDeliveryTarget = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            logisticsWorkCooldown = Random.Range(2.2f, 4.8f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void CompleteConstructionDelivery()
        {
            activeConstructionSource = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            waitTimer = constructionSite != null && constructionSite.ResourcesComplete
                ? Random.Range(0.05f, 0.22f)
                : Random.Range(0.20f, 0.55f);
        }

        private void CompleteHunterDelivery()
        {
            activeHuntTarget = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            huntingWorkCooldown = Random.Range(3.5f, 7.0f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void CompleteFisherDelivery()
        {
            activeFishTarget = null;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetFishingLineVisible(false);
            UseIdleSprite();
            activity = ResidentActivity.Idle;
            fishingWorkCooldown = Random.Range(3.5f, 7.0f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private bool HasAnyCarriedResource()
        {
            return carriedLogAmount > 0
                || carriedStoneAmount > 0
                || carriedIronAmount > 0
                || carriedCoalAmount > 0
                || carriedClayAmount > 0
                || carriedPlanksAmount > 0
                || carriedPotteryAmount > 0
                || carriedGameAmount > 0
                || carriedFishAmount > 0
                || carriedForageAmount > 0
                || GetCarriedHouseholdFoodAmount() > 0;
        }

        private void CaptureCarriedConstructionReturnReservation()
        {
            if (constructionSite == null || constructionSite.IsCompleted)
            {
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Logs && carriedLogAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Logs;
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Stone && carriedStoneAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Stone;
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Planks && carriedPlanksAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Planks;
                return;
            }

            if (carriedLogAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Logs;
                return;
            }

            if (carriedStoneAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Stone;
                return;
            }

            if (carriedPlanksAmount > 0)
            {
                carriedConstructionReturnSite = constructionSite;
                carriedConstructionReturnResource = StrategyConstructionResourceKind.Planks;
            }
        }

        private int GetRestorableCarriedConstructionReservationAmount(
            StrategyConstructionResourceKind resource,
            int amount,
            out StrategyConstructionSite site)
        {
            site = null;
            if (amount <= 0
                || carriedConstructionReturnSite == null
                || carriedConstructionReturnResource != resource
                || resource == StrategyConstructionResourceKind.None)
            {
                return 0;
            }

            StrategyConstructionSite candidate = carriedConstructionReturnSite;
            if (candidate == null || candidate.IsCompleted)
            {
                ClearCarriedConstructionReturnReservation();
                return 0;
            }

            int needed = resource switch
            {
                StrategyConstructionResourceKind.Logs => candidate.NeededLogs,
                StrategyConstructionResourceKind.Stone => candidate.NeededStone,
                StrategyConstructionResourceKind.Planks => candidate.NeededPlanks,
                _ => 0
            };
            if (needed <= 0)
            {
                ClearCarriedConstructionReturnReservation();
                return 0;
            }

            site = candidate;
            return Mathf.Min(amount, needed);
        }

        private void ClearCarriedConstructionReturnReservation()
        {
            carriedConstructionReturnSite = null;
            carriedConstructionReturnResource = StrategyConstructionResourceKind.None;
        }

        private void ClearEmptyCarriedResourceReturn(string reason)
        {
            returnStorageYard = null;
            returnGranary = null;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            ClearCarriedConstructionReturnReservation();
            if (IsReturningCarriedResourceActivity(activity)
                || activity == ResidentActivity.ReturningCoalToStorage
                || activity == ResidentActivity.ReturningClayToStorage
                || activity == ResidentActivity.ReturningPlanksToStorage
                || activity == ResidentActivity.ReturningPotteryToStorage)
            {
                activity = GetRestingActivity();
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.25f, 0.70f);
            StrategyDebugLogger.Info(
                "Logistics",
                "EmptyCarriedResourceReturnCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("reason", reason));
        }

        private bool TryStartCarriedResourceReturn(string reason, bool restartCurrentReturn = false)
        {
            if (deathRequested)
            {
                return false;
            }

            if (!HasAnyCarriedResource())
            {
                if (IsReturningCarriedResourceActivity(activity) || restartCurrentReturn)
                {
                    ClearEmptyCarriedResourceReturn(reason);
                }
                else
                {
                    ClearCarriedConstructionReturnReservation();
                }

                return false;
            }

            if (IsReturningCarriedResourceActivity(activity) && !restartCurrentReturn)
            {
                return true;
            }

            if (restartCurrentReturn)
            {
                returnStorageYard = null;
                returnGranary = null;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
            }

            if (carriedLogAmount > 0)
            {
                return TryStartMaterialReturn(StrategyConstructionResourceKind.Logs, reason);
            }

            if (carriedStoneAmount > 0)
            {
                return TryStartMaterialReturn(StrategyConstructionResourceKind.Stone, reason);
            }

            if (carriedPlanksAmount > 0)
            {
                return TryStartPlanksReturn(reason);
            }

            if (carriedIronAmount > 0)
            {
                return TryStartIronReturn(reason);
            }

            if (carriedCoalAmount > 0)
            {
                return TryStartCoalReturn(reason);
            }

            if (carriedClayAmount > 0)
            {
                return TryStartClayReturn(reason);
            }

            if (carriedPotteryAmount > 0)
            {
                return TryStartPotteryReturn(reason);
            }

            if (carriedGameAmount > 0)
            {
                return TryStartFoodReturn(StrategyResourceType.Game, reason);
            }

            if (carriedFishAmount > 0)
            {
                return TryStartFoodReturn(StrategyResourceType.Fish, reason);
            }

            return false;
        }

        private bool TryStartMaterialReturn(StrategyConstructionResourceKind resource, string reason)
        {
            if (resource != StrategyConstructionResourceKind.Logs
                && resource != StrategyConstructionResourceKind.Stone)
            {
                return false;
            }

            int amount = resource == StrategyConstructionResourceKind.Logs
                ? carriedLogAmount
                : carriedStoneAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyStorageYard.TryFindNearestDropoff(transform.position, out StrategyStorageYard yard, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = yard;
                returnGranary = null;
                activity = resource == StrategyConstructionResourceKind.Logs
                    ? ResidentActivity.ReturningLogsToStorage
                    : ResidentActivity.ReturningStoneToStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedLogsVisible(carriedLogAmount > 0);
                SetCarriedStoneVisible(carriedStoneAmount > 0);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Logistics",
                    "CarriedResourceReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedMaterialImmediately(resource, reason, "no_reachable_storage");
        }

        private bool TryStartFoodReturn(StrategyResourceType resource, string reason)
        {
            if (resource != StrategyResourceType.Game && resource != StrategyResourceType.Fish)
            {
                return false;
            }

            int amount = resource == StrategyResourceType.Game ? carriedGameAmount : carriedFishAmount;
            if (amount <= 0)
            {
                return false;
            }

            if (!returnCarriedResourcesImmediately
                && StrategyGranary.TryFindNearestDropoff(transform.position, out StrategyGranary granary, out Vector2Int dropoffCell)
                && TryBuildPathTo(dropoffCell))
            {
                returnStorageYard = null;
                returnGranary = granary;
                activity = resource == StrategyResourceType.Game
                    ? ResidentActivity.ReturningGameToGranary
                    : ResidentActivity.ReturningFishToGranary;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.12f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                SetCarriedGameVisible(carriedGameAmount > 0);
                SetCarriedFishVisible(carriedFishAmount > 0);
                SetFishingLineVisible(false);
                UseIdleSprite();
                StrategyDebugLogger.Info(
                    "Granary",
                    "CarriedFoodReturnStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell));
                return true;
            }

            return StoreCarriedFoodImmediately(resource, reason, "no_reachable_granary");
        }

    }
}
