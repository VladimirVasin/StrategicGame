using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {

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

        private bool TryPlaceIronForCell(CityMapCell cell)
        {
            if (iron == null
                || spawnedProps >= MaxNatureProps
                || spawnedIronDeposits >= MaxIronDeposits
                || !IsIronAllowedKind(cell.Kind))
            {
                return false;
            }

            float cluster = GetIronClusterScore(cell);
            float score = Mathf.Clamp01(GetIronScore(cell) * 0.80f + cluster * 0.26f);
            float roll = Hash01(map.ActiveSeed, cell.X, cell.Y, 2701);

            if (score > 0.80f
                && roll < StrategyMapDistributionUtility.ApplyClusterToChance(GetIronChance(cell.Kind, 0.036f), cluster, 0.18f, 2.25f))
            {
                Vector2Int veinFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 2707) > 0.58f
                    ? new Vector2Int(3, 2)
                    : new Vector2Int(2, 2);
                return TryCreateIronDeposit(
                    cell,
                    veinFootprint,
                    StrategyNaturePropKind.IronVein,
                    StrategyIronDepositKind.IronVein,
                    2711,
                    0.88f,
                    1.08f,
                    42,
                    74);
            }

            if (score > 0.64f
                && roll < StrategyMapDistributionUtility.ApplyClusterToChance(GetIronChance(cell.Kind, 0.075f), cluster, 0.24f, 1.95f))
            {
                Vector2Int stainedFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 2713) > 0.52f
                    ? new Vector2Int(2, 2)
                    : new Vector2Int(2, 1);
                return TryCreateIronDeposit(
                    cell,
                    stainedFootprint,
                    StrategyNaturePropKind.IronStainedGround,
                    StrategyIronDepositKind.IronStainedGround,
                    2719,
                    0.82f,
                    1.14f,
                    18,
                    36);
            }

            return false;
        }

        private bool TryCreateIronDeposit(
            CityMapCell cell,
            Vector2Int footprint,
            StrategyNaturePropKind propKind,
            StrategyIronDepositKind depositKind,
            int salt,
            float minScale,
            float maxScale,
            int minIron,
            int maxIron)
        {
            Vector2Int origin = new Vector2Int(cell.X, cell.Y);
            if (!CanPlaceIronFootprint(origin, footprint))
            {
                return false;
            }

            int variant = Hash(map.ActiveSeed, cell.X, cell.Y, salt, 11) % StrategyNatureSpriteFactory.GetVariantCount(propKind);
            Vector2 jitter = GetJitter(cell.X, cell.Y, salt) * 0.24f;
            Bounds bounds = map.GetCellRectWorld(origin, footprint);
            GameObject prop = new GameObject(depositKind.ToString());
            prop.transform.SetParent(propRoot, false);
            prop.transform.position = new Vector3(
                bounds.center.x + jitter.x * map.CellSize,
                bounds.center.y + jitter.y * map.CellSize,
                -0.14f);

            float scale = Mathf.Lerp(minScale, maxScale, Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 17));
            prop.transform.localScale = GetMineralVisualScale(scale, footprint);

            SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyNatureSpriteFactory.GetSprite(propKind, variant);
            renderer.color = Color.white;
            renderer.sortingOrder = IronSortingOrder;
            renderer.flipX = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 23) > 0.5f;

            int amountRange = Mathf.Max(1, maxIron - minIron + 1);
            int ironAmount = minIron + (Hash(map.ActiveSeed, cell.X, cell.Y, salt + 31, maxIron) % amountRange);
            iron.RegisterGeneratedDeposit(prop, origin, footprint, depositKind, ironAmount);

            spawnedProps++;
            spawnedIronDeposits++;
            if (depositKind == StrategyIronDepositKind.IronVein)
            {
                spawnedIronVeins++;
            }
            else
            {
                spawnedIronStainedGround++;
            }

            return true;
        }

        private bool CanPlaceIronFootprint(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    int cellX = origin.x + x;
                    int cellY = origin.y + y;
                    if (IsInsideExclusion(cellX, cellY)
                        || !map.TryGetCell(cellX, cellY, out CityMapCell cell)
                        || !IsIronAllowedKind(cell.Kind)
                        || !map.IsCellBuildable(cellX, cellY)
                        || !map.IsCellWalkable(cellX, cellY))
                    {
                        return false;
                    }
                }
            }

            return !HasIronDepositNearFootprint(origin, footprint, 0)
                && !HasCoalDepositNearFootprint(origin, footprint, 1);
        }

        private void EnsureMinimumIronDeposits()
        {
            if (iron == null || spawnedIronDeposits >= MinimumIronDeposits)
            {
                return;
            }

            int totalCells = map.Width * map.Height;
            int attempts = Mathf.Max(256, totalCells);
            for (int i = 0; i < attempts
                && spawnedIronDeposits < MinimumIronDeposits
                && spawnedProps < MaxNatureProps
                && spawnedIronDeposits < MaxIronDeposits; i++)
            {
                int cellIndex = StrategyMapDistributionUtility.GetShuffledIndex(map.ActiveSeed, i, totalCells, 2801);
                int x = cellIndex % map.Width;
                int y = cellIndex / map.Width;
                if (!map.TryGetCell(x, y, out CityMapCell cell)
                    || !IsIronAllowedKind(cell.Kind)
                    || IsInsideExclusion(x, y)
                    || !map.IsCellWalkable(x, y))
                {
                    continue;
                }

                float score = Mathf.Clamp01(GetIronScore(cell) * 0.80f + GetIronClusterScore(cell) * 0.26f);
                if (score > 0.72f && Hash01(map.ActiveSeed, x, y, 2813) > 0.58f)
                {
                    Vector2Int footprint = Hash01(map.ActiveSeed, x, y, 2819) > 0.66f
                        ? new Vector2Int(3, 2)
                        : new Vector2Int(2, 2);
                    if (TryCreateIronDeposit(
                        cell,
                        footprint,
                        StrategyNaturePropKind.IronVein,
                        StrategyIronDepositKind.IronVein,
                        2827,
                        0.86f,
                        1.08f,
                        38,
                        68))
                    {
                        continue;
                    }
                }

                Vector2Int stainedFootprint = Hash01(map.ActiveSeed, x, y, 2831) > 0.52f
                    ? new Vector2Int(2, 2)
                    : new Vector2Int(2, 1);
                TryCreateIronDeposit(
                    cell,
                    stainedFootprint,
                    StrategyNaturePropKind.IronStainedGround,
                    StrategyIronDepositKind.IronStainedGround,
                    2833,
                    0.82f,
                    1.12f,
                    16,
                    32);
            }

            if (spawnedIronDeposits < MinimumIronDeposits)
            {
                StrategyDebugLogger.Warn(
                    "Iron",
                    "MinimumDepositFallbackShort",
                    StrategyDebugLogger.F("deposits", spawnedIronDeposits),
                    StrategyDebugLogger.F("minimum", MinimumIronDeposits),
                    StrategyDebugLogger.F("attempts", attempts));
            }
        }

        private float GetIronScore(CityMapCell cell)
        {
            float broad = Mathf.PerlinNoise(
                map.ActiveSeed * 0.0193f + cell.X * 0.046f,
                map.ActiveSeed * 0.0217f + cell.Y * 0.046f);
            float seamNoise = Mathf.PerlinNoise(
                map.ActiveSeed * 0.0371f + cell.X * 0.128f,
                map.ActiveSeed * 0.0419f + cell.Y * 0.128f);
            float seam = Mathf.Abs(seamNoise - 0.5f) * 2f;
            return Mathf.Clamp01(broad * 0.58f + seam * 0.32f + GetIronTerrainBias(cell.Kind));
        }

        private static float GetIronChance(CityMapCellKind kind, float baseChance)
        {
            float multiplier = kind switch
            {
                CityMapCellKind.Dirt => 1.45f,
                CityMapCellKind.Grass => 1.0f,
                CityMapCellKind.Meadow => 0.84f,
                CityMapCellKind.Forest => 0.58f,
                CityMapCellKind.Shore => 0.36f,
                _ => 0f
            };
            return baseChance * multiplier;
        }

        private static float GetIronTerrainBias(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Dirt => 0.15f,
                CityMapCellKind.Grass => 0.03f,
                CityMapCellKind.Meadow => -0.02f,
                CityMapCellKind.Forest => -0.06f,
                CityMapCellKind.Shore => -0.12f,
                _ => -1f
            };
        }

        private static bool IsIronAllowedKind(CityMapCellKind kind)
        {
            return kind == CityMapCellKind.Grass
                || kind == CityMapCellKind.Meadow
                || kind == CityMapCellKind.Forest
                || kind == CityMapCellKind.Dirt
                || kind == CityMapCellKind.Shore;
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
