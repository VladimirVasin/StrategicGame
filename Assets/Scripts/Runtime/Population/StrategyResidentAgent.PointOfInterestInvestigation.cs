using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void StartInvestigatingPointOfInterest()
        {
            if (activeStoryPointOfInterest != null)
            {
                StartInvestigatingStoryPointOfInterest();
                return;
            }

            if (activeScoutLodge == null
                || activeScoutPointOfInterest == null
                || !activeScoutPointOfInterest.IsReservedBy(this))
            {
                CancelScoutWork();
                return;
            }

            activity = ResidentActivity.InvestigatingPointOfInterest;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            pointOfInterestInteractionTimer = Random.Range(
                PointOfInterestInteractionSecondsMin,
                PointOfInterestInteractionSecondsMax);
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            activeScoutLodge.NotifyPointOfInterestInvestigationStarted(this);
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "InvestigationStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pointId", activeScoutPointOfInterest.StableId),
                StrategyDebugLogger.F("target", activeScoutPointOfInterest.Cell));
        }

        private void UpdateInvestigatingPointOfInterest()
        {
            if (activeStoryPointOfInterest != null)
            {
                UpdateInvestigatingStoryPointOfInterest();
                return;
            }

            if (activeScoutLodge == null
                || activeScoutPointOfInterest == null
                || !activeScoutPointOfInterest.IsReservedBy(this))
            {
                CancelScoutWork();
                return;
            }

            pointOfInterestInteractionTimer -= Time.deltaTime;
            AnimateIdle();
            if (pointOfInterestInteractionTimer > 0f)
            {
                return;
            }

            StrategyScoutLodge completedLodge = activeScoutLodge;
            StrategyPointOfInterest completedPoint = activeScoutPointOfInterest;
            StrategyPointOfInterestController pointController = StrategyPointOfInterestController.Active;
            if (pointController == null || !pointController.CompleteInvestigation(completedPoint, this))
            {
                CancelScoutWork();
                return;
            }

            completedLodge.NotifyPointOfInterestCompleted(this);
            activeScoutLodge = null;
            activeScoutPointOfInterest = null;
            scoutTarget = default;
            hasScoutTarget = false;
            pointOfInterestInteractionTimer = 0f;
            scoutWorkCooldown = Random.Range(0.05f, 0.15f);
            activity = GetRestingActivity();
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
            waitTimer = Random.Range(0.05f, 0.15f);
            StrategyDebugLogger.Info(
                "PointOfInterest",
                "InvestigationCompleted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("pointId", completedPoint.StableId),
                StrategyDebugLogger.F("target", completedPoint.Cell));
        }
    }
}
