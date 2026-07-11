using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float ForageMinimumWorkWindowSeconds = 12f;

        public void AssignForagerWorkplace(StrategyForagerCamp camp)
        {
            if (camp == null
                || foragerWorkplace == camp
                || workplace != null
                || stoneWorkplace != null
                || hunterWorkplace != null
                || fisherWorkplace != null
                || mineWorkplace != null
                || coalPitWorkplace != null
                || clayPitWorkplace != null
                || sawmillWorkplace != null
                || kilnWorkplace != null
                || forgeWorkplace != null
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
            CancelHunterWork(true);
            CancelFisherWork(true);
            CancelForageWork(true);
            CancelMineWork();
            CancelCoalPitWork();
            CancelClayPitWork();
            CancelSawmillWork(true);
            CancelKilnWork(true);
            CancelForgeWork(true);
            CancelStorageWork(true);
            CancelGranaryWork(true);
            foragerWorkplace = camp;
            foragerWorkCooldown = Random.Range(0.45f, 2.0f);
            StrategyDebugLogger.Info(
                "ForagerCamp",
                "ResidentForagerWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin));
        }

        public void ClearForagerWorkplace(StrategyForagerCamp camp)
        {
            if (this == null)
            {
                return;
            }

            if (camp != null && foragerWorkplace != camp)
            {
                return;
            }

            StrategyForagerCamp previous = foragerWorkplace;
            CancelForageWork(true);
            foragerWorkplace = null;
            StrategyDebugLogger.Info(
                "ForagerCamp",
                "ResidentForagerWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", previous != null ? previous.Origin : Vector2Int.zero));
        }

        private bool TryStartForagerTask()
        {
            if (!CanStartForagerWorkForCamp(foragerWorkplace))
            {
                return false;
            }

            if (foragerWorkplace.TryReserveForageNode(this, out StrategyForageNode node, out Vector2Int workCell)
                && TryStartForagerCampForaging(foragerWorkplace, node, workCell))
            {
                return true;
            }

            node?.Release(this);
            activeForageNode = null;
            activeForagerCamp = null;
            foragerWorkCooldown = Random.Range(2.5f, 5.0f);
            return false;
        }

        private bool TryStartForagerCampForaging(
            StrategyForagerCamp camp,
            StrategyForageNode node,
            Vector2Int workCell)
        {
            if (camp == null || node == null || !node.IsReservedBy(this))
            {
                return false;
            }

            activeForagerCamp = camp;
            activeForageNode = node;
            activeLooseForageSource = null;
            activeGarden = null;
            carriedForageResource = StrategyResourceType.None;
            carriedForageAmount = 0;
            activity = ResidentActivity.MovingToForage;
            if (!TryBuildPathTo(workCell))
            {
                activeForageNode = null;
                activeForagerCamp = null;
                activity = ResidentActivity.Idle;
                if (WasLastPathBuildDeferred)
                {
                    foragerWorkCooldown = Random.Range(0.18f, 0.38f);
                    return false;
                }

                StrategyDebugLogger.Warn(
                    "ForagerCamp",
                    "ForageMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("nodeCell", node.Cell),
                    StrategyDebugLogger.F("workCell", workCell),
                    StrategyDebugLogger.F("reason", "no_path"));
                return false;
            }

            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.22f);
            StrategyDebugLogger.Info(
                "ForagerCamp",
                "ForageMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("campOrigin", camp.Origin),
                StrategyDebugLogger.F("resource", node.ResourceType),
                StrategyDebugLogger.F("nodeCell", node.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return true;
        }

        private bool CanStartForagerWorkForCamp(StrategyForagerCamp camp)
        {
            return activity == ResidentActivity.Idle
                && camp != null
                && camp == foragerWorkplace
                && camp.HasStorageSpace
                && CanWork
                && HasEnoughForageWorkWindow()
                && foragerWorkCooldown <= 0f
                && workplace == null
                && stoneWorkplace == null
                && hunterWorkplace == null
                && fisherWorkplace == null
                && mineWorkplace == null
                && coalPitWorkplace == null
                && clayPitWorkplace == null
                && sawmillWorkplace == null
                && kilnWorkplace == null
                && forgeWorkplace == null
                && storageWorkplace == null
                && builderWorkplace == null
                && granaryWorkplace == null
                && constructionSite == null
                && !deathRequested
                && !hiddenInsideHome
                && !hiddenUnderground
                && !IsPendingRefugee
                && activeForageNode == null
                && activeLooseForageSource == null
                && carriedForageAmount <= 0;
        }

        private static bool HasEnoughForageWorkWindow()
        {
            if (!StrategyDayNightCycleController.IsSettlementWorkTime)
            {
                return false;
            }

            float remainingWorkSeconds =
                (StrategyDayNightCycleController.NightStartPhase - StrategyDayNightCycleController.CurrentDayPhase)
                * StrategyDayNightCycleController.DayLengthSeconds;
            return remainingWorkSeconds >= ForageMinimumWorkWindowSeconds;
        }
    }
}
