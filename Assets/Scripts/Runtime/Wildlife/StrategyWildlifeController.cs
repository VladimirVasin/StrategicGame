using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyWildlifeController : MonoBehaviour
    {
        private const int MinDeer = 12;
        private const int MaxDeer = 16;
        private const int MaxHerds = 8;
        private const int MaxDeerPerHerd = 3;
        private const int HerdHomeRadius = 10;
        private const int SpawnSearchAttempts = 360;
        private const int CampAvoidRadius = 10;
        private const int MaxDeerPopulation = 24;
        private const int WolfDeerControlThreshold = 20;
        private const int MinRabbits = 16;
        private const int MaxRabbits = 22;
        private const int MaxRabbitGroups = 10;
        private const int NearCampRabbitGroups = 3;
        private const int RabbitHomeRadius = 6;
        private const int MaxRabbitsPerGroup = 3;
        private const int MaxRabbitPopulation = 30;
        private const int WolfRabbitControlThreshold = 24;
        private const int RabbitCampMinDistance = 7;
        private const int RabbitCampMaxDistance = 30;
        private const int RabbitGroupCenterMaxCampDistance = RabbitCampMaxDistance - 4;
        private const int MinFish = 22;
        private const int MaxFish = 32;
        private const int MaxFishShoals = 12;
        private const int MaxFishPerShoal = 3;
        private const int FishHomeRadius = 8;
        private const int MaxFishPopulation = 36;
        private const int LakeFishRegionMinCap = 3;
        private const int LakeFishRegionMaxCap = 8;
        private const int LakeFishCellsPerCapacity = 10;
        private const int MaxRiverFishPopulation = 8;
        private const int RiverFishShoalIdBase = 10000;
        private const float RiverFishSpawnIntervalMin = 7.5f;
        private const float RiverFishSpawnIntervalMax = 14.0f;
        private const int MinWolfPacks = 3;
        private const int MaxWolfPacks = 4;
        private const int WolfPackMinSize = 2;
        private const int WolfPackMaxSize = 3;
        private const int WolfHomeRadius = 14;
        private const int WolfCampAvoidRadius = 18;
        private const int WolfHuntRadius = 14;
        private const int WolfResidentThreatRadius = 12;
        private const float WolfSettlementPressureLimit = 2.35f;
        private const float WolfSettlementCacheInterval = 2.5f;
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
        private const float FishLakeBirthBlockedLogIntervalSeconds = 20f;
        private const float MigrationUpdateInterval = 5.0f;
        private const float MigrationInitialDelay = 38f;
        private const int DeerMigrationStep = 4;
        private const int RabbitMigrationStep = 3;
        private const int WolfMigrationStep = 5;
        private const int BirdMigrationStep = 6;
        private const int FishMigrationStep = 3;
        private const int DeerMigrationTargetMinDistance = 26;
        private const int RabbitMigrationTargetMinDistance = 18;
        private const int WolfMigrationTargetMinDistance = 34;
        private const int BirdMigrationTargetMinDistance = 24;
        private const int FishMigrationTargetMinDistance = 8;
        private const float DeerMigrationCooldownMin = 48f;
        private const float DeerMigrationCooldownMax = 96f;
        private const float RabbitMigrationCooldownMin = 72f;
        private const float RabbitMigrationCooldownMax = 132f;
        private const float WolfMigrationCooldownMin = 58f;
        private const float WolfMigrationCooldownMax = 118f;
        private const float BirdMigrationCooldownMin = 36f;
        private const float BirdMigrationCooldownMax = 84f;
        private const float FishMigrationCooldownMin = 64f;
        private const float FishMigrationCooldownMax = 118f;
        private const float DeerMigrationSettlementLimit = 1.85f;
        private const float RabbitMigrationSettlementLimit = 1.55f;
        private const float BirdMigrationSettlementLimit = 2.10f;
        private const float WolfMigrationSettlementLimit = WolfSettlementPressureLimit * 0.42f;

        private readonly List<StrategyDeerAgent> deer = new();
        private readonly Dictionary<StrategyDeerAgent, float> breedCooldowns = new();
        private readonly Dictionary<int, MigrationState> deerMigrations = new();
        private readonly List<StrategyRabbitAgent> rabbits = new();
        private readonly Dictionary<StrategyRabbitAgent, float> rabbitBreedCooldowns = new();
        private readonly Dictionary<int, MigrationState> rabbitMigrations = new();
        private readonly List<StrategyFishAgent> fish = new();
        private readonly Dictionary<StrategyFishAgent, float> fishBreedCooldowns = new();
        private readonly Dictionary<int, MigrationState> fishMigrations = new();
        private readonly Dictionary<int, float> fishLakeBirthBlockedLogTimes = new();
        private readonly List<FishWaterRegion> lakeFishRegions = new();
        private readonly Dictionary<Vector2Int, int> lakeRegionByCell = new();
        private readonly List<Vector2Int> riverRouteCells = new();
        private readonly List<StrategyBirdAgent> birds = new();
        private readonly Dictionary<int, MigrationState> birdMigrations = new();
        private readonly List<StrategyWolfPack> wolfPacks = new();
        private readonly List<StrategyWolfAgent> wolves = new();
        private readonly Dictionary<int, MigrationState> wolfMigrations = new();
        private readonly Dictionary<StrategyResidentAgent, StrategyWolfAgent> wolfResidentTargets = new();
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyFogOfWarController fog;
        private Transform wildlifeRoot;
        private Vector2Int campCell;
        private float breedingTimer;
        private float rabbitBreedingTimer;
        private float fishBreedingTimer;
        private float riverFishSpawnTimer;
        private float migrationTimer;
        private float nextSettlementCacheRefreshTime;
        private int nextRiverShoalId = RiverFishShoalIdBase;
        private bool hasCampCell;
        private StrategyPlacedBuilding[] settlementBuildings;
        private StrategyConstructionSite[] settlementConstructionSites;

        public static StrategyWildlifeController Active { get; private set; }
        public IReadOnlyList<StrategyDeerAgent> Deer => deer;
        public IReadOnlyList<StrategyRabbitAgent> Rabbits => rabbits;
        public IReadOnlyList<StrategyFishAgent> Fish => fish;
        public IReadOnlyList<StrategyBirdAgent> Birds => birds;
        public IReadOnlyList<StrategyWolfAgent> Wolves => wolves;

        private sealed class FishWaterRegion
        {
            public readonly List<Vector2Int> Cells = new();
            public int Id;
            public int Capacity;
            public Vector2Int Center;
        }

        private sealed class MigrationState
        {
            public Vector2Int Target;
            public float Cooldown;
            public bool HasTarget;
            public int FailedSteps;
        }

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

            UpdateRiverFishSpawning(Time.deltaTime);
            UpdateWildlifeMigration(Time.deltaTime);
        }

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyFogOfWarController fogController)
        {
            map = mapController;
            population = populationController;
            fog = fogController;
            hasCampCell = population != null && population.TryGetCampCell(out campCell);
            migrationTimer = MigrationInitialDelay;
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
            riverFishSpawnTimer = Random.Range(2.0f, 5.5f);
            nextRiverShoalId = RiverFishShoalIdBase + (Hash(map.ActiveSeed, 347, 563, 719, 887) % 997);
            BuildFishWaterRegions();

            int targetDeer = MinDeer + (Hash(map.ActiveSeed, 17, 31, 43, 59) % (MaxDeer - MinDeer + 1));
            int targetRabbits = MinRabbits + (Hash(map.ActiveSeed, 19, 37, 53, 79) % (MaxRabbits - MinRabbits + 1));
            int targetFish = MinFish + (Hash(map.ActiveSeed, 23, 41, 67, 83) % (MaxFish - MinFish + 1));
            int targetBirds = MinBirds + (Hash(map.ActiveSeed, 29, 47, 71, 101) % (MaxBirds - MinBirds + 1));
            int targetWolfPacks = MinWolfPacks + (Hash(map.ActiveSeed, 31, 61, 89, 131) % (MaxWolfPacks - MinWolfPacks + 1));
            int targetHerds = Mathf.Clamp(Mathf.CeilToInt(targetDeer / 2.25f), 4, MaxHerds);
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
                int maxThisHerd = Mathf.Min(MaxDeerPerHerd, remaining - reserveForLater);
                int herdSize = Mathf.Clamp(2 + (Hash(map.ActiveSeed, herd, 71, 89, 107) % 2), 2, Mathf.Max(2, maxThisHerd));

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
            int spawnedWolfPacks = GenerateWolves(targetWolfPacks, usedCells);
            int spawnedFishShoals = GenerateFish(targetFish);
            int spawnedBirds = GenerateBirds(targetBirds);
            int lakeFishCapacity = GetTotalLakeFishCapacity();

            StrategyDebugLogger.Info(
                "Wildlife",
                "Generated",
                StrategyDebugLogger.F("deer", deer.Count),
                StrategyDebugLogger.F("deerTarget", targetDeer),
                StrategyDebugLogger.F("deerCap", MaxDeerPopulation),
                StrategyDebugLogger.F("herds", spawnedHerds),
                StrategyDebugLogger.F("herdTarget", targetHerds),
                StrategyDebugLogger.F("herdCap", MaxDeerPerHerd),
                StrategyDebugLogger.F("rabbits", rabbits.Count),
                StrategyDebugLogger.F("rabbitTarget", targetRabbits),
                StrategyDebugLogger.F("rabbitCap", MaxRabbitPopulation),
                StrategyDebugLogger.F("rabbitGroupCap", MaxRabbitsPerGroup),
                StrategyDebugLogger.F("rabbitGroups", spawnedRabbitGroups),
                StrategyDebugLogger.F("nearCampRabbitGroups", hasCampCell ? NearCampRabbitGroups : 0),
                StrategyDebugLogger.F("rabbitCampMaxDistance", hasCampCell ? RabbitCampMaxDistance : 0),
                StrategyDebugLogger.F("wolves", wolves.Count),
                StrategyDebugLogger.F("wolfPacks", spawnedWolfPacks),
                StrategyDebugLogger.F("wolfPackTarget", targetWolfPacks),
                StrategyDebugLogger.F("wolfPackSizeMin", WolfPackMinSize),
                StrategyDebugLogger.F("wolfPackSizeMax", WolfPackMaxSize),
                StrategyDebugLogger.F("wolfCampAvoidRadius", hasCampCell ? WolfCampAvoidRadius : 0),
                StrategyDebugLogger.F("fish", fish.Count),
                StrategyDebugLogger.F("fishTarget", targetFish),
                StrategyDebugLogger.F("fishCap", MaxFishPopulation),
                StrategyDebugLogger.F("fishShoalCap", MaxFishPerShoal),
                StrategyDebugLogger.F("fishShoals", spawnedFishShoals),
                StrategyDebugLogger.F("lakeFishRegions", lakeFishRegions.Count),
                StrategyDebugLogger.F("lakeFishCapacity", lakeFishCapacity),
                StrategyDebugLogger.F("riverRouteCells", riverRouteCells.Count),
                StrategyDebugLogger.F("riverFishCap", MaxRiverFishPopulation),
                StrategyDebugLogger.F("birds", spawnedBirds),
                StrategyDebugLogger.F("birdTarget", targetBirds),
                StrategyDebugLogger.F("seed", map.ActiveSeed),
                StrategyDebugLogger.F("hasCampAvoidance", hasCampCell),
                StrategyDebugLogger.F("spawnPlacement", "hidden_near_settlement"),
                StrategyDebugLogger.F("hasFog", fog != null),
                StrategyDebugLogger.F("playerFogEnabled", fog != null && fog.IsPlayerFogEnabled));
        }

        public bool TryReserveRabbitForHunt(
            Vector2Int center,
            int radius,
            object owner,
            out StrategyRabbitAgent rabbit,
            System.Predicate<StrategyRabbitAgent> candidateFilter = null)
        {
            rabbit = null;
            if (owner == null || map == null)
            {
                return false;
            }

            RemoveMissingRabbits();
            float bestSqr = float.MaxValue;
            StrategyRabbitAgent best = null;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent candidate = rabbits[i];
                if (candidate == null
                    || !candidate.CanBeHunted
                    || !candidate.TryGetCurrentCell(out Vector2Int cell)
                    || !StrategyWildlifeRiverCrossing.IsLandCell(map, cell)
                    || (candidateFilter != null && !candidateFilter(candidate)))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null || !best.TryReserveForHunt(owner))
            {
                return false;
            }

            rabbit = best;
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitReservedForHunt",
                StrategyDebugLogger.F("campCenter", center),
                StrategyDebugLogger.F("radius", radius),
                StrategyDebugLogger.F("rabbitWorld", rabbit.transform.position));
            return true;
        }

        public int CountHuntableRabbits(Vector2Int center, int radius)
        {
            if (map == null)
            {
                return 0;
            }

            RemoveMissingRabbits();
            int count = 0;
            float radiusSqr = radius * radius;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent rabbit = rabbits[i];
                if (rabbit == null || !rabbit.CanBeHunted || !rabbit.TryGetCurrentCell(out Vector2Int cell) || !StrategyWildlifeRiverCrossing.IsLandCell(map, cell))
                {
                    continue;
                }

                if ((cell - center).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

    }
}
