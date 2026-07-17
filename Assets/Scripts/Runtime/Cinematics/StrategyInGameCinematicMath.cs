using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyInGameCinematicMath
    {
        public static float CalculateLetterboxBarFraction(
            float viewportAspect,
            float targetAspect,
            float minimumBarFraction,
            float maximumBarFraction = 0.24f)
        {
            float safeViewportAspect = viewportAspect > 0f
                ? viewportAspect
                : 16f / 9f;
            float safeTargetAspect = Mathf.Max(1f, targetAspect);
            float minimum = Mathf.Clamp(minimumBarFraction, 0f, 0.49f);
            float maximum = Mathf.Clamp(maximumBarFraction, minimum, 0.49f);
            float aspectBarFraction = (1f - safeViewportAspect / safeTargetAspect) * 0.5f;
            return Mathf.Clamp(Mathf.Max(minimum, aspectBarFraction), minimum, maximum);
        }

        public static float CalculateSafeHeightFraction(float barFraction)
        {
            return Mathf.Clamp01(1f - Mathf.Clamp(barFraction, 0f, 0.49f) * 2f);
        }

        public static float CalculateTargetOrthographicSize(
            StrategyInGameCinematicFraming framing,
            float cameraAspect,
            float safeHeightFraction)
        {
            Vector3 extents = framing.WorldBounds.extents;
            float paddedHalfWidth = Mathf.Max(0f, extents.x) + framing.PerSidePadding.x;
            float paddedHalfHeight = Mathf.Max(0f, extents.y) + framing.PerSidePadding.y;
            float widthSize = paddedHalfWidth / Mathf.Max(0.01f, cameraAspect);
            float heightSize = paddedHalfHeight / Mathf.Max(0.01f, safeHeightFraction);
            float requiredSize = Mathf.Max(widthSize, heightSize);
            float minimum = Mathf.Max(0.01f, framing.MinimumOrthographicSize);
            float maximum = framing.MaximumOrthographicSize > 0f
                ? Mathf.Max(minimum, framing.MaximumOrthographicSize)
                : float.PositiveInfinity;
            return float.IsPositiveInfinity(maximum)
                ? Mathf.Max(minimum, requiredSize)
                : Mathf.Clamp(requiredSize, minimum, maximum);
        }
    }
}
