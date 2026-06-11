using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyResidentFootstepAudio : MonoBehaviour
    {
        private const string GrassWalkPath = "Audio/Footsteps/GrassWalk";
        private const float StepCooldownSeconds = 0.12f;
        private const float AdultStepVolume = 0.16f;
        private const float ChildStepVolume = 0.095f;

        private static AudioClip[] grassClips;
        private static bool clipsLoaded;
        private static int nextFallbackSeed = 1;

        private StrategyResidentAgent resident;
        private AudioSource source;
        private AudioReverbFilter reverbFilter;
        private float nextStepTime;
        private int lastStepPhase = -1;
        private int clipCursor;
        private int fallbackSeed;

        public static int LoadedClipCount
        {
            get
            {
                EnsureClipsLoaded();
                return grassClips != null ? grassClips.Length : 0;
            }
        }

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

        public void PlayWalkFrame(int walkFrame, StrategyResidentLifeStage lifeStage)
        {
            EnsureClipsLoaded();
            if (grassClips == null || grassClips.Length <= 0)
            {
                return;
            }

            int stepPhase = GetStepPhase(walkFrame);
            if (stepPhase < 0 || stepPhase == lastStepPhase || Time.time < nextStepTime)
            {
                return;
            }

            EnsureSource();
            if (source == null)
            {
                return;
            }

            lastStepPhase = stepPhase;
            nextStepTime = Time.time + StepCooldownSeconds;
            int residentSeed = resident != null && resident.ResidentId > 0 ? resident.ResidentId : fallbackSeed;
            int clipIndex = Mathf.Abs(residentSeed * 31 + stepPhase * 7 + clipCursor) % grassClips.Length;
            clipCursor++;

            source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            float volume = lifeStage == StrategyResidentLifeStage.Child ? ChildStepVolume : AdultStepVolume;
            source.PlayOneShot(grassClips[clipIndex], volume);
        }

        public void ResetStepPhase()
        {
            lastStepPhase = -1;
        }

        private static int GetStepPhase(int walkFrame)
        {
            return walkFrame == 1
                ? 0
                : walkFrame == 5
                    ? 1
                    : -1;
        }

        private static void EnsureClipsLoaded()
        {
            if (clipsLoaded)
            {
                return;
            }

            clipsLoaded = true;
            grassClips = Resources.LoadAll<AudioClip>(GrassWalkPath);
            if (grassClips != null && grassClips.Length > 1)
            {
                Array.Sort(grassClips, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            }
        }

        private void EnsureSource()
        {
            if (source != null)
            {
                EnsureReverbFilter();
                return;
            }

            source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0.45f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 4f;
            source.maxDistance = 28f;
            source.priority = 128;
            EnsureReverbFilter();
        }

        private void EnsureReverbFilter()
        {
            if (reverbFilter == null)
            {
                reverbFilter = GetComponent<AudioReverbFilter>();
            }

            if (reverbFilter == null)
            {
                reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
            }

            reverbFilter.reverbPreset = AudioReverbPreset.User;
            reverbFilter.dryLevel = 0f;
            reverbFilter.room = -2600f;
            reverbFilter.roomHF = -1200f;
            reverbFilter.decayTime = 0.72f;
            reverbFilter.decayHFRatio = 0.42f;
            reverbFilter.reflectionsLevel = -3100f;
            reverbFilter.reflectionsDelay = 0.014f;
            reverbFilter.reverbLevel = -2100f;
            reverbFilter.reverbDelay = 0.026f;
            reverbFilter.diffusion = 58f;
            reverbFilter.density = 52f;
            reverbFilter.hfReference = 4200f;
            reverbFilter.roomLF = -850f;
            reverbFilter.lfReference = 250f;
        }
    }
}
