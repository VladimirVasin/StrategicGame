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
        private const float BreedingCheckInterval = 4.5f;
        private const float BreedCooldownMin = 85f;
        private const float BreedCooldownMax = 150f;
        private const float FailedBreedRetryMin = 18f;
        private const float FailedBreedRetryMax = 42f;
        private const float MateSearchRadius = 9.5f;
        private const int BirthCellSearchRadius = 5;

        private readonly List<StrategyDeerAgent> deer = new();
        private readonly Dictionary<StrategyDeerAgent, float> breedCooldowns = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private Transform wildlifeRoot;
        private Vector2Int campCell;
        private float breedingTimer;
        private bool hasCampCell;

        public static StrategyWildlifeController Active { get; private set; }
        public IReadOnlyList<StrategyDeerAgent> Deer => deer;

        private void Awake()
        {
            Active = this;
        }

        private void Update()
        {
            if (map == null || deer.Count <= 0)
            {
                return;
            }

            breedingTimer -= Time.deltaTime;
            if (breedingTimer > 0f)
            {
                return;
            }

            breedingTimer = BreedingCheckInterval;
            UpdateDeerBreeding(BreedingCheckInterval);
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

            int targetDeer = MinDeer + (Hash(map.ActiveSeed, 17, 31, 43, 59) % (MaxDeer - MinDeer + 1));
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

            StrategyDebugLogger.Info(
                "Wildlife",
                "Generated",
                StrategyDebugLogger.F("deer", deer.Count),
                StrategyDebugLogger.F("target", targetDeer),
                StrategyDebugLogger.F("cap", MaxDeerPopulation),
                StrategyDebugLogger.F("herds", spawnedHerds),
                StrategyDebugLogger.F("seed", map.ActiveSeed),
                StrategyDebugLogger.F("hasCampAvoidance", hasCampCell));
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
