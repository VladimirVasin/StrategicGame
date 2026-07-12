using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyAudioPriority
    {
        Ambient,
        Normal,
        Important,
        Critical
    }

    [DisallowMultipleComponent]
    public sealed class StrategyAudioVoicePool : MonoBehaviour
    {
        private const int VoiceCount = 18;
        private const float RefreshInterval = 0.10f;

        private static StrategyAudioVoicePool instance;
        private readonly Voice[] voices = new Voice[VoiceCount];
        private readonly Dictionary<string, float> nextPlayTimes = new();
        private float refreshTimer;
        private int droppedVoices;

        public static int ActiveVoiceCount => instance != null ? instance.CountActiveVoices() : 0;
        public static int DroppedVoiceCount => instance != null ? instance.droppedVoices : 0;
        public static int Capacity => VoiceCount;

        public static bool Play(
            AudioClip clip,
            Vector3 worldPosition,
            StrategyAudioBus bus,
            float volume,
            float pitchMin = 0.96f,
            float pitchMax = 1.04f,
            StrategyAudioPriority priority = StrategyAudioPriority.Normal,
            string concurrencyKey = null,
            float cooldown = 0f,
            int concurrencyLimit = 3,
            float spatialBlend = 0.72f,
            float minDistance = 3.5f,
            float maxDistance = 34f)
        {
            if (clip == null || volume <= 0f)
            {
                return false;
            }

            return EnsureInstance().PlayInternal(
                clip,
                worldPosition,
                bus,
                volume,
                pitchMin,
                pitchMax,
                priority,
                concurrencyKey,
                cooldown,
                concurrencyLimit,
                spatialBlend,
                minDistance,
                maxDistance);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureVoices();
        }

        private void Update()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = RefreshInterval;
            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (voice == null || voice.Source == null || !voice.Source.isPlaying)
                {
                    continue;
                }

                StrategyAudioWorldMix mix = StrategyAudioMixController.EvaluateWorld(voice.Position, voice.Bus);
                voice.Source.volume = voice.BaseVolume * mix.VolumeMultiplier;
                StrategyAudioMixController.ApplyWorldFilters(voice.LowPass, voice.Reverb, mix, voice.ReverbScale);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private static StrategyAudioVoicePool EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<StrategyAudioVoicePool>();
            if (instance == null)
            {
                GameObject poolObject = new GameObject("Strategy Audio Voice Pool");
                instance = poolObject.AddComponent<StrategyAudioVoicePool>();
            }

            return instance;
        }

        private bool PlayInternal(
            AudioClip clip,
            Vector3 position,
            StrategyAudioBus bus,
            float volume,
            float pitchMin,
            float pitchMax,
            StrategyAudioPriority priority,
            string concurrencyKey,
            float cooldown,
            int concurrencyLimit,
            float spatialBlend,
            float minDistance,
            float maxDistance)
        {
            EnsureVoices();
            float now = Time.unscaledTime;
            string key = string.IsNullOrEmpty(concurrencyKey) ? clip.name : concurrencyKey;
            if (nextPlayTimes.TryGetValue(key, out float nextPlay) && now < nextPlay)
            {
                return false;
            }

            int activeForKey = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i].Source.isPlaying && voices[i].Key == key)
                {
                    activeForKey++;
                }
            }

            if (activeForKey >= Mathf.Max(1, concurrencyLimit))
            {
                droppedVoices++;
                return false;
            }

            StrategyAudioWorldMix mix = StrategyAudioMixController.EvaluateWorld(position, bus);
            float finalVolume = volume * mix.VolumeMultiplier;
            if (finalVolume <= 0.004f && priority < StrategyAudioPriority.Important)
            {
                return false;
            }

            Voice voice = TakeVoice(priority, position);
            if (voice == null)
            {
                droppedVoices++;
                return false;
            }

            nextPlayTimes[key] = now + Mathf.Max(0f, cooldown);
            voice.Position = position;
            voice.Bus = bus;
            voice.BaseVolume = volume;
            voice.Priority = priority;
            voice.Key = key;
            voice.ReverbScale = bus == StrategyAudioBus.ImportantEvents ? 0.58f : 0.92f;
            voice.Source.Stop();
            StrategyAudioMixController.ApplySourceDefaults(voice.Source, bus);
            voice.Source.transform.position = position;
            voice.Source.clip = clip;
            voice.Source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax) * Mathf.Lerp(1f, 0.965f, mix.FarBlend);
            voice.Source.volume = finalVolume;
            voice.Source.spatialBlend = Mathf.Clamp01(spatialBlend);
            voice.Source.minDistance = Mathf.Max(0.1f, minDistance);
            voice.Source.maxDistance = Mathf.Max(voice.Source.minDistance + 0.1f, maxDistance);
            voice.Source.priority = GetUnityPriority(priority);
            StrategyAudioMixController.ApplyWorldFilters(voice.LowPass, voice.Reverb, mix, voice.ReverbScale);
            voice.Source.Play();
            return true;
        }

        private Voice TakeVoice(StrategyAudioPriority priority, Vector3 position)
        {
            Voice fallback = null;
            float fallbackScore = float.MaxValue;
            Camera camera = Camera.main;
            Vector3 listener = camera != null ? camera.transform.position : Vector3.zero;
            for (int i = 0; i < voices.Length; i++)
            {
                Voice voice = voices[i];
                if (!voice.Source.isPlaying)
                {
                    return voice;
                }

                float distance = (voice.Position - listener).sqrMagnitude;
                float score = (int)voice.Priority * 100000f - distance;
                if (voice.Priority <= priority && score < fallbackScore)
                {
                    fallback = voice;
                    fallbackScore = score;
                }
            }

            return fallback;
        }

        private void EnsureVoices()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null && voices[i].Source != null)
                {
                    continue;
                }

                AudioSource source = StrategyAudioMixController.CreateRuntimeSource(transform, "World Voice " + (i + 1), StrategyAudioBus.Work);
                source.loop = false;
                source.spatialBlend = 0.72f;
                source.rolloffMode = AudioRolloffMode.Linear;
                GameObject host = source.gameObject;
                AudioLowPassFilter lowPass = host.AddComponent<AudioLowPassFilter>();
                AudioReverbFilter reverb = host.AddComponent<AudioReverbFilter>();
                voices[i] = new Voice(source, lowPass, reverb);
            }
        }

        private int CountActiveVoices()
        {
            int count = 0;
            for (int i = 0; i < voices.Length; i++)
            {
                if (voices[i] != null && voices[i].Source != null && voices[i].Source.isPlaying)
                {
                    count++;
                }
            }

            return count;
        }

        private static int GetUnityPriority(StrategyAudioPriority priority)
        {
            return priority switch
            {
                StrategyAudioPriority.Critical => 32,
                StrategyAudioPriority.Important => 72,
                StrategyAudioPriority.Normal => 128,
                _ => 190
            };
        }

        private sealed class Voice
        {
            public Voice(AudioSource source, AudioLowPassFilter lowPass, AudioReverbFilter reverb)
            {
                Source = source;
                LowPass = lowPass;
                Reverb = reverb;
            }

            public AudioSource Source { get; }
            public AudioLowPassFilter LowPass { get; }
            public AudioReverbFilter Reverb { get; }
            public Vector3 Position { get; set; }
            public StrategyAudioBus Bus { get; set; }
            public StrategyAudioPriority Priority { get; set; }
            public float BaseVolume { get; set; }
            public float ReverbScale { get; set; }
            public string Key { get; set; }
        }
    }
}
