using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyStoryPointOfInterestActivationPolicy
    {
        public const float VisibilitySafetyMargin = 0.75f;
        public const float ActivationBandWidth = 3f;

        public static bool IsInsideActivationBand(
            long distanceSquared,
            float daylightVisibleOuterRadius)
        {
            float inner = Mathf.Max(0f, daylightVisibleOuterRadius) + VisibilitySafetyMargin;
            float outer = inner + ActivationBandWidth;
            return distanceSquared > inner * inner && distanceSquared <= outer * outer;
        }

        public static bool IsBetterCandidate(
            bool found,
            long distanceSquared,
            string anchorId,
            int residentId,
            long bestDistanceSquared,
            string bestAnchorId,
            int bestResidentId)
        {
            if (!found || distanceSquared < bestDistanceSquared)
            {
                return true;
            }

            if (distanceSquared > bestDistanceSquared)
            {
                return false;
            }

            int anchorComparison = string.CompareOrdinal(anchorId, bestAnchorId);
            return anchorComparison < 0
                || anchorComparison == 0 && residentId < bestResidentId;
        }
    }
}
