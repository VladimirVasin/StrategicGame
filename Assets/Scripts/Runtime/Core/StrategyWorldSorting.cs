using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyWorldSorting
    {
        public const int TerrainOrder = 0;
        public const int WaterOverlayOrder = 1;
        public const int SeasonalGroundOverlayOrder = 2;
        public const int SeasonalIceOverlayOrder = 3;
        public const int TrailOverlayOrder = 4;
        public const int WeatherGroundOverlayOrder = 5;
        public const int BridgeDeckOrder = 6;
        public const int WeatherCloudShadowOrder = 26500;
        public const int DayNightOverlayOrder = 27000;
        public const int CinematicDepthOverlayOrder = 27350;
        public const int WeatherMistOverlayOrder = 27400;
        public const int WeatherRainOverlayOrder = 27600;
        public const int WeatherSnowOverlayOrder = 27620;
        public const int CinematicForegroundOverlayOrder = 27720;
        public const int CinematicScreenFlashOrder = 27800;
        public const int PreviewOrder = 28000;
        public const int FogOrder = 30000;

        private const int WorldBaseOrder = 20000;
        private const int UnitsPerWorldUnit = 100;
        private const int MinWorldOrder = 2;
        private const int MaxWorldOrder = 26000;

        public static int ForWorldY(float worldY, int offset = 0)
        {
            int order = WorldBaseOrder - Mathf.RoundToInt(worldY * UnitsPerWorldUnit) + offset;
            return Mathf.Clamp(order, MinWorldOrder, MaxWorldOrder);
        }

        public static int ForPosition(Vector3 worldPosition, int offset = 0)
        {
            return ForWorldY(worldPosition.y, offset);
        }

        public static void Apply(SpriteRenderer renderer, Vector3 worldAnchor, int offset = 0)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingOrder = ForPosition(worldAnchor, offset);
        }
    }
}
