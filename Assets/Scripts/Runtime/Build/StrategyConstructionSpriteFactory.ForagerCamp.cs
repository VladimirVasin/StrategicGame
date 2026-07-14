using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyConstructionSpriteFactory
    {
        private static Sprite CreateGroundAlignedSprite(StrategyBuildTool tool, Sprite source)
        {
            if (source == null)
            {
                return null;
            }

            Vector2 pivot = tool == StrategyBuildTool.ForagerCamp
                ? StrategyForagerCampVisualProfile.ConstructionPivotNormalized
                : new Vector2(0.5f, 0.10f);
            Sprite aligned = Sprite.Create(
                source.texture,
                source.rect,
                pivot,
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);
            aligned.name = source.name + " Ground Aligned";
            return aligned;
        }
    }
}
