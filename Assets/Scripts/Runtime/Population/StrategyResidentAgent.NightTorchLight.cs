using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float NightTorchLightIntensity = 1.05f;
        private const float NightTorchLightRadius = 4.85f;
        private const float NightTorchMaskRadius = 5.10f;
        private const float NightTorchMaskStrength = 0.76f;

        private static readonly List<StrategyResidentAgent> ActiveNightTorchLights = new();

        private Light2D nightTorchPointLight;
        private SpriteRenderer nightTorchGlowRenderer;
        private SpriteRenderer nightTorchCoreRenderer;
        private float nightTorchFlickerSeed;
        private bool nightTorchLightActive;

        internal static int ActiveNightTorchLightCount => ActiveNightTorchLights.Count;

        internal static bool TryGetActiveNightTorchLight(
            int index,
            out Vector3 world,
            out float radius,
            out float strength)
        {
            world = Vector3.zero;
            radius = 0f;
            strength = 0f;
            if (index < 0 || index >= ActiveNightTorchLights.Count)
            {
                return false;
            }

            StrategyResidentAgent resident = ActiveNightTorchLights[index];
            return resident != null && resident.TryGetNightTorchMaskLight(out world, out radius, out strength);
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

            if (nightTorchFlickerSeed <= 0.001f)
            {
                nightTorchFlickerSeed = Random.Range(0f, 1000f);
            }

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
            float radius = NightTorchLightRadius * Mathf.Lerp(0.94f, 1.12f, flicker);
            float intensity = NightTorchLightIntensity * Mathf.Lerp(0.92f, 1.18f, flicker);
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

            float glowAlpha = usePointLight ? intensity * 0.22f : intensity * 0.18f;
            float coreAlpha = usePointLight ? intensity * 0.55f : intensity * 0.48f;
            float glowScale = usePointLight ? 1f : 0.82f;
            ApplyNightTorchRenderer(nightTorchGlowRenderer, world, color, glowAlpha, radius * 0.86f * glowScale, radius * 0.50f * glowScale, 20);
            ApplyNightTorchRenderer(nightTorchCoreRenderer, world, color, coreAlpha, 0.34f * glowScale, 0.26f * glowScale, 21);
        }

        private bool TryGetNightTorchMaskLight(out Vector3 world, out float radius, out float strength)
        {
            world = GetNightTorchLightWorld();
            radius = 0f;
            strength = 0f;
            if (!nightTorchLightActive || !ShouldKeepNightTorchLightActive() || hiddenInsideHome)
            {
                return false;
            }

            float flicker = GetNightTorchFlicker();
            radius = NightTorchMaskRadius * Mathf.Lerp(0.95f, 1.10f, flicker);
            strength = Mathf.Clamp01(NightTorchMaskStrength * Mathf.Lerp(0.92f, 1.16f, flicker));
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
            float t = Time.unscaledTime;
            float fast = Mathf.PerlinNoise(nightTorchFlickerSeed, t * 5.2f);
            float slow = Mathf.PerlinNoise(nightTorchFlickerSeed + 53.1f, t * 1.15f);
            return Mathf.Lerp(0.76f, 1.22f, fast * 0.70f + slow * 0.30f);
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
