namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyScoutLodge
    {
        private bool returnAfterStoryPoint;

        public bool ReturnAfterStoryPoint => returnAfterStoryPoint;

        public bool RequestRecall()
        {
            if (IsReturning || returnAfterStoryPoint)
            {
                return true;
            }

            return BeginScoutReturn("Recalled to Lodge", "recall");
        }

        public void NotifyStoryPointOfInterestTravelStarted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Approaching discovery";
            }
        }

        public void NotifyStoryPointOfInterestInvestigationStarted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Investigating discovery";
            }
        }

        public void NotifyStoryPointOfInterestCompleted(StrategyResidentAgent resident)
        {
            if (resident == null || !workers.Contains(resident))
            {
                return;
            }

            missionStatus = "Planning route";
            if (returnAfterStoryPoint && IsExploring)
            {
                returnAfterStoryPoint = false;
                BeginScoutReturn("Discovery complete - returning", "story_complete");
            }
        }

        public void NotifyStoryPointOfInterestInterrupted(StrategyResidentAgent resident)
        {
            if (resident != null && workers.Contains(resident))
            {
                missionStatus = "Planning route";
            }
        }

        private bool BeginScoutReturn(string status, string reason)
        {
            if (!IsExploring || !TryGetWorker(0, out StrategyResidentAgent worker))
            {
                return false;
            }

            if (worker.HasCommittedStoryPointOfInterest)
            {
                returnAfterStoryPoint = true;
                missionStatus = "Completing discovery before return";
                StrategyDebugLogger.Info(
                    "ScoutLodge",
                    "ReturnDeferredForStoryPoint",
                    StrategyDebugLogger.F("lodgeOrigin", Origin),
                    StrategyDebugLogger.F("worker", worker.FullName),
                    StrategyDebugLogger.F("reason", reason));
                return true;
            }

            returnAfterStoryPoint = false;
            expeditionState = StrategyScoutExpeditionState.Returning;
            remainingFieldRations = 0f;
            missionStatus = status;
            worker.BeginScoutReturn(this);
            StrategyDebugLogger.Info(
                "ScoutLodge",
                "ExpeditionReturning",
                StrategyDebugLogger.F("lodgeOrigin", Origin),
                StrategyDebugLogger.F("worker", worker.FullName),
                StrategyDebugLogger.F("reason", reason));
            return true;
        }
    }
}
