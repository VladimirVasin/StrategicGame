using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyCinematicRatAnimation
    {
        Run = 0,
        Escape = 1
    }

    internal static class StrategyCinematicRatSpriteFactory
    {
        internal const int VariantCount = 3;
        internal const int RunFrameCount = 6;
        internal const int EscapeFrameCount = 4;

        internal static int GetFrameCount(StrategyCinematicRatAnimation animation)
        {
            return animation == StrategyCinematicRatAnimation.Escape
                ? EscapeFrameCount
                : RunFrameCount;
        }

        internal static Sprite GetFrame(
            StrategyCinematicRatAnimation animation,
            int variant,
            int frameIndex)
        {
            // The cinematic must use the exact settlement-fauna mouse artwork.
            // Frame progression is expressed through actor transform motion.
            int normalizedVariant = PositiveModulo(variant, VariantCount);
            return StrategySettlementFaunaSpriteFactory.GetMouseSprite(normalizedVariant);
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
