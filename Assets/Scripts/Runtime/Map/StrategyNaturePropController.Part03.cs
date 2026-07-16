using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private const int CoalSortingOrder = StrategyWorldSorting.WaterOverlayOrder + 1;

        private StrategyCoalResourceController coal;
        private int spawnedCoalDeposits;
        private int spawnedCoalDustGround;
        private int spawnedCoalSeams;

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

        private bool TryCreateCoalDeposit(
            CityMapCell cell,
            Vector2Int footprint,
            StrategyNaturePropKind propKind,
            StrategyCoalDepositKind depositKind,
            int salt,
            float minScale,
            float maxScale,
            int minCoal,
            int maxCoal,
            bool countsTowardsNatureBudget = true)
        {
            Vector2Int origin = new Vector2Int(cell.X, cell.Y);
            if ((countsTowardsNatureBudget && spawnedProps >= MaxNatureProps)
                || !CanPlaceCoalFootprint(origin, footprint))
            {
                return false;
            }

            int variant = Hash(map.ActiveSeed, cell.X, cell.Y, salt, 13) % StrategyNatureSpriteFactory.GetVariantCount(propKind);
            Vector2 jitter = GetJitter(cell.X, cell.Y, salt) * 0.24f;
            Bounds bounds = map.GetCellRectWorld(origin, footprint);
            GameObject prop = new GameObject(depositKind.ToString());
            prop.transform.SetParent(propRoot, false);
            prop.transform.position = new Vector3(
                bounds.center.x + jitter.x * map.CellSize,
                bounds.center.y + jitter.y * map.CellSize,
                -0.145f);

            float scale = Mathf.Lerp(minScale, maxScale, Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 17));
            prop.transform.localScale = GetMineralVisualScale(scale, footprint);

            SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyNatureSpriteFactory.GetSprite(propKind, variant);
            renderer.color = Color.white;
            renderer.sortingOrder = CoalSortingOrder;
            renderer.flipX = Hash01(map.ActiveSeed, cell.X, cell.Y, salt + 23) > 0.5f;

            int amountRange = Mathf.Max(1, maxCoal - minCoal + 1);
            int coalAmount = minCoal + (Hash(map.ActiveSeed, cell.X, cell.Y, salt + 31, maxCoal) % amountRange);
            coal.RegisterGeneratedDeposit(prop, origin, footprint, depositKind, coalAmount);

            if (countsTowardsNatureBudget)
            {
                spawnedProps++;
            }

            spawnedCoalDeposits++;
            if (depositKind == StrategyCoalDepositKind.CoalSeam)
            {
                spawnedCoalSeams++;
            }
            else
            {
                spawnedCoalDustGround++;
            }

            return true;
        }

        private bool CanPlaceCoalFootprint(Vector2Int origin, Vector2Int footprint)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    int cellX = origin.x + x;
                    int cellY = origin.y + y;
                    if (IsInsideExclusion(cellX, cellY)
                        || !map.TryGetCell(cellX, cellY, out CityMapCell cell)
                        || !IsCoalAllowedKind(cell.Kind)
                        || !map.IsCellBuildable(cellX, cellY)
                        || !map.IsCellWalkable(cellX, cellY)
                        || HasRouteRoadAt(cellX, cellY))
                    {
                        return false;
                    }
                }
            }

            return !HasCoalDepositNearFootprint(origin, footprint, 0)
                && !HasIronDepositNearFootprint(origin, footprint, 1);
        }

        private static bool IsCoalAllowedKind(CityMapCellKind kind)
        {
            return kind == CityMapCellKind.Grass
                || kind == CityMapCellKind.Meadow
                || kind == CityMapCellKind.Forest
                || kind == CityMapCellKind.Dirt
                || kind == CityMapCellKind.Shore;
        }

        private static Vector3 GetMineralVisualScale(float baseScale, Vector2Int footprint)
        {
            float widthScale = Mathf.Lerp(1f, Mathf.Max(1f, footprint.x), 0.45f);
            float heightScale = Mathf.Lerp(1f, Mathf.Max(1f, footprint.y), 0.45f);
            return new Vector3(baseScale * widthScale, baseScale * heightScale, 1f);
        }

        private bool HasIronDepositNearFootprint(Vector2Int origin, Vector2Int footprint, int buffer)
        {
            if (iron == null)
            {
                return false;
            }

            IReadOnlyList<StrategyIronDeposit> deposits = iron.Deposits;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyIronDeposit deposit = deposits[i];
                if (deposit != null && RectanglesOverlap(origin, footprint, deposit.Cell, deposit.Footprint, buffer))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCoalDepositNearFootprint(Vector2Int origin, Vector2Int footprint, int buffer)
        {
            if (coal == null)
            {
                return false;
            }

            IReadOnlyList<StrategyCoalDeposit> deposits = coal.Deposits;
            for (int i = 0; i < deposits.Count; i++)
            {
                StrategyCoalDeposit deposit = deposits[i];
                if (deposit != null && RectanglesOverlap(origin, footprint, deposit.Cell, deposit.Footprint, buffer))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RectanglesOverlap(
            Vector2Int aOrigin,
            Vector2Int aSize,
            Vector2Int bOrigin,
            Vector2Int bSize,
            int buffer)
        {
            int safeBuffer = Mathf.Max(0, buffer);
            return aOrigin.x - safeBuffer < bOrigin.x + bSize.x
                && aOrigin.x + aSize.x + safeBuffer > bOrigin.x
                && aOrigin.y - safeBuffer < bOrigin.y + bSize.y
                && aOrigin.y + aSize.y + safeBuffer > bOrigin.y;
        }
    }
}
