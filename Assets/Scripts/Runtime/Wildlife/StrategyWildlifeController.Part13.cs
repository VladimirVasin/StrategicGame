using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const float HunterCampSupportSpawnSecondsMin = 18f;
        private const float HunterCampSupportSpawnSecondsMax = 34f;
        private const int HunterSupportRabbitTarget = 3;
        private const int HunterSupportRabbitSoftCap = 6;
        private const int HunterSupportDeerTarget = 1;
        private const int HunterSupportDeerSoftCap = 2;
        private const int HunterSupportMinDistance = 7;
        private const int HunterSupportRabbitDensityRadius = 5;
        private const int HunterSupportRabbitDensityMax = 3;
        private const int HunterSupportDeerDensityRadius = 7;
        private const int HunterSupportDeerDensityMax = 2;
        private const int HunterSupportRabbitGroupIdBase = 20000;
        private const int HunterSupportDeerHerdIdBase = 24000;

        private float hunterCampSupportSpawnTimer;
        private int nextHunterSupportRabbitGroupId;
        private int nextHunterSupportDeerHerdId;

        private void ResetHunterCampSupportSpawning()
        {
            hunterCampSupportSpawnTimer = Random.Range(
                HunterCampSupportSpawnSecondsMin,
                HunterCampSupportSpawnSecondsMax);
            nextHunterSupportRabbitGroupId = HunterSupportRabbitGroupIdBase
                + (map != null ? Hash(map.ActiveSeed, 211, 347, 563, 719) % 997 : 0);
            nextHunterSupportDeerHerdId = HunterSupportDeerHerdIdBase
                + (map != null ? Hash(map.ActiveSeed, 227, 359, 587, 733) % 997 : 0);
        }

        private void UpdateHunterCampSupportSpawning(float elapsedSeconds)
        {
            if (map == null || wildlifeRoot == null)
            {
                return;
            }

            hunterCampSupportSpawnTimer -= elapsedSeconds;
            if (hunterCampSupportSpawnTimer > 0f)
            {
                return;
            }

            hunterCampSupportSpawnTimer = Random.Range(
                HunterCampSupportSpawnSecondsMin,
                HunterCampSupportSpawnSecondsMax);
            if (!TryFindHunterCampNeedingWildlife(out StrategyHunterCamp camp, out bool spawnDeer, out int localCount))
            {
                return;
            }

            bool spawned = spawnDeer
                ? TrySpawnHunterSupportDeer(camp)
                : TrySpawnHunterSupportRabbit(camp);
            if (!spawned)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                spawnDeer ? "HunterCampSupportDeerSpawned" : "HunterCampSupportRabbitSpawned",
                StrategyDebugLogger.F("campOrigin", camp.Origin),
                StrategyDebugLogger.F("localCountBefore", localCount),
                StrategyDebugLogger.F("minDistance", HunterSupportMinDistance),
                StrategyDebugLogger.F("maxDistance", StrategyHunterCamp.WorkRadius));
        }

        private bool TryFindHunterCampNeedingWildlife(
            out StrategyHunterCamp camp,
            out bool spawnDeer,
            out int localCount)
        {
            camp = null;
            spawnDeer = false;
            localCount = 0;
            RemoveMissingRabbits();
            RemoveMissingDeer();
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            float bestScore = float.MaxValue;

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null
                    || !building.TryGetComponent(out StrategyHunterCamp candidate)
                    || candidate == null
                    || !candidate.HasStorageSpace)
                {
                    continue;
                }

                Vector2Int origin = candidate.Origin;
                int rabbitsNear = CountHuntableRabbits(origin, StrategyHunterCamp.WorkRadius);
                bool candidateWantsRabbit = rabbitsNear < HunterSupportRabbitTarget
                    && CountLivingRabbitsForWolfControl() < MaxRabbitPopulation
                    && CountLivingRabbitsNear(origin, StrategyHunterCamp.WorkRadius) < HunterSupportRabbitSoftCap;
                int deerNear = candidate.CanHuntDeer
                    ? CountHuntableDeer(origin, StrategyHunterCamp.WorkRadius)
                    : HunterSupportDeerTarget;
                bool candidateWantsDeer = candidate.CanHuntDeer
                    && deerNear < HunterSupportDeerTarget
                    && CountLivingDeerForWolfControl() < MaxDeerPopulation
                    && CountLivingDeerNear(origin, StrategyHunterCamp.WorkRadius) < HunterSupportDeerSoftCap;
                if (!candidateWantsRabbit && !candidateWantsDeer)
                {
                    continue;
                }

                bool chooseDeer = !candidateWantsRabbit && candidateWantsDeer;
                int count = chooseDeer ? deerNear : rabbitsNear;
                float score = count * 100f
                    + Hash01(map.ActiveSeed, origin.x, origin.y, i + nextHunterSupportRabbitGroupId) * 0.65f;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                camp = candidate;
                spawnDeer = chooseDeer;
                localCount = count;
            }

            return camp != null;
        }

        private bool TrySpawnHunterSupportRabbit(StrategyHunterCamp camp)
        {
            if (camp == null || rabbits.Count >= MaxRabbitPopulation)
            {
                return false;
            }

            if (!TryFindHunterSupportSpawnCell(camp.Origin, false, out Vector2Int spawnCell))
            {
                return false;
            }

            int groupId = nextHunterSupportRabbitGroupId++;
            StrategyRabbitSex sex = Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, groupId, 991) % 100 < 44
                ? StrategyRabbitSex.Female
                : StrategyRabbitSex.Male;
            SpawnRabbit(sex, groupId, spawnCell, spawnCell, StrategyRabbitLifeStage.Adult);
            return true;
        }

        private bool TrySpawnHunterSupportDeer(StrategyHunterCamp camp)
        {
            if (camp == null || deer.Count >= MaxDeerPopulation)
            {
                return false;
            }

            if (!TryFindHunterSupportSpawnCell(camp.Origin, true, out Vector2Int spawnCell))
            {
                return false;
            }

            int herdId = nextHunterSupportDeerHerdId++;
            StrategyDeerSex sex = Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, herdId, 997) % 100 < 34
                ? StrategyDeerSex.Male
                : StrategyDeerSex.Female;
            SpawnDeer(sex, herdId, spawnCell, spawnCell, StrategyDeerLifeStage.Adult);
            return true;
        }

        private bool TryFindHunterSupportSpawnCell(Vector2Int campOrigin, bool deerSpawn, out Vector2Int cell)
        {
            cell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int radius = HunterSupportMinDistance; radius <= StrategyHunterCamp.WorkRadius; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campOrigin + new Vector2Int(x, y);
                        float distance = Vector2Int.Distance(candidate, campOrigin);
                        if (distance < HunterSupportMinDistance
                            || distance > StrategyHunterCamp.WorkRadius
                            || !IsHunterSupportSpawnCandidate(candidate, deerSpawn))
                        {
                            continue;
                        }

                        float score = GetHunterSupportSpawnScore(candidate, campOrigin, deerSpawn, distance);
                        if (score <= bestScore)
                        {
                            continue;
                        }

                        bestScore = score;
                        cell = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private bool IsHunterSupportSpawnCandidate(Vector2Int cell, bool deerSpawn)
        {
            if (deerSpawn)
            {
                return IsHerdSpawnCandidate(cell)
                    && !IsTooCloseToOtherDeer(cell)
                    && CountLivingDeerNear(cell, HunterSupportDeerDensityRadius) < HunterSupportDeerDensityMax;
            }

            return IsRabbitSpawnCandidate(cell)
                && !IsTooCloseToOtherRabbits(cell)
                && CountLivingRabbitsNear(cell, HunterSupportRabbitDensityRadius) < HunterSupportRabbitDensityMax;
        }

        private float GetHunterSupportSpawnScore(
            Vector2Int cell,
            Vector2Int campOrigin,
            bool deerSpawn,
            float distance)
        {
            float idealDistance = Mathf.Lerp(HunterSupportMinDistance, StrategyHunterCamp.WorkRadius, 0.62f);
            float distanceScore = -Mathf.Abs(distance - idealDistance) * 0.35f;
            float terrainScore = deerSpawn ? GetSpawnTerrainScore(cell) : GetRabbitSpawnTerrainScore(cell);
            float densityPenalty = deerSpawn
                ? CountLivingDeerNear(cell, HunterSupportDeerDensityRadius) * 2.25f
                : CountLivingRabbitsNear(cell, HunterSupportRabbitDensityRadius) * 1.65f;
            return terrainScore
                + CountWalkableNeighbors(cell, 2) * 0.12f
                + distanceScore
                - densityPenalty
                + Hash01(map.ActiveSeed, cell.x, cell.y, campOrigin.x + campOrigin.y) * 0.35f;
        }

        private int CountLivingRabbitsNear(Vector2Int center, int radius)
        {
            int radiusSqr = radius * radius;
            int count = 0;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent rabbit = rabbits[i];
                if (rabbit == null
                    || !rabbit.IsAlive
                    || rabbit.IsCarcass
                    || !rabbit.TryGetCurrentCell(out Vector2Int cell))
                {
                    continue;
                }

                Vector2Int delta = cell - center;
                if (delta.x * delta.x + delta.y * delta.y <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountLivingDeerNear(Vector2Int center, int radius)
        {
            int radiusSqr = radius * radius;
            int count = 0;
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent == null
                    || !agent.IsAlive
                    || agent.IsCarcass
                    || !agent.TryGetCurrentCell(out Vector2Int cell))
                {
                    continue;
                }

                Vector2Int delta = cell - center;
                if (delta.x * delta.x + delta.y * delta.y <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
