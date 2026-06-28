using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private bool TryStartGranaryTask()
        {
            StrategyGranary targetGranary = granaryWorkplace;
            if (targetGranary == null
                && storageWorkplace != null
                && !StrategyGranary.TryFindNearestGranary(storageWorkplace.FootprintBounds.center, out targetGranary))
            {
                return false;
            }

            if (activity != ResidentActivity.Idle
                || targetGranary == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || builderWorkplace != null
                || !CanWork
                || logisticsWorkCooldown > 0f)
            {
                return false;
            }

            if (StrategyLooseCarriedResourcePile.TryReserveNearestForGranary(
                    targetGranary,
                    this,
                    out StrategyLooseCarriedResourcePile looseFoodSource,
                    out StrategyResourceType looseResource,
                    out Vector2Int loosePickupCell))
            {
                if (!TryBuildPathTo(loosePickupCell))
                {
                    looseFoodSource.ReleaseReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", looseFoodSource.Origin),
                        StrategyDebugLogger.F("resource", looseResource),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeLooseFoodSource = looseFoodSource;
                activeGranaryDeliveryTarget = targetGranary;
                activity = looseResource == StrategyResourceType.Game
                    ? ResidentActivity.MovingToGranaryGamePickup
                    : looseResource == StrategyResourceType.Fish
                        ? ResidentActivity.MovingToGranaryFishPickup
                        : ResidentActivity.MovingToGranaryForagePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "LooseFoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", looseFoodSource.Origin),
                    StrategyDebugLogger.F("resource", looseResource),
                    StrategyDebugLogger.F("pickupCell", loosePickupCell),
                    StrategyDebugLogger.F("granaryOrigin", targetGranary.Origin));
                return true;
            }

            if (!targetGranary.TryReserveFoodSource(
                    this,
                    out StrategyResourceType resource,
                    out StrategyHunterCamp gameSource,
                    out StrategyFisherHut fishSource,
                    out StrategyForagerCamp forageSource,
                    out StrategyChickenCoop eggSource))
            {
                logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
                return false;
            }

            if (resource == StrategyResourceType.Game)
            {
                if (gameSource == null
                    || !gameSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    gameSource?.ReleaseStoredGameReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", gameSource != null ? gameSource.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Game),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeGameSource = gameSource;
                activeGranaryDeliveryTarget = targetGranary;
                activity = ResidentActivity.MovingToGranaryGamePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "FoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", gameSource.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Game),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("granaryOrigin", targetGranary.Origin));
                return true;
            }

            if (resource == StrategyResourceType.Fish)
            {
                if (fishSource == null
                    || !fishSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    fishSource?.ReleaseStoredFishReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", fishSource != null ? fishSource.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Fish),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeFishSource = fishSource;
                activeGranaryDeliveryTarget = targetGranary;
                activity = ResidentActivity.MovingToGranaryFishPickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "FoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", fishSource.Origin),
                    StrategyDebugLogger.F("resource", StrategyResourceType.Fish),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("granaryOrigin", targetGranary.Origin));
                return true;
            }

            if (resource == StrategyResourceType.Eggs)
            {
                if (eggSource == null
                    || !eggSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    eggSource?.ReleaseStoredEggsReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", eggSource != null ? eggSource.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", StrategyResourceType.Eggs),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeEggFoodSource = eggSource;
                activeGranaryDeliveryTarget = targetGranary;
                carriedForageResource = resource;
                activity = ResidentActivity.MovingToGranaryForagePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "FoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", eggSource.Origin),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("granaryOrigin", targetGranary.Origin));
                return true;
            }

            if (IsForageFood(resource))
            {
                if (forageSource == null
                    || !forageSource.TryFindDropoffCell(out Vector2Int pickupCell)
                    || !TryBuildPathTo(pickupCell))
                {
                    forageSource?.ReleaseStoredForageReservation(this);
                    logisticsWorkCooldown = Random.Range(2.0f, 4.0f);
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FoodPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("sourceOrigin", forageSource != null ? forageSource.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", resource),
                        StrategyDebugLogger.F("reason", "no_pickup_path"));
                    return false;
                }

                activeForageFoodSource = forageSource;
                activeGranaryDeliveryTarget = targetGranary;
                carriedForageResource = resource;
                activity = ResidentActivity.MovingToGranaryForagePickup;
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.20f);
                StrategyDebugLogger.Info(
                    "Granary",
                    "FoodPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("sourceOrigin", forageSource.Origin),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("pickupCell", pickupCell),
                    StrategyDebugLogger.F("granaryOrigin", targetGranary.Origin));
                return true;
            }

            logisticsWorkCooldown = Random.Range(2.5f, 5.5f);
            return false;
        }

        private bool TryStartConstructionTask()
        {
            if (activity != ResidentActivity.Idle || constructionSite == null || builderWorkplace == null || !CanWork)
            {
                return false;
            }

            if (constructionSite.IsCompleted)
            {
                NotifyConstructionCompleted(constructionSite);
                return false;
            }

            if (!constructionSite.ResourcesComplete)
            {
                if (!constructionSite.TryFindResourcePickup(
                        this,
                        out IStrategyConstructionResourceSource source,
                        out StrategyConstructionResourceKind kind,
                        out Vector2Int pickupCell,
                        out int pickupAmount))
                {
                    waitTimer = Random.Range(0.45f, 1.1f);
                    return false;
                }

                if (!TryBuildPathTo(pickupCell))
                {
                    Vector2Int startCell = Vector2Int.zero;
                    bool hasStartCell = map != null && map.TryWorldToCell(transform.position, out startCell);
                    bool startWalkable = hasStartCell && map.IsCellWalkable(startCell);
                    bool pickupWalkable = map != null && map.IsCellWalkable(pickupCell);
                    constructionPickupPathFailures++;
                    waitTimer = Random.Range(0.45f, 1.1f);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "BuilderPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                        StrategyDebugLogger.F("resource", kind),
                        StrategyDebugLogger.F("startCell", hasStartCell ? startCell : Vector2Int.zero),
                        StrategyDebugLogger.F("startWalkable", startWalkable),
                        StrategyDebugLogger.F("pickupCell", pickupCell),
                        StrategyDebugLogger.F("pickupWalkable", pickupWalkable),
                        StrategyDebugLogger.F("failureCount", constructionPickupPathFailures),
                        StrategyDebugLogger.F("reason", "no_path"));
                    if (constructionPickupPathFailures >= ConstructionPickupPathFailureLimit)
                    {
                        StrategyConstructionSite failedSite = constructionSite;
                        Vector2Int failedOrigin = failedSite != null ? failedSite.Origin : Vector2Int.zero;
                        ClearConstructionSite(failedSite);
                        waitTimer = Random.Range(2.0f, 4.2f);
                        StrategyDebugLogger.Warn(
                            "Construction",
                            "BuilderConstructionAssignmentDropped",
                            StrategyDebugLogger.F("resident", FullName),
                            StrategyDebugLogger.F("siteOrigin", failedOrigin),
                            StrategyDebugLogger.F("reason", "pickup_path_repeatedly_failed"));
                    }

                    return false;
                }

                if (source == null
                    || pickupAmount <= 0
                    || !source.TryReserveConstructionPickup(constructionSite, this, kind, pickupAmount))
                {
                    hasTarget = false;
                    path.Clear();
                    pathIndex = 0;
                    waitTimer = Random.Range(0.45f, 1.1f);
                    StrategyDebugLogger.Warn(
                        "Construction",
                        "BuilderPickupMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                        StrategyDebugLogger.F("sourceOrigin", source != null ? source.Origin : Vector2Int.zero),
                        StrategyDebugLogger.F("resource", kind),
                        StrategyDebugLogger.F("amount", pickupAmount),
                        StrategyDebugLogger.F("pickupCell", pickupCell),
                        StrategyDebugLogger.F("reason", "reserve_failed"));
                    return false;
                }

                constructionPickupPathFailures = 0;
                activeConstructionSource = source;
                activeConstructionResource = kind;
                activity = ResidentActivity.MovingToConstructionStorage;
                hasTarget = true;
                waitTimer = Random.Range(0.02f, 0.14f);
                StrategyDebugLogger.Info(
                    "Construction",
                    "BuilderPickupMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("sourceOrigin", source != null ? source.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("resource", kind),
                    StrategyDebugLogger.F("amount", pickupAmount),
                    StrategyDebugLogger.F("pickupCell", pickupCell));
                return true;
            }

            if (!TryBuildPathToConstructionWorkCell(constructionSite, out Vector2Int workCell, out int checkedWorkCells))
            {
                waitTimer = Random.Range(0.45f, 1.1f);
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderWorkMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("workCell", workCell),
                    StrategyDebugLogger.F("checkedCells", checkedWorkCells),
                    StrategyDebugLogger.F("reason", "no_path"));
                return false;
            }

            activity = ResidentActivity.MovingToConstructionSite;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.14f);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderWorkMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("workCell", workCell));
            return true;
        }

        private bool TryBuildPathToConstructionWorkCell(
            StrategyConstructionSite site,
            out Vector2Int workCell,
            out int checkedCells)
        {
            workCell = default;
            checkedCells = 0;
            if (site == null || !site.TryCollectBuildWorkCells(constructionWorkCellCandidates))
            {
                return false;
            }

            while (constructionWorkCellCandidates.Count > 0)
            {
                int index = GetConstructionWorkCellIndex(site, constructionWorkCellCandidates);
                Vector2Int candidate = constructionWorkCellCandidates[index];
                constructionWorkCellCandidates.RemoveAt(index);
                checkedCells++;
                workCell = candidate;
                if (TryBuildPathTo(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryStartHunterTask()
        {
            if (activity != ResidentActivity.Idle
                || hunterWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || fisherWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || huntingWorkCooldown > 0f)
            {
                return false;
            }

            if (hunterWorkplace.TryReserveHuntTarget(this, out IStrategyHuntTarget target)
                && TryMoveToHuntingTarget(target))
            {
                return true;
            }

            target?.ReleaseHuntReservation(this);
            activeHuntTarget = null;
            huntingWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryMoveToHuntingTarget(IStrategyHuntTarget target)
        {
            return TryMoveToRangedHuntingTarget(target);
        }

        private bool TryStartFisherTask()
        {
            if (activity != ResidentActivity.Idle
                || fisherWorkplace == null
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || !CanWork
                || fishingWorkCooldown > 0f)
            {
                return false;
            }

            if (fisherWorkplace.TryReserveFishTarget(this, out StrategyFishAgent fish)
                && TryMoveToFishingTarget(fish))
            {
                return true;
            }

            fish?.ReleaseFishingReservation(this);
            activeFishTarget = null;
            fishingWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryMoveToFishingTarget(StrategyFishAgent fish)
        {
            if (fish == null || fish.IsCaught || fisherWorkplace == null)
            {
                return false;
            }

            activeFishTarget = fish;
            if (!fisherWorkplace.TryFindFishingCell(fish, out Vector2Int fishingCell)
                || !TryBuildPathTo(fishingCell))
            {
                activeFishTarget = null;
                ClearFishingStandTracking();
                activity = ResidentActivity.Idle;
                fishingWorkCooldown = Random.Range(2.0f, 4.0f);
                StrategyDebugLogger.Warn(
                    "Fishing",
                    "FishingMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("fishWorld", fish != null ? fish.transform.position : Vector3.zero),
                    StrategyDebugLogger.F("reason", "no_shore_path"),
                    StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
                return false;
            }

            activeFishingCell = fishingCell;
            hasActiveFishingCell = true;
            activity = ResidentActivity.MovingToFishingSpot;
            hasTarget = true;
            waitTimer = Random.Range(0.04f, 0.18f);
            StrategyDebugLogger.Info(
                "Fishing",
                "FishingMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fishingCell", fishingCell),
                StrategyDebugLogger.F("fishWorld", fish.transform.position),
                StrategyDebugLogger.F("hutOrigin", fisherWorkplace != null ? fisherWorkplace.Origin : Vector2Int.zero));
            return true;
        }

    }
}
