namespace ProjectUnknown.Strategy
{
    public static class StrategyResidentHudText
    {
        public static string GetRoleTitle(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "Settler";
            }

            if (!resident.IsAdult)
            {
                return "Child";
            }

            if (resident.IsHouseholder)
            {
                return "Householder";
            }

            if (resident.BuilderWorkplace != null || resident.ConstructionSite != null)
            {
                return "Builder";
            }

            if (resident.Workplace != null)
            {
                return "Lumberjack";
            }

            if (resident.StoneWorkplace != null)
            {
                return "Stonecutter";
            }

            if (resident.MineWorkplace != null)
            {
                return "Miner";
            }

            if (resident.CoalPitWorkplace != null)
            {
                return "Coal Miner";
            }

            if (resident.SawmillWorkplace != null)
            {
                return "Sawyer";
            }

            if (resident.HunterWorkplace != null)
            {
                return "Hunter";
            }

            if (resident.FisherWorkplace != null)
            {
                return "Fisher";
            }

            if (resident.StorageWorkplace != null || resident.GranaryWorkplace != null)
            {
                return "Hauler";
            }

            return "Settler";
        }

        public static string GetHomeTitle(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "Unknown";
            }

            if (resident.Home != null)
            {
                return "House";
            }

            if (resident.ConstructionWillBecomeHome)
            {
                return "Future House";
            }

            return resident.IsPendingRefugee ? "Refugee Camp" : "Camp";
        }

        public static string GetFoodTitle(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "unknown";
            }

            return resident.IsHungry
                ? resident.NutritionStatusText + " " + resident.DaysHungry + "d"
                : resident.NutritionStatusText;
        }

        public static string GetGenderTitle(StrategyResidentGender gender)
        {
            return gender == StrategyResidentGender.Male ? "male" : "female";
        }

        public static string GetLifeStageTitle(StrategyResidentAgent resident)
        {
            return resident != null && resident.LifeStage == StrategyResidentLifeStage.Child
                ? "child"
                : "adult";
        }

        public static string GetStatusText(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "unknown";
            }

            string status = resident.Activity switch
            {
                StrategyResidentAgent.ResidentActivity.TendingHousehold => "tending household",
                StrategyResidentAgent.ResidentActivity.StayingInsideHome => "inside home",
                StrategyResidentAgent.ResidentActivity.MovingHome => "going home",
                StrategyResidentAgent.ResidentActivity.ArrivingAsRefugee => "going to campfire",
                StrategyResidentAgent.ResidentActivity.LeavingSettlement => "leaving settlement",
                StrategyResidentAgent.ResidentActivity.WorkingGarden => "working garden beds",
                StrategyResidentAgent.ResidentActivity.MovingToGarden => "going to garden beds",
                StrategyResidentAgent.ResidentActivity.MovingToForage => "going foraging",
                StrategyResidentAgent.ResidentActivity.GatheringForage => "gathering food",
                StrategyResidentAgent.ResidentActivity.MovingToLooseForagePickup => "recovering dropped food",
                StrategyResidentAgent.ResidentActivity.PickingUpLooseForage => "picking up dropped food",
                StrategyResidentAgent.ResidentActivity.CarryingForage => "bringing food home",
                StrategyResidentAgent.ResidentActivity.DepositingForage => "storing household food",
                StrategyResidentAgent.ResidentActivity.MovingToTree => "going to a tree",
                StrategyResidentAgent.ResidentActivity.ChoppingTree => "chopping tree",
                StrategyResidentAgent.ResidentActivity.BuckingTree => "bucking trunk",
                StrategyResidentAgent.ResidentActivity.MovingToLogs => "going to Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogs => "carrying Logs",
                StrategyResidentAgent.ResidentActivity.DepositingLogs => "depositing Logs",
                StrategyResidentAgent.ResidentActivity.MovingToStoragePickup => "going for Logs",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageLogs => "picking up Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogsToStorage => "hauling Logs to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageLogs => "depositing Logs",
                StrategyResidentAgent.ResidentActivity.MovingToPlantTree => "looking for a planting spot",
                StrategyResidentAgent.ResidentActivity.PlantingTree => "planting a tree",
                StrategyResidentAgent.ResidentActivity.MovingToStone => "going to deposit",
                StrategyResidentAgent.ResidentActivity.MiningStone => "mining Stone",
                StrategyResidentAgent.ResidentActivity.CarryingStone => "carrying Stone",
                StrategyResidentAgent.ResidentActivity.DepositingStone => "depositing Stone",
                StrategyResidentAgent.ResidentActivity.MovingToMine => "going to mine",
                StrategyResidentAgent.ResidentActivity.MiningUnderground => "working underground",
                StrategyResidentAgent.ResidentActivity.MovingToCoalPit => "going to coal pit",
                StrategyResidentAgent.ResidentActivity.MiningCoalInPit => "digging Coal",
                StrategyResidentAgent.ResidentActivity.MovingToProductionInputPickup => "going for production input",
                StrategyResidentAgent.ResidentActivity.PickingUpProductionInput => "picking up production input",
                StrategyResidentAgent.ResidentActivity.CarryingProductionInput => "hauling production input",
                StrategyResidentAgent.ResidentActivity.DepositingProductionInput => "depositing production input",
                StrategyResidentAgent.ResidentActivity.MovingToSawmill => "going to sawmill",
                StrategyResidentAgent.ResidentActivity.SawingLogs => "sawing Logs",
                StrategyResidentAgent.ResidentActivity.MovingToStorageStonePickup => "going for Stone",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageStone => "picking up Stone",
                StrategyResidentAgent.ResidentActivity.CarryingStoneToStorage => "hauling Stone to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageStone => "depositing Stone",
                StrategyResidentAgent.ResidentActivity.MovingToStorageIronPickup => "going for Iron",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageIron => "picking up Iron",
                StrategyResidentAgent.ResidentActivity.CarryingIronToStorage => "hauling Iron to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageIron => "depositing Iron",
                StrategyResidentAgent.ResidentActivity.MovingToStorageCoalPickup => "going for Coal",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageCoal => "picking up Coal",
                StrategyResidentAgent.ResidentActivity.CarryingCoalToStorage => "hauling Coal to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageCoal => "depositing Coal",
                StrategyResidentAgent.ResidentActivity.MovingToStoragePlanksPickup => "going for Planks",
                StrategyResidentAgent.ResidentActivity.PickingUpStoragePlanks => "picking up Planks",
                StrategyResidentAgent.ResidentActivity.CarryingPlanksToStorage => "hauling Planks to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStoragePlanks => "depositing Planks",
                StrategyResidentAgent.ResidentActivity.MovingToConstructionStorage => "going for materials",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionLogs => "picking up construction Logs",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionStone => "picking up construction Stone",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionPlanks => "picking up construction Planks",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionLogs => "carrying Logs to construction",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionStone => "carrying Stone to construction",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionPlanks => "carrying Planks to construction",
                StrategyResidentAgent.ResidentActivity.DepositingConstructionResource => "depositing materials",
                StrategyResidentAgent.ResidentActivity.MovingToConstructionSite => "going to build",
                StrategyResidentAgent.ResidentActivity.BuildingConstruction => "building",
                StrategyResidentAgent.ResidentActivity.MovingToHuntingRange => "going hunting",
                StrategyResidentAgent.ResidentActivity.AimingBow => "aiming bow",
                StrategyResidentAgent.ResidentActivity.WaitingForHuntHit => "watching the arrow",
                StrategyResidentAgent.ResidentActivity.MovingToHuntCarcass => "going to game",
                StrategyResidentAgent.ResidentActivity.ButcheringRabbit => "butchering game",
                StrategyResidentAgent.ResidentActivity.CarryingGame => "carrying Game",
                StrategyResidentAgent.ResidentActivity.DepositingGame => "depositing Game",
                StrategyResidentAgent.ResidentActivity.MovingToFishingSpot => "going to shore",
                StrategyResidentAgent.ResidentActivity.CastingFishingLine => "casting line",
                StrategyResidentAgent.ResidentActivity.WaitingForFishBite => "waiting for a bite",
                StrategyResidentAgent.ResidentActivity.ReelingFish => "reeling fish",
                StrategyResidentAgent.ResidentActivity.CarryingFish => "carrying Fish",
                StrategyResidentAgent.ResidentActivity.DepositingFish => "depositing Fish",
                StrategyResidentAgent.ResidentActivity.MovingToGranaryGamePickup => "going for Game",
                StrategyResidentAgent.ResidentActivity.PickingUpGranaryGame => "picking up Game",
                StrategyResidentAgent.ResidentActivity.CarryingGameToGranary => "hauling Game to granary",
                StrategyResidentAgent.ResidentActivity.DepositingGranaryGame => "depositing Game in granary",
                StrategyResidentAgent.ResidentActivity.MovingToGranaryFishPickup => "going for Fish",
                StrategyResidentAgent.ResidentActivity.PickingUpGranaryFish => "picking up Fish",
                StrategyResidentAgent.ResidentActivity.CarryingFishToGranary => "hauling Fish to granary",
                StrategyResidentAgent.ResidentActivity.DepositingGranaryFish => "depositing Fish in granary",
                StrategyResidentAgent.ResidentActivity.MovingToHouseholdFoodPickup => "going to granary",
                StrategyResidentAgent.ResidentActivity.PickingUpHouseholdFood => "picking up household food",
                StrategyResidentAgent.ResidentActivity.CarryingHouseholdFoodHome => "bringing food home",
                StrategyResidentAgent.ResidentActivity.DepositingHouseholdFood => "storing household food",
                StrategyResidentAgent.ResidentActivity.ReturningLogsToStorage => "returning Logs to storage",
                StrategyResidentAgent.ResidentActivity.ReturningStoneToStorage => "returning Stone to storage",
                StrategyResidentAgent.ResidentActivity.ReturningIronToStorage => "returning Iron to storage",
                StrategyResidentAgent.ResidentActivity.ReturningCoalToStorage => "returning Coal to storage",
                StrategyResidentAgent.ResidentActivity.ReturningPlanksToStorage => "returning Planks to storage",
                StrategyResidentAgent.ResidentActivity.ReturningGameToGranary => "returning Game to granary",
                StrategyResidentAgent.ResidentActivity.ReturningFishToGranary => "returning Fish to granary",
                StrategyResidentAgent.ResidentActivity.MovingToFuneral => "going to funeral",
                StrategyResidentAgent.ResidentActivity.MourningCorpse => "mourning",
                StrategyResidentAgent.ResidentActivity.CarryingCorpseToCemetery => "carrying the dead",
                StrategyResidentAgent.ResidentActivity.MovingToBurial => "going to burial",
                StrategyResidentAgent.ResidentActivity.BuryingGrave => "burying the dead",
                StrategyResidentAgent.ResidentActivity.WaitingAtFuneral => "attending funeral",
                _ => "idle"
            };

            if (resident.IsPendingRefugee && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "refugee");
            }

            if (resident.BuilderWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for construction");
            }

            if (resident.HunterWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for prey");
            }

            if (resident.FisherWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for a bite");
            }

            if (resident.MineWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Iron");
            }

            if (resident.CoalPitWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Coal");
            }

            if (resident.SawmillWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Logs");
            }

            if (resident.StorageWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for hauling");
            }

            if (resident.GranaryWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for food hauling");
            }

            if (resident.IsHouseholder && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "tending household");
            }

            if (resident.Home == null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "at the campfire");
            }

            if (!resident.IsAdult && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "playing near home");
            }

            return AppendNutritionStatus(resident, status);
        }

        private static string AppendNutritionStatus(StrategyResidentAgent resident, string status)
        {
            if (resident == null || !resident.IsHungry)
            {
                return status;
            }

            return status + " | " + resident.NutritionStatusText + " " + resident.DaysHungry + "d";
        }
    }
}
