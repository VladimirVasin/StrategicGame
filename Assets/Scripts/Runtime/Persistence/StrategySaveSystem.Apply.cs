using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private void ApplyPendingLoad()
        {
            StrategySaveData data = pendingLoad;
            ReleasePendingPointOfInterestCells(map);
            StrategyCityInventoryRestoreFailure inventoryFailure =
                StrategyCityInventoryRestoreFailure.None;
            if (!ValidateSaveData(data, out string reason)
                || data.mapWidth != map.Width
                || data.mapHeight != map.Height
                || !TryRestoreCityInventory(
                    cityInventory,
                    data.cityItems,
                    out inventoryFailure))
            {
                if (string.IsNullOrEmpty(reason)
                    && inventoryFailure != StrategyCityInventoryRestoreFailure.None)
                {
                    reason = "city_inventory_" + inventoryFailure.ToString().ToLowerInvariant();
                }

                StrategyDebugLogger.Warn(
                    "Save",
                    "PendingLoadRejected",
                    StrategyDebugLogger.F("reason", string.IsNullOrEmpty(reason) ? "map_dimensions_changed" : reason));
                pendingLoad = null;
                StrategyPointOfInterestController.Active?.RestorePersistentState(null);
                return;
            }

            StrategyDayNightCycleController.RestoreElapsedSeconds(data.elapsedSeconds);
            StrategyPointOfInterestController.Active?.ClearForLoad();
            population.ClearResidentsForLoad();
            placement.ClearWorldForLoad();
            ClearLooseResources();
            bool regenerateLegacyPointsAfterWorld = data.pointsOfInterest == null
                || data.pointsOfInterest.Count <= 0;
            if (!regenerateLegacyPointsAfterWorld)
            {
                StrategyPointOfInterestController.Active?.RestorePersistentState(data.pointsOfInterest);
            }

            Dictionary<string, StrategyPlacedBuilding> buildingsById = new();
            for (int i = 0; i < data.buildings.Count; i++)
            {
                StrategyBuildingSaveData buildingData = data.buildings[i];
                StrategyPlacedBuilding building = placement.RestoreBuilding(buildingData);
                if (building == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(building.StableId))
                {
                    buildingsById[building.StableId] = building;
                }

                StrategyResourceStore store = ResolvePrimaryStore(building);
                store?.RestoreAmounts(buildingData.resourceAmounts);
                if (building.Tool == StrategyBuildTool.House && building.Resources != null)
                {
                    building.Resources.RestorePreparedDishState(
                        buildingData.preparedDishIds,
                        buildingData.preparedDishAmounts,
                        buildingData.leftoverRations);
                }
            }

            for (int i = 0; i < data.constructionSites.Count; i++)
            {
                placement.RestoreConstructionSite(data.constructionSites[i]);
            }

            if (regenerateLegacyPointsAfterWorld)
            {
                StrategyPointOfInterestController.Active?.RestorePersistentState(null);
            }

            for (int i = 0; i < data.residents.Count; i++)
            {
                population.RestoreResident(data.residents[i], buildingsById);
            }

            RestoreScoutLodges(data.scoutLodges, buildingsById);
            population.FinalizeResidentRestore();
            RestoreLooseResources(data.looseResources);
            StrategyTrailController.Active?.RestorePersistentTrailCells(data.trailCells);
            FindAnyObjectByType<StrategyFogOfWarController>()?.RestoreExploredCells(data.exploredCells);
            StrategyWeatherController.Active?.ForceWeather((StrategyWeatherKind)data.weatherKind);
            FindAnyObjectByType<StrategyStarterGoalSequenceController>()?.RefreshFromWorld();
            FindAnyObjectByType<StrategyFirstWinterController>()?.RestorePersistentState(
                data.firstWinterFoodPrepared,
                data.firstWinterFuelPrepared,
                data.firstWinterPassed);
            StrategyFirstNightFaunaEventController.Active?.RestoreStage(
                (StrategyFirstNightFaunaStage)data.firstNightFaunaStage);
            pendingLoad = null;
            StrategyEventLogHudController.Notify("Game loaded", new Color(0.58f, 0.78f, 0.92f));
            StrategyDebugLogger.Info(
                "Save",
                "Loaded",
                StrategyDebugLogger.F("buildings", data.buildings.Count),
                StrategyDebugLogger.F("sites", data.constructionSites.Count),
                StrategyDebugLogger.F("residents", data.residents.Count),
                StrategyDebugLogger.F("scoutLodges", data.scoutLodges.Count),
                StrategyDebugLogger.F("cityItems", data.cityItems.Count),
                StrategyDebugLogger.F("day", StrategyDayNightCycleController.CurrentDayIndex + 1));
        }

        private static void ClearLooseResources()
        {
            StrategyLooseConstructionResourcePile[] constructionPiles = FindObjectsByType<StrategyLooseConstructionResourcePile>();
            for (int i = 0; i < constructionPiles.Length; i++)
            {
                if (constructionPiles[i] != null)
                {
                    constructionPiles[i].ResourceStore.RestoreAmounts(null);
                    constructionPiles[i].gameObject.SetActive(false);
                    Destroy(constructionPiles[i].gameObject);
                }
            }

            StrategyLooseCarriedResourcePile[] resourcePiles = FindObjectsByType<StrategyLooseCarriedResourcePile>();
            for (int i = 0; i < resourcePiles.Length; i++)
            {
                if (resourcePiles[i] != null)
                {
                    resourcePiles[i].ResourceStore.RestoreAmounts(null);
                    resourcePiles[i].gameObject.SetActive(false);
                    Destroy(resourcePiles[i].gameObject);
                }
            }
        }

        private void RestoreLooseResources(IReadOnlyList<StrategyLooseResourceSaveData> savedResources)
        {
            if (savedResources == null)
            {
                return;
            }

            for (int i = 0; i < savedResources.Count; i++)
            {
                StrategyLooseResourceSaveData saved = savedResources[i];
                Vector2Int origin = new(saved.originX, saved.originY);
                Vector3 world = map.GetCellCenterWorld(origin.x, origin.y);
                if (saved.constructionPile)
                {
                    StrategyLooseConstructionResourcePile.Create(
                        map,
                        origin,
                        world,
                        saved.logs,
                        saved.stone,
                        saved.planks);
                }
                else if (saved.preparedDishPile)
                {
                    StrategyLooseCarriedResourcePile.CreatePreparedDishes(
                        map,
                        origin,
                        world,
                        saved.preparedDishRecipeId,
                        saved.preparedDishAmount,
                        saved.preparedDishLeftoverRations);
                }
                else
                {
                    StrategyLooseCarriedResourcePile.Create(
                        map,
                        origin,
                        world,
                        (StrategyResourceType)saved.resource,
                        saved.amount);
                }
            }
        }

        private static StrategyResourceStore ResolvePrimaryStore(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return null;
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                return building.Resources != null ? building.Resources.ResourceStore : null;
            }

            MonoBehaviour[] components = building.GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IStrategyResourceStoreOwner owner
                    && components[i] is not StrategyHouseResourceStore)
                {
                    return owner.ResourceStore;
                }
            }

            return null;
        }
    }
}
