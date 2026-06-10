using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyNaturePropController : MonoBehaviour
    {
        private const int MaxNatureProps = 3600;
        private const int MaxStoneDeposits = 440;
        private const int MinimumStoneDeposits = 112;
        private const int StarterStoneMinimumDeposits = 5;
        private const int StarterStoneMinDistance = 4;
        private const int StarterStoneMaxDistance = StrategyStonecutterCamp.WorkRadius;
        private const int TreeSortingOrder = 3;
        private const int ForestSortingOrder = 3;
        private const int BushSortingOrder = 2;
        private const int StoneSortingOrder = 2;

        private CityMapController map;
        private StrategyWindController wind;
        private StrategyForestryController forestry;
        private StrategyStoneResourceController stone;
        private Transform propRoot;
        private Vector2Int excludedCenter;
        private int excludedRadius;
        private bool hasExclusion;
        private int spawnedProps;
        private int spawnedStoneDeposits;
        private int spawnedBoulders;
        private int spawnedRockClusters;
        private int spawnedCliffs;
        private int stoneBlockedCells;

        public void Configure(CityMapController mapController)
        {
            Configure(mapController, StrategyWindController.Active, StrategyForestryController.Active, StrategyStoneResourceController.Active);
        }

        public void Configure(CityMapController mapController, StrategyWindController windController)
        {
            Configure(mapController, windController, StrategyForestryController.Active, StrategyStoneResourceController.Active);
        }

        public void Configure(
            CityMapController mapController,
            StrategyWindController windController,
            StrategyForestryController forestryController)
        {
            Configure(mapController, windController, forestryController, StrategyStoneResourceController.Active);
        }

        public void Configure(
            CityMapController mapController,
            StrategyWindController windController,
            StrategyForestryController forestryController,
            StrategyStoneResourceController stoneController)
        {
            ConfigureInternal(mapController, windController, forestryController, stoneController, default, 0, false);
        }

        public void Configure(
            CityMapController mapController,
            StrategyWindController windController,
            Vector2Int natureExcludedCenter,
            int natureExcludedRadius)
        {
            Configure(mapController, windController, StrategyForestryController.Active, natureExcludedCenter, natureExcludedRadius);
        }

        public void Configure(
            CityMapController mapController,
            StrategyWindController windController,
            StrategyForestryController forestryController,
            Vector2Int natureExcludedCenter,
            int natureExcludedRadius)
        {
            Configure(
                mapController,
                windController,
                forestryController,
                StrategyStoneResourceController.Active,
                natureExcludedCenter,
                natureExcludedRadius);
        }

        public void Configure(
            CityMapController mapController,
            StrategyWindController windController,
            StrategyForestryController forestryController,
            StrategyStoneResourceController stoneController,
            Vector2Int natureExcludedCenter,
            int natureExcludedRadius)
        {
            ConfigureInternal(
                mapController,
                windController,
                forestryController,
                stoneController,
                natureExcludedCenter,
                Mathf.Max(0, natureExcludedRadius),
                true);
        }

        private void ConfigureInternal(
            CityMapController mapController,
            StrategyWindController windController,
            StrategyForestryController forestryController,
            StrategyStoneResourceController stoneController,
            Vector2Int natureExcludedCenter,
            int natureExcludedRadius,
            bool useExclusion)
        {
            map = mapController;
            wind = windController;
            forestry = forestryController;
            stone = stoneController;
            excludedCenter = natureExcludedCenter;
            excludedRadius = natureExcludedRadius;
            hasExclusion = useExclusion;
            EnsurePropRoot();
            GenerateNature();
        }

        public void GenerateNature()
        {
            if (map == null)
            {
                return;
            }

            EnsurePropRoot();
            ClearProps();
            spawnedProps = 0;
            spawnedStoneDeposits = 0;
            spawnedBoulders = 0;
            spawnedRockClusters = 0;
            spawnedCliffs = 0;
            stoneBlockedCells = 0;

            EnsureStarterStoneDeposits();

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (spawnedProps >= MaxNatureProps
                        || IsInsideExclusion(x, y)
                        || !map.TryGetCell(x, y, out CityMapCell cell))
                    {
                        continue;
                    }

                    PlaceNatureForCell(cell);
                }
            }

            EnsureMinimumStoneDeposits();

            StrategyDebugLogger.Info(
                "Nature",
                "Generated",
                StrategyDebugLogger.F("props", spawnedProps),
                StrategyDebugLogger.F("max", MaxNatureProps),
                StrategyDebugLogger.F("stoneDeposits", spawnedStoneDeposits),
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
        }

        private void PlaceNatureForCell(CityMapCell cell)
        {
            if (!map.IsCellWalkable(cell.X, cell.Y))
            {
                return;
            }

            if (TryPlaceStoneForCell(cell))
            {
                return;
            }

            float roll = Hash01(map.ActiveSeed, cell.X, cell.Y, 11);
            switch (cell.Kind)
            {
                case CityMapCellKind.Forest:
                    PlaceForestNature(cell, roll);
                    break;
                case CityMapCellKind.Meadow:
                    PlaceMeadowNature(cell, roll);
                    break;
                case CityMapCellKind.Grass:
                    PlaceGrassNature(cell, roll);
                    break;
                case CityMapCellKind.Dirt:
                    PlaceDirtNature(cell, roll);
                    break;
                case CityMapCellKind.Shore:
                    PlaceShoreNature(cell, roll);
                    break;
            }
        }

        private void PlaceForestNature(CityMapCell cell, float roll)
        {
            int forestNeighbors = CountSameNeighbors(cell.X, cell.Y, CityMapCellKind.Forest);
            if (forestNeighbors >= 4 && roll < 0.26f)
            {
                CreateProp(cell, StrategyNaturePropKind.ForestGroup, 31, 0.88f, 1.08f, ForestSortingOrder);
                return;
            }

            if (roll < 0.64f)
            {
                CreateProp(cell, StrategyNaturePropKind.LargeTree, 37, 0.86f, 1.16f, TreeSortingOrder);
                return;
            }

            if (roll < 0.86f)
            {
                CreateProp(cell, StrategyNaturePropKind.SmallTree, 41, 0.82f, 1.12f, TreeSortingOrder);
                return;
            }

            if (roll < 0.94f)
            {
                CreateProp(cell, StrategyNaturePropKind.Bush, 43, 0.86f, 1.18f, BushSortingOrder);
            }
        }

        private void PlaceMeadowNature(CityMapCell cell, float roll)
        {
            if (roll < 0.018f)
            {
                CreateProp(cell, StrategyNaturePropKind.SmallTree, 53, 0.84f, 1.08f, TreeSortingOrder);
                return;
            }

            if (roll < 0.105f)
            {
                CreateProp(cell, StrategyNaturePropKind.Bush, 59, 0.78f, 1.08f, BushSortingOrder);
            }
        }

        private void PlaceGrassNature(CityMapCell cell, float roll)
        {
            if (roll < 0.018f)
            {
                CreateProp(cell, StrategyNaturePropKind.LargeTree, 61, 0.84f, 1.04f, TreeSortingOrder);
                return;
            }

            if (roll < 0.055f)
            {
                CreateProp(cell, StrategyNaturePropKind.SmallTree, 67, 0.80f, 1.04f, TreeSortingOrder);
                return;
            }

            if (roll < 0.095f)
            {
                CreateProp(cell, StrategyNaturePropKind.Bush, 71, 0.76f, 1.04f, BushSortingOrder);
            }
        }

        private void PlaceDirtNature(CityMapCell cell, float roll)
        {
            if (roll < 0.022f)
            {
                CreateProp(cell, StrategyNaturePropKind.Bush, 73, 0.70f, 0.96f, BushSortingOrder);
            }
        }

        private void PlaceShoreNature(CityMapCell cell, float roll)
        {
            if (roll < 0.016f)
            {
                CreateProp(cell, StrategyNaturePropKind.Bush, 79, 0.66f, 0.92f, BushSortingOrder);
            }
        }

        private void CreateProp(
            CityMapCell cell,
            StrategyNaturePropKind kind,
            int salt,
            float minScale,
            float maxScale,
            int sortingOrder)
        {
            if (spawnedProps >= MaxNatureProps)
            {
                return;
            }

            int variant = Hash(map.ActiveSeed, cell.X, cell.Y, salt, 5) % StrategyNatureSpriteFactory.GetVariantCount(kind);
            Vector2 jitter = GetJitter(cell.X, cell.Y, salt);
            Vector3 center = map.GetCellCenterWorld(cell.X, cell.Y);
            GameObject prop = new GameObject(kind.ToString());
            prop.transform.SetParent(propRoot, false);
            prop.transform.position = new Vector3(
                center.x + jitter.x * map.CellSize,
                center.y + jitter.y * map.CellSize,
                -0.11f);

            float scale = Mathf.Lerp(minScale, maxScale, Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 17));
            prop.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyNatureSpriteFactory.GetSprite(kind, variant);
            renderer.color = Color.white;
            StrategyWorldSorting.Apply(renderer, prop.transform.position);
            renderer.flipX = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 23) > 0.5f;

            AddWindSway(prop, kind, cell, salt, scale);
            AddFrameAnimation(prop, renderer, kind, variant, cell, salt);
            RegisterForestryTree(prop, renderer, kind, cell, variant);
            BlockNatureWalkability(kind, cell);
            spawnedProps++;
        }

        private bool TryPlaceStoneForCell(CityMapCell cell)
        {
            if (stone == null
                || spawnedProps >= MaxNatureProps
                || spawnedStoneDeposits >= MaxStoneDeposits
                || !IsStoneAllowedKind(cell.Kind))
            {
                return false;
            }

            float score = GetStoneScore(cell);
            float roll = Hash01(map.ActiveSeed, cell.X, cell.Y, 809);

            if (score > 0.76f && roll < GetStoneChance(cell.Kind, 0.032f))
            {
                Vector2Int cliffFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 811) > 0.42f
                    ? new Vector2Int(3, 2)
                    : new Vector2Int(2, 2);
                if (TryCreateStoneDeposit(
                    cell,
                    cliffFootprint,
                    StrategyNaturePropKind.Cliff,
                    StrategyStoneDepositKind.Cliff,
                    53,
                    0.92f,
                    1.06f,
                    58,
                    88))
                {
                    return true;
                }
            }

            if (score > 0.64f && roll < GetStoneChance(cell.Kind, 0.085f))
            {
                Vector2Int clusterFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 823) > 0.62f
                    ? new Vector2Int(2, 2)
                    : new Vector2Int(2, 1);
                if (TryCreateStoneDeposit(
                    cell,
                    clusterFootprint,
                    StrategyNaturePropKind.RockCluster,
                    StrategyStoneDepositKind.RockCluster,
                    59,
                    0.86f,
                    1.08f,
                    28,
                    44))
                {
                    return true;
                }
            }

            if (score > 0.50f && roll < GetStoneChance(cell.Kind, 0.155f))
            {
                return TryCreateStoneDeposit(
                    cell,
                    Vector2Int.one,
                    StrategyNaturePropKind.Boulder,
                    StrategyStoneDepositKind.Boulder,
                    61,
                    0.82f,
                    1.10f,
                    9,
                    16);
            }

            return false;
        }

        private void EnsureMinimumStoneDeposits()
        {
            if (stone == null || spawnedStoneDeposits >= MinimumStoneDeposits)
            {
                return;
            }

            int attempts = Mathf.Max(256, map.Width * map.Height * 2);
            for (int i = 0; i < attempts
                && spawnedStoneDeposits < MinimumStoneDeposits
                && spawnedProps < MaxNatureProps
                && spawnedStoneDeposits < MaxStoneDeposits; i++)
            {
                int x = Hash(map.ActiveSeed, i, 0, 1901, map.Width) % map.Width;
                int y = Hash(map.ActiveSeed, i, 0, 1907, map.Height) % map.Height;
                if (!map.TryGetCell(x, y, out CityMapCell cell)
                    || !IsStoneAllowedKind(cell.Kind)
                    || IsInsideExclusion(x, y)
                    || !map.IsCellWalkable(x, y))
                {
                    continue;
                }

                float score = GetStoneScore(cell);
                float clusterRoll = Hash01(map.ActiveSeed, x, y, 1913);
                if (score > 0.73f && clusterRoll > 0.82f)
                {
                    Vector2Int footprint = Hash01(map.ActiveSeed, x, y, 1919) > 0.58f
                        ? new Vector2Int(2, 2)
                        : new Vector2Int(2, 1);
                    if (TryCreateStoneDeposit(
                        cell,
                        footprint,
                        StrategyNaturePropKind.RockCluster,
                        StrategyStoneDepositKind.RockCluster,
                        1931,
                        0.86f,
                        1.08f,
                        28,
                        44))
                    {
                        continue;
                    }
                }

                TryCreateStoneDeposit(
                    cell,
                    Vector2Int.one,
                    StrategyNaturePropKind.Boulder,
                    StrategyStoneDepositKind.Boulder,
                    1937,
                    0.82f,
                    1.10f,
                    9,
                    16);
            }

            if (spawnedStoneDeposits < MinimumStoneDeposits)
            {
                StrategyDebugLogger.Warn(
                    "Stone",
                    "MinimumDepositFallbackShort",
                    StrategyDebugLogger.F("deposits", spawnedStoneDeposits),
                    StrategyDebugLogger.F("minimum", MinimumStoneDeposits),
                    StrategyDebugLogger.F("attempts", attempts));
            }
        }

        private void EnsureStarterStoneDeposits()
        {
            if (stone == null || !hasExclusion)
            {
                return;
            }

            int existingNearby = stone.CountAvailableDeposits(excludedCenter, StarterStoneMaxDistance);
            int needed = Mathf.Max(0, StarterStoneMinimumDeposits - existingNearby);
            int created = 0;
            for (int i = 0; i < needed; i++)
            {
                if (!TryFindStarterStoneCell(i, out CityMapCell cell))
                {
                    break;
                }

                if (TryCreateStarterStoneDeposit(cell, i))
                {
                    created++;
                }
            }

            int totalNearby = stone.CountAvailableDeposits(excludedCenter, StarterStoneMaxDistance);
            if (totalNearby < StarterStoneMinimumDeposits)
            {
                StrategyDebugLogger.Warn(
                    "Stone",
                    "StarterStoneFallbackShort",
                    StrategyDebugLogger.F("campCell", excludedCenter),
                    StrategyDebugLogger.F("created", created),
                    StrategyDebugLogger.F("nearby", totalNearby),
                    StrategyDebugLogger.F("minimum", StarterStoneMinimumDeposits),
                    StrategyDebugLogger.F("radius", StarterStoneMaxDistance));
                return;
            }

            StrategyDebugLogger.Info(
                "Stone",
                "StarterStoneReady",
                StrategyDebugLogger.F("campCell", excludedCenter),
                StrategyDebugLogger.F("created", created),
                StrategyDebugLogger.F("nearby", totalNearby),
                StrategyDebugLogger.F("radius", StarterStoneMaxDistance));
        }

        private bool TryFindStarterStoneCell(int placementIndex, out CityMapCell cell)
        {
            cell = default;
            List<CityMapCell> candidates = new();
            int minDistance = Mathf.Max(StarterStoneMinDistance, excludedRadius + 1);
            int minDistanceSqr = minDistance * minDistance;
            int maxDistanceSqr = StarterStoneMaxDistance * StarterStoneMaxDistance;

            for (int radius = minDistance; radius <= StarterStoneMaxDistance; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        int distanceSqr = x * x + y * y;
                        if (distanceSqr < minDistanceSqr || distanceSqr > maxDistanceSqr)
                        {
                            continue;
                        }

                        Vector2Int candidateCell = excludedCenter + new Vector2Int(x, y);
                        if (!map.TryGetCell(candidateCell.x, candidateCell.y, out CityMapCell candidate)
                            || !IsStoneAllowedKind(candidate.Kind)
                            || IsInsideExclusion(candidateCell.x, candidateCell.y)
                            || !map.IsCellWalkable(candidateCell)
                            || !HasAdjacentWalkableCell(candidateCell, Vector2Int.one))
                        {
                            continue;
                        }

                        candidates.Add(candidate);
                    }
                }

                if (candidates.Count > 0)
                {
                    int index = Hash(map.ActiveSeed, placementIndex, radius, 2027, candidates.Count) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            return false;
        }

        private bool TryCreateStarterStoneDeposit(CityMapCell cell, int placementIndex)
        {
            bool preferCluster = placementIndex % 3 == 1;
            if (preferCluster)
            {
                Vector2Int clusterFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 2053 + placementIndex) > 0.58f
                    ? new Vector2Int(2, 2)
                    : new Vector2Int(2, 1);
                if (HasAdjacentWalkableCell(new Vector2Int(cell.X, cell.Y), clusterFootprint)
                    && TryCreateStoneDeposit(
                        cell,
                        clusterFootprint,
                        StrategyNaturePropKind.RockCluster,
                        StrategyStoneDepositKind.RockCluster,
                        2063 + placementIndex * 7,
                        0.88f,
                        1.08f,
                        30,
                        46))
                {
                    return true;
                }
            }

            return TryCreateStoneDeposit(
                cell,
                Vector2Int.one,
                StrategyNaturePropKind.Boulder,
                StrategyStoneDepositKind.Boulder,
                2081 + placementIndex * 7,
                0.86f,
                1.12f,
                12,
                18);
        }

        private bool TryCreateStoneDeposit(
            CityMapCell cell,
            Vector2Int footprint,
            StrategyNaturePropKind propKind,
            StrategyStoneDepositKind depositKind,
            int salt,
            float minScale,
            float maxScale,
            int minStone,
            int maxStone)
        {
            Vector2Int origin = new Vector2Int(cell.X, cell.Y);
            if (!CanPlaceStoneFootprint(origin, footprint))
            {
                return false;
            }

            int variant = Hash(map.ActiveSeed, cell.X, cell.Y, salt, 7) % StrategyNatureSpriteFactory.GetVariantCount(propKind);
            Vector2 jitter = GetJitter(cell.X, cell.Y, salt) * 0.45f;
            Bounds bounds = map.GetCellRectWorld(origin, footprint);
            GameObject prop = new GameObject(depositKind.ToString());
            prop.transform.SetParent(propRoot, false);
            prop.transform.position = new Vector3(
                bounds.center.x + jitter.x * map.CellSize,
                bounds.center.y + jitter.y * map.CellSize,
                -0.12f);

            float scale = Mathf.Lerp(minScale, maxScale, Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 17));
            prop.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyNatureSpriteFactory.GetSprite(propKind, variant);
            renderer.color = Color.white;
            StrategyWorldSorting.Apply(renderer, prop.transform.position);
            renderer.flipX = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 23) > 0.5f;

            int amountRange = Mathf.Max(1, maxStone - minStone + 1);
            int stoneAmount = minStone + (Hash(map.ActiveSeed, cell.X, cell.Y, salt + 31, maxStone) % amountRange);
            stone.RegisterGeneratedDeposit(prop, origin, footprint, depositKind, stoneAmount);

            spawnedProps++;
            spawnedStoneDeposits++;
            stoneBlockedCells += footprint.x * footprint.y;
            switch (depositKind)
            {
                case StrategyStoneDepositKind.Boulder:
                    spawnedBoulders++;
                    break;
                case StrategyStoneDepositKind.RockCluster:
                    spawnedRockClusters++;
                    break;
                case StrategyStoneDepositKind.Cliff:
                    spawnedCliffs++;
                    break;
            }

            return true;
        }

        private bool HasAdjacentWalkableCell(Vector2Int origin, Vector2Int footprint)
        {
            for (int radius = 1; radius <= 2; radius++)
            {
                for (int y = -radius; y < footprint.y + radius; y++)
                {
                    for (int x = -radius; x < footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == footprint.x + radius - 1
                            || y == footprint.y + radius - 1;
                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool CanPlaceStoneFootprint(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    int cellX = origin.x + x;
                    int cellY = origin.y + y;
                    if (IsInsideExclusion(cellX, cellY)
                        || !map.TryGetCell(cellX, cellY, out CityMapCell cell)
                        || !IsStoneAllowedKind(cell.Kind)
                        || !map.IsCellWalkable(cellX, cellY))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void RegisterForestryTree(
            GameObject prop,
            SpriteRenderer renderer,
            StrategyNaturePropKind kind,
            CityMapCell cell,
            int variant)
        {
            if (forestry == null
                || (kind != StrategyNaturePropKind.LargeTree && kind != StrategyNaturePropKind.SmallTree))
            {
                return;
            }

            forestry.RegisterGeneratedTree(prop, renderer, new Vector2Int(cell.X, cell.Y), variant);
        }

        private void BlockNatureWalkability(StrategyNaturePropKind kind, CityMapCell cell)
        {
            if (map == null || (kind != StrategyNaturePropKind.ForestGroup && kind != StrategyNaturePropKind.Bush))
            {
                return;
            }

            map.SetCellsWalkable(new Vector2Int(cell.X, cell.Y), Vector2Int.one, false);
        }

        private void AddFrameAnimation(
            GameObject prop,
            SpriteRenderer renderer,
            StrategyNaturePropKind kind,
            int variant,
            CityMapCell cell,
            int salt)
        {
            if (prop == null || renderer == null)
            {
                return;
            }

            StrategyNatureFrameAnimator animator = prop.AddComponent<StrategyNatureFrameAnimator>();
            float phase = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 509) * Mathf.PI * 2f;
            animator.Configure(renderer, kind, variant, phase);
        }

        private void AddWindSway(GameObject prop, StrategyNaturePropKind kind, CityMapCell cell, int salt, float scale)
        {
            if (prop == null)
            {
                return;
            }

            StrategyWindSway sway = prop.AddComponent<StrategyWindSway>();
            float phase = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 307) * Mathf.PI * 2f;
            float bendDegrees = kind switch
            {
                StrategyNaturePropKind.LargeTree => 2.3f,
                StrategyNaturePropKind.SmallTree => 3.0f,
                StrategyNaturePropKind.ForestGroup => 1.8f,
                StrategyNaturePropKind.Bush => 1.15f,
                _ => 1.5f
            };
            float offsetAmplitude = kind == StrategyNaturePropKind.Bush ? 0.010f : 0.018f;
            float stretchAmplitude = kind == StrategyNaturePropKind.Bush ? 0.006f : 0.010f;
            float scaleFactor = Mathf.Clamp(1.15f - scale * 0.18f, 0.75f, 1.15f);
            sway.Configure(
                wind,
                phase,
                bendDegrees * scaleFactor,
                offsetAmplitude * scaleFactor,
                stretchAmplitude);
        }

        private float GetStoneScore(CityMapCell cell)
        {
            float broad = Mathf.PerlinNoise(
                map.ActiveSeed * 0.0137f + cell.X * 0.071f,
                map.ActiveSeed * 0.0173f + cell.Y * 0.071f);
            float vein = Mathf.PerlinNoise(
                map.ActiveSeed * 0.0311f + cell.X * 0.173f,
                map.ActiveSeed * 0.0277f + cell.Y * 0.173f);
            float ridge = Mathf.Abs(vein - 0.5f) * 2f;
            return Mathf.Clamp01(broad * 0.66f + ridge * 0.22f + GetStoneTerrainBias(cell.Kind));
        }

        private static float GetStoneChance(CityMapCellKind kind, float baseChance)
        {
            float multiplier = kind switch
            {
                CityMapCellKind.Dirt => 1.38f,
                CityMapCellKind.Shore => 1.05f,
                CityMapCellKind.Grass => 1.0f,
                CityMapCellKind.Meadow => 0.82f,
                CityMapCellKind.Forest => 0.58f,
                _ => 0f
            };
            return baseChance * multiplier;
        }

        private static float GetStoneTerrainBias(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Dirt => 0.12f,
                CityMapCellKind.Shore => 0.06f,
                CityMapCellKind.Grass => 0.02f,
                CityMapCellKind.Meadow => -0.03f,
                CityMapCellKind.Forest => -0.08f,
                _ => -1f
            };
        }

        private static bool IsStoneAllowedKind(CityMapCellKind kind)
        {
            return kind == CityMapCellKind.Grass
                || kind == CityMapCellKind.Meadow
                || kind == CityMapCellKind.Forest
                || kind == CityMapCellKind.Dirt
                || kind == CityMapCellKind.Shore;
        }

        private int CountSameNeighbors(int x, int y, CityMapCellKind kind)
        {
            int count = 0;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    if (map.TryGetCell(x + ox, y + oy, out CityMapCell cell) && cell.Kind == kind)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private bool IsInsideExclusion(int x, int y)
        {
            return hasExclusion
                && Mathf.Abs(x - excludedCenter.x) <= excludedRadius
                && Mathf.Abs(y - excludedCenter.y) <= excludedRadius;
        }

        private Vector2 GetJitter(int x, int y, int salt)
        {
            float jitterX = Hash01(map.ActiveSeed, x, y, salt + 101) - 0.5f;
            float jitterY = Hash01(map.ActiveSeed, x, y, salt + 103) - 0.5f;
            return new Vector2(jitterX * 0.44f, jitterY * 0.36f);
        }

        private void EnsurePropRoot()
        {
            if (propRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Nature Props");
            root.transform.SetParent(transform, false);
            propRoot = root.transform;
        }

        private void ClearProps()
        {
            if (propRoot == null)
            {
                return;
            }

            for (int i = propRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = propRoot.GetChild(i);
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
