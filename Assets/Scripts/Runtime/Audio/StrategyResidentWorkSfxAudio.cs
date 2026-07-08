using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyResidentWorkSfxAudio : MonoBehaviour
    {
        private const string AxeHitPath = "Audio/WorkSfx/AxeHitWood";
        private const string HammerHitPath = "Audio/WorkSfx/HammerHitBuild";
        private const string PickaxeHitPath = "Audio/WorkSfx/PickaxeHitStone";
        private const string FishingCastPath = "Audio/WorkSfx/FishingCast";
        private const string FishingCatchPath = "Audio/WorkSfx/FishingCatch";
        private const string BowShotPath = "Audio/WorkSfx/BowShot";

        private static AudioClip[] axeHitClips;
        private static AudioClip[] hammerHitClips;
        private static AudioClip[] pickaxeHitClips;
        private static AudioClip[] fishingCastClips;
        private static AudioClip[] fishingCatchClips;
        private static AudioClip[] bowShotClips;
        private static bool clipsLoaded;
        private static int nextFallbackSeed = 1;

        private StrategyResidentAgent resident;
        private AudioSource source;
        private AudioLowPassFilter lowPassFilter;
        private AudioReverbFilter reverbFilter;
        private int fallbackSeed;
        private int clipCursor;
        private float nextAxeTime;
        private float nextHammerTime;
        private float nextPickaxeTime;
        private float nextFishingCastTime;
        private float nextFishingCatchTime;
        private float nextBowTime;

        public void Configure(StrategyResidentAgent residentAgent)
        {
            resident = residentAgent;
            if (fallbackSeed <= 0)
            {
                fallbackSeed = nextFallbackSeed++;
            }

            EnsureSource();
            EnsureClipsLoaded();
        }

        public void PlayAxeHit()
        {
            PlayClip(axeHitClips, 0.24f, 0.92f, 1.08f, 0.06f, ref nextAxeTime, 11);
        }

        public void PlayHammerHit()
        {
            PlayClip(hammerHitClips, 0.21f, 0.94f, 1.07f, 0.055f, ref nextHammerTime, 23);
        }

        public void PlayPickaxeHit()
        {
            PlayClip(pickaxeHitClips, 0.22f, 0.93f, 1.08f, 0.055f, ref nextPickaxeTime, 29);
        }

        public void PlayFishingCast()
        {
            PlayClip(fishingCastClips, 0.15f, 0.96f, 1.05f, 0.16f, ref nextFishingCastTime, 37);
        }

        public void PlayFishingCatch()
        {
            PlayClip(fishingCatchClips, 0.19f, 0.94f, 1.06f, 0.16f, ref nextFishingCatchTime, 41);
        }

        public void PlayBowShot()
        {
            PlayClip(bowShotClips, 0.18f, 0.94f, 1.08f, 0.12f, ref nextBowTime, 53);
        }

        private void PlayClip(
            AudioClip[] clips,
            float volume,
            float pitchMin,
            float pitchMax,
            float cooldown,
            ref float nextAllowedTime,
            int seedSalt)
        {
            EnsureClipsLoaded();
            if (clips == null || clips.Length <= 0 || Time.unscaledTime < nextAllowedTime)
            {
                return;
            }

            EnsureSource();
            if (source == null)
            {
                return;
            }

            nextAllowedTime = Time.unscaledTime + cooldown;
            int residentSeed = resident != null && resident.ResidentId > 0 ? resident.ResidentId : fallbackSeed;
            int clipIndex = Mathf.Abs(residentSeed * seedSalt + clipCursor) % clips.Length;
            clipCursor++;
            StrategyAudioWorldMix mix = StrategyAudioMixController.EvaluateWorld(transform.position, StrategyAudioBus.ResidentWork);
            float finalVolume = volume * mix.VolumeMultiplier;
            if (finalVolume <= 0.006f)
            {
                return;
            }

            StrategyAudioMixController.ApplyWorldFilters(lowPassFilter, reverbFilter, mix, 0.92f);
            source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax) * Mathf.Lerp(1f, 0.965f, mix.FarBlend);
            source.PlayOneShot(clips[clipIndex], finalVolume);
        }

        private static void EnsureClipsLoaded()
        {
            if (clipsLoaded)
            {
                return;
            }

            clipsLoaded = true;
            axeHitClips = LoadSorted(AxeHitPath);
            hammerHitClips = LoadSorted(HammerHitPath);
            pickaxeHitClips = LoadSorted(PickaxeHitPath);
            fishingCastClips = LoadSorted(FishingCastPath);
            fishingCatchClips = LoadSorted(FishingCatchPath);
            bowShotClips = LoadSorted(BowShotPath);
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

        private void EnsureSource()
        {
            if (source != null)
            {
                EnsureWorldFilters();
                return;
            }

            source = StrategyAudioMixController.CreateRuntimeSource(transform, "Resident Work SFX Audio", StrategyAudioBus.ResidentWork);
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0.62f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3.5f;
            source.maxDistance = 30f;
            source.priority = 126;
            EnsureWorldFilters();
        }

        private void EnsureWorldFilters()
        {
            GameObject filterHost = source != null ? source.gameObject : gameObject;
            if (lowPassFilter == null)
            {
                lowPassFilter = filterHost.GetComponent<AudioLowPassFilter>();
            }

            if (lowPassFilter == null)
            {
                lowPassFilter = filterHost.AddComponent<AudioLowPassFilter>();
            }

            if (reverbFilter == null)
            {
                reverbFilter = filterHost.GetComponent<AudioReverbFilter>();
            }

            if (reverbFilter == null)
            {
                reverbFilter = filterHost.AddComponent<AudioReverbFilter>();
            }

            lowPassFilter.cutoffFrequency = 22000f;
            lowPassFilter.lowpassResonanceQ = 1f;
            reverbFilter.reverbPreset = AudioReverbPreset.User;
            reverbFilter.dryLevel = 0f;
            reverbFilter.room = -10000f;
            reverbFilter.reverbLevel = -10000f;
        }
    }
}
