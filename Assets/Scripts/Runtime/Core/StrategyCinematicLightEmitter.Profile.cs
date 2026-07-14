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
            ConfigureFlickerProfile(GetRoadsideFlickerKey(owner));

            configured = owner != null;
            RegisterCoverageEmitter();
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
                return Mathf.Clamp01(night * 0.76f + warm * 0.18f + weatherBoost);
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
            float t = Time.unscaledTime + flickerTimeOffset;
            float fast = Mathf.PerlinNoise(flickerSeed + t * flickerFastSpeed, flickerSeed * 0.37f + t * 0.29f);
            float slow = Mathf.PerlinNoise(flickerSeed + 31.7f + t * 0.21f, flickerSeed * 0.11f + t * flickerSlowSpeed);
            float wave = fast * 0.62f + slow * 0.38f;
            float softened = Mathf.Lerp(0.5f, wave, flickerDepth);
            return Mathf.Lerp(0.90f, 1.10f, softened);
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
            StrategyCinematicLightKind.RoadsideTorch => 0.52f,
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
            StrategyCinematicLightKind.RoadsideTorch => 2.75f,
            StrategyCinematicLightKind.Storage => 2.4f,
            StrategyCinematicLightKind.Granary => 2.8f,
            _ => 2.7f
        };

        private float GetGlowAlpha() => kind switch
        {
            StrategyCinematicLightKind.Campfire => 0.28f,
            StrategyCinematicLightKind.RoadsideTorch => 0.28f,
            _ => 0.18f
        };

        private float GetCoreAlpha() => kind switch
        {
            StrategyCinematicLightKind.House => 0.70f,
            StrategyCinematicLightKind.RoadsideTorch => 0.68f,
            _ => 0.52f
        };

        private float GetTorchIntensityBoost()
        {
            if (!CanRenderTorch())
            {
                return 1f;
            }

            return kind == StrategyCinematicLightKind.RoadsideTorch ? 1.85f : 1.35f;
        }

        private float GetTorchRadiusBoost()
        {
            if (!CanRenderTorch())
            {
                return 1f;
            }

            return kind == StrategyCinematicLightKind.RoadsideTorch ? 1.36f : 1.18f;
        }

        private Vector2 GetGlowScale(float radius) => kind == StrategyCinematicLightKind.RoadsideTorch
            ? new Vector2(radius * 0.56f, radius * 0.30f)
            : new Vector2(radius * 0.72f, radius * 0.38f);

        private Vector2 GetCoreScale() => kind switch
        {
            StrategyCinematicLightKind.House => Vector2.one,
            StrategyCinematicLightKind.RoadsideTorch => new Vector2(0.30f, 0.26f),
            _ => new Vector2(0.32f, 0.26f)
        };

        private Sprite GetCoreSprite() => kind == StrategyCinematicLightKind.House
            ? StrategyHouseAmbientSpriteFactory.GetWindowMaskSprite(
                building != null ? building.VisualVariant : 0)
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

        private void ConfigureFlickerProfile(int sourceKey)
        {
            int profileKey = GetFlickerProfileKey(sourceKey);
            if (flickerProfileKey == profileKey && flickerSeed > 0.001f)
            {
                return;
            }

            flickerProfileKey = profileKey;
            float a = GetHashUnit(profileKey, 0x9E3779B9u);
            float b = GetHashUnit(profileKey, 0x85EBCA6Bu);
            float c = GetHashUnit(profileKey, 0xC2B2AE35u);
            float d = GetHashUnit(profileKey, 0x27D4EB2Fu);
            float e = GetHashUnit(profileKey, 0x165667B1u);

            flickerSeed = 1f + a * 997f;
            flickerTimeOffset = b * 43f;
            flickerFastSpeed = Mathf.Lerp(2.15f, 4.85f, c);
            flickerSlowSpeed = Mathf.Lerp(0.36f, 1.08f, d);
            flickerDepth = Mathf.Lerp(0.46f, 0.78f, e);
            flickerFrameSpeedScale = Mathf.Lerp(0.86f, 1.16f, GetHashUnit(profileKey, 0xA24BAED5u));
            visualTimer = VisualUpdateInterval * GetHashUnit(profileKey, 0x9FB21C65u);
        }

        private int GetBuildingFlickerKey(StrategyPlacedBuilding owner)
        {
            if (owner == null)
            {
                return GetTransformFlickerKey();
            }

            return HashFlickerInts(
                (int)kind,
                (int)owner.Tool,
                owner.Origin.x,
                owner.Origin.y,
                owner.Footprint.x,
                owner.Footprint.y);
        }

        private int GetRoadsideFlickerKey(StrategyRoadsideLightSource owner)
        {
            if (owner == null)
            {
                return GetTransformFlickerKey();
            }

            return HashFlickerInts(
                (int)kind,
                owner.RoadCell.x,
                owner.RoadCell.y,
                owner.SideOffset.x,
                owner.SideOffset.y,
                17);
        }

        private int GetTransformFlickerKey()
        {
            Vector3 position = transform.position;
            return HashFlickerInts(
                (int)kind,
                Mathf.RoundToInt(position.x * 100f),
                Mathf.RoundToInt(position.y * 100f),
                Mathf.RoundToInt(position.z * 100f),
                GetStableTextHash(gameObject != null ? gameObject.name : string.Empty),
                31);
        }

        private int GetFlickerProfileKey(int sourceKey)
        {
            return HashFlickerInts((int)kind, sourceKey, 53, 97, 193, 389);
        }

        private static int HashFlickerInts(int a, int b, int c, int d, int e, int f)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)a) * 16777619u;
                hash = (hash ^ (uint)b) * 16777619u;
                hash = (hash ^ (uint)c) * 16777619u;
                hash = (hash ^ (uint)d) * 16777619u;
                hash = (hash ^ (uint)e) * 16777619u;
                hash = (hash ^ (uint)f) * 16777619u;
                return (int)(hash & 0x7FFFFFFFu);
            }
        }

        private static float GetHashUnit(int key, uint salt)
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

        private static int GetStableTextHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash = (hash ^ text[i]) * 16777619u;
                    }
                }

                return (int)(hash & 0x7FFFFFFFu);
            }
        }
    }
}
