using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyNavigationService : MonoBehaviour
    {
        private const int MaxPathComputationsPerFrame = 8;
        private const int MaxQueuedComputationsPerFrame = 4;
        private const int MaxPendingRequests = 256;
        private const int MaxCachedPaths = 384;
        private const float FrameWorkBudgetMs = 2.2f;
        private const float SuccessCacheSeconds = 1.5f;
        private const float FailureCacheSeconds = 0.75f;
        private const float QueueRequestLifetimeSeconds = 2f;
        private const float DiagnosticsIntervalSeconds = 6f;

        private readonly StrategyNavigationPathfinder pathfinder = new();
        private readonly Queue<PendingRequest> criticalPending = new();
        private readonly Queue<PendingRequest> normalPending = new();
        private readonly Queue<PendingRequest> backgroundPending = new();
        private readonly HashSet<StrategyNavigationQueryKey> pendingKeys = new();
        private readonly Dictionary<StrategyNavigationQueryKey, CachedPath> paths = new();
        private readonly LinkedList<StrategyNavigationQueryKey> cacheOrder = new();
        private readonly List<Vector2Int> rawScratch = new();
        private readonly List<Vector2Int> smoothedScratch = new();
        private CityMapController map;
        private int observedWalkabilityVersion = -1;
        private int budgetFrame = -1;
        private int computationsThisFrame;
        private long frameWorkStartedTicks;
        private int queuedSinceLog;
        private int cacheHitsSinceLog;
        private int computedSinceLog;
        private int unreachableSinceLog;
        private int droppedSinceLog;
        private float nextDiagnosticsTime;

        public static StrategyNavigationService Active { get; private set; }
        public int PendingCount => criticalPending.Count + normalPending.Count + backgroundPending.Count;
        public int CachedPathCount => paths.Count;

        private void Awake()
        {
            Active = this;
        }

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            observedWalkabilityVersion = map != null ? map.WalkabilityVersion : -1;
            ClearCachesAndQueue();
            StrategyDebugLogger.Info(
                "Navigation",
                "Configured",
                StrategyDebugLogger.F("mapWidth", map != null ? map.Width : 0),
                StrategyDebugLogger.F("mapHeight", map != null ? map.Height : 0),
                StrategyDebugLogger.F("walkabilityVersion", observedWalkabilityVersion),
                StrategyDebugLogger.F("framePathBudget", MaxPathComputationsPerFrame),
                StrategyDebugLogger.F("frameWorkBudgetMs", FrameWorkBudgetMs));
        }

        public StrategyNavigationStatus TryBuildPath(
            StrategyNavigationQuery query,
            List<Vector2Int> rawCells,
            List<Vector2Int> smoothedCells,
            bool allowDeferred = true)
        {
            if (rawCells == null || smoothedCells == null || map == null)
            {
                return StrategyNavigationStatus.Invalid;
            }

            rawCells.Clear();
            smoothedCells.Clear();
            EnsureCurrentRevision();
            StrategyNavigationQueryKey key = new(query, observedWalkabilityVersion);
            if (TryCopyCachedPath(key, rawCells, smoothedCells, out StrategyNavigationStatus cachedStatus))
            {
                cacheHitsSinceLog++;
                return cachedStatus;
            }

            if (CanComputeThisFrame() || !allowDeferred)
            {
                return ComputeAndCache(key, query, rawCells, smoothedCells);
            }

            Enqueue(key, query);
            return StrategyNavigationStatus.Deferred;
        }

        public bool TryGetCachedReachability(StrategyNavigationQuery query, out bool canReach)
        {
            EnsureCurrentRevision();
            StrategyNavigationQueryKey key = new(query, observedWalkabilityVersion);
            if (paths.TryGetValue(key, out CachedPath cached))
            {
                if (cached.ExpiresAt < Time.realtimeSinceStartup)
                {
                    RemoveCachedPath(key);
                    canReach = false;
                    return false;
                }

                RefreshCachedPathOrder(cached);
                cacheHitsSinceLog++;
                canReach = cached.Status == StrategyNavigationStatus.Success;
                return true;
            }

            canReach = false;
            return false;
        }

        private void Update()
        {
            if (map == null)
            {
                return;
            }

            EnsureCurrentRevision();
            BeginFrameBudget();
            int processed = 0;
            while (PendingCount > 0
                && processed < MaxQueuedComputationsPerFrame
                && CanComputeThisFrame())
            {
                if (!TryDequeuePending(out PendingRequest request))
                {
                    break;
                }

                pendingKeys.Remove(request.Key);
                if (request.Key.Revision != observedWalkabilityVersion
                    || Time.realtimeSinceStartup - request.EnqueuedAt > QueueRequestLifetimeSeconds)
                {
                    droppedSinceLog++;
                    continue;
                }

                if (paths.ContainsKey(request.Key))
                {
                    continue;
                }

                ComputeAndCache(request.Key, request.Query, rawScratch, smoothedScratch);
                processed++;
            }

            LogDiagnosticsIfDue();
        }

        private StrategyNavigationStatus ComputeAndCache(
            StrategyNavigationQueryKey key,
            StrategyNavigationQuery query,
            List<Vector2Int> rawCells,
            List<Vector2Int> smoothedCells)
        {
            using var profilerScope = StrategyPerformanceMarkers.NavigationCompute.Auto();
            BeginFrameBudget();
            computationsThisFrame++;
            StrategyNavigationStatus status = pathfinder.TryBuildPath(map, query, rawScratch, smoothedScratch);
            computedSinceLog++;
            if (status == StrategyNavigationStatus.Unreachable || status == StrategyNavigationStatus.Invalid)
            {
                unreachableSinceLog++;
            }

            bool canReach = status == StrategyNavigationStatus.Success;
            float ttl = canReach ? SuccessCacheSeconds : FailureCacheSeconds;
            RemoveCachedPath(key);
            CachedPath cachedPath = CachedPath.Create(
                status,
                rawScratch,
                smoothedScratch,
                Time.realtimeSinceStartup + ttl);
            cachedPath.OrderNode = cacheOrder.AddLast(key);
            paths[key] = cachedPath;
            TrimCachesIfNeeded();

            rawCells.Clear();
            smoothedCells.Clear();
            if (status == StrategyNavigationStatus.Success)
            {
                rawCells.AddRange(rawScratch);
                smoothedCells.AddRange(smoothedScratch);
            }

            return status;
        }

        private bool TryCopyCachedPath(
            StrategyNavigationQueryKey key,
            List<Vector2Int> rawCells,
            List<Vector2Int> smoothedCells,
            out StrategyNavigationStatus status)
        {
            if (!paths.TryGetValue(key, out CachedPath cached))
            {
                status = default;
                return false;
            }

            if (cached.ExpiresAt < Time.realtimeSinceStartup)
            {
                RemoveCachedPath(key);
                status = default;
                return false;
            }

            status = cached.Status;
            RefreshCachedPathOrder(cached);

            if (status == StrategyNavigationStatus.Success)
            {
                cached.CopyTo(rawCells, smoothedCells);
            }

            return true;
        }

        private void Enqueue(StrategyNavigationQueryKey key, StrategyNavigationQuery query)
        {
            if (pendingKeys.Contains(key))
            {
                return;
            }

            while (PendingCount >= MaxPendingRequests)
            {
                if (!TryDropLowestPriorityPending(out PendingRequest dropped))
                {
                    break;
                }

                pendingKeys.Remove(dropped.Key);
                droppedSinceLog++;
            }

            PendingRequest request = new(key, query, Time.realtimeSinceStartup);
            GetPendingQueue(query.Priority).Enqueue(request);
            pendingKeys.Add(key);
            queuedSinceLog++;
            StrategyResidentPerformanceCounters.RecordPathBudgetDeferral();
        }

        private bool CanComputeThisFrame()
        {
            BeginFrameBudget();
            return computationsThisFrame < MaxPathComputationsPerFrame
                && GetFrameWorkElapsedMs() < FrameWorkBudgetMs;
        }

        private void BeginFrameBudget()
        {
            if (budgetFrame == Time.frameCount)
            {
                return;
            }

            budgetFrame = Time.frameCount;
            computationsThisFrame = 0;
            frameWorkStartedTicks = Stopwatch.GetTimestamp();
        }

        private float GetFrameWorkElapsedMs()
        {
            long elapsed = Stopwatch.GetTimestamp() - frameWorkStartedTicks;
            return elapsed * 1000f / Stopwatch.Frequency;
        }

        private void EnsureCurrentRevision()
        {
            int current = map != null ? map.WalkabilityVersion : -1;
            if (current == observedWalkabilityVersion)
            {
                return;
            }

            observedWalkabilityVersion = current;
            ClearCachesAndQueue();
        }

        private void ClearCachesAndQueue()
        {
            criticalPending.Clear();
            normalPending.Clear();
            backgroundPending.Clear();
            pendingKeys.Clear();
            ReleaseAllCachedPaths();
        }

        private void TrimCachesIfNeeded()
        {
            if (paths.Count <= MaxCachedPaths)
            {
                return;
            }

            while (paths.Count > MaxCachedPaths && cacheOrder.Count > 0)
            {
                RemoveCachedPath(cacheOrder.First.Value);
            }
        }

        private void LogDiagnosticsIfDue()
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextDiagnosticsTime)
            {
                return;
            }

            nextDiagnosticsTime = now + DiagnosticsIntervalSeconds;
            if (queuedSinceLog > 0 || computedSinceLog > 0 || droppedSinceLog > 0)
            {
                StrategyDebugLogger.Info(
                    "Navigation",
                    "BudgetWindow",
                    StrategyDebugLogger.F("computed", computedSinceLog),
                    StrategyDebugLogger.F("queued", queuedSinceLog),
                    StrategyDebugLogger.F("cacheHits", cacheHitsSinceLog),
                    StrategyDebugLogger.F("unreachable", unreachableSinceLog),
                    StrategyDebugLogger.F("dropped", droppedSinceLog),
                    StrategyDebugLogger.F("pending", PendingCount),
                    StrategyDebugLogger.F("cachedPaths", paths.Count));
            }

            queuedSinceLog = 0;
            cacheHitsSinceLog = 0;
            computedSinceLog = 0;
            unreachableSinceLog = 0;
            droppedSinceLog = 0;
        }

        private void OnDestroy()
        {
            ClearCachesAndQueue();
            if (Active == this)
            {
                Active = null;
            }
        }

        private readonly struct PendingRequest
        {
            public PendingRequest(
                StrategyNavigationQueryKey key,
                StrategyNavigationQuery query,
                float enqueuedAt)
            {
                Key = key;
                Query = query;
                EnqueuedAt = enqueuedAt;
            }

            public StrategyNavigationQueryKey Key { get; }
            public StrategyNavigationQuery Query { get; }
            public float EnqueuedAt { get; }
        }

    }
}
