using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyForestryController : MonoBehaviour
    {
        private const float RejectedPlantingCellCooldownSeconds = 18f;
        private const float RejectedPlantingCellLogCooldownSeconds = 4f;

        private readonly List<StrategyForestryTree> trees = new();
        private readonly Dictionary<Vector2Int, float> rejectedPlantingCellUntil = new();
        private readonly List<Vector2Int> rejectedPlantingCellCleanup = new();
        private CityMapController map;
        private StrategyWindController wind;
        private Transform plantedRoot;
        private float nextRejectedPlantingCellLogTime;

        public static StrategyForestryController Active { get; private set; }
        public IReadOnlyList<StrategyForestryTree> Trees => trees;

        public void Configure(CityMapController mapController, StrategyWindController windController)
        {
            map = mapController;
            wind = windController;
            Active = this;
            EnsurePlantedRoot();
        }

        public void RegisterTree(StrategyForestryTree tree)
        {
            if (tree == null || trees.Contains(tree))
            {
                return;
            }

            trees.Add(tree);
        }

        public void UnregisterTree(StrategyForestryTree tree)
        {
            if (tree == null)
            {
                return;
            }

            trees.Remove(tree);
        }

        public void RegisterGeneratedTree(
            GameObject treeObject,
            SpriteRenderer renderer,
            Vector2Int cell,
            int variant,
            bool isLargeTree)
        {
            if (treeObject == null || map == null)
            {
                return;
            }

            StrategyForestryTree tree = treeObject.GetComponent<StrategyForestryTree>();
            if (tree == null)
            {
                tree = treeObject.AddComponent<StrategyForestryTree>();
            }

            int logYield = isLargeTree ? StrategyForestryTree.LargeTreeLogs : StrategyForestryTree.SmallTreeLogs;
            tree.Configure(this, map, wind, cell, 2, variant, renderer, false, isLargeTree, logYield);
        }

        public bool TryFindMatureTree(Vector2Int center, int radius, out StrategyForestryTree tree)
        {
            PruneNulls();
            float bestSqr = float.MaxValue;
            StrategyForestryTree best = null;

            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree candidate = trees[i];
                if (candidate == null
                    || !candidate.CanBeChopped
                    || candidate.IsReserved)
                {
                    continue;
                }

                float sqr = (candidate.Cell - center).sqrMagnitude;
                if (sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null)
            {
                tree = null;
                return false;
            }

            tree = best;
            return true;
        }

        public bool TryFindProcessableWood(Vector2Int center, int radius, out StrategyForestryTree tree)
        {
            PruneNulls();
            float bestSqr = float.MaxValue;
            StrategyForestryTree best = null;

            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree candidate = trees[i];
                if (candidate == null
                    || candidate.IsReserved
                    || (!candidate.CanBeBucked && !candidate.HasLogsReady))
                {
                    continue;
                }

                float sqr = (candidate.Cell - center).sqrMagnitude;
                if (sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null)
            {
                tree = null;
                return false;
            }

            tree = best;
            return true;
        }

        public bool TryFindPlantingCell(Vector2Int center, int radius, out Vector2Int cell)
        {
            PruneRejectedPlantingCells();
            List<Vector2Int> candidates = new();
            int radiusSqr = radius * radius;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    Vector2Int candidate = center + new Vector2Int(x, y);
                    if ((candidate - center).sqrMagnitude > radiusSqr
                        || !IsPlantingCell(candidate)
                        || IsPlantingCellTemporarilyRejected(candidate))
                    {
                        continue;
                    }

                    candidates.Add(candidate);
                }
            }

            if (candidates.Count <= 0)
            {
                cell = default;
                return false;
            }

            cell = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        public void RegisterRejectedPlantingCell(Vector2Int cell, string reason)
        {
            rejectedPlantingCellUntil[cell] = Time.time + RejectedPlantingCellCooldownSeconds;
            if (Time.time < nextRejectedPlantingCellLogTime)
            {
                return;
            }

            nextRejectedPlantingCellLogTime = Time.time + RejectedPlantingCellLogCooldownSeconds;
            StrategyDebugLogger.Info(
                "Forestry",
                "PlantingCellTemporarilyRejected",
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cooldownSeconds", RejectedPlantingCellCooldownSeconds));
        }

        public bool TryPlantTree(Vector2Int cell)
        {
            if (!IsPlantingCell(cell))
            {
                return false;
            }

            EnsurePlantedRoot();

            Vector3 center = map.GetCellCenterWorld(cell.x, cell.y);
            GameObject treeObject = new GameObject("Planted Tree");
            treeObject.transform.SetParent(plantedRoot, false);
            treeObject.transform.position = new Vector3(center.x, center.y, -0.11f);
            treeObject.transform.localScale = Vector3.one;

            int variant = Random.Range(0, StrategyNatureSpriteFactory.GetVariantCount(StrategyNaturePropKind.LargeTree));
            SpriteRenderer renderer = treeObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyNatureSpriteFactory.GetTreeGrowthSprite(0, variant);
            renderer.color = Color.white;
            StrategyWorldSorting.Apply(renderer, treeObject.transform.position);

            StrategyForestryTree tree = treeObject.AddComponent<StrategyForestryTree>();
            tree.Configure(
                this,
                map,
                wind,
                cell,
                0,
                variant,
                renderer,
                true,
                true,
                StrategyForestryTree.LargeTreeLogs);
            return true;
        }

        public int CountMatureTrees(Vector2Int center, int radius)
        {
            return CountTrees(center, radius, true);
        }

        public int CountGrowingTrees(Vector2Int center, int radius)
        {
            return CountTrees(center, radius, false);
        }

        public int CountProcessableWood(Vector2Int center, int radius)
        {
            PruneNulls();
            int count = 0;
            int radiusSqr = radius * radius;
            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree tree = trees[i];
                if (tree == null || (tree.Cell - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (tree.CanBeBucked || tree.HasLogsReady)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountTrees(Vector2Int center, int radius, bool mature)
        {
            PruneNulls();
            int count = 0;
            int radiusSqr = radius * radius;
            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree tree = trees[i];
                if (tree == null || (tree.Cell - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                bool matches = mature ? tree.CanBeChopped : !tree.IsMature;
                if (matches)
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsPlantingCell(Vector2Int cell)
        {
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && IsPlantableKind(mapCell.Kind)
                && map.IsCellWalkable(cell)
                && !HasTreeAt(cell);
        }

        private bool HasTreeAt(Vector2Int cell)
        {
            PruneNulls();
            for (int i = 0; i < trees.Count; i++)
            {
                StrategyForestryTree tree = trees[i];
                if (tree != null && tree.Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPlantingCellTemporarilyRejected(Vector2Int cell)
        {
            if (!rejectedPlantingCellUntil.TryGetValue(cell, out float rejectedUntil))
            {
                return false;
            }

            if (rejectedUntil > Time.time)
            {
                return true;
            }

            rejectedPlantingCellUntil.Remove(cell);
            return false;
        }

        private void PruneRejectedPlantingCells()
        {
            rejectedPlantingCellCleanup.Clear();
            foreach (KeyValuePair<Vector2Int, float> pair in rejectedPlantingCellUntil)
            {
                if (pair.Value <= Time.time)
                {
                    rejectedPlantingCellCleanup.Add(pair.Key);
                }
            }

            for (int i = 0; i < rejectedPlantingCellCleanup.Count; i++)
            {
                rejectedPlantingCellUntil.Remove(rejectedPlantingCellCleanup[i]);
            }
        }

        private void EnsurePlantedRoot()
        {
            if (plantedRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Planted Forestry Trees");
            root.transform.SetParent(transform, false);
            plantedRoot = root.transform;
        }

        private void PruneNulls()
        {
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                if (trees[i] == null)
                {
                    trees.RemoveAt(i);
                }
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private static bool IsPlantableKind(CityMapCellKind kind)
        {
            return kind == CityMapCellKind.Grass
                || kind == CityMapCellKind.Meadow
                || kind == CityMapCellKind.Forest
                || kind == CityMapCellKind.Dirt;
        }
    }
}
