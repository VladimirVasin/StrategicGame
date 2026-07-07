using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float HouseholdLogsDebugLogCooldownSeconds = 8f;

        private StrategyStorageYard activeHouseholdLogYard;
        private StrategyStarterCaravanCart activeHouseholdLogCart;
        private StrategyPlacedBuilding activeHouseholdLogHome;
        private float householdLogWorkCooldown;
        private float nextHouseholdLogsDebugLogTime;

        private bool TryStartHouseholdLogsDelivery()
        {
            if (home == null || home.Resources == null || !IsHouseholder)
            {
                return false;
            }

            int demand = home.Warmth != null
                ? home.Warmth.CurrentWinterLogDemand
                : StrategyHouseWarmthState.GetWinterLogDemandForHouse(home);
            if (demand <= 0)
            {
                return false;
            }

            if (!CanStartGardenDuty()
                || constructionSite != null
                || !CanWork
                || HasExternalWorkplace
                || householdLogWorkCooldown > 0f
                || !StrategyDayNightCycleController.IsHouseholdOutdoorWorkTime
                || HasAnyCarriedResource())
            {
                LogHouseholderLogsPickupSkipped("blocked", demand);
                return false;
            }

            if (!TryReserveHouseholdLogsPickupSource(out int amount, out Vector2Int pickupCell))
            {
                householdLogWorkCooldown = Random.Range(
                    HouseholdFoodPickupRetryCooldownMin,
                    HouseholdFoodPickupRetryCooldownMax);
                LogHouseholderLogsPickupSkipped("no_reservable_logs", demand);
                return false;
            }

            if (!TryBuildPathTo(pickupCell))
            {
                Vector2Int sourceOrigin = GetActiveHouseholdLogsSourceOrigin();
                string sourceKind = GetActiveHouseholdLogsSourceKind();
                ReleaseActiveHouseholdLogsReservation();
                householdLogWorkCooldown = Random.Range(
                    HouseholdFoodPickupRetryCooldownMin,
                    HouseholdFoodPickupRetryCooldownMax);
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderLogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("source", sourceKind),
                    StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                    StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activeHouseholdLogHome = home;
            activity = ResidentActivity.MovingToHouseholdLogsPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.20f);
            householdLogWorkCooldown = Random.Range(
                HouseholdFoodPickupCooldownMin,
                HouseholdFoodPickupCooldownMax);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderLogsPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("houseLogs", home.Resources.GetLogsAmount()),
                StrategyDebugLogger.F("targetLogs", StrategyHouseWarmthState.WinterHouseLogReserveTarget),
                StrategyDebugLogger.F("source", GetActiveHouseholdLogsSourceKind()),
                StrategyDebugLogger.F("sourceOrigin", GetActiveHouseholdLogsSourceOrigin()),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("pickupCell", pickupCell));
            return true;
        }

        private bool TryReserveHouseholdLogsPickupSource(out int amount, out Vector2Int pickupCell)
        {
            amount = 0;
            pickupCell = default;
            ReleaseActiveHouseholdLogsReservation();

            if (StrategyStorageYard.TryReserveNearestHouseholdLogs(
                    home,
                    this,
                    out StrategyStorageYard yard,
                    out amount,
                    out pickupCell))
            {
                activeHouseholdLogYard = yard;
                return true;
            }

            if (StrategyStarterCaravanCart.TryReserveNearestHouseholdLogs(
                    home,
                    this,
                    out StrategyStarterCaravanCart cart,
                    out amount,
                    out pickupCell))
            {
                activeHouseholdLogCart = cart;
                return true;
            }

            return false;
        }

        private void StartPickingUpHouseholdLogs()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (!HasActiveHouseholdLogsSource() || activeHouseholdLogHome == null)
            {
                ResetHouseholdLogsDeliveryToIdle(false, "missing_pickup_target");
                return;
            }

            activity = ResidentActivity.PickingUpHouseholdLogs;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(GetActiveHouseholdLogsSourceBounds().center);
        }

        private void UpdatePickingUpHouseholdLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeHouseholdLogHome == null || !TryBuildPathToHouseDropoff(activeHouseholdLogHome))
            {
                ResetHouseholdLogsDeliveryToIdle(false, "no_house_path");
                return;
            }

            Vector2Int sourceOrigin = GetActiveHouseholdLogsSourceOrigin();
            StrategyPlacedBuilding targetHouse = activeHouseholdLogHome;
            if (!TryTakeActiveHouseholdLogsReservation(out int amount) || amount <= 0)
            {
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderLogsPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                    StrategyDebugLogger.F("reason", "take_failed"));
                ResetHouseholdLogsDeliveryToIdle(false, "take_failed");
                return;
            }

            carriedLogAmount = amount;
            activeHouseholdLogHome = targetHouse;
            activity = ResidentActivity.CarryingLogsToHouse;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedLogsVisible(true);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderLogsPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("houseOrigin", targetHouse != null ? targetHouse.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("amount", carriedLogAmount));
        }

        private void StartDepositingHouseholdLogs()
        {
            if (activeHouseholdLogHome == null || carriedLogAmount <= 0)
            {
                ResetHouseholdLogsDeliveryToIdle(true, "missing_deposit_target");
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingHouseholdLogs;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            FaceWorldPoint(activeHouseholdLogHome.FootprintBounds.center);
            SetCarriedLogsVisible(true);
        }

        private void UpdateDepositingHouseholdLogs()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedLogsVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int amount = carriedLogAmount;
            StrategyPlacedBuilding targetHouse = activeHouseholdLogHome;
            if (targetHouse != null && targetHouse.Resources != null && amount > 0)
            {
                targetHouse.Resources.AddResource(StrategyResourceType.Logs, amount);
                StrategyWorldEffectAnimator.SpawnResourcePlaced(
                    StrategyResourceType.Logs,
                    targetHouse.HomeAnchor,
                    spriteRenderer != null ? spriteRenderer.sortingOrder : 0,
                    amount);
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholderLogsDelivered",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("houseOrigin", targetHouse.Origin),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("houseLogs", targetHouse.Resources.GetLogsAmount()));
            }

            carriedLogAmount = 0;
            SetCarriedLogsVisible(false);
            activeHouseholdLogHome = null;
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            householdLogWorkCooldown = Random.Range(
                HouseholdFoodPickupRetryCooldownMin,
                HouseholdFoodPickupRetryCooldownMax);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private bool TryTakeActiveHouseholdLogsReservation(out int amount)
        {
            amount = 0;
            if (activeHouseholdLogYard != null)
            {
                bool taken = activeHouseholdLogYard.TryTakeReservedHouseholdLogs(
                    this,
                    out StrategyPlacedBuilding house,
                    out amount);
                if (!taken)
                {
                    activeHouseholdLogYard.ReleaseHouseholdLogsReservation(this);
                }

                activeHouseholdLogYard = null;
                activeHouseholdLogHome = house != null ? house : activeHouseholdLogHome;
                return taken;
            }

            if (activeHouseholdLogCart != null)
            {
                bool taken = activeHouseholdLogCart.TryTakeReservedHouseholdLogs(this, out amount);
                if (!taken)
                {
                    activeHouseholdLogCart.ReleaseHouseholdLogsReservation(this);
                }

                activeHouseholdLogCart = null;
                return taken;
            }

            return false;
        }

        private void ResetHouseholdLogsDeliveryToIdle(bool storeCarried, string reason = "reset")
        {
            StoreCarriedHouseholdLogsOnCancel(storeCarried, reason);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            householdLogWorkCooldown = Random.Range(
                HouseholdFoodPickupRetryCooldownMin,
                HouseholdFoodPickupRetryCooldownMax);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void StoreCarriedHouseholdLogsOnCancel(bool storeCarried, string reason = "cancelled")
        {
            bool householdLogsActive = IsHouseholdLogsActivity(activity)
                || activeHouseholdLogYard != null
                || activeHouseholdLogCart != null
                || activeHouseholdLogHome != null;
            if (!householdLogsActive)
            {
                return;
            }

            ReleaseActiveHouseholdLogsReservation();
            if (carriedLogAmount > 0)
            {
                StrategyPlacedBuilding targetHouse = activeHouseholdLogHome != null ? activeHouseholdLogHome : home;
                if (storeCarried && targetHouse != null && targetHouse.Resources != null)
                {
                    targetHouse.Resources.AddResource(StrategyResourceType.Logs, carriedLogAmount);
                    StrategyDebugLogger.Info(
                        "Household",
                        "HouseholderLogsStoredOnCancel",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", reason),
                        StrategyDebugLogger.F("amount", carriedLogAmount),
                        StrategyDebugLogger.F("homeOrigin", targetHouse.Origin));
                    carriedLogAmount = 0;
                    SetCarriedLogsVisible(false);
                }
                else
                {
                    StoreCarriedMaterialImmediately(
                        StrategyConstructionResourceKind.Logs,
                        "household_logs_cancelled",
                        reason);
                }
            }

            activeHouseholdLogHome = null;
            activeHouseholdLogYard = null;
            activeHouseholdLogCart = null;
            if (carriedLogAmount <= 0)
            {
                SetCarriedLogsVisible(false);
            }
        }

        private void ReleaseActiveHouseholdLogsReservation()
        {
            activeHouseholdLogYard?.ReleaseHouseholdLogsReservation(this);
            activeHouseholdLogCart?.ReleaseHouseholdLogsReservation(this);
            activeHouseholdLogYard = null;
            activeHouseholdLogCart = null;
        }

        private bool HasActiveHouseholdLogsSource()
        {
            return activeHouseholdLogYard != null || activeHouseholdLogCart != null;
        }

        private Bounds GetActiveHouseholdLogsSourceBounds()
        {
            if (activeHouseholdLogYard != null)
            {
                return activeHouseholdLogYard.FootprintBounds;
            }

            return activeHouseholdLogCart != null
                ? activeHouseholdLogCart.FootprintBounds
                : new Bounds(transform.position, Vector3.one);
        }

        private Vector2Int GetActiveHouseholdLogsSourceOrigin()
        {
            if (activeHouseholdLogYard != null)
            {
                return activeHouseholdLogYard.Origin;
            }

            return activeHouseholdLogCart != null ? activeHouseholdLogCart.Origin : Vector2Int.zero;
        }

        private string GetActiveHouseholdLogsSourceKind()
        {
            if (activeHouseholdLogYard != null)
            {
                return "StorageYard";
            }

            return activeHouseholdLogCart != null ? "CaravanCart" : "None";
        }

        private void LogHouseholderLogsPickupSkipped(string reason, int demand)
        {
            if (Time.time < nextHouseholdLogsDebugLogTime || home == null || home.Resources == null)
            {
                return;
            }

            nextHouseholdLogsDebugLogTime = Time.time + HouseholdLogsDebugLogCooldownSeconds;
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderLogsPickupSkipped",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("activity", activity),
                StrategyDebugLogger.F("demand", demand),
                StrategyDebugLogger.F("houseLogs", home.Resources.GetLogsAmount()),
                StrategyDebugLogger.F("storageAvailable", StrategyStorageYard.CountAvailableHouseholdLogs()),
                StrategyDebugLogger.F("cartAvailable", StrategyStarterCaravanCart.GetTotalAvailableHouseholdLogs()),
                StrategyDebugLogger.F("cooldown", householdLogWorkCooldown));
        }

        private static bool IsHouseholdLogsActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToHouseholdLogsPickup
                || residentActivity == ResidentActivity.PickingUpHouseholdLogs
                || residentActivity == ResidentActivity.CarryingLogsToHouse
                || residentActivity == ResidentActivity.DepositingHouseholdLogs;
        }
    }
}
