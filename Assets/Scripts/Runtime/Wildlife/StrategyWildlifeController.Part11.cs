using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private const int WildlifeSettlementMinDistance = 7;
        private const int WildlifeSettlementMaxDistance = 36;
        private const int FishSettlementMinDistance = 6;
        private const int FishSettlementMaxDistance = 42;
        private const int WolfSettlementMinDistance = 16;
        private const int WolfSettlementMaxDistance = 44;

        private enum WildlifeSettlementSpawnKind
        {
            Deer,
            Rabbit,
            Fish,
            Bird,
            Wolf
        }

        private bool IsHiddenNearSettlementSpawnCell(Vector2Int cell, WildlifeSettlementSpawnKind kind)
        {
            return IsHiddenForWildlifeSpawn(cell) && IsNearSettlementForWildlife(cell, kind);
        }

        private bool IsHiddenForWildlifeSpawn(Vector2Int cell)
        {
            if (fog == null || !fog.IsPlayerFogEnabled)
            {
                return true;
            }

            return !fog.IsCellVisibleAtDaylightRange(cell);
        }

        private bool IsNearSettlementForWildlife(Vector2Int cell, WildlifeSettlementSpawnKind kind)
        {
            if (!TryGetNearestSettlementDistance(cell, out float distance))
            {
                return false;
            }

            GetWildlifeSettlementDistanceRange(kind, out int minDistance, out int maxDistance);
            return distance >= minDistance && distance <= maxDistance;
        }

        private bool TryGetNearestSettlementDistance(Vector2Int cell, out float distance)
        {
            distance = float.MaxValue;
            bool found = false;

            RefreshSettlementBuildingsIfNeeded();
            if (settlementBuildings != null)
            {
                for (int i = 0; i < settlementBuildings.Length; i++)
                {
                    StrategyPlacedBuilding building = settlementBuildings[i];
                    if (building == null)
                    {
                        continue;
                    }

                    distance = Mathf.Min(distance, GetDistanceToFootprint(cell, building.Origin, building.Footprint));
                    found = true;
                }
            }

            if (settlementConstructionSites != null)
            {
                for (int i = 0; i < settlementConstructionSites.Length; i++)
                {
                    StrategyConstructionSite site = settlementConstructionSites[i];
                    if (site == null || site.IsCompleted)
                    {
                        continue;
                    }

                    distance = Mathf.Min(distance, GetDistanceToFootprint(cell, site.Origin, site.Footprint));
                    found = true;
                }
            }

            if (!found && hasCampCell)
            {
                distance = Vector2Int.Distance(cell, campCell);
                found = true;
            }

            return found;
        }

        private static void GetWildlifeSettlementDistanceRange(
            WildlifeSettlementSpawnKind kind,
            out int minDistance,
            out int maxDistance)
        {
            switch (kind)
            {
                case WildlifeSettlementSpawnKind.Wolf:
                    minDistance = WolfSettlementMinDistance;
                    maxDistance = WolfSettlementMaxDistance;
                    return;
                case WildlifeSettlementSpawnKind.Fish:
                    minDistance = FishSettlementMinDistance;
                    maxDistance = FishSettlementMaxDistance;
                    return;
                default:
                    minDistance = WildlifeSettlementMinDistance;
                    maxDistance = WildlifeSettlementMaxDistance;
                    return;
            }
        }

        private static float GetDistanceToFootprint(Vector2Int cell, Vector2Int origin, Vector2Int footprint)
        {
            int width = Mathf.Max(1, footprint.x);
            int height = Mathf.Max(1, footprint.y);
            int maxX = origin.x + width - 1;
            int maxY = origin.y + height - 1;
            int dx = cell.x < origin.x ? origin.x - cell.x : cell.x > maxX ? cell.x - maxX : 0;
            int dy = cell.y < origin.y ? origin.y - cell.y : cell.y > maxY ? cell.y - maxY : 0;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private bool HasHiddenNearSettlementLakeFishCell(FishWaterRegion region)
        {
            if (region == null)
            {
                return false;
            }

            for (int i = 0; i < region.Cells.Count; i++)
            {
                if (IsLakeFishSpawnCandidate(region.Cells[i], region.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
