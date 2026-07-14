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

        public static void RunGameplayVisualCapture()
        {
            File.WriteAllText(GetResultPath("GameplayVisualCapture.txt"), "RUNNING");
            visualCaptureStage = 0;
            visualCaptureWaitFrames = 0;
            StartPlayModeSmoke(SmokeKind.GameplayVisualCapture, GameplayScenePath);
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

        private static void VerifyAuthoredHouseFamily()
        {
            for (int variant = 0; variant < StrategyBuildingSpriteFactory.HouseVariantCount; variant++)
            {
                string resourcePath = $"Visual/Authored/Buildings/House/V{variant + 1:00}";
                Sprite house = Resources.Load<Sprite>(resourcePath);
                Require(house != null, "Authored House sprite is missing: " + resourcePath);
                Require(
                    Mathf.RoundToInt(house.rect.width) == 80
                        && Mathf.RoundToInt(house.rect.height) == 80,
                    "Authored House sprite dimensions changed: " + resourcePath);
                Require(
                    Mathf.Approximately(house.pixelsPerUnit, 24f)
                        && Vector2.Distance(house.pivot, new Vector2(40f, 8f)) < 0.01f,
                    "Authored House sprite scale or pivot changed: " + resourcePath);
                Require(
                    house.texture != null && house.texture.filterMode == FilterMode.Point,
                    "Authored House sprite must use Point filtering: " + resourcePath);

                string assetPath = AssetDatabase.GetAssetPath(house.texture);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Require(importer != null, "Authored House texture importer is missing: " + assetPath);
                Require(!importer.mipmapEnabled, "Authored House mipmaps must stay disabled: " + assetPath);
                Require(!importer.isReadable, "Authored House texture must stay read-disabled: " + assetPath);
                Require(
                    importer.textureCompression == TextureImporterCompression.Uncompressed,
                    "Authored House texture must stay uncompressed: " + assetPath);
            }
        }

        private static void VerifyAuthoredHouseConstructionFamily(StrategyVisualCatalog catalog)
        {
            const int frameWidth = 92;
            const int frameHeight = 82;
            const int frameCount = 7;
            Vector2 expectedPivot = new(46f, 8.2f);
            for (int variant = 0; variant < StrategyBuildingSpriteFactory.HouseVariantCount; variant++)
            {
                string resourcePath = $"Visual/Authored/Construction/House/V{variant + 1:00}";
                Texture2D atlas = Resources.Load<Texture2D>(resourcePath);
                Require(atlas != null, "Authored House construction atlas is missing: " + resourcePath);
                Require(
                    atlas.width == frameWidth * frameCount && atlas.height == frameHeight,
                    "Authored House construction atlas dimensions changed: " + resourcePath);
                Require(
                    atlas.filterMode == FilterMode.Point,
                    "Authored House construction atlas must use Point filtering: " + resourcePath);

                string assetPath = AssetDatabase.GetAssetPath(atlas);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Require(importer != null, "Authored House construction importer is missing: " + assetPath);
                Require(
                    importer.textureType == TextureImporterType.Sprite
                        && importer.spriteImportMode == SpriteImportMode.Single,
                    "Authored House construction atlas must stay Sprite/Single: " + assetPath);
                Require(!importer.mipmapEnabled, "Authored House construction mipmaps must stay disabled: " + assetPath);
                Require(!importer.isReadable, "Authored House construction atlas must stay read-disabled: " + assetPath);
                Require(
                    importer.textureCompression == TextureImporterCompression.Uncompressed,
                    "Authored House construction atlas must stay uncompressed: " + assetPath);
                Require(importer.npotScale == TextureImporterNPOTScale.None, "Authored House construction atlas must preserve NPOT size: " + assetPath);

                string sequenceId = $"Construction/{StrategyBuildTool.House}/V{variant}";
                for (int frame = 0; frame < frameCount; frame++)
                {
                    Require(
                        catalog.TryGetSequenceSprite(sequenceId, frame, out Sprite sprite) && sprite != null,
                        $"Authored House construction frame is missing: {sequenceId}/{frame}");
                    Require(
                        sprite.texture == atlas,
                        $"House construction frame does not reference authored texture: {sequenceId}/{frame}");
                    Require(
                        Mathf.RoundToInt(sprite.rect.x) == frame * frameWidth
                            && Mathf.RoundToInt(sprite.rect.y) == 0
                            && Mathf.RoundToInt(sprite.rect.width) == frameWidth
                            && Mathf.RoundToInt(sprite.rect.height) == frameHeight,
                        $"Authored House construction frame rect changed: {sequenceId}/{frame}");
                    Require(
                        Mathf.Approximately(sprite.pixelsPerUnit, 24f)
                            && Vector2.Distance(sprite.pivot, expectedPivot) < 0.01f,
                        $"Authored House construction frame scale or pivot changed: {sequenceId}/{frame}");
                }

                VerifyEmbeddedFinalHousePixels(atlas, variant, frameWidth);
            }
        }

        private static void VerifyEmbeddedFinalHousePixels(
            Texture2D atlas,
            int variant,
            int frameWidth)
        {
            string housePath = $"Assets/Resources/Visual/Authored/Buildings/House/V{variant + 1:00}.png";
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false);
            Texture2D atlasCopy = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(source, File.ReadAllBytes(housePath), false), "Could not read " + housePath);
                string atlasPath = AssetDatabase.GetAssetPath(atlas);
                Require(ImageConversion.LoadImage(atlasCopy, File.ReadAllBytes(atlasPath), false), "Could not read " + atlasPath);
                Require(source.width == 80 && source.height == 80, "Completed House dimensions changed: " + housePath);

                Color32[] sourcePixels = source.GetPixels32();
                Color32[] atlasPixels = atlasCopy.GetPixels32();
                int atlasX = 6 * frameWidth + 6;
                for (int y = 0; y < source.height; y++)
                {
                    for (int x = 0; x < source.width; x++)
                    {
                        Color32 expected = sourcePixels[y * source.width + x];
                        if (expected.a == 0)
                        {
                            continue;
                        }

                        Color32 actual = atlasPixels[y * atlasCopy.width + atlasX + x];
                        Require(actual.Equals(expected), $"Construction stage 6 shifted or changed: House V{variant + 1:00} at {x},{y}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(atlasCopy);
            }
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
