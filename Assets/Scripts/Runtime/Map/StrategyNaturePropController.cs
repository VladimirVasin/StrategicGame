using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyNaturePropController : MonoBehaviour
    {
        private const int MaxNatureProps = 3600;
        private const int MaxStoneDeposits = 440;
        private const int MinimumStoneDeposits = 112;
        private const int MaxIronDeposits = 180;
        private const int MinimumIronDeposits = 48;
        private const int StarterStoneMinimumDeposits = 5;
        private const int StarterStoneMinDistance = 4;
        private const int StarterStoneMaxDistance = StrategyStonecutterCamp.WorkRadius;
        private const int StarterMineralMinimumDeposits = 2;
        private const int StarterMineralMinDistance = 10;
        private const int StarterMineralMaxDistance = 24;
        private const int TreeSortingOrder = 3;
        private const int ForestSortingOrder = 3;
        private const int BushSortingOrder = 2;
        private const int StoneSortingOrder = 2;
        private const int IronSortingOrder = StrategyWorldSorting.WaterOverlayOrder + 1;

        private CityMapController map;
        private StrategyWindController wind;
        private StrategyForestryController forestry;
        private StrategyStoneResourceController stone;
        private StrategyIronResourceController iron;
        private StrategyClayResourceController clay;
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
        private int spawnedIronDeposits;
        private int spawnedIronStainedGround;
        private int spawnedIronVeins;

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
            iron = StrategyIronResourceController.Active;
            coal = StrategyCoalResourceController.Active;
            clay = StrategyClayResourceController.Active;
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

            ResetNatureGeneration();
            GenerateGuaranteedNature();

            int totalCells = map.Width * map.Height;
            for (int i = 0; i < totalCells && spawnedProps < MaxNatureProps; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(map.ActiveSeed, i, totalCells, 701);
                int x = cellIndex % map.Width;
                int y = cellIndex / map.Width;
                if (IsInsideExclusion(x, y) || !map.TryGetCell(x, y, out CityMapCell cell))
                {
                    continue;
                }

                PlaceNatureForCell(cell);
            }

            LogNatureGeneration();
        }

        private void PlaceNatureForCell(CityMapCell cell)
        {
            if (!map.IsCellWalkable(cell.X, cell.Y)
                || !map.IsCellBuildable(cell.X, cell.Y)
                || HasRouteRoadAt(cell.X, cell.Y))
            {
                return;
            }

            if (TryPlaceStoneForCell(cell))
            {
                return;
            }

            if (TryPlaceClayForCell(cell))
            {
                return;
            }

            if (TryPlaceIronForCell(cell))
            {
                return;
            }

            if (TryPlaceCoalForCell(cell))
            {
                return;
            }

            float vegetationCluster = GetVegetationClusterScore(cell);
            float roll = StrategyMapDistributionUtility.ApplyClusterToRoll(
                Hash01(map.ActiveSeed, cell.X, cell.Y, 11),
                vegetationCluster,
                0.38f,
                1.48f);
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

        private void CreateProp(
            CityMapCell cell,
            StrategyNaturePropKind kind,
            int salt,
            float minScale,
            float maxScale,
            int sortingOrder)
        {
            if (spawnedProps >= MaxNatureProps || HasRouteRoadAt(cell.X, cell.Y))
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
            RegisterSeasonTintTarget(renderer, kind, variant);

            AttachNatureShadow(renderer, kind, scale);
            AddWindSway(prop, kind, cell, salt, scale);
            AddFrameAnimation(prop, renderer, kind, variant, cell, salt);
            RegisterForestryTree(prop, renderer, kind, cell, variant);
            BlockNatureWalkability(kind, cell);
            AddStaticInspectable(prop, renderer, kind, cell);
            spawnedProps++;
        }

        private static bool HasRouteRoadAt(int x, int y)
        {
            return StrategyTrailController.Active?.HasRouteRoadAt(new Vector2Int(x, y)) == true;
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

            float cluster = GetStoneClusterScore(cell);
            float score = Mathf.Clamp01(GetStoneScore(cell) * 0.82f + cluster * 0.24f);
            float roll = Hash01(map.ActiveSeed, cell.X, cell.Y, 809);

            if (score > 0.76f
                && roll < StrategyMapDistributionUtility.ApplyClusterToChance(GetStoneChance(cell.Kind, 0.032f), cluster, 0.24f, 2.10f))
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

            if (score > 0.64f
                && roll < StrategyMapDistributionUtility.ApplyClusterToChance(GetStoneChance(cell.Kind, 0.085f), cluster, 0.30f, 1.85f))
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

            if (score > 0.50f
                && roll < StrategyMapDistributionUtility.ApplyClusterToChance(GetStoneChance(cell.Kind, 0.155f), cluster, 0.34f, 1.65f))
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

            int totalCells = map.Width * map.Height;
            int attempts = Mathf.Max(256, totalCells);
            for (int i = 0; i < attempts
                && spawnedStoneDeposits < MinimumStoneDeposits
                && spawnedProps < MaxNatureProps
                && spawnedStoneDeposits < MaxStoneDeposits; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(map.ActiveSeed, i, totalCells, 1901);
                int x = cellIndex % map.Width;
                int y = cellIndex / map.Width;
                if (!map.TryGetCell(x, y, out CityMapCell cell)
                    || !IsStoneAllowedKind(cell.Kind)
                    || IsInsideExclusion(x, y)
                    || !map.IsCellWalkable(x, y))
                {
                    continue;
                }

                float score = Mathf.Clamp01(GetStoneScore(cell) * 0.82f + GetStoneClusterScore(cell) * 0.24f);
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
    }
}
