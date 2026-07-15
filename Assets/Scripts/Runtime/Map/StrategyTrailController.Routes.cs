using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private const int RouteTraversalsRequired = 3;

        private readonly Dictionary<string, int> routeTraversalCounts = new();
        private readonly Dictionary<string, List<Vector2Int>> canonicalRouteCells = new();
        private readonly HashSet<int> activeRouteCells = new();
        private readonly HashSet<int> pendingRouteReservations = new();
        private readonly List<int> routeDecayClearCells = new();
        private float[,] routeWear;
        private float[,] routeLastTraversalTimes;
        private byte[,] routeLevels;
        private int routeTraversalsSinceStats;
        private int routeLevelUpsSinceStats;
        private int routeLevelDownsSinceStats;
        private int routeInvalidationsSinceStats;
        private int routeClearsSinceStats;

        public bool HasRouteRoadAt(Vector2Int cell)
        {
            return map != null
                && cell.x >= 0
                && cell.x < map.Width
                && cell.y >= 0
                && cell.y < map.Height
                && (activeRouteCells.Contains(cell.y * map.Width + cell.x)
                    || pendingRouteReservations.Contains(cell.y * map.Width + cell.x));
        }

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
            IReadOnlyList<Vector2Int> cellsToRecord = GetCanonicalRouteCells(routeKey, routeCells);
            if (newCount < RouteTraversalsRequired)
            {
                return;
            }

            BuildSingleSidedRouteCells(cellsToRecord, routeConnectionCells);
            if (routeConnectionCells.Count < 2)
            {
                return;
            }

            int acceptedCells = 0;
            for (int i = 0; i < routeConnectionCells.Count; i++)
            {
                if (AddRouteRoad(routeConnectionCells[i]))
                {
                    acceptedCells++;
                }
            }

            if (acceptedCells <= 0)
            {
                return;
            }

            routeTraversalsSinceStats++;
            if (newCount == RouteTraversalsRequired || newCount % 8 == 0)
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
                    StrategyDebugLogger.F("fullCells", cellsToRecord.Count),
                    StrategyDebugLogger.F("connectedCells", routeConnectionCells.Count),
                    StrategyDebugLogger.F("canonicalRoute", cellsToRecord != routeCells),
                    StrategyDebugLogger.F("roadLevel", 3),
                    StrategyDebugLogger.F("instantRoad", false),
                    StrategyDebugLogger.F("requiredTraversals", RouteTraversalsRequired));
            }
        }

        private IReadOnlyList<Vector2Int> GetCanonicalRouteCells(string routeKey, IReadOnlyList<Vector2Int> routeCells)
        {
            if (canonicalRouteCells.TryGetValue(routeKey, out List<Vector2Int> canonical)
                && AreRouteCellsStillWearable(canonical))
            {
                return canonical;
            }

            if (routeCells == null || routeCells.Count < 2 || !AreRouteCellsStillWearable(routeCells))
            {
                canonicalRouteCells.Remove(routeKey);
                return routeCells;
            }

            List<Vector2Int> copy = new(routeCells.Count);
            for (int i = 0; i < routeCells.Count; i++)
            {
                copy.Add(routeCells[i]);
            }

            canonicalRouteCells[routeKey] = copy;
            return copy;
        }

        private bool AreRouteCellsStillWearable(IReadOnlyList<Vector2Int> routeCells)
        {
            if (routeCells == null)
            {
                return false;
            }

            for (int i = 0; i < routeCells.Count; i++)
            {
                if (GetWearRejectReason(routeCells[i]) != null)
                {
                    return false;
                }
            }

            return routeCells.Count >= 2;
        }

        private bool AddRouteRoad(Vector2Int cell)
        {
            if (GetWearRejectReason(cell) != null)
            {
                return false;
            }

            byte oldLevel = routeLevels[cell.x, cell.y];
            float oldWear = routeWear[cell.x, cell.y];
            routeWear[cell.x, cell.y] = MaxWear;
            routeLastTraversalTimes[cell.x, cell.y] = Time.time;
            activeRouteCells.Add(GetKey(cell));

            byte newLevel = GetLevelForWear(MaxWear);
            if (newLevel == oldLevel)
            {
                return true;
            }

            routeLevels[cell.x, cell.y] = newLevel;
            RecordRouteLevelChange(cell, oldLevel, newLevel, oldWear, MaxWear, "route_completed");
            RefreshCellAndNeighbors(cell);
            QueueRoadsideRefreshAround(cell);
            return true;
        }

        private bool WouldCompleteRouteSquare(Vector2Int cell)
        {
            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    Vector2Int corner = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (HasRouteSquareCell(corner, cell)
                        && HasRouteSquareCell(corner + Vector2Int.right, cell)
                        && HasRouteSquareCell(corner + Vector2Int.up, cell)
                        && HasRouteSquareCell(corner + Vector2Int.one, cell))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasRouteSquareCell(Vector2Int squareCell, Vector2Int candidate)
        {
            return squareCell == candidate || GetRawRouteTrailLevel(squareCell) > 0;
        }

        private void PruneInvalidRouteRoads()
        {
            if (map == null || routeWear == null || routeLastTraversalTimes == null || routeLevels == null)
            {
                return;
            }

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
                    QueueRoadsideRefreshAround(cell);
                    continue;
                }
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
            ClearRoadsideProps();
            routeTraversalCounts.Clear();
            canonicalRouteCells.Clear();
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
