using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyVisualCatalogProvider
    {
        private const string ResourcePath = "Visual/StrategyVisualCatalog";

        private static StrategyVisualCatalog catalog;
        private static bool loadAttempted;

        public static void Prewarm()
        {
            StrategyVisualCatalog current = EnsureLoaded();
            StrategyTerrainTexturePainter.PrewarmCatalog(current);
        }

        public static bool TryGetBuildingSprite(StrategyBuildTool tool, int variant, out Sprite sprite)
        {
            StrategyVisualCatalog current = EnsureLoaded();
            return current != null && current.TryGetBuildingSprite(tool, variant, out sprite)
                || ReturnMissing(out sprite);
        }

        public static bool TryGetResidentSprite(
            StrategyResidentGender gender,
            StrategyResidentLifeStage lifeStage,
            StrategyResidentVisualPose pose,
            int variant,
            int frame,
            out Sprite sprite)
        {
            StrategyVisualCatalog current = EnsureLoaded();
            return current != null
                && current.TryGetResidentSprite(gender, lifeStage, pose, variant, frame, out sprite)
                || ReturnMissing(out sprite);
        }

        public static bool TryGetBuildingGroundSprite(
            StrategyBuildTool tool,
            Vector2Int footprint,
            int variant,
            out Sprite sprite)
        {
            StrategyVisualCatalog current = EnsureLoaded();
            return current != null
                && current.TryGetBuildingGroundSprite(tool, footprint, variant, out sprite)
                || ReturnMissing(out sprite);
        }

        public static bool TryGetNatureSprite(
            StrategyNaturePropKind kind,
            int variant,
            out Sprite sprite)
        {
            StrategyVisualCatalog current = EnsureLoaded();
            return current != null && current.TryGetNatureSprite(kind, variant, out sprite)
                || ReturnMissing(out sprite);
        }

        public static bool TryGetSequenceSprite(string id, int frame, out Sprite sprite)
        {
            StrategyVisualCatalog current = EnsureLoaded();
            return current != null && current.TryGetSequenceSprite(id, frame, out sprite)
                || ReturnMissing(out sprite);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetCache();
        }

        internal static void ResetCache()
        {
            catalog = null;
            loadAttempted = false;
        }

        private static StrategyVisualCatalog EnsureLoaded()
        {
            if (!loadAttempted)
            {
                loadAttempted = true;
                catalog = Resources.Load<StrategyVisualCatalog>(ResourcePath);
            }

            return catalog;
        }

        private static bool ReturnMissing(out Sprite sprite)
        {
            sprite = null;
            return false;
        }
    }
}
