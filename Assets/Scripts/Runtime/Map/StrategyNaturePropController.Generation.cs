using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private const int IncrementalNaturePropsPerFrame = 48;
        private const int IncrementalNatureScansPerFrame = 384;

        public IEnumerator ConfigureIncremental(
            CityMapController mapController,
            StrategyWindController windController,
            StrategyForestryController forestryController,
            StrategyStoneResourceController stoneController,
            Vector2Int natureExcludedCenter,
            int natureExcludedRadius)
        {
            map = mapController;
            wind = windController;
            forestry = forestryController;
            stone = stoneController;
            iron = StrategyIronResourceController.Active;
            coal = StrategyCoalResourceController.Active;
            clay = StrategyClayResourceController.Active;
            excludedCenter = natureExcludedCenter;
            excludedRadius = Mathf.Max(0, natureExcludedRadius);
            hasExclusion = true;

            if (map == null)
            {
                yield break;
            }

            ResetNatureGeneration();
            EnsureStarterStoneDeposits();
            yield return null;
            EnsureMinimumStoneDeposits();
            yield return null;
            EnsureMinimumClayDeposits();
            yield return null;

            int spawnedThisFrame = 0;
            int scannedThisFrame = 0;
            int totalCells = map.Width * map.Height;
            for (int i = 0; i < totalCells && spawnedProps < MaxNatureProps; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(map.ActiveSeed, i, totalCells, 701);
                int x = cellIndex % map.Width;
                int y = cellIndex / map.Width;
                int previousCount = spawnedProps;
                if (!IsInsideExclusion(x, y) && map.TryGetCell(x, y, out CityMapCell cell))
                {
                    PlaceNatureForCell(cell);
                }

                scannedThisFrame++;
                spawnedThisFrame += spawnedProps - previousCount;
                if (spawnedThisFrame < IncrementalNaturePropsPerFrame
                    && scannedThisFrame < IncrementalNatureScansPerFrame)
                {
                    continue;
                }

                spawnedThisFrame = 0;
                scannedThisFrame = 0;
                yield return null;
            }

            LogNatureGeneration();
        }

        private void ResetNatureGeneration()
        {
            EnsurePropRoot();
            ClearProps();
            spawnedProps = 0;
            spawnedStoneDeposits = 0;
            spawnedBoulders = 0;
            spawnedRockClusters = 0;
            spawnedCliffs = 0;
            stoneBlockedCells = 0;
            spawnedIronDeposits = 0;
            spawnedIronStainedGround = 0;
            spawnedIronVeins = 0;
            spawnedCoalDeposits = 0;
            spawnedCoalDustGround = 0;
            spawnedCoalSeams = 0;
            spawnedClayDeposits = 0;
            spawnedClayPatches = 0;
            spawnedClayBanks = 0;
        }

        private void GenerateGuaranteedNature()
        {
            EnsureStarterStoneDeposits();
            EnsureMinimumStoneDeposits();
            EnsureMinimumClayDeposits();
        }

        private void LogNatureGeneration()
        {
            StrategyDebugLogger.Info(
                "Nature",
                "Generated",
                StrategyDebugLogger.F("props", spawnedProps),
                StrategyDebugLogger.F("max", MaxNatureProps),
                StrategyDebugLogger.F("stoneDeposits", spawnedStoneDeposits),
                StrategyDebugLogger.F("ironDeposits", spawnedIronDeposits),
                StrategyDebugLogger.F("coalDeposits", spawnedCoalDeposits),
                StrategyDebugLogger.F("clayDeposits", spawnedClayDeposits),
                StrategyDebugLogger.F("hasExclusion", hasExclusion),
                StrategyDebugLogger.F("excludedCenter", hasExclusion ? excludedCenter : Vector2Int.zero),
                StrategyDebugLogger.F("excludedRadius", hasExclusion ? excludedRadius : 0));
            StrategyDebugLogger.Info(
                "Stone",
                "Generated",
                StrategyDebugLogger.F("deposits", spawnedStoneDeposits),
                StrategyDebugLogger.F("boulders", spawnedBoulders),
                StrategyDebugLogger.F("rockClusters", spawnedRockClusters),
                StrategyDebugLogger.F("cliffs", spawnedCliffs),
                StrategyDebugLogger.F("blockedCells", stoneBlockedCells),
                StrategyDebugLogger.F("max", MaxStoneDeposits));
            StrategyDebugLogger.Info(
                "Iron",
                "Generated",
                StrategyDebugLogger.F("deposits", spawnedIronDeposits),
                StrategyDebugLogger.F("stainedGround", spawnedIronStainedGround),
                StrategyDebugLogger.F("veins", spawnedIronVeins));
            StrategyDebugLogger.Info(
                "Coal",
                "Generated",
                StrategyDebugLogger.F("deposits", spawnedCoalDeposits),
                StrategyDebugLogger.F("dustGround", spawnedCoalDustGround),
                StrategyDebugLogger.F("seams", spawnedCoalSeams));
            StrategyDebugLogger.Info(
                "Clay",
                "Generated",
                StrategyDebugLogger.F("deposits", spawnedClayDeposits),
                StrategyDebugLogger.F("patches", spawnedClayPatches),
                StrategyDebugLogger.F("banks", spawnedClayBanks),
                StrategyDebugLogger.F("max", MaxClayDeposits),
                StrategyDebugLogger.F("waterRadius", ClayWaterSearchRadius));
        }
    }
}
