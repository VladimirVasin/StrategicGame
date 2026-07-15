using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private static readonly Dictionary<int, Sprite> CachedChickenCoopFrames = new();

        public static Sprite GetStandaloneChickenCoopSprite(int frame)
        {
            int normalizedFrame = NormalizeVariant(
                frame,
                StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount);
            if (!CachedChickenCoopFrames.TryGetValue(normalizedFrame, out Sprite sprite)
                || sprite == null)
            {
                if (StrategyChickenCoopVisualProfile.TryGetAuthoredStandaloneSprite(
                        normalizedFrame,
                        out Sprite authored))
                {
                    sprite = AlignStandaloneChickenCoopSprite(authored);
                }
                else
                {
                    sprite = GetProceduralStandaloneChickenCoopSprite(normalizedFrame);
                }

                CachedChickenCoopFrames[normalizedFrame] = sprite;
            }

            return sprite;
        }

        internal static Sprite GetProceduralStandaloneChickenCoopSprite(int frame)
        {
            return CreateChickenCoopSprite(frame);
        }

        private static Sprite AlignStandaloneChickenCoopSprite(Sprite source)
        {
            if (source == null)
            {
                return null;
            }

            Sprite aligned = Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(0.5f, StrategyChickenCoopVisualProfile.StandalonePivotY),
                StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);
            aligned.name = source.name + " Standalone";
            return aligned;
        }
    }
}
