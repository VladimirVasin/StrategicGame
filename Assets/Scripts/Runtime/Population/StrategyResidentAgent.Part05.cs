using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private bool CanStartGardenDuty()
        {
            return activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold;
        }

        private bool TryStartGardenTask()
        {
            if (!CanStartGardenDuty()
                || home == null
                || !IsHouseholder
                || constructionSite != null
                || !CanWork
                || gardenWorkCooldown > 0f
                || !home.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out StrategyBuildingUpgrade garden))
            {
                return false;
            }

            if (HasExternalWorkplace)
            {
                PrepareHouseholderHomeDuty();
                if (HasExternalWorkplace)
                {
                    gardenWorkCooldown = Random.Range(GardenWorkRetryCooldownMin, GardenWorkRetryCooldownMax);
                    StrategyDebugLogger.Warn(
                        "Population",
                        "HouseholderGardenBlocked",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("homeOrigin", home.Origin),
                        StrategyDebugLogger.F("reason", "external_workplace"));
                    return false;
                }
            }

            if (!TryFindGardenWorkCell(garden, out Vector2Int workCell))
            {
                gardenWorkCooldown = Random.Range(GardenWorkRetryCooldownMin, GardenWorkRetryCooldownMax);
                StrategyDebugLogger.Warn(
                    "Population",
                    "HouseholderGardenBlocked",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("gardenOrigin", garden.Origin),
                    StrategyDebugLogger.F("reason", "no_work_cell"));
                return false;
            }

            activeGarden = garden;
            activity = ResidentActivity.MovingToGarden;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.25f);
                gardenWorkCooldown = Random.Range(GardenWorkCooldownMin, GardenWorkCooldownMax);
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholderGardenWorkStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("gardenOrigin", garden.Origin),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeGarden = null;
            activity = GetRestingActivity();
            gardenWorkCooldown = Random.Range(GardenWorkRetryCooldownMin, GardenWorkRetryCooldownMax);
            StrategyDebugLogger.Warn(
                "Population",
                "HouseholderGardenBlocked",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("gardenOrigin", garden.Origin),
                StrategyDebugLogger.F("workCell", workCell),
                StrategyDebugLogger.F("reason", "no_path"));
            return false;
        }

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

        private float CalculateHomeDailyRationNeed()
        {
            if (home == null)
            {
                return 0f;
            }

            float total = 0f;
            IReadOnlyList<StrategyResidentAgent> residents = home.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.Home == home && !resident.IsPendingRefugee)
                {
                    total += resident.DailyRationNeed;
                }
            }

            return total;
        }

        private bool TryStartLumberTask()
        {
            if (activity != ResidentActivity.Idle
                || workplace == null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || lumberWorkCooldown > 0f)
            {
                return false;
            }

            if (workplace.TryReserveProcessableWood(this, out StrategyForestryTree wood)
                && TryMoveToProcessableWood(wood))
            {
                return true;
            }

            wood?.Release(this);

            if (workplace.TryReserveMatureTree(this, out StrategyForestryTree tree)
                && TryMoveToTree(tree))
            {
                return true;
            }

            tree?.Release(this);

            if (TryStartPlantingTask())
            {
                return true;
            }

            lumberWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryStartStoneTask()
        {
            if (activity != ResidentActivity.Idle
                || stoneWorkplace == null
                || workplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || stoneWorkCooldown > 0f)
            {
                return false;
            }

            if (stoneWorkplace.TryReserveStoneDeposit(this, out StrategyStoneDeposit deposit)
                && TryMoveToStoneDeposit(deposit))
            {
                return true;
            }

            deposit?.Release(this);
            stoneWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryStartStorageTask()
        {
            if (activity != ResidentActivity.Idle
                || !HasStorageHaulerRole
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || sawmillWorkplace != null
                || kilnWorkplace != null
                || forgeWorkplace != null
                || !CanWork
                || logisticsWorkCooldown > 0f)
            {
                return false;
            }

            if (!TrySelectStorageHaulerYard())
            {
                if (TryStartGranaryTask() || TryStartHaulerConstructionDeliveryTask()) return true;
                logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
                return false;
            }

            bool stoneFirst = storageWorkplace.ShouldPrioritizeStonePickup();
            if (stoneFirst && TryStartStorageStonePickup())
            {
                return true;
            }

            if (TryStartProductionInputDelivery())
            {
                return true;
            }

            if (TryStartStorageLogPickup())
            {
                return true;
            }

            if (!stoneFirst && TryStartStorageStonePickup())
            {
                return true;
            }

            if (TryStartStorageIronPickup())
            {
                return true;
            }

            if (TryStartStorageCoalPickup())
            {
                return true;
            }

            if (TryStartStorageClayPickup())
            {
                return true;
            }

            if (TryStartStoragePlanksPickup())
            {
                return true;
            }

            if (TryStartStoragePotteryPickup())
            {
                return true;
            }

            if (TryStartStorageToolsPickup())
            {
                return true;
            }

            if (TryStartGranaryTask())
            {
                return true;
            }

            if (TryStartHaulerConstructionDeliveryTask())
            {
                return true;
            }

            logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
            return false;
        }

        private bool TryStartStorageLogPickup()
        {
            if (storageWorkplace.TryReserveLogSource(this, out StrategyLumberjackCamp source))
            {
                if (!source.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    source.ReleaseStoredLogsReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", source.Origin),
                        StrategyDebugLogger.F("resource", "logs"),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeLogSource = source;
                activity = ResidentActivity.MovingToStoragePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", source.Origin),
                    StrategyDebugLogger.F("resource", "logs"),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            if (StrategyLooseConstructionResourcePile.TryReserveNearestForStorage(
                    storageWorkplace,
                    this,
                    StrategyConstructionResourceKind.Logs,
                    out StrategyLooseConstructionResourcePile looseLogSource))
            {
                if (!looseLogSource.TryFindPickupCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    looseLogSource.ReleaseStorageReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", looseLogSource.Origin),
                        StrategyDebugLogger.F("resource", "logs"),
                        StrategyDebugLogger.F("reason", "no_loose_pickup_path"));
                    return false;
                }

                activeLooseLogSource = looseLogSource;
                activity = ResidentActivity.MovingToStoragePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", looseLogSource.Origin),
                    StrategyDebugLogger.F("source", "loose_construction_resources"),
                    StrategyDebugLogger.F("resource", "logs"),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            return false;
        }

        private bool TryStartStorageStonePickup()
        {
            if (storageWorkplace.TryReserveStoneSource(this, out StrategyStonecutterCamp stoneSource))
            {
                if (!stoneSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    stoneSource.ReleaseStoredStoneReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", stoneSource.Origin),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeStoneSource = stoneSource;
                activity = ResidentActivity.MovingToStorageStonePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", stoneSource.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            if (StrategyLooseConstructionResourcePile.TryReserveNearestForStorage(
                    storageWorkplace,
                    this,
                    StrategyConstructionResourceKind.Stone,
                    out StrategyLooseConstructionResourcePile looseStoneSource))
            {
                if (!looseStoneSource.TryFindPickupCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    looseStoneSource.ReleaseStorageReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Logistics",
                        "PickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", looseStoneSource.Origin),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                        StrategyDebugLogger.F("reason", "no_loose_pickup_path"));
                    return false;
                }

                activeLooseStoneSource = looseStoneSource;
                activity = ResidentActivity.MovingToStorageStonePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Logistics",
                    "PickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", looseStoneSource.Origin),
                    StrategyDebugLogger.F("source", "loose_construction_resources"),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Stone),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("yardOrigin", storageWorkplace.Origin));
                return true;
            }

            return false;
        }
    }
}
