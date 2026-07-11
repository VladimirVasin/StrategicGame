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
        private int newSettlementSeed;
        private int candidateSeed;
        private bool configured;
        private bool launchRequested;
        private bool synchronousFallbackStarted;
        private float contentProgress;
        private string contentStage = "Preparing content";

        public static StrategyMapPreloadCoordinator Active { get; private set; }

        public bool HasValidSave => savedGame != null;
        public bool IsLaunchRequested => launchRequested;
        public bool IsMapReady => preparedMap != null && preparedMap.IsGenerated;
        public StrategyLaunchMode CandidateMode => candidateMode;
        public StrategyLaunchMode RequestedMode => requestedMode;
        public string SaveSummary => BuildSaveSummary();
        public int PreparedSeed => candidateSeed;

        public float Progress
        {
            get
            {
                if (sceneLoadOperation != null)
                {
                    return Mathf.Lerp(0.96f, 1f, Mathf.Clamp01(sceneLoadOperation.progress / 0.9f));
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
                    return "Opening settlement";
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
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;

            StrategySaveSystem.TryReadSave(out savedGame, out string saveReason);
            newSettlementSeed = Random.Range(1, int.MaxValue);
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
            BeginLaunch(StrategyLaunchMode.Continue, savedGame.mapSeed);
            return true;
        }

        public bool RequestNewSettlement()
        {
            if (launchRequested)
            {
                return false;
            }

            StrategySaveSystem.ClearPendingLoad();
            BeginLaunch(StrategyLaunchMode.NewSettlement, newSettlementSeed);
            return true;
        }

        private void Update()
        {
            if (launchRequested
                && preparedMap != null
                && preparedMap.GenerationFailed
                && !synchronousFallbackStarted)
            {
                synchronousFallbackStarted = true;
                preparedMap.GenerateMap();
            }

            if (!launchRequested || sceneLoadOperation != null || !IsMapReady)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Menu",
                "PreloadReadyForLaunch",
                StrategyDebugLogger.F("mode", requestedMode),
                StrategyDebugLogger.F("seed", preparedMap.ActiveSeed));
            sceneLoadOperation = SceneManager.LoadSceneAsync(StrategySceneCatalog.GameplaySceneName, LoadSceneMode.Single);
        }

        private void BeginLaunch(StrategyLaunchMode mode, int seed)
        {
            bool preloadHit = candidateMode == mode && candidateSeed == seed && preparedMap != null;
            launchRequested = true;
            requestedMode = mode;
            if (!preloadHit)
            {
                StartCandidate(mode, seed);
            }

            preparedMap?.SetGenerationFrameBudget(LaunchFrameBudgetMs);
            StrategyDebugLogger.Info(
                "Menu",
                "LaunchRequested",
                StrategyDebugLogger.F("mode", mode),
                StrategyDebugLogger.F("seed", seed),
                StrategyDebugLogger.F("preloadHit", preloadHit));
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
            if (!StrategySceneCatalog.IsGameplayScene(scene))
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
