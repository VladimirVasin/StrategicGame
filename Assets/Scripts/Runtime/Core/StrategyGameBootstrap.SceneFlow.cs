using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static Scene bootstrappedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSceneFlow()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            bootstrappedScene = default;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneLoadHook()
        {
            bootstrappedScene = default;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapInitialScene()
        {
            TryBootstrapScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrapScene(scene);
        }

        private static void HandleSceneUnloaded(Scene scene)
        {
            if (bootstrappedScene == scene)
            {
                bootstrappedScene = default;
            }
        }

        private static void TryBootstrapScene(Scene scene)
        {
            if (!StrategySceneCatalog.IsGameplayScene(scene) || bootstrappedScene == scene)
            {
                return;
            }

            bootstrappedScene = scene;
            StrategyMapPreloadCoordinator.Active?.TryTransferPreparedMap(scene, out _);
            StrategyGameContext context = StrategyGameContext.GetOrCreateForScene(scene);
            if (!context.BeginBootstrap())
            {
                return;
            }

            GameObject runnerObject = new GameObject("Strategy Bootstrap Runner");
            runnerObject.transform.SetParent(context.transform, false);
            StrategyBootstrapRunner runner = runnerObject.AddComponent<StrategyBootstrapRunner>();
            runner.Run(BootstrapScene(context), context);
        }
    }
}
