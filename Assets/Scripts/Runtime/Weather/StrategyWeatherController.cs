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
        private float cloudIntensity;
        private float fogIntensity;
        private float stormIntensity;
        private float windIntensity;
        private float wetnessIntensity;
        private bool configured;

        public static StrategyWeatherController Active { get; private set; }
        public StrategyWeatherKind CurrentWeather => currentWeather;
        public float RainIntensity => rainIntensity;
        public float CloudIntensity => cloudIntensity;
        public float FogIntensity => fogIntensity;
        public float StormIntensity => stormIntensity;
        public float WindIntensity => windIntensity;
        public float WetnessIntensity => wetnessIntensity;
        public float HeavyRainIntensity => Mathf.Clamp01((rainIntensity - 0.55f) / 0.45f);
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
                StrategyDebugLogger.F("rain", rainIntensity),
                StrategyDebugLogger.F("cloud", cloudIntensity),
                StrategyDebugLogger.F("fog", fogIntensity),
                StrategyDebugLogger.F("storm", stormIntensity));
        }

        private void ApplyProfile(WeatherProfile profile, bool instant)
        {
            if (instant)
            {
                rainIntensity = profile.Rain;
                cloudIntensity = profile.Cloud;
                fogIntensity = profile.Fog;
                stormIntensity = profile.Storm;
                windIntensity = profile.Wind;
                wetnessIntensity = profile.Wetness;
                return;
            }

            float dt = Mathf.Max(0.001f, Time.deltaTime);
            rainIntensity = Mathf.MoveTowards(rainIntensity, profile.Rain, AtmosphereTransitionSpeed * dt);
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
            float pulseBoost = windIntensity * 0.18f + stormIntensity * 0.32f;
            float turbulenceBoost = windIntensity * 0.18f + rainIntensity * 0.10f + stormIntensity * 0.36f;
            wind.SetWeatherInfluence(mainBoost, pulseBoost, turbulenceBoost);
        }

        private StrategyWeatherKind PickNextWeather()
        {
            float clearWeight = currentWeather == StrategyWeatherKind.Storm ? 0.10f : 0.28f;
            float cloudyWeight = currentWeather == StrategyWeatherKind.Clear ? 0.34f : 0.26f;
            float lightRainWeight = currentWeather == StrategyWeatherKind.Cloudy ? 0.25f : 0.18f;
            float heavyRainWeight = currentWeather == StrategyWeatherKind.LightRain ? 0.16f : 0.08f;
            float fogWeight = currentWeather == StrategyWeatherKind.HeavyRain ? 0.14f : 0.09f;
            float stormWeight = currentWeather == StrategyWeatherKind.HeavyRain ? 0.10f : 0.04f;

            return PickWeighted(
                clearWeight,
                cloudyWeight,
                lightRainWeight,
                heavyRainWeight,
                fogWeight,
                stormWeight);
        }

        private StrategyWeatherKind PickWeighted(
            float clearWeight,
            float cloudyWeight,
            float lightRainWeight,
            float heavyRainWeight,
            float fogWeight,
            float stormWeight)
        {
            float total = 0f;
            total += currentWeather == StrategyWeatherKind.Clear ? 0f : clearWeight;
            total += currentWeather == StrategyWeatherKind.Cloudy ? 0f : cloudyWeight;
            total += currentWeather == StrategyWeatherKind.LightRain ? 0f : lightRainWeight;
            total += currentWeather == StrategyWeatherKind.HeavyRain ? 0f : heavyRainWeight;
            total += currentWeather == StrategyWeatherKind.Fog ? 0f : fogWeight;
            total += currentWeather == StrategyWeatherKind.Storm ? 0f : stormWeight;

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

            return StrategyWeatherKind.Storm;
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
                default:
                    return new WeatherProfile(0f, 0.08f, 0f, 0f, 0.18f, 0f);
            }
        }

        private readonly struct WeatherProfile
        {
            public WeatherProfile(float rain, float cloud, float fog, float storm, float wind, float wetness)
            {
                Rain = rain;
                Cloud = cloud;
                Fog = fog;
                Storm = storm;
                Wind = wind;
                Wetness = wetness;
            }

            public float Rain { get; }
            public float Cloud { get; }
            public float Fog { get; }
            public float Storm { get; }
            public float Wind { get; }
            public float Wetness { get; }
        }
    }
}
