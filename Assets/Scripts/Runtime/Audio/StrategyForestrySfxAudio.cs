using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyForestrySfxAudio : MonoBehaviour
    {
        private const string TreeFallPath = "Audio/WorkSfx/TreeFall";
        private const string TreeBreakLogsPath = "Audio/WorkSfx/TreeBreakLogs";
        private const int SourcePoolSize = 4;

        private static StrategyForestrySfxAudio instance;
        private static AudioClip[] treeFallClips;
        private static AudioClip[] treeBreakLogsClips;
        private static bool clipsLoaded;

        private readonly AudioSource[] sources = new AudioSource[SourcePoolSize];
        private readonly AudioLowPassFilter[] lowPassFilters = new AudioLowPassFilter[SourcePoolSize];
        private readonly AudioReverbFilter[] reverbFilters = new AudioReverbFilter[SourcePoolSize];
        private int sourceCursor;
        private int clipCursor;
        private float nextTreeFallTime;
        private float nextTreeBreakLogsTime;

        public static void PlayTreeFall(Vector3 worldPosition)
        {
            EnsureInstance()?.PlayClip(
                treeFallClips,
                worldPosition,
                0.52f,
                0.94f,
                1.04f,
                0.18f,
                ref instance.nextTreeFallTime,
                17);
        }

        public static void PlayTreeBreakLogs(Vector3 worldPosition)
        {
            EnsureInstance()?.PlayClip(
                treeBreakLogsClips,
                worldPosition,
                0.43f,
                0.96f,
                1.06f,
                0.12f,
                ref instance.nextTreeBreakLogsTime,
                29);
        }

        private static StrategyForestrySfxAudio EnsureInstance()
        {
            if (instance != null)
            {
                EnsureClipsLoaded();
                return instance;
            }

            instance = FindAnyObjectByType<StrategyForestrySfxAudio>();
            if (instance == null)
            {
                GameObject audioObject = new("Strategy Forestry SFX Audio");
                instance = audioObject.AddComponent<StrategyForestrySfxAudio>();
            }

            DontDestroyOnLoad(instance.gameObject);
            EnsureClipsLoaded();
            return instance;
        }

        private static void EnsureClipsLoaded()
        {
            if (clipsLoaded)
            {
                return;
            }

            clipsLoaded = true;
            treeFallClips = LoadSorted(TreeFallPath);
            treeBreakLogsClips = LoadSorted(TreeBreakLogsPath);
        }

        private static AudioClip[] LoadSorted(string path)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
            if (clips != null && clips.Length > 1)
            {
                Array.Sort(clips, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            }

            return clips;
        }

        private void PlayClip(
            AudioClip[] clips,
            Vector3 worldPosition,
            float volume,
            float pitchMin,
            float pitchMax,
            float cooldown,
            ref float nextAllowedTime,
            int seedSalt)
        {
            if (clips == null || clips.Length <= 0 || Time.unscaledTime < nextAllowedTime)
            {
                return;
            }

            StrategyAudioWorldMix mix = StrategyAudioMixController.EvaluateWorld(worldPosition, StrategyAudioBus.ResidentWork);
            float finalVolume = volume * mix.VolumeMultiplier;
            if (finalVolume <= 0.006f)
            {
                return;
            }

            int sourceIndex = TakeSourceIndex();
            AudioSource source = sources[sourceIndex];
            source.transform.position = worldPosition;
            StrategyAudioMixController.ApplyWorldFilters(lowPassFilters[sourceIndex], reverbFilters[sourceIndex], mix, 1.08f);

            nextAllowedTime = Time.unscaledTime + cooldown;
            int clipIndex = Mathf.Abs(Mathf.RoundToInt(worldPosition.x * 13f + worldPosition.y * 31f) * seedSalt + clipCursor) % clips.Length;
            clipCursor++;
            source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax) * Mathf.Lerp(1f, 0.955f, mix.FarBlend);
            source.PlayOneShot(clips[clipIndex], finalVolume);
        }

        private int TakeSourceIndex()
        {
            EnsureSourcePool();
            int selected = sourceCursor;
            for (int i = 0; i < sources.Length; i++)
            {
                int index = (sourceCursor + i) % sources.Length;
                if (sources[index] != null && !sources[index].isPlaying)
                {
                    selected = index;
                    break;
                }
            }

            sourceCursor = (selected + 1) % sources.Length;
            return selected;
        }

        private void EnsureSourcePool()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                {
                    continue;
                }

                AudioSource source = StrategyAudioMixController.CreateRuntimeSource(transform, "Forestry SFX Source " + (i + 1), StrategyAudioBus.ResidentWork);
                source.loop = false;
                source.playOnAwake = false;
                source.spatialBlend = 0.72f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 4.5f;
                source.maxDistance = 40f;
                source.priority = 118;
                sources[i] = source;
                lowPassFilters[i] = source.gameObject.AddComponent<AudioLowPassFilter>();
                reverbFilters[i] = source.gameObject.AddComponent<AudioReverbFilter>();
                lowPassFilters[i].cutoffFrequency = 22000f;
                lowPassFilters[i].lowpassResonanceQ = 1f;
                reverbFilters[i].reverbPreset = AudioReverbPreset.User;
                reverbFilters[i].dryLevel = 0f;
                reverbFilters[i].room = -10000f;
                reverbFilters[i].reverbLevel = -10000f;
            }
        }
    }
}
