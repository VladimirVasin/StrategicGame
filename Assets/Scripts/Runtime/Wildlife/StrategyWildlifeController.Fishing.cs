using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const int FisherSupportedShoalIdBase = 20000;

        public int EnsureCatchableFishNearFisherHut(Vector2Int center, int radius)
        {
            if (map == null
                || StrategySeasonalSurfaceController.IsWaterFrozenForGameplay
                || CountCatchableFish(center, radius) > 0)
            {
                return 0;
            }

            RemoveMissingFish();
            if (fish.Count >= MaxFishPopulation)
            {
                return 0;
            }

            FishWaterRegion bestRegion = null;
            Vector2Int bestCell = default;
            float bestSqr = float.MaxValue;
            float radiusSqr = radius * radius;
            for (int i = 0; i < lakeFishRegions.Count; i++)
            {
                FishWaterRegion region = lakeFishRegions[i];
                if (region == null || CountFishInLakeRegion(region.Id) >= region.Capacity)
                {
                    continue;
                }

                for (int j = 0; j < region.Cells.Count; j++)
                {
                    Vector2Int candidate = region.Cells[j];
                    float sqr = (candidate - center).sqrMagnitude;
                    if (sqr > radiusSqr || sqr >= bestSqr || !HasFishingShoreCell(candidate))
                    {
                        continue;
                    }

                    bestRegion = region;
                    bestCell = candidate;
                    bestSqr = sqr;
                }
            }

            if (bestRegion == null)
            {
                StrategyDebugLogger.Warn(
                    "Fishing",
                    "FisherHutFishSupportSkipped",
                    StrategyDebugLogger.F("hutOrigin", center),
                    StrategyDebugLogger.F("radius", radius),
                    StrategyDebugLogger.F("reason", "no_nearby_lake_with_shore"));
                return 0;
            }

            int regionRoom = bestRegion.Capacity - CountFishInLakeRegion(bestRegion.Id);
            int globalRoom = MaxFishPopulation - fish.Count;
            int spawnCount = Mathf.Min(2, regionRoom, globalRoom);
            int shoalId = FisherSupportedShoalIdBase + bestRegion.Id;
            StrategyFishSpecies species = PickFishSpecies(shoalId, bestCell);
            int spawned = 0;
            for (int i = 0; i < bestRegion.Cells.Count && spawned < spawnCount; i++)
            {
                Vector2Int candidate = bestRegion.Cells[(i + Mathf.Abs(shoalId)) % bestRegion.Cells.Count];
                if ((candidate - bestCell).sqrMagnitude > 9f || IsFishCellOccupied(candidate))
                {
                    continue;
                }

                SpawnFish(
                    species,
                    shoalId,
                    bestCell,
                    candidate,
                    StrategyFishLifeStage.Adult,
                    0f,
                    StrategyFishHabitatKind.Lake,
                    bestRegion.Id);
                spawned++;
            }

            StrategyDebugLogger.Info(
                "Fishing",
                "FisherHutFishSupportCompleted",
                StrategyDebugLogger.F("hutOrigin", center),
                StrategyDebugLogger.F("region", bestRegion.Id),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("spawned", spawned),
                StrategyDebugLogger.F("regionPopulation", CountFishInLakeRegion(bestRegion.Id)),
                StrategyDebugLogger.F("regionCapacity", bestRegion.Capacity));
            return spawned;
        }

        public bool TryReserveFishForFishing(Vector2Int center, int radius, object owner, out StrategyFishAgent reservedFish)
        {
            return TryReserveFishForFishing(center, radius, owner, null, out reservedFish);
        }

        public bool TryReserveFishForFishing(
            Vector2Int center,
            int radius,
            object owner,
            System.Func<StrategyFishAgent, bool> isCandidateReachable,
            out StrategyFishAgent reservedFish)
        {
            reservedFish = null;
            if (owner == null || map == null)
            {
                return false;
            }

            if (StrategySeasonalSurfaceController.IsWaterFrozenForGameplay)
            {
                return false;
            }

            RemoveMissingFish();
            float bestSqr = float.MaxValue;
            float radiusSqr = radius * radius;
            StrategyFishAgent best = null;
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent candidate = fish[i];
                if (candidate == null || !candidate.CanBeFished || !candidate.TryGetCurrentCell(out Vector2Int cell))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr > radiusSqr
                    || sqr >= bestSqr
                    || isCandidateReachable != null && !isCandidateReachable(candidate))
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null || !best.TryReserveForFishing(owner))
            {
                return false;
            }

            reservedFish = best;
            Vector2Int fishCell = reservedFish.TryGetCurrentCell(out Vector2Int reservedCell)
                ? reservedCell
                : Vector2Int.zero;
            StrategyDebugLogger.Info(
                "Wildlife",
                "FishReservedForFishing",
                StrategyDebugLogger.F("hutCenter", center),
                StrategyDebugLogger.F("radius", radius),
                StrategyDebugLogger.F("fishCell", fishCell),
                StrategyDebugLogger.F("fishWorld", reservedFish.transform.position));
            return true;
        }

        private bool HasFishingShoreCell(Vector2Int waterCell)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int shore = waterCell + CardinalDirections[i];
                if (map.IsCellWalkable(shore)
                    && map.TryGetCell(shore.x, shore.y, out CityMapCell cell)
                    && cell.Kind != CityMapCellKind.Water)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFishCellOccupied(Vector2Int cell)
        {
            for (int i = 0; i < fish.Count; i++)
            {
                if (fish[i] != null
                    && !fish[i].IsCaught
                    && fish[i].TryGetCurrentCell(out Vector2Int occupied)
                    && occupied == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
