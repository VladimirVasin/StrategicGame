using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static class StrategyFoundingJourneyBootstrap
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
        private static void BootstrapInitialScene()
        {
            BootstrapScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BootstrapScene(scene);
        }

        private static void BootstrapScene(Scene scene)
        {
            if (!StrategySceneCatalog.IsFoundingJourneyScene(scene))
            {
                return;
            }

            StrategyGameSettings.ApplyAtStartup();
            StrategyDebugLogger logger = Object.FindAnyObjectByType<StrategyDebugLogger>();
            if (logger == null)
            {
                logger = new GameObject("Strategy Debug Logger").AddComponent<StrategyDebugLogger>();
                Object.DontDestroyOnLoad(logger.gameObject);
            }

            logger.Configure();
            Camera journeyCamera = EnsureCamera();
            StrategyInputRouter inputRouter = Object.FindAnyObjectByType<StrategyInputRouter>();
            if (inputRouter == null)
            {
                inputRouter = new GameObject("Strategy Input Router").AddComponent<StrategyInputRouter>();
            }

            inputRouter.Configure();
            StrategyUiInputModuleBootstrap.Ensure();

            StrategyMapPreloadCoordinator preloader = StrategyMapPreloadCoordinator.Active;
            if (preloader == null)
            {
                StrategyDebugLogger.Error("FoundingJourney", "MissingPreloader");
                SceneManager.LoadSceneAsync(
                    StrategySceneCatalog.MainMenuSceneName,
                    LoadSceneMode.Single);
                return;
            }

            StrategyFoundingJourneyController controller =
                Object.FindAnyObjectByType<StrategyFoundingJourneyController>();
            if (controller == null)
            {
                controller = new GameObject("Strategy Founding Journey").AddComponent<StrategyFoundingJourneyController>();
            }

            controller.Configure(preloader, journeyCamera, inputRouter);
            StrategyDebugLogger.Info("FoundingJourney", "SceneReady");
        }

        private static Camera EnsureCamera()
        {
            Camera journeyCamera = Camera.main;
            if (journeyCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                journeyCamera = cameraObject.AddComponent<Camera>();
            }

            journeyCamera.orthographic = true;
            journeyCamera.orthographicSize = 4.5f;
            journeyCamera.clearFlags = CameraClearFlags.SolidColor;
            journeyCamera.backgroundColor = new Color(0.015f, 0.02f, 0.025f);
            journeyCamera.transform.position = new Vector3(0f, 0f, -10f);
            return journeyCamera;
        }
    }
}
