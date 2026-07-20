using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyGameSettings
    {
        private const string MasterVolumeKey = "settings.masterVolume";
        private const string MusicVolumeKey = "settings.musicVolume";
        private const string SfxVolumeKey = "settings.sfxVolume";
        private const string FullscreenKey = "settings.fullscreen";
        private const string UiScaleKey = "settings.uiScale";
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";

        private static bool loaded;
        private static float masterVolume;
        private static float musicVolume;
        private static float sfxVolume;
        private static bool fullscreen;
        private static float uiScale;
        private static bool reducedMotion;

        public static float MasterVolume
        {
            get
            {
                EnsureLoaded();
                return masterVolume;
            }
        }

        public static float MusicVolume
        {
            get
            {
                EnsureLoaded();
                return musicVolume;
            }
        }

        public static float SfxVolume
        {
            get
            {
                EnsureLoaded();
                return sfxVolume;
            }
        }

        public static bool Fullscreen
        {
            get
            {
                EnsureLoaded();
                return fullscreen;
            }
        }

        public static float UiScale
        {
            get
            {
                EnsureLoaded();
                return uiScale;
            }
        }

        public static bool ReducedMotion
        {
            get
            {
                EnsureLoaded();
                return reducedMotion;
            }
        }

        public static StrategyGameLanguage Language
        {
            get
            {
                EnsureLoaded();
                return StrategyLocalization.CurrentLanguage;
            }
        }

        public static void ApplyAtStartup()
        {
            EnsureLoaded();
            if (Screen.fullScreen != fullscreen)
            {
                Screen.fullScreen = fullscreen;
            }
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            masterVolume = SaveVolume(MasterVolumeKey, value);
        }

        public static void SetMusicVolume(float value)
        {
            EnsureLoaded();
            musicVolume = SaveVolume(MusicVolumeKey, value);
        }

        public static void SetSfxVolume(float value)
        {
            EnsureLoaded();
            sfxVolume = SaveVolume(SfxVolumeKey, value);
        }

        public static void SetFullscreen(bool value)
        {
            EnsureLoaded();
            fullscreen = value;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = fullscreen;
        }

        public static void SetUiScale(float value)
        {
            EnsureLoaded();
            uiScale = Mathf.Clamp(value, 0.85f, 1.25f);
            PlayerPrefs.SetFloat(UiScaleKey, uiScale);
            PlayerPrefs.Save();
            StrategyHudStyle.RefreshCanvasScalers();
        }

        public static void SetLanguage(StrategyGameLanguage value)
        {
            EnsureLoaded();
            StrategyGameLanguage supported = value == StrategyGameLanguage.English
                ? StrategyGameLanguage.English
                : StrategyGameLanguage.Russian;
            StrategyLocalization.SetLanguage(supported);
        }

        public static void SetReducedMotion(bool value)
        {
            EnsureLoaded();
            reducedMotion = value;
            PlayerPrefs.SetInt(ReducedMotionKey, reducedMotion ? 1 : 0);
            PlayerPrefs.Save();

            StrategyUiButtonFeedback[] feedback = Object.FindObjectsByType<StrategyUiButtonFeedback>(
                FindObjectsInactive.Include);
            for (int i = 0; i < feedback.Length; i++)
            {
                feedback[i].SetReducedMotion(reducedMotion);
            }
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
            uiScale = Mathf.Clamp(PlayerPrefs.GetFloat(UiScaleKey, 1f), 0.85f, 1.25f);
            reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            StrategyLocalization.Initialize();
        }

        private static float SaveVolume(string key, float value)
        {
            float normalized = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(key, normalized);
            PlayerPrefs.Save();
            return normalized;
        }
    }
}
