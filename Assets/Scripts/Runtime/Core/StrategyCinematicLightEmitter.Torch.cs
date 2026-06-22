using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyCinematicLightEmitter
    {
        private const int TorchSortingOffset = 23;
        private SpriteRenderer torchRenderer;
        private SpriteRenderer buildingRenderer;

        private void EnsureTorchVisuals()
        {
            if (!CanRenderTorch() || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            torchRenderer ??= CreateRenderer(
                "Cinematic Torch",
                StrategyBuildingLightSpriteFactory.GetSprite(GetTorchSpriteKind(), 0),
                TorchSortingOffset);
        }

        private void UpdateTorchAnchor()
        {
            if (torchRenderer == null)
            {
                return;
            }

            torchRenderer.transform.position = GetTorchAnchorWorld();
            torchRenderer.sortingOrder = GetTorchSortingOrder();
        }

        private void ApplyTorchLighting(Color lightColor, float activity, float flicker)
        {
            if (torchRenderer == null || !CanRenderTorch())
            {
                return;
            }

            StrategyWeatherController activeWeather = StrategyWeatherController.Active;
            float phase = StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            float rain = activeWeather != null ? activeWeather.RainIntensity : 0f;
            float fog = activeWeather != null ? activeWeather.FogIntensity : 0f;
            float storm = activeWeather != null ? activeWeather.StormIntensity : 0f;
            float weather = Mathf.Max(rain * 0.18f, fog * 0.36f, storm * 0.24f);
            float lit = Mathf.Clamp01(night * 0.98f + warm * 0.24f + weather);
            float visibility = Mathf.Clamp01(
                lit * Mathf.Lerp(0.82f, 1.06f, activity) * flicker * LocalLightStrengthMultiplier);

            if (visibility <= 0.035f)
            {
                torchRenderer.enabled = false;
                return;
            }

            int frame = Mathf.FloorToInt(Time.timeSinceLevelLoad * GetTorchFrameSpeed() + flickerSeed)
                % StrategyBuildingLightSpriteFactory.FrameCount;
            torchRenderer.sprite = StrategyBuildingLightSpriteFactory.GetSprite(GetTorchSpriteKind(), frame);
            torchRenderer.enabled = true;
            torchRenderer.color = new Color(
                Mathf.Lerp(1f, lightColor.r, 0.22f),
                Mathf.Lerp(1f, lightColor.g, 0.22f),
                Mathf.Lerp(1f, lightColor.b, 0.22f),
                1f);
            float pulse = Mathf.Lerp(0.92f, 1.09f, flicker);
            torchRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
        }

        private void DisableTorchVisuals()
        {
            if (torchRenderer != null)
            {
                torchRenderer.enabled = false;
            }
        }

        private bool CanRenderTorch()
        {
            return building != null && kind != StrategyCinematicLightKind.Campfire;
        }

        private float GetLightStateFactor()
        {
            return kind == StrategyCinematicLightKind.Campfire && campfire != null
                ? campfire.LightIntensityFactor
                : 1f;
        }

        private Vector3 GetLightSourceWorld()
        {
            return CanRenderTorch() ? GetTorchAnchorWorld() : GetAnchorWorld();
        }

        private Vector3 GetCoreVisualWorld(Vector3 defaultWorld)
        {
            return CanRenderTorch() && kind != StrategyCinematicLightKind.House
                ? GetTorchAnchorWorld()
                : defaultWorld;
        }

        internal bool TryGetNightMaskLight(out Vector3 world, out float radius, out float strength)
        {
            world = GetLightSourceWorld();
            radius = 0f;
            strength = 0f;
            if (!configured)
            {
                return false;
            }

            float phase = StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            float warm = StrategyCinematicVisualMath.WarmFactor(phase);
            StrategyWeatherController activeWeather = StrategyWeatherController.Active;
            float rain = activeWeather != null ? activeWeather.RainIntensity : 0f;
            float fog = activeWeather != null ? activeWeather.FogIntensity : 0f;
            float storm = activeWeather != null ? activeWeather.StormIntensity : 0f;
            float activity = GetActivityFactor(night, warm, rain, fog, storm);
            strength = Mathf.Clamp01(GetBaseIntensity() * LocalLightStrengthMultiplier * activity * GetLightStateFactor());
            if (strength <= 0.035f)
            {
                return false;
            }

            radius = GetBaseRadius() * LocalLightRadiusMultiplier * Mathf.Lerp(1.06f, 1.36f, strength);
            return radius > 0.2f;
        }

        private int GetTorchSortingOrder()
        {
            buildingRenderer ??= building != null ? building.GetComponent<SpriteRenderer>() : null;
            if (buildingRenderer != null)
            {
                return buildingRenderer.sortingOrder + TorchSortingOffset;
            }

            return StrategyWorldSorting.ForPosition(transform.position, TorchSortingOffset);
        }

        private StrategyBuildingLightSpriteKind GetTorchSpriteKind()
        {
            return kind switch
            {
                StrategyCinematicLightKind.Mine => StrategyBuildingLightSpriteKind.Lantern,
                StrategyCinematicLightKind.CoalPit => StrategyBuildingLightSpriteKind.Brazier,
                StrategyCinematicLightKind.Kiln => StrategyBuildingLightSpriteKind.Brazier,
                StrategyCinematicLightKind.Forge => StrategyBuildingLightSpriteKind.Brazier,
                StrategyCinematicLightKind.Storage => StrategyBuildingLightSpriteKind.Lantern,
                StrategyCinematicLightKind.Granary => StrategyBuildingLightSpriteKind.Lantern,
                StrategyCinematicLightKind.Bridge => StrategyBuildingLightSpriteKind.BridgeLamp,
                _ => StrategyBuildingLightSpriteKind.WallTorch
            };
        }

        private float GetTorchFrameSpeed()
        {
            return kind == StrategyCinematicLightKind.Bridge ? 5.6f : 7.4f;
        }

        private Vector3 GetTorchAnchorWorld()
        {
            if (building == null)
            {
                return GetAnchorWorld();
            }

            Bounds bounds = building.FootprintBounds;
            return building.Tool switch
            {
                StrategyBuildTool.House => LerpBounds(bounds, -0.16f, 0.30f),
                StrategyBuildTool.LumberjackCamp => LerpBounds(bounds, 1.15f, 0.30f),
                StrategyBuildTool.StonecutterCamp => LerpBounds(bounds, 1.14f, 0.28f),
                StrategyBuildTool.Sawmill => LerpBounds(bounds, -0.16f, 0.34f),
                StrategyBuildTool.Mine => LerpBounds(bounds, -0.14f, 0.30f),
                StrategyBuildTool.CoalPit => LerpBounds(bounds, 1.14f, 0.32f),
                StrategyBuildTool.ClayPit => LerpBounds(bounds, -0.15f, 0.31f),
                StrategyBuildTool.Kiln => LerpBounds(bounds, 1.14f, 0.31f),
                StrategyBuildTool.Forge => LerpBounds(bounds, 1.16f, 0.34f),
                StrategyBuildTool.HunterCamp => LerpBounds(bounds, -0.16f, 0.31f),
                StrategyBuildTool.FisherHut => LerpBounds(bounds, 1.15f, 0.30f),
                StrategyBuildTool.StorageYard => LerpBounds(bounds, -0.15f, 0.33f),
                StrategyBuildTool.Granary => LerpBounds(bounds, 1.15f, 0.32f),
                StrategyBuildTool.Bridge => GetBridgeTorchAnchorWorld(bounds),
                _ => LerpBounds(bounds, 1.14f, 0.30f)
            };
        }

        private Vector3 GetBridgeTorchAnchorWorld(Bounds bounds)
        {
            Vector2Int delta = building.BridgeEndCell - building.BridgeStartCell;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return LerpBounds(bounds, -0.14f, 0.50f);
            }

            return LerpBounds(bounds, 0.50f, -0.14f);
        }

        private static Vector3 LerpBounds(Bounds bounds, float x, float y)
        {
            return new Vector3(
                bounds.min.x + bounds.size.x * x,
                bounds.min.y + bounds.size.y * y,
                -0.20f);
        }
    }
}
