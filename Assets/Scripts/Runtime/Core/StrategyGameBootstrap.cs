using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private const float InitialCameraSize = 18f;
        private const float InitialCampCameraSize = 11f;
        private const int CampNatureClearRadius = 3;
        private const int InitialStarterLogs = 20;
        private const int InitialStarterStone = 20;
        private const float StarterFoodReserveDays = 3f;

        private static IEnumerator BootstrapScene(StrategyGameContext context)
        {
            StrategyGameSettings.ApplyAtStartup();
            CityMapController map = ConfigureDebugLoggingAndMap(context, out bool requiresGeneration);
            if (requiresGeneration)
            {
                int requestedSeed = map.ResolveBootstrapSeed();
                yield return map.GenerateMapIncremental(requestedSeed);
                if (map.GenerationFailed)
                {
                    StrategyDebugLogger.Warn(
                        "Bootstrap",
                        "IncrementalMapFallback",
                        StrategyDebugLogger.F("seed", requestedSeed));
                    map.GenerateMap(requestedSeed);
                }

                if (!map.IsGenerated)
                {
                    throw new System.InvalidOperationException("Map generation did not complete.");
                }

                map.SetPresentationVisible(true);
            }

            StrategyDebugLogger.Info("Bootstrap", "MapReady", StrategyDebugLogger.F("bounds", map.WorldBounds));
            StrategyFoundingStartState foundingStart = ResolveFoundingStartState(map);
            StrategyInputRouter inputRouter = context.GetOrCreate<StrategyInputRouter>("Strategy Input Router");
            if (!inputRouter.Configure())
            {
                throw new System.InvalidOperationException(inputRouter.ConfigurationError);
            }

            StrategyUiInputModuleBootstrap.Ensure();
            StrategyDebugLogger.Info("Bootstrap", "InputReady");

            StrategyWaterAnimationController water = context.GetOrCreate<StrategyWaterAnimationController>("Strategy Water Animation");
            water.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "WaterReady");

            StrategyTrailController trails = context.GetOrCreate<StrategyTrailController>("Strategy Trails");
            trails.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "TrailsReady");
            ConfigureNavigation(context, map);
            StrategyWindController wind = context.GetOrCreate<StrategyWindController>("Strategy Wind");

            wind.ConfigureDefault();
            StrategyDebugLogger.Info("Bootstrap", "WindReady");

            StrategyTimeScaleController timeScale = context.GetOrCreate<StrategyTimeScaleController>("Strategy Time Scale");
            timeScale.SetInputRouter(inputRouter);
            timeScale.Configure();
            context.HoldBootstrapPause(timeScale);
            StrategyDebugLogger.Info("Bootstrap", "TimeScaleReady");

            StrategyForestryController forestry = context.GetOrCreate<StrategyForestryController>("Strategy Forestry");
            forestry.Configure(map, wind);
            StrategyDebugLogger.Info("Bootstrap", "ForestryReady");

            StrategyStoneResourceController stone = context.GetOrCreate<StrategyStoneResourceController>("Strategy Stone Resources");
            stone.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "StoneResourcesReady");

            StrategyIronResourceController iron = context.GetOrCreate<StrategyIronResourceController>("Strategy Iron Resources");
            iron.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "IronResourcesReady");

            StrategyCoalResourceController coal = context.GetOrCreate<StrategyCoalResourceController>("Strategy Coal Resources");
            coal.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "CoalResourcesReady");

            StrategyClayResourceController clay = context.GetOrCreate<StrategyClayResourceController>("Strategy Clay Resources");
            clay.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "ClayResourcesReady");

            Camera mainCamera = context.GetOrCreate<Camera>("Main Camera");
            mainCamera.gameObject.tag = "MainCamera";

            mainCamera.orthographic = true;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.09f, 0.12f, 0.14f);

            Vector3 cameraPosition = mainCamera.transform.position;
            cameraPosition.z = -10f;
            mainCamera.transform.position = cameraPosition;

            StrategyCameraController cameraController = mainCamera.GetComponent<StrategyCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<StrategyCameraController>();
            }

            context.Register(cameraController);
            cameraController.SetInputRouter(inputRouter);
            cameraController.SetBounds(map.WorldBounds);
            cameraController.FocusOn(map.WorldBounds.center, InitialCameraSize);
            StrategyCameraFeedbackController cameraFeedback = mainCamera.GetComponent<StrategyCameraFeedbackController>();
            if (cameraFeedback == null)
            {
                cameraFeedback = mainCamera.gameObject.AddComponent<StrategyCameraFeedbackController>();
            }

            context.Register(cameraFeedback);
            cameraFeedback.Configure(mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "CameraReady", StrategyDebugLogger.F("position", mainCamera.transform.position));

            StrategyDayNightCycleController dayNight = context.GetOrCreate<StrategyDayNightCycleController>("Strategy Day Night Cycle");
            dayNight.Configure(map, mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "DayNightReady");

            StrategyWeatherController weather = context.GetOrCreate<StrategyWeatherController>("Strategy Weather");
            weather.Configure(map, wind);

            StrategyWeatherVisualController weatherVisuals = context.GetOrCreate<StrategyWeatherVisualController>("Strategy Weather Visuals");
            weatherVisuals.Configure(map, mainCamera, weather, wind);

            StrategySeasonAmbientDetailController seasonDetails = context.GetOrCreate<StrategySeasonAmbientDetailController>("Strategy Season Ambient Details");
            seasonDetails.Configure(mainCamera, weather, wind);

            StrategySeasonalSurfaceController seasonalSurfaces = context.GetOrCreate<StrategySeasonalSurfaceController>("Strategy Seasonal Surfaces");
            seasonalSurfaces.Configure(map, weather);
            StrategyDebugLogger.Info("Bootstrap", "WeatherReady");

            StrategyPostProcessController postProcess = context.GetOrCreate<StrategyPostProcessController>("Strategy Post Process");
            postProcess.Configure(mainCamera, dayNight, weather);
            StrategyDebugLogger.Info("Bootstrap", "PostProcessReady");

            StrategyCinematicVisualController cinematicVisuals = context.GetOrCreate<StrategyCinematicVisualController>("Strategy Cinematic Visuals");
            cinematicVisuals.Configure(map, mainCamera, dayNight, weather, wind);
            StrategyDebugLogger.Info("Bootstrap", "CinematicVisualsReady");

            ConfigureAudio(context, map, mainCamera);
            yield return null;

            StrategyBuildMenuController buildMenu = context.GetOrCreate<StrategyBuildMenuController>("Strategy Build Menu");
            StrategyBuildPlacementController placement = context.GetOrCreate<StrategyBuildPlacementController>("Strategy Build Placement");
            StrategyPopulationController population = context.GetOrCreate<StrategyPopulationController>("Strategy Population");
            buildMenu.SetInputRouter(inputRouter);
            placement.SetInputRouter(inputRouter);

            population.SetFoundingStartState(foundingStart);
            population.Configure(map);
            cameraController.SetCampFocusSource(map, population);

            StrategyNightLightTaskController nightLights = context.GetOrCreate<StrategyNightLightTaskController>("Strategy Night Light Tasks");
            nightLights.Configure(map, population);
            StrategyDebugLogger.Info("Bootstrap", "NightLightTasksReady");

            if (population.TryGetCampWorld(out Vector3 campWorld))
            {
                cameraController.FocusOn(campWorld, InitialCampCameraSize);
                StrategyDebugLogger.Info("Bootstrap", "CameraFocusedOnCamp", StrategyDebugLogger.F("world", campWorld));
            }

            StrategyNaturePropController nature = context.GetOrCreate<StrategyNaturePropController>("Strategy Nature Props");
            if (foundingStart != null && foundingStart.HasCaravanOrigin)
            {
                nature.SetAdditionalExclusion(
                    foundingStart.CaravanOrigin,
                    StrategyStartSiteSelector.CaravanReservedFootprint);
            }

            if (population.TryGetCampCell(out Vector2Int campCell))
            {
                yield return nature.ConfigureIncremental(
                    map,
                    wind,
                    forestry,
                    stone,
                    campCell,
                    CampNatureClearRadius);
                StrategyDebugLogger.Info(
                    "Bootstrap",
                    "NatureReady",
                    StrategyDebugLogger.F("excludedCell", campCell),
                    StrategyDebugLogger.F("excludedRadius", CampNatureClearRadius));
            }
            else
            {
                nature.Configure(map, wind, forestry, stone);
                StrategyDebugLogger.Info("Bootstrap", "NatureReady", StrategyDebugLogger.F("excluded", false));
            }

            StrategyForageResourceController forage = context.GetOrCreate<StrategyForageResourceController>("Strategy Forage Resources");
            if (foundingStart != null && foundingStart.HasCaravanOrigin)
            {
                forage.SetAdditionalExclusion(
                    foundingStart.CaravanOrigin,
                    StrategyStartSiteSelector.CaravanReservedFootprint);
            }

            if (population.TryGetCampCell(out Vector2Int forageCampCell))
            {
                forage.Configure(map, forageCampCell);
            }
            else
            {
                forage.Configure(map);
            }

            StrategyDebugLogger.Info("Bootstrap", "ForageReady");

            StrategyFogOfWarController fog = context.GetOrCreate<StrategyFogOfWarController>("Strategy Fog Of War");
            StrategyBuildingUpgradeController upgrades = context.GetOrCreate<StrategyBuildingUpgradeController>("Strategy Building Upgrades");

            upgrades.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "UpgradesReady");

            fog.Configure(map, population, placement, weather);
            placement.Configure(map, buildMenu, mainCamera, population, fog, forestry, stone, upgrades);
            trails.ConfigureRouteNetwork(placement);
            if ((foundingStart == null || !foundingStart.IsRestoredFromSave)
                && population.TryGetCampCell(out Vector2Int starterCartCampCell))
            {
                float starterFoodRations = CalculateStarterFoodRations(population, StarterFoodReserveDays);
                bool cartPlaced = foundingStart != null
                    && foundingStart.HasCaravanOrigin
                    && placement.TryPlaceStarterCaravanCartAt(
                        foundingStart.CaravanOrigin,
                        starterCartCampCell,
                        InitialStarterLogs,
                        InitialStarterStone,
                        starterFoodRations);
                if (!cartPlaced)
                {
                    placement.TryPlaceStarterCaravanCart(
                        starterCartCampCell,
                        InitialStarterLogs,
                        InitialStarterStone,
                        starterFoodRations);
                }
            }

            ConfigureWorldChunks(context, map, population, mainCamera);
            cinematicVisuals.RefreshSceneLightingNow();
            StrategyDebugLogger.Info("Bootstrap", "FogAndPlacementReady");

            StrategyDebugPanelController debugPanel = context.GetOrCreate<StrategyDebugPanelController>("Strategy Debug Panel");
            debugPanel.SetInputRouter(inputRouter);
            debugPanel.Configure(fog, weather);
            StrategyDebugLogger.Info("Bootstrap", "DebugPanelReady");

            StrategySettlementTreasury treasury = context.GetOrCreate<StrategySettlementTreasury>("Strategy Settlement Treasury");
            treasury.Configure(0);
            StrategyDebugLogger.Info("Bootstrap", "TreasuryReady");

            StrategyTradeCaravanController tradeCaravans = context.GetOrCreate<StrategyTradeCaravanController>("Strategy Trade Caravans");
            tradeCaravans.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "TradeCaravansReady");

            StrategyWildlifeController wildlife = context.GetOrCreate<StrategyWildlifeController>("Strategy Wildlife");
            wildlife.Configure(map, population, fog);
            StrategyDebugLogger.Info("Bootstrap", "WildlifeReady");

            StrategySettlementFaunaController settlementFauna = context.GetOrCreate<StrategySettlementFaunaController>("Strategy Settlement Fauna");
            settlementFauna.Configure(map, population, fog, placement);
            StrategyDebugLogger.Info("Bootstrap", "SettlementFaunaReady");
            yield return null;

            StrategyConfirmationDialogController confirmationDialog = context.GetOrCreate<StrategyConfirmationDialogController>("Strategy Confirmation Dialog");
            confirmationDialog.SetInputRouter(inputRouter);
            confirmationDialog.Configure();

            StrategyWorldSelectionController selection = context.GetOrCreate<StrategyWorldSelectionController>("Strategy World Selection");
            selection.SetInputRouter(inputRouter);
            selection.Configure(mainCamera, buildMenu, upgrades, fog, population, forestry, placement, confirmationDialog, map);
            StrategyDebugLogger.Info("Bootstrap", "SelectionReady");

            StrategyAutoWorkforceController autoWorkforce = context.GetOrCreate<StrategyAutoWorkforceController>("Strategy Auto Workforce");
            autoWorkforce.Configure(population);
            StrategyDebugLogger.Info("Bootstrap", "AutoWorkforceReady");

            StrategyProfessionHudController professionHud = context.GetOrCreate<StrategyProfessionHudController>("Strategy Profession HUD");
            professionHud.SetInputRouter(inputRouter);
            professionHud.Configure(population, autoWorkforce);
            StrategyDebugLogger.Info("Bootstrap", "ProfessionHudReady");

            StrategyPopulationRosterHudController populationRosterHud = context.GetOrCreate<StrategyPopulationRosterHudController>("Strategy Population Roster HUD");
            populationRosterHud.SetInputRouter(inputRouter);
            populationRosterHud.Configure(population);
            StrategyFamilyTreeHudController familyTreeHud = context.GetOrCreate<StrategyFamilyTreeHudController>("Strategy Family Tree HUD");
            familyTreeHud.SetInputRouter(inputRouter);
            familyTreeHud.Configure(population, timeScale);
            familyTreeHud.SetOpen(false);
            populationRosterHud.SetFamilyTreeHud(familyTreeHud);
            StrategyDebugLogger.Info("Bootstrap", "PopulationRosterHudReady");

            StrategyTopStatusHudController topStatusHud = context.GetOrCreate<StrategyTopStatusHudController>("Strategy Top Status HUD");
            topStatusHud.Configure(population, populationRosterHud, dayNight);
            StrategyDebugLogger.Info("Bootstrap", "TopStatusHudReady");

            StrategyEventLogHudController eventLogHud = context.GetOrCreate<StrategyEventLogHudController>("Strategy Event Log HUD");
            eventLogHud.Configure();
            StrategyDebugLogger.Info("Bootstrap", "EventLogHudReady");

            ConfigureProgression(context, buildMenu, placement, population);

            StrategyRefugeeDialogController refugeeDialog = context.GetOrCreate<StrategyRefugeeDialogController>("Strategy Refugee Dialog");
            refugeeDialog.SetInputRouter(inputRouter);
            refugeeDialog.Configure();

            StrategyRefugeeArrivalController refugees = context.GetOrCreate<StrategyRefugeeArrivalController>("Strategy Refugee Arrivals");
            refugees.Configure(map, population, timeScale, refugeeDialog, fog);
            debugPanel.Configure(fog, weather, refugees);
            StrategyDebugLogger.Info("Bootstrap", "RefugeesReady");

            ConfigurePerformanceDiagnostics(context, map, population, wildlife, weather, timeScale);
            ConfigurePersistence(context, map, placement, population, inputRouter, foundingStart);
            StrategyDebugLogger.Info("Bootstrap", "Complete");
        }

    }
}
