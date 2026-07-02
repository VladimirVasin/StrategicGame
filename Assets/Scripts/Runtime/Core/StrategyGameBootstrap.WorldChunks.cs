using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
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
