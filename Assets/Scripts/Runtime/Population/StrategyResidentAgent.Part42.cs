using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool UpdateNightHomeState()
        {
            if (sleepingInsideHome)
            {
                UpdateNightSleep();
                return true;
            }

            if (returningHomeToSleep && !IsNightSleepTime())
            {
                CancelNightSleepReturn();
                return false;
            }

            return TryStartNightSleep();
        }

        private bool TryStartNightSleep()
        {
            if (!CanStartNightSleep())
            {
                return false;
            }

            Vector3 targetWorld = GetHomeExitWorld();
            returningHomeToSleep = true;
            StartMovingHome(targetWorld);
            waitTimer = 0f;
            StrategyDebugLogger.Info(
                "Population",
                "ResidentGoingHomeToSleep",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
            return true;
        }

        private bool CanStartNightSleep()
        {
            return IsNightSleepTime()
                && home != null
                && !hiddenInsideHome
                && !hiddenUnderground
                && !IsPendingRefugee
                && !deathRequested
                && !IsHomeboundYoungChild
                && !IsFuneralActivity(activity)
                && !HasAnyCarriedResource()
                && (activity == ResidentActivity.Idle || activity == ResidentActivity.TendingHousehold)
                && !returningHomeToSleep;
        }

        private void EnterNightSleep()
        {
            if (home == null || !IsNightSleepTime())
            {
                CancelNightSleepReturn();
                return;
            }

            returningHomeToSleep = false;
            sleepingInsideHome = true;
            hiddenInsideHome = true;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.85f, 1.85f);
            activity = ResidentActivity.StayingInsideHome;
            activeGarden = null;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            ClearVisibleCarriedResourcesForHomeInterior();
            transform.position = GetHomeInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetWorldPresenceVisible(false);
            footstepAudio?.ResetStepPhase();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentWentToSleep",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("homeOrigin", home.Origin));
        }

        private void UpdateNightSleep()
        {
            if (home == null || !IsNightSleepTime())
            {
                ReleaseNightSleep(true);
                return;
            }

            activity = ResidentActivity.StayingInsideHome;
            transform.position = GetHomeInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            footstepAudio?.ResetStepPhase();
        }

        private void ReleaseNightSleep(bool log)
        {
            sleepingInsideHome = false;
            returningHomeToSleep = false;
            hiddenInsideHome = false;
            SetWorldPresenceVisible(true);
            transform.position = GetHomeExitWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.25f, 1.15f);
            UseIdleSprite();
            UpdateWorldSorting();

            if (log)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentWokeUp",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
            }
        }

        private void CancelNightSleepReturn()
        {
            returningHomeToSleep = false;
            if (activity != ResidentActivity.MovingHome)
            {
                return;
            }

            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.35f, 0.95f);
            UseIdleSprite();
        }

        private void ClearVisibleCarriedResourcesForHomeInterior()
        {
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedIronAmount = 0;
            carriedCoalAmount = 0;
            carriedPlanksAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            ClearCarriedHouseholdFood();
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedIronVisible(false);
            SetCarriedCoalVisible(false);
            SetCarriedPlanksVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            SetFishingLineVisible(false);
        }

        private static bool IsNightSleepTime()
        {
            return StrategyDayNightCycleController.CurrentCalendarSnapshot.Phase
                == StrategyTimeOfDayPhase.Night;
        }
    }
}
