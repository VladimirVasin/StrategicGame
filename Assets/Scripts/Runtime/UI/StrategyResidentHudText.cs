using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyResidentHudText
    {
        public static string GetRoleTitle(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return L("resident.role.settler");
            }

            if (!resident.IsAdult)
            {
                return L("resident.role.child");
            }

            if (resident.IsHouseholder)
            {
                return L("resident.role.householder");
            }

            if (resident.IsSettlementBuilder || resident.BuilderWorkplace != null || resident.ConstructionSite != null)
            {
                return L("resident.role.builder");
            }

            if (resident.Workplace != null)
            {
                return L("resident.role.lumberjack");
            }

            if (resident.StoneWorkplace != null)
            {
                return L("resident.role.stonecutter");
            }

            if (resident.MineWorkplace != null)
            {
                return L("resident.role.miner");
            }

            if (resident.CoalPitWorkplace != null)
            {
                return L("resident.role.coal_miner");
            }

            if (resident.ClayPitWorkplace != null)
            {
                return L("resident.role.clay_digger");
            }

            if (resident.SawmillWorkplace != null)
            {
                return L("resident.role.sawyer");
            }

            if (resident.KilnWorkplace != null)
            {
                return L("resident.role.potter");
            }

            if (resident.ForgeWorkplace != null)
            {
                return L("resident.role.blacksmith");
            }

            if (resident.HunterWorkplace != null)
            {
                return L("resident.role.hunter");
            }

            if (resident.FisherWorkplace != null)
            {
                return L("resident.role.fisher");
            }

            if (resident.ForagerWorkplace != null)
            {
                return L("resident.role.forager");
            }

            if (resident.ScoutWorkplace != null)
            {
                return L("resident.role.scout");
            }

            if (resident.IsSettlementHauler || resident.StorageWorkplace != null || resident.GranaryWorkplace != null)
            {
                return L("resident.role.hauler");
            }

            return L("resident.role.settler");
        }

        public static string GetHomeTitle(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return L("resident.home.unknown");
            }

            if (resident.Home != null)
            {
                return L("resident.home.house");
            }

            if (resident.ConstructionWillBecomeHome)
            {
                return L("resident.home.future_house");
            }

            return resident.IsPendingRefugee
                ? L("resident.home.refugee_camp")
                : L("resident.home.camp");
        }

        public static string GetFoodTitle(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return L("resident.common.unknown");
            }

            string nutrition = StrategyLocalization.TranslateLiteral(
                resident.NutritionStatusText);
            return resident.IsHungry
                ? L("resident.food.hungry_days", nutrition, resident.DaysHungry)
                : nutrition;
        }

        public static string GetGenderTitle(StrategyResidentGender gender)
        {
            return gender == StrategyResidentGender.Male
                ? L("resident.gender.male")
                : L("resident.gender.female");
        }

        public static string GetLifeStageTitle(StrategyResidentAgent resident)
        {
            return resident != null && resident.LifeStage == StrategyResidentLifeStage.Child
                ? L("resident.life_stage.child")
                : L("resident.life_stage.adult");
        }

        public static string GetStatusText(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return L("resident.common.unknown");
            }

            string status = resident.Activity switch
            {
                StrategyResidentAgent.ResidentActivity.TendingHousehold => "tending household",
                StrategyResidentAgent.ResidentActivity.StayingInsideHome => resident.IsSleepingInsideHome ? "sleeping at home" : "inside home",
                StrategyResidentAgent.ResidentActivity.MovingHome => "going home",
                StrategyResidentAgent.ResidentActivity.ArrivingAsRefugee => "going to campfire",
                StrategyResidentAgent.ResidentActivity.LeavingSettlement => "leaving settlement",
                StrategyResidentAgent.ResidentActivity.MovingToCampfireSleep => "going to campfire",
                StrategyResidentAgent.ResidentActivity.LightingCampfire => "lighting campfire",
                StrategyResidentAgent.ResidentActivity.SleepingByCampfire => "sleeping by campfire",
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
                StrategyResidentAgent.ResidentActivity.MovingToClayPit => "going to clay pit",
                StrategyResidentAgent.ResidentActivity.DiggingClayInPit => "digging Clay",
                StrategyResidentAgent.ResidentActivity.MovingToProductionInputPickup => "going for production input",
                StrategyResidentAgent.ResidentActivity.PickingUpProductionInput => "picking up production input",
                StrategyResidentAgent.ResidentActivity.CarryingProductionInput => "hauling production input",
                StrategyResidentAgent.ResidentActivity.DepositingProductionInput => "depositing production input",
                StrategyResidentAgent.ResidentActivity.MovingToSawmill => "going to sawmill",
                StrategyResidentAgent.ResidentActivity.StandingByAtSawmill => "on duty at sawmill",
                StrategyResidentAgent.ResidentActivity.SawingLogs => "sawing Logs",
                StrategyResidentAgent.ResidentActivity.MovingToKiln => "going to kiln",
                StrategyResidentAgent.ResidentActivity.StandingByAtKiln => "on duty at kiln",
                StrategyResidentAgent.ResidentActivity.FiringPottery => "firing Pottery",
                StrategyResidentAgent.ResidentActivity.MovingToForge => "going to forge",
                StrategyResidentAgent.ResidentActivity.StandingByAtForge => "on duty at forge",
                StrategyResidentAgent.ResidentActivity.ForgingTools => "forging Tools",
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
                StrategyResidentAgent.ResidentActivity.MovingToStorageClayPickup => "going for Clay",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageClay => "picking up Clay",
                StrategyResidentAgent.ResidentActivity.CarryingClayToStorage => "hauling Clay to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageClay => "depositing Clay",
                StrategyResidentAgent.ResidentActivity.MovingToStoragePlanksPickup => "going for Planks",
                StrategyResidentAgent.ResidentActivity.PickingUpStoragePlanks => "picking up Planks",
                StrategyResidentAgent.ResidentActivity.CarryingPlanksToStorage => "hauling Planks to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStoragePlanks => "depositing Planks",
                StrategyResidentAgent.ResidentActivity.MovingToStoragePotteryPickup => "going for Pottery",
                StrategyResidentAgent.ResidentActivity.PickingUpStoragePottery => "picking up Pottery",
                StrategyResidentAgent.ResidentActivity.CarryingPotteryToStorage => "hauling Pottery to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStoragePottery => "depositing Pottery",
                StrategyResidentAgent.ResidentActivity.MovingToStorageToolsPickup => "going for Tools",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageTools => "picking up Tools",
                StrategyResidentAgent.ResidentActivity.CarryingToolsToStorage => "hauling Tools to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageTools => "depositing Tools",
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
                StrategyResidentAgent.ResidentActivity.MovingToGranaryForagePickup => "going for forage",
                StrategyResidentAgent.ResidentActivity.PickingUpGranaryForage => "picking up forage",
                StrategyResidentAgent.ResidentActivity.CarryingForageToGranary => "hauling forage to granary",
                StrategyResidentAgent.ResidentActivity.DepositingGranaryForage => "depositing forage in granary",
                StrategyResidentAgent.ResidentActivity.MovingToHouseholdFoodPickup => "going to granary",
                StrategyResidentAgent.ResidentActivity.PickingUpHouseholdFood => "picking up household food",
                StrategyResidentAgent.ResidentActivity.CarryingHouseholdFoodHome => "bringing food home",
                StrategyResidentAgent.ResidentActivity.DepositingHouseholdFood => "storing household food",
                StrategyResidentAgent.ResidentActivity.MovingToHouseholdPotteryPickup => "going for household Pottery",
                StrategyResidentAgent.ResidentActivity.PickingUpHouseholdPottery => "picking up household Pottery",
                StrategyResidentAgent.ResidentActivity.CarryingPotteryToHouse => "bringing Pottery home",
                StrategyResidentAgent.ResidentActivity.DepositingHouseholdPottery => "storing household Pottery",
                StrategyResidentAgent.ResidentActivity.MovingToHouseholdLogsPickup => "going for household Logs",
                StrategyResidentAgent.ResidentActivity.PickingUpHouseholdLogs => "picking up household Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogsToHouse => "bringing Logs home",
                StrategyResidentAgent.ResidentActivity.DepositingHouseholdLogs => "storing household Logs",
                StrategyResidentAgent.ResidentActivity.MovingToHouseCooking => "going home to cook",
                StrategyResidentAgent.ResidentActivity.CookingHouseMeal => "cooking dinner",
                StrategyResidentAgent.ResidentActivity.MovingToChildPlay => "going to play",
                StrategyResidentAgent.ResidentActivity.PlayingAlone => "playing near home",
                StrategyResidentAgent.ResidentActivity.PlayingWithStick => "drawing in the dirt",
                StrategyResidentAgent.ResidentActivity.SittingNearHome => "resting near home",
                StrategyResidentAgent.ResidentActivity.WatchingActivity => "watching the village",
                StrategyResidentAgent.ResidentActivity.MovingToChildPartner => "joining other children",
                StrategyResidentAgent.ResidentActivity.PlayingWithChild => "talking with another child",
                StrategyResidentAgent.ResidentActivity.PlayingTag => "playing tag",
                StrategyResidentAgent.ResidentActivity.ReturningLogsToStorage => "returning Logs to storage",
                StrategyResidentAgent.ResidentActivity.ReturningStoneToStorage => "returning Stone to storage",
                StrategyResidentAgent.ResidentActivity.ReturningIronToStorage => "returning Iron to storage",
                StrategyResidentAgent.ResidentActivity.ReturningCoalToStorage => "returning Coal to storage",
                StrategyResidentAgent.ResidentActivity.ReturningClayToStorage => "returning Clay to storage",
                StrategyResidentAgent.ResidentActivity.ReturningPlanksToStorage => "returning Planks to storage",
                StrategyResidentAgent.ResidentActivity.ReturningPotteryToStorage => "returning Pottery to storage",
                StrategyResidentAgent.ResidentActivity.ReturningToolsToStorage => "returning Tools to storage",
                StrategyResidentAgent.ResidentActivity.ReturningGameToGranary => "returning Game to granary",
                StrategyResidentAgent.ResidentActivity.ReturningFishToGranary => "returning Fish to granary",
                StrategyResidentAgent.ResidentActivity.ReturningForageToGranary => "returning forage to granary",
                StrategyResidentAgent.ResidentActivity.MovingToNightLight => "going to light a lamp",
                StrategyResidentAgent.ResidentActivity.LightingNightLight => "lighting a lamp",
                StrategyResidentAgent.ResidentActivity.MovingToFuneral => "going to funeral",
                StrategyResidentAgent.ResidentActivity.MourningCorpse => "mourning",
                StrategyResidentAgent.ResidentActivity.CarryingCorpseToCemetery => "carrying the dead",
                StrategyResidentAgent.ResidentActivity.MovingToBurial => "going to burial",
                StrategyResidentAgent.ResidentActivity.BuryingGrave => "burying the dead",
                StrategyResidentAgent.ResidentActivity.WaitingAtFuneral => "attending funeral",
                StrategyResidentAgent.ResidentActivity.MovingToScoutFrontier => "exploring unknown land",
                StrategyResidentAgent.ResidentActivity.SurveyingFrontier => "surveying terrain",
                StrategyResidentAgent.ResidentActivity.MovingToPointOfInterest => "approaching a point of interest",
                StrategyResidentAgent.ResidentActivity.InvestigatingPointOfInterest => "investigating a point of interest",
                StrategyResidentAgent.ResidentActivity.ReturningToScoutLodge => "returning to Scout Lodge",
                StrategyResidentAgent.ResidentActivity.MovingToCombatRange => "going hunting",
                StrategyResidentAgent.ResidentActivity.AimingCombatBow => "aiming bow",
                StrategyResidentAgent.ResidentActivity.WaitingForCombatHit => "watching the arrow",
                _ => "idle"
            };

            if (resident.IsPendingRefugee && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "refugee");
            }

            if (resident.IsOffDutyForNight)
            {
                return AppendNutritionStatus(resident, "off duty for the night");
            }

            if ((resident.IsSettlementBuilder || resident.BuilderWorkplace != null) && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
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

            if (resident.ForagerWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for forage");
            }

            if (resident.ScoutWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for unexplored frontier");
            }

            if (resident.MineWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Iron");
            }

            if (resident.CoalPitWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Coal");
            }

            if (resident.ClayPitWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Clay");
            }

            if (resident.SawmillWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Logs");
            }

            if (resident.KilnWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Clay and Coal");
            }

            if (resident.ForgeWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return AppendNutritionStatus(resident, "waiting for Iron, Coal and Logs");
            }

            if ((resident.IsSettlementHauler || resident.StorageWorkplace != null) && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
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
            if (resident == null)
            {
                return StrategyLocalization.TranslateLiteral(status);
            }

            status = StrategyLocalization.TranslateLiteral(status);
            status = GetScoutExpeditionStatus(resident, status);

            if (resident.IsHungry)
            {
                status = L(
                    "resident.status.hungry_suffix",
                    status,
                    StrategyLocalization.TranslateLiteral(resident.NutritionStatusText),
                    resident.DaysHungry);
            }

            if (resident.IsColdAffected)
            {
                status = L(
                    "resident.status.cold_suffix",
                    status,
                    StrategyLocalization.TranslateLiteral(resident.ColdStatusText));
            }

            return status;
        }

        private static string GetScoutExpeditionStatus(
            StrategyResidentAgent resident,
            string activityStatus)
        {
            StrategyScoutLodge lodge = resident != null ? resident.ScoutWorkplace : null;
            if (lodge == null)
            {
                return activityStatus;
            }

            if (lodge.ExpeditionState == StrategyScoutExpeditionState.Returning)
            {
                return L("resident.status.returning_to_scout_lodge");
            }

            if (lodge.ExpeditionState == StrategyScoutExpeditionState.Ready)
            {
                return resident.Activity == StrategyResidentAgent.ResidentActivity.Idle
                    ? L("resident.status.ready_for_expedition")
                    : activityStatus;
            }

            float totalHours = Mathf.Max(0f, lodge.RemainingExpeditionSeconds)
                / StrategyDayNightCycleController.DayLengthSeconds
                * 24f;
            int days = Mathf.FloorToInt(totalHours / 24f);
            int hours = Mathf.CeilToInt(totalHours - days * 24f);
            if (hours >= 24)
            {
                days++;
                hours = 0;
            }

            string remaining = days > 0
                ? L("resident.time.days_hours", days, hours)
                : L("resident.time.hours", Mathf.Max(1, hours));
            return L("resident.status.expedition_remaining", activityStatus, remaining);
        }

        private static string L(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Residents,
                key,
                arguments);
        }
    }
}
