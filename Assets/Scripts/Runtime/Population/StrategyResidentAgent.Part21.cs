using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void HandleReachedPathTarget()
        {
            CompletePendingTrailRouteTraversal();
            hasTarget = false;
            if (TryDeferReachedWorkForNight())
            {
                return;
            }

            if (taskExecution.TryExecute(
                activity,
                StrategyResidentTaskExecutionPhase.PathCompleted))
            {
                return;
            }

            if (activity == ResidentActivity.MovingHome && returningHomeWithFuneralTorch)
            {
                CompleteFuneralTorchReturnHome();
            }
            else if (activity == ResidentActivity.MovingHome && returningHomeToSleep)
            {
                EnterNightSleep();
            }
            else if (activity == ResidentActivity.MovingToCampfireSleep && returningToHomelessCamp)
            {
                EnterHomelessCampSleepSpot();
            }
            else if (IsMovingChildPlayActivity(activity))
            {
                StartReachedChildPlay();
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
