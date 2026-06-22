using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float CampfireRelightSeconds = 4.8f;
        private const float CampfireKindleFrameRate = 7.2f;
        private const float GroundSleepFrameRate = 1.8f;

        private Vector2Int homelessCampSleepCell;
        private float homelessCampActionTimer;
        private bool hasHomelessCampSleepCell;
        private bool returningToHomelessCamp;
        private bool sleepingAtHomelessCamp;
        private bool relightingCampfire;

        public bool IsSleepingAtCampfire => sleepingAtHomelessCamp;

        private bool UpdateHomelessCampSleepState()
        {
            if (sleepingAtHomelessCamp)
            {
                UpdateSleepingByCampfire();
                return true;
            }

            if (relightingCampfire || activity == ResidentActivity.LightingCampfire)
            {
                UpdateLightingCampfire();
                return true;
            }

            if (returningToHomelessCamp && !IsNightSleepTime())
            {
                CancelHomelessCampSleepReturn();
                return false;
            }

            return TryStartHomelessCampSleep();
        }

        private bool TryStartHomelessCampSleep()
        {
            if (!CanStartHomelessCampSleep()
                || population == null
                || population.HomelessCamp == null)
            {
                return false;
            }

            StrategyHomelessCampController camp = population.HomelessCamp;
            if (!camp.TryReserveReachableSleepSpot(this, TryBuildPathTo, out Vector2Int sleepCell))
            {
                waitTimer = Random.Range(0.75f, 1.65f);
                StrategyDebugLogger.Warn(
                    "HomelessCamp",
                    "ResidentCampSleepPathRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("campCell", camp.CampCell));
                return false;
            }

            homelessCampSleepCell = sleepCell;
            hasHomelessCampSleepCell = true;
            returningToHomelessCamp = true;
            sleepingAtHomelessCamp = false;
            relightingCampfire = false;
            activity = ResidentActivity.MovingToCampfireSleep;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            StrategyDebugLogger.Info(
                "HomelessCamp",
                "ResidentGoingToCampSleep",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("sleepCell", sleepCell),
                StrategyDebugLogger.F("campCell", camp.CampCell));
            return true;
        }

        private bool CanStartHomelessCampSleep()
        {
            return IsNightSleepTime()
                && home == null
                && !hiddenInsideHome
                && !hiddenUnderground
                && !IsPendingRefugee
                && !deathRequested
                && !IsHomeboundYoungChild
                && !IsFuneralActivity(activity)
                && !HasAnyCarriedResource()
                && (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
                && !returningToHomelessCamp
                && !sleepingAtHomelessCamp
                && !relightingCampfire;
        }

        private void EnterHomelessCampSleepSpot()
        {
            returningToHomelessCamp = false;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (population == null || population.HomelessCamp == null || !hasHomelessCampSleepCell)
            {
                CancelHomelessCampSleepReturn();
                return;
            }

            StrategyHomelessCampController camp = population.HomelessCamp;
            transform.position = camp.GetSleepWorld(this, homelessCampSleepCell);
            FaceWorldPoint(camp.transform.position);
            if (camp.NeedsRelight && camp.TryReserveRelight(this))
            {
                StartLightingCampfire(camp);
                return;
            }

            StartSleepingByCampfire(camp);
        }

        private void StartLightingCampfire(StrategyHomelessCampController camp)
        {
            activity = ResidentActivity.LightingCampfire;
            relightingCampfire = true;
            sleepingAtHomelessCamp = false;
            homelessCampActionTimer = CampfireRelightSeconds;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            workFrame = Mathf.Abs(residentId) % StrategyResidentSpriteFactory.CampfireKindleFrameCount;
            workFrameTimer = 0f;
            camp.BeginRelight(this, CampfireRelightSeconds);
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "HomelessCamp",
                "ResidentLightingCampfire",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("sleepCell", homelessCampSleepCell));
        }

        private void UpdateLightingCampfire()
        {
            if (!IsNightSleepTime())
            {
                ReleaseHomelessCampSleep(true);
                return;
            }

            StrategyHomelessCampController camp = population != null ? population.HomelessCamp : null;
            if (camp == null || !camp.IsRelightReservedBy(this))
            {
                StartSleepingByCampfire(camp);
                return;
            }

            AnimateCampfireKindling();
            homelessCampActionTimer -= Time.deltaTime;
            if (homelessCampActionTimer > 0f && camp.Campfire != null && !camp.Campfire.IsLit)
            {
                return;
            }

            camp.CompleteRelight(this);
            StartSleepingByCampfire(camp);
        }

        private void StartSleepingByCampfire(StrategyHomelessCampController camp)
        {
            activity = ResidentActivity.SleepingByCampfire;
            returningToHomelessCamp = false;
            sleepingAtHomelessCamp = true;
            relightingCampfire = false;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.8f, 1.8f);
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            workFrame = Mathf.Abs(residentId) % StrategyResidentSpriteFactory.GroundSleepFrameCount;
            workFrameTimer = Random.Range(0f, 0.8f);
            if (camp != null && hasHomelessCampSleepCell)
            {
                transform.position = camp.GetSleepWorld(this, homelessCampSleepCell);
                FaceWorldPoint(camp.transform.position);
            }

            ApplyGroundSleepFrame();
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "HomelessCamp",
                "ResidentSleptByCampfire",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("sleepCell", homelessCampSleepCell));
        }

        private void UpdateSleepingByCampfire()
        {
            if (!IsNightSleepTime())
            {
                ReleaseHomelessCampSleep(true);
                return;
            }

            if (population != null && population.HomelessCamp != null && hasHomelessCampSleepCell)
            {
                transform.position = population.HomelessCamp.GetSleepWorld(this, homelessCampSleepCell);
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            footstepAudio?.ResetStepPhase();
            ApplyGroundSleepFrame();
        }

        private void ReleaseHomelessCampSleep(bool log)
        {
            StrategyHomelessCampController camp = population != null ? population.HomelessCamp : null;
            camp?.Release(this);
            bool wasSleeping = sleepingAtHomelessCamp || relightingCampfire || returningToHomelessCamp;
            returningToHomelessCamp = false;
            sleepingAtHomelessCamp = false;
            relightingCampfire = false;
            hasHomelessCampSleepCell = false;
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 1.15f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            UpdateWorldSorting();
            if (log && wasSleeping)
            {
                StrategyDebugLogger.Info(
                    "HomelessCamp",
                    "ResidentWokeAtCampfire",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId));
            }
        }

        private void CancelHomelessCampSleepReturn()
        {
            ReleaseHomelessCampSleep(false);
        }

        private void AnimateCampfireKindling()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = true;
            workFrameTimer += Time.deltaTime * CampfireKindleFrameRate;
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

        private void ApplyGroundSleepFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            usingWalkSprite = false;
            usingWorkSprite = true;
            workFrameTimer += Time.deltaTime * GroundSleepFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                workFrame = (workFrame + frameSteps) % StrategyResidentSpriteFactory.GroundSleepFrameCount;
                workFrameTimer -= frameSteps;
            }

            Sprite sprite = StrategyResidentSpriteFactory.GetGroundSleepSprite(
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
