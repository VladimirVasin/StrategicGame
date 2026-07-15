using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWorldAudioDirector : MonoBehaviour
    {
        private const float MixRefreshInterval = 0.20f;
        private const float DependencyRefreshInterval = 2f;
        private const float FadeSpeed = 0.16f;
        internal const float WolfHowlCooldownSeconds = 90f;
        internal const int WolfHowlConcurrencyLimit = 1;

        private CityMapController map;
        private Camera strategyCamera;
        private StrategyPopulationController population;
        private StrategyBuildPlacementController placement;
        private StrategyCampfireAnimator campfire;
        private AudioSource settlementDaySource;
        private AudioSource settlementNightSource;
        private AudioSource fireSource;
        private AudioSource winterSource;
        private float mixTimer;
        private float dependencyTimer;
        private float spotTimer;
        private float previousFireFactor;
        private float diagnosticsTimer = 30f;
        private bool configured;

        public void Configure(CityMapController mapController, Camera camera)
        {
            map = mapController;
            strategyCamera = camera != null ? camera : Camera.main;
            EnsureSources();
            RefreshDependencies();
            configured = map != null;
            StrategyDebugLogger.Info(
                "Audio",
                "WorldDirectorConfigured",
                StrategyDebugLogger.F("configured", configured),
                StrategyDebugLogger.F("voiceBudget", 18));
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            dependencyTimer -= Time.unscaledDeltaTime;
            if (dependencyTimer <= 0f)
            {
                dependencyTimer = DependencyRefreshInterval;
                RefreshDependencies();
            }

            mixTimer -= Time.unscaledDeltaTime;
            if (mixTimer <= 0f)
            {
                mixTimer = MixRefreshInterval;
                UpdateSoundscape(MixRefreshInterval);
            }

            UpdateSpotEvents();
            diagnosticsTimer -= Time.unscaledDeltaTime;
            if (diagnosticsTimer <= 0f)
            {
                diagnosticsTimer = 30f;
                StrategyDebugLogger.Info(
                    "Audio",
                    "VoiceBudget",
                    StrategyDebugLogger.F("active", StrategyAudioVoicePool.ActiveVoiceCount),
                    StrategyDebugLogger.F("capacity", StrategyAudioVoicePool.Capacity),
                    StrategyDebugLogger.F("dropped", StrategyAudioVoicePool.DroppedVoiceCount),
                    StrategyDebugLogger.F("suppressed", StrategyAudioVoicePool.SuppressedVoiceCount));
            }
        }

        private void OnDestroy()
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }
        }

        private void RefreshDependencies()
        {
            strategyCamera = strategyCamera != null ? strategyCamera : Camera.main;
            population ??= FindAnyObjectByType<StrategyPopulationController>();
            campfire ??= FindAnyObjectByType<StrategyCampfireAnimator>();
            StrategyBuildPlacementController nextPlacement = FindAnyObjectByType<StrategyBuildPlacementController>();
            if (nextPlacement != placement)
            {
                if (placement != null)
                {
                    placement.BuildingCompleted -= HandleBuildingCompleted;
                }

                placement = nextPlacement;
                if (placement != null)
                {
                    placement.BuildingCompleted += HandleBuildingCompleted;
                }
            }
        }

        private void EnsureSources()
        {
            settlementDaySource ??= CreateLoop("Settlement Day Soundscape", StrategyProceduralSound.SettlementDay, StrategyAudioBus.Settlement, 0f);
            settlementNightSource ??= CreateLoop("Settlement Night Soundscape", StrategyProceduralSound.SettlementNight, StrategyAudioBus.Settlement, 0f);
            fireSource ??= CreateLoop("Campfire Soundscape", StrategyProceduralSound.Fire, StrategyAudioBus.Fire, 0.82f);
            winterSource ??= CreateLoop("Winter Soundscape", StrategyProceduralSound.WinterWind, StrategyAudioBus.Weather, 0f);
            fireSource.spatialBlend = 0.82f;
            fireSource.minDistance = 4f;
            fireSource.maxDistance = 36f;
        }

        private AudioSource CreateLoop(string sourceName, StrategyProceduralSound sound, StrategyAudioBus bus, float spatialBlend)
        {
            AudioSource source = StrategyAudioMixController.CreateRuntimeSource(transform, sourceName, bus);
            source.clip = StrategyProceduralAudioLibrary.Get(sound);
            source.loop = true;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 6f;
            source.maxDistance = 54f;
            source.priority = 185;
            source.volume = 0f;
            source.Play();
            return source;
        }

        private void UpdateSoundscape(float dt)
        {
            EnsureSources();
            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            StrategyWeatherController weather = StrategyWeatherController.Active;
            float rain = weather != null ? weather.RainIntensity : 0f;
            float snow = weather != null ? weather.SnowIntensity : 0f;
            float wind = weather != null ? weather.WindIntensity : 0f;
            float night = GetNightBlend(snapshot);
            int nearbyResidents = CountNearbyResidents();
            int nearbyBuildings = CountNearbyBuildings();
            float settlementPresence = Mathf.Clamp01((nearbyResidents + nearbyBuildings * 1.5f) / 18f);
            float workFactor = StrategyDayNightCycleController.IsSettlementWorkTime ? 1f : 0.34f;
            float weatherDuck = Mathf.Lerp(1f, 0.48f, Mathf.Max(rain, snow));
            float settlementBus = StrategyAudioMixController.GetVolume(StrategyAudioBus.Settlement);
            float dayTarget = settlementBus * 0.075f * settlementPresence * workFactor * weatherDuck * (1f - night * 0.82f);
            float nightTarget = settlementBus * 0.045f * settlementPresence * Mathf.Lerp(0.28f, 1f, night) * weatherDuck;
            Fade(settlementDaySource, dayTarget, dt);
            Fade(settlementNightSource, nightTarget, dt);

            float fireFactor = campfire != null ? campfire.LightIntensityFactor : 0f;
            if (campfire != null)
            {
                fireSource.transform.position = campfire.transform.position;
            }

            StrategyAudioWorldMix fireMix = StrategyAudioMixController.EvaluateWorld(
                campfire != null ? campfire.transform.position : Vector3.zero,
                StrategyAudioBus.Fire);
            Fade(fireSource, 0.12f * fireFactor * fireMix.VolumeMultiplier, dt);
            if (previousFireFactor < 0.22f && fireFactor >= 0.42f && campfire != null)
            {
                PlayTorchIgnite(campfire.transform.position);
            }

            previousFireFactor = fireFactor;
            float winterBlend = snapshot.Season == StrategySeason.Winter ? 1f : 0f;
            float winterTarget = StrategyAudioMixController.GetVolume(StrategyAudioBus.Weather)
                * 0.065f
                * winterBlend
                * Mathf.Clamp01(0.28f + snow * 0.52f + wind * 0.42f);
            Fade(winterSource, winterTarget, dt);
        }

        private void UpdateSpotEvents()
        {
            if (Time.timeScale <= 0f || !StrategyDayNightCycleController.IsSettlementWorkTime)
            {
                return;
            }

            spotTimer -= Time.unscaledDeltaTime;
            if (spotTimer > 0f)
            {
                return;
            }

            int nearbyResidents = CountNearbyResidents();
            spotTimer = Random.Range(3.8f, 7.5f) / Mathf.Clamp(nearbyResidents / 8f, 0.75f, 1.8f);
            if (nearbyResidents <= 0 || !TryPickAudibleSettlementPoint(out Vector3 point))
            {
                return;
            }

            StrategyAudioVoicePool.Play(
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.DistantWork),
                point,
                StrategyAudioBus.Settlement,
                0.085f,
                0.88f,
                1.08f,
                StrategyAudioPriority.Ambient,
                "settlement_spot",
                1.2f,
                2,
                0.72f,
                5f,
                42f);
        }

        private int CountNearbyResidents()
        {
            if (population == null || strategyCamera == null)
            {
                return 0;
            }

            Vector3 focus = strategyCamera.transform.position;
            focus.z = 0f;
            float radius = strategyCamera.orthographicSize * 1.65f;
            float radiusSqr = radius * radius;
            int count = 0;
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && !resident.IsSleepingInsideHome && (resident.transform.position - focus).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountNearbyBuildings()
        {
            if (strategyCamera == null)
            {
                return 0;
            }

            Vector3 focus = strategyCamera.transform.position;
            focus.z = 0f;
            float radiusSqr = Mathf.Pow(strategyCamera.orthographicSize * 1.85f, 2f);
            int count = 0;
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null && (building.transform.position - focus).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryPickAudibleSettlementPoint(out Vector3 point)
        {
            point = strategyCamera != null ? strategyCamera.transform.position : Vector3.zero;
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            if (buildings.Count <= 0)
            {
                return false;
            }

            int start = Random.Range(0, buildings.Count);
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[(start + i) % buildings.Count];
                if (building == null)
                {
                    continue;
                }

                StrategyAudioWorldMix mix = StrategyAudioMixController.EvaluateWorld(building.transform.position, StrategyAudioBus.Settlement);
                if (mix.FocusFactor > 0.05f)
                {
                    point = building.transform.position;
                    return true;
                }
            }

            return false;
        }

        private void HandleBuildingCompleted(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return;
            }

            StrategyAudioVoicePool.Play(
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.BuildComplete),
                building.transform.position,
                StrategyAudioBus.ImportantEvents,
                0.30f,
                0.96f,
                1.03f,
                StrategyAudioPriority.Critical,
                "building_complete",
                0.35f,
                2,
                0.76f,
                6f,
                60f);
        }

        public static void PlayTorchIgnite(Vector3 position)
        {
            StrategyAudioVoicePool.Play(
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.TorchIgnite),
                position,
                StrategyAudioBus.Fire,
                0.20f,
                0.94f,
                1.07f,
                StrategyAudioPriority.Normal,
                "fire_ignite",
                0.18f,
                3,
                0.74f,
                3f,
                34f);
        }

        public static void PlayResourceDrop(Vector3 position, bool heavy)
        {
            StrategyAudioVoicePool.Play(
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.ResourceDrop),
                position,
                StrategyAudioBus.Work,
                heavy ? 0.16f : 0.11f,
                heavy ? 0.82f : 0.94f,
                heavy ? 0.96f : 1.07f,
                StrategyAudioPriority.Normal,
                heavy ? "resource_drop_heavy" : "resource_drop_light",
                0.08f,
                3,
                0.66f,
                3.5f,
                28f);
        }

        public static void PlayWolfHowl(Vector3 position)
        {
            StrategyAudioVoicePool.Play(
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.WolfHowl),
                position,
                StrategyAudioBus.Wildlife,
                0.20f,
                0.94f,
                1.04f,
                StrategyAudioPriority.Important,
                "wolf_howl",
                WolfHowlCooldownSeconds,
                WolfHowlConcurrencyLimit,
                0.88f,
                8f,
                70f);
        }

        public static void PlayBurial(Vector3 position)
        {
            StrategyAudioVoicePool.Play(
                StrategyProceduralAudioLibrary.Get(StrategyProceduralSound.BurialDirt),
                position,
                StrategyAudioBus.ImportantEvents,
                0.18f,
                0.88f,
                1.02f,
                StrategyAudioPriority.Important,
                "burial_dirt",
                0.5f,
                2,
                0.72f,
                4f,
                44f);
        }

        private static float GetNightBlend(StrategyCalendarSnapshot snapshot)
        {
            if (snapshot.Phase == StrategyTimeOfDayPhase.Night)
            {
                return 1f;
            }

            if (snapshot.Phase == StrategyTimeOfDayPhase.Dusk)
            {
                return snapshot.PhaseProgress;
            }

            return snapshot.Phase == StrategyTimeOfDayPhase.Dawn ? 1f - snapshot.PhaseProgress : 0f;
        }

        private static void Fade(AudioSource source, float target, float dt)
        {
            if (source != null)
            {
                source.volume = Mathf.MoveTowards(source.volume, Mathf.Clamp01(target), FadeSpeed * dt);
            }
        }
    }
}
