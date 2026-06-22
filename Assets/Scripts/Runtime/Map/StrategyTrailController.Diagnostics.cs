using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTrailController
    {
        private const float TrailStatsLogIntervalSeconds = 20f;

        private float trailStatsTimer;
        private int acceptedFootfallsSinceStats;
        private int rejectedFootfallsSinceStats;
        private int rejectedWeightSinceStats;
        private int rejectedMapSinceStats;
        private int rejectedBoundsSinceStats;
        private int rejectedWaterSinceStats;
        private int rejectedBridgeSinceStats;
        private int rejectedWalkabilitySinceStats;
        private int rejectedBuildabilitySinceStats;
        private int trailLevelUpsSinceStats;
        private int trailLevelDownsSinceStats;
        private int trailInvalidationsSinceStats;
        private int trailClearsSinceStats;

        private string GetWearRejectReason(Vector2Int cell)
        {
            if (map == null)
            {
                return "map_missing";
            }

            if (cell.x < 0 || cell.x >= map.Width || cell.y < 0 || cell.y >= map.Height)
            {
                return "out_of_bounds";
            }

            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return "cell_missing";
            }

            if (mapCell.Kind == CityMapCellKind.Water)
            {
                return "water";
            }

            if (map.IsBridgeWalkableCell(cell))
            {
                return "bridge";
            }

            if (!map.IsCellWalkable(cell))
            {
                return "not_walkable";
            }

            return null;
        }

        private void RecordAcceptedFootfall()
        {
            acceptedFootfallsSinceStats++;
        }

        private void RecordRejectedFootfall(Vector2Int cell, float weight, string reason)
        {
            rejectedFootfallsSinceStats++;
            switch (reason)
            {
                case "non_positive_weight":
                    rejectedWeightSinceStats++;
                    break;
                case "map_missing":
                case "cell_missing":
                    rejectedMapSinceStats++;
                    break;
                case "out_of_bounds":
                    rejectedBoundsSinceStats++;
                    break;
                case "water":
                    rejectedWaterSinceStats++;
                    break;
                case "bridge":
                    rejectedBridgeSinceStats++;
                    break;
                case "not_walkable":
                    rejectedWalkabilitySinceStats++;
                    break;
                case "not_buildable":
                    rejectedBuildabilitySinceStats++;
                    break;
            }

            if (rejectedFootfallsSinceStats % 100 != 1)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Map",
                "TrailFootfallRejectedSample",
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("weight", weight),
                StrategyDebugLogger.F("reason", reason));
        }

        private void RecordTrailLevelChange(
            Vector2Int cell,
            byte oldLevel,
            byte newLevel,
            float oldWear,
            float newWear,
            string reason)
        {
            if (newLevel > oldLevel)
            {
                trailLevelUpsSinceStats++;
            }
            else if (newLevel < oldLevel)
            {
                trailLevelDownsSinceStats++;
            }

            if (newWear <= 0f)
            {
                trailClearsSinceStats++;
            }

            StrategyDebugLogger.Info(
                "Map",
                "TrailLevelChanged",
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("oldLevel", (int)oldLevel),
                StrategyDebugLogger.F("newLevel", (int)newLevel),
                StrategyDebugLogger.F("oldWear", oldWear),
                StrategyDebugLogger.F("newWear", newWear),
                StrategyDebugLogger.F("visible", GetVisibleTrailLevel(cell) > 0),
                StrategyDebugLogger.F("rawNeighbors", CountRawTrailNeighbors(cell)),
                StrategyDebugLogger.F("cardinalNeighbors", CountCardinalTrailNeighbors(cell)));
        }

        private void RecordInvalidatedTrailCell(Vector2Int cell, byte oldLevel, float oldWear)
        {
            if (oldLevel <= 0 && oldWear <= 0f)
            {
                return;
            }

            trailInvalidationsSinceStats++;
            if (oldWear > 0f)
            {
                trailClearsSinceStats++;
            }

            StrategyDebugLogger.Info(
                "Map",
                "TrailCellInvalidated",
                StrategyDebugLogger.F("cell", cell),
                StrategyDebugLogger.F("oldLevel", (int)oldLevel),
                StrategyDebugLogger.F("oldWear", oldWear),
                StrategyDebugLogger.F("reason", GetWearRejectReason(cell) ?? "none"));
        }

        private void LogTrailStatsIfDue(float elapsed)
        {
            trailStatsTimer += Mathf.Max(0f, elapsed);
            if (trailStatsTimer < TrailStatsLogIntervalSeconds)
            {
                return;
            }

            float interval = trailStatsTimer;
            trailStatsTimer = 0f;
            CountTrailCells(out int faint, out int clear, out int worn, out int visible, out int hiddenFaint);
            StrategyDebugLogger.Info(
                "Map",
                "TrailStats",
                StrategyDebugLogger.F("interval", interval),
                StrategyDebugLogger.F("activeWearCells", activeWearCells.Count),
                StrategyDebugLogger.F("visibleCells", visible),
                StrategyDebugLogger.F("rendererCells", renderers.Count),
                StrategyDebugLogger.F("faintCells", faint),
                StrategyDebugLogger.F("clearCells", clear),
                StrategyDebugLogger.F("wornCells", worn),
                StrategyDebugLogger.F("hiddenFaintCells", hiddenFaint),
                StrategyDebugLogger.F("acceptedFootfalls", acceptedFootfallsSinceStats),
                StrategyDebugLogger.F("rejectedFootfalls", rejectedFootfallsSinceStats),
                StrategyDebugLogger.F("rejectedWeight", rejectedWeightSinceStats),
                StrategyDebugLogger.F("rejectedMap", rejectedMapSinceStats),
                StrategyDebugLogger.F("rejectedBounds", rejectedBoundsSinceStats),
                StrategyDebugLogger.F("rejectedWater", rejectedWaterSinceStats),
                StrategyDebugLogger.F("rejectedBridge", rejectedBridgeSinceStats),
                StrategyDebugLogger.F("rejectedWalkability", rejectedWalkabilitySinceStats),
                StrategyDebugLogger.F("rejectedBuildability", rejectedBuildabilitySinceStats),
                StrategyDebugLogger.F("levelUps", trailLevelUpsSinceStats),
                StrategyDebugLogger.F("levelDowns", trailLevelDownsSinceStats),
                StrategyDebugLogger.F("invalidations", trailInvalidationsSinceStats),
                StrategyDebugLogger.F("clears", trailClearsSinceStats));

            ResetTrailStatsCounters();
        }

        private void CountTrailCells(out int faint, out int clear, out int worn, out int visible, out int hiddenFaint)
        {
            faint = 0;
            clear = 0;
            worn = 0;
            visible = 0;
            hiddenFaint = 0;
            foreach (int key in activeWearCells)
            {
                Vector2Int cell = new Vector2Int(key % map.Width, key / map.Width);
                byte level = GetTrailLevel(cell);
                if (level == 1)
                {
                    faint++;
                }
                else if (level == 2)
                {
                    clear++;
                }
                else if (level >= 3)
                {
                    worn++;
                }

                if (GetVisibleTrailLevel(cell) > 0)
                {
                    visible++;
                }
                else if (level == 1)
                {
                    hiddenFaint++;
                }
            }
        }

        private void ResetTrailStatsCounters()
        {
            acceptedFootfallsSinceStats = 0;
            rejectedFootfallsSinceStats = 0;
            rejectedWeightSinceStats = 0;
            rejectedMapSinceStats = 0;
            rejectedBoundsSinceStats = 0;
            rejectedWaterSinceStats = 0;
            rejectedBridgeSinceStats = 0;
            rejectedWalkabilitySinceStats = 0;
            rejectedBuildabilitySinceStats = 0;
            trailLevelUpsSinceStats = 0;
            trailLevelDownsSinceStats = 0;
            trailInvalidationsSinceStats = 0;
            trailClearsSinceStats = 0;
        }
    }
}
