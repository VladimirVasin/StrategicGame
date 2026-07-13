using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigurePerformanceDiagnostics(
            StrategyGameContext context,
            CityMapController map,
            StrategyPopulationController population,
            StrategyWildlifeController wildlife,
            StrategyWeatherController weather,
            StrategyTimeScaleController timeScale)
        {
            StrategyPerformanceDiagnostics diagnostics = context.GetOrCreate<StrategyPerformanceDiagnostics>("Strategy Performance Diagnostics");
            diagnostics.Configure(map, population, wildlife, weather, timeScale);
            StrategyDebugLogger.Info("Bootstrap", "PerformanceDiagnosticsReady");
        }
    }
}
