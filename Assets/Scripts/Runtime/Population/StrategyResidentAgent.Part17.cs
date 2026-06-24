using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void AnimateFuneralPose()
        {
            footstepAudio?.ResetStepPhase();

            float pulse = Mathf.Sin((Time.time + bobPhase) * 4.2f) * 0.018f;
            if (activity == ResidentActivity.MourningCorpse
                || activity == ResidentActivity.WaitingAtFuneral)
            {
                if (!silentFuneralDuty)
                {
                    ApplyCryingFrame();
                }
                else
                {
                    UseIdleSprite();
                    SyncReadabilityRenderers();
                }

                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time + bobPhase) * 4.6f) * 2.4f);
                transform.localScale = new Vector3(1f, 0.93f + pulse, 1f);
                return;
            }

            UseIdleSprite();
            SyncReadabilityRenderers();
            if (activity == ResidentActivity.BuryingGrave)
            {
                float dig = Mathf.Sin((Time.time + bobPhase) * 11f);
                transform.localRotation = Quaternion.Euler(0f, 0f, dig * 4.5f);
                transform.localScale = new Vector3(1.02f, 0.92f + pulse, 1f);
                return;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time + bobPhase) * 3.1f) * 1.6f);
            transform.localScale = new Vector3(1f, 0.94f + pulse, 1f);
        }

        private void ApplyCryingFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            usingWalkSprite = false;
            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * CryingAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                workFrame = (workFrame + frameSteps) % StrategyResidentSpriteFactory.CryFrameCount;
                workFrameTimer -= frameSteps;
            }

            if (appliedWorkFrame != workFrame)
            {
                spriteRenderer.sprite = StrategyResidentSpriteFactory.GetCryingSprite(
                    gender,
                    VisualVariant,
                    lifeStage,
                    workFrame);
                appliedWorkFrame = workFrame;
                SyncReadabilityRenderers();
            }
        }

        private static bool IsFuneralActivity(ResidentActivity residentActivity)
        {
            return IsFuneralMoveActivity(residentActivity)
                || IsStationaryFuneralActivity(residentActivity);
        }

        private static bool IsFuneralMoveActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToFuneral
                || residentActivity == ResidentActivity.CarryingCorpseToCemetery
                || residentActivity == ResidentActivity.MovingToBurial;
        }

        private static bool IsStationaryFuneralActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MourningCorpse
                || residentActivity == ResidentActivity.BuryingGrave
                || residentActivity == ResidentActivity.WaitingAtFuneral;
        }

        private static bool IsConstructionActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToConstructionStorage
                || residentActivity == ResidentActivity.PickingUpConstructionLogs
                || residentActivity == ResidentActivity.PickingUpConstructionStone
                || residentActivity == ResidentActivity.PickingUpConstructionPlanks
                || residentActivity == ResidentActivity.CarryingConstructionLogs
                || residentActivity == ResidentActivity.CarryingConstructionStone
                || residentActivity == ResidentActivity.CarryingConstructionPlanks
                || residentActivity == ResidentActivity.DepositingConstructionResource
                || residentActivity == ResidentActivity.MovingToConstructionSite
                || residentActivity == ResidentActivity.BuildingConstruction;
        }

        private static bool IsForagingActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToForage
                || residentActivity == ResidentActivity.GatheringForage
                || residentActivity == ResidentActivity.MovingToLooseForagePickup
                || residentActivity == ResidentActivity.PickingUpLooseForage
                || residentActivity == ResidentActivity.CarryingForage
                || residentActivity == ResidentActivity.DepositingForage;
        }

        private static bool IsHouseholdFoodActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToHouseholdFoodPickup
                || residentActivity == ResidentActivity.PickingUpHouseholdFood
                || residentActivity == ResidentActivity.CarryingHouseholdFoodHome
                || residentActivity == ResidentActivity.DepositingHouseholdFood
                || residentActivity == ResidentActivity.MovingToHouseholdPotteryPickup
                || residentActivity == ResidentActivity.PickingUpHouseholdPottery
                || residentActivity == ResidentActivity.CarryingPotteryToHouse
                || residentActivity == ResidentActivity.DepositingHouseholdPottery
                || residentActivity == ResidentActivity.MovingToHouseCooking
                || residentActivity == ResidentActivity.CookingHouseMeal;
        }

        private static bool IsReturningCarriedResourceActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.ReturningLogsToStorage
                || residentActivity == ResidentActivity.ReturningStoneToStorage
                || residentActivity == ResidentActivity.ReturningIronToStorage
                || residentActivity == ResidentActivity.ReturningClayToStorage
                || residentActivity == ResidentActivity.ReturningGameToGranary
                || residentActivity == ResidentActivity.ReturningFishToGranary
                || residentActivity == ResidentActivity.ReturningForageToGranary;
        }

        private void FaceWorldPoint(Vector3 world)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.flipX = transform.position.x > world.x;
            SyncReadabilityRenderers();
        }

        private void SetCarriedLogsVisible(bool visible)
        {
            if (!visible || carriedLogAmount <= 0)
            {
                if (carriedLogsRenderer != null)
                {
                    carriedLogsRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedLogsRenderer();
            if (carriedLogsRenderer == null)
            {
                return;
            }

            carriedLogsRenderer.gameObject.SetActive(true);
            SyncCarriedLogsRenderer();
        }

        private void SetCarriedStoneVisible(bool visible)
        {
            if (!visible || carriedStoneAmount <= 0)
            {
                if (carriedStoneRenderer != null)
                {
                    carriedStoneRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedStoneRenderer();
            if (carriedStoneRenderer == null)
            {
                return;
            }

            carriedStoneRenderer.gameObject.SetActive(true);
            SyncCarriedStoneRenderer();
        }

        private void SetCarriedGameVisible(bool visible)
        {
            if (!visible || carriedGameAmount <= 0)
            {
                if (carriedGameRenderer != null)
                {
                    carriedGameRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedGameRenderer();
            if (carriedGameRenderer == null)
            {
                return;
            }

            carriedGameRenderer.gameObject.SetActive(true);
            SyncCarriedGameRenderer();
        }

        private void SetCarriedFishVisible(bool visible)
        {
            if (!visible || carriedFishAmount <= 0)
            {
                if (carriedFishRenderer != null)
                {
                    carriedFishRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedFishRenderer();
            if (carriedFishRenderer == null)
            {
                return;
            }

            carriedFishRenderer.gameObject.SetActive(true);
            SyncCarriedFishRenderer();
        }

        private void SetCarriedForageVisible(bool visible)
        {
            if (!visible || carriedForageAmount <= 0 || carriedForageResource == StrategyResourceType.None)
            {
                if (carriedForageRenderer != null)
                {
                    carriedForageRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureCarriedForageRenderer();
            if (carriedForageRenderer == null)
            {
                return;
            }

            carriedForageRenderer.gameObject.SetActive(true);
            SyncCarriedForageRenderer();
        }

        private void SetFishingLineVisible(bool visible)
        {
            if (!visible)
            {
                if (fishingLineRenderer != null)
                {
                    fishingLineRenderer.gameObject.SetActive(false);
                }

                if (fishingBobberRenderer != null)
                {
                    fishingBobberRenderer.gameObject.SetActive(false);
                }

                return;
            }

            EnsureFishingRenderers();
            if (fishingLineRenderer == null || fishingBobberRenderer == null)
            {
                return;
            }

            fishingLineRenderer.gameObject.SetActive(true);
            fishingBobberRenderer.gameObject.SetActive(true);
            SyncFishingLineRenderer();
        }

        private void EnsureReadabilityRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (readabilityShadowSprite == null)
            {
                readabilityShadowSprite = CreateReadabilityShadowSprite();
            }

            if (shadowRenderer == null)
            {
                GameObject shadowObject = new GameObject("Resident Readability Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowObject.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
                shadowObject.transform.localScale = new Vector3(1.05f, 0.68f, 1f);
                shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = readabilityShadowSprite;
                shadowRenderer.color = new Color(0.02f, 0.03f, 0.025f, 0.34f);
            }

            if (outlineRenderer == null)
            {
                GameObject outlineObject = new GameObject("Resident Readability Outline");
                outlineObject.transform.SetParent(transform, false);
                outlineObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                outlineObject.transform.localScale = Vector3.one * ReadabilityOutlineScale;
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.color = new Color(0.02f, 0.04f, 0.03f, 0.70f);
            }
        }

        private void EnsureCarriedLogsRenderer()
        {
            if (spriteRenderer == null || carriedLogsRenderer != null)
            {
                return;
            }

            GameObject logsObject = new GameObject("Carried Logs");
            logsObject.transform.SetParent(transform, false);
            carriedLogsRenderer = logsObject.AddComponent<SpriteRenderer>();
            carriedLogsRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedLogsSprite();
            carriedLogsRenderer.color = Color.white;
            carriedLogsRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedStoneRenderer()
        {
            if (spriteRenderer == null || carriedStoneRenderer != null)
            {
                return;
            }

            GameObject stoneObject = new GameObject("Carried Stone");
            stoneObject.transform.SetParent(transform, false);
            carriedStoneRenderer = stoneObject.AddComponent<SpriteRenderer>();
            carriedStoneRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedStoneSprite();
            carriedStoneRenderer.color = Color.white;
            carriedStoneRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedGameRenderer()
        {
            if (spriteRenderer == null || carriedGameRenderer != null)
            {
                return;
            }

            GameObject gameObject = new GameObject("Carried Game");
            gameObject.transform.SetParent(transform, false);
            carriedGameRenderer = gameObject.AddComponent<SpriteRenderer>();
            carriedGameRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedGameSprite();
            carriedGameRenderer.color = Color.white;
            carriedGameRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedFishRenderer()
        {
            if (spriteRenderer == null || carriedFishRenderer != null)
            {
                return;
            }

            GameObject fishObject = new GameObject("Carried Fish");
            fishObject.transform.SetParent(transform, false);
            carriedFishRenderer = fishObject.AddComponent<SpriteRenderer>();
            carriedFishRenderer.sprite = StrategyNatureSpriteFactory.GetCarriedFishSprite();
            carriedFishRenderer.color = Color.white;
            carriedFishRenderer.gameObject.SetActive(false);
        }

        private void EnsureCarriedForageRenderer()
        {
            if (spriteRenderer == null || carriedForageRenderer != null)
            {
                return;
            }

            GameObject forageObject = new GameObject("Carried Forage");
            forageObject.transform.SetParent(transform, false);
            carriedForageRenderer = forageObject.AddComponent<SpriteRenderer>();
            carriedForageRenderer.sprite = StrategyForageSpriteFactory.GetCarriedSprite(carriedForageResource);
            carriedForageRenderer.color = Color.white;
            carriedForageRenderer.gameObject.SetActive(false);
        }

        private void EnsureFishingRenderers()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (fishingLineSprite == null)
            {
                fishingLineSprite = CreateFishingLineSprite();
            }

            if (fishingBobberSprite == null)
            {
                fishingBobberSprite = CreateFishingBobberSprite();
            }

            if (fishingLineRenderer == null)
            {
                GameObject lineObject = new GameObject("Fishing Line");
                lineObject.transform.SetParent(transform, false);
                fishingLineRenderer = lineObject.AddComponent<SpriteRenderer>();
                fishingLineRenderer.sprite = fishingLineSprite;
                fishingLineRenderer.color = new Color(0.82f, 0.88f, 0.82f, 0.72f);
                fishingLineRenderer.gameObject.SetActive(false);
            }

            if (fishingBobberRenderer == null)
            {
                GameObject bobberObject = new GameObject("Fishing Bobber");
                bobberObject.transform.SetParent(transform, false);
                fishingBobberRenderer = bobberObject.AddComponent<SpriteRenderer>();
                fishingBobberRenderer.sprite = fishingBobberSprite;
                fishingBobberRenderer.color = Color.white;
                fishingBobberRenderer.gameObject.SetActive(false);
            }
        }

    }
}
