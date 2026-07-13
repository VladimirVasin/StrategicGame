using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigurePersistence(
            StrategyGameContext context,
            CityMapController map,
            StrategyBuildPlacementController placement,
            StrategyPopulationController population,
            StrategyInputRouter inputRouter)
        {
            StrategySaveSystem saveSystem = context.GetOrCreate<StrategySaveSystem>("Strategy Save System");
            saveSystem.SetInputRouter(inputRouter);
            saveSystem.Configure(map, placement, population);
            StrategyDebugLogger.Info("Bootstrap", "PersistenceReady", StrategyDebugLogger.F("savePath", StrategySaveSystem.SavePath));
        }
    }
}
