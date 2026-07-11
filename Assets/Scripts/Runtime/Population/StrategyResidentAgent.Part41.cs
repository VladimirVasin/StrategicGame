using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const int ScheduledDecisionBudgetPerFrameSmallSettlement = 8;
        private const int ScheduledDecisionBudgetPerFrameLargeSettlement = 4;
        private const int LargeSettlementResidentThreshold = 14;
        private const float ScheduledDecisionDeferredWaitMin = 0.06f;
        private const float ScheduledDecisionDeferredWaitMax = 0.18f;
        private const float ScheduledDecisionBudgetLogIntervalSeconds = 6f;

        private static int scheduledDecisionBudgetFrame = -1;
        private static int scheduledDecisionsThisFrame;
        private static int scheduledDecisionDeferralsSinceLog;
        private static float nextScheduledDecisionBudgetLogTime;

        public bool IsOffDutyForNight
        {
            get
            {
                return StrategyDayNightCycleController.IsResidentEveningHomeTime
                    && (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
                    && (HasExternalWorkplace || constructionSite != null || IsHouseholder);
            }
        }

        private bool TryStartScheduledWorkTask()
        {
            if (StrategyDayNightCycleController.IsResidentEveningHomeTime)
            {
                waitTimer = Random.Range(0.55f, 1.25f);
                return false;
            }

            if (!TryConsumeScheduledDecisionBudget())
            {
                waitTimer = Random.Range(ScheduledDecisionDeferredWaitMin, ScheduledDecisionDeferredWaitMax);
                return true;
            }

            pathBuildDeferredDuringDecision = false;
            evaluatingPlannedTasks = true;
            bool started = taskExecution.TryStartPlannedTask(() => pathBuildDeferredDuringDecision);
            evaluatingPlannedTasks = false;
            if (pathBuildDeferredDuringDecision)
            {
                waitTimer = Random.Range(0.18f, 0.38f);
                return true;
            }

            return started;
        }

        private bool TryConsumeScheduledDecisionBudget()
        {
            int frame = Time.frameCount;
            if (scheduledDecisionBudgetFrame != frame)
            {
                scheduledDecisionBudgetFrame = frame;
                scheduledDecisionsThisFrame = 0;
            }

            int residentCount = population != null && population.Residents != null ? population.Residents.Count : 0;
            int budget = residentCount >= LargeSettlementResidentThreshold
                ? ScheduledDecisionBudgetPerFrameLargeSettlement
                : ScheduledDecisionBudgetPerFrameSmallSettlement;
            if (scheduledDecisionsThisFrame < budget)
            {
                scheduledDecisionsThisFrame++;
                StrategyResidentPerformanceCounters.RecordScheduledDecisionRun();
                return true;
            }

            scheduledDecisionDeferralsSinceLog++;
            StrategyResidentPerformanceCounters.RecordScheduledDecisionDeferral();
            float now = Time.realtimeSinceStartup;
            if (now >= nextScheduledDecisionBudgetLogTime)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentScheduledDecisionBudgetDeferred",
                    StrategyDebugLogger.F("deferred", scheduledDecisionDeferralsSinceLog),
                    StrategyDebugLogger.F("budgetPerFrame", budget),
                    StrategyDebugLogger.F("residents", residentCount));
                scheduledDecisionDeferralsSinceLog = 0;
                nextScheduledDecisionBudgetLogTime = now + ScheduledDecisionBudgetLogIntervalSeconds;
            }

            return false;
        }

        private bool TryPauseActiveWorkForNight()
        {
            if (!StrategyDayNightCycleController.IsResidentEveningHomeTime)
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
            if (!StrategyDayNightCycleController.IsResidentEveningHomeTime
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

            if (deferredActivity == ResidentActivity.MovingToForge)
            {
                CancelForgeWork(true);
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

            if (deferredActivity == ResidentActivity.MovingToHouseholdFoodPickup
                || deferredActivity == ResidentActivity.MovingToHouseholdPotteryPickup)
            {
                CancelHouseholdFoodWork(true, "nightfall");
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
                || residentActivity == ResidentActivity.MovingToForge
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
                || residentActivity == ResidentActivity.MovingToStorageToolsPickup
                || residentActivity == ResidentActivity.MovingToHouseholdPotteryPickup
                || residentActivity == ResidentActivity.MovingToGranaryGamePickup
                || residentActivity == ResidentActivity.MovingToGranaryFishPickup
                || residentActivity == ResidentActivity.MovingToGranaryForagePickup
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
                || residentActivity == ResidentActivity.MovingToStorageToolsPickup
                || residentActivity == ResidentActivity.MovingToProductionInputPickup;
        }

        private static bool IsNightBlockedGranaryPickupActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToGranaryGamePickup
                || residentActivity == ResidentActivity.MovingToGranaryFishPickup
                || residentActivity == ResidentActivity.MovingToGranaryForagePickup;
        }
    }
}
