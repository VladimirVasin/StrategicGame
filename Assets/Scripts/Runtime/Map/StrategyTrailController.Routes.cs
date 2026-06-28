using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private const float RouteTraversalWear = 2.75f;
        private const float RouteEndpointWearMultiplier = 0.35f;
        private const float RouteNearEndpointWearMultiplier = 0.70f;
        private const int RouteEndpointAttachWindowCells = 3;

        private readonly Dictionary<string, int> routeTraversalCounts = new();
        private readonly HashSet<int> activeRouteCells = new();
        private readonly List<int> routeDecayClearCells = new();
        private readonly List<Vector2Int> routeRecordScratch = new();
        private readonly List<int> routeRecordIndexScratch = new();
        private readonly HashSet<int> routeRecordKeyScratch = new();
        private float[,] routeWear;
        private float[,] routeLastTraversalTimes;
        private byte[,] routeLevels;
        private int routeTraversalsSinceStats;
        private int routeLevelUpsSinceStats;
        private int routeLevelDownsSinceStats;
        private int routeInvalidationsSinceStats;
        private int routeClearsSinceStats;

        public void RecordBuildingRouteTraversal(
            StrategyPlacedBuilding fromBuilding,
            StrategyPlacedBuilding toBuilding,
            IReadOnlyList<Vector2Int> routeCells)
        {
            if (fromBuilding == null || toBuilding == null || fromBuilding == toBuilding || routeCells == null || routeCells.Count < 2)
            {
                return;
            }

            EnsureRouteStorage();
            if (routeWear == null || routeLastTraversalTimes == null || routeLevels == null)
            {
                return;
            }

            string routeKey = GetBuildingRouteKey(fromBuilding, toBuilding);
            routeTraversalCounts.TryGetValue(routeKey, out int oldCount);
            int newCount = oldCount + 1;
            routeTraversalCounts[routeKey] = newCount;

            SelectRouteCellsForWear(
                routeCells,
                out bool attachedToTrail,
                out Vector2Int startAttachCell,
                out Vector2Int endAttachCell);

            int acceptedCells = 0;
            for (int i = 0; i < routeRecordScratch.Count; i++)
            {
                float weight = GetRouteCellWeight(routeRecordIndexScratch[i], routeCells.Count);
                if (AddRouteWear(routeRecordScratch[i], weight))
                {
                    acceptedCells++;
                }
            }

            if (acceptedCells <= 0)
            {
                return;
            }

            routeTraversalsSinceStats++;
            if (newCount == 1 || newCount % 8 == 0)
            {
                StrategyDebugLogger.Info(
                    "Map",
                    "TrailRouteTraversed",
                    StrategyDebugLogger.F("fromTool", fromBuilding.Tool),
                    StrategyDebugLogger.F("fromOrigin", fromBuilding.Origin),
                    StrategyDebugLogger.F("toTool", toBuilding.Tool),
                    StrategyDebugLogger.F("toOrigin", toBuilding.Origin),
                    StrategyDebugLogger.F("count", newCount),
                    StrategyDebugLogger.F("cells", acceptedCells),
                    StrategyDebugLogger.F("fullCells", routeCells.Count),
                    StrategyDebugLogger.F("recordedCells", routeRecordScratch.Count),
                    StrategyDebugLogger.F("attachedToTrail", attachedToTrail),
                    StrategyDebugLogger.F("startAttachCell", attachedToTrail ? startAttachCell : Vector2Int.zero),
                    StrategyDebugLogger.F("endAttachCell", attachedToTrail ? endAttachCell : Vector2Int.zero));
            }
        }

        private void SelectRouteCellsForWear(
            IReadOnlyList<Vector2Int> routeCells,
            out bool attachedToTrail,
            out Vector2Int startAttachCell,
            out Vector2Int endAttachCell)
        {
            routeRecordScratch.Clear();
            routeRecordIndexScratch.Clear();
            routeRecordKeyScratch.Clear();
            attachedToTrail = false;
            startAttachCell = Vector2Int.zero;
            endAttachCell = Vector2Int.zero;

            int firstStableIndex = FindStableRouteCellIndex(routeCells, 0, 1);
            if (firstStableIndex < 0)
            {
                AddRouteRecordRange(routeCells, 0, routeCells.Count - 1);
                return;
            }

            int lastStableIndex = FindStableRouteCellIndex(routeCells, routeCells.Count - 1, -1);
            attachedToTrail = true;
            startAttachCell = routeCells[firstStableIndex];
            endAttachCell = routeCells[lastStableIndex];

            bool startAlreadyConnected = firstStableIndex <= RouteEndpointAttachWindowCells;
            bool endAlreadyConnected = routeCells.Count - 1 - lastStableIndex <= RouteEndpointAttachWindowCells;
            if (!startAlreadyConnected)
            {
                AddRouteRecordRange(routeCells, 0, firstStableIndex);
            }

            if (!endAlreadyConnected)
            {
                AddRouteRecordRange(routeCells, lastStableIndex, routeCells.Count - 1);
            }
        }

        private int FindStableRouteCellIndex(IReadOnlyList<Vector2Int> routeCells, int startIndex, int step)
        {
            for (int i = startIndex; i >= 0 && i < routeCells.Count; i += step)
            {
                if (IsStableRouteTrailCell(routeCells[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private void AddRouteRecordRange(IReadOnlyList<Vector2Int> routeCells, int startIndex, int endIndex)
        {
            int step = startIndex <= endIndex ? 1 : -1;
            for (int i = startIndex; i >= 0 && i < routeCells.Count; i += step)
            {
                AddRouteRecordCell(routeCells[i], i);
                if (i == endIndex)
                {
                    break;
                }
            }
        }

        private void AddRouteRecordCell(Vector2Int cell, int sourceIndex)
        {
            if (GetWearRejectReason(cell) != null)
            {
                return;
            }

            int key = GetKey(cell);
            if (!routeRecordKeyScratch.Add(key))
            {
                return;
            }

            routeRecordScratch.Add(cell);
            routeRecordIndexScratch.Add(sourceIndex);
        }

        private bool AddRouteWear(Vector2Int cell, float weight)
        {
            if (weight <= 0f || GetWearRejectReason(cell) != null)
            {
                return false;
            }

            byte oldLevel = routeLevels[cell.x, cell.y];
            float oldWear = routeWear[cell.x, cell.y];
            float newWear = Mathf.Min(MaxWear, oldWear + RouteTraversalWear * Mathf.Clamp(weight, 0.05f, 1.25f));
            routeWear[cell.x, cell.y] = newWear;
            routeLastTraversalTimes[cell.x, cell.y] = Time.time;
            activeRouteCells.Add(GetKey(cell));

            byte newLevel = GetLevelForWear(newWear);
            if (newLevel == oldLevel)
            {
                return true;
            }

            routeLevels[cell.x, cell.y] = newLevel;
            RecordRouteLevelChange(cell, oldLevel, newLevel, oldWear, newWear, "route");
            RefreshCellAndNeighbors(cell);
            return true;
        }

        private void DecayOldRouteTrails(float elapsed)
        {
            if (map == null || routeWear == null || routeLastTraversalTimes == null || routeLevels == null)
            {
                return;
            }

            float decay = DecayWearPerSecond * Mathf.Max(0f, elapsed);
            if (decay <= 0f)
            {
                return;
            }

            float now = Time.time;
            routeDecayClearCells.Clear();
            foreach (int key in activeRouteCells)
            {
                int x = key % map.Width;
                int y = key / map.Width;
                Vector2Int cell = new Vector2Int(x, y);
                float currentWear = routeWear[x, y];
                if (currentWear <= 0f || !CanWearCell(cell))
                {
                    byte oldInvalidLevel = routeLevels[x, y];
                    routeWear[x, y] = 0f;
                    routeLevels[x, y] = 0;
                    routeDecayClearCells.Add(key);
                    RecordRouteInvalidated(cell, oldInvalidLevel, currentWear);
                    RefreshCellAndNeighbors(cell);
                    continue;
                }

                if (now - routeLastTraversalTimes[x, y] < DecayGraceSeconds)
                {
                    continue;
                }

                byte oldLevel = routeLevels[x, y];
                float newWear = Mathf.Max(0f, currentWear - decay);
                routeWear[x, y] = newWear;
                byte newLevel = GetLevelForWear(newWear);
                if (newWear <= 0f)
                {
                    routeDecayClearCells.Add(key);
                }

                if (newLevel == oldLevel)
                {
                    continue;
                }

                routeLevels[x, y] = newLevel;
                RecordRouteLevelChange(cell, oldLevel, newLevel, currentWear, newWear, "decay");
                RefreshCellAndNeighbors(cell);
            }

            for (int i = 0; i < routeDecayClearCells.Count; i++)
            {
                activeRouteCells.Remove(routeDecayClearCells[i]);
            }
        }

        private byte GetRawRouteTrailLevel(Vector2Int cell)
        {
            if (map == null
                || routeLevels == null
                || cell.x < 0
                || cell.x >= map.Width
                || cell.y < 0
                || cell.y >= map.Height
                || !CanWearCell(cell))
            {
                return 0;
            }

            return routeLevels[cell.x, cell.y];
        }

        private int CountCardinalRouteTrailNeighbors(Vector2Int cell)
        {
            int count = 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.up) > 0 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.right) > 0 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.down) > 0 ? 1 : 0;
            count += GetRawRouteTrailLevel(cell + Vector2Int.left) > 0 ? 1 : 0;
            return count;
        }

        private void EnsureRouteStorage()
        {
            if (map == null)
            {
                return;
            }

            if (routeWear != null
                && routeLastTraversalTimes != null
                && routeLevels != null
                && routeWear.GetLength(0) == map.Width
                && routeWear.GetLength(1) == map.Height)
            {
                return;
            }

            activeRouteCells.Clear();
            routeDecayClearCells.Clear();
            routeTraversalCounts.Clear();
            routeWear = new float[map.Width, map.Height];
            routeLastTraversalTimes = new float[map.Width, map.Height];
            routeLevels = new byte[map.Width, map.Height];
        }

        private void RecordRouteLevelChange(Vector2Int cell, byte oldLevel, byte newLevel, float oldWear, float newWear, string reason)
        {
            if (newLevel > oldLevel)
            {
                routeLevelUpsSinceStats++;
            }
            else if (newLevel < oldLevel)
            {
                routeLevelDownsSinceStats++;
            }

            if (newWear <= 0f)
            {
                routeClearsSinceStats++;
            }

            StrategyDebugLogger.Info(
                "Map",
                "TrailRouteLevelChanged",
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("oldLevel", (int)oldLevel),
                StrategyDebugLogger.F("newLevel", (int)newLevel),
                StrategyDebugLogger.F("oldWear", oldWear),
                StrategyDebugLogger.F("newWear", newWear));
        }

        private void RecordRouteInvalidated(Vector2Int cell, byte oldLevel, float oldWear)
        {
            if (oldLevel <= 0 && oldWear <= 0f)
            {
                return;
            }

            routeInvalidationsSinceStats++;
            if (oldWear > 0f)
            {
                routeClearsSinceStats++;
            }
        }

        private static float GetRouteCellWeight(int index, int count)
        {
            if (count <= 2)
            {
                return 1f;
            }

            if (index == 0 || index == count - 1)
            {
                return RouteEndpointWearMultiplier;
            }

            return index == 1 || index == count - 2 ? RouteNearEndpointWearMultiplier : 1f;
        }

        private static string GetBuildingRouteKey(StrategyPlacedBuilding fromBuilding, StrategyPlacedBuilding toBuilding)
        {
            var left = fromBuilding.GetEntityId();
            var right = toBuilding.GetEntityId();
            if (left > right)
            {
                (left, right) = (right, left);
            }

            return left + ":" + right;
        }
    }
}
