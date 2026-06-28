using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {

        private bool TryPickMigrationStep(
            Vector2Int currentCenter,
            Vector2Int target,
            int maxStep,
            System.Func<Vector2Int, bool> isCandidate,
            System.Func<Vector2Int, float> scoreCandidate,
            bool requireWalkableConnection,
            out Vector2Int stepCell)
        {
            stepCell = currentCenter;
            float currentDistance = Vector2Int.Distance(currentCenter, target);
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int y = -maxStep; y <= maxStep; y++)
            {
                for (int x = -maxStep; x <= maxStep; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    Vector2Int candidate = currentCenter + new Vector2Int(x, y);
                    float stepDistance = Vector2Int.Distance(currentCenter, candidate);
                    if (stepDistance > maxStep || !isCandidate(candidate))
                    {
                        continue;
                    }

                    if (requireWalkableConnection
                        && !HasWalkableMigrationConnection(currentCenter, candidate, maxStep * maxStep + 24))
                    {
                        continue;
                    }

                    float targetDistance = Vector2Int.Distance(candidate, target);
                    float progress = currentDistance - targetDistance;
                    if (progress < -0.25f)
                    {
                        continue;
                    }

                    float score = progress * 8f
                        - targetDistance * 0.18f
                        + scoreCandidate(candidate)
                        + Random.value * 0.18f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        stepCell = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private bool HasWalkableMigrationConnection(Vector2Int start, Vector2Int target, int maxVisited)
        {
            if (start == target)
            {
                return true;
            }

            if (map == null
                || !IsLandWildlifeTravelCell(start, true)
                || !IsLandWildlifeTargetCell(target))
            {
                return false;
            }

            Queue<Vector2Int> frontier = new();
            HashSet<Vector2Int> visited = new();
            bool allowStructureBuffer = IsLandWildlifeStructureBufferCell(start);
            frontier.Enqueue(start);
            visited.Add(start);

            while (frontier.Count > 0 && visited.Count < maxVisited)
            {
                Vector2Int current = frontier.Dequeue();
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (visited.Contains(next) || !IsLandWildlifeTravelCell(next, allowStructureBuffer))
                    {
                        continue;
                    }

                    if (next == target)
                    {
                        return true;
                    }

                    visited.Add(next);
                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        private void ApplyDeerHerdCenter(int herdId, Vector2Int center)
        {
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent != null && agent.HerdId == herdId)
                {
                    agent.RetargetHerdCenter(center, HerdHomeRadius);
                }
            }
        }

        private void ApplyRabbitGroupCenter(int groupId, Vector2Int center)
        {
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent agent = rabbits[i];
                if (agent != null && agent.GroupId == groupId)
                {
                    agent.RetargetGroupCenter(center, RabbitHomeRadius);
                }
            }
        }

        private void ApplyFishShoalCenter(int shoalId, Vector2Int center)
        {
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent agent = fish[i];
                if (agent != null && agent.ShoalId == shoalId)
                {
                    agent.RetargetShoalCenter(center, FishHomeRadius);
                }
            }
        }

        private void ApplyWolfPackCenter(StrategyWolfPack pack, Vector2Int center)
        {
            if (pack == null)
            {
                return;
            }

            pack.SetRoamCenter(center);
            IReadOnlyList<StrategyWolfAgent> members = pack.Members;
            for (int i = 0; i < members.Count; i++)
            {
                members[i]?.RetargetPackCenter(center, WolfHomeRadius);
            }
        }

        public int CountCatchableFish(Vector2Int center, int radius)
        {
            if (map == null)
            {
                return 0;
            }

            RemoveMissingFish();
            int count = 0;
            float radiusSqr = radius * radius;
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent candidate = fish[i];
                if (candidate == null || !candidate.CanBeFished || !candidate.TryGetCurrentCell(out Vector2Int cell))
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

        private int GenerateRabbits(int targetRabbits, HashSet<Vector2Int> usedCells)
        {
            int targetGroups = Mathf.Clamp(Mathf.CeilToInt(targetRabbits / 2.35f), 5, MaxRabbitGroups);
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
                int maxThisGroup = Mathf.Min(MaxRabbitsPerGroup, remaining - reserveForLater);
                int groupSize = Mathf.Clamp(
                    2 + (Hash(map.ActiveSeed, group, 97, 149, 211) % 2),
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

        private int GenerateWolves(int targetPacks, HashSet<Vector2Int> usedCells)
        {
            int spawnedPacks = 0;
            for (int packIndex = 0; packIndex < targetPacks; packIndex++)
            {
                if (!TryFindWolfPackCenter(packIndex, usedCells, GetPreferredWolfRiverSide(packIndex), out Vector2Int packCenter))
                {
                    StrategyDebugLogger.Warn(
                        "Wildlife",
                        "WolfPackSpawnSkipped",
                        StrategyDebugLogger.F("pack", packIndex),
                        StrategyDebugLogger.F("reason", "no_safe_center"));
                    continue;
                }

                StrategyWolfPack pack = new StrategyWolfPack(packIndex, packCenter, WolfHomeRadius);
                wolfPacks.Add(pack);
                int packSize = Mathf.Clamp(
                    WolfPackMinSize + (Hash(map.ActiveSeed, packIndex, 173, 229, 281) % (WolfPackMaxSize - WolfPackMinSize + 1)),
                    WolfPackMinSize,
                    WolfPackMaxSize);
                int spawnedInPack = 0;
                for (int slot = 0; slot < packSize; slot++)
                {
                    if (!TryFindWolfSpawnCell(packCenter, packIndex, slot, usedCells, out Vector2Int spawnCell))
                    {
                        continue;
                    }

                    SpawnWolf(pack, packCenter, spawnCell, slot);
                    usedCells.Add(spawnCell);
                    spawnedInPack++;
                }

                if (spawnedInPack <= 0)
                {
                    wolfPacks.Remove(pack);
                    continue;
                }

                spawnedPacks++;
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "WolfPackSpawned",
                    StrategyDebugLogger.F("pack", packIndex),
                    StrategyDebugLogger.F("wolves", spawnedInPack),
                    StrategyDebugLogger.F("center", packCenter));
            }

            return spawnedPacks;
        }

        private int GenerateFish(int targetFish)
        {
            int totalLakeCapacity = GetTotalLakeFishCapacity();
            if (lakeFishRegions.Count <= 0 || totalLakeCapacity <= 0)
            {
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "LakeFishInitialSpawnSkipped",
                    StrategyDebugLogger.F("reason", "no lake regions"),
                    StrategyDebugLogger.F("target", targetFish));
                return 0;
            }

            int targetLakeFish = Mathf.Min(targetFish, totalLakeCapacity);
            int targetShoals = Mathf.Clamp(
                Mathf.CeilToInt(targetLakeFish / 2.5f),
                1,
                Mathf.Min(MaxFishShoals, Mathf.Max(1, totalLakeCapacity)));
            HashSet<Vector2Int> usedWaterCells = new();
            int remaining = targetLakeFish;
            int spawnedShoals = 0;

            for (int shoal = 0; shoal < targetShoals && remaining > 0; shoal++)
            {
                FishWaterRegion region = PickLakeFishRegionForShoal(shoal);
                if (region == null)
                {
                    continue;
                }

                int regionRoom = region.Capacity - CountFishInLakeRegion(region.Id);
                if (regionRoom <= 0)
                {
                    continue;
                }

                if (!TryFindFishShoalCenter(region, shoal, usedWaterCells, out Vector2Int shoalCenter))
                {
                    continue;
                }

                int shoalsLeft = targetShoals - shoal;
                int reserveForLater = Mathf.Max(0, (shoalsLeft - 1) * 2);
                int maxThisShoal = Mathf.Min(MaxFishPerShoal, regionRoom, Mathf.Max(1, remaining - reserveForLater));
                int shoalSize = Mathf.Clamp(
                    2 + (Hash(map.ActiveSeed, shoal, 173, 197, 223) % 2),
                    1,
                    Mathf.Max(1, maxThisShoal));
                StrategyFishSpecies species = PickFishSpecies(shoal, shoalCenter);
                int spawnedInShoal = 0;

                for (int slot = 0; slot < shoalSize && remaining > 0; slot++)
                {
                    if (!TryFindFishSpawnCell(region, shoalCenter, shoal, slot, usedWaterCells, out Vector2Int spawnCell))
                    {
                        continue;
                    }

                    SpawnFish(
                        species,
                        shoal,
                        shoalCenter,
                        spawnCell,
                        StrategyFishLifeStage.Adult,
                        0f,
                        StrategyFishHabitatKind.Lake,
                        region.Id);
                    usedWaterCells.Add(spawnCell);
                    remaining--;
                    spawnedInShoal++;
                }

                if (spawnedInShoal > 0)
                {
                    spawnedShoals++;
                }
            }

            return spawnedShoals;
        }

        private void BuildFishWaterRegions()
        {
            lakeFishRegions.Clear();
            lakeRegionByCell.Clear();
            riverRouteCells.Clear();

            BuildLakeFishRegions();
            BuildRiverFishRoute();

            StrategyDebugLogger.Info(
                "Wildlife",
                "FishWaterRegionsBuilt",
                StrategyDebugLogger.F("lakeRegions", lakeFishRegions.Count),
                StrategyDebugLogger.F("lakeCapacity", GetTotalLakeFishCapacity()),
                StrategyDebugLogger.F("riverRouteCells", riverRouteCells.Count),
                StrategyDebugLogger.F("riverFlow", map != null ? map.RiverFlowDirection : Vector2Int.zero));
        }

        private void BuildLakeFishRegions()
        {
            if (map == null)
            {
                return;
            }

            bool[,] visited = new bool[map.Width, map.Height];
            int nextRegionId = 0;
            Queue<Vector2Int> open = new();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (visited[x, y] || !IsWaterCellOfKind(new Vector2Int(x, y), CityMapWaterKind.Lake))
                    {
                        continue;
                    }

                    FishWaterRegion region = new FishWaterRegion
                    {
                        Id = nextRegionId++
                    };
                    open.Clear();
                    open.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    while (open.Count > 0)
                    {
                        Vector2Int current = open.Dequeue();
                        region.Cells.Add(current);
                        lakeRegionByCell[current] = region.Id;

                        for (int i = 0; i < CardinalDirections.Length; i++)
                        {
                            Vector2Int next = current + CardinalDirections[i];
                            if (next.x < 0
                                || next.x >= map.Width
                                || next.y < 0
                                || next.y >= map.Height
                                || visited[next.x, next.y]
                                || !IsWaterCellOfKind(next, CityMapWaterKind.Lake))
                            {
                                continue;
                            }

                            visited[next.x, next.y] = true;
                            open.Enqueue(next);
                        }
                    }

                    if (region.Cells.Count <= 0)
                    {
                        continue;
                    }

                    region.Center = PickWaterRegionCenter(region.Cells);
                    region.Capacity = Mathf.Clamp(
                        Mathf.CeilToInt(region.Cells.Count / (float)LakeFishCellsPerCapacity),
                        LakeFishRegionMinCap,
                        LakeFishRegionMaxCap);
                    lakeFishRegions.Add(region);
                }
            }
        }
    }
}
