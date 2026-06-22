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

            if (sleepingInsideHome)
            {
                UpdateNightSleep();
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

            if (clayWorkCooldown > 0f)
            {
                clayWorkCooldown -= Time.deltaTime;
            }

            if (sawmillWorkCooldown > 0f)
            {
                sawmillWorkCooldown -= Time.deltaTime;
            }

            if (kilnWorkCooldown > 0f)
            {
                kilnWorkCooldown -= Time.deltaTime;
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

            if (TryPauseActiveWorkForNight())
            {
                return;
            }

            if (UpdateNightHomeState())
            {
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

            if (activity == ResidentActivity.DiggingClayInPit)
            {
                UpdateDiggingClayInPit();
                return;
            }

            if (activity == ResidentActivity.PickingUpProductionInput)
            {
                UpdatePickingUpProductionInput();
                return;
            }

            if (activity == ResidentActivity.DepositingProductionInput)
            {
                UpdateDepositingProductionInput();
                return;
            }

            if (activity == ResidentActivity.SawingLogs)
            {
                UpdateSawingLogs();
                return;
            }

            if (activity == ResidentActivity.FiringPottery)
            {
                UpdateFiringPottery();
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

            if (activity == ResidentActivity.PickingUpStorageClay)
            {
                UpdatePickingUpStorageClay();
                return;
            }

            if (activity == ResidentActivity.DepositingStorageClay)
            {
                UpdateDepositingStorageClay();
                return;
            }

            if (activity == ResidentActivity.PickingUpStoragePlanks)
            {
                UpdatePickingUpStoragePlanks();
                return;
            }

            if (activity == ResidentActivity.DepositingStoragePlanks)
            {
                UpdateDepositingStoragePlanks();
                return;
            }

            if (activity == ResidentActivity.PickingUpStoragePottery)
            {
                UpdatePickingUpStoragePottery();
                return;
            }

            if (activity == ResidentActivity.DepositingStoragePottery)
            {
                UpdateDepositingStoragePottery();
                return;
            }

            if (activity == ResidentActivity.PickingUpHouseholdPottery)
            {
                UpdatePickingUpHouseholdPottery();
                return;
            }

            if (activity == ResidentActivity.DepositingHouseholdPottery)
            {
                UpdateDepositingHouseholdPottery();
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

            if (activity == ResidentActivity.CookingHouseMeal)
            {
                UpdateHouseholdCooking();
                return;
            }

            if (activity == ResidentActivity.PickingUpConstructionLogs
                || activity == ResidentActivity.PickingUpConstructionStone
                || activity == ResidentActivity.PickingUpConstructionPlanks)
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

                if (TryRetryStorageProductionResourceReturn())
                {
                    return;
                }

                if (TryStartScheduledWorkTask())
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

            MoveAlongCurrentPathTarget(targetWorld);
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
