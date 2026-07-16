using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private void ClearProps()
        {
            seasonTintTargets.Clear();
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

        private bool TryCreateIronDeposit(
            CityMapCell cell,
            Vector2Int footprint,
            StrategyNaturePropKind propKind,
            StrategyIronDepositKind depositKind,
            int salt,
            float minScale,
            float maxScale,
            int minIron,
            int maxIron,
            bool countsTowardsNatureBudget = true)
        {
            Vector2Int origin = new Vector2Int(cell.X, cell.Y);
            if ((countsTowardsNatureBudget && spawnedProps >= MaxNatureProps)
                || !CanPlaceIronFootprint(origin, footprint))
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

            if (countsTowardsNatureBudget)
            {
                spawnedProps++;
            }

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
                        || !map.IsCellWalkable(cellX, cellY)
                        || HasRouteRoadAt(cellX, cellY))
                    {
                        return false;
                    }
                }
            }

            return !HasIronDepositNearFootprint(origin, footprint, 0)
                && !HasCoalDepositNearFootprint(origin, footprint, 1);
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
