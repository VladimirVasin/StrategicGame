using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        public void AssignHome(StrategyPlacedBuilding newHome)
        {
            if (newHome == null || home == newHome)
            {
                return;
            }

            if (!newHome.CanAcceptResident(this))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "ResidentHomeAssignRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", newHome.Origin),
                    StrategyDebugLogger.F("reason", "resident_capacity"));
                return;
            }

            CancelHouseholdFoodWork(true);
            home?.UnregisterResident(this);
            home = newHome;
            home.TryRegisterResident(this);

            idleOrigin = home.Origin;
            idleFootprint = home.Footprint;
            activeGarden = null;
            gardenWorkTimer = 0f;
            gardenWorkCooldown = Random.Range(2.5f, 6.5f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHomeAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home.Origin),
                StrategyDebugLogger.F("homeTool", home.Tool));

            if (IsHomeboundYoungChild)
            {
                EnterHomeboundChildState(true);
            }
        }

        public void AssignHome(StrategyPlacedBuilding newHome, Vector3 targetWorld)
        {
            AssignHome(newHome);
            if (home == newHome && CanStartHomeMoveNow())
            {
                StartMovingHome(targetWorld);
            }
        }

        public void PrepareHouseholderHomeDuty()
        {
            StrategyLumberjackCamp previousLumber = workplace;
            StrategyStonecutterCamp previousStone = stoneWorkplace;
            StrategyHunterCamp previousHunter = hunterWorkplace;
            StrategyFisherHut previousFisher = fisherWorkplace;
            StrategyMine previousMine = mineWorkplace;
            StrategyCoalPit previousCoal = coalPitWorkplace;
            StrategyStorageYard previousStorage = storageWorkplace;
            StrategyStorageYard previousBuilder = builderWorkplace;
            StrategyGranary previousGranary = granaryWorkplace;
            bool hadExternalWork = HasExternalWorkplace || constructionSite != null;

            previousLumber?.UnassignWorker(this);
            previousStone?.UnassignWorker(this);
            previousHunter?.UnassignWorker(this);
            previousFisher?.UnassignWorker(this);
            previousMine?.UnassignWorker(this);
            previousCoal?.UnassignWorker(this);
            previousStorage?.UnassignWorker(this);
            previousBuilder?.UnassignBuilder(this);
            previousGranary?.UnassignWorker(this);
            ClearConstructionSite(null);
            CancelForageWork(true);
            CancelHouseholdFoodWork(true);

            if (home != null)
            {
                idleOrigin = home.Origin;
                idleFootprint = home.Footprint;
            }

            if (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
            {
                activity = ResidentActivity.TendingHousehold;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Mathf.Min(waitTimer, Random.Range(0.10f, 0.35f));
                UseIdleSprite();
            }

            gardenWorkCooldown = Mathf.Min(gardenWorkCooldown, Random.Range(0.35f, 1.25f));
            if (hadExternalWork)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseholderExternalWorkCleared",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
            }
        }

        private bool CanStartHomeMoveNow()
        {
            return constructionSite == null
                && (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
                && !hasTarget;
        }

        public void AssignConstructionSite(StrategyConstructionSite site, bool willLiveThere)
        {
            if (site == null
                || constructionSite == site
                || builderWorkplace == null
                || IsReturningCarriedResourceActivity(activity)
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelMineWork();
            CancelForageWork(true);
            CancelHouseholdFoodWork(true);
            constructionSite = site;
            constructionFutureHome = willLiveThere;
            ClearCarriedConstructionReturnReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.08f, 0.32f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentConstructionAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteTool", site.Tool),
                StrategyDebugLogger.F("siteOrigin", site.Origin),
                StrategyDebugLogger.F("futureHome", willLiveThere));
        }

        public void NotifyConstructionCompleted(StrategyConstructionSite site)
        {
            if (site != null && constructionSite != site)
            {
                return;
            }

            constructionSite = null;
            ReleaseActiveConstructionPickupReservation();
            ClearCarriedConstructionReturnReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            constructionFutureHome = false;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.12f, 0.38f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentConstructionCompleted",
                StrategyDebugLogger.F("resident", FullName));
        }

        public void ClearConstructionSite(StrategyConstructionSite site)
        {
            if (site != null && constructionSite != site)
            {
                return;
            }

            bool hadCarriedResources = carriedLogAmount > 0
                || carriedStoneAmount > 0
                || carriedIronAmount > 0
                || carriedGameAmount > 0
                || carriedFishAmount > 0;
            CaptureCarriedConstructionReturnReservation();

            if (constructionSite != null)
            {
                constructionSite.UnregisterBuilder(this);
            }

            ReleaseActiveConstructionPickupReservation();
            if (IsConstructionActivity(activity))
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.70f);
            }

            constructionSite = null;
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            constructionFutureHome = false;
            if (hadCarriedResources && TryStartCarriedResourceReturn("construction_assignment_cleared"))
            {
                return;
            }

            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        public void ExtractCarriedConstructionResources(
            StrategyConstructionSite site,
            out int logs,
            out int stone)
        {
            logs = 0;
            stone = 0;
            if (site != null && constructionSite != site)
            {
                return;
            }

            logs = carriedLogAmount;
            stone = carriedStoneAmount;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            ClearCarriedConstructionReturnReservation();
        }

        public void ClearHome(StrategyPlacedBuilding removedHome)
        {
            if (removedHome != null && home != removedHome)
            {
                return;
            }

            if (hiddenInsideHome)
            {
                hiddenInsideHome = false;
                SetWorldPresenceVisible(true);
            }

            CancelForageWork(false);
            CancelHouseholdFoodWork(false);
            home = null;
            activeGarden = null;
            gardenWorkTimer = 0f;
            gardenWorkCooldown = Random.Range(2.0f, 5.0f);
            if (activity == ResidentActivity.TendingHousehold
                || activity == ResidentActivity.MovingToGarden
                || activity == ResidentActivity.WorkingGarden
                || IsHouseholdFoodActivity(activity)
                || activity == ResidentActivity.StayingInsideHome
                || activity == ResidentActivity.MovingHome)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.35f, 0.95f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }

            if (map != null && map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                idleOrigin = currentCell;
            }

            idleFootprint = Vector2Int.one;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHomeCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("removedHomeOrigin", removedHome != null ? removedHome.Origin : Vector2Int.zero));
        }

        public void AssignWorkplace(StrategyLumberjackCamp camp)
        {
            if (camp == null
                || workplace == camp
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            workplace = camp;
            lumberWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void AssignStoneWorkplace(StrategyStonecutterCamp camp)
        {
            if (camp == null
                || stoneWorkplace == camp
                || workplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelStoneWork();
            stoneWorkplace = camp;
            stoneWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStoneWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void AssignHunterWorkplace(StrategyHunterCamp camp)
        {
            if (camp == null
                || hunterWorkplace == camp
                || workplace != null
                || stoneWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || storageWorkplace != null
                || builderWorkplace != null
                || granaryWorkplace != null
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            hunterWorkplace = camp;
            huntingWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHunterWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void ClearWorkplace(StrategyLumberjackCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && workplace != camp)
            {
                return;
            }

            StrategyLumberjackCamp previousWorkplace = workplace;
            workplace = null;
            CancelLumberWork();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void ClearStoneWorkplace(StrategyStonecutterCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && stoneWorkplace != camp)
            {
                return;
            }

            StrategyStonecutterCamp previousWorkplace = stoneWorkplace;
            stoneWorkplace = null;
            CancelStoneWork();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentStoneWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }

        public void ClearHunterWorkplace(StrategyHunterCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && hunterWorkplace != camp)
            {
                return;
            }

            StrategyHunterCamp previousWorkplace = hunterWorkplace;
            CancelHunterWork(true);
            hunterWorkplace = null;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentHunterWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previousWorkplace != null ? previousWorkplace.Origin : Vector2Int.zero));
        }
    }
}
