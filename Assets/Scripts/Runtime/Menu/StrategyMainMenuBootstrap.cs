using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static class StrategyMainMenuBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapMenu()
        {
            BootstrapMenu(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BootstrapMenu(scene);
        }

        private static void BootstrapMenu(Scene scene)
        {
            if (!StrategySceneCatalog.IsMainMenuScene(scene))
            {
                return;
            }

            StrategyGameSettings.ApplyAtStartup();
            StrategyDebugLogger logger = Object.FindAnyObjectByType<StrategyDebugLogger>();
            if (logger == null)
            {
                logger = new GameObject("Strategy Debug Logger").AddComponent<StrategyDebugLogger>();
            }

            logger.Configure();
            Object.DontDestroyOnLoad(logger.gameObject);

            Camera menuCamera = Camera.main;
            if (menuCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                menuCamera = cameraObject.AddComponent<Camera>();
            }

            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 4.5f;
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(0.025f, 0.04f, 0.035f);
            menuCamera.transform.position = new Vector3(0f, 0f, -10f);

            StrategyInputRouter inputRouter = Object.FindAnyObjectByType<StrategyInputRouter>();
            if (inputRouter == null)
            {
                inputRouter = new GameObject("Strategy Input Router").AddComponent<StrategyInputRouter>();
            }

            inputRouter.Configure();
            StrategyUiInputModuleBootstrap.Ensure();

            StrategyMainMenuBackdrop backdrop = Object.FindAnyObjectByType<StrategyMainMenuBackdrop>();
            if (backdrop == null)
            {
                backdrop = new GameObject("Main Menu Settlement Backdrop").AddComponent<StrategyMainMenuBackdrop>();
            }

            backdrop.Configure();

            StrategyMapPreloadCoordinator preloader = Object.FindAnyObjectByType<StrategyMapPreloadCoordinator>();
            if (preloader == null)
            {
                preloader = new GameObject("Strategy Map Preloader").AddComponent<StrategyMapPreloadCoordinator>();
            }

            preloader.Configure();

            StrategyMainMenuController menu = Object.FindAnyObjectByType<StrategyMainMenuController>();
            if (menu == null)
            {
                menu = new GameObject("Strategy Main Menu").AddComponent<StrategyMainMenuController>();
            }

            menu.SetInputRouter(inputRouter);
            menu.Configure(preloader, menuCamera);
            StrategyDebugLogger.Info("Menu", "Interactive");
        }
    }
}
