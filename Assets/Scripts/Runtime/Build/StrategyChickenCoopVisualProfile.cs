using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyChickenCoopVisualProfile
    {
        public const int AnimationFrameCount =
            StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount;
        public const int AuthoredFrameWidth = 92;
        public const int AuthoredFrameHeight = 92;
        public const float StandalonePixelsPerUnit = 42f;
        public const float HouseUpgradePixelsPerUnit = 84f;
        public const float StandalonePivotY = 0.10f;
        public const float UpgradePivotY = 0.12f;
        internal const float ProceduralStandalonePixelsPerUnit = 21f;

        private static readonly Dictionary<int, Sprite> CachedUpgradeFrames = new();

        internal static void ResetCache()
        {
            CachedUpgradeFrames.Clear();
        }

        public static bool TryGetAuthoredStandaloneSprite(int frame, out Sprite sprite)
        {
            int normalizedFrame = PositiveModulo(frame, AnimationFrameCount);
            if (!StrategyVisualCatalogProvider.TryGetSequenceSprite(
                    StrategyVisualSequenceIds.ChickenCoopProduction,
                    normalizedFrame,
                    out sprite)
                || !MatchesAuthoredFrameContract(sprite, normalizedFrame))
            {
                sprite = null;
                return false;
            }

            return true;
        }

        public static bool TryGetAuthoredUpgradeSprite(int frame, out Sprite sprite)
        {
            int normalizedFrame = PositiveModulo(frame, AnimationFrameCount);
            if (CachedUpgradeFrames.TryGetValue(normalizedFrame, out sprite) && sprite != null)
            {
                return true;
            }

            if (!TryGetAuthoredStandaloneSprite(normalizedFrame, out Sprite source))
            {
                sprite = null;
                return false;
            }

            sprite = Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(0.5f, UpgradePivotY),
                HouseUpgradePixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);
            sprite.name = source.name + " House Upgrade";
            CachedUpgradeFrames[normalizedFrame] = sprite;
            return true;
        }

        private static bool MatchesAuthoredFrameContract(Sprite sprite, int frame)
        {
            return sprite != null
                && sprite.texture != null
                && sprite.texture.width == AuthoredFrameWidth * AnimationFrameCount
                && sprite.texture.height == AuthoredFrameHeight
                && Mathf.RoundToInt(sprite.rect.x) == frame * AuthoredFrameWidth
                && Mathf.RoundToInt(sprite.rect.y) == 0
                && Mathf.RoundToInt(sprite.rect.width) == AuthoredFrameWidth
                && Mathf.RoundToInt(sprite.rect.height) == AuthoredFrameHeight
                && Mathf.Approximately(sprite.pixelsPerUnit, StandalonePixelsPerUnit)
                && Mathf.Approximately(sprite.pivot.x / sprite.rect.width, 0.5f)
                && Mathf.Approximately(sprite.pivot.y / sprite.rect.height, StandalonePivotY);
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int normalized = value % modulus;
            return normalized < 0 ? normalized + modulus : normalized;
        }
    }
}
