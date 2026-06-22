using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    internal sealed partial class StrategyCinematicLightEmitter : MonoBehaviour
    {
        private const float VisualUpdateInterval = 0.105f;
        private const float LocalLightStrengthMultiplier = 2f;
        private const float LocalLightRadiusMultiplier = 2f;

        private StrategyPlacedBuilding building;
        private StrategyCampfireAnimator campfire;
        private StrategyCinematicLightKind kind;
        private Light2D pointLight;
        private SpriteRenderer glowRenderer;
        private SpriteRenderer coreRenderer;
        private SpriteRenderer interiorRenderer;
        private float flickerSeed;
        private float effectTimer;
        private float visualTimer;
        private float visualElapsed;
        private bool lodVisible = true;
        private bool lodPointLight = true;
        private bool visualsDisabled;
        private bool configured;

        public bool IsCinematicVisible => lodVisible;
        public bool CinematicPointLightAllowed => lodPointLight;

        public void ConfigureForBuilding(StrategyPlacedBuilding owner)
        {
            if (configured && building == owner && owner != null)
            {
                return;
            }

            building = owner;
            campfire = null;
            kind = GetKind(owner != null ? owner.Tool : StrategyBuildTool.None);
            if (flickerSeed <= 0.001f)
            {
                flickerSeed = Random.Range(0f, 1000f);
                visualTimer = Random.Range(0f, VisualUpdateInterval);
            }

            configured = owner != null;
            EnsureVisuals();
        }

        public void ConfigureForCampfire(StrategyCampfireAnimator owner)
        {
            if (configured && campfire == owner && owner != null)
            {
                return;
            }

            campfire = owner;
            building = null;
            kind = StrategyCinematicLightKind.Campfire;
            if (flickerSeed <= 0.001f)
            {
                flickerSeed = Random.Range(0f, 1000f);
                visualTimer = Random.Range(0f, VisualUpdateInterval);
            }

            configured = owner != null;
            EnsureVisuals();
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            float dt = Mathf.Max(0f, Time.deltaTime);
            visualElapsed += dt;
            if (!lodVisible)
            {
                DisableVisuals();
                return;
            }

            visualTimer -= dt;
            if (visualTimer > 0f)
            {
                return;
            }

            float elapsed = Mathf.Max(visualElapsed, VisualUpdateInterval);
            visualElapsed = 0f;
            visualTimer = GetNextVisualInterval();
            UpdateAnchor();
            ApplyLighting();
            TrySpawnAmbientParticles(elapsed);
            visualsDisabled = false;
        }

        public bool RefreshCinematicVisibility(Rect visibleWorldRect)
        {
            Vector3 anchor = GetLightSourceWorld();
            bool visible = visibleWorldRect.Contains(new Vector2(anchor.x, anchor.y));
            SetCinematicLod(visible, false);
            return visible;
        }

        public void EnableCinematicPointLight()
        {
            lodPointLight = true;
        }

        public float GetCinematicLightScore(Vector3 cameraCenter)
        {
            Vector3 anchor = GetLightSourceWorld();
            float distanceSqr = (anchor - cameraCenter).sqrMagnitude;
            float priority = kind switch
            {
                StrategyCinematicLightKind.Campfire => 125f,
                StrategyCinematicLightKind.Mine => 92f,
                StrategyCinematicLightKind.CoalPit => 86f,
                StrategyCinematicLightKind.Kiln => 84f,
                StrategyCinematicLightKind.Forge => 88f,
                StrategyCinematicLightKind.House => building != null && building.ResidentCount > 0 ? 82f : 48f,
                StrategyCinematicLightKind.Bridge => 68f,
                StrategyCinematicLightKind.Granary => 66f,
                StrategyCinematicLightKind.Storage => 58f,
                _ => 42f
            };

            return priority - distanceSqr * 0.07f;
        }

        private void SetCinematicLod(bool visible, bool allowPointLight)
        {
            lodVisible = visible;
            lodPointLight = visible && allowPointLight;
            if (!lodVisible || !lodPointLight)
            {
                if (pointLight != null)
                {
                    pointLight.enabled = false;
                }
            }

            if (!lodVisible)
            {
                DisableVisuals();
            }
        }

        private void EnsureVisuals()
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            glowRenderer ??= CreateRenderer("Cinematic Pixel Glow", StrategyCinematicVisualSprites.GetGlowSprite(), 18);
            coreRenderer ??= CreateRenderer("Cinematic Emissive Core", GetCoreSprite(), 19);
            EnsureTorchVisuals();
            if (kind == StrategyCinematicLightKind.House)
            {
                interiorRenderer ??= CreateRenderer(
                    "Cinematic Interior Shadow",
                    StrategyCinematicVisualSprites.GetInteriorShadowSprite(),
                    20);
            }
        }

        private SpriteRenderer CreateRenderer(string objectName, Sprite sprite, int orderOffset)
        {
            GameObject rendererObject = new GameObject(objectName);
            rendererObject.transform.SetParent(transform, false);
            SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = StrategyWorldSorting.ForPosition(transform.position, orderOffset);
            renderer.color = Color.clear;
            return renderer;
        }

        private void UpdateAnchor()
        {
            Vector3 world = GetAnchorWorld();
            Vector3 lightWorld = GetLightSourceWorld();
            Transform lightTransform = pointLight != null ? pointLight.transform : null;
            if (lightTransform != null)
            {
                lightTransform.position = lightWorld;
            }

            PositionRenderer(glowRenderer, lightWorld, 18);
            PositionRenderer(coreRenderer, GetCoreVisualWorld(world), 19);
            PositionRenderer(interiorRenderer, world + GetInteriorOffset(), 20);
            UpdateTorchAnchor();
        }

        private void PositionRenderer(SpriteRenderer renderer, Vector3 world, int offset)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = world;
            renderer.sortingOrder = StrategyWorldSorting.ForPosition(world, offset);
        }

        private void ApplyLighting()
        {
            float phase = StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            StrategyWeatherController activeWeather = StrategyWeatherController.Active;
            float rain = activeWeather != null ? activeWeather.RainIntensity : 0f;
            float fog = activeWeather != null ? activeWeather.FogIntensity : 0f;
            float storm = activeWeather != null ? activeWeather.StormIntensity : 0f;
            float wet = activeWeather != null ? activeWeather.WetnessIntensity : 0f;
            float activity = GetActivityFactor(night, warm, rain, fog, storm);
            float lightState = GetLightStateFactor() * GetDarkTimeLightFactor();
            float flicker = GetFlicker();
            Color color = GetColor(wet, storm);
            float intensity = GetBaseIntensity() * LocalLightStrengthMultiplier * activity * lightState * flicker;
            float radius = GetBaseRadius() * LocalLightRadiusMultiplier * Mathf.Lerp(0.92f, 1.12f, activity);

            if (lodPointLight)
            {
                EnsurePointLight();
            }

            if (pointLight != null)
            {
                pointLight.enabled = lodPointLight && intensity > 0.015f;
                pointLight.color = color;
                pointLight.intensity = intensity;
                pointLight.pointLightOuterRadius = radius;
                pointLight.pointLightInnerRadius = Mathf.Max(0.05f, radius * 0.18f);
            }

            ApplyRenderer(glowRenderer, color, intensity * GetGlowAlpha(), GetGlowScale(radius));
            ApplyRenderer(coreRenderer, color, intensity * GetCoreAlpha(), GetCoreScale());
            ApplyInteriorShadow(activity, night);
            ApplyTorchLighting(color, activity, flicker);
        }

        private void EnsurePointLight()
        {
            if (pointLight != null || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            GameObject lightObject = new GameObject("Cinematic 2D Light");
            lightObject.transform.SetParent(transform, false);
            pointLight = lightObject.AddComponent<Light2D>();
            pointLight.lightType = Light2D.LightType.Point;
            pointLight.falloffIntensity = 0.74f;
            pointLight.shadowsEnabled = false;
            pointLight.volumetricEnabled = false;
            pointLight.blendStyleIndex = 0;
        }

        private void ApplyRenderer(SpriteRenderer renderer, Color color, float alpha, Vector2 scale)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = alpha > 0.01f;
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
            renderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        private void ApplyInteriorShadow(float activity, float night)
        {
            if (interiorRenderer == null)
            {
                return;
            }

            bool occupied = building != null && building.ResidentCount > 0;
            float pulse = Mathf.Sin(Time.timeSinceLevelLoad * 0.85f + flickerSeed) * 0.5f + 0.5f;
            float alpha = occupied ? activity * night * pulse * 0.34f : 0f;
            interiorRenderer.enabled = alpha > 0.025f;
            interiorRenderer.color = new Color(0.05f, 0.025f, 0.01f, alpha);
            interiorRenderer.transform.localScale = new Vector3(0.62f + pulse * 0.10f, 0.80f, 1f);
        }

        private void TrySpawnAmbientParticles(float elapsed)
        {
            if (!lodVisible || (!lodPointLight && kind != StrategyCinematicLightKind.Campfire))
            {
                return;
            }

            if (kind != StrategyCinematicLightKind.Campfire
                && kind != StrategyCinematicLightKind.CoalPit
                && kind != StrategyCinematicLightKind.Mine
                && kind != StrategyCinematicLightKind.Kiln
                && kind != StrategyCinematicLightKind.Forge)
            {
                return;
            }

            effectTimer -= elapsed;
            if (effectTimer > 0f)
            {
                return;
            }

            if (kind == StrategyCinematicLightKind.Campfire && GetLightStateFactor() <= 0.08f)
            {
                return;
            }

            if (kind != StrategyCinematicLightKind.Campfire && GetDarkTimeLightFactor() <= 0.08f)
            {
                return;
            }

            effectTimer = Random.Range(0.55f, 1.45f);
            StrategyWorldEffectKind effect = kind == StrategyCinematicLightKind.Mine
                ? StrategyWorldEffectKind.Dust
                : StrategyWorldEffectKind.IronSparks;
            StrategyWorldEffectAnimator.Spawn(
                effect,
                GetAnchorWorld() + new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.05f, 0.14f), 0f),
                StrategyWorldSorting.ForPosition(transform.position, 24),
                Mathf.RoundToInt(flickerSeed + Time.frameCount),
                kind == StrategyCinematicLightKind.Campfire ? 0.82f : 0.58f);
        }

        private void DisableVisuals()
        {
            if (visualsDisabled)
            {
                return;
            }

            if (pointLight != null)
            {
                pointLight.enabled = false;
            }

            SetRendererEnabled(glowRenderer, false);
            SetRendererEnabled(coreRenderer, false);
            SetRendererEnabled(interiorRenderer, false);
            DisableTorchVisuals();
            visualsDisabled = true;
        }

        private static void SetRendererEnabled(SpriteRenderer renderer, bool enabled)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        private float GetNextVisualInterval()
        {
            float phase = Mathf.Repeat(flickerSeed * 0.017f, 0.045f);
            return VisualUpdateInterval + phase;
        }

        private Vector3 GetAnchorWorld()
        {
            if (building != null)
            {
                Bounds bounds = building.FootprintBounds;
                return building.Tool switch
                {
                    StrategyBuildTool.House => new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.55f, -0.22f),
                    StrategyBuildTool.Mine => new Vector3(bounds.center.x - 0.16f, bounds.min.y + bounds.size.y * 0.22f, -0.22f),
                    StrategyBuildTool.CoalPit => new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.35f, -0.22f),
                    StrategyBuildTool.Kiln => new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.34f, -0.22f),
                    StrategyBuildTool.Forge => new Vector3(bounds.center.x + 0.10f, bounds.min.y + bounds.size.y * 0.38f, -0.22f),
                    StrategyBuildTool.StorageYard => new Vector3(bounds.min.x + bounds.size.x * 0.26f, bounds.min.y + bounds.size.y * 0.42f, -0.22f),
                    StrategyBuildTool.Granary => new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.46f, -0.22f),
                    StrategyBuildTool.Bridge => new Vector3(bounds.center.x, bounds.center.y, -0.22f),
                    _ => new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.42f, -0.22f)
                };
            }

            return transform.position + new Vector3(0f, 0.20f, -0.22f);
        }

        private Vector3 GetInteriorOffset()
        {
            if (kind != StrategyCinematicLightKind.House)
            {
                return Vector3.zero;
            }

            float x = Mathf.Sin(Time.timeSinceLevelLoad * 0.55f + flickerSeed) * 0.18f;
            return new Vector3(x, -0.01f, 0f);
        }

        private float GetActivityFactor(float night, float warm, float rain, float fog, float storm)
        {
            float weatherBoost = Mathf.Max(rain * 0.26f, fog * 0.30f) + storm * 0.24f;
            if (kind == StrategyCinematicLightKind.Campfire)
            {
                return Mathf.Clamp01(0.84f + night * 0.20f + weatherBoost);
            }

            if (kind == StrategyCinematicLightKind.House)
            {
                float occupied = building != null && building.ResidentCount > 0 ? 1f : 0.32f;
                return Mathf.Clamp01((night * 0.92f + warm * 0.30f + weatherBoost) * occupied);
            }

            float worksiteBase = kind == StrategyCinematicLightKind.Mine
                || kind == StrategyCinematicLightKind.CoalPit
                || kind == StrategyCinematicLightKind.Kiln
                || kind == StrategyCinematicLightKind.Forge
                ? 0.34f
                : 0.12f;
            return Mathf.Clamp01(worksiteBase + night * 0.62f + warm * 0.16f + weatherBoost);
        }

        private float GetFlicker()
        {
            float t = Time.timeSinceLevelLoad;
            float fast = Mathf.PerlinNoise(flickerSeed, t * 3.4f);
            float slow = Mathf.PerlinNoise(flickerSeed + 31.7f, t * 0.72f);
            return Mathf.Lerp(0.82f, 1.16f, fast * 0.72f + slow * 0.28f);
        }

        private Color GetColor(float wet, float storm)
        {
            Color baseColor = kind switch
            {
                StrategyCinematicLightKind.Mine => new Color(1f, 0.79f, 0.46f, 1f),
                StrategyCinematicLightKind.CoalPit => new Color(1f, 0.48f, 0.24f, 1f),
                StrategyCinematicLightKind.Kiln => new Color(1f, 0.43f, 0.18f, 1f),
                StrategyCinematicLightKind.Forge => new Color(1f, 0.36f, 0.16f, 1f),
                StrategyCinematicLightKind.Campfire => new Color(1f, 0.54f, 0.22f, 1f),
                StrategyCinematicLightKind.House => new Color(1f, 0.77f, 0.38f, 1f),
                StrategyCinematicLightKind.Granary => new Color(1f, 0.68f, 0.34f, 1f),
                _ => new Color(1f, 0.70f, 0.35f, 1f)
            };
            return Color.Lerp(baseColor, new Color(0.82f, 0.90f, 1f, 1f), storm * 0.22f + wet * 0.08f);
        }

        private float GetBaseIntensity() => kind switch
        {
            StrategyCinematicLightKind.Campfire => 1.15f,
            StrategyCinematicLightKind.Mine => 0.82f,
            StrategyCinematicLightKind.CoalPit => 0.72f,
            StrategyCinematicLightKind.Kiln => 0.70f,
            StrategyCinematicLightKind.Forge => 0.78f,
            StrategyCinematicLightKind.House => 0.54f,
            StrategyCinematicLightKind.Bridge => 0.44f,
            StrategyCinematicLightKind.Storage => 0.34f,
            StrategyCinematicLightKind.Granary => 0.40f,
            _ => 0.42f
        };

        private float GetBaseRadius() => kind switch
        {
            StrategyCinematicLightKind.Campfire => 4.6f,
            StrategyCinematicLightKind.Mine => 3.8f,
            StrategyCinematicLightKind.CoalPit => 3.4f,
            StrategyCinematicLightKind.Kiln => 3.2f,
            StrategyCinematicLightKind.Forge => 3.5f,
            StrategyCinematicLightKind.House => 3.0f,
            StrategyCinematicLightKind.Bridge => 2.8f,
            StrategyCinematicLightKind.Storage => 2.4f,
            StrategyCinematicLightKind.Granary => 2.8f,
            _ => 2.7f
        };

        private float GetGlowAlpha() => kind == StrategyCinematicLightKind.Campfire ? 0.28f : 0.18f;
        private float GetCoreAlpha() => kind == StrategyCinematicLightKind.House ? 0.70f : 0.52f;
        private Vector2 GetGlowScale(float radius) => new(radius * 0.72f, radius * 0.38f);
        private Vector2 GetCoreScale() => kind == StrategyCinematicLightKind.House ? new Vector2(0.78f, 0.34f) : new Vector2(0.32f, 0.26f);
        private Sprite GetCoreSprite() => kind == StrategyCinematicLightKind.House
            ? StrategyCinematicVisualSprites.GetWindowMaskSprite()
            : StrategyCinematicVisualSprites.GetLampCoreSprite();

        private static StrategyCinematicLightKind GetKind(StrategyBuildTool tool) => tool switch
        {
            StrategyBuildTool.House => StrategyCinematicLightKind.House,
            StrategyBuildTool.Mine => StrategyCinematicLightKind.Mine,
            StrategyBuildTool.CoalPit => StrategyCinematicLightKind.CoalPit,
            StrategyBuildTool.Kiln => StrategyCinematicLightKind.Kiln,
            StrategyBuildTool.Forge => StrategyCinematicLightKind.Forge,
            StrategyBuildTool.Bridge => StrategyCinematicLightKind.Bridge,
            StrategyBuildTool.StorageYard => StrategyCinematicLightKind.Storage,
            StrategyBuildTool.Granary => StrategyCinematicLightKind.Granary,
            _ => StrategyCinematicLightKind.Worksite
        };
    }
}
