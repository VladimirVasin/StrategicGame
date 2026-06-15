using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyGameBootstrap
    {
        private const float InitialCameraSize = 18f;
        private const float InitialCampCameraSize = 11f;
        private const int CampNatureClearRadius = 3;
        private const int InitialStorageLogs = 16;
        private const int InitialStorageStone = 12;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapScene()
        {
            StrategyDebugLogger debugLogger = Object.FindAnyObjectByType<StrategyDebugLogger>();
            if (debugLogger == null)
            {
                GameObject debugLoggerObject = new GameObject("Strategy Debug Logger");
                debugLogger = debugLoggerObject.AddComponent<StrategyDebugLogger>();
            }

            debugLogger.Configure();
            StrategyDebugLogger.Info("Bootstrap", "Start");

            CityMapController map = Object.FindAnyObjectByType<CityMapController>();
            if (map == null)
            {
                GameObject mapObject = new GameObject("City Map");
                map = mapObject.AddComponent<CityMapController>();
            }

            map.GenerateMap();
            StrategyDebugLogger.Info("Bootstrap", "MapReady", StrategyDebugLogger.F("bounds", map.WorldBounds));

            StrategyWaterAnimationController water = Object.FindAnyObjectByType<StrategyWaterAnimationController>();
            if (water == null)
            {
                GameObject waterObject = new GameObject("Strategy Water Animation");
                water = waterObject.AddComponent<StrategyWaterAnimationController>();
            }

            water.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "WaterReady");

            StrategyWindController wind = Object.FindAnyObjectByType<StrategyWindController>();
            if (wind == null)
            {
                GameObject windObject = new GameObject("Strategy Wind");
                wind = windObject.AddComponent<StrategyWindController>();
            }

            wind.ConfigureDefault();
            StrategyDebugLogger.Info("Bootstrap", "WindReady");

            StrategyTimeScaleController timeScale = Object.FindAnyObjectByType<StrategyTimeScaleController>();
            if (timeScale == null)
            {
                GameObject timeScaleObject = new GameObject("Strategy Time Scale");
                timeScale = timeScaleObject.AddComponent<StrategyTimeScaleController>();
            }

            timeScale.Configure();
            StrategyDebugLogger.Info("Bootstrap", "TimeScaleReady");

            StrategyForestryController forestry = Object.FindAnyObjectByType<StrategyForestryController>();
            if (forestry == null)
            {
                GameObject forestryObject = new GameObject("Strategy Forestry");
                forestry = forestryObject.AddComponent<StrategyForestryController>();
            }

            forestry.Configure(map, wind);
            StrategyDebugLogger.Info("Bootstrap", "ForestryReady");

            StrategyStoneResourceController stone = Object.FindAnyObjectByType<StrategyStoneResourceController>();
            if (stone == null)
            {
                GameObject stoneObject = new GameObject("Strategy Stone Resources");
                stone = stoneObject.AddComponent<StrategyStoneResourceController>();
            }

            stone.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "StoneResourcesReady");

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
            }

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

            cameraController.SetBounds(map.WorldBounds);
            cameraController.FocusOn(map.WorldBounds.center, InitialCameraSize);
            StrategyDebugLogger.Info("Bootstrap", "CameraReady", StrategyDebugLogger.F("position", mainCamera.transform.position));

            StrategyDayNightCycleController dayNight = Object.FindAnyObjectByType<StrategyDayNightCycleController>();
            if (dayNight == null)
            {
                GameObject dayNightObject = new GameObject("Strategy Day Night Cycle");
                dayNight = dayNightObject.AddComponent<StrategyDayNightCycleController>();
            }

            dayNight.Configure(map, mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "DayNightReady");

            StrategyWeatherController weather = Object.FindAnyObjectByType<StrategyWeatherController>();
            if (weather == null)
            {
                GameObject weatherObject = new GameObject("Strategy Weather");
                weather = weatherObject.AddComponent<StrategyWeatherController>();
            }

            weather.Configure(map, wind);

            StrategyWeatherVisualController weatherVisuals = Object.FindAnyObjectByType<StrategyWeatherVisualController>();
            if (weatherVisuals == null)
            {
                GameObject weatherVisualsObject = new GameObject("Strategy Weather Visuals");
                weatherVisuals = weatherVisualsObject.AddComponent<StrategyWeatherVisualController>();
            }

            weatherVisuals.Configure(map, mainCamera, weather, wind);
            StrategyDebugLogger.Info("Bootstrap", "WeatherReady");

            StrategyAmbientAudioController ambientAudio = Object.FindAnyObjectByType<StrategyAmbientAudioController>();
            if (ambientAudio == null)
            {
                GameObject ambientAudioObject = new GameObject("Strategy Ambient Audio");
                ambientAudio = ambientAudioObject.AddComponent<StrategyAmbientAudioController>();
            }

            ambientAudio.Configure(map, mainCamera);
            StrategyDebugLogger.Info(
                "Bootstrap",
                "AmbientAudioReady",
                StrategyDebugLogger.F("footstepClips", StrategyResidentFootstepAudio.LoadedClipCount));

            StrategyMusicController music = Object.FindAnyObjectByType<StrategyMusicController>();
            if (music == null)
            {
                GameObject musicObject = new GameObject("Strategy Music");
                music = musicObject.AddComponent<StrategyMusicController>();
            }

            music.Configure();
            StrategyDebugLogger.Info("Bootstrap", "MusicReady");

            StrategyBuildMenuController buildMenu = Object.FindAnyObjectByType<StrategyBuildMenuController>();
            if (buildMenu == null)
            {
                GameObject buildMenuObject = new GameObject("Strategy Build Menu");
                buildMenu = buildMenuObject.AddComponent<StrategyBuildMenuController>();
            }

            StrategyBuildPlacementController placement = Object.FindAnyObjectByType<StrategyBuildPlacementController>();
            if (placement == null)
            {
                GameObject placementObject = new GameObject("Strategy Build Placement");
                placement = placementObject.AddComponent<StrategyBuildPlacementController>();
            }

            StrategyPopulationController population = Object.FindAnyObjectByType<StrategyPopulationController>();
            if (population == null)
            {
                GameObject populationObject = new GameObject("Strategy Population");
                population = populationObject.AddComponent<StrategyPopulationController>();
            }

            population.Configure(map);
            if (population.TryGetCampWorld(out Vector3 campWorld))
            {
                cameraController.FocusOn(campWorld, InitialCampCameraSize);
                StrategyDebugLogger.Info("Bootstrap", "CameraFocusedOnCamp", StrategyDebugLogger.F("world", campWorld));
            }

            StrategyNaturePropController nature = Object.FindAnyObjectByType<StrategyNaturePropController>();
            if (nature == null)
            {
                GameObject natureObject = new GameObject("Strategy Nature Props");
                nature = natureObject.AddComponent<StrategyNaturePropController>();
            }

            if (population.TryGetCampCell(out Vector2Int campCell))
            {
                nature.Configure(map, wind, forestry, stone, campCell, CampNatureClearRadius);
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

            StrategyForageResourceController forage = Object.FindAnyObjectByType<StrategyForageResourceController>();
            if (forage == null)
            {
                GameObject forageObject = new GameObject("Strategy Forage Resources");
                forage = forageObject.AddComponent<StrategyForageResourceController>();
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

            StrategyFogOfWarController fog = Object.FindAnyObjectByType<StrategyFogOfWarController>();
            if (fog == null)
            {
                GameObject fogObject = new GameObject("Strategy Fog Of War");
                fog = fogObject.AddComponent<StrategyFogOfWarController>();
            }

            StrategyBuildingUpgradeController upgrades = Object.FindAnyObjectByType<StrategyBuildingUpgradeController>();
            if (upgrades == null)
            {
                GameObject upgradesObject = new GameObject("Strategy Building Upgrades");
                upgrades = upgradesObject.AddComponent<StrategyBuildingUpgradeController>();
            }

            upgrades.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "UpgradesReady");

            fog.Configure(map, population, placement);
            placement.Configure(map, buildMenu, mainCamera, population, fog, forestry, stone, upgrades);
            if (population.TryGetCampCell(out Vector2Int starterStorageCampCell))
            {
                placement.TryPlaceStarterStorageYard(starterStorageCampCell, InitialStorageLogs, InitialStorageStone);
            }

            StrategyDebugLogger.Info("Bootstrap", "FogAndPlacementReady");

            StrategyWildlifeController wildlife = Object.FindAnyObjectByType<StrategyWildlifeController>();
            if (wildlife == null)
            {
                GameObject wildlifeObject = new GameObject("Strategy Wildlife");
                wildlife = wildlifeObject.AddComponent<StrategyWildlifeController>();
            }

            wildlife.Configure(map, population);
            StrategyDebugLogger.Info("Bootstrap", "WildlifeReady");

            StrategyConfirmationDialogController confirmationDialog = Object.FindAnyObjectByType<StrategyConfirmationDialogController>();
            if (confirmationDialog == null)
            {
                GameObject confirmationDialogObject = new GameObject("Strategy Confirmation Dialog");
                confirmationDialog = confirmationDialogObject.AddComponent<StrategyConfirmationDialogController>();
            }

            confirmationDialog.Configure();

            StrategyWorldSelectionController selection = Object.FindAnyObjectByType<StrategyWorldSelectionController>();
            if (selection == null)
            {
                GameObject selectionObject = new GameObject("Strategy World Selection");
                selection = selectionObject.AddComponent<StrategyWorldSelectionController>();
            }

            selection.Configure(mainCamera, buildMenu, upgrades, fog, population, forestry, placement, confirmationDialog, map);
            StrategyDebugLogger.Info("Bootstrap", "SelectionReady");

            StrategyProfessionHudController professionHud = Object.FindAnyObjectByType<StrategyProfessionHudController>();
            if (professionHud == null)
            {
                GameObject professionHudObject = new GameObject("Strategy Profession HUD");
                professionHud = professionHudObject.AddComponent<StrategyProfessionHudController>();
            }

            professionHud.Configure(population);
            StrategyDebugLogger.Info("Bootstrap", "ProfessionHudReady");

            StrategyTopStatusHudController topStatusHud = Object.FindAnyObjectByType<StrategyTopStatusHudController>();
            if (topStatusHud == null)
            {
                GameObject topStatusHudObject = new GameObject("Strategy Top Status HUD");
                topStatusHud = topStatusHudObject.AddComponent<StrategyTopStatusHudController>();
            }

            topStatusHud.Configure(population);
            StrategyDebugLogger.Info("Bootstrap", "TopStatusHudReady");

            StrategyRefugeeDialogController refugeeDialog = Object.FindAnyObjectByType<StrategyRefugeeDialogController>();
            if (refugeeDialog == null)
            {
                GameObject refugeeDialogObject = new GameObject("Strategy Refugee Dialog");
                refugeeDialog = refugeeDialogObject.AddComponent<StrategyRefugeeDialogController>();
            }

            refugeeDialog.Configure();

            StrategyRefugeeArrivalController refugees = Object.FindAnyObjectByType<StrategyRefugeeArrivalController>();
            if (refugees == null)
            {
                GameObject refugeesObject = new GameObject("Strategy Refugee Arrivals");
                refugees = refugeesObject.AddComponent<StrategyRefugeeArrivalController>();
            }

            refugees.Configure(map, population, timeScale, refugeeDialog);
            StrategyDebugLogger.Info("Bootstrap", "RefugeesReady");
        }
    }
}
