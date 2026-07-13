using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    [InitializeOnLoad]
    public static partial class StrategyVerificationRunner
    {
        private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string FoundingJourneyScenePath = "Assets/Scenes/FoundingJourney.unity";
        private const int RequiredPlayFrames = 4;
        private const double RefugeeRouteTimeoutSeconds = 30d;
        private const string PlayModeSessionKey = "ProjectUnknown.PlayModeSmokeActive";
        private const string PlayModeKindSessionKey = "ProjectUnknown.PlayModeSmokeKind";

        private static int playFrames;
        private static SmokeKind smokeKind;
        private static bool launchRequestedBySmoke;
        private static int gameplayFramesAfterLaunch;
        private static double gameplayActionStartedAt;

        static StrategyVerificationRunner()
        {
            if (SessionState.GetBool(PlayModeSessionKey, false))
            {
                smokeKind = (SmokeKind)SessionState.GetInt(PlayModeKindSessionKey, (int)SmokeKind.Gameplay);
                ResetSmokeWatchdog();
                RestoreSmokeErrorCapture();
                if (IsSoakKind(smokeKind))
                {
                    RestoreSoakAfterDomainReload();
                }

                EditorApplication.update -= UpdatePlayMode;
                EditorApplication.update += UpdatePlayMode;
            }
        }

        public static void RunEditMode()
        {
            string resultPath = GetResultPath("EditModeVerification.txt");
            try
            {
                VerifyCalendar();
                VerifyResourceStore();
                VerifyColdState();
                VerifySaveRoundTrip();
                VerifyRefugeeBalance();
                VerifyBuildScenes();
                VerifyNavigationPriorities();
                VerifyVisualCatalog();
                VerifyExplicitMapSeed();
                VerifyAudioImportProfiles();
                VerifyAudioArchitecture();
                VerifySourceQuality();
                File.WriteAllText(resultPath, "PASS: technical verification checks");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(resultPath, "FAIL: " + exception);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void RunPlayMode()
        {
            File.WriteAllText(GetResultPath("PlayModeSmoke.txt"), "RUNNING");
            StartPlayModeSmoke(SmokeKind.Gameplay, GameplayScenePath);
        }

        public static void RunMainMenuSmoke()
        {
            File.WriteAllText(GetResultPath("MainMenuSmoke.txt"), "RUNNING");
            StartPlayModeSmoke(SmokeKind.MainMenu, MainMenuScenePath);
        }

        public static void RunMainMenuLaunchSmoke()
        {
            File.WriteAllText(GetResultPath("MainMenuLaunchSmoke.txt"), "RUNNING");
            StartPlayModeSmoke(SmokeKind.MainMenuLaunch, MainMenuScenePath);
        }

        public static void RunMainMenuRenderCapture()
        {
            File.WriteAllText(GetResultPath("MainMenuRenderCapture.txt"), "RUNNING");
            StartPlayModeSmoke(SmokeKind.MainMenuRenderCapture, MainMenuScenePath);
        }

        private static void UpdatePlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.isPaused)
            {
                Debug.LogWarning("Verification resumed play mode after an unexpected Editor pause.");
                EditorApplication.isPaused = false;
            }

            if (++playFrames < RequiredPlayFrames)
            {
                return;
            }

            try
            {
                if (smokeKind == SmokeKind.MainMenuLaunch)
                {
                    UpdateMainMenuLaunchSmoke();
                    return;
                }

                if (smokeKind == SmokeKind.MainMenuRenderCapture)
                {
                    if (Time.frameCount < 30)
                    {
                        return;
                    }

                    CaptureMainMenuRender();
                    CompletePlayMode(true, "PASS: main menu render captured");
                    return;
                }

                if (smokeKind == SmokeKind.MainMenu)
                {
                    StrategyMainMenuController menu = UnityEngine.Object.FindAnyObjectByType<StrategyMainMenuController>();
                    Require(menu != null && menu.IsConfigured, "Main menu bootstrap failed");
                    Require(UnityEngine.Object.FindAnyObjectByType<StrategyMapPreloadCoordinator>() != null, "Menu preloader failed");
                    Require(UnityEngine.Object.FindAnyObjectByType<StrategyPopulationController>() == null, "Gameplay population bootstrapped in menu");
                    Require(UnityEngine.Object.FindAnyObjectByType<StrategyBuildMenuController>() == null, "Gameplay HUD bootstrapped in menu");
                    Require(UnityEngine.Object.FindAnyObjectByType<StrategyMusicController>() == null, "Music must stay disabled in the main menu");
                    Require(
                        UnityEngine.Object.FindObjectsByType<StrategyMainMenuButtonHover>(
                            FindObjectsInactive.Include).Length >= 5,
                        "Main-menu button hover controllers are missing");
                    VerifyRuntimeInput(null);
                    CompletePlayMode(true, "PASS: menu systems ready");
                    return;
                }

                StrategyGameContext context = StrategyGameContext.Current;
                Require(context == null || context.State != StrategyGameContextState.Failed, "Gameplay bootstrap failed: " + context?.FailureReason);
                if (context == null || !context.IsReady)
                {
                    VerifyGameplayBootstrapWatchdog(context);
                    return;
                }

                CityMapController map = UnityEngine.Object.FindAnyObjectByType<CityMapController>();
                StrategyPopulationController population = UnityEngine.Object.FindAnyObjectByType<StrategyPopulationController>();
                Require(map != null && map.ActiveSeed > 0, "Map bootstrap failed");
                Require(population != null && population.TotalResidentCount > 0, "Population bootstrap failed");
                Require(UnityEngine.Object.FindAnyObjectByType<StrategyBuildPlacementController>() != null, "Placement bootstrap failed");
                Require(UnityEngine.Object.FindAnyObjectByType<StrategySaveSystem>() != null, "Persistence bootstrap failed");
                Require(UnityEngine.Object.FindAnyObjectByType<StrategyWorldAudioDirector>() != null, "World audio director bootstrap failed");
                Require(StrategyAudioVoicePool.ActiveVoiceCount <= StrategyAudioVoicePool.Capacity, "World audio voice budget exceeded");
                Require(StrategyTrailController.Active != null, "Trail bootstrap failed");
                VerifyRuntimeInput(context, !IsSoakKind(smokeKind));
                VerifyBuildingGroundDetails();
                if (smokeKind == SmokeKind.GameplayVisualCapture)
                {
                    UpdateGameplayVisualCapture(map, population);
                    return;
                }

                if (IsSoakKind(smokeKind))
                {
                    UpdateSoakSmoke(map, population);
                    return;
                }

                VerifyGeneratedResourceMinimums();
                StrategyRefugeeArrivalController refugees = UnityEngine.Object.FindAnyObjectByType<StrategyRefugeeArrivalController>();
                Require(refugees != null, "Refugee controller bootstrap failed");
                if (!launchRequestedBySmoke)
                {
                    Require(refugees.DebugStartArrival(), "Refugee route preparation did not start");
                    launchRequestedBySmoke = true;
                    gameplayFramesAfterLaunch = 0;
                    gameplayActionStartedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (refugees.HasActiveArrivalFamily)
                {
                    CompletePlayMode(true, "PASS: bootstrap and refugee routing ready");
                    return;
                }

                Require(
                    EditorApplication.timeSinceStartup - gameplayActionStartedAt <= RefugeeRouteTimeoutSeconds,
                    $"Refugee route preparation exceeded {RefugeeRouteTimeoutSeconds:0}s");
            }
            catch (Exception exception)
            {
                CompletePlayMode(false, "FAIL: " + exception);
            }
        }

        internal static void VerifyCalendar()
        {
            Require(StrategySeasonCalendar.GetSeason(0) == StrategySeason.Summer, "Day 1 must be Summer");
            Require(StrategySeasonCalendar.GetSeason(7) == StrategySeason.Spring, "Day 8 must be Spring");
            Require(StrategySeasonCalendar.GetSeason(14) == StrategySeason.Autumn, "Day 15 must be Autumn");
            Require(StrategySeasonCalendar.GetSeason(21) == StrategySeason.Winter, "Day 22 must be Winter");
            Require(StrategySeasonCalendar.GetYear(28) == 2, "Day 29 must begin year 2");
        }

        internal static void VerifyResourceStore()
        {
            StrategyResourceStore store = new();
            object owner = new();
            object reservationOwner = new();
            store.Bind(owner, StrategyResourceStoreScope.Settlement);
            Require(store.Add(StrategyResourceType.Logs, 12) == 12, "Resource add failed");
            Require(store.TryReserve(
                reservationOwner,
                StrategyResourceType.Logs,
                5,
                StrategyResourceReservationChannel.Construction,
                out int reserved) && reserved == 5,
                "Resource reservation failed");
            Require(store.GetAvailable(StrategyResourceType.Logs) == 7, "Reserved stock remained available");
            int[] snapshot = store.CaptureAmounts();
            store.RestoreAmounts(snapshot);
            Require(store.GetStored(StrategyResourceType.Logs) == 12, "Resource restore failed");
            Require(store.GetReserved(StrategyResourceType.Logs) == 0, "Reservations survived restore");
        }

        internal static void VerifyColdState()
        {
            StrategyResidentColdState state = new();
            state.ApplyNight(-8f, 1, 1f, out _);
            state.ApplyNight(-8f, 2, 1f, out _);
            Require(state.Condition == StrategyResidentColdCondition.Sick, "Freezing exposure did not cause sickness");
            state.ApplyNight(15f, 3, 1f, out _);
            state.ApplyNight(15f, 4, 1f, out _);
            state.ApplyNight(15f, 5, 1f, out _);
            Require(state.Condition == StrategyResidentColdCondition.Healthy, "Warm recovery failed");
        }

        internal static void VerifySaveRoundTrip()
        {
            StrategySaveData source = new()
            {
                mapSeed = 12345,
                mapWidth = 120,
                mapHeight = 80,
                elapsedSeconds = 456.5f
            };
            source.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = "building-a",
                tool = (int)StrategyBuildTool.House,
                originX = 2,
                originY = 3,
                footprintX = 2,
                footprintY = 2,
                resourceAmounts = new[] { 0, 0, 2, 3 }
            });
            source.residents.Add(new StrategyResidentSaveData
            {
                residentId = 17,
                homeStableId = "building-a"
            });
            StrategySaveData restored = JsonUtility.FromJson<StrategySaveData>(JsonUtility.ToJson(source));
            Require(restored.version == StrategySaveData.CurrentVersion, "Save version changed in JSON");
            Require(restored.buildings[0].stableId == "building-a", "Building stable ID was lost");
            Require(restored.buildings[0].resourceAmounts[2] == 2, "Stored resources were lost");
            Require(restored.residents[0].homeStableId == "building-a", "Resident home link was lost");
            Require(
                StrategySaveSystem.ValidateSaveData(restored, out string reason),
                "Round-tripped save failed validation: " + reason);
        }

        internal static void VerifyRefugeeBalance()
        {
            Require(
                StrategyFirstYearBalance.GetRefugeeHousingMultiplier(0)
                    < StrategyFirstYearBalance.GetRefugeeHousingMultiplier(3),
                "Housing shortage must reduce arrivals");
            Require(
                StrategyFirstYearBalance.GetRefugeeSeasonMultiplier(StrategySeason.Winter)
                    < StrategyFirstYearBalance.GetRefugeeSeasonMultiplier(StrategySeason.Summer),
                "Winter must reduce arrivals");
        }

        internal static void VerifyBuildScenes()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Require(scenes.Length >= 3, "Menu, founding journey, and gameplay scenes must be in Build Settings");
            Require(scenes[0].enabled && scenes[0].path == MainMenuScenePath, "Main menu must be the first build scene");
            Require(scenes[1].enabled && scenes[1].path == FoundingJourneyScenePath, "Founding journey must follow the main menu");
            Require(scenes[2].enabled && scenes[2].path == GameplayScenePath, "Gameplay scene must follow the founding journey");
        }

        internal static void VerifyNavigationPriorities()
        {
            StrategyNavigationQuery wildlife = new(
                Vector2Int.zero,
                Vector2Int.one,
                StrategyNavigationMode.WildlifeLand);
            StrategyNavigationQuery resident = new(
                Vector2Int.zero,
                Vector2Int.one,
                StrategyNavigationMode.ResidentTrail);
            StrategyNavigationQuery critical = new(
                Vector2Int.zero,
                Vector2Int.one,
                StrategyNavigationMode.ResidentTrail,
                priority: StrategyNavigationPriority.Critical);
            Require(wildlife.Priority == StrategyNavigationPriority.Background, "Wildlife navigation must be background priority");
            Require(resident.Priority == StrategyNavigationPriority.Normal, "Resident navigation must be normal priority");
            Require(critical.Priority == StrategyNavigationPriority.Critical, "Critical navigation priority was not preserved");
        }

        private static void VerifyGeneratedResourceMinimums()
        {
            Require(StrategyStoneResourceController.Active != null
                && StrategyStoneResourceController.Active.Deposits.Count >= 112,
                "Generated Stone minimum was not met");
            Require(StrategyIronResourceController.Active != null
                && StrategyIronResourceController.Active.Deposits.Count >= 48,
                "Generated Iron minimum was not met");
            Require(StrategyCoalResourceController.Active != null
                && StrategyCoalResourceController.Active.Deposits.Count >= 42,
                "Generated Coal minimum was not met");
            Require(StrategyClayResourceController.Active != null
                && StrategyClayResourceController.Active.Deposits.Count >= 28,
                "Generated Clay minimum was not met");
            GameObject natureRoot = GameObject.Find("Nature Props");
            Require(natureRoot != null && natureRoot.transform.childCount <= 3600,
                "Nature prop limit was exceeded");
        }

        private static void StartPlayModeSmoke(SmokeKind kind, string scenePath)
        {
            smokeKind = kind;
            playFrames = 0;
            launchRequestedBySmoke = false;
            gameplayFramesAfterLaunch = 0;
            gameplayActionStartedAt = 0d;
            ResetSmokeWatchdog();
            ResetSmokeErrorCapture();
            SessionState.SetInt(PlayModeKindSessionKey, (int)kind);
            SessionState.SetBool(PlayModeSessionKey, true);
            EditorApplication.update -= UpdatePlayMode;
            EditorApplication.update += UpdatePlayMode;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void CompletePlayMode(bool passed, string result)
        {
            ApplySmokeErrors(ref passed, ref result);
            string resultFile = smokeKind switch
            {
                SmokeKind.MainMenu => "MainMenuSmoke.txt",
                SmokeKind.MainMenuLaunch => "MainMenuLaunchSmoke.txt",
                SmokeKind.MainMenuRenderCapture => "MainMenuRenderCapture.txt",
                SmokeKind.GameplayVisualCapture => "GameplayVisualCapture.txt",
                SmokeKind.Soak => "SoakSmoke.txt",
                SmokeKind.QuickSoak => "QuickSoakSmoke.txt",
                _ => "PlayModeSmoke.txt"
            };
            File.WriteAllText(GetResultPath(resultFile), result);
            if (IsSoakKind(smokeKind))
            {
                CleanupSoak();
            }

            SessionState.SetBool(PlayModeSessionKey, false);
            SessionState.EraseInt(PlayModeKindSessionKey);
            EditorApplication.update -= UpdatePlayMode;
            CleanupSmokeErrorCapture();
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static string GetResultPath(string fileName)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void CaptureMainMenuRender()
        {
            Camera camera = Camera.main;
            Require(camera != null, "Main menu camera missing");
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = 1f;
                canvases[i].overrideSorting = true;
                canvases[i].sortingOrder = 100;
            }

            SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder -= 30000;
            }

            Canvas.ForceUpdateCanvases();
            RenderTexture renderTexture = new(1600, 900, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new(1600, 900, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            screenshot.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
            screenshot.Apply(false, false);
            File.WriteAllBytes(GetResultPath("MainMenuRender.png"), screenshot.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }

        private enum SmokeKind
        {
            Gameplay,
            MainMenu,
            MainMenuLaunch,
            MainMenuRenderCapture,
            GameplayVisualCapture,
            Soak,
            QuickSoak
        }
    }
}
