using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private void SpawnWolf(
            StrategyWolfPack pack,
            Vector2Int packCenter,
            Vector2Int spawnCell,
            int slot)
        {
            Vector3 spawnWorld = map.GetCellCenterWorld(spawnCell.x, spawnCell.y);
            Vector2 jitter = GetJitter(spawnCell.x, spawnCell.y, pack.PackId + 1699 + slot) * (map.CellSize * 0.20f);
            spawnWorld.x += jitter.x;
            spawnWorld.y += jitter.y;

            int variant = Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, pack.PackId, slot) % 4;
            GameObject wolfObject = new GameObject($"Wolf Pack {pack.PackId + 1}");
            wolfObject.transform.SetParent(wildlifeRoot, false);

            SpriteRenderer renderer = wolfObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyWolfSpriteFactory.GetIdleSprite(variant, Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, pack.PackId, 1667));
            renderer.color = Color.white;

            StrategyWolfAgent agent = wolfObject.AddComponent<StrategyWolfAgent>();
            agent.Configure(
                map,
                population,
                this,
                pack,
                packCenter,
                WolfHomeRadius,
                spawnWorld,
                renderer,
                variant);
            pack.AddMember(agent);
            wolves.Add(agent);

            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfSpawned",
                StrategyDebugLogger.F("pack", pack.PackId),
                StrategyDebugLogger.F("variant", variant),
                StrategyDebugLogger.F("cell", spawnCell),
                StrategyDebugLogger.F("home", packCenter),
                StrategyDebugLogger.F("world", spawnWorld));
        }

        private void SpawnFish(
            StrategyFishSpecies species,
            int shoalId,
            Vector2Int shoalCenter,
            Vector2Int spawnCell,
            StrategyFishLifeStage lifeStage,
            float initialAgeSeconds = 0f,
            StrategyFishHabitatKind habitatKind = StrategyFishHabitatKind.Lake,
            int waterRegionId = -1,
            IReadOnlyList<Vector3> riverRoute = null,
            float riverSpeedMultiplier = 1f)
        {
            Vector3 spawnWorld = map.GetCellCenterWorld(spawnCell.x, spawnCell.y);
            Vector2 jitter = GetJitter(spawnCell.x, spawnCell.y, shoalId + 1289) * (map.CellSize * 0.30f);
            spawnWorld.x += jitter.x;
            spawnWorld.y += jitter.y;

            GameObject fishObject = new GameObject(lifeStage == StrategyFishLifeStage.Fry
                ? "Fish Fry"
                : $"Fish {species}");
            fishObject.transform.SetParent(wildlifeRoot, false);

            SpriteRenderer renderer = fishObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyFishSpriteFactory.GetIdleSprite(species, Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, shoalId, 719));
            renderer.color = Color.white;

            StrategyFishAgent agent = fishObject.AddComponent<StrategyFishAgent>();
            agent.Configure(
                map,
                population,
                species,
                shoalCenter,
                FishHomeRadius,
                shoalId,
                spawnWorld,
                renderer,
                lifeStage,
                initialAgeSeconds,
                habitatKind,
                waterRegionId);
            if (habitatKind == StrategyFishHabitatKind.River && riverRoute != null)
            {
                agent.ConfigureRiverRoute(riverRoute, riverSpeedMultiplier);
            }

            fish.Add(agent);
            if (agent.IsLakeFish && agent.IsAdult)
            {
                fishBreedCooldowns[agent] = Random.Range(FishBreedCooldownMin * 0.45f, FishBreedCooldownMax);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                lifeStage == StrategyFishLifeStage.Fry ? "FishBorn" : "FishSpawned",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("lifeStage", lifeStage),
                StrategyDebugLogger.F("habitat", habitatKind),
                StrategyDebugLogger.F("waterRegion", waterRegionId),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("cell", spawnCell),
                StrategyDebugLogger.F("home", shoalCenter),
                StrategyDebugLogger.F("world", spawnWorld));
        }

        private void SpawnBird(
            StrategyBirdSpecies species,
            int birdId,
            Vector2Int spawnCell)
        {
            Vector3 spawnWorld = map.GetCellCenterWorld(spawnCell.x, spawnCell.y);
            Vector2 jitter = GetJitter(spawnCell.x, spawnCell.y, birdId + 1801) * (map.CellSize * 0.30f);
            spawnWorld.x += jitter.x;
            spawnWorld.y += jitter.y;

            GameObject birdObject = new GameObject($"Bird {species}");
            birdObject.transform.SetParent(wildlifeRoot, false);

            SpriteRenderer renderer = birdObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyBirdSpriteFactory.GetIdleSprite(species, Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, birdId, 1439));
            renderer.color = Color.white;

            StrategyBirdAgent agent = birdObject.AddComponent<StrategyBirdAgent>();
            agent.Configure(
                map,
                population,
                species,
                spawnCell,
                BirdHomeRadius,
                birdId,
                spawnWorld,
                renderer);
            birds.Add(agent);

            StrategyDebugLogger.Info(
                "Wildlife",
                "BirdSpawned",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("bird", birdId),
                StrategyDebugLogger.F("cell", spawnCell),
                StrategyDebugLogger.F("world", spawnWorld));
        }

        private void UpdateDeerBreeding(float elapsedSeconds)
        {
            RemoveMissingDeer();
            if (deer.Count >= MaxDeerPopulation)
            {
                return;
            }

            for (int i = 0; i < deer.Count && deer.Count < MaxDeerPopulation; i++)
            {
                StrategyDeerAgent doe = deer[i];
                if (doe == null || doe.Sex != StrategyDeerSex.Female || !doe.IsAdult)
                {
                    continue;
                }

                int herdCount = CountLivingDeerInHerd(doe.HerdId);
                if (herdCount >= MaxDeerPerHerd)
                {
                    breedCooldowns[doe] = Random.Range(FailedBreedRetryMin, FailedBreedRetryMax);
                    continue;
                }

                float cooldown = GetBreedCooldown(doe) - elapsedSeconds;
                if (cooldown > 0f)
                {
                    breedCooldowns[doe] = cooldown;
                    continue;
                }

                if (!doe.CanBreed
                    || !HasAdultMaleMateNear(doe)
                    || !TryFindBirthCell(doe, out Vector2Int birthCell))
                {
                    breedCooldowns[doe] = Random.Range(FailedBreedRetryMin, FailedBreedRetryMax);
                    continue;
                }

                StrategyDeerSex fawnSex = Random.value < 0.5f ? StrategyDeerSex.Male : StrategyDeerSex.Female;
                SpawnDeer(fawnSex, doe.HerdId, doe.HomeCell, birthCell, StrategyDeerLifeStage.Fawn);
                breedCooldowns[doe] = Random.Range(BreedCooldownMin, BreedCooldownMax);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "DeerPopulationChanged",
                    StrategyDebugLogger.F("count", deer.Count),
                    StrategyDebugLogger.F("cap", MaxDeerPopulation),
                    StrategyDebugLogger.F("motherHerd", doe.HerdId),
                    StrategyDebugLogger.F("herdCount", herdCount + 1),
                    StrategyDebugLogger.F("herdCap", MaxDeerPerHerd));
            }
        }

        private int CountLivingDeerInHerd(int herdId)
        {
            int count = 0;
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null
                    && agent.HerdId == herdId
                    && agent.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private float GetBreedCooldown(StrategyDeerAgent doe)
        {
            if (doe == null)
            {
                return BreedCooldownMax;
            }

            if (!breedCooldowns.TryGetValue(doe, out float cooldown))
            {
                cooldown = Random.Range(BreedCooldownMin * 0.65f, BreedCooldownMax);
                breedCooldowns[doe] = cooldown;
            }

            return cooldown;
        }

        private bool HasAdultMaleMateNear(StrategyDeerAgent doe)
        {
            if (doe == null)
            {
                return false;
            }

            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent candidate = deer[i];
                if (candidate == null
                    || candidate == doe
                    || candidate.HerdId != doe.HerdId
                    || candidate.Sex != StrategyDeerSex.Male
                    || !candidate.IsAdult)
                {
                    continue;
                }

                float distance = Vector3.Distance(candidate.transform.position, doe.transform.position);
                if (distance <= MateSearchRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindBirthCell(StrategyDeerAgent mother, out Vector2Int cell)
        {
            cell = default;
            if (mother == null || !mother.TryGetCurrentCell(out Vector2Int motherCell))
            {
                return false;
            }

            for (int radius = 1; radius <= BirthCellSearchRadius; radius++)
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

                        Vector2Int candidate = motherCell + new Vector2Int(x, y);
                        if (IsBirthCellCandidate(candidate, mother))
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

        private bool IsBirthCellCandidate(Vector2Int cell, StrategyDeerAgent mother)
        {
            return mother != null
                && IsHerdSpawnCandidate(cell)
                && Vector2Int.Distance(cell, mother.HomeCell) <= mother.HomeRadius + 2
                && !IsTooCloseToOtherDeer(cell);
        }

        private bool IsTooCloseToOtherDeer(Vector2Int cell)
        {
            if (map == null)
            {
                return true;
            }

            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null && Vector3.Distance(agent.transform.position, world) < 0.65f)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateRabbitBreeding(float elapsedSeconds)
        {
            RemoveMissingRabbits();
            if (rabbits.Count >= MaxRabbitPopulation)
            {
                return;
            }

            for (int i = 0; i < rabbits.Count && rabbits.Count < MaxRabbitPopulation; i++)
            {
                StrategyRabbitAgent doe = rabbits[i];
                if (doe == null || doe.Sex != StrategyRabbitSex.Female || !doe.IsAdult)
                {
                    continue;
                }

                int groupCount = CountLivingRabbitsInGroup(doe.GroupId);
                if (groupCount >= MaxRabbitsPerGroup)
                {
                    rabbitBreedCooldowns[doe] = Random.Range(RabbitFailedBreedRetryMin, RabbitFailedBreedRetryMax);
                    continue;
                }

                float cooldown = GetRabbitBreedCooldown(doe) - elapsedSeconds;
                if (cooldown > 0f)
                {
                    rabbitBreedCooldowns[doe] = cooldown;
                    continue;
                }

                if (!doe.CanBreed
                    || !HasAdultMaleRabbitMateNear(doe)
                    || !TryFindRabbitBirthCell(doe, out Vector2Int birthCell))
                {
                    rabbitBreedCooldowns[doe] = Random.Range(RabbitFailedBreedRetryMin, RabbitFailedBreedRetryMax);
                    continue;
                }

                StrategyRabbitSex kitSex = Random.value < 0.5f ? StrategyRabbitSex.Male : StrategyRabbitSex.Female;
                SpawnRabbit(kitSex, doe.GroupId, doe.HomeCell, birthCell, StrategyRabbitLifeStage.Kit);
                rabbitBreedCooldowns[doe] = Random.Range(RabbitBreedCooldownMin, RabbitBreedCooldownMax);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "RabbitPopulationChanged",
                    StrategyDebugLogger.F("count", rabbits.Count),
                    StrategyDebugLogger.F("cap", MaxRabbitPopulation),
                    StrategyDebugLogger.F("motherGroup", doe.GroupId),
                    StrategyDebugLogger.F("groupCount", groupCount + 1),
                    StrategyDebugLogger.F("groupCap", MaxRabbitsPerGroup));
            }
        }

        private int CountLivingRabbitsInGroup(int groupId)
        {
            int count = 0;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent rabbit = rabbits[i];
                if (rabbit != null
                    && rabbit.GroupId == groupId
                    && rabbit.IsAlive
                    && !rabbit.IsCarcass)
                {
                    count++;
                }
            }

            return count;
        }

        private float GetRabbitBreedCooldown(StrategyRabbitAgent doe)
        {
            if (doe == null)
            {
                return RabbitBreedCooldownMax;
            }

            if (!rabbitBreedCooldowns.TryGetValue(doe, out float cooldown))
            {
                cooldown = Random.Range(RabbitBreedCooldownMin * 0.65f, RabbitBreedCooldownMax);
                rabbitBreedCooldowns[doe] = cooldown;
            }

            return cooldown;
        }

        private bool HasAdultMaleRabbitMateNear(StrategyRabbitAgent doe)
        {
            if (doe == null)
            {
                return false;
            }

            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent candidate = rabbits[i];
                if (candidate == null
                    || candidate == doe
                    || candidate.GroupId != doe.GroupId
                    || candidate.Sex != StrategyRabbitSex.Male
                    || !candidate.IsAdult)
                {
                    continue;
                }

                float distance = Vector3.Distance(candidate.transform.position, doe.transform.position);
                if (distance <= RabbitMateSearchRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindRabbitBirthCell(StrategyRabbitAgent mother, out Vector2Int cell)
        {
            cell = default;
            if (mother == null || !mother.TryGetCurrentCell(out Vector2Int motherCell))
            {
                return false;
            }

            for (int radius = 1; radius <= RabbitBirthCellSearchRadius; radius++)
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

                        Vector2Int candidate = motherCell + new Vector2Int(x, y);
                        if (IsRabbitBirthCellCandidate(candidate, mother))
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

        private bool IsRabbitBirthCellCandidate(Vector2Int cell, StrategyRabbitAgent mother)
        {
            return mother != null
                && IsRabbitSpawnCandidate(cell)
                && Vector2Int.Distance(cell, mother.HomeCell) <= mother.HomeRadius + 1
                && !IsTooCloseToOtherRabbits(cell);
        }
    }
}
