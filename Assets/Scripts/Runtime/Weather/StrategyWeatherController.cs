using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWeatherController : MonoBehaviour
    {
        private const float AtmosphereTransitionSpeed = 0.24f;
        private const float WetnessRiseSpeed = 0.18f;
        private const float WetnessDrySpeed = 0.028f;

        private CityMapController map;
        private StrategyWindController wind;
        private StrategyWeatherKind currentWeather;
        private float stateTimer;
        private float rainIntensity;
        private float snowIntensity;
        private float cloudIntensity;
        private float fogIntensity;
        private float stormIntensity;
        private float windIntensity;
        private float wetnessIntensity;
        private bool configured;

        public static StrategyWeatherController Active { get; private set; }
        public StrategyWeatherKind CurrentWeather => currentWeather;
        public float RainIntensity => rainIntensity;
        public float SnowIntensity => snowIntensity;
        public float CloudIntensity => cloudIntensity;
        public float FogIntensity => fogIntensity;
        public float StormIntensity => stormIntensity;
        public float WindIntensity => windIntensity;
        public float WetnessIntensity => wetnessIntensity;
        public float HeavyRainIntensity => Mathf.Clamp01((rainIntensity - 0.55f) / 0.45f);
        public float HeavySnowIntensity => Mathf.Clamp01((snowIntensity - 0.55f) / 0.45f);
        public string WeatherName => currentWeather.ToString();

        public void Configure(CityMapController mapController, StrategyWindController windController)
        {
            Active = this;
            map = mapController;
            wind = windController;
            configured = map != null;
            currentWeather = Random.value < 0.72f ? StrategyWeatherKind.Clear : StrategyWeatherKind.Cloudy;
            stateTimer = GetDuration(currentWeather);
            ApplyProfile(GetProfile(currentWeather), true);
            ApplyWindInfluence();
            StrategyDebugLogger.Info(
                "Weather",
                "Configured",
                StrategyDebugLogger.F("state", WeatherName),
                StrategyDebugLogger.F("duration", stateTimer),
                StrategyDebugLogger.F("season", StrategyDayNightCycleController.CurrentCalendarSnapshot.SeasonLabel),
                StrategyDebugLogger.F("mapSeed", map != null ? map.ActiveSeed : 0));
        }

        public void ForceWeather(StrategyWeatherKind weatherKind)
        {
            if (!configured)
            {
                return;
            }

            SetWeather(weatherKind, true);
        }

        public void ForceWeatherSmooth(StrategyWeatherKind weatherKind)
        {
            if (!configured)
            {
                return;
            }

            SetWeather(weatherKind, false);
        }

        private void Awake()
        {
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            if (wind != null)
            {
                wind.SetWeatherInfluence(0f, 0f, 0f);
            }
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            float dt = Mathf.Max(0f, Time.deltaTime);
            stateTimer -= dt;
            if (stateTimer <= 0f)
            {
                SetWeather(PickNextWeather(), false);
            }

            ApplyProfile(GetProfile(currentWeather), false);
            ApplyWindInfluence();
        }

        private void SetWeather(StrategyWeatherKind weatherKind, bool forceInstant)
        {
            currentWeather = weatherKind;
            stateTimer = GetDuration(currentWeather);
            ApplyProfile(GetProfile(currentWeather), forceInstant);
            ApplyWindInfluence();
            StrategyDebugLogger.Info(
                "Weather",
                "StateChanged",
                StrategyDebugLogger.F("state", WeatherName),
                StrategyDebugLogger.F("duration", stateTimer),
                StrategyDebugLogger.F("season", StrategyDayNightCycleController.CurrentCalendarSnapshot.SeasonLabel),
                StrategyDebugLogger.F("rain", rainIntensity),
                StrategyDebugLogger.F("snow", snowIntensity),
                StrategyDebugLogger.F("cloud", cloudIntensity),
                StrategyDebugLogger.F("fog", fogIntensity),
                StrategyDebugLogger.F("storm", stormIntensity));
        }

        private void ApplyProfile(WeatherProfile profile, bool instant)
        {
            if (instant)
            {
                rainIntensity = profile.Rain;
                snowIntensity = profile.Snow;
                cloudIntensity = profile.Cloud;
                fogIntensity = profile.Fog;
                stormIntensity = profile.Storm;
                windIntensity = profile.Wind;
                wetnessIntensity = profile.Wetness;
                return;
            }

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            rainIntensity = Mathf.MoveTowards(rainIntensity, profile.Rain, AtmosphereTransitionSpeed * dt);
            snowIntensity = Mathf.MoveTowards(snowIntensity, profile.Snow, AtmosphereTransitionSpeed * dt);
            cloudIntensity = Mathf.MoveTowards(cloudIntensity, profile.Cloud, AtmosphereTransitionSpeed * dt);
            fogIntensity = Mathf.MoveTowards(fogIntensity, profile.Fog, AtmosphereTransitionSpeed * dt);
            stormIntensity = Mathf.MoveTowards(stormIntensity, profile.Storm, AtmosphereTransitionSpeed * dt);
            windIntensity = Mathf.MoveTowards(windIntensity, profile.Wind, AtmosphereTransitionSpeed * dt);
            float wetnessSpeed = profile.Wetness > wetnessIntensity ? WetnessRiseSpeed : WetnessDrySpeed;
            wetnessIntensity = Mathf.MoveTowards(wetnessIntensity, profile.Wetness, wetnessSpeed * dt);
        }

        private void ApplyWindInfluence()
        {
            if (wind == null)
            {
                return;
            }

            float mainBoost = windIntensity * 0.42f;
            float pulseBoost = windIntensity * 0.18f + stormIntensity * 0.32f + snowIntensity * 0.10f;
            float turbulenceBoost = windIntensity * 0.18f + rainIntensity * 0.10f + snowIntensity * 0.12f + stormIntensity * 0.36f;
            wind.SetWeatherInfluence(mainBoost, pulseBoost, turbulenceBoost);
        }

        private StrategyWeatherKind PickNextWeather()
        {
            bool severeWeather = currentWeather == StrategyWeatherKind.Storm
                || currentWeather == StrategyWeatherKind.Blizzard;
            float clearWeight = severeWeather ? 0.10f : 0.28f;
            float cloudyWeight = currentWeather == StrategyWeatherKind.Clear ? 0.34f : 0.26f;
            float lightRainWeight = currentWeather == StrategyWeatherKind.Cloudy ? 0.25f : 0.18f;
            float heavyRainWeight = currentWeather == StrategyWeatherKind.LightRain ? 0.16f : 0.08f;
            float fogWeight = currentWeather == StrategyWeatherKind.HeavyRain ? 0.14f : 0.09f;
            float stormWeight = currentWeather == StrategyWeatherKind.HeavyRain ? 0.10f : 0.04f;
            float snowWeight = currentWeather == StrategyWeatherKind.Cloudy
                || currentWeather == StrategyWeatherKind.Fog
                ? 0.22f
                : 0.12f;
            float blizzardWeight = currentWeather == StrategyWeatherKind.Snow ? 0.14f : 0.035f;
            ApplySeasonWeatherBias(
                StrategyDayNightCycleController.CurrentCalendarSnapshot.Season,
                ref clearWeight,
                ref cloudyWeight,
                ref lightRainWeight,
                ref heavyRainWeight,
                ref fogWeight,
                ref stormWeight,
                ref snowWeight,
                ref blizzardWeight);

            return PickWeighted(
                clearWeight,
                cloudyWeight,
                lightRainWeight,
                heavyRainWeight,
                fogWeight,
                stormWeight,
                snowWeight,
                blizzardWeight);
        }

        private static void ApplySeasonWeatherBias(
            StrategySeason season,
            ref float clearWeight,
            ref float cloudyWeight,
            ref float lightRainWeight,
            ref float heavyRainWeight,
            ref float fogWeight,
            ref float stormWeight,
            ref float snowWeight,
            ref float blizzardWeight)
        {
            switch (season)
            {
                case StrategySeason.Spring:
                    clearWeight *= 0.86f;
                    cloudyWeight *= 1.12f;
                    lightRainWeight *= 1.38f;
                    heavyRainWeight *= 1.24f;
                    fogWeight *= 0.96f;
                    stormWeight *= 0.92f;
                    snowWeight = 0f;
                    blizzardWeight = 0f;
                    break;
                case StrategySeason.Autumn:
                    clearWeight *= 0.74f;
                    cloudyWeight *= 1.26f;
                    lightRainWeight *= 1.06f;
                    heavyRainWeight *= 1.02f;
                    fogWeight *= 1.46f;
                    stormWeight *= 1.30f;
                    snowWeight = 0f;
                    blizzardWeight = 0f;
                    break;
                case StrategySeason.Winter:
                    clearWeight *= 0.72f;
                    cloudyWeight *= 1.28f;
                    lightRainWeight *= 0.14f;
                    heavyRainWeight *= 0.05f;
                    fogWeight *= 1.66f;
                    stormWeight *= 0.30f;
                    snowWeight *= 3.40f;
                    blizzardWeight *= 2.85f;
                    break;
                default:
                    clearWeight *= 1.30f;
                    cloudyWeight *= 1.05f;
                    lightRainWeight *= 0.76f;
                    heavyRainWeight *= 0.56f;
                    fogWeight *= 0.66f;
                    stormWeight *= 0.74f;
                    snowWeight = 0f;
                    blizzardWeight = 0f;
                    break;
            }
        }

        private StrategyWeatherKind PickWeighted(
            float clearWeight,
            float cloudyWeight,
            float lightRainWeight,
            float heavyRainWeight,
            float fogWeight,
            float stormWeight,
            float snowWeight,
            float blizzardWeight)
        {
            float total = 0f;
            total += currentWeather == StrategyWeatherKind.Clear ? 0f : clearWeight;
            total += currentWeather == StrategyWeatherKind.Cloudy ? 0f : cloudyWeight;
            total += currentWeather == StrategyWeatherKind.LightRain ? 0f : lightRainWeight;
            total += currentWeather == StrategyWeatherKind.HeavyRain ? 0f : heavyRainWeight;
            total += currentWeather == StrategyWeatherKind.Fog ? 0f : fogWeight;
            total += currentWeather == StrategyWeatherKind.Storm ? 0f : stormWeight;
            total += currentWeather == StrategyWeatherKind.Snow ? 0f : snowWeight;
            total += currentWeather == StrategyWeatherKind.Blizzard ? 0f : blizzardWeight;

            float roll = Random.value * Mathf.Max(0.001f, total);
            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.Clear ? 0f : clearWeight))
            {
                return StrategyWeatherKind.Clear;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.Cloudy ? 0f : cloudyWeight))
            {
                return StrategyWeatherKind.Cloudy;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.LightRain ? 0f : lightRainWeight))
            {
                return StrategyWeatherKind.LightRain;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.HeavyRain ? 0f : heavyRainWeight))
            {
                return StrategyWeatherKind.HeavyRain;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.Fog ? 0f : fogWeight))
            {
                return StrategyWeatherKind.Fog;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.Storm ? 0f : stormWeight))
            {
                return StrategyWeatherKind.Storm;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.Snow ? 0f : snowWeight))
            {
                return StrategyWeatherKind.Snow;
            }

            if (TryConsume(ref roll, currentWeather == StrategyWeatherKind.Blizzard ? 0f : blizzardWeight))
            {
                return StrategyWeatherKind.Blizzard;
            }

            return StrategyWeatherKind.Clear;
        }

        private static bool TryConsume(ref float roll, float weight)
        {
            if (weight <= 0f)
            {
                return false;
            }

            if (roll <= weight)
            {
                return true;
            }

            roll -= weight;
            return false;
        }

        private static float GetDuration(StrategyWeatherKind weatherKind)
        {
            switch (weatherKind)
            {
                case StrategyWeatherKind.Clear:
                    return Random.Range(90f, 165f);
                case StrategyWeatherKind.Cloudy:
                    return Random.Range(70f, 135f);
                case StrategyWeatherKind.LightRain:
                    return Random.Range(48f, 92f);
                case StrategyWeatherKind.HeavyRain:
                    return Random.Range(38f, 76f);
                case StrategyWeatherKind.Fog:
                    return Random.Range(44f, 88f);
                case StrategyWeatherKind.Storm:
                    return Random.Range(26f, 54f);
                case StrategyWeatherKind.Snow:
                    return Random.Range(56f, 112f);
                case StrategyWeatherKind.Blizzard:
                    return Random.Range(30f, 62f);
                default:
                    return Random.Range(70f, 120f);
            }
        }

        private static WeatherProfile GetProfile(StrategyWeatherKind weatherKind)
        {
            switch (weatherKind)
            {
                case StrategyWeatherKind.Cloudy:
                    return new WeatherProfile(0f, 0.58f, 0.04f, 0f, 0.32f, 0f);
                case StrategyWeatherKind.LightRain:
                    return new WeatherProfile(0.42f, 0.76f, 0.10f, 0f, 0.46f, 0.42f);
                case StrategyWeatherKind.HeavyRain:
                    return new WeatherProfile(0.82f, 0.92f, 0.16f, 0f, 0.66f, 0.80f);
                case StrategyWeatherKind.Fog:
                    return new WeatherProfile(0f, 0.44f, 0.72f, 0f, 0.16f, 0.18f);
                case StrategyWeatherKind.Storm:
                    return new WeatherProfile(1f, 1f, 0.18f, 1f, 1f, 1f);
                case StrategyWeatherKind.Snow:
                    return new WeatherProfile(0f, 0.86f, 0.12f, 0f, 0.48f, 0.06f, 0.62f);
                case StrategyWeatherKind.Blizzard:
                    return new WeatherProfile(0f, 1f, 0.30f, 0f, 1f, 0.04f, 1f);
                default:
                    return new WeatherProfile(0f, 0.08f, 0f, 0f, 0.18f, 0f);
            }
        }

        private readonly struct WeatherProfile
        {
            public WeatherProfile(
                float rain,
                float cloud,
                float fog,
                float storm,
                float wind,
                float wetness,
                float snow = 0f)
            {
                Rain = rain;
                Snow = snow;
                Cloud = cloud;
                Fog = fog;
                Storm = storm;
                Wind = wind;
                Wetness = wetness;
            }

            public float Rain { get; }
            public float Snow { get; }
            public float Cloud { get; }
            public float Fog { get; }
            public float Storm { get; }
            public float Wind { get; }
            public float Wetness { get; }
        }
    }
}
