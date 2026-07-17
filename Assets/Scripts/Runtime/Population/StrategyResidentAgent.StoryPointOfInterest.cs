using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private StrategyStoryPointOfInterestAnchor activeStoryPointOfInterest;
        private bool storyPointEncounterPending;

        internal bool IsEligibleStoryPointActivationScout =>
            IsScoutExploring
            && activity == ResidentActivity.MovingToScoutFrontier
            && activeScoutLodge != null
            && activeScoutPointOfInterest == null
            && activeStoryPointOfInterest == null
            && hasScoutTarget;

        internal bool HasCommittedStoryPointOfInterest =>
            activeStoryPointOfInterest != null
            || StrategyStoryPointOfInterestController.Active?.HasCommittedForResident(ResidentId) == true;

        internal bool TryGetStoryPointActivationCell(out Vector2Int cell)
        {
            cell = default;
            return map != null && map.TryWorldToCell(transform.position, out cell);
        }

        private bool TryCommitApproachedStoryPointOfInterest()
        {
            StrategyStoryPointOfInterestController controller =
                StrategyStoryPointOfInterestController.Active;
            if (!IsEligibleStoryPointActivationScout
                || controller == null
                || !controller.TryGetActivationCandidate(
                    this,
                    out StrategyStoryPointOfInterestCandidatePlan candidate)
                || !CanReachCellForReservation(candidate.Cell))
            {
                return false;
            }

            List<Vector3> previousPath = new(path);
            int previousPathIndex = pathIndex;
            bool previousHasTarget = hasTarget;
            float previousWait = waitTimer;
            ResidentActivity previousActivity = activity;
            activity = ResidentActivity.MovingToPointOfInterest;
            if (!TryBuildPathTo(candidate.Cell)
                || !controller.TryCommitActivation(candidate, this, out StrategyStoryPointOfInterestAnchor anchor))
            {
                path.Clear();
                path.AddRange(previousPath);
                pathIndex = previousPathIndex;
                hasTarget = previousHasTarget;
                waitTimer = previousWait;
                activity = previousActivity;
                return false;
            }

            activeScoutLodge.ReleaseExplorationTarget(this);
            activeStoryPointOfInterest = anchor;
            activeScoutPointOfInterest = null;
            scoutTarget = candidate.Cell;
            hasScoutTarget = true;
            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.2f);
            activeScoutLodge.NotifyStoryPointOfInterestTravelStarted(this);
            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "ScoutMoveStarted",
                StrategyDebugLogger.F("residentId", ResidentId),
                StrategyDebugLogger.F("anchorId", anchor.StableId),
                StrategyDebugLogger.F("target", anchor.Cell));
            return true;
        }

        private bool TryStartAssignedStoryPointOfInterestTask(
            StrategyScoutLodge lodge,
            Vector2Int currentCell)
        {
            StrategyStoryPointOfInterestController controller =
                StrategyStoryPointOfInterestController.Active;
            if (controller == null
                || !controller.TryClaimAssignedTarget(
                    this,
                    out StrategyStoryPointOfInterestAnchor anchor))
            {
                return false;
            }

            activeScoutLodge = lodge;
            activeStoryPointOfInterest = anchor;
            activeScoutPointOfInterest = null;
            scoutTarget = anchor.Cell;
            hasScoutTarget = true;
            if (currentCell == scoutTarget)
            {
                StartInvestigatingStoryPointOfInterest();
                return activity == ResidentActivity.InvestigatingPointOfInterest;
            }

            activity = ResidentActivity.MovingToPointOfInterest;
            if (!TryBuildPathTo(scoutTarget))
            {
                controller.ReleaseCommitment(anchor, this);
                activeScoutLodge = null;
                activeStoryPointOfInterest = null;
                hasScoutTarget = false;
                scoutTarget = default;
                scoutWorkCooldown = WasLastPathBuildDeferred
                    ? Random.Range(0.12f, 0.24f)
                    : Random.Range(0.8f, 1.6f);
                ResetScoutMovementToIdle();
                return false;
            }

            hasTarget = true;
            waitTimer = Random.Range(0.05f, 0.2f);
            lodge.NotifyStoryPointOfInterestTravelStarted(this);
            return true;
        }

        private void StartInvestigatingStoryPointOfInterest()
        {
            if (activeScoutLodge == null
                || activeStoryPointOfInterest == null
                || !activeStoryPointOfInterest.IsCommittedTo(this))
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
            activeScoutLodge.NotifyStoryPointOfInterestInvestigationStarted(this);
        }

        private void UpdateInvestigatingStoryPointOfInterest()
        {
            if (activeScoutLodge == null
                || activeStoryPointOfInterest == null
                || !activeStoryPointOfInterest.IsCommittedTo(this))
            {
                CancelScoutWork();
                return;
            }

            pointOfInterestInteractionTimer -= Time.deltaTime;
            AnimateIdle();
            if (pointOfInterestInteractionTimer > 0f || storyPointEncounterPending)
            {
                return;
            }

            StrategyScoutLodge completedLodge = activeScoutLodge;
            StrategyStoryPointOfInterestAnchor completedPoint = activeStoryPointOfInterest;
            StrategyStoryPointOfInterestController controller =
                StrategyStoryPointOfInterestController.Active;
            storyPointEncounterPending = true;
            if (controller == null
                || !controller.BeginInvestigationEncounter(
                    completedPoint,
                    this,
                    resolved => HandleStoryPointEncounterCompleted(
                        completedLodge,
                        completedPoint,
                        resolved)))
            {
                storyPointEncounterPending = false;
                pointOfInterestInteractionTimer = 0.25f;
                return;
            }
        }

        private void HandleStoryPointEncounterCompleted(
            StrategyScoutLodge completedLodge,
            StrategyStoryPointOfInterestAnchor completedPoint,
            bool resolved)
        {
            storyPointEncounterPending = false;
            if (!resolved
                || completedLodge == null
                || completedPoint == null
                || activeScoutLodge != completedLodge
                || activeStoryPointOfInterest != completedPoint)
            {
                return;
            }

            activeStoryPointOfInterest = null;
            activeScoutLodge = null;
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
            completedLodge.NotifyStoryPointOfInterestCompleted(this);
        }

        private void ReleaseActiveStoryPointOfInterest()
        {
            if (activeStoryPointOfInterest == null)
            {
                return;
            }

            storyPointEncounterPending = false;

            activeScoutLodge?.NotifyStoryPointOfInterestInterrupted(this);
            StrategyStoryPointOfInterestController.Active?.ReleaseCommitment(
                activeStoryPointOfInterest,
                this);
            activeStoryPointOfInterest = null;
        }
    }
}
