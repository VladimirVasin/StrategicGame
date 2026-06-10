using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyGameBootstrap
    {
        private const float InitialCameraSize = 18f;
        private const float InitialCampCameraSize = 11f;
        private const int CampNatureClearRadius = 3;
        private const int InitialStorageLogs = 13;
        private const int InitialStorageStone = 9;

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

            StrategyFogOfWarController fog = Object.FindAnyObjectByType<StrategyFogOfWarController>();
            if (fog == null)
            {
                GameObject fogObject = new GameObject("Strategy Fog Of War");
                fog = fogObject.AddComponent<StrategyFogOfWarController>();
            }

            fog.Configure(map, population, placement);
            placement.Configure(map, buildMenu, mainCamera, population, fog, forestry, stone);
            if (population.TryGetCampCell(out Vector2Int starterStorageCampCell))
            {
                placement.TryPlaceStarterStorageYard(starterStorageCampCell, InitialStorageLogs, InitialStorageStone);
            }

            StrategyDebugLogger.Info("Bootstrap", "FogAndPlacementReady");

            StrategyBuildingUpgradeController upgrades = Object.FindAnyObjectByType<StrategyBuildingUpgradeController>();
            if (upgrades == null)
            {
                GameObject upgradesObject = new GameObject("Strategy Building Upgrades");
                upgrades = upgradesObject.AddComponent<StrategyBuildingUpgradeController>();
            }

            upgrades.Configure(map);
            StrategyDebugLogger.Info("Bootstrap", "UpgradesReady");

            StrategyWorldSelectionController selection = Object.FindAnyObjectByType<StrategyWorldSelectionController>();
            if (selection == null)
            {
                GameObject selectionObject = new GameObject("Strategy World Selection");
                selection = selectionObject.AddComponent<StrategyWorldSelectionController>();
            }

            selection.Configure(mainCamera, buildMenu, upgrades, fog, population, forestry);
            StrategyDebugLogger.Info("Bootstrap", "SelectionReady");
        }
    }
}
