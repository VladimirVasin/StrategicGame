using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        public bool IsOffDutyForNight
        {
            get
            {
                return !StrategyDayNightCycleController.IsSettlementWorkTime
                    && (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
                    && (HasExternalWorkplace || constructionSite != null || IsHouseholder);
            }
        }

        private bool TryStartScheduledWorkTask()
        {
            if (!StrategyDayNightCycleController.IsSettlementWorkTime)
            {
                waitTimer = Random.Range(0.55f, 1.25f);
                return false;
            }

            if (TryStartHouseholdCookingTask())
            {
                return true;
            }

            if (TryStartHouseholdFoodPickupTask())
            {
                return true;
            }

            if (TryStartGardenTask())
            {
                return true;
            }

            if (TryStartLumberTask())
            {
                return true;
            }

            if (TryStartStoneTask())
            {
                return true;
            }

            if (TryStartMineTask())
            {
                return true;
            }

            if (TryStartCoalPitTask())
            {
                return true;
            }

            if (TryStartClayPitTask())
            {
                return true;
            }

            if (TryStartSawmillTask())
            {
                return true;
            }

            if (TryStartKilnTask())
            {
                return true;
            }

            if (TryStartStorageTask())
            {
                return true;
            }

            if (TryStartGranaryTask())
            {
                return true;
            }

            if (TryStartConstructionTask())
            {
                return true;
            }

            if (TryStartHunterTask())
            {
                return true;
            }

            return TryStartFisherTask();
        }

        private bool TryPauseActiveWorkForNight()
        {
            if (StrategyDayNightCycleController.IsSettlementWorkTime)
            {
                return false;
            }

            if (!IsInterruptibleNightWorkActivity(activity)
                && !IsNightBlockedReachedActivity(activity))
            {
                return false;
            }

            DeferScheduledWorkForNight(activity);
            return true;
        }

        private bool TryDeferReachedWorkForNight()
        {
            if (StrategyDayNightCycleController.IsSettlementWorkTime
                || !IsNightBlockedReachedActivity(activity))
            {
                return false;
            }

            DeferScheduledWorkForNight(activity);
            return true;
        }

        private void DeferScheduledWorkForNight(ResidentActivity deferredActivity)
        {
            StrategyDebugLogger.Info(
                "Schedule",
                "ResidentWorkDeferredForNight",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("activity", deferredActivity),
                StrategyDebugLogger.F("phase", StrategyDayNightCycleController.CurrentCalendarSnapshot.PhaseLabel));

            if (deferredActivity == ResidentActivity.MovingToGarden
                || deferredActivity == ResidentActivity.WorkingGarden)
            {
                activeGarden = null;
                ResetToNightRest();
                return;
            }

            if (IsForagingActivity(deferredActivity))
            {
                CancelForageWork(true);
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToTree
                || deferredActivity == ResidentActivity.ChoppingTree
                || deferredActivity == ResidentActivity.BuckingTree
                || deferredActivity == ResidentActivity.MovingToPlantTree
                || deferredActivity == ResidentActivity.PlantingTree)
            {
                CancelLumberWork();
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToStone
                || deferredActivity == ResidentActivity.MiningStone)
            {
                CancelStoneWork();
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToMine)
            {
                CancelMineWork();
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToCoalPit)
            {
                CancelCoalPitWork();
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToClayPit)
            {
                CancelClayPitWork();
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToSawmill)
            {
                CancelSawmillWork(true);
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToKiln)
            {
                CancelKilnWork(true);
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToConstructionStorage
                || deferredActivity == ResidentActivity.MovingToConstructionSite
                || deferredActivity == ResidentActivity.BuildingConstruction)
            {
                ResetConstructionWorkToIdle();
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToHuntingRange
                || deferredActivity == ResidentActivity.AimingBow
                || deferredActivity == ResidentActivity.WaitingForHuntHit
                || deferredActivity == ResidentActivity.MovingToHuntCarcass
                || deferredActivity == ResidentActivity.ButcheringRabbit)
            {
                ResetHunterWorkToIdle(true);
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToFishingSpot
                || deferredActivity == ResidentActivity.CastingFishingLine
                || deferredActivity == ResidentActivity.WaitingForFishBite
                || deferredActivity == ResidentActivity.ReelingFish)
            {
                ResetFisherWorkToIdle(true);
                return;
            }

            if (deferredActivity == ResidentActivity.MovingToHouseholdFoodPickup)
            {
                CancelHouseholdFoodWork(true);
                return;
            }

            if (IsNightBlockedStoragePickupActivity(deferredActivity))
            {
                CancelStorageWork(true);
                return;
            }

            if (IsNightBlockedGranaryPickupActivity(deferredActivity))
            {
                CancelGranaryWork(true);
                return;
            }

            ResetToNightRest();
        }

        private void ResetToNightRest()
        {
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            if (TryStartNightSleep())
            {
                return;
            }

            waitTimer = Random.Range(0.65f, 1.45f);
        }

        private static bool IsInterruptibleNightWorkActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.WorkingGarden
                || residentActivity == ResidentActivity.GatheringForage
                || residentActivity == ResidentActivity.PickingUpLooseForage
                || residentActivity == ResidentActivity.ChoppingTree
                || residentActivity == ResidentActivity.BuckingTree
                || residentActivity == ResidentActivity.MiningStone
                || residentActivity == ResidentActivity.BuildingConstruction
                || residentActivity == ResidentActivity.AimingBow
                || residentActivity == ResidentActivity.WaitingForHuntHit
                || residentActivity == ResidentActivity.ButcheringRabbit
                || residentActivity == ResidentActivity.CastingFishingLine
                || residentActivity == ResidentActivity.WaitingForFishBite
                || residentActivity == ResidentActivity.ReelingFish
                || residentActivity == ResidentActivity.PlantingTree;
        }

        private static bool IsNightBlockedReachedActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToGarden
                || residentActivity == ResidentActivity.MovingToForage
                || residentActivity == ResidentActivity.MovingToLooseForagePickup
                || residentActivity == ResidentActivity.MovingToTree
                || residentActivity == ResidentActivity.MovingToStone
                || residentActivity == ResidentActivity.MovingToMine
                || residentActivity == ResidentActivity.MovingToCoalPit
                || residentActivity == ResidentActivity.MovingToClayPit
                || residentActivity == ResidentActivity.MovingToSawmill
                || residentActivity == ResidentActivity.MovingToKiln
                || residentActivity == ResidentActivity.MovingToConstructionStorage
                || residentActivity == ResidentActivity.MovingToConstructionSite
                || residentActivity == ResidentActivity.MovingToHuntingRange
                || residentActivity == ResidentActivity.MovingToHuntCarcass
                || residentActivity == ResidentActivity.MovingToFishingSpot
                || residentActivity == ResidentActivity.MovingToHouseholdFoodPickup
                || residentActivity == ResidentActivity.MovingToProductionInputPickup
                || residentActivity == ResidentActivity.MovingToStoragePickup
                || residentActivity == ResidentActivity.MovingToStorageStonePickup
                || residentActivity == ResidentActivity.MovingToStorageIronPickup
                || residentActivity == ResidentActivity.MovingToStorageCoalPickup
                || residentActivity == ResidentActivity.MovingToStorageClayPickup
                || residentActivity == ResidentActivity.MovingToStoragePlanksPickup
                || residentActivity == ResidentActivity.MovingToStoragePotteryPickup
                || residentActivity == ResidentActivity.MovingToGranaryGamePickup
                || residentActivity == ResidentActivity.MovingToGranaryFishPickup
                || residentActivity == ResidentActivity.MovingToPlantTree;
        }

        private static bool IsNightBlockedStoragePickupActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToStoragePickup
                || residentActivity == ResidentActivity.MovingToStorageStonePickup
                || residentActivity == ResidentActivity.MovingToStorageIronPickup
                || residentActivity == ResidentActivity.MovingToStorageCoalPickup
                || residentActivity == ResidentActivity.MovingToStorageClayPickup
                || residentActivity == ResidentActivity.MovingToStoragePlanksPickup
                || residentActivity == ResidentActivity.MovingToStoragePotteryPickup
                || residentActivity == ResidentActivity.MovingToProductionInputPickup;
        }

        private static bool IsNightBlockedGranaryPickupActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToGranaryGamePickup
                || residentActivity == ResidentActivity.MovingToGranaryFishPickup;
        }
    }
}
