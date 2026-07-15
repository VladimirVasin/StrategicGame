using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyConstructionSpriteFactory
    {
        private static Sprite CreateGroundAlignedSprite(
            StrategyBuildTool tool,
            int variant,
            Sprite source)
        {
            if (source == null)
            {
                return null;
            }

            Vector2 pivot = StrategyBuildingVisualAlignment.GetConstructionPivot(
                tool,
                variant,
                source);
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
