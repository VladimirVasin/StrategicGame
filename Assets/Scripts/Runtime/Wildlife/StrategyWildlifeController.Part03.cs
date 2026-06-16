using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private void BuildRiverFishRoute()
        {
            if (map == null)
            {
                return;
            }

            List<Vector2Int> riverCells = new();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (IsWaterCellOfKind(cell, CityMapWaterKind.River))
                    {
                        riverCells.Add(cell);
                    }
                }
            }

            if (riverCells.Count <= 0)
            {
                return;
            }

            Vector2Int flow = map.RiverFlowDirection;
            bool horizontal = Mathf.Abs(flow.x) >= Mathf.Abs(flow.y);
            int flowSign = horizontal
                ? flow.x >= 0 ? 1 : -1
                : flow.y >= 0 ? 1 : -1;
            BuildRiverRouteByAxis(riverCells, horizontal, flowSign);
        }

        private void BuildRiverRouteByAxis(List<Vector2Int> riverCells, bool horizontal, int flowSign)
        {
            int minAlong = int.MaxValue;
            int maxAlong = int.MinValue;
            for (int i = 0; i < riverCells.Count; i++)
            {
                int along = horizontal ? riverCells[i].x : riverCells[i].y;
                minAlong = Mathf.Min(minAlong, along);
                maxAlong = Mathf.Max(maxAlong, along);
            }

            if (minAlong > maxAlong)
            {
                return;
            }

            int step = flowSign >= 0 ? 1 : -1;
            int start = flowSign >= 0 ? minAlong : maxAlong;
            int end = flowSign >= 0 ? maxAlong : minAlong;
            for (int along = start; flowSign >= 0 ? along <= end : along >= end; along += step)
            {
                int count = 0;
                float acrossSum = 0f;
                for (int i = 0; i < riverCells.Count; i++)
                {
                    Vector2Int cell = riverCells[i];
                    if ((horizontal ? cell.x : cell.y) != along)
                    {
                        continue;
                    }

                    acrossSum += horizontal ? cell.y : cell.x;
                    count++;
                }

                if (count <= 0)
                {
                    continue;
                }

                int targetAcross = Mathf.RoundToInt(acrossSum / count);
                Vector2Int best = default;
                int bestDistance = int.MaxValue;
                bool found = false;
                for (int i = 0; i < riverCells.Count; i++)
                {
                    Vector2Int cell = riverCells[i];
                    if ((horizontal ? cell.x : cell.y) != along)
                    {
                        continue;
                    }

                    int across = horizontal ? cell.y : cell.x;
                    int distance = Mathf.Abs(across - targetAcross);
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    best = cell;
                    found = true;
                }

                if (found && (riverRouteCells.Count <= 0 || riverRouteCells[riverRouteCells.Count - 1] != best))
                {
                    riverRouteCells.Add(best);
                }
            }
        }

        private Vector2Int PickWaterRegionCenter(List<Vector2Int> cells)
        {
            if (cells == null || cells.Count <= 0)
            {
                return Vector2Int.zero;
            }

            float sumX = 0f;
            float sumY = 0f;
            for (int i = 0; i < cells.Count; i++)
            {
                sumX += cells[i].x;
                sumY += cells[i].y;
            }

            Vector2 average = new Vector2(sumX / cells.Count, sumY / cells.Count);
            Vector2Int best = cells[0];
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                float distance = Vector2.Distance(cell, average);
                float score = CountWaterNeighbors(cell, 2, CityMapWaterKind.Lake) * 0.45f - distance;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }

            return best;
        }

        private FishWaterRegion PickLakeFishRegionForShoal(int shoal)
        {
            if (lakeFishRegions.Count <= 0)
            {
                return null;
            }

            int start = Hash(map.ActiveSeed, shoal, 811, 853, 907) % lakeFishRegions.Count;
            for (int offset = 0; offset < lakeFishRegions.Count; offset++)
            {
                FishWaterRegion region = lakeFishRegions[(start + offset) % lakeFishRegions.Count];
                if (CountFishInLakeRegion(region.Id) < region.Capacity)
                {
                    return region;
                }
            }

            return null;
        }

        private int GetTotalLakeFishCapacity()
        {
            int capacity = 0;
            for (int i = 0; i < lakeFishRegions.Count; i++)
            {
                capacity += lakeFishRegions[i].Capacity;
            }

            return capacity;
        }

        private int CountFishInLakeRegion(int regionId)
        {
            int count = 0;
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null
                    && agent.IsLakeFish
                    && !agent.IsCaught
                    && agent.WaterRegionId == regionId)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetLakeRegion(int regionId, out FishWaterRegion region)
        {
            for (int i = 0; i < lakeFishRegions.Count; i++)
            {
                if (lakeFishRegions[i].Id == regionId)
                {
                    region = lakeFishRegions[i];
                    return true;
                }
            }

            region = null;
            return false;
        }

        private void UpdateRiverFishSpawning(float elapsedSeconds)
        {
            if (map == null || wildlifeRoot == null || riverRouteCells.Count < 2)
            {
                return;
            }

            RemoveMissingFish();
            riverFishSpawnTimer -= elapsedSeconds;
            if (riverFishSpawnTimer > 0f)
            {
                return;
            }

            int activeRiverFish = CountRiverFish();
            if (activeRiverFish < MaxRiverFishPopulation)
            {
                TrySpawnRiverFish(activeRiverFish);
            }

            riverFishSpawnTimer = Random.Range(RiverFishSpawnIntervalMin, RiverFishSpawnIntervalMax);
        }

        private int CountRiverFish()
        {
            int count = 0;
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null && agent.IsRiverFish && !agent.IsCaught)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TrySpawnRiverFish(int activeRiverFish)
        {
            Vector2Int spawnCell = riverRouteCells[0];
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent agent = fish[i];
                if (agent == null
                    || !agent.IsRiverFish
                    || !agent.TryGetCurrentCell(out Vector2Int cell)
                    || Vector2Int.Distance(cell, spawnCell) >= 5f)
                {
                    continue;
                }

                return false;
            }

            int shoalId = nextRiverShoalId++;
            StrategyFishSpecies species = PickFishSpecies(shoalId, spawnCell);
            List<Vector3> route = BuildRiverFishWorldRoute(shoalId);
            if (route.Count < 2)
            {
                return false;
            }

            float speedMultiplier = 0.88f + Hash01(map.ActiveSeed, shoalId, route.Count, 1201) * 0.24f;
            SpawnFish(
                species,
                shoalId,
                spawnCell,
                spawnCell,
                StrategyFishLifeStage.Adult,
                0f,
                StrategyFishHabitatKind.River,
                -1,
                route,
                speedMultiplier);

            StrategyDebugLogger.Info(
                "Wildlife",
                "RiverFishSpawned",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("activeRiverFish", activeRiverFish + 1),
                StrategyDebugLogger.F("riverFishCap", MaxRiverFishPopulation),
                StrategyDebugLogger.F("routeCells", riverRouteCells.Count),
                StrategyDebugLogger.F("spawnCell", spawnCell),
                StrategyDebugLogger.F("endCell", riverRouteCells[riverRouteCells.Count - 1]),
                StrategyDebugLogger.F("flow", map.RiverFlowDirection));
            return true;
        }

        private List<Vector3> BuildRiverFishWorldRoute(int routeSeed)
        {
            List<Vector3> route = new(riverRouteCells.Count);
            Vector2Int flow = map != null ? map.RiverFlowDirection : Vector2Int.right;
            bool horizontal = Mathf.Abs(flow.x) >= Mathf.Abs(flow.y);
            for (int i = 0; i < riverRouteCells.Count; i++)
            {
                Vector2Int cell = riverRouteCells[i];
                Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
                float jitter = (Hash01(map.ActiveSeed, routeSeed + i, cell.x + cell.y, 1301) - 0.5f) * map.CellSize * 0.44f;
                if (horizontal)
                {
                    world.y += jitter;
                }
                else
                {
                    world.x += jitter;
                }

                route.Add(new Vector3(world.x, world.y, -0.068f));
            }

            return route;
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
                this,
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
                this,
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
    }
}
