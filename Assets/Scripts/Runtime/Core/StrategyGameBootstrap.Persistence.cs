using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigurePersistence(
            CityMapController map,
            StrategyBuildPlacementController placement,
            StrategyPopulationController population)
        {
            StrategySaveSystem saveSystem = Object.FindAnyObjectByType<StrategySaveSystem>();
            if (saveSystem == null)
            {
                saveSystem = new GameObject("Strategy Save System").AddComponent<StrategySaveSystem>();
            }

            saveSystem.Configure(map, placement, population);
            StrategyDebugLogger.Info("Bootstrap", "PersistenceReady", StrategyDebugLogger.F("savePath", StrategySaveSystem.SavePath));
        }
    }
}
