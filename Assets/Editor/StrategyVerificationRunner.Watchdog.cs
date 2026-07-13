using System;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const double MainMenuLaunchTimeoutSeconds = 180d;
        private const double MainMenuLaunchStallSeconds = 60d;
        private const double GameplayBootstrapTimeoutSeconds = 180d;
        private const double GameplayBootstrapStallSeconds = 60d;
        private const float PreloadProgressEpsilon = 0.0001f;
        private const float PreloadLogInterval = 0.10f;

        private static double smokeStartedAt;
        private static double preloadLastProgressAt;
        private static float preloadLastProgress;
        private static float preloadLastLoggedProgress;
        private static string preloadLastStage = string.Empty;

        private static void ResetSmokeWatchdog()
        {
            double now = EditorApplication.timeSinceStartup;
            smokeStartedAt = now;
            preloadLastProgressAt = now;
            preloadLastProgress = -1f;
            preloadLastLoggedProgress = -1f;
            preloadLastStage = string.Empty;
        }

        private static void VerifyMainMenuLaunchWatchdog(StrategyMapPreloadCoordinator preloader)
        {
            double now = EditorApplication.timeSinceStartup;
            float progress = preloader.Progress;
            string stage = preloader.Stage ?? string.Empty;
            bool stageChanged = !string.Equals(stage, preloadLastStage, StringComparison.Ordinal);
            if (stageChanged || progress > preloadLastProgress + PreloadProgressEpsilon)
            {
                preloadLastProgress = progress;
                preloadLastProgressAt = now;
                preloadLastStage = stage;
            }

            if (stageChanged || progress >= preloadLastLoggedProgress + PreloadLogInterval)
            {
                preloadLastLoggedProgress = progress;
                Debug.Log($"MainMenuLaunch preload stage='{stage}' progress={progress:P0}");
            }

            Require(
                now - smokeStartedAt <= MainMenuLaunchTimeoutSeconds,
                $"Menu preload exceeded {MainMenuLaunchTimeoutSeconds:0}s at stage '{stage}' ({progress:P0})");
            Require(
                now - preloadLastProgressAt <= MainMenuLaunchStallSeconds,
                $"Menu preload made no progress for {MainMenuLaunchStallSeconds:0}s at stage '{stage}' ({progress:P0})");
        }

        private static void VerifyGameplayBootstrapWatchdog(StrategyGameContext context)
        {
            double now = EditorApplication.timeSinceStartup;
            Require(
                now - smokeStartedAt <= GameplayBootstrapTimeoutSeconds,
                $"Gameplay bootstrap exceeded {GameplayBootstrapTimeoutSeconds:0}s");

            CityMapController map = UnityEngine.Object.FindAnyObjectByType<CityMapController>();
            if (map == null || !map.IsGenerating)
            {
                return;
            }

            float progress = map.GenerationProgress;
            string stage = map.GenerationStage ?? string.Empty;
            bool stageChanged = !string.Equals(stage, preloadLastStage, StringComparison.Ordinal);
            if (stageChanged || progress > preloadLastProgress + PreloadProgressEpsilon)
            {
                preloadLastProgress = progress;
                preloadLastProgressAt = now;
                preloadLastStage = stage;
            }

            if (stageChanged || progress >= preloadLastLoggedProgress + PreloadLogInterval)
            {
                preloadLastLoggedProgress = progress;
                Debug.Log($"Gameplay bootstrap map stage='{stage}' progress={progress:P0}");
            }

            Require(
                now - preloadLastProgressAt <= GameplayBootstrapStallSeconds,
                $"Gameplay map made no progress for {GameplayBootstrapStallSeconds:0}s at stage '{stage}' ({progress:P0})");
        }
    }
}
