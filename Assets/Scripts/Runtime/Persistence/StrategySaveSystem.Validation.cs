using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private const int MaxSaveMapDimension = 1024;
        private const long MaxSaveMapCells = 1_048_576;
        private const int MaxSaveBuildings = 65_536;
        private const int MaxSaveConstructionSites = 65_536;
        private const int MaxSaveResidents = 100_000;
        private const int MaxSaveLooseResources = 262_144;
        private const int MaxSavePointsOfInterest = 256;
        internal const int MaxSaveStoryPointsOfInterest = 256;
        internal const int MaxSaveCityItems = 4_096;
        internal const int MaxSaveCityItemIdLength = StrategyCityItemDefinition.MaximumIdLength;
        internal const int MaxSaveCityItemQuantity = StrategyCityItemDefinition.MaximumQuantity;
        internal const int MaxSaveResidentItemsPerResident = StrategyResidentPersonalInventory.SlotCapacity;
        internal const int MaxSaveResidentItemIdLength = StrategyResidentItemDefinition.MaximumIdLength;
        internal const int MaxSaveResidentItemQuantity = StrategyResidentItemDefinition.MaximumQuantity;
        internal const int MaxSaveChildLinksPerResident = 256;
        internal const int MaxSavePreparedDishesPerBuilding = 256;
        internal const int MaxSavePreparedDishIdLength = 128;

        internal static bool ValidateSaveData(StrategySaveData data, out string reason)
        {
            if (data == null)
            {
                reason = "empty_or_invalid_json";
                return false;
            }

            if (data.version != StrategySaveData.CurrentVersion)
            {
                reason = "unsupported_version_" + data.version;
                return false;
            }

            if (!Enum.IsDefined(typeof(StrategyFirstNightFaunaStage), data.firstNightFaunaStage))
            {
                reason = "invalid_first_night_fauna_stage";
                return false;
            }

            long cellCount = (long)data.mapWidth * data.mapHeight;
            if (data.mapSeed <= 0
                || data.mapWidth <= 0
                || data.mapHeight <= 0
                || data.mapWidth > MaxSaveMapDimension
                || data.mapHeight > MaxSaveMapDimension
                || cellCount > MaxSaveMapCells)
            {
                reason = "invalid_map_metadata";
                return false;
            }

            if (!IsFinite(data.elapsedSeconds) || data.elapsedSeconds < 0f)
            {
                reason = "invalid_elapsed_time";
                return false;
            }

            if (!Enum.IsDefined(typeof(StrategyWeatherKind), data.weatherKind))
            {
                reason = "invalid_weather_kind";
                return false;
            }

            if (!ValidateFoundingStart(data.foundingStart, data.mapWidth, data.mapHeight, out reason))
            {
                return false;
            }

            if (data.buildings == null
                || data.constructionSites == null
                || data.residents == null
                || data.looseResources == null
                || data.pointsOfInterest == null
                || data.storyPointsOfInterest == null
                || data.cityItems == null
                || data.scoutLodges == null
                || data.exploredCells == null
                || data.trailCells == null)
            {
                reason = "missing_collections";
                return false;
            }

            if (data.buildings.Count > Math.Min(cellCount, MaxSaveBuildings)
                || data.constructionSites.Count > Math.Min(cellCount, MaxSaveConstructionSites)
                || data.residents.Count > MaxSaveResidents
                || data.looseResources.Count > MaxSaveLooseResources
                || data.pointsOfInterest.Count > MaxSavePointsOfInterest
                || data.storyPointsOfInterest.Count > MaxSaveStoryPointsOfInterest
                || data.cityItems.Count > MaxSaveCityItems
                || data.scoutLodges.Count > data.buildings.Count
                || data.exploredCells.Count > cellCount
                || data.trailCells.Count > cellCount)
            {
                reason = "collection_limit_exceeded";
                return false;
            }

            HashSet<string> buildingIds = new(StringComparer.Ordinal);
            if (!ValidateBuildings(data, buildingIds, out reason)
                || !ValidateConstructionSites(data, out reason)
                || !ValidateResidents(data, buildingIds, out reason)
                || !ValidateLooseResources(data, out reason)
                || !ValidatePointsOfInterest(data, out reason)
                || !ValidateStoryPointsOfInterest(data, out reason)
                || !ValidateCityItems(data.cityItems, out reason)
                || !ValidateScoutLodges(data, out reason)
                || !ValidateCellIndices(data.exploredCells, cellCount, "explored", out reason)
                || !ValidateCellIndices(data.trailCells, cellCount, "trail", out reason))
            {
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidateBuildings(
            StrategySaveData data,
            HashSet<string> buildingIds,
            out string reason)
        {
            int resourceCount = Enum.GetValues(typeof(StrategyResourceType)).Length;
            for (int i = 0; i < data.buildings.Count; i++)
            {
                StrategyBuildingSaveData building = data.buildings[i];
                if (building == null)
                {
                    reason = "null_building_" + i;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(building.stableId)
                    || building.stableId.Length > 128
                    || !buildingIds.Add(building.stableId))
                {
                    reason = "invalid_or_duplicate_building_id_" + i;
                    return false;
                }

                if (!IsBuildTool(building.tool)
                    || !IsFootprintInsideMap(
                        building.originX,
                        building.originY,
                        building.footprintX,
                        building.footprintY,
                        data.mapWidth,
                        data.mapHeight))
                {
                    reason = "invalid_building_geometry_" + i;
                    return false;
                }

                if (!ValidateCells(building.bridgeCells, data.mapWidth, data.mapHeight)
                    || building.resourceAmounts != null
                    && (building.resourceAmounts.Length > resourceCount
                        || !AllNonNegative(building.resourceAmounts)))
                {
                    reason = "invalid_building_resources_or_bridge_" + i;
                    return false;
                }

                if (building.preparedDishIds == null
                    || building.preparedDishAmounts == null
                    || building.preparedDishIds.Count != building.preparedDishAmounts.Count
                    || !IsFinite(building.leftoverRations)
                    || building.leftoverRations < 0f)
                {
                    reason = "invalid_household_food_state_" + i;
                    return false;
                }

                if (building.preparedDishIds.Count > MaxSavePreparedDishesPerBuilding)
                {
                    reason = "prepared_dish_limit_exceeded_" + i;
                    return false;
                }

                HashSet<string> preparedDishIds = new(StringComparer.Ordinal);
                for (int dishIndex = 0; dishIndex < building.preparedDishIds.Count; dishIndex++)
                {
                    string dishId = building.preparedDishIds[dishIndex];
                    if (string.IsNullOrWhiteSpace(dishId)
                        || dishId.Length > MaxSavePreparedDishIdLength
                        || !preparedDishIds.Add(dishId)
                        || building.preparedDishAmounts[dishIndex] <= 0)
                    {
                        reason = "invalid_prepared_dish_state_" + i;
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidateConstructionSites(StrategySaveData data, out string reason)
        {
            for (int i = 0; i < data.constructionSites.Count; i++)
            {
                StrategyConstructionSiteSaveData site = data.constructionSites[i];
                if (site == null
                    || !IsBuildTool(site.tool)
                    || !IsFootprintInsideMap(
                        site.originX,
                        site.originY,
                        site.footprintX,
                        site.footprintY,
                        data.mapWidth,
                        data.mapHeight)
                    || !ValidateCells(site.bridgeCells, data.mapWidth, data.mapHeight)
                    || site.costLogs < 0
                    || site.costStone < 0
                    || site.costPlanks < 0
                    || site.deliveredLogs < 0
                    || site.deliveredStone < 0
                    || site.deliveredPlanks < 0
                    || site.deliveredLogs > site.costLogs
                    || site.deliveredStone > site.costStone
                    || site.deliveredPlanks > site.costPlanks
                    || !IsFinite(site.progress)
                    || site.progress < 0f
                    || site.progress > 1f)
                {
                    reason = "invalid_construction_site_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidateResidents(
            StrategySaveData data,
            HashSet<string> buildingIds,
            out string reason)
        {
            HashSet<int> residentIds = new();
            for (int i = 0; i < data.residents.Count; i++)
            {
                StrategyResidentSaveData resident = data.residents[i];
                if (resident == null
                    || resident.residentId <= 0
                    || !residentIds.Add(resident.residentId)
                    || !Enum.IsDefined(typeof(StrategyResidentGender), resident.gender)
                    || !Enum.IsDefined(typeof(StrategyResidentLifeStage), resident.lifeStage)
                    || !IsFinite(resident.worldX)
                    || !IsFinite(resident.worldY)
                    || !IsFinite(resident.ageYears)
                    || resident.ageYears < 0f
                    || !IsFinite(resident.nutritionDebt)
                    || resident.nutritionDebt < 0f
                    || resident.daysHungry < 0
                    || !IsFinite(resident.coldExposure)
                    || resident.coldExposure < 0f
                    || resident.childIds == null)
                {
                    reason = "invalid_resident_" + i;
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(resident.homeStableId)
                    && !buildingIds.Contains(resident.homeStableId))
                {
                    reason = "missing_resident_home_" + i;
                    return false;
                }

                if (resident.childIds.Count > MaxSaveChildLinksPerResident)
                {
                    reason = "resident_child_limit_exceeded_" + i;
                    return false;
                }

                if (!ValidateResidentPersonalItems(resident, i, out reason))
                {
                    return false;
                }

                if (resident.fatherId < 0
                    || resident.motherId < 0
                    || resident.fatherId == resident.residentId
                    || resident.motherId == resident.residentId
                    || resident.fatherId > 0 && resident.fatherId == resident.motherId)
                {
                    reason = "invalid_resident_family_links_" + i;
                    return false;
                }

                HashSet<int> childIds = new();
                for (int childIndex = 0; childIndex < resident.childIds.Count; childIndex++)
                {
                    int childId = resident.childIds[childIndex];
                    if (childId <= 0
                        || childId == resident.residentId
                        || !childIds.Add(childId))
                    {
                        reason = "invalid_resident_family_links_" + i;
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidateCellIndices(
            IReadOnlyList<int> cells,
            long cellCount,
            string label,
            out string reason)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] < 0 || cells[i] >= cellCount)
                {
                    reason = "invalid_" + label + "_cell_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool ValidateCells(
            IReadOnlyList<StrategyCellSaveData> cells,
            int width,
            int height)
        {
            if (cells == null || cells.Count > (long)width * height)
            {
                return false;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                if (!IsCellInside(cells[i].x, cells[i].y, width, height))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBuildTool(int value)
        {
            return value != (int)StrategyBuildTool.None
                && Enum.IsDefined(typeof(StrategyBuildTool), value);
        }

        private static bool IsFootprintInsideMap(
            int x,
            int y,
            int footprintX,
            int footprintY,
            int width,
            int height)
        {
            return footprintX > 0
                && footprintY > 0
                && IsCellInside(x, y, width, height)
                && (long)x + footprintX <= width
                && (long)y + footprintY <= height;
        }

        private static bool IsCellInside(int x, int y, int width, int height)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        private static bool AllNonNegative(IReadOnlyList<int> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
