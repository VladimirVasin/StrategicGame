using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        public void ReservePendingPersistentTrailCells(IReadOnlyList<int> source)
        {
            pendingRouteReservations.Clear();
            if (map == null || source == null)
            {
                return;
            }

            int cellCount = map.Width * map.Height;
            for (int i = 0; i < source.Count; i++)
            {
                int key = source[i];
                if (key >= 0 && key < cellCount)
                {
                    pendingRouteReservations.Add(key);
                }
            }

            if (pendingRouteReservations.Count > 0)
            {
                StrategyDebugLogger.Info(
                    "Map",
                    "PendingTrailCellsReserved",
                    StrategyDebugLogger.F("cells", pendingRouteReservations.Count));
            }
        }

        public void CapturePersistentTrailCells(List<int> target)
        {
            target.Clear();
            if (map == null)
            {
                return;
            }

            foreach (int key in activeRouteCells)
            {
                Vector2Int cell = new(key % map.Width, key / map.Width);
                if (GetWearRejectReason(cell) == null)
                {
                    target.Add(key);
                }
            }
        }

        public void RestorePersistentTrailCells(IReadOnlyList<int> source)
        {
            pendingRouteReservations.Clear();
            EnsureRouteStorage();
            if (map == null || routeWear == null)
            {
                return;
            }

            System.Array.Clear(routeWear, 0, routeWear.Length);
            System.Array.Clear(routeLastTraversalTimes, 0, routeLastTraversalTimes.Length);
            System.Array.Clear(routeLevels, 0, routeLevels.Length);
            activeRouteCells.Clear();
            if (source != null)
            {
                int cellCount = map.Width * map.Height;
                for (int i = 0; i < source.Count; i++)
                {
                    int key = source[i];
                    if (key < 0 || key >= cellCount)
                    {
                        continue;
                    }

                    Vector2Int cell = new(key % map.Width, key / map.Width);
                    if (GetWearRejectReason(cell) != null)
                    {
                        continue;
                    }

                    routeWear[cell.x, cell.y] = MaxWear;
                    routeLastTraversalTimes[cell.x, cell.y] = Time.time;
                    routeLevels[cell.x, cell.y] = GetLevelForWear(MaxWear);
                    activeRouteCells.Add(key);
                }
            }

            RefreshArea(Vector2Int.zero, new Vector2Int(map.Width, map.Height));
        }
    }
}
