using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public enum StrategyLaunchMode
    {
        None,
        NewSettlement,
        Continue
    }

    public enum StrategyPreloadPhase
    {
        PreparingCandidate,
        OpeningFoundingJourney,
        AwaitingFoundingDecision,
        PreparingGameplay,
        OpeningGameplay,
        ReturningToMainMenu,
        Completed
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyMapPreloadCoordinator : MonoBehaviour
    {
        private const float IdleFrameBudgetMs = 4f;
        private const float LaunchFrameBudgetMs = 14f;

        private StrategySaveData savedGame;
        private CityMapController preparedMap;
        private Coroutine mapGenerationRoutine;
        private AsyncOperation sceneLoadOperation;
        private StrategyLaunchMode candidateMode;
        private StrategyLaunchMode requestedMode;
        private StrategyPreloadPhase phase;
        private int newSettlementSeed;
        private int candidateSeed;
        private bool configured;
        private bool launchRequested;
        private bool synchronousFallbackStarted;
        private float contentProgress;
        private string contentStage = "Preparing content";
        private string launchFailureReason = string.Empty;

        public static StrategyMapPreloadCoordinator Active { get; private set; }

        public bool HasValidSave => savedGame != null;
        public bool IsLaunchRequested => launchRequested;
        public bool IsMapReady => preparedMap != null && preparedMap.IsGenerated;
        public StrategyLaunchMode CandidateMode => candidateMode;
        public StrategyLaunchMode RequestedMode => requestedMode;
        public StrategyPreloadPhase Phase => phase;
        public string SaveSummary => BuildSaveSummary();
        public int PreparedSeed => candidateSeed;
        public string LaunchFailureReason => launchFailureReason;

        public float Progress
        {
            get
            {
                if (sceneLoadOperation != null)
                {
                    float sceneProgress = Mathf.Clamp01(sceneLoadOperation.progress / 0.9f);
                    return phase == StrategyPreloadPhase.OpeningGameplay
                        ? Mathf.Lerp(0.96f, 1f, sceneProgress)
                        : sceneProgress;
                }

                float mapProgress = preparedMap != null ? preparedMap.GenerationProgress : 0f;
                return Mathf.Clamp01(mapProgress * 0.9f + contentProgress * 0.1f);
            }
        }

        public string Stage
        {
            get
            {
                if (sceneLoadOperation != null)
                {
                    return phase switch
                    {
                        StrategyPreloadPhase.OpeningFoundingJourney => "Opening founding journey",
                        StrategyPreloadPhase.ReturningToMainMenu => "Returning to main menu",
                        _ => "Opening settlement"
                    };
                }

                if (preparedMap != null && preparedMap.GenerationProgress < 1f)
                {
                    return preparedMap.GenerationStage;
                }

                return contentProgress < 1f ? contentStage : "Ready";
            }
        }

        public void Configure()
        {
            if (configured)
            {
                return;
            }

            if (Active != null && Active != this)
            {
                Destroy(gameObject);
                return;
            }

            Active = this;
            configured = true;
            phase = StrategyPreloadPhase.PreparingCandidate;
            launchFailureReason = string.Empty;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;

            StrategySaveSystem.TryReadSave(out savedGame, out string saveReason);
            newSettlementSeed = StrategyPerformanceBenchmarkOptions.TryGetForcedSeed(out int forcedSeed)
                ? forcedSeed
                : Random.Range(1, int.MaxValue);
            candidateMode = HasValidSave ? StrategyLaunchMode.Continue : StrategyLaunchMode.NewSettlement;
            int seed = HasValidSave ? savedGame.mapSeed : newSettlementSeed;
            StrategyVisualCatalogProvider.Prewarm();
            StartCandidate(candidateMode, seed);
            StartCoroutine(PrewarmContent());

            StrategyDebugLogger.Info(
                "Menu",
                "PreloadConfigured",
                StrategyDebugLogger.F("hasSave", HasValidSave),
                StrategyDebugLogger.F("saveState", HasValidSave ? "valid" : saveReason),
                StrategyDebugLogger.F("candidateMode", candidateMode),
                StrategyDebugLogger.F("seed", seed));
        }

        public bool RequestContinue()
        {
            if (!HasValidSave || launchRequested)
            {
                return false;
            }

            StrategySaveSystem.PreparePendingLoad(savedGame);
            return BeginGameplayLaunch(StrategyLaunchMode.Continue, savedGame.mapSeed);
        }

        public bool RequestNewSettlement()
        {
            if (launchRequested)
            {
                return false;
            }

            StrategySaveSystem.ClearPendingLoad();
            return BeginFoundingJourney(newSettlementSeed);
        }

        public bool TryGetPreparedMap(out CityMapController map)
        {
            map = preparedMap;
            return map != null && map.IsGenerated && !map.GenerationFailed;
        }

        public bool CompleteFoundingJourney()
        {
            if (!configured
                || requestedMode != StrategyLaunchMode.NewSettlement
                || phase != StrategyPreloadPhase.AwaitingFoundingDecision)
            {
                return false;
            }

            launchFailureReason = string.Empty;
            phase = StrategyPreloadPhase.PreparingGameplay;
            preparedMap?.SetGenerationFrameBudget(LaunchFrameBudgetMs);
            StrategyDebugLogger.Info(
                "Menu",
                "FoundingJourneyCompleted",
                StrategyDebugLogger.F("seed", candidateSeed),
                StrategyDebugLogger.F("mapReady", IsMapReady));
            return true;
        }

        public bool CancelFoundingJourney()
        {
            if (!configured || phase != StrategyPreloadPhase.AwaitingFoundingDecision)
            {
                return false;
            }

            launchRequested = false;
            requestedMode = StrategyLaunchMode.None;
            phase = StrategyPreloadPhase.ReturningToMainMenu;
            preparedMap?.SetGenerationFrameBudget(IdleFrameBudgetMs);
            if (HasValidSave && (candidateMode != StrategyLaunchMode.Continue || candidateSeed != savedGame.mapSeed))
            {
                StartCandidate(StrategyLaunchMode.Continue, savedGame.mapSeed);
            }

            sceneLoadOperation = SceneManager.LoadSceneAsync(
                StrategySceneCatalog.MainMenuSceneName,
                LoadSceneMode.Single);
            if (sceneLoadOperation == null)
            {
                launchRequested = true;
                requestedMode = StrategyLaunchMode.NewSettlement;
                phase = StrategyPreloadPhase.AwaitingFoundingDecision;
                launchFailureReason = "The main menu scene could not be opened.";
                StrategyDebugLogger.Error("Menu", "MainMenuSceneOpenRejected");
            }
            StrategyDebugLogger.Info("Menu", "FoundingJourneyCancelled");
            return sceneLoadOperation != null;
        }

        private void Update()
        {
            if (launchRequested
                && preparedMap != null
                && preparedMap.GenerationFailed
                && !synchronousFallbackStarted)
            {
                synchronousFallbackStarted = true;
                preparedMap.GenerateMap(candidateSeed);
            }

            if (phase != StrategyPreloadPhase.PreparingGameplay
                || sceneLoadOperation != null
                || !IsMapReady)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Menu",
                "PreloadReadyForLaunch",
                StrategyDebugLogger.F("mode", requestedMode),
                StrategyDebugLogger.F("seed", preparedMap.ActiveSeed));
            phase = StrategyPreloadPhase.OpeningGameplay;
            sceneLoadOperation = SceneManager.LoadSceneAsync(StrategySceneCatalog.GameplaySceneName, LoadSceneMode.Single);
            if (sceneLoadOperation == null)
            {
                launchFailureReason = "The gameplay scene could not be opened.";
                StrategyDebugLogger.Error(
                    "Menu",
                    "GameplaySceneOpenRejected",
                    StrategyDebugLogger.F("mode", requestedMode));
                if (requestedMode == StrategyLaunchMode.NewSettlement)
                {
                    phase = StrategyPreloadPhase.AwaitingFoundingDecision;
                }
                else
                {
                    launchRequested = false;
                    requestedMode = StrategyLaunchMode.None;
                    phase = StrategyPreloadPhase.PreparingCandidate;
                }
            }
        }

        private bool BeginFoundingJourney(int seed)
        {
            launchFailureReason = string.Empty;
            bool preloadHit = candidateMode == StrategyLaunchMode.NewSettlement
                && candidateSeed == seed
                && preparedMap != null;
            launchRequested = true;
            requestedMode = StrategyLaunchMode.NewSettlement;
            if (!preloadHit)
            {
                StartCandidate(StrategyLaunchMode.NewSettlement, seed);
            }

            preparedMap?.SetGenerationFrameBudget(IdleFrameBudgetMs);
            phase = StrategyPreloadPhase.OpeningFoundingJourney;
            sceneLoadOperation = SceneManager.LoadSceneAsync(
                StrategySceneCatalog.FoundingJourneySceneName,
                LoadSceneMode.Single);
            if (sceneLoadOperation == null)
            {
                launchRequested = false;
                requestedMode = StrategyLaunchMode.None;
                phase = StrategyPreloadPhase.PreparingCandidate;
                launchFailureReason = "The founding journey scene could not be opened.";
                StrategyDebugLogger.Error("Menu", "FoundingJourneySceneOpenRejected");
            }
            StrategyDebugLogger.Info(
                "Menu",
                "FoundingJourneyRequested",
                StrategyDebugLogger.F("seed", seed),
                StrategyDebugLogger.F("preloadHit", preloadHit));
            return sceneLoadOperation != null;
        }

        private bool BeginGameplayLaunch(StrategyLaunchMode mode, int seed)
        {
            launchFailureReason = string.Empty;
            bool preloadHit = candidateMode == mode && candidateSeed == seed && preparedMap != null;
            launchRequested = true;
            requestedMode = mode;
            if (!preloadHit)
            {
                StartCandidate(mode, seed);
            }

            preparedMap?.SetGenerationFrameBudget(LaunchFrameBudgetMs);
            phase = StrategyPreloadPhase.PreparingGameplay;
            StrategyDebugLogger.Info(
                "Menu",
                "LaunchRequested",
                StrategyDebugLogger.F("mode", mode),
                StrategyDebugLogger.F("seed", seed),
                StrategyDebugLogger.F("preloadHit", preloadHit));
            return true;
        }

        private void StartCandidate(StrategyLaunchMode mode, int seed)
        {
            if (mapGenerationRoutine != null)
            {
                StopCoroutine(mapGenerationRoutine);
                mapGenerationRoutine = null;
            }

            if (preparedMap != null)
            {
                preparedMap.CancelIncrementalGeneration();
                Destroy(preparedMap.gameObject);
            }

            candidateMode = mode;
            candidateSeed = Mathf.Max(1, seed);
            synchronousFallbackStarted = false;
            GameObject mapObject = new GameObject("Preloaded City Map");
            DontDestroyOnLoad(mapObject);
            preparedMap = mapObject.AddComponent<CityMapController>();
            preparedMap.SetPresentationVisible(false);
            preparedMap.SetGenerationFrameBudget(launchRequested ? LaunchFrameBudgetMs : IdleFrameBudgetMs);
            mapGenerationRoutine = StartCoroutine(preparedMap.GenerateMapIncremental(candidateSeed));
        }

        private string BuildSaveSummary()
        {
            if (!HasValidSave)
            {
                return "No saved settlement";
            }

            int day = Mathf.Max(1, Mathf.FloorToInt(savedGame.elapsedSeconds / 360f) + 1);
            int residents = savedGame.residents != null ? savedGame.residents.Count : 0;
            return "Day " + day + "  /  " + residents + " residents";
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (StrategySceneCatalog.IsFoundingJourneyScene(scene))
            {
                sceneLoadOperation = null;
                phase = StrategyPreloadPhase.AwaitingFoundingDecision;
                preparedMap?.SetGenerationFrameBudget(IdleFrameBudgetMs);
                StrategyDebugLogger.Info(
                    "Menu",
                    "FoundingJourneyOpened",
                    StrategyDebugLogger.F("seed", candidateSeed));
                return;
            }

            if (StrategySceneCatalog.IsMainMenuScene(scene)
                && phase == StrategyPreloadPhase.ReturningToMainMenu)
            {
                sceneLoadOperation = null;
                phase = StrategyPreloadPhase.PreparingCandidate;
                return;
            }

            if (!StrategySceneCatalog.IsGameplayScene(scene))
            {
                return;
            }

            sceneLoadOperation = null;
            phase = StrategyPreloadPhase.Completed;
            TryTransferPreparedMap(scene, out _);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Destroy(gameObject);
        }

        public bool TryTransferPreparedMap(Scene targetScene, out CityMapController map)
        {
            map = null;
            if (preparedMap == null
                || !targetScene.IsValid()
                || !targetScene.isLoaded
                || !StrategySceneCatalog.IsGameplayScene(targetScene))
            {
                return false;
            }

            if (mapGenerationRoutine != null)
            {
                StopCoroutine(mapGenerationRoutine);
                mapGenerationRoutine = null;
            }

            map = preparedMap;
            preparedMap = null;
            SceneManager.MoveGameObjectToScene(map.gameObject, targetScene);
            map.SetPresentationVisible(true);
            StrategyDebugLogger.Info(
                "Menu",
                "PreloadedMapTransferred",
                StrategyDebugLogger.F("seed", map.ActiveSeed),
                StrategyDebugLogger.F("scene", targetScene.name));
            return true;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (mapGenerationRoutine != null)
            {
                StopCoroutine(mapGenerationRoutine);
                mapGenerationRoutine = null;
            }

            if (preparedMap != null)
            {
                preparedMap.CancelIncrementalGeneration();
                Destroy(preparedMap.gameObject);
                preparedMap = null;
            }

            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
