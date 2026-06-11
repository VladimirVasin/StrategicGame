using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWildlifeController : MonoBehaviour
    {
        private const int MinDeer = 8;
        private const int MaxDeer = 12;
        private const int MaxHerds = 4;
        private const int HerdHomeRadius = 10;
        private const int SpawnSearchAttempts = 360;
        private const int CampAvoidRadius = 10;
        private const int MaxDeerPopulation = 20;
        private const int MinRabbits = 12;
        private const int MaxRabbits = 18;
        private const int MaxRabbitGroups = 5;
        private const int RabbitHomeRadius = 6;
        private const int MaxRabbitPopulation = 36;
        private const int MinFish = 18;
        private const int MaxFish = 28;
        private const int MaxFishShoals = 7;
        private const int FishHomeRadius = 8;
        private const int MaxFishPopulation = 60;
        private const int MinBirds = 20;
        private const int MaxBirds = 32;
        private const int BirdHomeRadius = 14;
        private const float BreedingCheckInterval = 4.5f;
        private const float BreedCooldownMin = 85f;
        private const float BreedCooldownMax = 150f;
        private const float FailedBreedRetryMin = 18f;
        private const float FailedBreedRetryMax = 42f;
        private const float MateSearchRadius = 9.5f;
        private const int BirthCellSearchRadius = 5;
        private const float RabbitBreedCooldownMin = 38f;
        private const float RabbitBreedCooldownMax = 78f;
        private const float RabbitFailedBreedRetryMin = 10f;
        private const float RabbitFailedBreedRetryMax = 24f;
        private const float RabbitMateSearchRadius = 4.75f;
        private const int RabbitBirthCellSearchRadius = 4;
        private const float FishBreedCooldownMin = 42f;
        private const float FishBreedCooldownMax = 86f;
        private const float FishFailedBreedRetryMin = 12f;
        private const float FishFailedBreedRetryMax = 28f;
        private const float FishMateSearchRadius = 5.5f;
        private const int FishBirthCellSearchRadius = 4;

        private readonly List<StrategyDeerAgent> deer = new();
        private readonly Dictionary<StrategyDeerAgent, float> breedCooldowns = new();
        private readonly List<StrategyRabbitAgent> rabbits = new();
        private readonly Dictionary<StrategyRabbitAgent, float> rabbitBreedCooldowns = new();
        private readonly List<StrategyFishAgent> fish = new();
        private readonly Dictionary<StrategyFishAgent, float> fishBreedCooldowns = new();
        private readonly List<StrategyBirdAgent> birds = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private Transform wildlifeRoot;
        private Vector2Int campCell;
        private float breedingTimer;
        private float rabbitBreedingTimer;
        private float fishBreedingTimer;
        private bool hasCampCell;

        public static StrategyWildlifeController Active { get; private set; }
        public IReadOnlyList<StrategyDeerAgent> Deer => deer;
        public IReadOnlyList<StrategyRabbitAgent> Rabbits => rabbits;
        public IReadOnlyList<StrategyFishAgent> Fish => fish;
        public IReadOnlyList<StrategyBirdAgent> Birds => birds;

        private void Awake()
        {
            Active = this;
        }

        private void Update()
        {
            if (map == null)
            {
                return;
            }

            if (deer.Count > 0)
            {
                breedingTimer -= Time.deltaTime;
                if (breedingTimer <= 0f)
                {
                    breedingTimer = BreedingCheckInterval;
                    UpdateDeerBreeding(BreedingCheckInterval);
                }
            }

            if (rabbits.Count > 0)
            {
                rabbitBreedingTimer -= Time.deltaTime;
                if (rabbitBreedingTimer <= 0f)
                {
                    rabbitBreedingTimer = BreedingCheckInterval;
                    UpdateRabbitBreeding(BreedingCheckInterval);
                }
            }

            if (fish.Count > 0)
            {
                fishBreedingTimer -= Time.deltaTime;
                if (fishBreedingTimer <= 0f)
                {
                    fishBreedingTimer = BreedingCheckInterval;
                    UpdateFishBreeding(BreedingCheckInterval);
                }
            }
        }

        public void Configure(CityMapController mapController, StrategyPopulationController populationController)
        {
            map = mapController;
            population = populationController;
            hasCampCell = population != null && population.TryGetCampCell(out campCell);
            EnsureWildlifeRoot();
            GenerateWildlife();
        }

        public void GenerateWildlife()
        {
            if (map == null)
            {
                return;
            }

            EnsureWildlifeRoot();
            ClearWildlife();
            breedingTimer = Random.Range(8f, 18f);
            rabbitBreedingTimer = Random.Range(5f, 14f);
            fishBreedingTimer = Random.Range(6f, 16f);

            int targetDeer = MinDeer + (Hash(map.ActiveSeed, 17, 31, 43, 59) % (MaxDeer - MinDeer + 1));
            int targetRabbits = MinRabbits + (Hash(map.ActiveSeed, 19, 37, 53, 79) % (MaxRabbits - MinRabbits + 1));
            int targetFish = MinFish + (Hash(map.ActiveSeed, 23, 41, 67, 83) % (MaxFish - MinFish + 1));
            int targetBirds = MinBirds + (Hash(map.ActiveSeed, 29, 47, 71, 101) % (MaxBirds - MinBirds + 1));
            int targetHerds = Mathf.Clamp(Mathf.CeilToInt(targetDeer / 3.2f), 2, MaxHerds);
            HashSet<Vector2Int> usedCells = new();
            int remaining = targetDeer;
            int spawnedHerds = 0;

            for (int herd = 0; herd < targetHerds && remaining > 0; herd++)
            {
                if (!TryFindHerdCenter(herd, usedCells, out Vector2Int herdCenter))
                {
                    continue;
                }

                int herdsLeft = targetHerds - herd;
                int reserveForLater = Mathf.Max(0, (herdsLeft - 1) * 2);
                int maxThisHerd = Mathf.Min(4, remaining - reserveForLater);
                int herdSize = Mathf.Clamp(2 + (Hash(map.ActiveSeed, herd, 71, 89, 107) % 3), 2, Mathf.Max(2, maxThisHerd));

                bool spawnedMale = false;
                bool spawnedFemale = false;
                for (int slot = 0; slot < herdSize && remaining > 0; slot++)
                {
                    if (!TryFindHerdSpawnCell(herdCenter, herd, slot, usedCells, out Vector2Int spawnCell))
                    {
                        continue;
                    }

                    StrategyDeerSex sex = PickHerdSex(slot, spawnedMale, spawnedFemale);
                    SpawnDeer(sex, herd, herdCenter, spawnCell, StrategyDeerLifeStage.Adult);
                    usedCells.Add(spawnCell);
                    spawnedMale |= sex == StrategyDeerSex.Male;
                    spawnedFemale |= sex == StrategyDeerSex.Female;
                    remaining--;
                }

                spawnedHerds++;
            }

            int spawnedRabbitGroups = GenerateRabbits(targetRabbits, usedCells);
            int spawnedFishShoals = GenerateFish(targetFish);
            int spawnedBirds = GenerateBirds(targetBirds);

            StrategyDebugLogger.Info(
                "Wildlife",
                "Generated",
                StrategyDebugLogger.F("deer", deer.Count),
                StrategyDebugLogger.F("deerTarget", targetDeer),
                StrategyDebugLogger.F("deerCap", MaxDeerPopulation),
                StrategyDebugLogger.F("herds", spawnedHerds),
                StrategyDebugLogger.F("rabbits", rabbits.Count),
                StrategyDebugLogger.F("rabbitTarget", targetRabbits),
                StrategyDebugLogger.F("rabbitCap", MaxRabbitPopulation),
                StrategyDebugLogger.F("rabbitGroups", spawnedRabbitGroups),
                StrategyDebugLogger.F("fish", fish.Count),
                StrategyDebugLogger.F("fishTarget", targetFish),
                StrategyDebugLogger.F("fishCap", MaxFishPopulation),
                StrategyDebugLogger.F("fishShoals", spawnedFishShoals),
                StrategyDebugLogger.F("birds", spawnedBirds),
                StrategyDebugLogger.F("birdTarget", targetBirds),
                StrategyDebugLogger.F("seed", map.ActiveSeed),
                StrategyDebugLogger.F("hasCampAvoidance", hasCampCell));
        }

        private int GenerateRabbits(int targetRabbits, HashSet<Vector2Int> usedCells)
        {
            int targetGroups = Mathf.Clamp(Mathf.CeilToInt(targetRabbits / 4.0f), 3, MaxRabbitGroups);
            int remaining = targetRabbits;
            int spawnedGroups = 0;

            for (int group = 0; group < targetGroups && remaining > 0; group++)
            {
                if (!TryFindRabbitGroupCenter(group, usedCells, out Vector2Int groupCenter))
                {
                    continue;
                }

                int groupsLeft = targetGroups - group;
                int reserveForLater = Mathf.Max(0, (groupsLeft - 1) * 2);
                int maxThisGroup = Mathf.Min(5, remaining - reserveForLater);
                int groupSize = Mathf.Clamp(
                    3 + (Hash(map.ActiveSeed, group, 97, 149, 211) % 3),
                    2,
                    Mathf.Max(2, maxThisGroup));

                bool spawnedMale = false;
                bool spawnedFemale = false;
                for (int slot = 0; slot < groupSize && remaining > 0; slot++)
                {
                    if (!TryFindRabbitSpawnCell(groupCenter, group, slot, usedCells, out Vector2Int spawnCell))
                    {
                        continue;
                    }

                    StrategyRabbitSex sex = PickRabbitSex(slot, spawnedMale, spawnedFemale);
                    SpawnRabbit(sex, group, groupCenter, spawnCell, StrategyRabbitLifeStage.Adult);
                    usedCells.Add(spawnCell);
                    spawnedMale |= sex == StrategyRabbitSex.Male;
                    spawnedFemale |= sex == StrategyRabbitSex.Female;
                    remaining--;
                }

                spawnedGroups++;
            }

            return spawnedGroups;
        }

        private int GenerateFish(int targetFish)
        {
            int targetShoals = Mathf.Clamp(Mathf.CeilToInt(targetFish / 5.0f), 4, MaxFishShoals);
            HashSet<Vector2Int> usedWaterCells = new();
            int remaining = targetFish;
            int spawnedShoals = 0;

            for (int shoal = 0; shoal < targetShoals && remaining > 0; shoal++)
            {
                if (!TryFindFishShoalCenter(shoal, usedWaterCells, out Vector2Int shoalCenter))
                {
                    continue;
                }

                int shoalsLeft = targetShoals - shoal;
                int reserveForLater = Mathf.Max(0, (shoalsLeft - 1) * 3);
                int maxThisShoal = Mathf.Min(7, remaining - reserveForLater);
                int shoalSize = Mathf.Clamp(
                    4 + (Hash(map.ActiveSeed, shoal, 173, 197, 223) % 3),
                    3,
                    Mathf.Max(3, maxThisShoal));
                StrategyFishSpecies species = PickFishSpecies(shoal, shoalCenter);

                for (int slot = 0; slot < shoalSize && remaining > 0; slot++)
                {
                    if (!TryFindFishSpawnCell(shoalCenter, shoal, slot, usedWaterCells, out Vector2Int spawnCell))
                    {
                        continue;
                    }

                    SpawnFish(species, shoal, shoalCenter, spawnCell, StrategyFishLifeStage.Adult);
                    usedWaterCells.Add(spawnCell);
                    remaining--;
                }

                spawnedShoals++;
            }

            return spawnedShoals;
        }

        private int GenerateBirds(int targetBirds)
        {
            HashSet<Vector2Int> usedBirdCells = new();
            int spawnedBirds = 0;

            for (int bird = 0; bird < targetBirds; bird++)
            {
                StrategyBirdSpecies species = PickBirdSpecies(bird);
                if (!TryFindBirdSpawnCell(species, bird, usedBirdCells, out Vector2Int spawnCell))
                {
                    StrategyBirdSpecies fallbackSpecies = species switch
                    {
                        StrategyBirdSpecies.Duck => StrategyBirdSpecies.Sparrow,
                        StrategyBirdSpecies.Crow => StrategyBirdSpecies.Sparrow,
                        _ => StrategyBirdSpecies.Crow
                    };
                    if (!TryFindBirdSpawnCell(fallbackSpecies, bird + 997, usedBirdCells, out spawnCell))
                    {
                        continue;
                    }

                    species = fallbackSpecies;
                }

                SpawnBird(species, bird, spawnCell);
                usedBirdCells.Add(spawnCell);
                spawnedBirds++;
            }

            return spawnedBirds;
        }

        private void SpawnDeer(
            StrategyDeerSex sex,
            int herdId,
            Vector2Int herdCenter,
            Vector2Int spawnCell,
            StrategyDeerLifeStage lifeStage,
            float initialAgeSeconds = 0f)
        {
            Vector3 spawnWorld = map.GetCellCenterWorld(spawnCell.x, spawnCell.y);
            Vector2 jitter = GetJitter(spawnCell.x, spawnCell.y, herdId + 311) * (map.CellSize * 0.22f);
            spawnWorld.x += jitter.x;
            spawnWorld.y += jitter.y;

            GameObject deerObject = new GameObject(lifeStage == StrategyDeerLifeStage.Fawn
                ? "Deer Fawn"
                : sex == StrategyDeerSex.Male
                    ? "Deer Buck"
                    : "Deer Doe");
            deerObject.transform.SetParent(wildlifeRoot, false);

            SpriteRenderer renderer = deerObject.AddComponent<SpriteRenderer>();
            StrategyDeerSex spriteSex = lifeStage == StrategyDeerLifeStage.Fawn ? StrategyDeerSex.Female : sex;
            renderer.sprite = StrategyDeerSpriteFactory.GetIdleSprite(spriteSex, Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, herdId, 137));
            renderer.color = Color.white;

            StrategyDeerAgent agent = deerObject.AddComponent<StrategyDeerAgent>();
            agent.Configure(
                map,
                population,
                sex,
                herdCenter,
                HerdHomeRadius,
                herdId,
                spawnWorld,
                renderer,
                lifeStage,
                initialAgeSeconds);
            deer.Add(agent);
            if (agent.Sex == StrategyDeerSex.Female && agent.IsAdult)
            {
                breedCooldowns[agent] = Random.Range(BreedCooldownMin * 0.45f, BreedCooldownMax);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                lifeStage == StrategyDeerLifeStage.Fawn ? "DeerBorn" : "DeerSpawned",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("lifeStage", lifeStage),
                StrategyDebugLogger.F("herd", herdId),
                StrategyDebugLogger.F("cell", spawnCell),
                StrategyDebugLogger.F("home", herdCenter),
                StrategyDebugLogger.F("world", spawnWorld));
        }

        private void SpawnRabbit(
            StrategyRabbitSex sex,
            int groupId,
            Vector2Int groupCenter,
            Vector2Int spawnCell,
            StrategyRabbitLifeStage lifeStage,
            float initialAgeSeconds = 0f)
        {
            Vector3 spawnWorld = map.GetCellCenterWorld(spawnCell.x, spawnCell.y);
            Vector2 jitter = GetJitter(spawnCell.x, spawnCell.y, groupId + 941) * (map.CellSize * 0.25f);
            spawnWorld.x += jitter.x;
            spawnWorld.y += jitter.y;

            GameObject rabbitObject = new GameObject(lifeStage == StrategyRabbitLifeStage.Kit
                ? "Rabbit Kit"
                : sex == StrategyRabbitSex.Male
                    ? "Rabbit Buck"
                    : "Rabbit Doe");
            rabbitObject.transform.SetParent(wildlifeRoot, false);

            SpriteRenderer renderer = rabbitObject.AddComponent<SpriteRenderer>();
            StrategyRabbitSex spriteSex = lifeStage == StrategyRabbitLifeStage.Kit ? StrategyRabbitSex.Female : sex;
            renderer.sprite = StrategyRabbitSpriteFactory.GetIdleSprite(spriteSex, Hash(map.ActiveSeed, spawnCell.x, spawnCell.y, groupId, 419));
            renderer.color = Color.white;

            StrategyRabbitAgent agent = rabbitObject.AddComponent<StrategyRabbitAgent>();
            agent.Configure(
                map,
                population,
                sex,
                groupCenter,
                RabbitHomeRadius,
                groupId,
                spawnWorld,
                renderer,
                lifeStage,
                initialAgeSeconds);
            rabbits.Add(agent);
            if (agent.Sex == StrategyRabbitSex.Female && agent.IsAdult)
            {
                rabbitBreedCooldowns[agent] = Random.Range(RabbitBreedCooldownMin * 0.45f, RabbitBreedCooldownMax);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                lifeStage == StrategyRabbitLifeStage.Kit ? "RabbitBorn" : "RabbitSpawned",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("lifeStage", lifeStage),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("cell", spawnCell),
                StrategyDebugLogger.F("home", groupCenter),
                StrategyDebugLogger.F("world", spawnWorld));
        }

        private void SpawnFish(
            StrategyFishSpecies species,
            int shoalId,
            Vector2Int shoalCenter,
            Vector2Int spawnCell,
            StrategyFishLifeStage lifeStage,
            float initialAgeSeconds = 0f)
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
                initialAgeSeconds);
            fish.Add(agent);
            if (agent.IsAdult)
            {
                fishBreedCooldowns[agent] = Random.Range(FishBreedCooldownMin * 0.45f, FishBreedCooldownMax);
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                lifeStage == StrategyFishLifeStage.Fry ? "FishBorn" : "FishSpawned",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("lifeStage", lifeStage),
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
                    StrategyDebugLogger.F("motherHerd", doe.HerdId));
            }
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
                    StrategyDebugLogger.F("motherGroup", doe.GroupId));
            }
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
                if (adult == null || !adult.IsAdult)
                {
                    continue;
                }

                float cooldown = GetFishBreedCooldown(adult) - elapsedSeconds;
                if (cooldown > 0f)
                {
                    fishBreedCooldowns[adult] = cooldown;
                    continue;
                }

                if (!adult.CanBreed
                    || !HasAdultFishMateNear(adult)
                    || !TryFindFishBirthCell(adult, out Vector2Int birthCell))
                {
                    fishBreedCooldowns[adult] = Random.Range(FishFailedBreedRetryMin, FishFailedBreedRetryMax);
                    continue;
                }

                SpawnFish(adult.Species, adult.ShoalId, adult.HomeCell, birthCell, StrategyFishLifeStage.Fry);
                fishBreedCooldowns[adult] = Random.Range(FishBreedCooldownMin, FishBreedCooldownMax);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "FishPopulationChanged",
                    StrategyDebugLogger.F("count", fish.Count),
                    StrategyDebugLogger.F("cap", MaxFishPopulation),
                    StrategyDebugLogger.F("shoal", adult.ShoalId));
            }
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
                && IsFishSpawnCandidate(cell)
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

        private bool TryFindRabbitGroupCenter(int group, HashSet<Vector2Int> usedCells, out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, group, attempt, 431, 463) % map.Width;
                int y = Hash(map.ActiveSeed, group, attempt, 487, 541) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedCells.Contains(candidate) || !IsRabbitGroupCenterCandidate(candidate))
                {
                    continue;
                }

                float score = GetRabbitSpawnTerrainScore(candidate) + CountWalkableNeighbors(candidate, 2) * 0.28f;
                if (hasCampCell)
                {
                    score += Mathf.Clamp(Vector2Int.Distance(candidate, campCell) - CampAvoidRadius, 0f, 16f) * 0.10f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindRabbitSpawnCell(
            Vector2Int groupCenter,
            int group,
            int slot,
            HashSet<Vector2Int> usedCells,
            out Vector2Int cell)
        {
            for (int radius = 0; radius <= 4; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = groupCenter + new Vector2Int(x, y);
                        if (!usedCells.Contains(candidate) && IsRabbitSpawnCandidate(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, group, slot, radius, 563) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsRabbitGroupCenterCandidate(Vector2Int cell)
        {
            return IsRabbitSpawnCandidate(cell)
                && GetRabbitSpawnTerrainScore(cell) > 0f
                && CountWalkableNeighbors(cell, 2) >= 6;
        }

        private StrategyFishSpecies PickFishSpecies(int shoal, Vector2Int shoalCenter)
        {
            int roll = Hash(map.ActiveSeed, shoal, shoalCenter.x, shoalCenter.y, 997) % 3;
            return roll switch
            {
                1 => StrategyFishSpecies.Carp,
                2 => StrategyFishSpecies.Perch,
                _ => StrategyFishSpecies.Minnow
            };
        }

        private StrategyBirdSpecies PickBirdSpecies(int bird)
        {
            int roll = Hash(map.ActiveSeed, bird, 811, 829, 853) % 10;
            return roll switch
            {
                <= 1 => StrategyBirdSpecies.Duck,
                <= 4 => StrategyBirdSpecies.Crow,
                _ => StrategyBirdSpecies.Sparrow
            };
        }

        private bool TryFindBirdSpawnCell(
            StrategyBirdSpecies species,
            int bird,
            HashSet<Vector2Int> usedBirdCells,
            out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, bird, attempt, 821, 859) % map.Width;
                int y = Hash(map.ActiveSeed, bird, attempt, 887, 907) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedBirdCells.Contains(candidate) || !IsBirdSpawnCandidate(species, candidate))
                {
                    continue;
                }

                float score = GetBirdSpawnTerrainScore(species, candidate);
                if (hasCampCell)
                {
                    float campDistance = Vector2Int.Distance(candidate, campCell);
                    if (species == StrategyBirdSpecies.Sparrow)
                    {
                        score += Mathf.Clamp(8f - campDistance, 0f, 8f) * 0.05f;
                    }
                    else if (species == StrategyBirdSpecies.Crow)
                    {
                        score += Mathf.Clamp(campDistance - 4f, 0f, 12f) * 0.04f;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindFishShoalCenter(int shoal, HashSet<Vector2Int> usedWaterCells, out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, shoal, attempt, 601, 653) % map.Width;
                int y = Hash(map.ActiveSeed, shoal, attempt, 677, 733) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedWaterCells.Contains(candidate) || !IsFishShoalCenterCandidate(candidate))
                {
                    continue;
                }

                float score = GetFishSpawnTerrainScore(candidate);
                if (hasCampCell)
                {
                    score += Mathf.Clamp(Vector2Int.Distance(candidate, campCell) - 5f, 0f, 14f) * 0.04f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindFishSpawnCell(
            Vector2Int shoalCenter,
            int shoal,
            int slot,
            HashSet<Vector2Int> usedWaterCells,
            out Vector2Int cell)
        {
            for (int radius = 0; radius <= 5; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = shoalCenter + new Vector2Int(x, y);
                        if (!usedWaterCells.Contains(candidate)
                            && IsFishSpawnCandidate(candidate)
                            && CountWaterNeighbors(candidate, 1) >= 1)
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, shoal, slot, radius, 761) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsFishShoalCenterCandidate(Vector2Int cell)
        {
            return IsFishSpawnCandidate(cell)
                && CountWaterNeighbors(cell, 1) >= 2
                && GetFishSpawnTerrainScore(cell) > 0f;
        }

        private bool TryFindHerdCenter(int herd, HashSet<Vector2Int> usedCells, out Vector2Int cell)
        {
            cell = default;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int attempt = 0; attempt < SpawnSearchAttempts; attempt++)
            {
                int x = Hash(map.ActiveSeed, herd, attempt, 191, 251) % map.Width;
                int y = Hash(map.ActiveSeed, herd, attempt, 257, 313) % map.Height;
                Vector2Int candidate = new Vector2Int(x, y);
                if (usedCells.Contains(candidate) || !IsHerdCenterCandidate(candidate))
                {
                    continue;
                }

                float score = GetSpawnTerrainScore(candidate) + CountWalkableNeighbors(candidate, 3) * 0.35f;
                if (hasCampCell)
                {
                    score += Mathf.Clamp(Vector2Int.Distance(candidate, campCell) - CampAvoidRadius, 0f, 18f) * 0.12f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindHerdSpawnCell(
            Vector2Int herdCenter,
            int herd,
            int slot,
            HashSet<Vector2Int> usedCells,
            out Vector2Int cell)
        {
            for (int radius = 0; radius <= 5; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = herdCenter + new Vector2Int(x, y);
                        if (!usedCells.Contains(candidate) && IsHerdSpawnCandidate(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, herd, slot, radius, 337) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsHerdCenterCandidate(Vector2Int cell)
        {
            return IsHerdSpawnCandidate(cell)
                && GetSpawnTerrainScore(cell) > 0f
                && CountWalkableNeighbors(cell, 2) >= 5;
        }

        private bool IsHerdSpawnCandidate(Vector2Int cell)
        {
            if (map == null
                || !map.IsCellWalkable(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind == CityMapCellKind.Water)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < CampAvoidRadius)
            {
                return false;
            }

            return mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Forest
                || mapCell.Kind == CityMapCellKind.Dirt;
        }

        private bool IsRabbitSpawnCandidate(Vector2Int cell)
        {
            if (map == null
                || !map.IsCellWalkable(cell)
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind == CityMapCellKind.Water
                || mapCell.Kind == CityMapCellKind.Shore)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < CampAvoidRadius)
            {
                return false;
            }

            return mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Forest
                || mapCell.Kind == CityMapCellKind.Dirt;
        }

        private bool IsFishSpawnCandidate(Vector2Int cell)
        {
            if (map == null
                || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                || mapCell.Kind != CityMapCellKind.Water)
            {
                return false;
            }

            if (hasCampCell && Vector2Int.Distance(cell, campCell) < 5)
            {
                return false;
            }

            return true;
        }

        private bool IsBirdSpawnCandidate(StrategyBirdSpecies species, Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return false;
            }

            return species switch
            {
                StrategyBirdSpecies.Duck => mapCell.Kind == CityMapCellKind.Water
                    || (mapCell.Kind == CityMapCellKind.Shore && map.IsCellWalkable(cell)),
                StrategyBirdSpecies.Crow => mapCell.Kind == CityMapCellKind.Forest
                    || (map.IsCellWalkable(cell)
                        && (mapCell.Kind == CityMapCellKind.Dirt
                            || mapCell.Kind == CityMapCellKind.Grass
                            || mapCell.Kind == CityMapCellKind.Meadow)),
                _ => map.IsCellWalkable(cell)
                    && (mapCell.Kind == CityMapCellKind.Meadow
                        || mapCell.Kind == CityMapCellKind.Grass
                        || mapCell.Kind == CityMapCellKind.Dirt
                        || mapCell.Kind == CityMapCellKind.Shore)
            };
        }

        private int CountWalkableNeighbors(Vector2Int center, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int candidate = center + new Vector2Int(x, y);
                    if (map.IsCellWalkable(candidate))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private float GetSpawnTerrainScore(Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            float baseScore = mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 5.0f,
                CityMapCellKind.Grass => 3.0f,
                CityMapCellKind.Forest => 1.6f,
                CityMapCellKind.Dirt => 0.35f,
                _ => -10f
            };

            if (mapCell.Kind == CityMapCellKind.Meadow || mapCell.Kind == CityMapCellKind.Grass)
            {
                baseScore += CountForestNeighbors(cell) * 0.28f;
            }

            return baseScore;
        }

        private float GetRabbitSpawnTerrainScore(Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            float baseScore = mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 5.4f,
                CityMapCellKind.Grass => 4.1f,
                CityMapCellKind.Forest => 1.2f,
                CityMapCellKind.Dirt => 0.55f,
                _ => -10f
            };

            if (mapCell.Kind == CityMapCellKind.Meadow || mapCell.Kind == CityMapCellKind.Grass)
            {
                baseScore += CountForestNeighbors(cell) * 0.34f;
            }

            return baseScore;
        }

        private float GetFishSpawnTerrainScore(Vector2Int cell)
        {
            if (!IsFishSpawnCandidate(cell))
            {
                return -10f;
            }

            int waterNeighbors = CountWaterNeighbors(cell, 2);
            int shoreNeighbors = CountShoreNeighbors(cell, 2);
            float score = 1f + waterNeighbors * 0.34f + Mathf.Min(shoreNeighbors, 5) * 0.10f;
            if (waterNeighbors >= 12)
            {
                score += 2.0f;
            }
            else if (waterNeighbors >= 6)
            {
                score += 0.9f;
            }

            if (shoreNeighbors > 12)
            {
                score -= 0.75f;
            }

            return score;
        }

        private float GetBirdSpawnTerrainScore(StrategyBirdSpecies species, Vector2Int cell)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            return species switch
            {
                StrategyBirdSpecies.Duck => mapCell.Kind switch
                {
                    CityMapCellKind.Water => 4.4f + CountWaterNeighbors(cell, 2) * 0.28f + CountShoreNeighbors(cell, 2) * 0.08f,
                    CityMapCellKind.Shore => 2.8f + CountWaterNeighbors(cell, 2) * 0.32f,
                    _ => -10f
                },
                StrategyBirdSpecies.Crow => mapCell.Kind switch
                {
                    CityMapCellKind.Forest => 4.8f + CountForestNeighbors(cell) * 0.26f,
                    CityMapCellKind.Dirt => 3.1f + CountForestNeighbors(cell) * 0.16f,
                    CityMapCellKind.Grass => 1.8f + CountForestNeighbors(cell) * 0.12f,
                    CityMapCellKind.Meadow => 1.2f + CountForestNeighbors(cell) * 0.10f,
                    _ => -10f
                },
                _ => mapCell.Kind switch
                {
                    CityMapCellKind.Meadow => 5.2f + CountWalkableNeighbors(cell, 1) * 0.10f,
                    CityMapCellKind.Grass => 4.2f + CountWalkableNeighbors(cell, 1) * 0.08f,
                    CityMapCellKind.Dirt => 2.1f + CountWalkableNeighbors(cell, 1) * 0.05f,
                    CityMapCellKind.Shore => 1.4f + CountWaterNeighbors(cell, 1) * 0.08f,
                    _ => -10f
                }
            };
        }

        private int CountWaterNeighbors(Vector2Int cell, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Water)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountShoreNeighbors(Vector2Int cell, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Shore)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountForestNeighbors(Vector2Int cell)
        {
            int count = 0;
            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && neighbor.Kind == CityMapCellKind.Forest)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private Vector2 GetJitter(int x, int y, int salt)
        {
            float jitterX = Hash01(map.ActiveSeed, x, y, salt) - 0.5f;
            float jitterY = Hash01(map.ActiveSeed, x, y, salt + 17) - 0.5f;
            return new Vector2(jitterX, jitterY);
        }

        private void EnsureWildlifeRoot()
        {
            if (wildlifeRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Wildlife");
            root.transform.SetParent(transform, false);
            wildlifeRoot = root.transform;
        }

        private void ClearWildlife()
        {
            deer.Clear();
            breedCooldowns.Clear();
            rabbits.Clear();
            rabbitBreedCooldowns.Clear();
            fish.Clear();
            fishBreedCooldowns.Clear();
            birds.Clear();
            if (wildlifeRoot == null)
            {
                return;
            }

            for (int i = wildlifeRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = wildlifeRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void RemoveMissingDeer()
        {
            for (int i = deer.Count - 1; i >= 0; i--)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null)
                {
                    continue;
                }

                deer.RemoveAt(i);
            }

            List<StrategyDeerAgent> missingCooldowns = null;
            foreach (KeyValuePair<StrategyDeerAgent, float> pair in breedCooldowns)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                missingCooldowns ??= new List<StrategyDeerAgent>();
                missingCooldowns.Add(pair.Key);
            }

            if (missingCooldowns == null)
            {
                return;
            }

            for (int i = 0; i < missingCooldowns.Count; i++)
            {
                breedCooldowns.Remove(missingCooldowns[i]);
            }
        }

        private void RemoveMissingRabbits()
        {
            for (int i = rabbits.Count - 1; i >= 0; i--)
            {
                StrategyRabbitAgent agent = rabbits[i];
                if (agent != null)
                {
                    continue;
                }

                rabbits.RemoveAt(i);
            }

            List<StrategyRabbitAgent> missingCooldowns = null;
            foreach (KeyValuePair<StrategyRabbitAgent, float> pair in rabbitBreedCooldowns)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                missingCooldowns ??= new List<StrategyRabbitAgent>();
                missingCooldowns.Add(pair.Key);
            }

            if (missingCooldowns == null)
            {
                return;
            }

            for (int i = 0; i < missingCooldowns.Count; i++)
            {
                rabbitBreedCooldowns.Remove(missingCooldowns[i]);
            }
        }

        private void RemoveMissingFish()
        {
            for (int i = fish.Count - 1; i >= 0; i--)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null)
                {
                    continue;
                }

                fish.RemoveAt(i);
            }

            List<StrategyFishAgent> missingCooldowns = null;
            foreach (KeyValuePair<StrategyFishAgent, float> pair in fishBreedCooldowns)
            {
                if (pair.Key != null)
                {
                    continue;
                }

                missingCooldowns ??= new List<StrategyFishAgent>();
                missingCooldowns.Add(pair.Key);
            }

            if (missingCooldowns == null)
            {
                return;
            }

            for (int i = 0; i < missingCooldowns.Count; i++)
            {
                fishBreedCooldowns.Remove(missingCooldowns[i]);
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private static float Hash01(int seed, int x, int y, int salt)
        {
            return Hash(seed, x, y, salt, 0) / (float)int.MaxValue;
        }

        private static int Hash(int seed, int a, int b, int c, int d)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h = h * 1597334677 + d * 381201580;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & int.MaxValue;
            }
        }
    }
}
