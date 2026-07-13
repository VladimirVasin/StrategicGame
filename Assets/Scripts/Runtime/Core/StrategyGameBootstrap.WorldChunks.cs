using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static CityMapController ConfigureDebugLoggingAndMap(
            StrategyGameContext context,
            out bool requiresGeneration)
        {
            StrategyDebugLogger debugLogger = StrategyDebugLogger.Active;
            if (debugLogger == null)
            {
                GameObject debugLoggerObject = new GameObject("Strategy Debug Logger");
                debugLogger = debugLoggerObject.AddComponent<StrategyDebugLogger>();
            }

            context.Register(debugLogger);
            debugLogger.Configure();
            StrategyDebugLogger.Info("Bootstrap", "Start");

            CityMapController map = context.GetOrCreate<CityMapController>("City Map");
            requiresGeneration = !map.IsGenerated;
            if (requiresGeneration)
            {
                map.CancelIncrementalGeneration();
            }
            else
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (map.gameObject.scene != activeScene && activeScene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(map.gameObject, activeScene);
                }

                map.SetPresentationVisible(true);
                StrategyDebugLogger.Info(
                    "Bootstrap",
                    "PreloadedMapAccepted",
                    StrategyDebugLogger.F("seed", map.ActiveSeed));
            }

            return map;
        }

        private static void ConfigureWorldChunks(
            StrategyGameContext context,
            CityMapController map,
            StrategyPopulationController population,
            Camera mainCamera)
        {
            StrategyWorldChunkRegistry chunks = context.GetOrCreate<StrategyWorldChunkRegistry>("Strategy World Chunks");
            chunks.Configure(map, population, mainCamera);
            StrategyDebugLogger.Info(
                "Bootstrap",
                "WorldChunksReady",
                StrategyDebugLogger.F("chunkSize", StrategyWorldChunkRegistry.ChunkSize),
                StrategyDebugLogger.F("chunks", chunks.ChunkCount));
        }
    }
}
