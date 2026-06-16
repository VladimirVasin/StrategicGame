using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyTrailController : MonoBehaviour
    {
        private const float FootfallWear = 1f;
        private const float MaxWear = 100f;
        private const float FaintThreshold = 14f;
        private const float ClearThreshold = 34f;
        private const float WornThreshold = 68f;
        private const float TrailSpeedMultiplier = 1.15f;
        private const float TrailPathCostMultiplier = 0.68f;
        private const float DecayTickSeconds = 1.0f;
        private const float DecayGraceSeconds = 90f;
        private const float DecayWearPerSecond = 0.16f;
        private const int North = 1;
        private const int East = 2;
        private const int South = 4;
        private const int West = 8;
        private const int NorthEast = 16;
        private const int SouthEast = 32;
        private const int SouthWest = 64;
        private const int NorthWest = 128;

        private static readonly Vector2Int[] NeighborCells =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1)
        };

        private readonly Dictionary<int, SpriteRenderer> renderers = new();
        private readonly HashSet<int> activeWearCells = new();
        private readonly List<int> decayClearCells = new();
        private CityMapController map;
        private Transform visualRoot;
        private float[,] wear;
        private float[,] lastFootfallTimes;
        private byte[,] levels;
        private float decayTimer;

        public static StrategyTrailController Active { get; private set; }
        public float SpeedMultiplier => TrailSpeedMultiplier;
        public float PathCostMultiplier => TrailPathCostMultiplier;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            Active = this;
            EnsureStorage();
            EnsureVisualRoot();
            RefreshArea(Vector2Int.zero, map != null ? new Vector2Int(map.Width, map.Height) : Vector2Int.zero);
            StrategyDebugLogger.Info(
                "Map",
                "TrailsReady",
                StrategyDebugLogger.F("width", map != null ? map.Width : 0),
                StrategyDebugLogger.F("height", map != null ? map.Height : 0),
                StrategyDebugLogger.F("speedMultiplier", TrailSpeedMultiplier));
        }

        private void Update()
        {
            if (map == null || wear == null || lastFootfallTimes == null)
            {
                return;
            }

            decayTimer += Time.deltaTime;
            if (decayTimer < DecayTickSeconds)
            {
                return;
            }

            float elapsed = decayTimer;
            decayTimer = 0f;
            DecayOldTrails(elapsed);
            LogTrailStatsIfDue(elapsed);
        }

        public void RecordFootfall(Vector2Int cell)
        {
            RecordFootfall(cell, 1f);
        }

        public void RecordFootfall(Vector2Int cell, float weight)
        {
            if (weight <= 0f)
            {
                RecordRejectedFootfall(cell, weight, "non_positive_weight");
                return;
            }

            string rejectReason = GetWearRejectReason(cell);
            if (rejectReason != null)
            {
                RecordRejectedFootfall(cell, weight, rejectReason);
                return;
            }

            EnsureStorage();
            byte oldLevel = levels[cell.x, cell.y];
            float oldWear = wear[cell.x, cell.y];
            float newWear = Mathf.Min(MaxWear, oldWear + FootfallWear * Mathf.Clamp(weight, 0.05f, 2.0f));
            wear[cell.x, cell.y] = newWear;
            lastFootfallTimes[cell.x, cell.y] = Time.time;
            activeWearCells.Add(GetKey(cell));

            byte newLevel = GetLevelForWear(newWear);
            RecordAcceptedFootfall();
            if (newLevel == oldLevel)
            {
                return;
            }

            levels[cell.x, cell.y] = newLevel;
            RecordTrailLevelChange(cell, oldLevel, newLevel, oldWear, newWear, "footfall");
            RefreshCellAndNeighbors(cell);
        }

        private void DecayOldTrails(float elapsed)
        {
            float now = Time.time;
            float decay = DecayWearPerSecond * Mathf.Max(0f, elapsed);
            if (decay <= 0f)
            {
                return;
            }

            decayClearCells.Clear();
            foreach (int key in activeWearCells)
            {
                int x = key % map.Width;
                int y = key / map.Width;
                Vector2Int cell = new Vector2Int(x, y);
                float currentWear = wear[x, y];
                if (currentWear <= 0f || !CanWearCell(cell))
                {
                    byte invalidatedLevel = levels[x, y];
                    wear[x, y] = 0f;
                    levels[x, y] = 0;
                    decayClearCells.Add(key);
                    RecordInvalidatedTrailCell(cell, invalidatedLevel, currentWear);
                    RefreshCellAndNeighbors(cell);
                    continue;
                }

                if (now - lastFootfallTimes[x, y] < DecayGraceSeconds)
                {
                    continue;
                }

                byte oldLevel = levels[x, y];
                float newWear = Mathf.Max(0f, currentWear - decay);
                wear[x, y] = newWear;
                byte newLevel = GetLevelForWear(newWear);
                if (newWear <= 0f)
                {
                    decayClearCells.Add(key);
                }

                if (newLevel == oldLevel)
                {
                    continue;
                }

                levels[x, y] = newLevel;
                RecordTrailLevelChange(cell, oldLevel, newLevel, currentWear, newWear, "decay");
                RefreshCellAndNeighbors(cell);
            }

            for (int i = 0; i < decayClearCells.Count; i++)
            {
                activeWearCells.Remove(decayClearCells[i]);
            }
        }

        public bool IsTrailCell(Vector2Int cell)
        {
            return GetVisibleTrailLevel(cell) > 0;
        }

        public float GetMoveSpeedMultiplier(Vector2Int cell)
        {
            return IsTrailCell(cell) ? TrailSpeedMultiplier : 1f;
        }

        public float GetPathCostMultiplier(Vector2Int cell)
        {
            return IsTrailCell(cell) ? TrailPathCostMultiplier : 1f;
        }

        public void RefreshArea(Vector2Int origin, Vector2Int size)
        {
            if (map == null || size.x <= 0 || size.y <= 0)
            {
                return;
            }

            EnsureStorage();
            int minX = Mathf.Max(0, origin.x - 1);
            int minY = Mathf.Max(0, origin.y - 1);
            int maxX = Mathf.Min(map.Width - 1, origin.x + size.x);
            int maxY = Mathf.Min(map.Height - 1, origin.y + size.y);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    RefreshCell(new Vector2Int(x, y));
                }
            }
        }

        private byte GetTrailLevel(Vector2Int cell)
        {
            if (map == null
                || levels == null
                || cell.x < 0
                || cell.x >= map.Width
                || cell.y < 0
                || cell.y >= map.Height
                || !CanWearCell(cell))
            {
                return 0;
            }

            return levels[cell.x, cell.y];
        }

        private bool CanWearCell(Vector2Int cell)
        {
            return GetWearRejectReason(cell) == null;
        }

        private void RefreshCellAndNeighbors(Vector2Int cell)
        {
            RefreshCell(cell);
            for (int i = 0; i < NeighborCells.Length; i++)
            {
                RefreshCell(cell + NeighborCells[i]);
            }
        }

        private void RefreshCell(Vector2Int cell)
        {
            if (map == null || cell.x < 0 || cell.x >= map.Width || cell.y < 0 || cell.y >= map.Height)
            {
                return;
            }

            int key = GetKey(cell);
            byte level = GetVisibleTrailLevel(cell);
            if (level <= 0)
            {
                RemoveRenderer(key);
                return;
            }

            SpriteRenderer renderer = EnsureRenderer(key, cell);
            int mask = GetConnectionMask(cell);
            int variant = (Hash(map.ActiveSeed, cell.x, cell.y, 97) & int.MaxValue) % 4;
            renderer.sprite = StrategyTrailSpriteFactory.GetSprite(mask, level, variant);
            renderer.sortingOrder = StrategyWorldSorting.TrailOverlayOrder;
            Vector3 center = map.GetCellCenterWorld(cell.x, cell.y);
            renderer.transform.position = new Vector3(center.x, center.y, -0.045f);
        }

        private int GetConnectionMask(Vector2Int cell)
        {
            int mask = 0;
            if (GetVisibleTrailLevel(cell + Vector2Int.up) > 0)
            {
                mask |= North;
            }

            if (GetVisibleTrailLevel(cell + Vector2Int.right) > 0)
            {
                mask |= East;
            }

            if (GetVisibleTrailLevel(cell + Vector2Int.down) > 0)
            {
                mask |= South;
            }

            if (GetVisibleTrailLevel(cell + Vector2Int.left) > 0)
            {
                mask |= West;
            }

            if (HasDiagonalConnection(cell, 1, 1))
            {
                mask |= NorthEast;
            }

            if (HasDiagonalConnection(cell, 1, -1))
            {
                mask |= SouthEast;
            }

            if (HasDiagonalConnection(cell, -1, -1))
            {
                mask |= SouthWest;
            }

            if (HasDiagonalConnection(cell, -1, 1))
            {
                mask |= NorthWest;
            }

            return mask;
        }

        private bool HasDiagonalConnection(Vector2Int cell, int x, int y)
        {
            if (GetVisibleTrailLevel(cell + new Vector2Int(x, y)) <= 0)
            {
                return false;
            }

            return map != null
                && map.IsCellWalkable(cell + new Vector2Int(x, 0))
                && map.IsCellWalkable(cell + new Vector2Int(0, y));
        }

        private SpriteRenderer EnsureRenderer(int key, Vector2Int cell)
        {
            EnsureVisualRoot();
            if (renderers.TryGetValue(key, out SpriteRenderer existing) && existing != null)
            {
                return existing;
            }

            GameObject trailObject = new GameObject($"Trail {cell.x},{cell.y}");
            trailObject.transform.SetParent(visualRoot, false);
            SpriteRenderer renderer = trailObject.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            renderers[key] = renderer;
            return renderer;
        }

        private void RemoveRenderer(int key)
        {
            if (!renderers.TryGetValue(key, out SpriteRenderer renderer))
            {
                return;
            }

            renderers.Remove(key);
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }

        private void EnsureStorage()
        {
            if (map == null)
            {
                return;
            }

            if (wear != null
                && lastFootfallTimes != null
                && levels != null
                && wear.GetLength(0) == map.Width
                && wear.GetLength(1) == map.Height)
            {
                return;
            }

            ClearVisuals();
            activeWearCells.Clear();
            wear = new float[map.Width, map.Height];
            lastFootfallTimes = new float[map.Width, map.Height];
            levels = new byte[map.Width, map.Height];
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Trail Visuals");
            rootObject.transform.SetParent(transform, false);
            visualRoot = rootObject.transform;
        }

        private void ClearVisuals()
        {
            foreach (SpriteRenderer renderer in renderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }

            renderers.Clear();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private static byte GetLevelForWear(float value)
        {
            if (value >= WornThreshold)
            {
                return 3;
            }

            if (value >= ClearThreshold)
            {
                return 2;
            }

            return value >= FaintThreshold ? (byte)1 : (byte)0;
        }

        private int GetKey(Vector2Int cell)
        {
            return cell.y * map.Width + cell.x;
        }

        private static int Hash(int seed, int x, int y, int salt)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + x * 668265263;
                h = h * 1274126177 + y * 461845907;
                h ^= salt * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
