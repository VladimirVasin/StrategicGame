using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyAmbientAudioController : MonoBehaviour
    {
        private const string NaturePath = "Audio/Nature/";
        private const float FadeSpeed = 0.55f;
        private const float RiverRefreshSeconds = 0.45f;
        private const float RiverNearDistance = 8f;
        private const float RiverFarDistance = 34f;
        private const float RiverTargetVolume = 0.075f;

        private readonly List<Vector3> waterPoints = new();
        private CityMapController map;
        private Camera strategyCamera;
        private AudioSource forestBirdsSource;
        private AudioSource cicadasSource;
        private AudioSource nightSource;
        private AudioSource rainCalmSource;
        private AudioSource rainStrongSource;
        private AudioSource riverSource;
        private AudioSource windCalmSource;
        private AudioSource windForestSource;
        private AudioClip forestBirdsClip;
        private AudioClip cicadasClip;
        private AudioClip nightClip;
        private AudioClip rainCalmClip;
        private AudioClip rainStrongClip;
        private AudioClip riverClip;
        private AudioClip windCalmClip;
        private AudioClip windForestClip;
        private float forestBlend;
        private float riverRefreshTimer;
        private float riverDistanceBlend;
        private bool configured;

        public void Configure(CityMapController mapController, Camera camera)
        {
            map = mapController;
            strategyCamera = camera;
            configured = map != null;
            EnsureListener();
            LoadClips();
            RebuildMapAudioProfile();
            EnsureSources();
            StrategyDebugLogger.Info(
                "Audio",
                "AmbientConfigured",
                StrategyDebugLogger.F("configured", configured),
                StrategyDebugLogger.F("waterPoints", waterPoints.Count),
                StrategyDebugLogger.F("forestBlend", forestBlend));
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            EnsureSources();
            UpdateRiverSourcePosition();

            float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float dayPhase = StrategyDayNightCycleController.CurrentDayPhase;
            float dayBlend = Mathf.Clamp01(Mathf.Sin(dayPhase * Mathf.PI * 2f - 0.25f) * 0.55f + 0.55f);
            float nightBlend = 1f - dayBlend;
            float eveningBlend = Mathf.Clamp01(1f - Mathf.Abs(dayPhase - 0.72f) / 0.18f);
            StrategyWeatherController weather = StrategyWeatherController.Active;
            float rainBlend = weather != null ? weather.RainIntensity : 0f;
            float strongRainBlend = weather != null ? Mathf.Max(weather.HeavyRainIntensity, weather.StormIntensity) : 0f;
            float weatherWindBlend = weather != null ? weather.WindIntensity : 0f;
            StrategySeason season = StrategyDayNightCycleController.CurrentCalendarSnapshot.Season;
            float birdSeason = season switch
            {
                StrategySeason.Spring => 1.22f,
                StrategySeason.Summer => 1f,
                StrategySeason.Autumn => 0.58f,
                StrategySeason.Winter => 0.12f,
                _ => 1f
            };
            float insectSeason = season switch
            {
                StrategySeason.Summer => 1f,
                StrategySeason.Spring => 0.48f,
                StrategySeason.Autumn => 0.22f,
                _ => 0f
            };
            float winterHush = season == StrategySeason.Winter ? 0.68f : 1f;
            float ambienceBus = StrategyAudioMixController.GetVolume(StrategyAudioBus.Ambience);
            float weatherBus = StrategyAudioMixController.GetVolume(StrategyAudioBus.Weather);
            float waterBus = StrategyAudioMixController.GetVolume(StrategyAudioBus.Water);
            float windPulse = StrategyWindController.Active != null && StrategyWindController.Active.WindZone != null
                ? Mathf.Clamp01(StrategyWindController.Active.WindZone.windMain
                    + StrategyWindController.Active.WindZone.windPulseMagnitude * 0.5f)
                : 0.55f;

            FadeLoop(forestBirdsSource, forestBirdsClip, ambienceBus * 0.17f * birdSeason * dayBlend * Mathf.Lerp(0.65f, 1f, forestBlend) * (1f - rainBlend * 0.55f), dt);
            FadeLoop(cicadasSource, cicadasClip, ambienceBus * 0.07f * insectSeason * eveningBlend * (1f - rainBlend * 0.7f), dt);
            FadeLoop(nightSource, nightClip, ambienceBus * 0.10f * winterHush * nightBlend * (1f - rainBlend * 0.35f), dt);
            FadeLoop(rainCalmSource, rainCalmClip, weatherBus * 0.085f * rainBlend * (1f - strongRainBlend * 0.35f), dt);
            FadeLoop(rainStrongSource, rainStrongClip, weatherBus * 0.07f * strongRainBlend, dt);
            FadeLoop(windCalmSource, windCalmClip, weatherBus * (0.08f + windPulse * 0.05f + weatherWindBlend * 0.025f), dt);
            FadeLoop(windForestSource, windForestClip, weatherBus * forestBlend * (0.06f + windPulse * 0.05f + weatherWindBlend * 0.025f) * (1f - rainBlend * 0.25f), dt);
            FadeLoop(riverSource, riverClip, waterBus * RiverTargetVolume * riverDistanceBlend, dt);
        }

        private void LoadClips()
        {
            forestBirdsClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Forest_Birds_Loop_Stereo");
            cicadasClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Cicadas_Loop_Stereo");
            nightClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Night_Loop_Stereo");
            rainCalmClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Rain_Calm_Loop_Stereo");
            rainStrongClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Rain_Strong_Loop_Stereo");
            riverClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_River_Moderate_Loop_Stereo");
            windCalmClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Wind_Calm_Loop_Stereo");
            windForestClip = Resources.Load<AudioClip>(NaturePath + "Ambiance_Wind_Forest_Loop_Stereo");
        }

        private void EnsureSources()
        {
            EnsureFlatLoop(ref forestBirdsSource, "Ambience Forest Birds", forestBirdsClip, 170, StrategyAudioBus.Ambience);
            EnsureFlatLoop(ref cicadasSource, "Ambience Cicadas", cicadasClip, 175, StrategyAudioBus.Ambience);
            EnsureFlatLoop(ref nightSource, "Ambience Night", nightClip, 176, StrategyAudioBus.Ambience);
            EnsureFlatLoop(ref rainCalmSource, "Ambience Rain Calm", rainCalmClip, 178, StrategyAudioBus.Weather);
            EnsureFlatLoop(ref rainStrongSource, "Ambience Rain Strong", rainStrongClip, 179, StrategyAudioBus.Weather);
            EnsureFlatLoop(ref windCalmSource, "Ambience Wind Calm", windCalmClip, 180, StrategyAudioBus.Weather);
            EnsureFlatLoop(ref windForestSource, "Ambience Wind Forest", windForestClip, 181, StrategyAudioBus.Weather);
            EnsureRiverSource();
        }

        private void EnsureFlatLoop(ref AudioSource source, string sourceName, AudioClip clip, int priority, StrategyAudioBus bus)
        {
            if (source != null || clip == null)
            {
                return;
            }

            source = CreateSource(sourceName, true, 0f, 0f, priority, bus);
            source.clip = clip;
            source.Play();
        }

        private void EnsureRiverSource()
        {
            if (riverSource != null || riverClip == null)
            {
                return;
            }

            riverSource = CreateSource("Ambience River", true, 0f, 1f, 165, StrategyAudioBus.Water);
            riverSource.clip = riverClip;
            riverSource.minDistance = 6f;
            riverSource.maxDistance = 42f;
            riverSource.rolloffMode = AudioRolloffMode.Linear;
            ConfigureRiverReverb(riverSource.gameObject);
            riverSource.Play();
        }

        private static void ConfigureRiverReverb(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return;
            }

            AudioReverbFilter reverb = sourceObject.GetComponent<AudioReverbFilter>();
            if (reverb == null)
            {
                reverb = sourceObject.AddComponent<AudioReverbFilter>();
            }

            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.dryLevel = 0f;
            reverb.room = -2200f;
            reverb.roomHF = -850f;
            reverb.decayTime = 1.25f;
            reverb.decayHFRatio = 0.46f;
            reverb.reflectionsLevel = -2600f;
            reverb.reflectionsDelay = 0.024f;
            reverb.reverbLevel = -1650f;
            reverb.reverbDelay = 0.045f;
            reverb.diffusion = 70f;
            reverb.density = 64f;
            reverb.hfReference = 4600f;
            reverb.roomLF = -550f;
            reverb.lfReference = 260f;
        }

        private AudioSource CreateSource(string sourceName, bool loop, float volume, float spatialBlend, int priority, StrategyAudioBus bus)
        {
            AudioSource source = StrategyAudioMixController.CreateRuntimeSource(transform, sourceName, bus);
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.priority = priority;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.minDistance = 10f;
            source.maxDistance = 70f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        private void RebuildMapAudioProfile()
        {
            waterPoints.Clear();
            forestBlend = 0f;
            if (map == null)
            {
                return;
            }

            int forestCells = 0;
            int totalCells = Mathf.Max(1, map.Width * map.Height);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!map.TryGetCell(x, y, out CityMapCell cell))
                    {
                        continue;
                    }

                    if (cell.Kind == CityMapCellKind.Forest)
                    {
                        forestCells++;
                    }

                    if (cell.Kind == CityMapCellKind.Water)
                    {
                        waterPoints.Add(map.GetCellCenterWorld(x, y));
                    }
                }
            }

            forestBlend = Mathf.Clamp01(forestCells / (float)totalCells * 4.5f);
        }

        private void UpdateRiverSourcePosition()
        {
            if (riverSource == null || waterPoints.Count <= 0)
            {
                riverDistanceBlend = 0f;
                return;
            }

            riverRefreshTimer -= Time.unscaledDeltaTime;
            if (riverRefreshTimer > 0f)
            {
                return;
            }

            riverRefreshTimer = RiverRefreshSeconds;
            Vector3 focus = strategyCamera != null ? strategyCamera.transform.position : map.WorldBounds.center;
            focus.z = 0f;
            Vector3 nearest = waterPoints[0];
            float nearestSqr = (nearest - focus).sqrMagnitude;
            for (int i = 1; i < waterPoints.Count; i++)
            {
                float distanceSqr = (waterPoints[i] - focus).sqrMagnitude;
                if (distanceSqr < nearestSqr)
                {
                    nearestSqr = distanceSqr;
                    nearest = waterPoints[i];
                }
            }

            riverSource.transform.position = new Vector3(nearest.x, nearest.y, 0f);
            float distance = Mathf.Sqrt(nearestSqr);
            riverDistanceBlend = 1f - Mathf.InverseLerp(RiverNearDistance, RiverFarDistance, distance);
        }

        private static void FadeLoop(AudioSource source, AudioClip clip, float targetVolume, float dt)
        {
            if (source == null || clip == null)
            {
                return;
            }

            if (!source.isPlaying)
            {
                source.Play();
            }

            source.volume = Mathf.MoveTowards(source.volume, Mathf.Clamp01(targetVolume), FadeSpeed * dt);
        }

        private void EnsureListener()
        {
            if (strategyCamera == null)
            {
                strategyCamera = Camera.main;
            }

            if (strategyCamera != null
                && strategyCamera.GetComponent<AudioListener>() == null
                && Object.FindAnyObjectByType<AudioListener>() == null)
            {
                strategyCamera.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
