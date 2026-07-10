using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static CityMapController ConfigureDebugLoggingAndMap()
        {
            StrategyDebugLogger debugLogger = Object.FindAnyObjectByType<StrategyDebugLogger>();
            if (debugLogger == null)
            {
                GameObject debugLoggerObject = new GameObject("Strategy Debug Logger");
                debugLogger = debugLoggerObject.AddComponent<StrategyDebugLogger>();
            }

            debugLogger.Configure();
            StrategyDebugLogger.Info("Bootstrap", "Start");

            CityMapController map = Object.FindAnyObjectByType<CityMapController>();
            if (map == null)
            {
                GameObject mapObject = new GameObject("City Map");
                map = mapObject.AddComponent<CityMapController>();
            }

            if (!map.IsGenerated)
            {
                map.CancelIncrementalGeneration();
                map.GenerateMap();
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

            StrategyDebugLogger.Info("Bootstrap", "MapReady", StrategyDebugLogger.F("bounds", map.WorldBounds));
            return map;
        }

        private static void ConfigureWorldChunks(
            CityMapController map,
            StrategyPopulationController population,
            Camera mainCamera)
        {
            StrategyWorldChunkRegistry chunks = Object.FindAnyObjectByType<StrategyWorldChunkRegistry>();
            if (chunks == null)
            {
                GameObject chunksObject = new GameObject("Strategy World Chunks");
                chunks = chunksObject.AddComponent<StrategyWorldChunkRegistry>();
            }

            chunks.Configure(map, population, mainCamera);
            StrategyDebugLogger.Info(
                "Bootstrap",
                "WorldChunksReady",
                StrategyDebugLogger.F("chunkSize", StrategyWorldChunkRegistry.ChunkSize),
                StrategyDebugLogger.F("chunks", chunks.ChunkCount));
        }
    }
}
