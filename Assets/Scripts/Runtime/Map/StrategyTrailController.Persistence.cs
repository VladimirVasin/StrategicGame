using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        public void CapturePersistentTrailCells(List<int> target)
        {
            target.Clear();
            foreach (int key in activeRouteCells)
            {
                target.Add(key);
            }
        }

        public void RestorePersistentTrailCells(IReadOnlyList<int> source)
        {
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
