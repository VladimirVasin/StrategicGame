using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float NightLightActionSecondsMin = 2.15f;
        private const float NightLightActionSecondsMax = 3.65f;
        private const float NightLightKindleFrameRate = 7.0f;

        private StrategyNightLightSource activeNightLightSource;
        private Vector2Int activeNightLightWorkCell;
        private float nightLightActionTimer;

        public bool CanAcceptNightLightTask
        {
            get
            {
                if (!IsAdult
                    || home == null
                    || deathRequested
                    || IsPendingRefugee
                    || IsHomeboundYoungChild
                    || IsFuneralActivity(activity)
                    || HasAnyCarriedResource())
                {
                    return false;
                }

                return activity == ResidentActivity.Idle
                    || activity == ResidentActivity.TendingHousehold
                    || activity == ResidentActivity.StayingInsideHome
                    || (activity == ResidentActivity.MovingHome && returningHomeToSleep);
            }
        }

        internal bool TryStartNightLightTask(StrategyNightLightSource source, Vector2Int workCell)
        {
            if (source == null
                || !source.IsReservedBy(this)
                || !CanAcceptNightLightTask
                || map == null)
            {
                return false;
            }

            LeaveHomeForNightLightTask();
            activeNightLightSource = source;
            activeNightLightWorkCell = workCell;
            activity = ResidentActivity.MovingToNightLight;
            if (!TryBuildPathTo(workCell))
            {
                CancelNightLightTask("no_path");
                return false;
            }

            hasTarget = path.Count > 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "NightLights",
                "ResidentNightLightTaskStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("source", source.SourceKind),
                StrategyDebugLogger.F("workCell", workCell),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
            return hasTarget;
        }

        private bool IsNightLightActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToNightLight
                || residentActivity == ResidentActivity.LightingNightLight;
        }

        private void StartLightingNightLight()
        {
            if (activeNightLightSource == null
                || !activeNightLightSource.IsReservedBy(this)
                || !IsNightSleepTime())
            {
                CancelNightLightTask("missing_source");
                FinishNightLightDuty();
                return;
            }

            activity = ResidentActivity.LightingNightLight;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            nightLightActionTimer = Random.Range(NightLightActionSecondsMin, NightLightActionSecondsMax);
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            workFrame = Mathf.Abs(residentId) % StrategyResidentSpriteFactory.CampfireKindleFrameCount;
            workFrameTimer = 0f;
            FaceWorldPoint(activeNightLightSource.WorldPosition);
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "NightLights",
                "ResidentLightingNightLight",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("source", activeNightLightSource.SourceKind),
                StrategyDebugLogger.F("workCell", activeNightLightWorkCell));
        }

        private void UpdateLightingNightLight()
        {
            if (!IsNightSleepTime())
            {
                CancelNightLightTask("daylight");
                FinishNightLightDuty();
                return;
            }

            if (activeNightLightSource == null || !activeNightLightSource.IsReservedBy(this))
            {
                FinishNightLightDuty();
                return;
            }

            AnimateCampfireKindlingForNightLight();
            FaceWorldPoint(activeNightLightSource.WorldPosition);
            nightLightActionTimer -= Time.deltaTime;
            if (nightLightActionTimer > 0f)
            {
                return;
            }

            bool completed = activeNightLightSource.CompleteLighting(this);
            StrategyDebugLogger.Info(
                "NightLights",
                completed ? "NightLightIgnited" : "NightLightIgniteFailed",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("source", activeNightLightSource.SourceKind),
                StrategyDebugLogger.F("workCell", activeNightLightWorkCell));
            activeNightLightSource = null;
            PrepareForNextNightLightTask();
            if (StrategyNightLightTaskController.Active != null
                && StrategyNightLightTaskController.Active.TryStartNextTaskForResident(this))
            {
                return;
            }

            FinishNightLightDuty();
        }

        private void CancelNightLightTask(string reason)
        {
            if (activeNightLightSource != null)
            {
                activeNightLightSource.ReleaseReservation(this);
                StrategyDebugLogger.Info(
                    "NightLights",
                    "ResidentNightLightTaskCancelled",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("reason", reason),
                    StrategyDebugLogger.F("source", activeNightLightSource.SourceKind));
            }

            activeNightLightSource = null;
        }

        private void FinishNightLightDuty()
        {
            activeNightLightSource = null;
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            UseIdleSprite();
            if (TryStartNightSleep())
            {
                return;
            }

            waitTimer = Random.Range(0.35f, 0.90f);
        }

        private void PrepareForNextNightLightTask()
        {
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            UseIdleSprite();
        }

        private void LeaveHomeForNightLightTask()
        {
            if (sleepingInsideHome)
            {
                ReleaseNightSleep(false);
                return;
            }

            if (returningHomeToSleep)
            {
                CancelNightSleepReturn();
            }

            if (!hiddenInsideHome)
            {
                return;
            }

            hiddenInsideHome = false;
            SetWorldPresenceVisible(true);
            transform.position = GetHomeExitWorld();
            UpdateWorldSorting();
        }

        private void AnimateCampfireKindlingForNightLight()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = true;
            workFrameTimer += Time.deltaTime * NightLightKindleFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                workFrame = (workFrame + frameSteps) % StrategyResidentSpriteFactory.CampfireKindleFrameCount;
                workFrameTimer -= frameSteps;
            }

            Sprite sprite = StrategyResidentSpriteFactory.GetCampfireKindleSprite(
                gender,
                VisualVariant,
                lifeStage,
                workFrame);
            if (spriteRenderer.sprite != sprite || appliedWorkFrame != workFrame)
            {
                spriteRenderer.sprite = sprite;
                appliedWorkFrame = workFrame;
                SyncReadabilityRenderers();
            }
        }
    }
}
