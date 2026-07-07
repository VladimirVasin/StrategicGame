using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float NightTorchLightIntensity = 1.25f;
        private const float NightTorchLightRadius = 5.45f;
        private const float NightTorchMaskRadius = 5.85f;
        private const float NightTorchMaskStrength = 0.88f;

        private static readonly List<StrategyResidentAgent> ActiveNightTorchLights = new();

        private Light2D nightTorchPointLight;
        private SpriteRenderer nightTorchGlowRenderer;
        private SpriteRenderer nightTorchCoreRenderer;
        private float nightTorchFlickerSeed;
        private float nightTorchFlickerTimeOffset;
        private float nightTorchFlickerFastSpeed = 5.2f;
        private float nightTorchFlickerSlowSpeed = 1.15f;
        private float nightTorchFlickerDepth = 0.8f;
        private int nightTorchFlickerProfileKey;
        private bool nightTorchLightActive;

        internal static int ActiveNightTorchLightCount => ActiveNightTorchLights.Count;

        internal static bool TryGetActiveNightTorchLight(
            int index,
            out Vector3 world,
            out float radius,
            out float strength,
            out float edgeFlicker)
        {
            world = Vector3.zero;
            radius = 0f;
            strength = 0f;
            edgeFlicker = 1f;
            if (index < 0 || index >= ActiveNightTorchLights.Count)
            {
                return false;
            }

            StrategyResidentAgent resident = ActiveNightTorchLights[index];
            return resident != null && resident.TryGetNightTorchMaskLight(out world, out radius, out strength, out edgeFlicker);
        }

        private void EnableNightTorchLight()
        {
            if (nightTorchLightActive)
            {
                UpdateNightTorchLight();
                return;
            }

            nightTorchLightActive = true;
            if (!ActiveNightTorchLights.Contains(this))
            {
                ActiveNightTorchLights.Add(this);
            }

            ConfigureNightTorchFlickerProfile();
            UpdateNightTorchLight();
        }

        private void DisableNightTorchLight()
        {
            nightTorchLightActive = false;
            ActiveNightTorchLights.Remove(this);
            if (nightTorchPointLight != null)
            {
                nightTorchPointLight.enabled = false;
            }

            if (nightTorchGlowRenderer != null)
            {
                nightTorchGlowRenderer.enabled = false;
            }

            if (nightTorchCoreRenderer != null)
            {
                nightTorchCoreRenderer.enabled = false;
            }
        }

        private void UpdateNightTorchLight()
        {
            if (!nightTorchLightActive)
            {
                return;
            }

            if (!ShouldKeepNightTorchLightActive() || hiddenInsideHome)
            {
                DisableNightTorchLight();
                return;
            }

            bool usePointLight = IsNightLightActivity(activity);
            EnsureNightTorchLightObjects(usePointLight);
            Vector3 world = GetNightTorchLightWorld();
            float flicker = GetNightTorchFlicker();
            float radius = NightTorchLightRadius;
            float intensity = NightTorchLightIntensity;
            Color color = Color.Lerp(
                new Color(1f, 0.50f, 0.18f, 1f),
                new Color(1f, 0.78f, 0.34f, 1f),
                flicker);

            if (nightTorchPointLight != null)
            {
                nightTorchPointLight.enabled = usePointLight;
                if (usePointLight)
                {
                    nightTorchPointLight.transform.position = world;
                    nightTorchPointLight.color = color;
                    nightTorchPointLight.intensity = intensity;
                    nightTorchPointLight.pointLightOuterRadius = radius;
                    nightTorchPointLight.pointLightInnerRadius = Mathf.Max(0.10f, radius * 0.22f);
                }
            }

            float glowFlicker = Mathf.Lerp(0.94f, 1.08f, flicker);
            float glowAlpha = (usePointLight ? intensity * 0.22f : intensity * 0.18f) * glowFlicker;
            float coreAlpha = usePointLight ? intensity * 0.55f : intensity * 0.48f;
            float glowScale = usePointLight ? 1f : 0.82f;
            ApplyNightTorchRenderer(
                nightTorchGlowRenderer,
                world,
                color,
                glowAlpha,
                radius * 0.86f * glowScale * Mathf.Lerp(0.97f, 1.05f, flicker),
                radius * 0.50f * glowScale * Mathf.Lerp(0.97f, 1.05f, flicker),
                20);
            ApplyNightTorchRenderer(nightTorchCoreRenderer, world, color, coreAlpha, 0.34f * glowScale, 0.26f * glowScale, 21);
        }

        private bool TryGetNightTorchMaskLight(
            out Vector3 world,
            out float radius,
            out float strength,
            out float edgeFlicker)
        {
            world = GetNightTorchLightWorld();
            radius = 0f;
            strength = 0f;
            edgeFlicker = 1f;
            if (!nightTorchLightActive || !ShouldKeepNightTorchLightActive() || hiddenInsideHome)
            {
                return false;
            }

            radius = NightTorchMaskRadius;
            strength = NightTorchMaskStrength;
            edgeFlicker = GetNightTorchFlicker();
            return true;
        }

        private void EnsureNightTorchLightObjects(bool includePointLight)
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            if (includePointLight && nightTorchPointLight == null)
            {
                GameObject lightObject = new GameObject("Resident Night Torch 2D Light");
                lightObject.transform.SetParent(transform, false);
                nightTorchPointLight = lightObject.AddComponent<Light2D>();
                nightTorchPointLight.lightType = Light2D.LightType.Point;
                nightTorchPointLight.falloffIntensity = 0.62f;
                nightTorchPointLight.shadowsEnabled = false;
                nightTorchPointLight.volumetricEnabled = false;
                nightTorchPointLight.blendStyleIndex = 0;
            }

            nightTorchGlowRenderer ??= CreateNightTorchRenderer(
                "Resident Night Torch Glow",
                StrategyCinematicVisualSprites.GetGlowSprite());
            nightTorchCoreRenderer ??= CreateNightTorchRenderer(
                "Resident Night Torch Core",
                StrategyCinematicVisualSprites.GetLampCoreSprite());
        }

        private SpriteRenderer CreateNightTorchRenderer(string objectName, Sprite sprite)
        {
            GameObject rendererObject = new GameObject(objectName);
            rendererObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.clear;
            renderer.enabled = false;
            return renderer;
        }

        private void ApplyNightTorchRenderer(
            SpriteRenderer renderer,
            Vector3 world,
            Color color,
            float alpha,
            float scaleX,
            float scaleY,
            int orderOffset)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = world;
            renderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            renderer.sortingOrder = StrategyWorldSorting.ForPosition(world, orderOffset);
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            renderer.enabled = alpha > 0.01f;
        }

        private Vector3 GetNightTorchLightWorld()
        {
            float side = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
            float reach = activity == ResidentActivity.LightingNightLight ? 0.30f : 0.20f;
            float height = activity == ResidentActivity.LightingNightLight ? 0.48f : 0.40f;
            return transform.position + new Vector3(side * reach, height, -0.04f);
        }

        private float GetNightTorchFlicker()
        {
            float t = Time.unscaledTime + nightTorchFlickerTimeOffset;
            float fast = Mathf.PerlinNoise(
                nightTorchFlickerSeed + t * nightTorchFlickerFastSpeed,
                nightTorchFlickerSeed * 0.41f + t * 0.33f);
            float slow = Mathf.PerlinNoise(
                nightTorchFlickerSeed + 53.1f + t * 0.24f,
                nightTorchFlickerSeed * 0.19f + t * nightTorchFlickerSlowSpeed);
            float wave = fast * 0.64f + slow * 0.36f;
            float softened = Mathf.Lerp(0.5f, wave, nightTorchFlickerDepth);
            return Mathf.Lerp(0.88f, 1.14f, softened);
        }

        private void ConfigureNightTorchFlickerProfile()
        {
            int sourceKey = residentId > 0
                ? residentId
                : HashNightTorchFlickerInts(
                    Mathf.RoundToInt(transform.position.x * 100f),
                    Mathf.RoundToInt(transform.position.y * 100f),
                    VisualVariant,
                    (int)gender ^ ((int)lifeStage << 8));
            int profileKey = HashNightTorchFlickerInts(sourceKey, VisualVariant, (int)gender, (int)lifeStage);
            if (nightTorchFlickerProfileKey == profileKey && nightTorchFlickerSeed > 0.001f)
            {
                return;
            }

            nightTorchFlickerProfileKey = profileKey;
            float a = GetNightTorchHashUnit(profileKey, 0x9E3779B9u);
            float b = GetNightTorchHashUnit(profileKey, 0x85EBCA6Bu);
            float c = GetNightTorchHashUnit(profileKey, 0xC2B2AE35u);
            float d = GetNightTorchHashUnit(profileKey, 0x27D4EB2Fu);
            float e = GetNightTorchHashUnit(profileKey, 0x165667B1u);

            nightTorchFlickerSeed = 1f + a * 997f;
            nightTorchFlickerTimeOffset = b * 47f;
            nightTorchFlickerFastSpeed = Mathf.Lerp(3.25f, 6.20f, c);
            nightTorchFlickerSlowSpeed = Mathf.Lerp(0.58f, 1.55f, d);
            nightTorchFlickerDepth = Mathf.Lerp(0.48f, 0.78f, e);
        }

        private static int HashNightTorchFlickerInts(int a, int b, int c, int d)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)a) * 16777619u;
                hash = (hash ^ (uint)b) * 16777619u;
                hash = (hash ^ (uint)c) * 16777619u;
                hash = (hash ^ (uint)d) * 16777619u;
                return (int)(hash & 0x7FFFFFFFu);
            }
        }

        private static float GetNightTorchHashUnit(int key, uint salt)
        {
            unchecked
            {
                uint hash = ((uint)key ^ salt) * 1664525u + 1013904223u;
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private void OnDisable()
        {
            DisableNightTorchLight();
        }

        private void OnDestroy()
        {
            DisableNightTorchLight();
        }
    }
}
