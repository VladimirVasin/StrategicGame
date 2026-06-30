using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyCinematicLightEmitter
    {
        private StrategyRoadsideLightSource roadsideLight;

        public void ConfigureForRoadsideLight(StrategyRoadsideLightSource owner)
        {
            if (configured && roadsideLight == owner && owner != null)
            {
                return;
            }

            building = null;
            campfire = null;
            roadsideLight = owner;
            kind = StrategyCinematicLightKind.RoadsideTorch;
            if (flickerSeed <= 0.001f)
            {
                flickerSeed = Random.Range(0f, 1000f);
                visualTimer = Random.Range(0f, VisualUpdateInterval);
            }

            configured = owner != null;
            RefreshNightLightSource();
            EnsureVisuals();
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

            if (kind == StrategyCinematicLightKind.RoadsideTorch)
            {
                return Mathf.Clamp01(night * 0.58f + warm * 0.12f + weatherBoost);
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
            float t = Time.unscaledTime;
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
                StrategyCinematicLightKind.RoadsideTorch => new Color(1f, 0.64f, 0.28f, 1f),
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
            StrategyCinematicLightKind.RoadsideTorch => 0.27f,
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
            StrategyCinematicLightKind.RoadsideTorch => 1.85f,
            StrategyCinematicLightKind.Storage => 2.4f,
            StrategyCinematicLightKind.Granary => 2.8f,
            _ => 2.7f
        };

        private float GetGlowAlpha() => kind switch
        {
            StrategyCinematicLightKind.Campfire => 0.28f,
            StrategyCinematicLightKind.RoadsideTorch => 0.12f,
            _ => 0.18f
        };

        private float GetCoreAlpha() => kind switch
        {
            StrategyCinematicLightKind.House => 0.70f,
            StrategyCinematicLightKind.RoadsideTorch => 0.38f,
            _ => 0.52f
        };

        private Vector2 GetGlowScale(float radius) => kind == StrategyCinematicLightKind.RoadsideTorch
            ? new Vector2(radius * 0.56f, radius * 0.30f)
            : new Vector2(radius * 0.72f, radius * 0.38f);

        private Vector2 GetCoreScale() => kind switch
        {
            StrategyCinematicLightKind.House => new Vector2(0.78f, 0.34f),
            StrategyCinematicLightKind.RoadsideTorch => new Vector2(0.22f, 0.20f),
            _ => new Vector2(0.32f, 0.26f)
        };

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
