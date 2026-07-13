using Unity.Profiling;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyPerformanceMarkers
    {
        public static readonly ProfilerMarker NavigationCompute =
            new("Strategy.Navigation.ComputeAndCache");

        public static readonly ProfilerMarker NavigationAStar =
            new("Strategy.Navigation.AStar");

        public static readonly ProfilerMarker TerrainPaint =
            new("Strategy.Map.TerrainPaint");

        public static readonly ProfilerMarker ResidentTaskSelection =
            new("Strategy.Resident.TaskSelection");

        public static readonly ProfilerMarker AutoWorkforceTick =
            new("Strategy.AutoWorkforce.Tick");

        public static readonly ProfilerMarker CinematicLightsScan =
            new("Strategy.CinematicLights.ScanEmitters");

        public static readonly ProfilerMarker CinematicLightsRefreshLods =
            new("Strategy.CinematicLights.RefreshLods");
    }
}
