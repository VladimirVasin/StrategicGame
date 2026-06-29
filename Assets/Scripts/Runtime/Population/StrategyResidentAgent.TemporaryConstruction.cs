using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool temporaryConstructionAssignment;

        public bool AssignTemporaryConstructionSite(StrategyConstructionSite site)
        {
            if (site == null
                || constructionSite == site
                || IsReturningCarriedResourceActivity(activity)
                || activity == ResidentActivity.ReturningCoalToStorage
                || activity == ResidentActivity.ReturningClayToStorage
                || activity == ResidentActivity.ReturningPlanksToStorage
                || activity == ResidentActivity.ReturningPotteryToStorage
                || activity == ResidentActivity.ReturningToolsToStorage
                || !CanAcceptWorkAssignment)
            {
                return false;
            }

            ClearConstructionSite(null);
            CancelLumberWork();
            CancelStoneWork();
            CancelStorageWork(true);
            CancelGranaryWork(true);
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelMineWork();
            CancelSawmillWork(true);
            CancelKilnWork(true);
            CancelForgeWork(true);
            CancelClayPitWork();
            CancelForageWork(true);
            CancelHouseholdFoodWork(true);
            constructionSite = site;
            constructionFutureHome = false;
            temporaryConstructionAssignment = true;
            ClearCarriedConstructionReturnReservation();
            activeConstructionSource = null;
            activeConstructionResource = StrategyConstructionResourceKind.None;
            constructionPickupPathFailures = 0;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedPlanksAmount = 0;
            carriedPotteryAmount = 0;
            carriedToolsAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedPlanksVisible(false);
            SetCarriedPotteryVisible(false);
            SetCarriedToolsVisible(false);
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
                "ResidentTemporaryConstructionAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteTool", site.Tool),
                StrategyDebugLogger.F("siteOrigin", site.Origin));
            return true;
        }
    }
}
