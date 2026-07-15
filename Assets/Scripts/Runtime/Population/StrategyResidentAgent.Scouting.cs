using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float ScoutSurveySecondsMin = 2.5f;
        private const float ScoutSurveySecondsMax = 3.5f;
        private const float PointOfInterestInteractionSecondsMin = 1.5f;
        private const float PointOfInterestInteractionSecondsMax = 2.5f;
        private const float ScoutRouteRetrySecondsMin = 0.35f;
        private const float ScoutRouteRetrySecondsMax = 0.75f;

        private StrategyScoutLodge scoutWorkplace;
        private StrategyScoutLodge activeScoutLodge;
        private StrategyPointOfInterest activeScoutPointOfInterest;
        private Vector2Int scoutTarget;
        private float scoutWorkCooldown;
        private float scoutSurveyTimer;
        private float pointOfInterestInteractionTimer;
        private bool hasScoutTarget;

        public StrategyScoutLodge ScoutWorkplace => scoutWorkplace;
        public bool IsOnScoutExpedition => scoutWorkplace != null;

        public void AssignScoutWorkplace(StrategyScoutLodge lodge)
        {
            if (lodge == null
                || scoutWorkplace == lodge
                || HasExternalWorkplace
                || constructionSite != null
                || !CanAcceptWorkAssignment)
            {
                return;
            }

            CancelScoutWork();
            scoutWorkplace = lodge;
            LeaveNightRestForScoutDuty();
            ResetScoutMovementToIdle();
            waitTimer = 0f;
            scoutWorkCooldown = Random.Range(0.10f, 0.30f);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ResidentScoutWorkplaceAssigned",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("lodgeOrigin", lodge.Origin));
        }

        public void ClearScoutWorkplace(StrategyScoutLodge lodge)
        {
            if (this == null || (lodge != null && scoutWorkplace != lodge))
            {
                return;
            }

            StrategyScoutLodge previous = scoutWorkplace;
            CancelScoutWork();
            scoutWorkplace = null;
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ResidentScoutWorkplaceCleared",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("lodgeOrigin", previous != null ? previous.Origin : Vector2Int.zero));
        }

        private bool TryStartScoutTask()
        {
            if (!CanStartScoutWorkForLodge(scoutWorkplace))
            {
                return false;
            }

            StrategyScoutLodge lodge = scoutWorkplace;
            if (!map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                scoutWorkCooldown = Random.Range(
                    ScoutRouteRetrySecondsMin,
                    ScoutRouteRetrySecondsMax);
                return false;
            }

            StrategyPointOfInterestController pointController = StrategyPointOfInterestController.Active;
            if (pointController != null
                && pointController.TryReserveNearestDiscovered(
                    this,
                    currentCell,
                    out StrategyPointOfInterest pointOfInterest))
            {
                return TryStartPointOfInterestTask(
                    lodge,
                    pointController,
                    pointOfInterest,
                    currentCell);
            }

            if (!lodge.TryReserveExplorationTarget(this, out Vector2Int target))
            {
                scoutWorkCooldown = Random.Range(
                    ScoutRouteRetrySecondsMin,
                    ScoutRouteRetrySecondsMax);
                return false;
            }

            activeScoutLodge = lodge;
            scoutTarget = target;
            hasScoutTarget = true;
            if (currentCell == target)
            {
                StartSurveyingFrontier();
                return activity == ResidentActivity.SurveyingFrontier;
            }

            activity = ResidentActivity.MovingToScoutFrontier;
            if (!TryBuildPathTo(target))
            {
                bool deferred = WasLastPathBuildDeferred;
                if (deferred)
                {
                    lodge.ReleaseExplorationTarget(this);
                }
                else
                {
                    lodge.MarkTargetUnreachable(this, target);
                }

                activeScoutLodge = null;
                hasScoutTarget = false;
                scoutTarget = default;
                scoutWorkCooldown = deferred
                    ? Random.Range(0.12f, 0.24f)
                    : Random.Range(0.8f, 1.6f);
                ResetScoutMovementToIdle();
                if (!deferred)
                {
                    StrategyDebugLogger.Warn(
                        "ScoutLodge",
                        "ScoutMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("lodgeOrigin", lodge.Origin),
                        StrategyDebugLogger.F("target", target),
                        StrategyDebugLogger.F("reason", "no_path"));
                }

                return false;
            }

            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.2f);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ScoutMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("lodgeOrigin", lodge.Origin),
                StrategyDebugLogger.F("target", target));
            return true;
        }

        private bool TryStartPointOfInterestTask(
            StrategyScoutLodge lodge,
            StrategyPointOfInterestController pointController,
            StrategyPointOfInterest pointOfInterest,
            Vector2Int currentCell)
        {
            if (lodge == null || pointController == null || pointOfInterest == null)
            {
                return false;
            }

            activeScoutLodge = lodge;
            activeScoutPointOfInterest = pointOfInterest;
            scoutTarget = pointOfInterest.Cell;
            hasScoutTarget = true;
            if (currentCell == scoutTarget)
            {
                StartInvestigatingPointOfInterest();
                return activity == ResidentActivity.InvestigatingPointOfInterest;
            }

            activity = ResidentActivity.MovingToPointOfInterest;
            if (!TryBuildPathTo(scoutTarget))
            {
                bool deferred = WasLastPathBuildDeferred;
                if (!deferred)
                {
                    pointController.MarkTemporarilyUnreachable(pointOfInterest, this);
                }

                pointController.ReleaseReservation(pointOfInterest, this);
                activeScoutLodge = null;
                activeScoutPointOfInterest = null;
                hasScoutTarget = false;
                scoutTarget = default;
                scoutWorkCooldown = deferred
                    ? Random.Range(0.12f, 0.24f)
                    : Random.Range(0.8f, 1.6f);
                ResetScoutMovementToIdle();
                if (!deferred)
                {
                    StrategyDebugLogger.Warn(
                        "PointOfInterest",
                        "ScoutMoveRejected",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("pointId", pointOfInterest.StableId),
                        StrategyDebugLogger.F("target", pointOfInterest.Cell),
                        StrategyDebugLogger.F("reason", "no_path"));
                }

                return false;
            }

            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.2f);
            lodge.NotifyPointOfInterestTravelStarted(this);
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "ScoutMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pointId", pointOfInterest.StableId),
                StrategyDebugLogger.F("target", pointOfInterest.Cell));
            return true;
        }

        private void StartSurveyingFrontier()
        {
            if (activeScoutLodge == null
                || !hasScoutTarget
                || !activeScoutLodge.IsTargetReservedBy(this, scoutTarget))
            {
                CancelScoutWork();
                return;
            }

            activity = ResidentActivity.SurveyingFrontier;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            scoutSurveyTimer = Random.Range(ScoutSurveySecondsMin, ScoutSurveySecondsMax);
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activeScoutLodge.NotifySurveyStarted(this, scoutTarget);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "FrontierSurveyStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("target", scoutTarget));
        }

        private void UpdateSurveyingFrontier()
        {
            if (activeScoutLodge == null
                || !hasScoutTarget
                || !activeScoutLodge.IsTargetReservedBy(this, scoutTarget))
            {
                CancelScoutWork();
                return;
            }

            scoutSurveyTimer -= Time.deltaTime;
            AnimateIdle();
            if (scoutSurveyTimer > 0f)
            {
                return;
            }

            StrategyScoutLodge completedLodge = activeScoutLodge;
            Vector2Int completedTarget = scoutTarget;
            completedLodge.CompleteExplorationTarget(this, completedTarget);
            activeScoutLodge = null;
            scoutTarget = default;
            hasScoutTarget = false;
            scoutSurveyTimer = 0f;
            scoutWorkCooldown = Random.Range(0.05f, 0.15f);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.05f, 0.15f);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "FrontierSurveyCompleted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("lodgeOrigin", completedLodge.Origin),
                StrategyDebugLogger.F("target", completedTarget));
        }

        private void StartInvestigatingPointOfInterest()
        {
            if (activeScoutLodge == null
                || activeScoutPointOfInterest == null
                || !activeScoutPointOfInterest.IsReservedBy(this))
            {
                CancelScoutWork();
                return;
            }

            activity = ResidentActivity.InvestigatingPointOfInterest;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            pointOfInterestInteractionTimer = Random.Range(
                PointOfInterestInteractionSecondsMin,
                PointOfInterestInteractionSecondsMax);
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activeScoutLodge.NotifyPointOfInterestInvestigationStarted(this);
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "InvestigationStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pointId", activeScoutPointOfInterest.StableId),
                StrategyDebugLogger.F("target", activeScoutPointOfInterest.Cell));
        }

        private void UpdateInvestigatingPointOfInterest()
        {
            if (activeScoutLodge == null
                || activeScoutPointOfInterest == null
                || !activeScoutPointOfInterest.IsReservedBy(this))
            {
                CancelScoutWork();
                return;
            }

            pointOfInterestInteractionTimer -= Time.deltaTime;
            AnimateIdle();
            if (pointOfInterestInteractionTimer > 0f)
            {
                return;
            }

            StrategyScoutLodge completedLodge = activeScoutLodge;
            StrategyPointOfInterest completedPoint = activeScoutPointOfInterest;
            StrategyPointOfInterestController pointController = StrategyPointOfInterestController.Active;
            if (pointController == null || !pointController.CompleteInvestigation(completedPoint, this))
            {
                CancelScoutWork();
                return;
            }

            completedLodge.NotifyPointOfInterestCompleted(this);
            activeScoutLodge = null;
            activeScoutPointOfInterest = null;
            scoutTarget = default;
            hasScoutTarget = false;
            pointOfInterestInteractionTimer = 0f;
            scoutWorkCooldown = Random.Range(0.05f, 0.15f);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.05f, 0.15f);
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "InvestigationCompleted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pointId", completedPoint.StableId),
                StrategyDebugLogger.F("target", completedPoint.Cell));
        }

        private bool CanStartScoutWorkForLodge(StrategyScoutLodge lodge)
        {
            return activity == ResidentActivity.Idle
                && lodge != null
                && lodge == scoutWorkplace
                && CanWork
                && scoutWorkCooldown <= 0f
                && workplace == null
                && stoneWorkplace == null
                && hunterWorkplace == null
                && fisherWorkplace == null
                && foragerWorkplace == null
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
                && !settlementHaulerRole
                && !settlementBuilderRole
                && !deathRequested
                && !hiddenInsideHome
                && !hiddenUnderground
                && !IsPendingRefugee
                && activeScoutLodge == null
                && activeScoutPointOfInterest == null
                && !hasScoutTarget;
        }

        private void CancelScoutWork()
        {
            if (activeScoutLodge != null)
            {
                activeScoutLodge.ReleaseExplorationTarget(this);
            }

            if (activeScoutPointOfInterest != null)
            {
                activeScoutLodge?.NotifyPointOfInterestInterrupted(this);
                StrategyPointOfInterestController.Active?.ReleaseReservation(
                    activeScoutPointOfInterest,
                    this);
            }

            activeScoutLodge = null;
            activeScoutPointOfInterest = null;
            scoutTarget = default;
            hasScoutTarget = false;
            scoutSurveyTimer = 0f;
            pointOfInterestInteractionTimer = 0f;
            if (IsScoutActivity(activity))
            {
                ResetScoutMovementToIdle();
                waitTimer = Random.Range(0.3f, 0.85f);
            }
        }

        private void LeaveNightRestForScoutDuty()
        {
            if (sleepingInsideHome)
            {
                ReleaseNightSleep(false);
            }
            else if (returningHomeToSleep)
            {
                CancelNightSleepReturn();
            }

            if (sleepingAtHomelessCamp || returningToHomelessCamp || relightingCampfire)
            {
                ReleaseHomelessCampSleep(false);
            }
        }

        private void ResetScoutMovementToIdle()
        {
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private static bool IsScoutActivity(ResidentActivity residentActivity)
        {
            return residentActivity is ResidentActivity.MovingToScoutFrontier
                or ResidentActivity.SurveyingFrontier
                or ResidentActivity.MovingToPointOfInterest
                or ResidentActivity.InvestigatingPointOfInterest;
        }
    }
}
