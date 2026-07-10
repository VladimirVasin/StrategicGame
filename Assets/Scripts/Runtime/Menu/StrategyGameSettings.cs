using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyGameSettings
    {
        private const string MasterVolumeKey = "settings.masterVolume";
        private const string MusicVolumeKey = "settings.musicVolume";
        private const string SfxVolumeKey = "settings.sfxVolume";
        private const string FullscreenKey = "settings.fullscreen";

        private static bool loaded;
        private static float masterVolume;
        private static float musicVolume;
        private static float sfxVolume;
        private static bool fullscreen;

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
