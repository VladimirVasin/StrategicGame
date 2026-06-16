using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const int WildlifeStructureAvoidanceRadius = 4;
        private const float WolfPreySearchSkipLogInterval = 8f;

        private float nextWolfPreySearchSkipLogTime;

        public bool IsLandWildlifeTravelCell(Vector2Int cell, bool allowStructureBuffer = false)
        {
            return StrategyWildlifeRiverCrossing.IsLandOrRiverCell(map, cell)
                && (allowStructureBuffer || !IsLandWildlifeStructureBufferCell(cell));
        }

        public bool IsLandWildlifeStructureBufferCell(Vector2Int cell)
        {
            if (hasCampCell && ChebyshevDistance(cell, campCell) < WildlifeStructureAvoidanceRadius)
            {
                return true;
            }

            RefreshSettlementBuildingsIfNeeded();
            if (settlementBuildings != null)
            {
                for (int i = 0; i < settlementBuildings.Length; i++)
                {
                    StrategyPlacedBuilding building = settlementBuildings[i];
                    if (building != null && IsInsideStructureBuffer(cell, building.Origin, building.Footprint))
                    {
                        return true;
                    }
                }
            }

            if (settlementConstructionSites != null)
            {
                for (int i = 0; i < settlementConstructionSites.Length; i++)
                {
                    StrategyConstructionSite site = settlementConstructionSites[i];
                    if (site != null && !site.IsCompleted && IsInsideStructureBuffer(cell, site.Origin, site.Footprint))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsInsideStructureBuffer(Vector2Int cell, Vector2Int origin, Vector2Int footprint)
        {
            int width = Mathf.Max(1, footprint.x);
            int height = Mathf.Max(1, footprint.y);
            int maxX = origin.x + width - 1;
            int maxY = origin.y + height - 1;
            int dx = cell.x < origin.x ? origin.x - cell.x : cell.x > maxX ? cell.x - maxX : 0;
            int dy = cell.y < origin.y ? origin.y - cell.y : cell.y > maxY ? cell.y - maxY : 0;
            return Mathf.Max(dx, dy) < WildlifeStructureAvoidanceRadius;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private void LogWolfPreySearchSkipped(
            Vector2Int center,
            int rabbitCount,
            int rabbitSurplus,
            int deerCount,
            int deerSurplus)
        {
            if (Time.time < nextWolfPreySearchSkipLogTime)
            {
                return;
            }

            nextWolfPreySearchSkipLogTime = Time.time + WolfPreySearchSkipLogInterval;
            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfPreySearchSkipped",
                StrategyDebugLogger.F("cell", center),
                StrategyDebugLogger.F("rabbitCount", rabbitCount),
                StrategyDebugLogger.F("rabbitThreshold", WolfRabbitControlThreshold),
                StrategyDebugLogger.F("rabbitSurplus", rabbitSurplus),
                StrategyDebugLogger.F("deerCount", deerCount),
                StrategyDebugLogger.F("deerThreshold", WolfDeerControlThreshold),
                StrategyDebugLogger.F("deerSurplus", deerSurplus));
        }
    }
}
