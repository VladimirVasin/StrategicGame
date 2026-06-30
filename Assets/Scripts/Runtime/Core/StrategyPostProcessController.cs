using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyPostProcessController : MonoBehaviour
    {
        private const string ProfileName = "Strategy Runtime Post Process Profile";
        private const float VolumePriority = 20f;

        private Camera strategyCamera;
        private StrategyDayNightCycleController dayNight;
        private StrategyWeatherController weather;
        private Volume volume;
        private VolumeProfile profile;
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;
        private Vignette vignette;
        private bool ownsProfile;
        private bool configured;

        public void Configure(
            Camera camera,
            StrategyDayNightCycleController dayNightController,
            StrategyWeatherController weatherController)
        {
            strategyCamera = camera;
            dayNight = dayNightController;
            weather = weatherController;
            configured = strategyCamera != null;

            EnsureCameraPostProcessing();
            EnsureVolume();
            ApplyPostProcess();

            StrategyDebugLogger.Info(
                "PostProcess",
                "Configured",
                StrategyDebugLogger.F("camera", strategyCamera != null ? strategyCamera.name : "none"),
                StrategyDebugLogger.F("volumePriority", VolumePriority));
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            ApplyPostProcess();
        }

        private void OnDestroy()
        {
            if (ownsProfile && profile != null)
            {
                Destroy(profile);
            }
        }

        private void EnsureCameraPostProcessing()
        {
            if (strategyCamera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData = strategyCamera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.volumeTrigger = strategyCamera.transform;
            int mask = cameraData.volumeLayerMask.value;
            mask |= 1 << gameObject.layer;
            cameraData.volumeLayerMask = mask;
        }

        private void EnsureVolume()
        {
            if (volume == null && !TryGetComponent(out volume))
            {
                volume = gameObject.AddComponent<Volume>();
            }

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = ProfileName;
                ownsProfile = true;
            }

            volume.isGlobal = true;
            volume.priority = VolumePriority;
            volume.weight = 1f;
            volume.profile = profile;

            colorAdjustments = profile.TryGet(out ColorAdjustments existingColor)
                ? existingColor
                : profile.Add<ColorAdjustments>(true);
            bloom = profile.TryGet(out Bloom existingBloom)
                ? existingBloom
                : profile.Add<Bloom>(true);
            vignette = profile.TryGet(out Vignette existingVignette)
                ? existingVignette
                : profile.Add<Vignette>(true);

            ConfigureStaticOverrides();
        }

        private void ConfigureStaticOverrides()
        {
            colorAdjustments.active = true;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.saturation.overrideState = true;

            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.intensity.overrideState = true;
            bloom.scatter.overrideState = true;
            bloom.tint.overrideState = true;
            bloom.highQualityFiltering.overrideState = true;
            bloom.downscale.overrideState = true;
            bloom.highQualityFiltering.value = false;
            bloom.downscale.value = BloomDownscaleMode.Quarter;
            bloom.scatter.value = 0.46f;
            bloom.tint.value = new Color(1f, 0.94f, 0.82f, 1f);

            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.center.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.rounded.overrideState = true;
            vignette.color.value = new Color(0.015f, 0.020f, 0.035f, 1f);
            vignette.center.value = new Vector2(0.5f, 0.5f);
            vignette.smoothness.value = 0.58f;
            vignette.rounded.value = false;
        }

        private void ApplyPostProcess()
        {
            if (colorAdjustments == null || bloom == null || vignette == null)
            {
                return;
            }

            float phase = dayNight != null ? dayNight.DayPhase : StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            float rain = weather != null ? weather.RainIntensity : 0f;
            float cloud = weather != null ? weather.CloudIntensity : 0f;
            float fog = weather != null ? weather.FogIntensity : 0f;
            float storm = weather != null ? weather.StormIntensity : 0f;
            float wetness = weather != null ? weather.WetnessIntensity : 0f;
            float heavyRain = weather != null ? weather.HeavyRainIntensity : 0f;

            Color tint = Color.white;
            tint = BlendTint(tint, new Color(1f, 0.88f, 0.68f, 1f), warm * 0.50f);
            tint = BlendTint(tint, new Color(0.68f, 0.78f, 1f, 1f), night * 0.50f);
            tint = BlendTint(tint, new Color(0.86f, 0.92f, 0.98f, 1f), Mathf.Max(cloud * 0.12f, rain * 0.24f));
            tint = BlendTint(tint, new Color(0.88f, 0.91f, 0.90f, 1f), fog * 0.35f);
            tint = BlendTint(tint, new Color(0.72f, 0.78f, 0.88f, 1f), storm * 0.30f);

            colorAdjustments.colorFilter.value = tint;
            colorAdjustments.postExposure.value = Mathf.Clamp(
                warm * 0.035f + fog * 0.018f - night * 0.105f - rain * 0.030f - storm * 0.055f,
                -0.18f,
                0.04f);
            colorAdjustments.contrast.value = Mathf.Clamp(
                2f + warm * 2f - cloud * 3f - rain * 7f - fog * 15f - storm * 7f - night * 6f,
                -22f,
                8f);
            colorAdjustments.saturation.value = Mathf.Clamp(
                3f + warm * 4f - cloud * 4f - rain * 11f - fog * 18f - storm * 8f - night * 11f,
                -24f,
                9f);

            float bloomMood = Mathf.Max(night * 0.72f, wetness * 0.38f);
            bloomMood = Mathf.Max(bloomMood, warm * 0.28f);
            bloomMood = Mathf.Max(bloomMood, storm * 0.32f);
            bloom.threshold.value = Mathf.Lerp(0.94f, 0.86f, bloomMood);
            bloom.intensity.value = Mathf.Clamp(0.018f + bloomMood * 0.105f, 0f, 0.135f);

            vignette.intensity.value = Mathf.Clamp(
                0.026f + night * 0.135f + heavyRain * 0.035f + fog * 0.035f + storm * 0.060f,
                0f,
                0.25f);
        }

        private static Color BlendTint(Color from, Color to, float amount)
        {
            return Color.Lerp(from, to, Mathf.Clamp01(amount));
        }
    }
}
