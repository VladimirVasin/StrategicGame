using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyGameBootstrap
    {
        private static void ConfigureAudio(StrategyGameContext context, CityMapController map, Camera mainCamera)
        {
            StrategyAudioMixController audioMix = StrategyAudioMixController.Active;
            if (audioMix == null)
            {
                GameObject audioMixObject = new GameObject("Strategy Audio Mix");
                audioMix = audioMixObject.AddComponent<StrategyAudioMixController>();
            }

            context.Register(audioMix);
            audioMix.Configure(mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "AudioMixReady");

            StrategyAmbientAudioController ambientAudio = context.GetOrCreate<StrategyAmbientAudioController>("Strategy Ambient Audio");
            ambientAudio.Configure(map, mainCamera);
            StrategyDebugLogger.Info(
                "Bootstrap",
                "AmbientAudioReady",
                StrategyDebugLogger.F("footstepClips", StrategyResidentFootstepAudio.LoadedClipCount));

            StrategyWorldAudioDirector worldAudio = context.GetOrCreate<StrategyWorldAudioDirector>("Strategy World Audio Director");
            worldAudio.Configure(map, mainCamera);
            StrategyDebugLogger.Info("Bootstrap", "WorldAudioReady");

            StrategyMusicController music = context.GetOrCreate<StrategyMusicController>("Strategy Music");
            music.Configure();
            StrategyDebugLogger.Info("Bootstrap", "MusicReady");
        }
    }
}
