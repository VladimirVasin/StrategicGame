using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyForagerCampVisualProfile
    {
        public const int SpriteWidth = 176;
        public const int SpriteHeight = 116;
        public const int ConstructionFrameWidth = 184;
        public const int ConstructionFrameHeight = 164;
        public const int ConstructionFrameCount = 7;
        public const float PixelsPerUnit = 48f;
        public const float SpritePivotY = 0.20f;

        public static Vector2 ConstructionPivotNormalized => new(
            0.5f,
            SpriteHeight * SpritePivotY / ConstructionFrameHeight);

        public static int NormalizeVariant(StrategyBuildTool tool, int variant)
        {
            return tool == StrategyBuildTool.ForagerCamp ? 0 : Mathf.Max(0, variant);
        }

        public static Vector3 GetTorchAnchorWorld(Bounds bounds)
        {
            return new Vector3(
                bounds.min.x + bounds.size.x * 1.10f,
                bounds.min.y + bounds.size.y * 0.71f,
                -0.22f);
        }

        public static Vector3 GetStockAnchorWorld(Bounds bounds)
        {
            return new Vector3(
                bounds.min.x + bounds.size.x * 0.77f,
                bounds.min.y + bounds.size.y * 0.31f,
                -0.13f);
        }
    }
}
