using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private bool UpdateAge()
        {
            ageYears += Time.deltaTime / SecondsPerYear;
            if (lifeStage == StrategyResidentLifeStage.Child && ageYears >= AdultAgeYears)
            {
                GrowUp();
            }

            int currentAge = DisplayAgeYears;
            if (currentAge <= lastMortalityAgeChecked)
            {
                return false;
            }

            for (int age = lastMortalityAgeChecked + 1; age <= currentAge; age++)
            {
                if (population != null && population.TryResolveAnnualMortality(this, age))
                {
                    return true;
                }
            }

            lastMortalityAgeChecked = currentAge;
            return false;
        }

        private void GrowUp()
        {
            CancelChildPlay(false);
            lifeStage = StrategyResidentLifeStage.Adult;
            ageYears = Mathf.Max(ageYears, AdultAgeYears);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            UseIdleSprite();
            EnsureClickCollider();
            home?.EnsureHouseholder();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGrownUp",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("age", DisplayAgeYears));
        }

        private void EnsureClickCollider()
        {
            CircleCollider2D circle = GetComponent<CircleCollider2D>();
            if (circle == null)
            {
                circle = gameObject.AddComponent<CircleCollider2D>();
            }

            circle.isTrigger = true;
            circle.offset = IsAdult ? new Vector2(0f, 0.36f) : new Vector2(0f, 0.25f);
            circle.radius = IsAdult ? 0.28f : 0.21f;
        }

        private void AnimateIdle()
        {
            if (ShouldUsePersonalNightTorch())
            {
                UseNightTorchCarrySprite();
                footstepAudio?.ResetStepPhase();
                SyncReadabilityRenderers();
                return;
            }

            UseIdleSprite();
            footstepAudio?.ResetStepPhase();
            SyncReadabilityRenderers();
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 5f) * 0.035f;
            transform.localScale = new Vector3(1f, pulse, 1f);
        }

        private void AnimateWalk()
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
                walkFrame = (walkFrame + frameSteps) % StrategyResidentSpriteFactory.WalkFrameCount;
                walkFrameTimer -= frameSteps;
            }

            if (appliedWalkFrame != walkFrame)
            {
                spriteRenderer.sprite = StrategyResidentSpriteFactory.GetWalkSprite(gender, VisualVariant, lifeStage, walkFrame);
                appliedWalkFrame = walkFrame;
                footstepAudio?.PlayWalkFrame(walkFrame, lifeStage);
                SyncReadabilityRenderers();
            }
        }

        private void EnsureFootstepAudio()
        {
            if (footstepAudio != null)
            {
                return;
            }

            footstepAudio = GetComponent<StrategyResidentFootstepAudio>();
            if (footstepAudio == null)
            {
                footstepAudio = gameObject.AddComponent<StrategyResidentFootstepAudio>();
            }

            footstepAudio.Configure(this);
        }

        private void AnimateGardenWork()
        {
            UseIdleSprite();
            SyncReadabilityRenderers();
            float swing = Mathf.Sin((Time.time + bobPhase) * 8.5f);
            float bend = Mathf.Abs(swing);
            transform.localRotation = Quaternion.Euler(0f, 0f, swing * 5.5f);
            transform.localScale = new Vector3(1f + bend * 0.035f, 0.94f - bend * 0.045f, 1f);
        }

        private void AnimateChoppingWork()
        {
            AnimateWoodcutWork(false);
        }

        private void AnimateBuckingWork()
        {
            AnimateWoodcutWork(true);
        }

        private void AnimateWoodcutWork(bool bucking)
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

            workFrameTimer += Time.deltaTime * GetLumberWorkAnimationRate();
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.WoodcutFrameCount;
                    if (workFrame == WoodcutImpactFrame)
                    {
                        if (activeTree != null)
                        {
                            if (bucking)
                            {
                                activeTree.ReceiveBuckHit(transform.position);
                            }
                            else
                            {
                                activeTree.ReceiveChopHit(transform.position);
                            }

                            PlayAxeHitSfx();
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyWoodcutFrame(workFrame);
        }

        private void AnimateWoodcutHold()
        {
            if (activeTree != null)
            {
                FaceWorldPoint(activeTree.transform.position);
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            ApplyWoodcutFrame(7);
        }

        private void ApplyWoodcutFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetWoodcutSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateStonecutWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (activeStoneDeposit != null)
            {
                FaceWorldPoint(activeStoneDeposit.transform.position);
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

            workFrameTimer += Time.deltaTime * GetStonecutWorkAnimationRate();
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.StonecutFrameCount;
                    if (workFrame == StonecutImpactFrame && activeStoneDeposit != null)
                    {
                        if (stoneWorkplace != null && !stoneWorkplace.HasStorageSpace)
                        {
                            ResetStoneWorkToIdle();
                            return;
                        }

                        activeStoneDeposit.ReceivePickHit(this, transform.position, out int minedAmount);
                        PlayPickaxeHitSfx();
                        if (minedAmount > 0)
                        {
                            ApplyStonecutFrame(workFrame);
                            StartCarryingMinedStone(minedAmount);
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyStonecutFrame(workFrame);
        }

        private void ApplyStonecutFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetStonecutSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateConstructionWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (constructionSite != null)
            {
                FaceWorldPoint(constructionSite.FootprintBounds.center);
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

            workFrameTimer += Time.deltaTime * ConstructionAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.ConstructionFrameCount;
                    if (workFrame == ConstructionImpactFrame && constructionSite != null)
                    {
                        if (!constructionSite.ConsumeReservedBuildWork(this, transform.position))
                        {
                            ResetConstructionWorkToIdle();
                            return;
                        }

                        PlayHammerHitSfx();
                        if (constructionSite == null || constructionSite.IsCompleted)
                        {
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyConstructionFrame(workFrame);
        }

        private void ApplyConstructionFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetConstructionSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateBowWork()
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

            workFrameTimer += Time.deltaTime * BowAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.BowFrameCount;
                    if (workFrame == BowReleaseFrame && !bowShotReleased && activeHuntTarget != null)
                    {
                        bowShotReleased = true;
                        huntingWorkTimer = 0.28f;
                        PlayBowShotSfx();
                        ReleaseHuntingArrow();
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyBowFrame(workFrame);
        }

        private void ApplyBowFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetBowSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void AnimateButcherWork()
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

            workFrameTimer += Time.deltaTime * ButcherAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.ButcherFrameCount;
                    if (workFrame == ButcherImpactFrame && activeHuntTarget != null)
                    {
                        if (hunterWorkplace != null && !hunterWorkplace.HasStorageSpace)
                        {
                            ResetHunterWorkToIdle(true);
                            return;
                        }

                        activeHuntTarget.ReceiveButcherHit(this, transform.position, out int gameAmount);
                        if (gameAmount > 0)
                        {
                            ApplyButcherFrame(workFrame);
                            StartCarryingGame(gameAmount);
                            return;
                        }
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyButcherFrame(workFrame);
        }

        private void ApplyButcherFrame(int frame)
        {
            if (spriteRenderer == null || appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = StrategyResidentSpriteFactory.GetButcherSprite(gender, VisualVariant, frame);
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }
    }
}
