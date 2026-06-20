using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private const int MaxClayDeposits = 150;
        private const int MinimumClayDeposits = 28;
        private const int ClayWaterSearchRadius = 4;
        private const int ClaySortingOrder = StrategyWorldSorting.WaterOverlayOrder + 1;

        private int spawnedClayDeposits;
        private int spawnedClayPatches;
        private int spawnedClayBanks;

        private bool TryPlaceClayForCell(CityMapCell cell)
        {
            if (clay == null
                || spawnedProps >= MaxNatureProps
                || spawnedClayDeposits >= MaxClayDeposits
                || !IsClayAllowedKind(cell.Kind)
                || !IsClayNearWater(new Vector2Int(cell.X, cell.Y), Vector2Int.one, ClayWaterSearchRadius))
            {
                return false;
            }

            float score = GetClayScore(cell);
            float roll = Hash01(map.ActiveSeed, cell.X, cell.Y, 4301);
            if (score > 0.78f && roll < GetClayChance(cell.Kind, 0.060f))
            {
                Vector2Int bankFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 4307) > 0.56f
                    ? new Vector2Int(3, 2)
                    : new Vector2Int(2, 2);
                return TryCreateClayDeposit(
                    cell,
                    bankFootprint,
                    StrategyNaturePropKind.ClayBank,
                    StrategyClayDepositKind.ClayBank,
                    4311,
                    0.86f,
                    1.10f,
                    34,
                    64);
            }

            if (score > 0.52f && roll < GetClayChance(cell.Kind, 0.112f))
            {
                Vector2Int patchFootprint = Hash01(map.ActiveSeed, cell.X, cell.Y, 4313) > 0.58f
                    ? new Vector2Int(2, 2)
                    : new Vector2Int(2, 1);
                return TryCreateClayDeposit(
                    cell,
                    patchFootprint,
                    StrategyNaturePropKind.ClayPatch,
                    StrategyClayDepositKind.ClayPatch,
                    4319,
                    0.82f,
                    1.14f,
                    14,
                    32);
            }

            return false;
        }

        private bool TryCreateClayDeposit(
            CityMapCell cell,
            Vector2Int footprint,
            StrategyNaturePropKind propKind,
            StrategyClayDepositKind depositKind,
            int salt,
            float minScale,
            float maxScale,
            int minClay,
            int maxClay)
        {
            Vector2Int origin = new Vector2Int(cell.X, cell.Y);
            if (!CanPlaceClayFootprint(origin, footprint))
            {
                return false;
            }

            int variant = Hash(map.ActiveSeed, cell.X, cell.Y, salt, 17) % StrategyNatureSpriteFactory.GetVariantCount(propKind);
            Vector2 jitter = GetJitter(cell.X, cell.Y, salt) * 0.22f;
            Bounds bounds = map.GetCellRectWorld(origin, footprint);
            GameObject prop = new GameObject(depositKind.ToString());
            prop.transform.SetParent(propRoot, false);
            prop.transform.position = new Vector3(
                bounds.center.x + jitter.x * map.CellSize,
                bounds.center.y + jitter.y * map.CellSize,
                -0.146f);

            float scale = Mathf.Lerp(minScale, maxScale, Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 17));
            prop.transform.localScale = GetMineralVisualScale(scale, footprint);

            SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyNatureSpriteFactory.GetSprite(propKind, variant);
            renderer.color = Color.white;
            renderer.sortingOrder = ClaySortingOrder;
            renderer.flipX = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 23) > 0.5f;

            int amountRange = Mathf.Max(1, maxClay - minClay + 1);
            int clayAmount = minClay + (Hash(map.ActiveSeed, cell.X, cell.Y, salt + 31, maxClay) % amountRange);
            clay.RegisterGeneratedDeposit(prop, origin, footprint, depositKind, clayAmount);

            spawnedProps++;
            spawnedClayDeposits++;
            if (depositKind == StrategyClayDepositKind.ClayBank)
            {
                spawnedClayBanks++;
            }
            else
            {
                spawnedClayPatches++;
            }

            return true;
        }

        private bool CanPlaceClayFootprint(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    int cellX = origin.x + x;
                    int cellY = origin.y + y;
                    if (IsInsideExclusion(cellX, cellY)
                        || !map.TryGetCell(cellX, cellY, out CityMapCell cell)
                        || !IsClayAllowedKind(cell.Kind)
                        || !map.IsCellBuildable(cellX, cellY)
                        || !map.IsCellWalkable(cellX, cellY))
                    {
                        return false;
                    }
                }
            }

            return IsClayNearWater(origin, footprint, ClayWaterSearchRadius)
                && !HasClayDepositNearFootprint(origin, footprint, 1)
                && !HasIronDepositNearFootprint(origin, footprint, 1)
                && !HasCoalDepositNearFootprint(origin, footprint, 1);
        }

        private void EnsureMinimumClayDeposits()
        {
            if (clay == null || spawnedClayDeposits >= MinimumClayDeposits)
            {
                return;
            }

            int attempts = Mathf.Max(256, map.Width * map.Height);
            for (int i = 0; i < attempts
                && spawnedClayDeposits < MinimumClayDeposits
                && spawnedProps < MaxNatureProps
                && spawnedClayDeposits < MaxClayDeposits; i++)
            {
                int x = Hash(map.ActiveSeed, i, 0, 4401, map.Width) % map.Width;
                int y = Hash(map.ActiveSeed, i, 0, 4407, map.Height) % map.Height;
                if (!map.TryGetCell(x, y, out CityMapCell cell)
                    || !IsClayAllowedKind(cell.Kind)
                    || IsInsideExclusion(x, y)
                    || !map.IsCellWalkable(x, y)
                    || !IsClayNearWater(new Vector2Int(x, y), Vector2Int.one, ClayWaterSearchRadius))
                {
                    continue;
                }

                float score = GetClayScore(cell);
                if (score > 0.68f && Hash01(map.ActiveSeed, x, y, 4413) > 0.58f)
                {
                    Vector2Int footprint = Hash01(map.ActiveSeed, x, y, 4419) > 0.62f
                        ? new Vector2Int(3, 2)
                        : new Vector2Int(2, 2);
                    if (TryCreateClayDeposit(
                        cell,
                        footprint,
                        StrategyNaturePropKind.ClayBank,
                        StrategyClayDepositKind.ClayBank,
                        4427,
                        0.86f,
                        1.08f,
                        28,
                        58))
                    {
                        continue;
                    }
                }

                Vector2Int patchFootprint = Hash01(map.ActiveSeed, x, y, 4431) > 0.52f
                    ? new Vector2Int(2, 2)
                    : new Vector2Int(2, 1);
                TryCreateClayDeposit(
                    cell,
                    patchFootprint,
                    StrategyNaturePropKind.ClayPatch,
                    StrategyClayDepositKind.ClayPatch,
                    4433,
                    0.82f,
                    1.12f,
                    12,
                    28);
            }

            if (spawnedClayDeposits < MinimumClayDeposits)
            {
                StrategyDebugLogger.Warn(
                    "Clay",
                    "MinimumDepositFallbackShort",
                    StrategyDebugLogger.F("deposits", spawnedClayDeposits),
                    StrategyDebugLogger.F("minimum", MinimumClayDeposits),
                    StrategyDebugLogger.F("attempts", attempts));
            }
        }

        private float GetClayScore(CityMapCell cell)
        {
            float wetBed = Mathf.PerlinNoise(
                map.ActiveSeed * 0.0217f + cell.X * 0.061f,
                map.ActiveSeed * 0.0173f + cell.Y * 0.061f);
            float bankNoise = Mathf.PerlinNoise(
                map.ActiveSeed * 0.041f + cell.X * 0.118f,
                map.ActiveSeed * 0.035f + cell.Y * 0.118f);
            float bank = Mathf.Abs(bankNoise - 0.48f) * 2f;
            float waterBoost = GetClayWaterBoost(new Vector2Int(cell.X, cell.Y));
            return Mathf.Clamp01(wetBed * 0.44f + bank * 0.24f + waterBoost + GetClayTerrainBias(cell.Kind));
        }

        private float GetClayWaterBoost(Vector2Int cell)
        {
            int nearest = FindNearestWaterDistance(cell, ClayWaterSearchRadius);
            if (nearest < 0)
            {
                return -1f;
            }

            return Mathf.Lerp(0.22f, 0.04f, nearest / (float)ClayWaterSearchRadius);
        }

        private bool IsClayNearWater(Vector2Int origin, Vector2Int footprint, int radius)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    if (FindNearestWaterDistance(new Vector2Int(origin.x + x, origin.y + y), radius) < 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private int FindNearestWaterDistance(Vector2Int cell, int radius)
        {
            int safeRadius = Mathf.Max(1, radius);
            int best = int.MaxValue;
            for (int y = -safeRadius; y <= safeRadius; y++)
            {
                for (int x = -safeRadius; x <= safeRadius; x++)
                {
                    int distance = Mathf.Abs(x) + Mathf.Abs(y);
                    if (distance > safeRadius || distance >= best)
                    {
                        continue;
                    }

                    if (map.TryGetCell(cell.x + x, cell.y + y, out CityMapCell neighbor)
                        && (neighbor.IsWater || neighbor.IsShore))
                    {
                        best = distance;
                    }
                }
            }

            return best == int.MaxValue ? -1 : best;
        }

        private static float GetClayChance(CityMapCellKind kind, float baseChance)
        {
            float multiplier = kind switch
            {
                CityMapCellKind.Shore => 1.85f,
                CityMapCellKind.Dirt => 1.25f,
                CityMapCellKind.Meadow => 0.82f,
                CityMapCellKind.Grass => 0.72f,
                _ => 0f
            };
            return baseChance * multiplier;
        }

        private static float GetClayTerrainBias(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Shore => 0.20f,
                CityMapCellKind.Dirt => 0.10f,
                CityMapCellKind.Meadow => 0.00f,
                CityMapCellKind.Grass => -0.04f,
                _ => -1f
            };
        }

        private static bool IsClayAllowedKind(CityMapCellKind kind)
        {
            return kind == CityMapCellKind.Shore
                || kind == CityMapCellKind.Dirt
                || kind == CityMapCellKind.Meadow
                || kind == CityMapCellKind.Grass;
        }

        private bool HasClayDepositNearFootprint(Vector2Int origin, Vector2Int footprint, int buffer)
        {
            if (clay == null)
            {
                return false;
            }

            IReadOnlyList<StrategyClayDeposit> deposits = clay.Deposits;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyClayDeposit deposit = deposits[i];
                if (deposit != null && RectanglesOverlap(origin, footprint, deposit.Cell, deposit.Footprint, buffer))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
