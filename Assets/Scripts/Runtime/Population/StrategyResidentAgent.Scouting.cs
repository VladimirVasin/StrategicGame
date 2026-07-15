using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float ScoutSurveySecondsMin = 2.5f;
        private const float ScoutSurveySecondsMax = 3.5f;
        private const float ScoutRouteRetrySecondsMin = 0.35f;
        private const float ScoutRouteRetrySecondsMax = 0.75f;

        private StrategyScoutLodge scoutWorkplace;
        private StrategyScoutLodge activeScoutLodge;
        private Vector2Int scoutTarget;
        private float scoutWorkCooldown;
        private float scoutSurveyTimer;
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
            if (map.TryWorldToCell(transform.position, out Vector2Int currentCell)
                && currentCell == target)
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
                && !hasScoutTarget;
        }

        private void CancelScoutWork()
        {
            if (activeScoutLodge != null)
            {
                activeScoutLodge.ReleaseExplorationTarget(this);
            }

            activeScoutLodge = null;
            scoutTarget = default;
            hasScoutTarget = false;
            scoutSurveyTimer = 0f;
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
                or ResidentActivity.SurveyingFrontier;
        }
    }
}
