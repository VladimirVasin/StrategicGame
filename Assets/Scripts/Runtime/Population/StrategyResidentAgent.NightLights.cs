using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float NightLightActionSecondsMin = 2.15f;
        private const float NightLightActionSecondsMax = 3.65f;
        private const float NightLightTorchFrameRate = 7.0f;

        private StrategyNightLightSource activeNightLightSource;
        private Vector2Int activeNightLightWorkCell;
        private float nightLightActionTimer;
        private bool eveningNightTorchActive;

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
            UseNightTorchCarrySprite();
            EnableNightTorchLight();
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

        internal void SetEveningNightTorchActive(bool active)
        {
            if (eveningNightTorchActive == active)
            {
                if (active && !nightTorchLightActive && ShouldUsePersonalNightTorch() && !hiddenInsideHome)
                {
                    RefreshEveningNightTorch();
                }

                return;
            }

            eveningNightTorchActive = active;
            if (!eveningNightTorchActive)
            {
                if (!IsNightLightActivity(activity))
                {
                    DisableNightTorchLight();
                }

                return;
            }

            RefreshEveningNightTorch();
        }

        private void RefreshEveningNightTorch()
        {
            if (!ShouldUsePersonalNightTorch())
            {
                if (!IsNightLightActivity(activity))
                {
                    DisableNightTorchLight();
                }

                return;
            }

            EnableNightTorchLight();
        }

        private bool ShouldUseNightTorchCarryVisual()
        {
            return activity == ResidentActivity.MovingToNightLight || ShouldUsePersonalNightTorch();
        }

        private bool ShouldKeepNightTorchLightActive()
        {
            return IsNightLightActivity(activity) || ShouldUsePersonalNightTorch();
        }

        private bool ShouldUsePersonalNightTorch()
        {
            return eveningNightTorchActive
                && IsEveningNightTorchTime()
                && CanCarryPersonalNightTorch();
        }

        private bool CanCarryPersonalNightTorch()
        {
            return IsAdult
                && !hiddenInsideHome
                && !hiddenUnderground
                && !deathRequested
                && !IsPendingRefugee
                && !IsHomeboundYoungChild
                && !IsFuneralActivity(activity)
                && !HasAnyCarriedResource()
                && (activity == ResidentActivity.Idle
                    || activity == ResidentActivity.TendingHousehold
                    || activity == ResidentActivity.MovingHome);
        }

        private static bool IsEveningNightTorchTime()
        {
            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            return (snapshot.Phase == StrategyTimeOfDayPhase.Dusk && snapshot.PhaseProgress >= 1f / 3f)
                || snapshot.Phase == StrategyTimeOfDayPhase.Night;
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
            workFrame = Mathf.Abs(residentId) % StrategyResidentSpriteFactory.NightTorchLightFrameCount;
            workFrameTimer = 0f;
            FaceWorldPoint(activeNightLightSource.WorldPosition);
            EnableNightTorchLight();
            UpdateNightTorchLight();
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

            AnimateNightTorchLighting();
            FaceWorldPoint(activeNightLightSource.WorldPosition);
            UpdateNightTorchLight();
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
            DisableNightTorchLight();
        }

        private void FinishNightLightDuty()
        {
            activeNightLightSource = null;
            DisableNightTorchLight();
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
            DisableNightTorchLight();
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

        private void UseNightTorchCarrySprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = true;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            appliedWalkFrame = -1;
            walkFrame = 0;
            walkFrameTimer = 0f;
            ApplyNightTorchWalkFrame(false);
            EnableNightTorchLight();
            UpdateNightTorchLight();
        }

        private void AnimateNightTorchWalk()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWorkSprite = false;
            appliedWorkFrame = -1;

            if (!usingWalkSprite)
            {
                usingWalkSprite = true;
                walkFrame = 0;
                walkFrameTimer = 0f;
                appliedWalkFrame = -1;
            }

            walkFrameTimer += Time.deltaTime * WalkAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(walkFrameTimer);
            if (frameSteps > 0)
            {
                walkFrame = (walkFrame + frameSteps) % StrategyResidentSpriteFactory.NightTorchWalkFrameCount;
                walkFrameTimer -= frameSteps;
            }

            ApplyNightTorchWalkFrame(true);
            UpdateNightTorchLight();
        }

        private void ApplyNightTorchWalkFrame(bool playFootstep)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (appliedWalkFrame == walkFrame && usingWalkSprite)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetNightTorchWalkSprite(
                gender,
                VisualVariant,
                lifeStage,
                walkFrame);
            appliedWalkFrame = walkFrame;
            if (playFootstep)
            {
                footstepAudio?.PlayWalkFrame(walkFrame, lifeStage);
            }

            SyncReadabilityRenderers();
        }

        private void AnimateNightTorchLighting()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = true;
            workFrameTimer += Time.deltaTime * NightLightTorchFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                workFrame = (workFrame + frameSteps) % StrategyResidentSpriteFactory.NightTorchLightFrameCount;
                workFrameTimer -= frameSteps;
            }

            Sprite sprite = StrategyResidentSpriteFactory.GetNightTorchLightSprite(
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

            UpdateNightTorchLight();
        }
    }
}
