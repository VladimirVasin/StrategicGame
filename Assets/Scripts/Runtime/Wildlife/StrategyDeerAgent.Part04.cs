using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDeerAgent
    {
        private const float ThreatReactionRefreshSeconds = 1.15f;
        private const float ThreatReactionRefreshDistanceSqr = 1.0f;
        private const float FailedFleeRetrySeconds = 1.4f;

        private float nextThreatReactionRefreshTime;
        private float nextFailedFleeRetryTime;
        private Vector3 lastReactionThreatWorld;

        private void StartAlert(Vector3 threatWorld, bool noisyThreat)
        {
            if (ShouldSkipThreatReaction(StrategyDeerBehaviorState.Alert, threatWorld))
            {
                lastThreatWorld = threatWorld;
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            MarkThreatReaction(threatWorld);
            stateTimer = noisyThreat ? Random.Range(1.5f, 3.2f) : Random.Range(0.9f, 2.2f);
            SetState(StrategyDeerBehaviorState.Alert, true, noisyThreat);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            if (ShouldDeferFailedFleeRetry(threatWorld))
            {
                lastThreatWorld = threatWorld;
                return;
            }

            if (ShouldSkipThreatReaction(StrategyDeerBehaviorState.Fleeing, threatWorld))
            {
                lastThreatWorld = threatWorld;
                return;
            }

            lastThreatWorld = threatWorld;
            MarkThreatReaction(threatWorld);
            stateTimer = noisyThreat ? Random.Range(2.1f, 3.8f) : Random.Range(1.5f, 2.8f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget)
            {
                nextFailedFleeRetryTime = Time.realtimeSinceStartup + FailedFleeRetrySeconds;
                if (state != StrategyDeerBehaviorState.Alert)
                {
                    StartAlert(threatWorld, noisyThreat);
                }

                return;
            }

            SetState(StrategyDeerBehaviorState.Fleeing, true, noisyThreat);
        }

        private void StartFailedFleeAlert(Vector3 threatWorld, bool noisyThreat)
        {
            nextFailedFleeRetryTime = Time.realtimeSinceStartup + FailedFleeRetrySeconds;
            StartAlert(threatWorld, noisyThreat);
        }

        private bool ShouldSkipThreatReaction(StrategyDeerBehaviorState reactionState, Vector3 threatWorld)
        {
            return state == reactionState
                && Time.realtimeSinceStartup < nextThreatReactionRefreshTime
                && (threatWorld - lastReactionThreatWorld).sqrMagnitude <= ThreatReactionRefreshDistanceSqr;
        }

        private bool ShouldDeferFailedFleeRetry(Vector3 threatWorld)
        {
            return Time.realtimeSinceStartup < nextFailedFleeRetryTime
                && (threatWorld - lastReactionThreatWorld).sqrMagnitude <= ThreatReactionRefreshDistanceSqr;
        }

        private void MarkThreatReaction(Vector3 threatWorld)
        {
            lastReactionThreatWorld = threatWorld;
            nextThreatReactionRefreshTime = Time.realtimeSinceStartup + ThreatReactionRefreshSeconds;
        }
    }
}
