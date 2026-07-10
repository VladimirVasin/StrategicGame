using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigurePerformanceDiagnostics(
            CityMapController map,
            StrategyPopulationController population,
            StrategyWildlifeController wildlife,
            StrategyWeatherController weather,
            StrategyTimeScaleController timeScale)
        {
            StrategyPerformanceDiagnostics diagnostics = Object.FindAnyObjectByType<StrategyPerformanceDiagnostics>();
            if (diagnostics == null)
            {
                GameObject diagnosticsObject = new GameObject("Strategy Performance Diagnostics");
                diagnostics = diagnosticsObject.AddComponent<StrategyPerformanceDiagnostics>();
            }

            diagnostics.Configure(map, population, wildlife, weather, timeScale);
            StrategyDebugLogger.Info("Bootstrap", "PerformanceDiagnosticsReady");
        }
    }
}
