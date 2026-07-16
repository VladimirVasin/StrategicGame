using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private const int MaxSavedMineralAmount = 1_000_000;

        private static bool ValidatePointsOfInterest(StrategySaveData data, out string reason)
        {
            HashSet<string> stableIds = new(StringComparer.Ordinal);
            HashSet<long> cells = new();
            List<StrategyPointOfInterestSaveData> mineralSites = new();
            for (int i = 0; i < data.pointsOfInterest.Count; i++)
            {
                StrategyPointOfInterestSaveData point = data.pointsOfInterest[i];
                if (point == null
                    || string.IsNullOrWhiteSpace(point.stableId)
                    || point.stableId.Length > 128
                    || !stableIds.Add(point.stableId))
                {
                    reason = "invalid_or_duplicate_point_of_interest_id_" + i;
                    return false;
                }

                long cellKey = (long)point.cellY * data.mapWidth + point.cellX;
                if (!IsCellInside(point.cellX, point.cellY, data.mapWidth, data.mapHeight)
                    || !cells.Add(cellKey))
                {
                    reason = "invalid_or_duplicate_point_of_interest_cell_" + i;
                    return false;
                }

                if (point.resourceKind < (int)StrategyPointOfInterestResourceKind.None
                    || point.resourceKind > (int)StrategyPointOfInterestResourceKind.Iron)
                {
                    reason = "invalid_point_of_interest_resource_kind_" + i;
                    return false;
                }

                if (OverlapsSavedWorld(point.cellX, point.cellY, data))
                {
                    reason = "point_of_interest_overlaps_world_" + i;
                    return false;
                }

                StrategyPointOfInterestResourceKind kind =
                    (StrategyPointOfInterestResourceKind)point.resourceKind;
                if (!ValidatePointMineralSite(point, kind, data, mineralSites, out reason))
                {
                    reason += "_" + i;
                    return false;
                }

                if (point.hasMineralSite)
                {
                    mineralSites.Add(point);
                }
            }

            if (!ValidateMineralSiteRelationships(data.pointsOfInterest, out reason))
            {
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidateMineralSiteRelationships(
            IReadOnlyList<StrategyPointOfInterestSaveData> points,
            out string reason)
        {
            for (int siteIndex = 0; siteIndex < points.Count; siteIndex++)
            {
                StrategyPointOfInterestSaveData site = points[siteIndex];
                if (site == null || !site.hasMineralSite)
                {
                    continue;
                }

                Vector2Int origin = new(site.mineralOriginX, site.mineralOriginY);
                for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
                {
                    if (pointIndex == siteIndex || points[pointIndex] == null)
                    {
                        continue;
                    }

                    StrategyPointOfInterestSaveData other = points[pointIndex];
                    int distance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                        new Vector2Int(other.cellX, other.cellY),
                        origin,
                        StrategyPointOfInterestPlacement.MineralFootprint);
                    bool conflictsWithNeutral = other.resourceKind
                            == (int)StrategyPointOfInterestResourceKind.None
                        && distance <= StrategyPointOfInterestPlacement.MineralFreeRadius;
                    bool belongsToAnotherPoint = other.resourceKind
                            != (int)StrategyPointOfInterestResourceKind.None
                        && distance <= StrategyPointOfInterestPlacement.MineralPointMaxDistance;
                    if (conflictsWithNeutral || belongsToAnotherPoint)
                    {
                        reason = "point_of_interest_mineral_site_has_ambiguous_owner_"
                            + siteIndex;
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidatePointMineralSite(
            StrategyPointOfInterestSaveData point,
            StrategyPointOfInterestResourceKind kind,
            StrategySaveData data,
            IReadOnlyList<StrategyPointOfInterestSaveData> existingSites,
            out string reason)
        {
            if (kind == StrategyPointOfInterestResourceKind.None)
            {
                bool empty = !point.hasMineralSite
                    && point.mineralOriginX == 0
                    && point.mineralOriginY == 0
                    && point.remainingMineralAmount == 0;
                reason = empty ? string.Empty : "neutral_point_has_mineral_site";
                return empty;
            }

            Vector2Int origin = new(point.mineralOriginX, point.mineralOriginY);
            Vector2Int pointCell = new(point.cellX, point.cellY);
            if (!point.hasMineralSite
                || point.remainingMineralAmount < 0
                || point.remainingMineralAmount > MaxSavedMineralAmount
                || !IsFootprintInsideMap(
                    origin.x,
                    origin.y,
                    StrategyPointOfInterestPlacement.MineralFootprint.x,
                    StrategyPointOfInterestPlacement.MineralFootprint.y,
                    data.mapWidth,
                    data.mapHeight))
            {
                reason = "invalid_point_of_interest_mineral_site";
                return false;
            }

            int distance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                pointCell,
                origin,
                StrategyPointOfInterestPlacement.MineralFootprint);
            if (distance < StrategyPointOfInterestPlacement.MineralPointMinDistance
                || distance > StrategyPointOfInterestPlacement.MineralPointMaxDistance
                || IsInsideCampMineralExclusion(origin, data))
            {
                reason = "point_of_interest_mineral_site_out_of_zone";
                return false;
            }

            for (int i = 0; i < existingSites.Count; i++)
            {
                StrategyPointOfInterestSaveData existing = existingSites[i];
                if (FootprintsTouchOrOverlap(
                        origin,
                        new Vector2Int(existing.mineralOriginX, existing.mineralOriginY)))
                {
                    reason = "duplicate_point_of_interest_mineral_site";
                    return false;
                }
            }

            if (point.remainingMineralAmount > 0
                && (MineralOverlapsInvalidSavedWorld(origin, kind, data)
                    || MineralOverlapsSavedTrail(origin, data)))
            {
                reason = "point_of_interest_mineral_site_overlaps_world";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool IsInsideCampMineralExclusion(Vector2Int origin, StrategySaveData data)
        {
            if (data.foundingStart == null || !data.foundingStart.hasStarterCamp)
            {
                return false;
            }

            Vector2Int camp = new(
                data.foundingStart.starterCampX,
                data.foundingStart.starterCampY);
            return StrategyPointOfInterestPlacement.DistanceToFootprint(
                    camp,
                    origin,
                    StrategyPointOfInterestPlacement.MineralFootprint)
                <= StrategyPointOfInterestPlacement.CampMineralExclusionRadius;
        }

        private static bool FootprintsTouchOrOverlap(Vector2Int left, Vector2Int right)
        {
            Vector2Int size = StrategyPointOfInterestPlacement.MineralFootprint;
            return left.x <= right.x + size.x
                && left.x + size.x >= right.x
                && left.y <= right.y + size.y
                && left.y + size.y >= right.y;
        }

        private static bool MineralOverlapsInvalidSavedWorld(
            Vector2Int origin,
            StrategyPointOfInterestResourceKind kind,
            StrategySaveData data)
        {
            for (int i = 0; i < data.buildings.Count; i++)
            {
                StrategyBuildingSaveData building = data.buildings[i];
                if (FootprintsOverlap(
                        origin.x,
                        origin.y,
                        2,
                        2,
                        building.originX,
                        building.originY,
                        building.footprintX,
                        building.footprintY)
                    && !IsMatchingExtractionTool(building.tool, kind))
                {
                    return true;
                }
            }

            for (int i = 0; i < data.constructionSites.Count; i++)
            {
                StrategyConstructionSiteSaveData site = data.constructionSites[i];
                if (FootprintsOverlap(
                        origin.x,
                        origin.y,
                        2,
                        2,
                        site.originX,
                        site.originY,
                        site.footprintX,
                        site.footprintY)
                    && !IsMatchingExtractionTool(site.tool, kind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MineralOverlapsSavedTrail(Vector2Int origin, StrategySaveData data)
        {
            for (int y = 0; y < StrategyPointOfInterestPlacement.MineralFootprint.y; y++)
            {
                for (int x = 0; x < StrategyPointOfInterestPlacement.MineralFootprint.x; x++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    int cellIndex = cell.y * data.mapWidth + cell.x;
                    for (int i = 0; i < data.trailCells.Count; i++)
                    {
                        if (data.trailCells[i] == cellIndex)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsMatchingExtractionTool(
            int tool,
            StrategyPointOfInterestResourceKind kind)
        {
            return kind == StrategyPointOfInterestResourceKind.Coal
                ? tool == (int)StrategyBuildTool.CoalPit
                : tool == (int)StrategyBuildTool.Mine;
        }

        private static bool OverlapsSavedWorld(int x, int y, StrategySaveData data)
        {
            for (int i = 0; i < data.buildings.Count; i++)
            {
                StrategyBuildingSaveData building = data.buildings[i];
                if (IsInsideFootprint(
                        x,
                        y,
                        building.originX,
                        building.originY,
                        building.footprintX,
                        building.footprintY))
                {
                    return true;
                }
            }

            for (int i = 0; i < data.constructionSites.Count; i++)
            {
                StrategyConstructionSiteSaveData site = data.constructionSites[i];
                if (IsInsideFootprint(
                        x,
                        y,
                        site.originX,
                        site.originY,
                        site.footprintX,
                        site.footprintY))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInsideFootprint(
            int x,
            int y,
            int originX,
            int originY,
            int width,
            int height)
        {
            return x >= originX
                && x < originX + width
                && y >= originY
                && y < originY + height;
        }

        private static bool FootprintsOverlap(
            int leftX,
            int leftY,
            int leftWidth,
            int leftHeight,
            int rightX,
            int rightY,
            int rightWidth,
            int rightHeight)
        {
            return leftX < rightX + rightWidth
                && leftX + leftWidth > rightX
                && leftY < rightY + rightHeight
                && leftY + leftHeight > rightY;
        }
    }
}
