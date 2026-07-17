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

            if (cinematicVisualOverride != null)
            {
                return;
            }

            if (UpdateAge())
            {
                return;
            }

            UpdatePersonalNightTorchState();

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

            if (IsSleepingAtCampfire)
            {
                UpdateSleepingByCampfire();
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

            UpdateWorkCooldowns(Time.deltaTime);
            if (IsStationaryFuneralActivity(activity))
            {
                UpdateFuneralActivity();
                return;
            }

            if (TryPauseActiveWorkForNight() || TryCancelChildPlayForNight())
            {
                return;
            }

            if (taskExecution.TryExecute(
                activity,
                StrategyResidentTaskExecutionPhase.BeforeHomeSchedule))
            {
                return;
            }

            if (!IsNightLightActivity(activity) && UpdateNightHomeState())
            {
                return;
            }

            if (taskExecution.TryExecute(activity, StrategyResidentTaskExecutionPhase.Normal))
            {
                return;
            }

            if (TryCommitApproachedStoryPointOfInterest())
            {
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

                if (TryRetryStorageProductionResourceReturn()
                    || TryStartScheduledWorkTask()
                    || TryStartChildIdleActivity())
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
