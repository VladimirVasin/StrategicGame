using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static int visualCaptureStage;
        private static int visualCaptureWaitFrames;

        public static void RunGameplayVisualCapture()
        {
            File.WriteAllText(GetResultPath("GameplayVisualCapture.txt"), "RUNNING");
            visualCaptureStage = 0;
            visualCaptureWaitFrames = 0;
            StartPlayModeSmoke(SmokeKind.GameplayVisualCapture, GameplayScenePath);
        }

        private static void VerifyVisualCatalog()
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
            Require(Resources.Load<Font>("Fonts/PixelifySans") != null, "Pixel UI font is missing");
        }

        private static void VerifyAudioImportProfiles()
        {
            VerifyAudioFolderLoadType("Assets/Resources/Audio/Music", AudioClipLoadType.Streaming);
            VerifyAudioFolderLoadType("Assets/Resources/Audio/Nature", AudioClipLoadType.CompressedInMemory);
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
                    StrategyDayNightCycleController.RestoreElapsedSeconds(105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Clear);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 60;
                    visualCaptureStage = 1;
                    break;
                case 1:
                    CaptureGameplayRender("VisualNoon.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(
                        StrategyDayNightCycleController.DayLengthSeconds * 7f + 105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Clear);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 40;
                    visualCaptureStage = 2;
                    break;
                case 2:
                    CaptureGameplayRender("VisualSpring.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(
                        StrategyDayNightCycleController.DayLengthSeconds * 14f + 105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Clear);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 40;
                    visualCaptureStage = 3;
                    break;
                case 3:
                    CaptureGameplayRender("VisualAutumn.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(270f);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 16;
                    visualCaptureStage = 4;
                    break;
                case 4:
                    CaptureGameplayRender("VisualNight.png");
                    StrategyDayNightCycleController.RestoreElapsedSeconds(
                        StrategyDayNightCycleController.DayLengthSeconds * 21f + 105f);
                    StrategyWeatherController.Active?.ForceWeather(StrategyWeatherKind.Blizzard);
                    StrategySeasonalSurfaceController seasonal =
                        UnityEngine.Object.FindAnyObjectByType<StrategySeasonalSurfaceController>();
                    seasonal?.DebugSetCoverage(0.88f, 0.82f);
                    RefreshVisualLighting();
                    visualCaptureWaitFrames = 16;
                    visualCaptureStage = 5;
                    break;
                default:
                    CaptureGameplayRender("VisualWinter.png");
                    CompletePlayMode(true, "PASS: noon, spring, autumn, night, and winter visuals captured");
                    break;
            }
        }

        private static void PrepareVisualCaptureView(
            CityMapController map,
            StrategyPopulationController population)
        {
            Require(population.TryGetCampCell(out Vector2Int campCell), "Camp focus cell missing");
            StrategyCameraController cameraController =
                UnityEngine.Object.FindAnyObjectByType<StrategyCameraController>();
            Require(cameraController != null, "Strategy camera controller missing");
            cameraController.FocusOn(map.GetCellCenterWorld(campCell.x, campCell.y), 11f);
        }

        private static void RefreshVisualLighting()
        {
            StrategyCinematicVisualController visuals =
                UnityEngine.Object.FindAnyObjectByType<StrategyCinematicVisualController>();
            visuals?.RefreshSceneLightingNow();
        }

        private static void CaptureGameplayRender(string fileName)
        {
            Require(
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null,
                "Gameplay visual capture requires a graphics device");
            Camera camera = Camera.main;
            Require(camera != null, "Gameplay camera missing");
            RenderTexture renderTexture = new(1600, 900, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new(1600, 900, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            screenshot.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
            screenshot.Apply(false, false);
            File.WriteAllBytes(GetResultPath(fileName), screenshot.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }
}
