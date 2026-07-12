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
        private CityMapController map;
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

            EnsureClipsLoaded();
            map ??= FindAnyObjectByType<CityMapController>();
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

            lastStepPhase = stepPhase;
            nextStepTime = Time.time + StepCooldownSeconds;
            int residentSeed = resident != null && resident.ResidentId > 0 ? resident.ResidentId : fallbackSeed;
            int clipIndex = Mathf.Abs(residentSeed * 31 + stepPhase * 7 + clipCursor) % grassClips.Length;
            clipCursor++;

            float volume = lifeStage == StrategyResidentLifeStage.Child ? ChildStepVolume : AdultStepVolume;
            GetSurfaceProfile(out string surface, out float pitch, out float surfaceVolume);
            StrategyAudioVoicePool.Play(
                grassClips[clipIndex],
                transform.position,
                StrategyAudioBus.Footsteps,
                volume * surfaceVolume,
                pitch * 0.95f,
                pitch * 1.05f,
                StrategyAudioPriority.Ambient,
                "footstep_" + surface,
                0.025f,
                5,
                0.48f,
                3.5f,
                25f);
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

        private void GetSurfaceProfile(out string surface, out float pitch, out float volume)
        {
            surface = "grass";
            pitch = 1f;
            volume = 1f;
            if (map == null)
            {
                map = FindAnyObjectByType<CityMapController>();
            }

            if (map == null || !map.TryWorldToCell(transform.position, out Vector2Int cell))
            {
                return;
            }

            if (StrategyTrailController.Active != null && StrategyTrailController.Active.IsTrailCell(cell))
            {
                surface = "road";
                pitch = 1.11f;
                volume = 1.08f;
                return;
            }

            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            StrategyWeatherController weather = StrategyWeatherController.Active;
            if (snapshot.Season == StrategySeason.Winter && weather != null && weather.SnowIntensity > 0.15f)
            {
                surface = "snow";
                pitch = 0.82f;
                volume = 0.72f;
                return;
            }

            if (map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                if (mapCell.Kind == CityMapCellKind.Dirt)
                {
                    surface = "dirt";
                    pitch = 0.91f;
                    volume = 0.92f;
                }
                else if (mapCell.Kind == CityMapCellKind.Forest)
                {
                    surface = "forest";
                    pitch = 0.86f;
                    volume = 0.84f;
                }
            }
        }
    }
}
