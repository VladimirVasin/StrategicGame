using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {

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

        private static void AddStaticInspectable(GameObject prop, SpriteRenderer renderer, StrategyNaturePropKind kind, CityMapCell cell)
        {
            if (prop == null
                || renderer == null
                || kind == StrategyNaturePropKind.LargeTree
                || kind == StrategyNaturePropKind.SmallTree
                || kind == StrategyNaturePropKind.Boulder
                || kind == StrategyNaturePropKind.RockCluster
                || kind == StrategyNaturePropKind.Cliff)
            {
                return;
            }

            StrategyStaticWorldInspectable inspectable = prop.GetComponent<StrategyStaticWorldInspectable>();
            if (inspectable == null)
            {
                inspectable = prop.AddComponent<StrategyStaticWorldInspectable>();
            }

            string title = kind == StrategyNaturePropKind.ForestGroup ? "Forest Thicket" : "Bush";
            string body = "Nature prop"
                + "\nBlocks movement: yes"
                + "\nCell terrain: "
                + cell.Kind;
            inspectable.Configure(
                title,
                "Nature",
                body,
                renderer.sprite,
                new Vector2Int(cell.X, cell.Y),
                true);
        }

        private static void AttachNatureShadow(SpriteRenderer renderer, StrategyNaturePropKind kind, float propScale)
        {
            if (renderer == null
                || kind == StrategyNaturePropKind.LargeTree
                || kind == StrategyNaturePropKind.SmallTree
                || kind == StrategyNaturePropKind.Boulder
                || kind == StrategyNaturePropKind.RockCluster
                || kind == StrategyNaturePropKind.Cliff)
            {
                return;
            }

            if (kind == StrategyNaturePropKind.Bush)
            {
                StrategyShadowCaster2D.Attach(
                    renderer,
                    StrategyShadowShape.SoftEllipse,
                    new Vector2(0.04f, -0.02f),
                    new Vector2(0.36f * propScale, 0.13f * propScale),
                    0.19f,
                    -4,
                    0f,
                    false);
                return;
            }

            StrategyShadowCaster2D.Attach(
                renderer,
                StrategyShadowShape.CastOval,
                new Vector2(0.14f, -0.07f),
                new Vector2(0.72f * propScale, 0.24f * propScale),
                0.23f,
                -5,
                -7f,
                true);
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
    }
}
