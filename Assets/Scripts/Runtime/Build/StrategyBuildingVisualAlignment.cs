using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingVisualAlignment
    {
        public static float GetSpritePivotY(StrategyBuildTool tool)
        {
            return tool == StrategyBuildTool.ForagerCamp
                || tool == StrategyBuildTool.TradingPost
                    ? 0.20f
                    : 0.10f;
        }

        public static Vector2 GetConstructionPivot(
            StrategyBuildTool tool,
            int variant,
            Sprite constructionSprite)
        {
            if (constructionSprite == null)
            {
                return new Vector2(0.5f, GetSpritePivotY(tool));
            }

            if (constructionSprite.pixelsPerUnit <= 24f + 0.01f)
            {
                return tool == StrategyBuildTool.ForagerCamp
                    ? StrategyForagerCampVisualProfile.ConstructionPivotNormalized
                    : new Vector2(0.5f, 0.10f);
            }

            float pivotY = GetSpritePivotY(tool);
            if (StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, variant, out Sprite finalSprite)
                && finalSprite != null
                && finalSprite.pixelsPerUnit > 0f)
            {
                float finalPivotWorld = finalSprite.rect.height * pivotY / finalSprite.pixelsPerUnit;
                float constructionPivotPixels = finalPivotWorld * constructionSprite.pixelsPerUnit;
                pivotY = constructionPivotPixels / Mathf.Max(1f, constructionSprite.rect.height);
            }

            return new Vector2(0.5f, Mathf.Clamp01(pivotY));
        }
    }
}
