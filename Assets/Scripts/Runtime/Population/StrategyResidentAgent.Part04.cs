using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void Update()
        {
            if (map == null || deathRequested)
            {
                return;
            }

            if (UpdateAge())
            {
                return;
            }

            if (IsHomeboundYoungChild)
            {
                UpdateHomeboundChild();
                return;
            }

            if (hiddenInsideHome)
            {
                ReleaseHomeboundChild();
            }

            if (gardenWorkCooldown > 0f)
            {
                gardenWorkCooldown -= Time.deltaTime;
            }

            if (lumberWorkCooldown > 0f)
            {
                lumberWorkCooldown -= Time.deltaTime;
            }

            if (stoneWorkCooldown > 0f)
            {
                stoneWorkCooldown -= Time.deltaTime;
            }

            if (logisticsWorkCooldown > 0f)
            {
                logisticsWorkCooldown -= Time.deltaTime;
            }

            if (huntingWorkCooldown > 0f)
            {
                huntingWorkCooldown -= Time.deltaTime;
            }

            if (fishingWorkCooldown > 0f)
            {
                fishingWorkCooldown -= Time.deltaTime;
            }

            if (householdFoodWorkCooldown > 0f)
            {
                householdFoodWorkCooldown -= Time.deltaTime;
            }

            if (IsStationaryFuneralActivity(activity))
            {
                UpdateFuneralActivity();
                return;
            }

            if (activity == ResidentActivity.WorkingGarden)
            {
                UpdateGardenWork();
                return;
            }

            if (activity == ResidentActivity.GatheringForage)
            {
                UpdateGatheringForage();
                return;
            }

            if (activity == ResidentActivity.PickingUpLooseForage)
            {
                UpdatePickingUpLooseForage();
                return;
            }

            if (activity == ResidentActivity.DepositingForage)
            {
                UpdateDepositingForage();
                return;
            }

            if (activity == ResidentActivity.ChoppingTree)
            {
                UpdateChoppingTree();
                return;
            }

            if (activity == ResidentActivity.BuckingTree)
            {
                UpdateBuckingTree();
                return;
            }

            if (activity == ResidentActivity.DepositingLogs)
            {
                UpdateDepositingLogs();
                return;
            }

            if (activity == ResidentActivity.MiningStone)
            {
                UpdateMiningStone();
                return;
            }

            if (activity == ResidentActivity.DepositingStone)
            {
                UpdateDepositingStone();
                return;
            }

            if (activity == ResidentActivity.PickingUpStorageLogs)
            {
                UpdatePickingUpStorageLogs();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageLogs)
            {
                UpdateDepositingStorageLogs();
                return;
            }

            if (activity == ResidentActivity.PickingUpStorageStone)
            {
                UpdatePickingUpStorageStone();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageStone)
            {
                UpdateDepositingStorageStone();
                return;
            }

            if (activity == ResidentActivity.PickingUpGranaryGame)
            {
                UpdatePickingUpGranaryGame();
                return;
            }

            if (activity == ResidentActivity.DepositingGranaryGame)
            {
                UpdateDepositingGranaryGame();
                return;
            }

            if (activity == ResidentActivity.PickingUpGranaryFish)
            {
                UpdatePickingUpGranaryFish();
                return;
            }

            if (activity == ResidentActivity.DepositingGranaryFish)
            {
                UpdateDepositingGranaryFish();
                return;
            }

            if (activity == ResidentActivity.PickingUpHouseholdFood)
            {
                UpdatePickingUpHouseholdFood();
                return;
            }

            if (activity == ResidentActivity.DepositingHouseholdFood)
            {
                UpdateDepositingHouseholdFood();
                return;
            }

            if (activity == ResidentActivity.PickingUpConstructionLogs
                || activity == ResidentActivity.PickingUpConstructionStone)
            {
                UpdatePickingUpConstructionResource();
                return;
            }

            if (activity == ResidentActivity.DepositingConstructionResource)
            {
                UpdateDepositingConstructionResource();
                return;
            }

            if (activity == ResidentActivity.BuildingConstruction)
            {
                UpdateBuildingConstruction();
                return;
            }

            if (activity == ResidentActivity.AimingBow)
            {
                UpdateAimingBow();
                return;
            }

            if (activity == ResidentActivity.WaitingForHuntHit)
            {
                UpdateWaitingForHuntHit();
                return;
            }

            if (activity == ResidentActivity.ButcheringRabbit)
            {
                UpdateButcheringRabbit();
                return;
            }

            if (activity == ResidentActivity.DepositingGame)
            {
                UpdateDepositingGame();
                return;
            }

            if (activity == ResidentActivity.CastingFishingLine)
            {
                UpdateCastingFishingLine();
                return;
            }

            if (activity == ResidentActivity.WaitingForFishBite)
            {
                UpdateWaitingForFishBite();
                return;
            }

            if (activity == ResidentActivity.ReelingFish)
            {
                UpdateReelingFish();
                return;
            }

            if (activity == ResidentActivity.DepositingFish)
            {
                UpdateDepositingFish();
                return;
            }

            if (activity == ResidentActivity.PlantingTree)
            {
                UpdatePlantingTree();
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                AnimateIdle();
                return;
            }

            if (!hasTarget || pathIndex >= path.Count)
            {
                if (IsReturningCarriedResourceActivity(activity))
                {
                    if (!TryStartCarriedResourceReturn("resource_return_retry", true))
                    {
                        ScheduleCarriedResourceReturnRetry();
                    }

                    return;
                }

                if (TryStartHouseholdFoodPickupTask())
                {
                    return;
                }

                if (TryStartGardenTask())
                {
                    return;
                }

                if (TryStartLumberTask())
                {
                    return;
                }

                if (TryStartStoneTask())
                {
                    return;
                }

                if (TryStartStorageTask())
                {
                    return;
                }

                if (TryStartGranaryTask())
                {
                    return;
                }

                if (TryStartConstructionTask())
                {
                    return;
                }

                if (TryStartHunterTask())
                {
                    return;
                }

                if (TryStartFisherTask())
                {
                    return;
                }

                PickNextIdleTarget();
                return;
            }

            Vector3 targetWorld = path[pathIndex];
            if (Vector3.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    hasTarget = false;
                    if (activity == ResidentActivity.MovingToGarden)
                    {
                        StartGardenWork();
                    }
                    else if (activity == ResidentActivity.MovingToForage)
                    {
                        StartGatheringForage();
                    }
                    else if (activity == ResidentActivity.MovingToLooseForagePickup)
                    {
                        StartPickingUpLooseForage();
                    }
                    else if (activity == ResidentActivity.CarryingForage)
                    {
                        StartDepositingForage();
                    }
                    else if (activity == ResidentActivity.MovingToTree)
                    {
                        StartChoppingTree();
                    }
                    else if (activity == ResidentActivity.MovingToLogs)
                    {
                        StartCollectingLogs();
                    }
                    else if (activity == ResidentActivity.CarryingLogs)
                    {
                        StartDepositingLogs();
                    }
                    else if (activity == ResidentActivity.MovingToStone)
                    {
                        StartMiningStone();
                    }
                    else if (activity == ResidentActivity.CarryingStone)
                    {
                        StartDepositingStone();
                    }
                    else if (activity == ResidentActivity.MovingToStoragePickup)
                    {
                        StartPickingUpStorageLogs();
                    }
                    else if (activity == ResidentActivity.CarryingLogsToStorage)
                    {
                        StartDepositingStorageLogs();
                    }
                    else if (activity == ResidentActivity.MovingToStorageStonePickup)
                    {
                        StartPickingUpStorageStone();
                    }
                    else if (activity == ResidentActivity.CarryingStoneToStorage)
                    {
                        StartDepositingStorageStone();
                    }
                    else if (activity == ResidentActivity.MovingToConstructionStorage)
                    {
                        StartPickingUpConstructionResource();
                    }
                    else if (activity == ResidentActivity.CarryingConstructionLogs
                        || activity == ResidentActivity.CarryingConstructionStone)
                    {
                        StartDepositingConstructionResource();
                    }
                    else if (activity == ResidentActivity.MovingToConstructionSite)
                    {
                        StartBuildingConstruction();
                    }
                    else if (activity == ResidentActivity.MovingToHuntingRange)
                    {
                        StartAimingBow();
                    }
                    else if (activity == ResidentActivity.MovingToHuntCarcass)
                    {
                        StartButcheringRabbit();
                    }
                    else if (activity == ResidentActivity.CarryingGame)
                    {
                        StartDepositingGame();
                    }
                    else if (activity == ResidentActivity.MovingToFishingSpot)
                    {
                        StartCastingFishingLine();
                    }
                    else if (activity == ResidentActivity.CarryingFish)
                    {
                        StartDepositingFish();
                    }
                    else if (activity == ResidentActivity.MovingToGranaryGamePickup)
                    {
                        StartPickingUpGranaryGame();
                    }
                    else if (activity == ResidentActivity.CarryingGameToGranary)
                    {
                        StartDepositingGranaryGame();
                    }
                    else if (activity == ResidentActivity.MovingToGranaryFishPickup)
                    {
                        StartPickingUpGranaryFish();
                    }
                    else if (activity == ResidentActivity.CarryingFishToGranary)
                    {
                        StartDepositingGranaryFish();
                    }
                    else if (activity == ResidentActivity.MovingToHouseholdFoodPickup)
                    {
                        StartPickingUpHouseholdFood();
                    }
                    else if (activity == ResidentActivity.CarryingHouseholdFoodHome)
                    {
                        StartDepositingHouseholdFood();
                    }
                    else if (IsReturningCarriedResourceActivity(activity))
                    {
                        CompleteCarriedResourceReturn();
                    }
                    else if (activity == ResidentActivity.MovingToPlantTree)
                    {
                        StartPlantingTree();
                    }
                    else if (IsFuneralMoveActivity(activity))
                    {
                        activity = ResidentActivity.WaitingAtFuneral;
                        funeralTimer = FuneralWaitingAutoReleaseSeconds;
                        waitTimer = 0f;
                        UseIdleSprite();
                    }
                    else
                    {
                        activity = GetRestingActivity();
                        waitTimer = Random.Range(0.35f, 1.1f);
                        UseIdleSprite();
                    }
                }

                return;
            }

            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, MoveSpeed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
                SyncReadabilityRenderers();
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                AnimateWalk();
            }
            else
            {
                AnimateIdle();
            }
        }
        private void LateUpdate()
        {
            UpdateWorldSorting();
        }

        private ResidentActivity GetRestingActivity()
        {
            return IsHouseholder && !HasExternalWorkplace && constructionSite == null
                ? ResidentActivity.TendingHousehold
                : ResidentActivity.Idle;
        }
    }
}
