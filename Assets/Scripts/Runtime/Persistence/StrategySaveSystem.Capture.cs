using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private StrategySaveData CaptureSaveData()
        {
            placement?.FlushPendingBuildingDemolitions();
            RefreshFoundingStartGeometry();
            StrategySaveData data = new()
            {
                savedUtcTicks = DateTime.UtcNow.Ticks,
                mapSeed = map.ActiveSeed,
                mapWidth = map.Width,
                mapHeight = map.Height,
                elapsedSeconds = StrategyDayNightCycleController.CurrentElapsedSeconds,
                weatherKind = StrategyWeatherController.Active != null
                    ? (int)StrategyWeatherController.Active.CurrentWeather
                    : (int)StrategyWeatherKind.Clear,
                foundingStart = CopyFoundingStartData(foundingStart)
            };

            StrategyFirstWinterController firstWinter = FindAnyObjectByType<StrategyFirstWinterController>();
            if (firstWinter != null)
            {
                data.firstWinterFoodPrepared = firstWinter.FoodPrepared;
                data.firstWinterFuelPrepared = firstWinter.FuelPrepared;
                data.firstWinterPassed = firstWinter.HasPassedFirstWinter;
            }

            CaptureBuildings(data);
            CaptureConstructionSites(data);
            CaptureResidents(data);
            CaptureLooseResources(data);
            FindAnyObjectByType<StrategyFogOfWarController>()?.CaptureExploredCells(data.exploredCells);
            StrategyTrailController.Active?.CapturePersistentTrailCells(data.trailCells);
            StrategyPointOfInterestController.Active?.CapturePersistentState(data.pointsOfInterest);
            return data;
        }

        private void CaptureBuildings(StrategySaveData save)
        {
            for (int i = 0; i < placement.PlacedBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = placement.PlacedBuildings[i];
                if (building == null)
                {
                    continue;
                }

                StrategyBuildingSaveData data = new()
                {
                    stableId = building.StableId,
                    tool = (int)building.Tool,
                    originX = building.Origin.x,
                    originY = building.Origin.y,
                    footprintX = building.Footprint.x,
                    footprintY = building.Footprint.y,
                    visualVariant = building.VisualVariant,
                    bridgeStartX = building.BridgeStartCell.x,
                    bridgeStartY = building.BridgeStartCell.y,
                    bridgeEndX = building.BridgeEndCell.x,
                    bridgeEndY = building.BridgeEndCell.y
                };
                CopyCells(building.BridgeCells, data.bridgeCells);
                StrategyResourceStore store = ResolvePrimaryStore(building);
                data.resourceAmounts = store?.CaptureAmounts();
                if (building.Tool == StrategyBuildTool.House && building.Resources != null)
                {
                    building.Resources.CapturePreparedDishState(
                        data.preparedDishIds,
                        data.preparedDishAmounts,
                        out data.leftoverRations);
                }

                save.buildings.Add(data);
            }
        }

        private static void CaptureConstructionSites(StrategySaveData save)
        {
            System.Collections.Generic.IReadOnlyList<StrategyConstructionSite> sites = StrategyConstructionSite.ActiveSites;
            for (int i = 0; i < sites.Count; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                StrategyConstructionSiteSaveData data = new()
                {
                    tool = (int)site.Tool,
                    title = site.Title,
                    originX = site.Origin.x,
                    originY = site.Origin.y,
                    footprintX = site.Footprint.x,
                    footprintY = site.Footprint.y,
                    visualVariant = site.VisualVariant,
                    costLogs = site.Cost.Logs,
                    costStone = site.Cost.Stone,
                    costPlanks = site.Cost.Planks,
                    deliveredLogs = site.DeliveredLogs,
                    deliveredStone = site.DeliveredStone,
                    deliveredPlanks = site.DeliveredPlanks,
                    progress = site.Progress,
                    hasBridgeSpan = site.HasBridgeSpan,
                    bridgeStartX = site.BridgeStartCell.x,
                    bridgeStartY = site.BridgeStartCell.y,
                    bridgeEndX = site.BridgeEndCell.x,
                    bridgeEndY = site.BridgeEndCell.y
                };
                CopyCells(site.BridgeCells, data.bridgeCells);
                save.constructionSites.Add(data);
            }
        }

        private void CaptureResidents(StrategySaveData save)
        {
            for (int i = 0; i < population.Residents.Count; i++)
            {
                StrategyResidentAgent resident = population.Residents[i];
                if (resident == null || resident.IsPendingRefugee)
                {
                    continue;
                }

                StrategyResidentSaveData data = new()
                {
                    residentId = resident.ResidentId,
                    homeStableId = resident.Home != null ? resident.Home.StableId : string.Empty,
                    gender = (int)resident.Gender,
                    lifeStage = (int)resident.LifeStage,
                    visualVariant = resident.VisualVariant,
                    fullName = resident.FullName,
                    familyName = resident.FamilyName,
                    ageYears = resident.AgeYears,
                    fatherId = resident.FatherId,
                    motherId = resident.MotherId,
                    worldX = resident.transform.position.x,
                    worldY = resident.transform.position.y,
                    nutritionDebt = resident.NutritionDebt,
                    daysHungry = resident.DaysHungry,
                    lastNutritionDayIndex = resident.LastNutritionDayIndex,
                    coldExposure = resident.ColdExposure,
                    lastColdResolutionDayIndex = resident.LastColdResolutionDayIndex
                };
                for (int child = 0; child < resident.ChildIds.Count; child++)
                {
                    data.childIds.Add(resident.ChildIds[child]);
                }

                save.residents.Add(data);
                resident.CaptureCarriedResourcesForSave(save.looseResources);
            }
        }

        private static void CaptureLooseResources(StrategySaveData save)
        {
            StrategyLooseConstructionResourcePile[] constructionPiles = FindObjectsByType<StrategyLooseConstructionResourcePile>();
            for (int i = 0; i < constructionPiles.Length; i++)
            {
                StrategyLooseConstructionResourcePile pile = constructionPiles[i];
                if (pile != null)
                {
                    save.looseResources.Add(new StrategyLooseResourceSaveData
                    {
                        constructionPile = true,
                        originX = pile.Origin.x,
                        originY = pile.Origin.y,
                        logs = pile.Logs,
                        stone = pile.Stone,
                        planks = pile.Planks
                    });
                }
            }

            StrategyLooseCarriedResourcePile[] resourcePiles = FindObjectsByType<StrategyLooseCarriedResourcePile>();
            for (int i = 0; i < resourcePiles.Length; i++)
            {
                StrategyLooseCarriedResourcePile pile = resourcePiles[i];
                if (pile != null && pile.Amount > 0)
                {
                    save.looseResources.Add(new StrategyLooseResourceSaveData
                    {
                        originX = pile.Origin.x,
                        originY = pile.Origin.y,
                        resource = (int)pile.Resource,
                        amount = pile.Amount,
                        preparedDishPile = pile.HasPreparedDishPayload,
                        preparedDishRecipeId = pile.PreparedDishRecipeId,
                        preparedDishAmount = pile.PreparedDishAmount,
                        preparedDishLeftoverRations = pile.PreparedDishLeftoverRations
                    });
                }
            }
        }

        private static void CopyCells(
            System.Collections.Generic.IReadOnlyList<Vector2Int> source,
            System.Collections.Generic.List<StrategyCellSaveData> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                target.Add(new StrategyCellSaveData(source[i].x, source[i].y));
            }
        }
    }
}
