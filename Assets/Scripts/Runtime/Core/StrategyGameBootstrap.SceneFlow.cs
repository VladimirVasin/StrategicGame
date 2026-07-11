using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static Scene bootstrappedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneLoadHook()
        {
            bootstrappedScene = default;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
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

        private static void TryBootstrapScene(Scene scene)
        {
            if (!StrategySceneCatalog.IsGameplayScene(scene) || bootstrappedScene == scene)
            {
                return;
            }

            bootstrappedScene = scene;
            GameObject runnerObject = new GameObject("Strategy Bootstrap Runner");
            StrategyBootstrapRunner runner = runnerObject.AddComponent<StrategyBootstrapRunner>();
            runner.Run(BootstrapScene());
        }
    }
}
