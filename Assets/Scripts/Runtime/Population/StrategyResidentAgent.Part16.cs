using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void AnimateFishingWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * GetFishingWorkAnimationRate();
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.FishingFrameCount;
                    if (activity == ResidentActivity.CastingFishingLine && workFrame == FishingHookFrame && !fishingLineCast)
                    {
                        fishingLineCast = true;
                        bool hooked = activeFishTarget != null && activeFishTarget.ReceiveFishingHook(this, GetFishingBobberWorld());
                        if (!hooked)
                        {
                            ResetFisherWorkToIdle(false);
                            return;
                        }

                        StrategyDebugLogger.Info(
                            "Fishing",
                            "LineCast",
                            StrategyDebugLogger.F("resident", FullName),
                            StrategyDebugLogger.F("fishWorld", activeFishTarget != null ? activeFishTarget.transform.position : Vector3.zero));
                    }
                    else if (activity == ResidentActivity.ReelingFish && workFrame == FishingReelFrame && activeFishTarget != null)
                    {
                        if (fisherWorkplace != null && !fisherWorkplace.HasStorageSpace)
                        {
                            ResetFisherWorkToIdle(true);
                            return;
                        }

                        activeFishTarget.ReceiveReelPull(this, GetFishingReelTargetWorld(), out int fishAmount);
                        if (fishAmount > 0)
                        {
                            ApplyFishingFrame(workFrame);
                            StartCarryingFish(fishAmount);
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyFishingFrame(workFrame);
            SyncFishingLineRenderer();
        }

        private void ApplyFishingFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetFishingSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private Vector3 GetFishingBobberWorld()
        {
            if (activeFishTarget != null)
            {
                if (activeFishTarget.IsHooked)
                {
                    return activeFishTarget.FishingHookWorld;
                }

                Vector3 fishWorld = activeFishTarget.transform.position;
                float bob = Mathf.Sin((Time.time + bobPhase) * 8.0f) * 0.035f;
                return new Vector3(fishWorld.x, fishWorld.y + 0.12f + bob, -0.11f);
            }

            float side = spriteRenderer != null && spriteRenderer.flipX ? -0.85f : 0.85f;
            return new Vector3(transform.position.x + side, transform.position.y + 0.10f, -0.11f);
        }

        private Vector3 GetFishingRodTipWorld()
        {
            if (spriteRenderer == null)
            {
                return new Vector3(transform.position.x, transform.position.y + 0.52f, -0.11f);
            }

            return new Vector3(
                transform.position.x + (spriteRenderer.flipX ? -0.20f : 0.20f),
                transform.position.y + 0.52f,
                -0.11f);
        }

        private Vector3 GetFishingReelTargetWorld()
        {
            Vector3 fishWorld = activeFishTarget != null
                ? activeFishTarget.transform.position
                : GetFishingBobberWorld();
            Vector3 towardFish = fishWorld - transform.position;
            towardFish.z = 0f;
            if (towardFish.sqrMagnitude < 0.001f)
            {
                float side = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
                towardFish = new Vector3(side, 0f, 0f);
            }

            towardFish.Normalize();
            Vector3 target = transform.position + towardFish * 0.48f;
            return new Vector3(target.x, target.y + 0.06f, -0.068f);
        }

        private Vector3 GetBowWorldPosition()
        {
            float side = spriteRenderer != null && spriteRenderer.flipX ? -0.22f : 0.22f;
            return new Vector3(transform.position.x + side, transform.position.y + 0.42f, -0.11f);
        }

        private void AnimateLumberWork(float frequency, float angle)
        {
            UseIdleSprite();
            SyncReadabilityRenderers();
            float swing = Mathf.Sin((Time.time + bobPhase) * frequency);
            float force = Mathf.Abs(swing);
            transform.localRotation = Quaternion.Euler(0f, 0f, swing * angle);
            transform.localScale = new Vector3(1f + force * 0.045f, 0.93f - force * 0.050f, 1f);
        }

        private void UseIdleSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Sprite idleSprite = StrategyResidentSpriteFactory.GetSprite(gender, VisualVariant, lifeStage);
            if (usingWalkSprite || usingWorkSprite || spriteRenderer.sprite != idleSprite)
            {
                spriteRenderer.sprite = idleSprite;
                SyncReadabilityRenderers();
            }

            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            walkFrame = 0;
            walkFrameTimer = 0f;
            workFrame = 0;
            workFrameTimer = 0f;
        }

        private void CompleteGardenWork()
        {
            if (activeGarden == null
                || activeGarden.Owner == null
                || activeGarden.Owner.Resources == null
                || activeGarden.ProducedResource == StrategyResourceType.None)
            {
                return;
            }

            activeGarden.BoostGardenGrowthFromWork();
            StrategyDebugLogger.Info(
                "Population",
                "HouseholderGardenWorkCompleted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("gardenOrigin", activeGarden.Origin),
                StrategyDebugLogger.F("crop", activeGarden.ProducedResource));
        }

        private void CancelLumberWork()
        {
            if (this == null)
            {
                return;
            }

            if (activeTree != null)
            {
                activeTree.Release(this);
            }

            activeTree = null;

            if (activity == ResidentActivity.MovingToTree
                || activity == ResidentActivity.ChoppingTree
                || activity == ResidentActivity.BuckingTree
                || activity == ResidentActivity.MovingToLogs
                || activity == ResidentActivity.CarryingLogs
                || activity == ResidentActivity.DepositingLogs
                || activity == ResidentActivity.MovingToPlantTree
                || activity == ResidentActivity.PlantingTree)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.75f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                if (carriedLogAmount > 0 && TryStartCarriedResourceReturn("lumber_work_cancelled"))
                {
                    return;
                }

                carriedLogAmount = 0;
                SetCarriedLogsVisible(false);
                UseIdleSprite();
            }
        }

        private void CancelStoneWork()
        {
            if (this == null)
            {
                return;
            }

            if (activeStoneDeposit != null)
            {
                activeStoneDeposit.Release(this);
            }

            activeStoneDeposit = null;

            if (activity == ResidentActivity.MovingToStone
                || activity == ResidentActivity.MiningStone
                || activity == ResidentActivity.CarryingStone
                || activity == ResidentActivity.DepositingStone)
            {
                activity = ResidentActivity.Idle;
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.75f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                if (carriedStoneAmount > 0 && TryStartCarriedResourceReturn("stone_work_cancelled"))
                {
                    return;
                }

                carriedStoneAmount = 0;
                SetCarriedStoneVisible(false);
                UseIdleSprite();
            }
        }

        private void CancelGranaryWork(bool storeCarriedFood)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToGranaryGamePickup
                || activity == ResidentActivity.PickingUpGranaryGame
                || activity == ResidentActivity.CarryingGameToGranary
                || activity == ResidentActivity.DepositingGranaryGame
                || activity == ResidentActivity.MovingToGranaryFishPickup
                || activity == ResidentActivity.PickingUpGranaryFish
                || activity == ResidentActivity.CarryingFishToGranary
                || activity == ResidentActivity.DepositingGranaryFish
                || activity == ResidentActivity.MovingToGranaryForagePickup
                || activity == ResidentActivity.PickingUpGranaryForage
                || activity == ResidentActivity.CarryingForageToGranary
                || activity == ResidentActivity.DepositingGranaryForage)
            {
                ResetGranaryWorkToIdle(storeCarriedFood);
            }
            else if (activeGameSource != null)
            {
                activeGameSource.ReleaseStoredGameReservation(this);
                activeGameSource = null;
            }
            else if (activeFishSource != null)
            {
                activeFishSource.ReleaseStoredFishReservation(this);
                activeFishSource = null;
            }
            else if (activeForageFoodSource != null)
            {
                activeForageFoodSource.ReleaseStoredForageReservation(this);
                activeForageFoodSource = null;
            }
            else if (activeEggFoodSource != null)
            {
                activeEggFoodSource.ReleaseStoredEggsReservation(this);
                activeEggFoodSource = null;
            }
        }

        private void CancelHouseholdFoodWork(bool storeCarriedFood, string reason = "cancelled")
        {
            if (this == null)
            {
                return;
            }

            if (IsHouseholdFoodActivity(activity))
            {
                ResetHouseholdFoodWorkToIdle(storeCarriedFood, reason);
                return;
            }

            ReleaseActiveHouseholdFoodReservation();
            StoreCarriedHouseholdLogsOnCancel(storeCarriedFood, reason);

            if (storeCarriedFood
                && carriedHouseholdFoodResource != StrategyResourceType.None
                && GetCarriedHouseholdFoodAmount() > 0
                && home != null
                && home.Resources != null)
            {
                home.Resources.AddResource(carriedHouseholdFoodResource, GetCarriedHouseholdFoodAmount());
                StrategyDebugLogger.Info(
                    "Household",
                    "HouseholderFoodStoredOnCancel",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("resource", carriedHouseholdFoodResource),
                    StrategyDebugLogger.F("amount", GetCarriedHouseholdFoodAmount()),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
                ClearCarriedHouseholdFood();
                SetCarriedGameVisible(false);
                SetCarriedFishVisible(false);
                SetCarriedForageVisible(false);
            }
        }

        private void CancelHunterWork(bool storeCarriedGame)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToHuntingRange
                || activity == ResidentActivity.AimingBow
                || activity == ResidentActivity.WaitingForHuntHit
                || activity == ResidentActivity.MovingToHuntCarcass
                || activity == ResidentActivity.ButcheringRabbit
                || activity == ResidentActivity.CarryingGame
                || activity == ResidentActivity.DepositingGame)
            {
                ResetHunterWorkToIdle(true);
                return;
            }

            if (activeHuntTarget != null)
            {
                activeHuntTarget.ReleaseHuntReservation(this);
                activeHuntTarget = null;
            }

            if (storeCarriedGame
                && carriedGameAmount > 0
                && TryStartCarriedResourceReturn("hunter_work_cancelled"))
            {
                return;
            }

            carriedGameAmount = 0;
            SetCarriedGameVisible(false);
        }

        private void CancelFisherWork(bool storeCarriedFish)
        {
            if (this == null)
            {
                return;
            }

            if (activity == ResidentActivity.MovingToFishingSpot
                || activity == ResidentActivity.CastingFishingLine
                || activity == ResidentActivity.WaitingForFishBite
                || activity == ResidentActivity.ReelingFish
                || activity == ResidentActivity.CarryingFish
                || activity == ResidentActivity.DepositingFish)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            if (activeFishTarget != null)
            {
                activeFishTarget.ReleaseFishingReservation(this);
                activeFishTarget = null;
            }

            if (storeCarriedFish
                && carriedFishAmount > 0
                && TryStartCarriedResourceReturn("fisher_work_cancelled"))
            {
                return;
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            SetFishingLineVisible(false);
        }

        private void UpdateFuneralActivity()
        {
            AnimateFuneralPose();
            if (activity == ResidentActivity.WaitingAtFuneral)
            {
                funeralTimer -= Time.deltaTime;
                if (funeralTimer <= 0f)
                {
                    StrategyDebugLogger.Warn(
                        "Funeral",
                        "ResidentFuneralDutyAutoReleased",
                        StrategyDebugLogger.F("resident", FullName),
                        StrategyDebugLogger.F("reason", "waiting_timeout"));
                    EndFuneralDuty();
                }

                return;
            }

            funeralTimer -= Time.deltaTime;
            if (funeralTimer > 0f)
            {
                return;
            }

            activity = ResidentActivity.WaitingAtFuneral;
            funeralTimer = FuneralWaitingAutoReleaseSeconds;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }
    }
}
