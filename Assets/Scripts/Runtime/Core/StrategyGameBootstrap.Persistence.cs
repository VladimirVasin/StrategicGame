using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static StrategySaveSystem ConfigurePersistence(
            StrategyGameContext context,
            CityMapController map,
            StrategyBuildPlacementController placement,
            StrategyPopulationController population,
            StrategyCityInventory cityInventory,
            StrategyInputRouter inputRouter,
            StrategyFoundingStartState foundingStart)
        {
            StrategySaveSystem saveSystem = context.GetOrCreate<StrategySaveSystem>("Strategy Save System");
            saveSystem.SetInputRouter(inputRouter);
            saveSystem.SetCityInventory(cityInventory);
            if (foundingStart != null)
            {
                saveSystem.SetFoundingStartData(foundingStart.CreateSaveData());
            }

            saveSystem.Configure(map, placement, population);
            StrategyDebugLogger.Info("Bootstrap", "PersistenceReady", StrategyDebugLogger.F("savePath", StrategySaveSystem.SavePath));
            return saveSystem;
        }
    }
}
