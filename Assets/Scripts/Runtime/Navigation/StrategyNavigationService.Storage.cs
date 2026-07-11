using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNavigationService
    {
        private Queue<PendingRequest> GetPendingQueue(StrategyNavigationPriority priority)
        {
            return priority switch
            {
                StrategyNavigationPriority.Critical => criticalPending,
                StrategyNavigationPriority.Background => backgroundPending,
                _ => normalPending
            };
        }

        private bool TryDequeuePending(out PendingRequest request)
        {
            if (criticalPending.Count > 0)
            {
                request = criticalPending.Dequeue();
                return true;
            }

            if (normalPending.Count > 0)
            {
                request = normalPending.Dequeue();
                return true;
            }

            if (backgroundPending.Count > 0)
            {
                request = backgroundPending.Dequeue();
                return true;
            }

            request = default;
            return false;
        }

        private bool TryDropLowestPriorityPending(out PendingRequest request)
        {
            if (backgroundPending.Count > 0)
            {
                request = backgroundPending.Dequeue();
                return true;
            }

            if (normalPending.Count > 0)
            {
                request = normalPending.Dequeue();
                return true;
            }

            if (criticalPending.Count > 0)
            {
                request = criticalPending.Dequeue();
                return true;
            }

            request = default;
            return false;
        }

        private void RemoveCachedPath(StrategyNavigationQueryKey key)
        {
            if (paths.Remove(key, out CachedPath cached))
            {
                if (cached.OrderNode != null)
                {
                    cacheOrder.Remove(cached.OrderNode);
                }

                cached.Release();
            }

        }

        private void RefreshCachedPathOrder(CachedPath cached)
        {
            if (cached?.OrderNode == null)
            {
                return;
            }

            cacheOrder.Remove(cached.OrderNode);
            cacheOrder.AddLast(cached.OrderNode);
        }

        private void ReleaseAllCachedPaths()
        {
            foreach (CachedPath cached in paths.Values)
            {
                cached.Release();
            }

            paths.Clear();
            cacheOrder.Clear();
        }

        private sealed class CachedPath
        {
            private CachedPath(
                StrategyNavigationStatus status,
                Vector2Int[] rawCells,
                int rawCount,
                Vector2Int[] smoothedCells,
                int smoothedCount,
                float expiresAt)
            {
                Status = status;
                RawCells = rawCells;
                RawCount = rawCount;
                SmoothedCells = smoothedCells;
                SmoothedCount = smoothedCount;
                ExpiresAt = expiresAt;
            }

            public StrategyNavigationStatus Status { get; }
            public Vector2Int[] RawCells { get; }
            public int RawCount { get; }
            public Vector2Int[] SmoothedCells { get; }
            public int SmoothedCount { get; }
            public float ExpiresAt { get; }
            public LinkedListNode<StrategyNavigationQueryKey> OrderNode { get; set; }

            public static CachedPath Create(
                StrategyNavigationStatus status,
                List<Vector2Int> rawCells,
                List<Vector2Int> smoothedCells,
                float expiresAt)
            {
                Vector2Int[] raw = RentCopy(rawCells);
                Vector2Int[] smoothed = RentCopy(smoothedCells);
                return new CachedPath(
                    status,
                    raw,
                    rawCells.Count,
                    smoothed,
                    smoothedCells.Count,
                    expiresAt);
            }

            public void CopyTo(List<Vector2Int> rawCells, List<Vector2Int> smoothedCells)
            {
                for (int i = 0; i < RawCount; i++)
                {
                    rawCells.Add(RawCells[i]);
                }

                for (int i = 0; i < SmoothedCount; i++)
                {
                    smoothedCells.Add(SmoothedCells[i]);
                }
            }

            public void Release()
            {
                if (RawCells.Length > 0)
                {
                    ArrayPool<Vector2Int>.Shared.Return(RawCells);
                }

                if (SmoothedCells.Length > 0)
                {
                    ArrayPool<Vector2Int>.Shared.Return(SmoothedCells);
                }
            }

            private static Vector2Int[] RentCopy(List<Vector2Int> source)
            {
                if (source.Count <= 0)
                {
                    return Array.Empty<Vector2Int>();
                }

                Vector2Int[] result = ArrayPool<Vector2Int>.Shared.Rent(source.Count);
                source.CopyTo(result, 0);
                return result;
            }
        }
    }
}
