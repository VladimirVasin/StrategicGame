using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static int visualCaptureStage;
        private static int visualCaptureWaitFrames;
        private static GameObject buildTooltipCaptureRoot;

        public static void RunGameplayVisualCapture()
        {
            File.WriteAllText(GetResultPath("GameplayVisualCapture.txt"), "RUNNING");
            visualCaptureStage = 0;
            visualCaptureWaitFrames = 0;
            StartPlayModeSmoke(SmokeKind.GameplayVisualCapture, GameplayScenePath);
        }

        public static void RunBuildTooltipVisualCapture()
        {
            File.WriteAllText(GetResultPath("BuildTooltipVisualCapture.txt"), "RUNNING");
            visualCaptureStage = 0;
            visualCaptureWaitFrames = 0;
            StartPlayModeSmoke(SmokeKind.BuildTooltipVisualCapture, MainMenuScenePath);
        }

        private static void UpdateBuildTooltipVisualCapture()
        {
            if (visualCaptureWaitFrames > 0)
            {
                visualCaptureWaitFrames--;
                return;
            }

            if (visualCaptureStage == 0)
            {
                Canvas[] existingCanvases =
                    UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
                for (int i = 0; i < existingCanvases.Length; i++)
                {
                    existingCanvases[i].gameObject.SetActive(false);
                }

                buildTooltipCaptureRoot = new GameObject("Build Tooltip Visual Capture");
                StrategyDebugOptions.SetInstantConstructionEnabled(true);
                StrategyBuildMenuController menu =
                    buildTooltipCaptureRoot.AddComponent<StrategyBuildMenuController>();
                menu.ClearAllowedTools();
                menu.ToggleMenu();
                UnityEngine.UI.Button extraction = FindCaptureComponent<UnityEngine.UI.Button>(
                    buildTooltipCaptureRoot,
                    "BuildCategory_Extraction");
                Require(extraction != null, "Extraction category missing");
                extraction.onClick.Invoke();
                visualCaptureWaitFrames = 30;
                visualCaptureStage = 1;
                return;
            }

            if (visualCaptureStage == 1)
            {
                StrategyHudTooltip tooltip = FindCaptureComponent<StrategyHudTooltip>(
                    buildTooltipCaptureRoot,
                    "BuildItem_LumberjackCamp");
                Require(tooltip != null, "Lumberjack build tooltip missing");
                Require(tooltip.gameObject.activeInHierarchy, "Lumberjack build item is not visible");
                tooltip.OnSelect(null);
                visualCaptureWaitFrames = 2;
                visualCaptureStage = 2;
                return;
            }

            if (visualCaptureStage == 2)
            {
                CaptureGameplayRender("VisualBuildTooltip_1280x720.png", 1280, 720);
                CaptureGameplayRender("VisualBuildTooltip_1484x839.png", 1484, 839);
                StrategyHudTooltip tooltip = FindCaptureComponent<StrategyHudTooltip>(
                    buildTooltipCaptureRoot,
                    "BuildItem_LumberjackCamp");
                tooltip.OnDeselect(null);
                tooltip.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                visualCaptureWaitFrames = 12;
                visualCaptureStage = 3;
                return;
            }

            CaptureGameplayRender("VisualBuildPlacement_1280x720.png", 1280, 720);
            CaptureGameplayRender("VisualBuildPlacement_1484x839.png", 1484, 839);
            StrategyDebugOptions.SetInstantConstructionEnabled(false);
            CompletePlayMode(true, "PASS: compact build browse and placement visuals captured");
        }

        private static T FindCaptureComponent<T>(GameObject root, string name)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.name == name)
                {
                    return components[i];
                }
            }

            return null;
        }

        internal static void VerifyVisualCatalog()
        {
            StrategyVisualCatalog catalog = Resources.Load<StrategyVisualCatalog>("Visual/StrategyVisualCatalog");
            Require(catalog != null, "Visual catalog resource is missing");
            Require(
                catalog.TryGetBuildingSprite(StrategyBuildTool.House, 0, out Sprite building) && building != null,
                "Visual catalog building sprite is missing");
            Require(
                catalog.TryGetResidentSprite(
                    StrategyResidentGender.Male,
                    StrategyResidentLifeStage.Adult,
                    StrategyResidentVisualPose.Idle,
                    0,
                    0,
                    out Sprite resident) && resident != null,
                "Visual catalog resident atlas is missing");
            Require(
                catalog.TryGetNatureSprite(StrategyNaturePropKind.LargeTree, 0, out Sprite nature) && nature != null,
                "Visual catalog nature sprite is missing");
            Require(
                catalog.TryGetTerrainSprite(CityMapCellKind.Grass, 0, out Sprite terrain)
                    && terrain != null
                    && terrain.texture != null
                    && terrain.texture.isReadable,
                "Visual catalog readable terrain swatch is missing");
            Require(
                catalog.TryGetSequenceSprite(StrategyVisualSequenceIds.StorageLogs, 0, out Sprite stock)
                    && stock != null,
                "Visual catalog stock sequence is missing");
            Require(Resources.Load<Font>("Fonts/Inter-Regular") != null, "Readable Inter UI font is missing");
            Sprite menuArt = Resources.Load<Sprite>("Visual/Menu/MainMenuKeyArt");
            Require(
                menuArt != null
                    && menuArt.texture != null
                    && menuArt.texture.filterMode == FilterMode.Point
                    && menuArt.texture.width >= 1280,
                "Generated point-filtered main-menu key art is missing");
            VerifyAuthoredHouseFamily();
            VerifyAuthoredHouseConstructionFamily(catalog);
            VerifyAuthoredForagerCamp(catalog);
        }

        internal static void VerifyTerrainPainterCharacterization()
        {
            System.Reflection.MethodInfo resetCatalog = typeof(StrategyTerrainTexturePainter).GetMethod(
                "ResetCatalogCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Require(resetCatalog != null, "Terrain painter catalog reset hook is missing");
            try
            {
                resetCatalog.Invoke(null, null);

            CityMapCell[,] cells =
            {
                {
                    new CityMapCell(0, 0, CityMapCellKind.Water, CityMapWaterKind.River, 0.02f),
                    new CityMapCell(0, 1, CityMapCellKind.Forest, CityMapWaterKind.None, 0.66f),
                    new CityMapCell(0, 2, CityMapCellKind.Meadow, CityMapWaterKind.None, 0.31f)
                },
                {
                    new CityMapCell(1, 0, CityMapCellKind.Shore, CityMapWaterKind.River, 0.08f),
                    new CityMapCell(1, 1, CityMapCellKind.Grass, CityMapWaterKind.None, 0.78f),
                    new CityMapCell(1, 2, CityMapCellKind.Dirt, CityMapWaterKind.None, 0.42f)
                },
                {
                    new CityMapCell(2, 0, CityMapCellKind.Dirt, CityMapWaterKind.None, 0.25f),
                    new CityMapCell(2, 1, CityMapCellKind.Meadow, CityMapWaterKind.None, 0.54f),
                    new CityMapCell(2, 2, CityMapCellKind.Forest, CityMapWaterKind.None, 0.87f)
                }
            };
            const int tilePixels = 8;
            const int textureWidth = tilePixels * 3;
            Color32[] pixels = new Color32[textureWidth * textureWidth];
            StrategyTerrainTexturePainter.PaintTile(
                pixels,
                textureWidth,
                cells,
                1,
                1,
                tilePixels,
                74123,
                true);

            ulong hash = 14695981039346656037UL;
            for (int y = tilePixels; y < tilePixels * 2; y++)
            {
                for (int x = tilePixels; x < tilePixels * 2; x++)
                {
                    Color32 pixel = pixels[y * textureWidth + x];
                    hash = (hash ^ pixel.r) * 1099511628211UL;
                    hash = (hash ^ pixel.g) * 1099511628211UL;
                    hash = (hash ^ pixel.b) * 1099511628211UL;
                    hash = (hash ^ pixel.a) * 1099511628211UL;
                }
            }

            const ulong expectedHash = 2423015668320230376UL;
            Require(
                hash == expectedHash,
                $"Terrain painter output changed: expected {expectedHash}, got {hash}");
            Debug.Log("TerrainPainterCharacterizationHash=" + hash);

            resetCatalog.Invoke(null, null);
            StrategyVisualCatalogProvider.Prewarm();
            const int catalogTilePixels = 16;
            const int catalogTextureWidth = catalogTilePixels * 3;
            Color32[] catalogPixels = new Color32[catalogTextureWidth * catalogTextureWidth];
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    StrategyTerrainTexturePainter.PaintTile(
                        catalogPixels,
                        catalogTextureWidth,
                        cells,
                        x,
                        y,
                        catalogTilePixels,
                        74123,
                        true);
                }
            }

            ulong catalogHash = 14695981039346656037UL;
            for (int i = 0; i < catalogPixels.Length; i++)
            {
                Color32 pixel = catalogPixels[i];
                catalogHash = (catalogHash ^ pixel.r) * 1099511628211UL;
                catalogHash = (catalogHash ^ pixel.g) * 1099511628211UL;
                catalogHash = (catalogHash ^ pixel.b) * 1099511628211UL;
                catalogHash = (catalogHash ^ pixel.a) * 1099511628211UL;
            }

            const ulong expectedCatalogHash = 3868434508176179381UL;
            Require(
                catalogHash == expectedCatalogHash,
                $"Catalog terrain painter output changed: expected {expectedCatalogHash}, got {catalogHash}");
            Debug.Log("TerrainPainterCatalogHash=" + catalogHash);
            }
            finally
            {
                resetCatalog.Invoke(null, null);
                StrategyVisualCatalogProvider.Prewarm();
            }
        }

        internal static void VerifyTerrainNoiseThroughput()
        {
            const int sampleCount = 4096;
            float checksum = 0f;
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < sampleCount; i++)
            {
                checksum += Mathf.PerlinNoise(i * 0.0137f, i * 0.0211f);
            }

            timer.Stop();
            Require(float.IsFinite(checksum), "Terrain noise produced a non-finite value");
            Require(
                timer.Elapsed.TotalSeconds < 2d,
                $"Terrain noise throughput regressed: {sampleCount} samples took {timer.Elapsed.TotalSeconds:F2}s");
            Debug.Log($"TerrainNoiseThroughput samples={sampleCount} durationMs={timer.Elapsed.TotalMilliseconds:F2}");
        }

        internal static void VerifyAudioImportProfiles()
        {
            VerifyAudioFolderLoadType("Assets/Resources/Audio/Music", AudioClipLoadType.Streaming);
            VerifyAudioFolderLoadType("Assets/Resources/Audio/Nature", AudioClipLoadType.CompressedInMemory);
        }

        internal static void VerifyAudioArchitecture()
        {
            Require(PlayerSettings.runInBackground,
                "Player must keep simulation updates running while unfocused");
            AudioMixer mixer = Resources.Load<AudioMixer>("Audio/StrategyAudioMixer");
            Require(mixer != null, "Strategy AudioMixer resource is missing");
            string[] busNames = System.Enum.GetNames(typeof(StrategyAudioBus));
            for (int i = 0; i < busNames.Length; i++)
            {
                Require(mixer.FindMatchingGroups(busNames[i]).Length > 0,
                    "AudioMixer group is missing: " + busNames[i]);
            }

            Require(StrategyAudioVoicePool.Capacity >= 12 && StrategyAudioVoicePool.Capacity <= 24,
                "World audio voice pool must stay bounded");
            Require(StrategyWorldAudioDirector.WolfHowlCooldownSeconds >= 90f,
                "Wolf howl cadence must remain rare across the whole map");
            Require(StrategyWorldAudioDirector.WolfHowlConcurrencyLimit == 1,
                "Wolf howls must not overlap");
            Require((int)StrategyAudioBus.ImportantEvents > (int)StrategyAudioBus.Footsteps,
                "Audio priority buses are missing");
            AudioClip fire = StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.Fire);
            AudioClip settlement = StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.SettlementDay);
            AudioClip completion = StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.BuildComplete);
            Require(fire != null && fire.length >= 4f, "Procedural fire soundscape is missing");
            Require(settlement != null && settlement.length >= 6f, "Settlement soundscape is missing");
            Require(completion != null && completion.length >= 0.8f, "Construction completion SFX is missing");
        }

        private static void VerifyAudioFolderLoadType(string folder, AudioClipLoadType expectedLoadType)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            Require(guids.Length > 0, "Audio folder is empty: " + folder);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Require(importer != null, "Audio importer is missing: " + path);
                Require(
                    importer.defaultSampleSettings.loadType == expectedLoadType,
                    "Unexpected audio load type: " + path);
                Require(importer.loadInBackground, "Long audio must load in background: " + path);
            }
        }

        private static void VerifyBuildingGroundDetails()
        {
            StrategyPlacedBuilding[] buildings =
                UnityEngine.Object.FindObjectsByType<StrategyPlacedBuilding>();
            int checkedBuildings = 0;
            for (int i = 0; i < buildings.Length; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null || building.Tool == StrategyBuildTool.Bridge)
                {
                    continue;
                }

                StrategyBuildingGroundDetail detail = building.GetComponent<StrategyBuildingGroundDetail>();
                Require(detail != null && detail.HasVisibleDetail,
                    $"Building ground detail missing for {building.Tool}");
                checkedBuildings++;
            }

            Require(checkedBuildings > 0, "No placed building ground details were verified");
        }

        private static void UpdateGameplayVisualCapture(
            CityMapController map,
            StrategyPopulationController population)
        {
            if (visualCaptureWaitFrames > 0)
            {
                visualCaptureWaitFrames--;
                return;
            }

            switch (visualCaptureStage)
            {
                case 0:
                    PrepareVisualCaptureView(map, population);
                    StrategyDayNightCycleController.RestoreElapsedSeconds(
                        StrategyDayNightCycleController.DayLengthSeconds * 7f + 105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Clear);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 60;
                    visualCaptureStage = 1;
                    break;
                case 1:
                    CaptureGameplayRender("VisualNoon.png");
                    CaptureGameplayRender("VisualHud_1280x720.png", 1280, 720);
                    CaptureGameplayRender("VisualHud_1366x768.png", 1366, 768);
                    CaptureGameplayRender("VisualHud_1484x839.png", 1484, 839);
                    CaptureGameplayRender("VisualHud_1920x1080.png", 1920, 1080);
                    CaptureGameplayRender("VisualHud_2560x1440.png", 2560, 1440);
                    CaptureGameplayRender("VisualHud_3440x1440.png", 3440, 1440);
                    StrategyResourceOverviewHudController resourceOverview =
                        UnityEngine.Object.FindAnyObjectByType<StrategyResourceOverviewHudController>();
                    Require(resourceOverview != null, "Resource overview HUD missing");
                    resourceOverview.SetOpen(true, true, false);
                    CaptureGameplayRender("VisualResourceOverview_1280x720.png", 1280, 720);
                    CaptureGameplayRender("VisualResourceOverview_1484x839.png", 1484, 839);
                    resourceOverview.SetOpen(false, true, false);
                    CaptureProfessionHudFrames();
                    StrategyBuildMenuController menu =
                        UnityEngine.Object.FindAnyObjectByType<StrategyBuildMenuController>();
                    menu?.ClearAllowedTools();
                    menu?.ToggleMenu();
                    GameObject productionCategory = GameObject.Find("BuildCategory_Production");
                    productionCategory?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                    visualCaptureWaitFrames = 30;
                    visualCaptureStage = 2;
                    break;
                case 2:
                    CaptureGameplayRender("VisualBuildMenu.png");
                    CaptureGameplayRender("VisualBuildMenu_1280x720.png", 1280, 720);
                    CaptureGameplayRender("VisualBuildMenu_1484x839.png", 1484, 839);
                    GameObject housingCategory = GameObject.Find("BuildCategory_Housing");
                    housingCategory?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                    GameObject extractionCategory = GameObject.Find("BuildCategory_Extraction");
                    extractionCategory?.GetComponent<UnityEngine.UI.Button>()?.onClick.Invoke();
                    visualCaptureWaitFrames = 30;
                    visualCaptureStage = 3;
                    break;
                case 3:
                    CaptureGameplayRender("VisualBuildBrowse_1280x720.png", 1280, 720);
                    CaptureGameplayRender("VisualBuildBrowse_1484x839.png", 1484, 839);
                    GameObject lumberjackItem = GameObject.Find("BuildItem_LumberjackCamp");
                    Require(lumberjackItem != null, "Lumberjack build item missing");
                    StrategyHudTooltip itemTooltip = lumberjackItem.GetComponent<StrategyHudTooltip>();
                    Require(itemTooltip != null, "Lumberjack build tooltip missing");
                    itemTooltip.OnSelect(null);
                    visualCaptureWaitFrames = 2;
                    visualCaptureStage = 4;
                    break;
                case 4:
                    CaptureGameplayRender("VisualBuildTooltip_1280x720.png", 1280, 720);
                    CaptureGameplayRender("VisualBuildTooltip_1484x839.png", 1484, 839);
                    GameObject.Find("BuildItem_LumberjackCamp")
                        ?.GetComponent<StrategyHudTooltip>()
                        ?.OnDeselect(null);
                    UnityEngine.Object.FindAnyObjectByType<StrategyBuildMenuController>()?.ToggleMenu();
                    StrategyDayNightCycleController.RestoreElapsedSeconds(105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Clear);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 40;
                    visualCaptureStage = 5;
                    break;
                case 5:
                    CaptureGameplayRender("VisualSpring.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(
                        StrategyDayNightCycleController.DayLengthSeconds * 14f + 105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Clear);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 40;
                    visualCaptureStage = 6;
                    break;
                case 6:
                    CaptureGameplayRender("VisualAutumn.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(270f);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 16;
                    visualCaptureStage = 7;
                    break;
                case 7:
                    CaptureGameplayRender("VisualNight.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(
                        StrategyDayNightCycleController.DayLengthSeconds * 21f + 105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Blizzard);
                    StrategySeasonalSurfaceController seasonal =
                        UnityEngine.Object.FindAnyObjectByType<StrategySeasonalSurfaceController>();
                    seasonal?.DebugSetCoverage(0.88f, 0.82f);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 16;
                    visualCaptureStage = 8;
                    break;
                default:
                    CaptureGameplayRender("VisualWinter.png");
                    CompletePlayMode(true, "PASS: HUD, build menu, and seasonal visuals captured");
                    break;
            }
        }

        private static void PrepareVisualCaptureView(
            CityMapController map,
            StrategyPopulationController population)
        {
            StrategyFirstNightFaunaEventController.Active?.RestoreStage(
                StrategyFirstNightFaunaStage.StoryCompleted);
            Require(population.TryGetCampCell(out Vector2Int campCell), "Camp focus cell missing");
            StrategyCameraController cameraController =
                UnityEngine.Object.FindAnyObjectByType<StrategyCameraController>();
            Require(cameraController != null, "Strategy camera controller missing");
            cameraController.FocusOn(map.GetCellCenterWorld(campCell.x, campCell.y), 11f);

            StrategyBuildPlacementController placement =
                UnityEngine.Object.FindAnyObjectByType<StrategyBuildPlacementController>();
            StrategyWorldSelectionController selection =
                UnityEngine.Object.FindAnyObjectByType<StrategyWorldSelectionController>();
            if (selection != null && population.Residents.Count > 0)
            {
                selection.SelectResident(population.Residents[0]);
            }
            else if (placement != null && selection != null && placement.PlacedBuildings.Count > 0)
            {
                selection.SelectBuilding(placement.PlacedBuildings[0]);
            }
        }

        private static void RefreshVisualLighting()
        {
            StrategyCinematicVisualController visuals =
                UnityEngine.Object.FindAnyObjectByType<StrategyCinematicVisualController>();
            visuals?.RefreshSceneLightingNow();
        }

    }
}
