using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public static class StrategySceneCatalog
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string GameplaySceneName = "SampleScene";

        public static bool IsMainMenuScene(Scene scene)
        {
            return scene.IsValid() && scene.name == MainMenuSceneName;
        }

        public static bool IsGameplayScene(Scene scene)
        {
            return scene.IsValid() && scene.name == GameplaySceneName;
        }
    }
}
