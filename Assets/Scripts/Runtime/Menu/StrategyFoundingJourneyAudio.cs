using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFoundingJourneyAudio : MonoBehaviour
    {
        private const string CalmWindPath = "Audio/Nature/Ambiance_Wind_Calm_Loop_Stereo";
        private const string ForestWindPath = "Audio/Nature/Ambiance_Wind_Forest_Loop_Stereo";
        private const string CalmRainPath = "Audio/Nature/Ambiance_Rain_Calm_Loop_Stereo";
        private const float FadeSpeed = 0.16f;

        private readonly struct AudioProfile
        {
            public AudioProfile(float calmWind, float forestWind, float rain, float fire)
            {
                CalmWind = calmWind;
                ForestWind = forestWind;
                Rain = rain;
                Fire = fire;
            }

            public float CalmWind { get; }
            public float ForestWind { get; }
            public float Rain { get; }
            public float Fire { get; }
        }

        private AudioSource calmWindSource;
        private AudioSource forestWindSource;
        private AudioSource rainSource;
        private AudioSource fireSource;
        private AudioProfile target;
        private bool configured;

        public void Configure(Camera journeyCamera)
        {
            if (configured)
            {
                return;
            }

            configured = true;
            EnsureListener(journeyCamera);
            StrategyAudioMixController.GetVolume(StrategyAudioBus.Weather);
            StartCoroutine(LoadLoops());
        }

        internal void SetPanel(StrategyFoundingStoryPanel panel)
        {
            target = panel.Atmosphere switch
            {
                StrategyFoundingAtmosphere.Embers => new AudioProfile(0.070f, 0.018f, 0f, 0.115f),
                StrategyFoundingAtmosphere.Rain => new AudioProfile(0.025f, 0.025f, 0.125f, 0f),
                StrategyFoundingAtmosphere.Mist => new AudioProfile(0.025f, 0.080f, 0f, 0f),
                _ => new AudioProfile(0.020f, 0.055f, 0f, 0.038f)
            };
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            float deltaTime = Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float weatherVolume = StrategyAudioMixController.GetVolume(StrategyAudioBus.Weather);
            float fireVolume = StrategyAudioMixController.GetVolume(StrategyAudioBus.Fire);
            Fade(calmWindSource, target.CalmWind * weatherVolume, deltaTime);
            Fade(forestWindSource, target.ForestWind * weatherVolume, deltaTime);
            Fade(rainSource, target.Rain * weatherVolume, deltaTime);
            Fade(fireSource, target.Fire * fireVolume, deltaTime);
        }

        private void OnDestroy()
        {
            Stop(calmWindSource);
            Stop(forestWindSource);
            Stop(rainSource);
            Stop(fireSource);
        }

        private AudioSource CreateLoop(string sourceName, AudioClip clip, StrategyAudioBus bus, int priority)
        {
            if (clip == null)
            {
                return null;
            }

            AudioSource source = StrategyAudioMixController.CreateRuntimeSource(transform, sourceName, bus);
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.spatialBlend = 0f;
            source.priority = priority;
            source.playOnAwake = false;
            source.Play();
            return source;
        }

        private IEnumerator LoadLoops()
        {
            ResourceRequest calmWindRequest = Resources.LoadAsync<AudioClip>(CalmWindPath);
            ResourceRequest forestWindRequest = Resources.LoadAsync<AudioClip>(ForestWindPath);
            ResourceRequest rainRequest = Resources.LoadAsync<AudioClip>(CalmRainPath);
            yield return null;
            fireSource = CreateLoop(
                "Journey Distant Fire",
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.Fire),
                StrategyAudioBus.Fire,
                180);
            yield return calmWindRequest;
            calmWindSource = CreateLoop(
                "Journey Calm Wind",
                GetLoadedClip(calmWindRequest, CalmWindPath),
                StrategyAudioBus.Weather,
                182);
            yield return forestWindRequest;
            forestWindSource = CreateLoop(
                "Journey Forest Wind",
                GetLoadedClip(forestWindRequest, ForestWindPath),
                StrategyAudioBus.Weather,
                183);
            yield return rainRequest;
            rainSource = CreateLoop(
                "Journey Rain",
                GetLoadedClip(rainRequest, CalmRainPath),
                StrategyAudioBus.Weather,
                181);
        }

        private static AudioClip GetLoadedClip(ResourceRequest request, string resourcePath)
        {
            AudioClip clip = request.asset as AudioClip;
            if (clip == null)
            {
                StrategyDebugLogger.Warn(
                    "FoundingJourney",
                    "AmbienceClipMissing",
                    StrategyDebugLogger.F("path", resourcePath));
            }

            return clip;
        }

        private static void Fade(AudioSource source, float targetVolume, float deltaTime)
        {
            if (source == null)
            {
                return;
            }

            if (!source.isPlaying)
            {
                source.Play();
            }

            source.volume = Mathf.MoveTowards(
                source.volume,
                Mathf.Clamp01(targetVolume),
                FadeSpeed * deltaTime);
        }

        private static void Stop(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
            }
        }

        private static void EnsureListener(Camera camera)
        {
            Camera owner = camera != null ? camera : Camera.main;
            if (owner != null
                && owner.GetComponent<AudioListener>() == null
                && Object.FindAnyObjectByType<AudioListener>() == null)
            {
                owner.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
