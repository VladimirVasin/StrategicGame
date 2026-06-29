using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {
        private const float ThreatReactionRefreshSeconds = 1.1f;
        private const float ThreatReactionRefreshDistanceSqr = 0.85f;

        private float nextThreatReactionRefreshTime;
        private Vector3 lastReactionThreatWorld;

        private bool ShouldSkipThreatReaction(StrategyRabbitBehaviorState reactionState, Vector3 threatWorld)
        {
            return state == reactionState
                && Time.realtimeSinceStartup < nextThreatReactionRefreshTime
                && (threatWorld - lastReactionThreatWorld).sqrMagnitude <= ThreatReactionRefreshDistanceSqr;
        }

        private void MarkThreatReaction(Vector3 threatWorld)
        {
            lastReactionThreatWorld = threatWorld;
            nextThreatReactionRefreshTime = Time.realtimeSinceStartup + ThreatReactionRefreshSeconds;
        }
    }
}
