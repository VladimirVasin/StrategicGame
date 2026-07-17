namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void ConfigureTaskArrivalExecution()
        {
            RegisterTaskArrival(ResidentActivity.MovingToGarden, StartGardenWork);
            RegisterTaskArrival(ResidentActivity.MovingToForage, StartGatheringForage);
            RegisterTaskArrival(ResidentActivity.MovingToLooseForagePickup, StartPickingUpLooseForage);
            RegisterTaskArrival(ResidentActivity.CarryingForage, StartDepositingForage);
            RegisterTaskArrival(ResidentActivity.MovingToTree, StartChoppingTree);
            RegisterTaskArrival(ResidentActivity.MovingToLogs, StartCollectingLogs);
            RegisterTaskArrival(ResidentActivity.CarryingLogs, StartDepositingLogs);
            RegisterTaskArrival(ResidentActivity.MovingToStone, StartMiningStone);
            RegisterTaskArrival(ResidentActivity.CarryingStone, StartDepositingStone);
            RegisterTaskArrival(ResidentActivity.MovingToMine, StartMiningUnderground);
            RegisterTaskArrival(ResidentActivity.MovingToCoalPit, StartMiningCoalInPit);
            RegisterTaskArrival(ResidentActivity.MovingToClayPit, StartDiggingClayInPit);
            RegisterTaskArrival(ResidentActivity.MovingToProductionInputPickup, StartPickingUpProductionInput);
            RegisterTaskArrival(ResidentActivity.CarryingProductionInput, StartDepositingProductionInput);
            RegisterTaskArrival(ResidentActivity.MovingToSawmill, StartSawingLogs);
            RegisterTaskArrival(ResidentActivity.MovingToKiln, StartFiringPottery);
            RegisterTaskArrival(ResidentActivity.MovingToForge, StartForgingTools);
            RegisterTaskArrival(ResidentActivity.MovingToStoragePickup, StartPickingUpStorageLogs);
            RegisterTaskArrival(ResidentActivity.CarryingLogsToStorage, StartDepositingStorageLogs);
            RegisterTaskArrival(ResidentActivity.MovingToStorageStonePickup, StartPickingUpStorageStone);
            RegisterTaskArrival(ResidentActivity.CarryingStoneToStorage, StartDepositingStorageStone);
            RegisterTaskArrival(ResidentActivity.MovingToStorageIronPickup, StartPickingUpStorageIron);
            RegisterTaskArrival(ResidentActivity.CarryingIronToStorage, StartDepositingStorageIron);
            RegisterTaskArrival(ResidentActivity.MovingToStorageCoalPickup, StartPickingUpStorageCoal);
            RegisterTaskArrival(ResidentActivity.CarryingCoalToStorage, StartDepositingStorageCoal);
            RegisterTaskArrival(ResidentActivity.MovingToStorageClayPickup, StartPickingUpStorageClay);
            RegisterTaskArrival(ResidentActivity.CarryingClayToStorage, StartDepositingStorageClay);
            RegisterTaskArrival(ResidentActivity.MovingToStoragePlanksPickup, StartPickingUpStoragePlanks);
            RegisterTaskArrival(ResidentActivity.CarryingPlanksToStorage, StartDepositingStoragePlanks);
            RegisterTaskArrival(ResidentActivity.MovingToStoragePotteryPickup, StartPickingUpStoragePottery);
            RegisterTaskArrival(ResidentActivity.CarryingPotteryToStorage, StartDepositingStoragePottery);
            RegisterTaskArrival(ResidentActivity.MovingToStorageToolsPickup, StartPickingUpStorageTools);
            RegisterTaskArrival(ResidentActivity.CarryingToolsToStorage, StartDepositingStorageTools);
            RegisterTaskArrival(ResidentActivity.MovingToHouseholdPotteryPickup, StartPickingUpHouseholdPottery);
            RegisterTaskArrival(ResidentActivity.CarryingPotteryToHouse, StartDepositingHouseholdPottery);
            RegisterTaskArrival(ResidentActivity.MovingToHouseholdLogsPickup, StartPickingUpHouseholdLogs);
            RegisterTaskArrival(ResidentActivity.CarryingLogsToHouse, StartDepositingHouseholdLogs);
            RegisterTaskArrival(ResidentActivity.MovingToConstructionStorage, StartPickingUpConstructionResource);
            RegisterTaskArrival(ResidentActivity.CarryingConstructionLogs, StartDepositingConstructionResource);
            RegisterTaskArrival(ResidentActivity.CarryingConstructionStone, StartDepositingConstructionResource);
            RegisterTaskArrival(ResidentActivity.CarryingConstructionPlanks, StartDepositingConstructionResource);
            RegisterTaskArrival(ResidentActivity.MovingToConstructionSite, StartBuildingConstruction);
            RegisterTaskArrival(ResidentActivity.MovingToHuntingRange, StartAimingBow);
            RegisterTaskArrival(ResidentActivity.MovingToHuntCarcass, StartButcheringRabbit);
            RegisterTaskArrival(ResidentActivity.CarryingGame, StartDepositingGame);
            RegisterTaskArrival(ResidentActivity.MovingToFishingSpot, StartCastingFishingLine);
            RegisterTaskArrival(ResidentActivity.CarryingFish, StartDepositingFish);
            RegisterTaskArrival(ResidentActivity.MovingToGranaryGamePickup, StartPickingUpGranaryGame);
            RegisterTaskArrival(ResidentActivity.CarryingGameToGranary, StartDepositingGranaryGame);
            RegisterTaskArrival(ResidentActivity.MovingToGranaryFishPickup, StartPickingUpGranaryFish);
            RegisterTaskArrival(ResidentActivity.CarryingFishToGranary, StartDepositingGranaryFish);
            RegisterTaskArrival(ResidentActivity.MovingToGranaryForagePickup, StartPickingUpGranaryForage);
            RegisterTaskArrival(ResidentActivity.CarryingForageToGranary, StartDepositingGranaryForage);
            RegisterTaskArrival(ResidentActivity.MovingToHouseholdFoodPickup, StartPickingUpHouseholdFood);
            RegisterTaskArrival(ResidentActivity.CarryingHouseholdFoodHome, StartDepositingHouseholdFood);
            RegisterTaskArrival(ResidentActivity.MovingToHouseCooking, StartHouseholdCooking);
            RegisterTaskArrival(ResidentActivity.ReturningLogsToStorage, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningStoneToStorage, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningIronToStorage, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningClayToStorage, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningGameToGranary, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningFishToGranary, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningForageToGranary, CompleteCarriedResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningCoalToStorage, CompleteCoalResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningPlanksToStorage, CompletePlanksResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningPotteryToStorage, CompletePotteryResourceReturn);
            RegisterTaskArrival(ResidentActivity.ReturningToolsToStorage, CompleteToolsResourceReturn);
            RegisterTaskArrival(ResidentActivity.MovingToPlantTree, StartPlantingTree);
            RegisterTaskArrival(ResidentActivity.MovingToNightLight, StartLightingNightLight);
            RegisterTaskArrival(ResidentActivity.MovingToScoutFrontier, StartSurveyingFrontier);
            RegisterTaskArrival(ResidentActivity.MovingToPointOfInterest, StartInvestigatingPointOfInterest);
            RegisterTaskArrival(ResidentActivity.ReturningToScoutLodge, CompleteScoutReturn);
        }
    }
}
