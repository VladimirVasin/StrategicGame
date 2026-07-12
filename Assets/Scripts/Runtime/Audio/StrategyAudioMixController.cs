using UnityEngine;
using UnityEngine.Audio;

namespace ProjectUnknown.Strategy
{
    public enum StrategyAudioBus
    {
        Master,
        Music,
        Ambience,
        Weather,
        Water,
        Settlement,
        Work,
        Footsteps,
        Wildlife,
        Fire,
        ImportantEvents,
        Hud
    }

    public struct StrategyAudioWorldMix
    {
        public StrategyAudioWorldMix(
            float volumeMultiplier,
            float farBlend,
            float focusFactor,
            float zoomFactor,
            float lowPassCutoff)
        {
            VolumeMultiplier = volumeMultiplier;
            FarBlend = farBlend;
            FocusFactor = focusFactor;
            ZoomFactor = zoomFactor;
            LowPassCutoff = lowPassCutoff;
        }

        public float VolumeMultiplier { get; }
        public float FarBlend { get; }
        public float FocusFactor { get; }
        public float ZoomFactor { get; }
        public float LowPassCutoff { get; }
    }

    [DisallowMultipleComponent]
    public sealed class StrategyAudioMixController : MonoBehaviour
    {
        private const float NearZoomSize = 7f;
        private const float FarZoomSize = 42f;
        private const float NearCutoff = 22000f;
        private const float MidCutoff = 7200f;
        private const float FarCutoff = 2600f;

        private static StrategyAudioMixController instance;

        private readonly float[] busVolumes = new float[(int)StrategyAudioBus.Hud + 1];
        private readonly AudioMixerGroup[] busGroups = new AudioMixerGroup[(int)StrategyAudioBus.Hud + 1];
        private AudioMixer unityMixer;
        private Camera strategyCamera;
        private int lastRefreshFrame = -1;
        private float zoomFactor;
        private float weatherMask;
        private float nightBlend;
        private float pausedBlend;

        public static StrategyAudioMixController Active => instance;

        public float CameraZoomFactor
        {
            get
            {
                RefreshMix(false);
                return zoomFactor;
            }
        }

        public float NightBlend => nightBlend;
        public float WeatherBlend => weatherMask;
        public float PausedBlend => pausedBlend;

        public void Configure(Camera camera)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            strategyCamera = camera != null ? camera : Camera.main;
            DontDestroyOnLoad(gameObject);
            LoadMixerRouting();
            RefreshMix(true);
            StrategyDebugLogger.Info(
                "Audio",
                "MixConfigured",
                StrategyDebugLogger.F("cameraFound", strategyCamera != null),
                StrategyDebugLogger.F("zoomFactor", zoomFactor));
        }

        public static float GetVolume(StrategyAudioBus bus)
        {
            StrategyAudioMixController mixer = EnsureInstance();
            mixer.RefreshMix(false);
            return mixer.GetBusVolume(bus);
        }

        public static StrategyAudioWorldMix EvaluateWorld(Vector3 worldPosition, StrategyAudioBus bus)
        {
            StrategyAudioMixController mixer = EnsureInstance();
            mixer.RefreshMix(false);
            return mixer.EvaluateWorldInternal(worldPosition, bus);
        }

        public static void ApplySourceDefaults(AudioSource source, StrategyAudioBus bus)
        {
            if (source == null)
            {
                return;
            }

            source.dopplerLevel = 0f;
            AudioMixerGroup group = instance != null ? instance.GetBusGroup(bus) : null;
            if (group != null)
            {
                source.outputAudioMixerGroup = group;
            }

            if (bus == StrategyAudioBus.Hud)
            {
                source.ignoreListenerPause = true;
            }
        }

        public static AudioSource CreateRuntimeSource(Transform parent, string sourceName, StrategyAudioBus bus)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.SetActive(false);
            if (parent != null)
            {
                sourceObject.transform.SetParent(parent, false);
            }

            sourceObject.transform.localPosition = Vector3.zero;
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            ApplySourceDefaults(source, bus);
            sourceObject.SetActive(true);
            return source;
        }

        public static void ApplyWorldFilters(
            AudioLowPassFilter lowPass,
            AudioReverbFilter reverb,
            StrategyAudioWorldMix mix,
            float reverbScale)
        {
            if (lowPass != null)
            {
                lowPass.cutoffFrequency = mix.LowPassCutoff;
                lowPass.lowpassResonanceQ = Mathf.Lerp(1.0f, 1.18f, mix.FarBlend);
            }

            if (reverb == null)
            {
                return;
            }

            float blend = Mathf.Clamp01(mix.FarBlend * Mathf.Max(0f, reverbScale));
            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.dryLevel = 0f;
            reverb.room = Mathf.Lerp(-10000f, -1700f, blend);
            reverb.roomHF = Mathf.Lerp(-10000f, -1200f, blend);
            reverb.decayTime = Mathf.Lerp(0.45f, 1.55f, blend);
            reverb.decayHFRatio = Mathf.Lerp(0.34f, 0.52f, blend);
            reverb.reflectionsLevel = Mathf.Lerp(-10000f, -2600f, blend);
            reverb.reflectionsDelay = Mathf.Lerp(0.006f, 0.034f, blend);
            reverb.reverbLevel = Mathf.Lerp(-10000f, -1500f, blend);
            reverb.reverbDelay = Mathf.Lerp(0.014f, 0.052f, blend);
            reverb.diffusion = Mathf.Lerp(48f, 78f, blend);
            reverb.density = Mathf.Lerp(46f, 72f, blend);
            reverb.hfReference = Mathf.Lerp(6200f, 3900f, blend);
            reverb.roomLF = Mathf.Lerp(-1200f, -520f, blend);
            reverb.lfReference = 250f;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            strategyCamera = strategyCamera != null ? strategyCamera : Camera.main;
            DontDestroyOnLoad(gameObject);
            LoadMixerRouting();
            RefreshMix(true);
        }

        private void Update()
        {
            RefreshMix(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private static StrategyAudioMixController EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<StrategyAudioMixController>();
            if (instance == null)
            {
                GameObject mixerObject = new GameObject("Strategy Audio Mix");
                instance = mixerObject.AddComponent<StrategyAudioMixController>();
            }

            instance.Configure(Camera.main);
            return instance;
        }

        private void RefreshMix(bool force)
        {
            if (!force && lastRefreshFrame == Time.frameCount)
            {
                return;
            }

            lastRefreshFrame = Time.frameCount;
            if (strategyCamera == null)
            {
                strategyCamera = Camera.main;
            }

            float dayPhase = StrategyDayNightCycleController.CurrentDayPhase;
            float dayBlend = Mathf.Clamp01(Mathf.Sin(dayPhase * Mathf.PI * 2f - 0.25f) * 0.55f + 0.55f);
            nightBlend = 1f - dayBlend;

            StrategyWeatherController weather = StrategyWeatherController.Active;
            float rain = weather != null ? weather.RainIntensity : 0f;
            float snow = weather != null ? weather.SnowIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float fog = weather != null ? weather.FogIntensity : 0f;
            float wind = weather != null ? weather.WindIntensity : 0f;
            weatherMask = Mathf.Clamp01(Mathf.Max(Mathf.Max(rain, snow), Mathf.Max(storm, fog * 0.65f)));
            pausedBlend = Mathf.MoveTowards(pausedBlend, Time.timeScale <= 0f ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            zoomFactor = strategyCamera != null
                ? Mathf.InverseLerp(NearZoomSize, FarZoomSize, strategyCamera.orthographicSize)
                : 0f;

            float master = StrategyGameSettings.MasterVolume;
            float music = StrategyGameSettings.MusicVolume;
            float sfx = StrategyGameSettings.SfxVolume;
            float worldPause = Mathf.Lerp(1f, 0.34f, pausedBlend);
            float stormDuck = Mathf.Lerp(1f, 0.82f, Mathf.Max(storm, weatherMask * 0.35f));
            SetVolume(StrategyAudioBus.Master, master);
            SetVolume(StrategyAudioBus.Music, master * music * 0.88f * stormDuck * Mathf.Lerp(1f, 0.82f, pausedBlend));
            SetVolume(StrategyAudioBus.Ambience, master * sfx * worldPause * Mathf.Lerp(0.96f, 1.12f, zoomFactor) * Mathf.Lerp(1f, 1.05f, nightBlend));
            SetVolume(StrategyAudioBus.Weather, master * sfx * worldPause * Mathf.Lerp(0.98f, 1.16f, Mathf.Max(weatherMask, wind * 0.6f)) * Mathf.Lerp(1f, 1.08f, zoomFactor));
            SetVolume(StrategyAudioBus.Water, master * sfx * worldPause * Mathf.Lerp(1.04f, 0.86f, zoomFactor));
            SetVolume(StrategyAudioBus.Settlement, master * sfx * worldPause * 0.82f * Mathf.Lerp(0.76f, 1.16f, zoomFactor) * Mathf.Lerp(1f, 0.72f, weatherMask));
            SetVolume(StrategyAudioBus.Work, master * sfx * worldPause * 0.88f * Mathf.Lerp(1f, 0.60f, zoomFactor) * Mathf.Lerp(1f, 0.82f, weatherMask));
            SetVolume(StrategyAudioBus.Footsteps, master * sfx * worldPause * 0.74f * Mathf.Lerp(1f, 0.38f, zoomFactor) * Mathf.Lerp(1f, 0.72f, weatherMask));
            SetVolume(StrategyAudioBus.Wildlife, master * sfx * worldPause * 0.76f * Mathf.Lerp(1f, 0.70f, weatherMask));
            SetVolume(StrategyAudioBus.Fire, master * sfx * worldPause * 0.92f * Mathf.Lerp(1f, 0.74f, weatherMask));
            SetVolume(StrategyAudioBus.ImportantEvents, master * sfx * Mathf.Lerp(1f, 0.78f, pausedBlend));
            SetVolume(StrategyAudioBus.Hud, master * sfx * 0.82f);
        }

        private StrategyAudioWorldMix EvaluateWorldInternal(Vector3 worldPosition, StrategyAudioBus bus)
        {
            float focus = 1f;
            float visibilityVolume = 1f;
            float offscreen = 0f;
            if (strategyCamera != null)
            {
                Vector3 viewport = strategyCamera.WorldToViewportPoint(worldPosition);
                float x = (viewport.x - 0.5f) * 2f;
                float y = (viewport.y - 0.5f) * 2f;
                float centerDistance = Mathf.Sqrt(x * x + y * y);
                focus = 1f - Mathf.InverseLerp(0.22f, 1.18f, centerDistance);

                bool visible = viewport.z >= -0.1f
                    && viewport.x >= 0f
                    && viewport.x <= 1f
                    && viewport.y >= 0f
                    && viewport.y <= 1f;

                float outsideX = Mathf.Max(0f, Mathf.Abs(x) - 1f);
                float outsideY = Mathf.Max(0f, Mathf.Abs(y) - 1f);
                offscreen = Mathf.Clamp01(Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY));
                visibilityVolume = visible ? 1f : Mathf.Lerp(0.50f, 0.18f, offscreen);
            }

            float focusVolume = Mathf.Lerp(0.42f, 1f, Mathf.Pow(Mathf.Clamp01(focus), 0.7f));
            float farBlend = Mathf.Clamp01((1f - focus) * 0.58f + zoomFactor * 0.46f + offscreen * 0.52f + weatherMask * 0.10f);
            float muffledCutoff = Mathf.Lerp(MidCutoff, FarCutoff, zoomFactor);
            float cutoff = Mathf.Lerp(NearCutoff, muffledCutoff, farBlend);
            float volume = GetBusVolume(bus) * focusVolume * visibilityVolume;
            return new StrategyAudioWorldMix(volume, farBlend, focus, zoomFactor, cutoff);
        }

        private float GetBusVolume(StrategyAudioBus bus)
        {
            int index = (int)bus;
            if (index < 0 || index >= busVolumes.Length)
            {
                return 1f;
            }

            return busVolumes[index];
        }

        private void SetVolume(StrategyAudioBus bus, float value)
        {
            int index = (int)bus;
            if (index < 0 || index >= busVolumes.Length)
            {
                return;
            }

            busVolumes[index] = Mathf.Clamp01(value);
        }

        private void LoadMixerRouting()
        {
            if (unityMixer != null)
            {
                return;
            }

            unityMixer = Resources.Load<AudioMixer>("Audio/StrategyAudioMixer");
            if (unityMixer == null)
            {
                return;
            }

            for (int i = 0; i < busGroups.Length; i++)
            {
                StrategyAudioBus bus = (StrategyAudioBus)i;
                AudioMixerGroup[] matches = unityMixer.FindMatchingGroups(bus.ToString());
                busGroups[i] = matches != null && matches.Length > 0 ? matches[0] : null;
            }
        }

        private AudioMixerGroup GetBusGroup(StrategyAudioBus bus)
        {
            LoadMixerRouting();
            int index = (int)bus;
            return index >= 0 && index < busGroups.Length ? busGroups[index] : null;
        }
    }
}
