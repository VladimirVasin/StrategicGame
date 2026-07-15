using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingVisualAnchorProfile
    {
        public const int InteriorWorkerLayerOffset = 96;

        public static Vector3 GetStockAnchorWorld(
            StrategyBuildTool tool,
            StrategyResourceType resource,
            Bounds bounds)
        {
            return tool switch
            {
                StrategyBuildTool.LumberjackCamp => AtMaxX(bounds, 0.28f, 0.34f, -0.13f),
                StrategyBuildTool.StonecutterCamp => AtMaxX(bounds, 0.30f, 0.34f, -0.13f),
                StrategyBuildTool.Mine => AtMaxX(bounds, 0.26f, 0.32f, -0.13f),
                StrategyBuildTool.CoalPit => AtMaxX(bounds, 0.26f, 0.30f, -0.13f),
                StrategyBuildTool.ClayPit => AtMaxX(bounds, 0.28f, 0.31f, -0.13f),
                StrategyBuildTool.HunterCamp => AtMaxX(bounds, 0.30f, 0.35f, -0.13f),
                StrategyBuildTool.FisherHut => AtMaxX(bounds, 0.25f, 0.38f, -0.13f),
                StrategyBuildTool.ForagerCamp => StrategyForagerCampVisualProfile.GetStockAnchorWorld(bounds),
                StrategyBuildTool.ChickenCoop => AtMaxX(bounds, 0.36f, 0.36f, -0.13f),
                StrategyBuildTool.Sawmill => GetSawmillStockAnchorWorld(resource, bounds),
                StrategyBuildTool.Kiln => GetKilnStockAnchorWorld(resource, bounds),
                StrategyBuildTool.Forge => GetForgeStockAnchorWorld(resource, bounds),
                StrategyBuildTool.StorageYard => GetStorageStockAnchorWorld(resource, bounds),
                StrategyBuildTool.Granary => GetGranaryStockAnchorWorld(resource, bounds),
                _ => new Vector3(bounds.center.x, bounds.min.y + 0.32f, -0.13f)
            };
        }

        public static Vector3 GetInteriorWorkWorld(
            StrategyBuildTool tool,
            Bounds bounds,
            int workerSlot,
            int activeWorkerCount)
        {
            int slot = Mathf.Max(0, workerSlot);
            bool split = activeWorkerCount > 1;
            return tool switch
            {
                StrategyBuildTool.CoalPit => new Vector3(
                    bounds.center.x + (split ? (slot == 0 ? -0.27f : 0.27f) : 0f),
                    bounds.min.y + bounds.size.y * (split && slot > 0 ? 0.43f : 0.38f),
                    -0.08f),
                StrategyBuildTool.ClayPit => new Vector3(
                    bounds.center.x + (split ? (slot == 0 ? -0.25f : 0.25f) : 0f),
                    bounds.min.y + bounds.size.y * (split && slot > 0 ? 0.44f : 0.39f),
                    -0.08f),
                StrategyBuildTool.Sawmill => new Vector3(
                    bounds.center.x + (split ? (slot == 0 ? -0.34f : 0.34f) : 0f),
                    bounds.min.y + bounds.size.y * 0.44f,
                    -0.08f),
                StrategyBuildTool.Kiln => new Vector3(
                    bounds.center.x - 0.24f,
                    bounds.min.y + bounds.size.y * 0.42f,
                    -0.08f),
                StrategyBuildTool.Forge => new Vector3(
                    bounds.center.x - 0.18f,
                    bounds.min.y + bounds.size.y * 0.46f,
                    -0.08f),
                _ => new Vector3(
                    bounds.center.x,
                    bounds.min.y + bounds.size.y * 0.42f,
                    -0.08f)
            };
        }

        public static Vector3 GetWorkFocusWorld(StrategyBuildTool tool, Bounds bounds)
        {
            return tool switch
            {
                StrategyBuildTool.Sawmill => new Vector3(
                    bounds.center.x,
                    bounds.min.y + bounds.size.y * 0.45f,
                    -0.08f),
                StrategyBuildTool.Kiln => new Vector3(
                    bounds.center.x + 0.16f,
                    bounds.min.y + bounds.size.y * 0.44f,
                    -0.08f),
                StrategyBuildTool.Forge => new Vector3(
                    bounds.center.x + 0.10f,
                    bounds.min.y + bounds.size.y * 0.43f,
                    -0.08f),
                _ => new Vector3(
                    bounds.center.x,
                    bounds.min.y + bounds.size.y * 0.42f,
                    -0.08f)
            };
        }

        public static int GetInteriorWorkerSortingOffset(int workerSlot)
        {
            return InteriorWorkerLayerOffset + Mathf.Max(0, workerSlot) * 2;
        }

        public static Vector3 GetMineEntranceEffectWorld(Bounds bounds)
        {
            return new Vector3(bounds.center.x - 0.22f, bounds.min.y + 0.40f, -0.12f);
        }

        public static Vector3 GetCinematicAnchorWorld(StrategyBuildTool tool, Bounds bounds)
        {
            return tool switch
            {
                StrategyBuildTool.House => LerpBounds(bounds, 0.50f, 0.55f, -0.22f),
                StrategyBuildTool.Mine => new Vector3(
                    bounds.center.x - 0.16f,
                    bounds.min.y + bounds.size.y * 0.22f,
                    -0.22f),
                StrategyBuildTool.CoalPit => LerpBounds(bounds, 0.50f, 0.35f, -0.22f),
                StrategyBuildTool.Kiln => LerpBounds(bounds, 0.50f, 0.34f, -0.22f),
                StrategyBuildTool.Forge => new Vector3(
                    bounds.center.x + 0.10f,
                    bounds.min.y + bounds.size.y * 0.38f,
                    -0.22f),
                StrategyBuildTool.StorageYard => LerpBounds(bounds, 0.26f, 0.42f, -0.22f),
                StrategyBuildTool.Granary => LerpBounds(bounds, 0.50f, 0.46f, -0.22f),
                StrategyBuildTool.Bridge => new Vector3(bounds.center.x, bounds.center.y, -0.22f),
                _ => LerpBounds(bounds, 0.50f, 0.42f, -0.22f)
            };
        }

        public static Vector3 GetTorchAnchorWorld(StrategyBuildTool tool, Bounds bounds)
        {
            return tool switch
            {
                StrategyBuildTool.House => LerpBounds(bounds, -0.16f, 0.30f),
                StrategyBuildTool.LumberjackCamp => LerpBounds(bounds, 1.15f, 0.30f),
                StrategyBuildTool.StonecutterCamp => LerpBounds(bounds, 1.14f, 0.28f),
                StrategyBuildTool.Sawmill => LerpBounds(bounds, -0.16f, 0.34f),
                StrategyBuildTool.Mine => LerpBounds(bounds, -0.14f, 0.30f),
                StrategyBuildTool.CoalPit => LerpBounds(bounds, 1.14f, 0.32f),
                StrategyBuildTool.ClayPit => LerpBounds(bounds, -0.15f, 0.31f),
                StrategyBuildTool.Kiln => LerpBounds(bounds, 1.14f, 0.31f),
                StrategyBuildTool.Forge => LerpBounds(bounds, 1.16f, 0.34f),
                StrategyBuildTool.HunterCamp => LerpBounds(bounds, -0.16f, 0.31f),
                StrategyBuildTool.FisherHut => LerpBounds(bounds, 1.15f, 0.30f),
                StrategyBuildTool.ForagerCamp => StrategyForagerCampVisualProfile.GetTorchAnchorWorld(bounds),
                StrategyBuildTool.ChickenCoop => new Vector3(
                    bounds.center.x + 0.95f,
                    bounds.min.y + 1.05f,
                    -0.20f),
                StrategyBuildTool.StorageYard => LerpBounds(bounds, -0.15f, 0.33f),
                StrategyBuildTool.Granary => LerpBounds(bounds, 1.15f, 0.32f),
                StrategyBuildTool.TradingPost => new Vector3(
                    bounds.center.x + 1.08f,
                    bounds.min.y + 0.62f,
                    -0.20f),
                StrategyBuildTool.StarterCaravanCart => new Vector3(
                    bounds.center.x - 1.00f,
                    bounds.min.y + 0.72f,
                    -0.20f),
                _ => LerpBounds(bounds, 1.14f, 0.30f)
            };
        }

        private static Vector3 GetSawmillStockAnchorWorld(StrategyResourceType resource, Bounds bounds)
        {
            return resource == StrategyResourceType.Logs
                ? new Vector3(bounds.min.x + 0.46f, bounds.min.y + 0.36f, -0.14f)
                : new Vector3(bounds.max.x - 0.42f, bounds.min.y + 0.34f, -0.13f);
        }

        private static Vector3 GetKilnStockAnchorWorld(StrategyResourceType resource, Bounds bounds)
        {
            return resource switch
            {
                StrategyResourceType.Clay => new Vector3(bounds.min.x + 0.30f, bounds.min.y + 0.33f, -0.14f),
                StrategyResourceType.Coal => new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.31f, -0.14f),
                _ => new Vector3(bounds.max.x - 0.58f, bounds.min.y + 0.52f, -0.13f)
            };
        }

        private static Vector3 GetForgeStockAnchorWorld(StrategyResourceType resource, Bounds bounds)
        {
            return resource switch
            {
                StrategyResourceType.Iron => new Vector3(bounds.min.x + 0.32f, bounds.min.y + 0.32f, -0.14f),
                StrategyResourceType.Coal => new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.31f, -0.14f),
                StrategyResourceType.Logs => new Vector3(bounds.min.x + 0.24f, bounds.min.y + 0.52f, -0.13f),
                _ => new Vector3(bounds.max.x - 0.56f, bounds.min.y + 0.54f, -0.13f)
            };
        }

        private static Vector3 GetStorageStockAnchorWorld(StrategyResourceType resource, Bounds bounds)
        {
            return resource switch
            {
                StrategyResourceType.Logs => new Vector3(bounds.center.x + 0.28f, bounds.min.y + 0.45f, -0.16f),
                StrategyResourceType.Stone => new Vector3(bounds.center.x - 0.86f, bounds.min.y + 0.37f, -0.155f),
                StrategyResourceType.Iron => new Vector3(bounds.center.x + 0.82f, bounds.min.y + 0.34f, -0.15f),
                StrategyResourceType.Coal => new Vector3(bounds.center.x + 0.18f, bounds.min.y + 0.28f, -0.145f),
                StrategyResourceType.Clay => new Vector3(bounds.max.x - 0.16f, bounds.min.y + 0.30f, -0.145f),
                StrategyResourceType.Pottery => new Vector3(bounds.max.x - 0.34f, bounds.min.y + 0.58f, -0.146f),
                StrategyResourceType.Planks => new Vector3(bounds.max.x - 0.56f, bounds.min.y + 0.39f, -0.148f),
                StrategyResourceType.Tools => new Vector3(bounds.center.x + 0.64f, bounds.min.y + 0.60f, -0.147f),
                _ => new Vector3(bounds.center.x, bounds.min.y + 0.32f, -0.13f)
            };
        }

        private static Vector3 GetGranaryStockAnchorWorld(StrategyResourceType resource, Bounds bounds)
        {
            return resource switch
            {
                StrategyResourceType.Game => new Vector3(bounds.min.x + 0.42f, bounds.min.y + 0.35f, -0.13f),
                StrategyResourceType.Eggs => new Vector3(bounds.center.x, bounds.min.y + 0.32f, -0.13f),
                _ => new Vector3(bounds.max.x - 0.42f, bounds.min.y + 0.37f, -0.13f)
            };
        }

        private static Vector3 AtMaxX(Bounds bounds, float inset, float yFromMin, float z)
        {
            return new Vector3(bounds.max.x - inset, bounds.min.y + yFromMin, z);
        }

        private static Vector3 LerpBounds(Bounds bounds, float x, float y, float z = -0.20f)
        {
            return new Vector3(
                bounds.min.x + bounds.size.x * x,
                bounds.min.y + bounds.size.y * y,
                z);
        }
    }
}
