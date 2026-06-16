using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void HandleReachedPathTarget()
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
            else if (activity == ResidentActivity.MovingToMine)
            {
                StartMiningUnderground();
            }
            else if (activity == ResidentActivity.MovingToCoalPit)
            {
                StartMiningCoalInPit();
            }
            else if (activity == ResidentActivity.MovingToProductionInputPickup)
            {
                StartPickingUpProductionInput();
            }
            else if (activity == ResidentActivity.CarryingProductionInput)
            {
                StartDepositingProductionInput();
            }
            else if (activity == ResidentActivity.MovingToSawmill)
            {
                StartSawingLogs();
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
            else if (activity == ResidentActivity.MovingToStorageIronPickup)
            {
                StartPickingUpStorageIron();
            }
            else if (activity == ResidentActivity.CarryingIronToStorage)
            {
                StartDepositingStorageIron();
            }
            else if (activity == ResidentActivity.MovingToStorageCoalPickup)
            {
                StartPickingUpStorageCoal();
            }
            else if (activity == ResidentActivity.CarryingCoalToStorage)
            {
                StartDepositingStorageCoal();
            }
            else if (activity == ResidentActivity.MovingToStoragePlanksPickup)
            {
                StartPickingUpStoragePlanks();
            }
            else if (activity == ResidentActivity.CarryingPlanksToStorage)
            {
                StartDepositingStoragePlanks();
            }
            else if (activity == ResidentActivity.MovingToConstructionStorage)
            {
                StartPickingUpConstructionResource();
            }
            else if (activity == ResidentActivity.CarryingConstructionLogs
                || activity == ResidentActivity.CarryingConstructionStone
                || activity == ResidentActivity.CarryingConstructionPlanks)
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
            else if (activity == ResidentActivity.ReturningCoalToStorage)
            {
                CompleteCoalResourceReturn();
            }
            else if (activity == ResidentActivity.ReturningPlanksToStorage)
            {
                CompletePlanksResourceReturn();
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
    }
}
