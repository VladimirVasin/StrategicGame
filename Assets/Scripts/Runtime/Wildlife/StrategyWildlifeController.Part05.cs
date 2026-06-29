using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private bool IsTooCloseToOtherRabbits(Vector2Int cell)
        {
            if (map == null)
            {
                return true;
            }

            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent agent = rabbits[i];
                if (agent != null && Vector3.Distance(agent.transform.position, world) < 0.42f)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateFishBreeding(float elapsedSeconds)
        {
            RemoveMissingFish();
            if (fish.Count >= MaxFishPopulation)
            {
                return;
            }

            for (int i = 0; i < fish.Count && fish.Count < MaxFishPopulation; i++)
            {
                StrategyFishAgent adult = fish[i];
                if (adult == null || !adult.IsLakeFish || !adult.IsAdult)
                {
                    continue;
                }

                float cooldown = GetFishBreedCooldown(adult) - elapsedSeconds;
                if (cooldown > 0f)
                {
                    fishBreedCooldowns[adult] = cooldown;
                    continue;
                }

                if (!TryGetLakeRegion(adult.WaterRegionId, out FishWaterRegion region))
                {
                    fishBreedCooldowns[adult] = Random.Range(FishFailedBreedRetryMin, FishFailedBreedRetryMax);
                    continue;
                }

                int shoalPopulation = CountLivingLakeFishInShoal(adult.ShoalId);
                if (shoalPopulation >= MaxFishPerShoal)
                {
                    fishBreedCooldowns[adult] = Random.Range(FishFailedBreedRetryMin, FishFailedBreedRetryMax);
                    continue;
                }

                int regionPopulation = CountFishInLakeRegion(region.Id);
                if (regionPopulation >= region.Capacity)
                {
                    fishBreedCooldowns[adult] = Random.Range(FishFailedBreedRetryMin, FishFailedBreedRetryMax);
                    if (ShouldLogFishLakeBirthBlocked(region.Id))
                    {
                        StrategyDebugLogger.Info(
                            "Wildlife",
                            "FishLakeBirthBlocked",
                            StrategyDebugLogger.F("reason", "lake cap"),
                            StrategyDebugLogger.F("region", region.Id),
                            StrategyDebugLogger.F("regionPopulation", regionPopulation),
                            StrategyDebugLogger.F("regionCapacity", region.Capacity),
                            StrategyDebugLogger.F("shoal", adult.ShoalId),
                            StrategyDebugLogger.F("throttleSeconds", FishLakeBirthBlockedLogIntervalSeconds));
                    }

                    continue;
                }

                if (!adult.CanBreed
                    || !HasAdultFishMateNear(adult)
                    || !TryFindFishBirthCell(adult, out Vector2Int birthCell))
                {
                    fishBreedCooldowns[adult] = Random.Range(FishFailedBreedRetryMin, FishFailedBreedRetryMax);
                    continue;
                }

                SpawnFish(
                    adult.Species,
                    adult.ShoalId,
                    adult.HomeCell,
                    birthCell,
                    StrategyFishLifeStage.Fry,
                    0f,
                    StrategyFishHabitatKind.Lake,
                    region.Id);
                fishBreedCooldowns[adult] = Random.Range(FishBreedCooldownMin, FishBreedCooldownMax);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "FishPopulationChanged",
                    StrategyDebugLogger.F("count", fish.Count),
                    StrategyDebugLogger.F("cap", MaxFishPopulation),
                    StrategyDebugLogger.F("lakeRegion", region.Id),
                    StrategyDebugLogger.F("lakeRegionPopulation", CountFishInLakeRegion(region.Id)),
                    StrategyDebugLogger.F("lakeRegionCapacity", region.Capacity),
                    StrategyDebugLogger.F("shoal", adult.ShoalId),
                    StrategyDebugLogger.F("shoalPopulation", shoalPopulation + 1),
                    StrategyDebugLogger.F("shoalCap", MaxFishPerShoal));
            }
        }

        private int CountLivingLakeFishInShoal(int shoalId)
        {
            int count = 0;
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null
                    && agent.IsLakeFish
                    && !agent.IsCaught
                    && agent.ShoalId == shoalId)
                {
                    count++;
                }
            }

            return count;
        }

        private float GetFishBreedCooldown(StrategyFishAgent adult)
        {
            if (adult == null)
            {
                return FishBreedCooldownMax;
            }

            if (!fishBreedCooldowns.TryGetValue(adult, out float cooldown))
            {
                cooldown = Random.Range(FishBreedCooldownMin * 0.65f, FishBreedCooldownMax);
                fishBreedCooldowns[adult] = cooldown;
            }

            return cooldown;
        }

        private bool ShouldLogFishLakeBirthBlocked(int regionId)
        {
            float now = Time.realtimeSinceStartup;
            if (fishLakeBirthBlockedLogTimes.TryGetValue(regionId, out float nextLogTime)
                && now < nextLogTime)
            {
                return false;
            }

            fishLakeBirthBlockedLogTimes[regionId] = now + FishLakeBirthBlockedLogIntervalSeconds;
            return true;
        }

        private bool HasAdultFishMateNear(StrategyFishAgent adult)
        {
            if (adult == null)
            {
                return false;
            }

            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent candidate = fish[i];
                if (candidate == null
                    || candidate == adult
                    || candidate.ShoalId != adult.ShoalId
                    || candidate.WaterRegionId != adult.WaterRegionId
                    || candidate.Species != adult.Species
                    || !candidate.IsAdult)
                {
                    continue;
                }

                float distance = Vector3.Distance(candidate.transform.position, adult.transform.position);
                if (distance <= FishMateSearchRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindFishBirthCell(StrategyFishAgent parent, out Vector2Int cell)
        {
            cell = default;
            if (parent == null || !parent.TryGetCurrentCell(out Vector2Int parentCell))
            {
                return false;
            }

            for (int radius = 1; radius <= FishBirthCellSearchRadius; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = parentCell + new Vector2Int(x, y);
                        if (IsFishBirthCellCandidate(candidate, parent))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            return false;
        }

        private bool IsFishBirthCellCandidate(Vector2Int cell, StrategyFishAgent parent)
        {
            return parent != null
                && IsLakeFishSpawnCandidate(cell, parent.WaterRegionId)
                && Vector2Int.Distance(cell, parent.HomeCell) <= parent.HomeRadius + 1
                && !IsTooCloseToOtherFish(cell);
        }

        private bool IsTooCloseToOtherFish(Vector2Int cell)
        {
            if (map == null)
            {
                return true;
            }

            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null && Vector3.Distance(agent.transform.position, world) < 0.36f)
                {
                    return true;
                }
            }

            return false;
        }

        private StrategyDeerSex PickHerdSex(int slot, bool spawnedMale, bool spawnedFemale)
        {
            if (slot == 0 && !spawnedMale)
            {
                return StrategyDeerSex.Male;
            }

            if (slot == 1 && !spawnedFemale)
            {
                return StrategyDeerSex.Female;
            }

            return Random.value < 0.34f ? StrategyDeerSex.Male : StrategyDeerSex.Female;
        }

        private StrategyRabbitSex PickRabbitSex(int slot, bool spawnedMale, bool spawnedFemale)
        {
            if (slot == 0 && !spawnedMale)
            {
                return StrategyRabbitSex.Male;
            }

            if (slot == 1 && !spawnedFemale)
            {
                return StrategyRabbitSex.Female;
            }

            return Random.value < 0.42f ? StrategyRabbitSex.Male : StrategyRabbitSex.Female;
        }

        private bool TryFindWolfSpawnCell(
            Vector2Int packCenter,
            int pack,
            int slot,
            HashSet<Vector2Int> usedCells,
            out Vector2Int cell)
        {
            cell = default;
            for (int radius = 0; radius <= 5; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        Vector2Int candidate = packCenter + new Vector2Int(x, y);
                        if (!usedCells.Contains(candidate)
                            && IsWolfRoamCandidate(candidate)
                            && IsHiddenNearSettlementSpawnCell(candidate, WildlifeSettlementSpawnKind.Wolf))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                while (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, pack, slot, radius, candidates.Count) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            return false;
        }

        private bool IsWolfPackCenterCandidate(Vector2Int cell)
        {
            return IsWolfRoamCandidate(cell)
                && IsHiddenNearSettlementSpawnCell(cell, WildlifeSettlementSpawnKind.Wolf)
                && GetWolfTerrainScore(cell) > 0f
                && CountWalkableNeighbors(cell, 3) >= 8;
        }

        private bool IsWolfRoamCandidate(Vector2Int cell)
        {
            if (map == null
                || !IsLandWildlifeTravelCell(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind == CityMapCellKind.Water
                || mapCell.Kind == CityMapCellKind.Shore)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < WolfCampAvoidRadius)
            {
                return false;
            }

            return mapCell.Kind == CityMapCellKind.Forest
                || mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Dirt;
        }

        private bool TryFindRabbitGroupCenter(int group, HashSet<Vector2Int> usedCells, out Vector2Int cell)
        {
            if (hasCampCell
                && group < NearCampRabbitGroups
                && TryFindRabbitGroupCenterNearCamp(group, usedCells, out cell))
            {
                return true;
            }

            bool found = TryFindRabbitGroupCenterMapWide(group, usedCells, out cell);
            if (found)
            {
                return true;
            }

            if (hasCampCell
                && group >= NearCampRabbitGroups
                && TryFindRabbitGroupCenterNearCamp(group, usedCells, out cell))
            {
                return true;
            }

            if (hasCampCell)
            {
                StrategyDebugLogger.Warn(
                    "Wildlife",
                    "RabbitGroupSpawnFailed",
                    StrategyDebugLogger.F("group", group),
                    StrategyDebugLogger.F("campCell", campCell),
                    StrategyDebugLogger.F("minDistance", RabbitCampMinDistance),
                    StrategyDebugLogger.F("maxDistance", RabbitCampMaxDistance));
            }

            return false;
        }

        private bool TryFindRabbitGroupCenterNearCamp(int group, HashSet<Vector2Int> usedCells, out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int radius = RabbitCampMinDistance; radius <= RabbitGroupCenterMaxCampDistance; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        if (usedCells.Contains(candidate) || !IsRabbitGroupCenterCandidate(candidate))
                        {
                            continue;
                        }

                        float distance = Vector2Int.Distance(candidate, campCell);
                        float score = GetRabbitGroupCenterScore(candidate)
                            + Mathf.Clamp(RabbitGroupCenterMaxCampDistance - distance, 0f, RabbitGroupCenterMaxCampDistance) * 0.08f
                            - GetUsedCellSpacingPenalty(candidate, usedCells, 7, 0.28f)
                            + Hash01(map.ActiveSeed, group, candidate.x, candidate.y) * 0.15f;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCell = candidate;
                            found = true;
                        }
                    }
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }
    }
}
