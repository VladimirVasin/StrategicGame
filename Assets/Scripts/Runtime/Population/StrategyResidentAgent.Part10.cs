using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void UpdatePickingUpGranaryFish()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if ((activeFishSource == null && activeLooseFoodSource == null)
                || activeGranaryDeliveryTarget == null
                || !activeGranaryDeliveryTarget.TryFindDropoffCell(out Vector2Int dropoffCell)
                || !TryBuildPathTo(dropoffCell))
            {
                activeFishSource?.ReleaseStoredFishReservation(this);
                activeLooseFoodSource?.ReleaseReservation(this);
                StrategyDebugLogger.Warn(
                    "Granary",
                    "FishPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "no_granary_path"),
                    StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
                ResetGranaryWorkToIdle();
                return;
            }

            Vector2Int sourceOrigin;
            if (activeLooseFoodSource != null)
            {
                sourceOrigin = activeLooseFoodSource.Origin;
                if (!activeLooseFoodSource.TryTakeReserved(this, out StrategyResourceType resource, out carriedFishAmount)
                    || resource != StrategyResourceType.Fish)
                {
                    StrategyDebugLogger.Warn(
                        "Granary",
                        "FishPickupRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "loose_take_failed"),
                        StrategyDebugLogger.F("sourceOrigin", sourceOrigin));
                    activeLooseFoodSource = null;
                    ResetGranaryWorkToIdle();
                    return;
                }

                activeLooseFoodSource = null;
            }
            else if (!activeFishSource.TryTakeReservedFish(this, out carriedFishAmount))
            {
                StrategyDebugLogger.Warn(
                    "Granary",
                    "FishPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", "take_failed"),
                    StrategyDebugLogger.F("sourceOrigin", activeFishSource.Origin));
                ResetGranaryWorkToIdle();
                return;
            }
            else
            {
                sourceOrigin = activeFishSource.Origin;
            }

            activeFishSource = null;
            activity = ResidentActivity.CarryingFishToGranary;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            SetCarriedFishVisible(true);
            StrategyDebugLogger.Info(
                "Granary",
                "FishPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("dropoffCell", dropoffCell),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget.Origin));
        }

        private void StartDepositingGranaryFish()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingGranaryFish;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            if (activeGranaryDeliveryTarget != null)
            {
                FaceWorldPoint(activeGranaryDeliveryTarget.FootprintBounds.center);
            }

            StrategyDebugLogger.Info(
                "Granary",
                "FishDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", carriedFishAmount),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero));
        }

        private void UpdateDepositingGranaryFish()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedFishVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            int depositedAmount = carriedFishAmount;
            if (activeGranaryDeliveryTarget != null)
            {
                activeGranaryDeliveryTarget.AddFish(depositedAmount);
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            StrategyDebugLogger.Info(
                "Granary",
                "FishDelivered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("amount", depositedAmount),
                StrategyDebugLogger.F("granaryOrigin", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("granaryStock", activeGranaryDeliveryTarget != null ? activeGranaryDeliveryTarget.FishStored : -1));
            CompleteGranaryDelivery();
        }

        private void StartPickingUpHouseholdFood()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (activeHouseholdFoodGranary == null
                || carriedHouseholdFoodResource == StrategyResourceType.None)
            {
                ResetHouseholdFoodWorkToIdle(false);
                return;
            }

            activity = ResidentActivity.PickingUpHouseholdFood;
            lumberWorkTimer = Random.Range(LogisticsPickupSecondsMin, LogisticsPickupSecondsMax);
            FaceWorldPoint(activeHouseholdFoodGranary.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderFoodPickupCollecting",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("granaryOrigin", activeHouseholdFoodGranary.Origin),
                StrategyDebugLogger.F("resource", carriedHouseholdFoodResource));
        }

        private void UpdatePickingUpHouseholdFood()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (activeHouseholdFoodGranary == null
                || home == null
                || home.Resources == null
                || !TryBuildPathToHomeDropoff())
            {
                activeHouseholdFoodGranary?.ReleaseHouseholdFoodReservation(this);
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderFoodPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("granaryOrigin", activeHouseholdFoodGranary != null ? activeHouseholdFoodGranary.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("reason", "no_home_path"));
                ResetHouseholdFoodWorkToIdle(false);
                return;
            }

            Vector2Int granaryOrigin = activeHouseholdFoodGranary.Origin;
            if (!activeHouseholdFoodGranary.TryTakeReservedHouseholdFood(
                    this,
                    out StrategyResourceType resource,
                    out int amount)
                || amount <= 0
                || !StrategyFoodNutrition.IsIngredientFood(resource))
            {
                StrategyDebugLogger.Warn(
                    "Household",
                    "HouseholderFoodPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("granaryOrigin", granaryOrigin),
                    StrategyDebugLogger.F("reason", "take_failed"));
                ResetHouseholdFoodWorkToIdle(false);
                return;
            }

            activeHouseholdFoodGranary = null;
            carriedHouseholdFoodResource = resource;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            if (resource == StrategyResourceType.Game)
            {
                carriedGameAmount = amount;
                SetCarriedGameVisible(true);
                SetCarriedFishVisible(false);
                SetCarriedForageVisible(false);
            }
            else if (resource == StrategyResourceType.Fish)
            {
                carriedFishAmount = amount;
                SetCarriedFishVisible(true);
                SetCarriedGameVisible(false);
                SetCarriedForageVisible(false);
            }
            else
            {
                carriedForageResource = resource;
                carriedForageAmount = amount;
                SetCarriedForageVisible(true);
                SetCarriedGameVisible(false);
                SetCarriedFishVisible(false);
            }

            activity = ResidentActivity.CarryingHouseholdFoodHome;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderFoodPickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", resource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("granaryOrigin", granaryOrigin),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private void StartDepositingHouseholdFood()
        {
            if (home == null
                || home.Resources == null
                || carriedHouseholdFoodResource == StrategyResourceType.None
                || GetCarriedHouseholdFoodAmount() <= 0)
            {
                ResetHouseholdFoodWorkToIdle(false);
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.DepositingHouseholdFood;
            lumberWorkTimer = Random.Range(LogisticsDepositSecondsMin, LogisticsDepositSecondsMax);
            FaceWorldPoint(home.FootprintBounds.center);
            SetCarriedHouseholdFoodVisible(true);
            StrategyDebugLogger.Info(
                "Household",
                "HouseholderFoodDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("resource", carriedHouseholdFoodResource),
                StrategyDebugLogger.F("amount", GetCarriedHouseholdFoodAmount()),
                StrategyDebugLogger.F("homeOrigin", home.Origin));
        }

        private void UpdateDepositingHouseholdFood()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.2f);
            SetCarriedHouseholdFoodVisible(true);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            StrategyResourceType resource = carriedHouseholdFoodResource;
            int amount = GetCarriedHouseholdFoodAmount();
            if (home != null && home.Resources != null && amount > 0)
            {
                home.Resources.AddResource(resource, amount);
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholderFoodDeposited",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", resource),
                    StrategyDebugLogger.F("amount", amount),
                    StrategyDebugLogger.F("rationValue", amount * StrategyFoodNutrition.GetRationValue(resource)),
                    StrategyDebugLogger.F("homeOrigin", home.Origin),
                    StrategyDebugLogger.F("preparedRations", home.Resources.GetPreparedDishRations()),
                    StrategyDebugLogger.F("ingredientRations", home.Resources.GetTotalIngredientRationValue()));
            }

            ClearCarriedHouseholdFood();
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            activity = GetRestingActivity();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.35f, 0.95f);
        }

        private void StartPickingUpConstructionResource()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (constructionSite == null
                || activeConstructionSource == null
                || activeConstructionResource == StrategyConstructionResourceKind.None)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = activeConstructionResource switch
            {
                StrategyConstructionResourceKind.Logs => ResidentActivity.PickingUpConstructionLogs,
                StrategyConstructionResourceKind.Stone => ResidentActivity.PickingUpConstructionStone,
                StrategyConstructionResourceKind.Planks => ResidentActivity.PickingUpConstructionPlanks,
                _ => ResidentActivity.Idle
            };
            lumberWorkTimer = Random.Range(ConstructionPickupSecondsMin, ConstructionPickupSecondsMax);
            FaceWorldPoint(activeConstructionSource.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderPickupStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("sourceOrigin", activeConstructionSource.Origin),
                StrategyDebugLogger.F("resource", activeConstructionResource));
        }

        private void UpdatePickingUpConstructionResource()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.8f, 3.0f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            Vector2Int dropoffCell = default;
            int checkedDropoffCells = 0;
            if (constructionSite == null
                || activeConstructionSource == null
                || activeConstructionResource == StrategyConstructionResourceKind.None
                || !TryBuildPathToConstructionDropoffCell(
                    constructionSite,
                    out dropoffCell,
                    out checkedDropoffCells))
            {
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite != null ? constructionSite.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("dropoffCell", dropoffCell),
                    StrategyDebugLogger.F("checkedDropoffCells", checkedDropoffCells),
                    StrategyDebugLogger.F("reason", "no_dropoff_path"));
                ResetConstructionWorkToIdle();
                return;
            }

            if (!activeConstructionSource.TryTakeReservedConstructionResource(constructionSite, this, activeConstructionResource, StrategyProductionStorage.BuilderCarryLimit, out int amount))
            {
                StrategyDebugLogger.Warn(
                    "Construction",
                    "BuilderPickupRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                    StrategyDebugLogger.F("sourceOrigin", activeConstructionSource.Origin),
                    StrategyDebugLogger.F("resource", activeConstructionResource),
                    StrategyDebugLogger.F("reason", "take_failed"));
                ResetConstructionWorkToIdle();
                return;
            }

            if (activeConstructionResource == StrategyConstructionResourceKind.Logs)
            {
                carriedLogAmount = amount;
                SetCarriedLogsVisible(true);
                activity = ResidentActivity.CarryingConstructionLogs;
            }
            else if (activeConstructionResource == StrategyConstructionResourceKind.Stone)
            {
                carriedStoneAmount = amount;
                SetCarriedStoneVisible(true);
                activity = ResidentActivity.CarryingConstructionStone;
            }
            else if (activeConstructionResource == StrategyConstructionResourceKind.Planks)
            {
                carriedPlanksAmount = amount;
                SetCarriedPlanksVisible(true);
                activity = ResidentActivity.CarryingConstructionPlanks;
            }

            CaptureCarriedConstructionReturnReservation();
            Vector2Int sourceOrigin = activeConstructionSource.Origin;
            activeConstructionSource = null;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.10f);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderResourcePickedUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("sourceOrigin", sourceOrigin),
                StrategyDebugLogger.F("resource", activeConstructionResource),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("dropoffCell", dropoffCell));
        }

        private void StartDepositingConstructionResource()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (constructionSite == null)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            activity = ResidentActivity.DepositingConstructionResource;
            lumberWorkTimer = Random.Range(ConstructionDepositSecondsMin, ConstructionDepositSecondsMax);
            FaceWorldPoint(constructionSite.FootprintBounds.center);
            StrategyDebugLogger.Info(
                "Construction",
                "BuilderDepositStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", constructionSite.Origin),
                StrategyDebugLogger.F("resource", activeConstructionResource),
                StrategyDebugLogger.F("logs", carriedLogAmount),
                StrategyDebugLogger.F("stone", carriedStoneAmount),
                StrategyDebugLogger.F("planks", carriedPlanksAmount));
        }

        private void UpdateDepositingConstructionResource()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(7.0f, 3.3f);
            SetCarriedLogsVisible(carriedLogAmount > 0);
            SetCarriedStoneVisible(carriedStoneAmount > 0);
            SetCarriedPlanksVisible(carriedPlanksAmount > 0);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            if (constructionSite != null)
            {
                if (activeConstructionResource == StrategyConstructionResourceKind.Logs && carriedLogAmount > 0)
                {
                    constructionSite.AddDeliveredResource(StrategyConstructionResourceKind.Logs, carriedLogAmount);
                }
                else if (activeConstructionResource == StrategyConstructionResourceKind.Stone && carriedStoneAmount > 0)
                {
                    constructionSite.AddDeliveredResource(StrategyConstructionResourceKind.Stone, carriedStoneAmount);
                }
                else if (activeConstructionResource == StrategyConstructionResourceKind.Planks && carriedPlanksAmount > 0)
                {
                    constructionSite.AddDeliveredResource(StrategyConstructionResourceKind.Planks, carriedPlanksAmount);
                }
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedPlanksAmount = 0;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            ClearCarriedConstructionReturnReservation();
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedPlanksVisible(false);
            CompleteConstructionDelivery();
        }

    }
}
