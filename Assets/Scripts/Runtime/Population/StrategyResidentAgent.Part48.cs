using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float HouseholdPotteryDebugLogCooldownSeconds = 6f;

        private StrategyStorageYard activeHouseholdPotteryYard;
        private StrategyPlacedBuilding activeHouseholdPotteryHome;
        private float nextHouseholdPotteryDebugLogTime;

        private bool TryStartHouseholdPotteryDelivery()
        {
            if (home == null || home.Resources == null)
            {
                return false;
            }

            float dailyNeed = CalculateHomeDailyRationNeed();
            int demand = home.Resources.GetPotteryDemandForCooking(dailyNeed);
            if (demand <= 0 || !IsHouseholder)
            {
                return false;
            }

            if (!CanStartGardenDuty())
            {
                LogHouseholderPotteryPickupSkipped("not_home_duty_state", demand, dailyNeed);
                return false;
            }

            if (constructionSite != null)
            {
                LogHouseholderPotteryPickupSkipped("construction_assignment", demand, dailyNeed);
                return false;
            }

            if (!CanWork)
            {
                LogHouseholderPotteryPickupSkipped("cannot_work", demand, dailyNeed);
                return false;
            }

            if (HasExternalWorkplace)
            {
                LogHouseholderPotteryPickupSkipped("external_workplace", demand, dailyNeed);
                return false;
            }

            if (householdFoodWorkCooldown > 0f)
            {
                LogHouseholderPotteryPickupSkipped("cooldown", demand, dailyNeed);
                return false;
            }

            if (!StrategyDayNightCycleController.IsHouseholdOutdoorWorkTime)
            {
                LogHouseholderPotteryPickupSkipped("outside_work_time", demand, dailyNeed);
                return false;
            }

            if (HasAnyCarriedResource())
            {
                LogHouseholderPotteryPickupSkipped("already_carrying", demand, dailyNeed);
                return false;
            }

            if (!StrategyStorageYard.TryReserveNearestHouseholdPottery(
                    home,
                    this,
                    out StrategyStorageYard yard,
                    out int amount,
                    out Vector2Int pickupCell))
            {
                householdFoodWorkCooldown = Random.Range(
                    HouseholdFoodPickupRetryCooldownMin,
                    HouseholdFoodPickupRetryCooldownMax);
                LogHouseholderPotteryPickupSkipped("no_reservable_pottery", demand, dailyNeed);
                return false;
            }

            if (!TryBuildPathTo(pickupCell))
            {
                yard.ReleaseHouseholdPotteryReservation(this);
                householdFoodWorkCooldown = Random.Range(
                    HouseholdFoodPickupRetryCooldownMin,
                    HouseholdFoodPickupRetryCooldownMax);
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderPotteryPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin),
                    StrategyDebugLogger.F("houseOrigin", home.Origin),
                StrategyDebugLogger.F("reason", "no_pickup_path"));
                return false;
            }

            activeHouseholdPotteryYard = yard;
            activeHouseholdPotteryHome = home;
            activity = ResidentActivity.MovingToHouseholdPotteryPickup;
            hasTarget = true;
            waitTimer = Random.Range(0.03f, 0.12f);
            householdFoodWorkCooldown = Random.Range(
                HouseholdFoodPickupCooldownMin,
                HouseholdFoodPickupCooldownMax);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderPotteryPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", yard.Origin),
                StrategyDebugLogger.F("houseOrigin", home.Origin),
                StrategyDebugLogger.F("houseDemand", demand),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("pickupCell", pickupCell));
            return true;
        }

        private void StartPickingUpHouseholdPottery()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (activeHouseholdPotteryYard == null || activeHouseholdPotteryHome == null)
            {
                ResetHouseholdPotteryDeliveryToIdle(false, "missing_pickup_target");
                return;
            }

            activity = ResidentActivity.PickingUpHouseholdPottery;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeHouseholdPotteryYard.FootprintBounds.center);
        }

        private void UpdatePickingUpHouseholdPottery()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeHouseholdPotteryYard == null
                || activeHouseholdPotteryHome == null
                || !TryBuildPathToHouseDropoff(activeHouseholdPotteryHome))
            {
                activeHouseholdPotteryYard?.ReleaseHouseholdPotteryReservation(this);
                ResetHouseholdPotteryDeliveryToIdle(false, "no_house_path");
                return;
            }

            Vector2Int yardOrigin = activeHouseholdPotteryYard.Origin;
            if (!activeHouseholdPotteryYard.TryTakeReservedHouseholdPottery(
                    this,
                    out StrategyPlacedBuilding targetHouse,
                    out carriedPotteryAmount)
                || carriedPotteryAmount <= 0)
            {
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderPotteryPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("yardOrigin", yardOrigin),
                    StrategyDebugLogger.F("reason", "take_failed"));
                ResetHouseholdPotteryDeliveryToIdle(false, "take_failed");
                return;
            }

            activeHouseholdPotteryYard = null;
            activeHouseholdPotteryHome = targetHouse;
            activity = ResidentActivity.CarryingPotteryToHouse;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedPotteryVisible(true);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderPotteryPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("yardOrigin", yardOrigin),
                StrategyDebugLogger.F("houseOrigin", targetHouse != null ? targetHouse.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("amount", carriedPotteryAmount));
        }

        private void StartDepositingHouseholdPottery()
        {
            if (activeHouseholdPotteryHome == null || carriedPotteryAmount <= 0)
            {
                ResetHouseholdPotteryDeliveryToIdle(true, "missing_deposit_target");
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingHouseholdPottery;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            FaceWorldPoint(activeHouseholdPotteryHome.FootprintBounds.center);
            SetCarriedPotteryVisible(true);
        }

        private void UpdateDepositingHouseholdPottery()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedPotteryVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int amount = carriedPotteryAmount;
            StrategyPlacedBuilding targetHouse = activeHouseholdPotteryHome;
            if (targetHouse != null && targetHouse.Resources != null && amount > 0)
            {
                targetHouse.Resources.AddResource(StrategyResourceType.Pottery, amount);
                StrategyWorldEffectAnimator.SpawnResourcePlaced(
                    StrategyResourceType.Pottery,
                    targetHouse.HomeAnchor,
                    spriteRenderer != null ? spriteRenderer.sortingOrder : 0,
                    amount);
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholderPotteryDelivered",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("houseOrigin", targetHouse.Origin),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("housePottery", targetHouse.Resources.GetPotteryAmount()));
            }

            carriedPotteryAmount = 0;
            SetCarriedPotteryVisible(false);
            activeHouseholdPotteryHome = null;
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            householdFoodWorkCooldown = Random.Range(
                HouseholdFoodPickupRetryCooldownMin,
                HouseholdFoodPickupRetryCooldownMax);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void ResetHouseholdPotteryDeliveryToIdle(bool storeCarried, string reason = "reset")
        {
            LogHouseholderPotteryPickupCancelled(reason, storeCarried);
            activeHouseholdPotteryYard?.ReleaseHouseholdPotteryReservation(this);
            activeHouseholdPotteryYard = null;
            if (storeCarried && carriedPotteryAmount > 0)
            {
                StrategyPlacedBuilding targetHouse = activeHouseholdPotteryHome != null
                    ? activeHouseholdPotteryHome
                    : home;
                if (targetHouse != null && targetHouse.Resources != null)
                {
                    targetHouse.Resources.AddResource(StrategyResourceType.Pottery, carriedPotteryAmount);
                    StrategyDebugLogger.Info(
                        "Household",
                        "HouseholderPotteryStoredOnCancel",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("houseOrigin", targetHouse.Origin),
                        StrategyDebugLogger.F("amount", carriedPotteryAmount));
                    carriedPotteryAmount = 0;
                    SetCarriedPotteryVisible(false);
                    activeHouseholdPotteryHome = null;
                }
                else if (StoreCarriedPotteryImmediately("household_pottery_cancelled", "storage_return"))
                {
                    activeHouseholdPotteryHome = null;
                    return;
                }
            }

            activeHouseholdPotteryHome = null;
            carriedPotteryAmount = 0;
            SetCarriedPotteryVisible(false);
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
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void StoreCarriedHouseholdPotteryOnCancel(bool storeCarried, string reason = "cancelled")
        {
            LogHouseholderPotteryPickupCancelled(reason, storeCarried);
            activeHouseholdPotteryYard?.ReleaseHouseholdPotteryReservation(this);
            activeHouseholdPotteryYard = null;
            if (storeCarried && carriedPotteryAmount > 0 && home != null && home.Resources != null)
            {
                home.Resources.AddResource(StrategyResourceType.Pottery, carriedPotteryAmount);
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholderPotteryStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("amount", carriedPotteryAmount),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }

            activeHouseholdPotteryHome = null;
            carriedPotteryAmount = 0;
            SetCarriedPotteryVisible(false);
        }

        private void LogHouseholderPotteryPickupSkipped(string reason, int demand, float dailyNeed)
        {
            if (Time.time < nextHouseholdPotteryDebugLogTime || home == null || home.Resources == null)
            {
                return;
            }

            nextHouseholdPotteryDebugLogTime = Time.time + HouseholdPotteryDebugLogCooldownSeconds;
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderPotteryPickupSkipped",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("activity", activity),
                StrategyDebugLogger.F("demand", demand),
                StrategyDebugLogger.F("dailyNeed", dailyNeed),
                StrategyDebugLogger.F("housePottery", home.Resources.GetPotteryAmount()),
                StrategyDebugLogger.F("preparedRations", home.Resources.GetPreparedDishRations()),
                StrategyDebugLogger.F("ingredientRations", home.Resources.GetTotalIngredientRationValue()),
                StrategyDebugLogger.F("storageAvailable", StrategyStorageYard.CountAvailableHouseholdPottery()),
                StrategyDebugLogger.F("cooldown", householdFoodWorkCooldown));
        }

        private void LogHouseholderPotteryPickupCancelled(string reason, bool storeCarried)
        {
            if (activeHouseholdPotteryYard == null
                && activeHouseholdPotteryHome == null
                && carriedPotteryAmount <= 0)
            {
                return;
            }

            StrategyPlacedBuilding targetHouse = activeHouseholdPotteryHome != null
                ? activeHouseholdPotteryHome
                : home;
            StrategyDebugLogger.Warn(
                "Household",
                "HouseholderPotteryPickupCancelled",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("activity", activity),
                StrategyDebugLogger.F("storeCarried", storeCarried),
                StrategyDebugLogger.F("yardOrigin", activeHouseholdPotteryYard != null ? activeHouseholdPotteryYard.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("houseOrigin", targetHouse != null ? targetHouse.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("carriedAmount", carriedPotteryAmount));
        }

        private bool TryBuildPathToHouseDropoff(StrategyPlacedBuilding targetHouse)
        {
            if (map == null || targetHouse == null)
            {
                return false;
            }

            Vector2Int origin = targetHouse.Origin;
            Vector2Int footprint = targetHouse.Footprint;
            for (int radius = 1; radius <= IdleRadius; radius++)
            {
                int minX = origin.x - radius;
                int maxX = origin.x + footprint.x + radius - 1;
                int minY = origin.y - radius;
                int maxY = origin.y + footprint.y + radius - 1;
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (x != minX && x != maxX && y != minY && y != maxY)
                        {
                            continue;
                        }

                        Vector2Int candidate = new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && TryBuildPathTo(candidate))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
