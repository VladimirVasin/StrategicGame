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

            if (hiddenUnderground && activity != ResidentActivity.MiningUnderground)
            {
                ExitUndergroundAtMineEntrance();
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

            if (mineWorkCooldown > 0f)
            {
                mineWorkCooldown -= Time.deltaTime;
            }

            if (coalWorkCooldown > 0f)
            {
                coalWorkCooldown -= Time.deltaTime;
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

            if (activity == ResidentActivity.MiningUnderground)
            {
                UpdateMiningUnderground();
                return;
            }

            if (activity == ResidentActivity.MiningCoalInPit)
            {
                UpdateMiningCoalInPit();
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

            if (activity == ResidentActivity.PickingUpStorageIron)
            {
                UpdatePickingUpStorageIron();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageIron)
            {
                UpdateDepositingStorageIron();
                return;
            }

            if (activity == ResidentActivity.PickingUpStorageCoal)
            {
                UpdatePickingUpStorageCoal();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageCoal)
            {
                UpdateDepositingStorageCoal();
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

                if (activity == ResidentActivity.ReturningCoalToStorage)
                {
                    if (!TryStartCoalReturn("resource_return_retry", true))
                    {
                        ScheduleCoalResourceReturnRetry();
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

                if (TryStartMineTask())
                {
                    return;
                }

                if (TryStartCoalPitTask())
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
                    HandleReachedPathTarget();
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
