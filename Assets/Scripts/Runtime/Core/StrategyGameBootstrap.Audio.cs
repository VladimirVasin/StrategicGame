using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureAudio(CityMapController map, Camera mainCamera)
        {
            StrategyAudioMixController audioMix = Object.FindAnyObjectByType<StrategyAudioMixController>();
            if (audioMix == null)
            {
                GameObject audioMixObject = new GameObject("Strategy Audio Mix");
                audioMix = audioMixObject.AddComponent<StrategyAudioMixController>();
            }

            audioMix.Configure(mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "AudioMixReady");

            StrategyAmbientAudioController ambientAudio = Object.FindAnyObjectByType<StrategyAmbientAudioController>();
            if (ambientAudio == null)
            {
                GameObject ambientAudioObject = new GameObject("Strategy Ambient Audio");
                ambientAudio = ambientAudioObject.AddComponent<StrategyAmbientAudioController>();
            }

            ambientAudio.Configure(map, mainCamera);
            StrategyDebugLogger.Info(
                "Bootstrap",
                "AmbientAudioReady",
                StrategyDebugLogger.F("footstepClips", StrategyResidentFootstepAudio.LoadedClipCount));

            StrategyWorldAudioDirector worldAudio = Object.FindAnyObjectByType<StrategyWorldAudioDirector>();
            if (worldAudio == null)
            {
                GameObject worldAudioObject = new GameObject("Strategy World Audio Director");
                worldAudio = worldAudioObject.AddComponent<StrategyWorldAudioDirector>();
            }

            worldAudio.Configure(map, mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "WorldAudioReady");

            StrategyMusicController music = Object.FindAnyObjectByType<StrategyMusicController>();
            if (music == null)
            {
                GameObject musicObject = new GameObject("Strategy Music");
                music = musicObject.AddComponent<StrategyMusicController>();
            }

            music.Configure();
            StrategyDebugLogger.Info("Bootstrap", "MusicReady");
        }
    }
}
