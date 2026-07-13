namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void ConfigureTaskExecution()
        {
            taskExecution.Register(
                ResidentActivity.LightingNightLight,
                StrategyResidentTaskExecutionPhase.BeforeHomeSchedule,
                UpdateLightingNightLight);

            RegisterNormalTask(ResidentActivity.WorkingGarden, UpdateGardenWork);
            RegisterNormalTask(ResidentActivity.LightingCampfire, UpdateLightingCampfire);
            RegisterNormalTask(ResidentActivity.GatheringForage, UpdateGatheringForage);
            RegisterNormalTask(ResidentActivity.PickingUpLooseForage, UpdatePickingUpLooseForage);
            RegisterNormalTask(ResidentActivity.DepositingForage, UpdateDepositingForage);
            RegisterNormalTask(ResidentActivity.ChoppingTree, UpdateChoppingTree);
            RegisterNormalTask(ResidentActivity.BuckingTree, UpdateBuckingTree);
            RegisterNormalTask(ResidentActivity.DepositingLogs, UpdateDepositingLogs);
            RegisterNormalTask(ResidentActivity.MiningStone, UpdateMiningStone);
            RegisterNormalTask(ResidentActivity.DepositingStone, UpdateDepositingStone);
            RegisterNormalTask(ResidentActivity.MiningUnderground, UpdateMiningUnderground);
            RegisterNormalTask(ResidentActivity.MiningCoalInPit, UpdateMiningCoalInPit);
            RegisterNormalTask(ResidentActivity.DiggingClayInPit, UpdateDiggingClayInPit);
            RegisterNormalTask(ResidentActivity.PickingUpProductionInput, UpdatePickingUpProductionInput);
            RegisterNormalTask(ResidentActivity.DepositingProductionInput, UpdateDepositingProductionInput);
            RegisterNormalTask(ResidentActivity.SawingLogs, UpdateSawingLogs);
            RegisterNormalTask(ResidentActivity.FiringPottery, UpdateFiringPottery);
            RegisterNormalTask(ResidentActivity.ForgingTools, UpdateForgingTools);
            RegisterNormalTask(ResidentActivity.PickingUpStorageLogs, UpdatePickingUpStorageLogs);
            RegisterNormalTask(ResidentActivity.DepositingStorageLogs, UpdateDepositingStorageLogs);
            RegisterNormalTask(ResidentActivity.PickingUpStorageStone, UpdatePickingUpStorageStone);
            RegisterNormalTask(ResidentActivity.DepositingStorageStone, UpdateDepositingStorageStone);
            RegisterNormalTask(ResidentActivity.PickingUpStorageIron, UpdatePickingUpStorageIron);
            RegisterNormalTask(ResidentActivity.DepositingStorageIron, UpdateDepositingStorageIron);
            RegisterNormalTask(ResidentActivity.PickingUpStorageCoal, UpdatePickingUpStorageCoal);
            RegisterNormalTask(ResidentActivity.DepositingStorageCoal, UpdateDepositingStorageCoal);
            RegisterNormalTask(ResidentActivity.PickingUpStorageClay, UpdatePickingUpStorageClay);
            RegisterNormalTask(ResidentActivity.DepositingStorageClay, UpdateDepositingStorageClay);
            RegisterNormalTask(ResidentActivity.PickingUpStoragePlanks, UpdatePickingUpStoragePlanks);
            RegisterNormalTask(ResidentActivity.DepositingStoragePlanks, UpdateDepositingStoragePlanks);
            RegisterNormalTask(ResidentActivity.PickingUpStoragePottery, UpdatePickingUpStoragePottery);
            RegisterNormalTask(ResidentActivity.DepositingStoragePottery, UpdateDepositingStoragePottery);
            RegisterNormalTask(ResidentActivity.PickingUpStorageTools, UpdatePickingUpStorageTools);
            RegisterNormalTask(ResidentActivity.DepositingStorageTools, UpdateDepositingStorageTools);
            RegisterNormalTask(ResidentActivity.PickingUpHouseholdPottery, UpdatePickingUpHouseholdPottery);
            RegisterNormalTask(ResidentActivity.DepositingHouseholdPottery, UpdateDepositingHouseholdPottery);
            RegisterNormalTask(ResidentActivity.PickingUpHouseholdLogs, UpdatePickingUpHouseholdLogs);
            RegisterNormalTask(ResidentActivity.DepositingHouseholdLogs, UpdateDepositingHouseholdLogs);
            RegisterNormalTask(ResidentActivity.PickingUpGranaryGame, UpdatePickingUpGranaryGame);
            RegisterNormalTask(ResidentActivity.DepositingGranaryGame, UpdateDepositingGranaryGame);
            RegisterNormalTask(ResidentActivity.PickingUpGranaryFish, UpdatePickingUpGranaryFish);
            RegisterNormalTask(ResidentActivity.DepositingGranaryFish, UpdateDepositingGranaryFish);
            RegisterNormalTask(ResidentActivity.PickingUpGranaryForage, UpdatePickingUpGranaryForage);
            RegisterNormalTask(ResidentActivity.DepositingGranaryForage, UpdateDepositingGranaryForage);
            RegisterNormalTask(ResidentActivity.PickingUpHouseholdFood, UpdatePickingUpHouseholdFood);
            RegisterNormalTask(ResidentActivity.DepositingHouseholdFood, UpdateDepositingHouseholdFood);
            RegisterNormalTask(ResidentActivity.CookingHouseMeal, UpdateHouseholdCooking);
            RegisterNormalTask(ResidentActivity.PickingUpConstructionLogs, UpdatePickingUpConstructionResource);
            RegisterNormalTask(ResidentActivity.PickingUpConstructionStone, UpdatePickingUpConstructionResource);
            RegisterNormalTask(ResidentActivity.PickingUpConstructionPlanks, UpdatePickingUpConstructionResource);
            RegisterNormalTask(ResidentActivity.DepositingConstructionResource, UpdateDepositingConstructionResource);
            RegisterNormalTask(ResidentActivity.BuildingConstruction, UpdateBuildingConstruction);
            RegisterNormalTask(ResidentActivity.AimingBow, UpdateAimingBow);
            RegisterNormalTask(ResidentActivity.WaitingForHuntHit, UpdateWaitingForHuntHit);
            RegisterNormalTask(ResidentActivity.ButcheringRabbit, UpdateButcheringRabbit);
            RegisterNormalTask(ResidentActivity.DepositingGame, UpdateDepositingGame);
            RegisterNormalTask(ResidentActivity.CastingFishingLine, UpdateCastingFishingLine);
            RegisterNormalTask(ResidentActivity.WaitingForFishBite, UpdateWaitingForFishBite);
            RegisterNormalTask(ResidentActivity.ReelingFish, UpdateReelingFish);
            RegisterNormalTask(ResidentActivity.DepositingFish, UpdateDepositingFish);
            RegisterNormalTask(ResidentActivity.PlantingTree, UpdatePlantingTree);
            RegisterNormalTask(ResidentActivity.PlayingAlone, UpdateChildPlayActivity);
            RegisterNormalTask(ResidentActivity.PlayingWithStick, UpdateChildPlayActivity);
            RegisterNormalTask(ResidentActivity.SittingNearHome, UpdateChildPlayActivity);
            RegisterNormalTask(ResidentActivity.WatchingActivity, UpdateChildPlayActivity);
            RegisterNormalTask(ResidentActivity.PlayingWithChild, UpdateChildPlayActivity);
            RegisterNormalTask(ResidentActivity.PlayingTag, UpdateChildPlayActivity);

            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Household, TryStartHouseholdCookingTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Logistics, TryStartHouseholdLogsDelivery);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Logistics, TryStartHouseholdPotteryDelivery);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Logistics, TryStartHouseholdFoodPickupTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Forestry, TryStartLumberTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, TryStartStoneTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, TryStartMineTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, TryStartCoalPitTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, TryStartClayPitTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Production, TryStartSawmillTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Production, TryStartKilnTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Production, TryStartForgeTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Logistics, TryStartStorageTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Logistics, TryStartGranaryTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, TryStartForagerTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Construction, TryStartConstructionTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Hunting, TryStartHunterTask);
            taskExecution.RegisterPlannedTask(StrategyResidentTaskKind.Fishing, TryStartFisherTask);
        }

        private void RegisterNormalTask(ResidentActivity activity, System.Action handler)
        {
            taskExecution.Register(activity, StrategyResidentTaskExecutionPhase.Normal, handler);
        }
    }
}
