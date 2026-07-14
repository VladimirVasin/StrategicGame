using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyCinematicLightEmitter
    {
        private const int TorchBaseSortingOffset = 23;
        private const int TorchFlameSortingOffset = 24;
        private SpriteRenderer torchBaseRenderer;
        private SpriteRenderer torchFlameRenderer;
        private SpriteRenderer buildingRenderer;

        private void EnsureTorchVisuals()
        {
            if (!CanRenderTorch() || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            if (building != null)
            {
                torchBaseRenderer ??= CreateRenderer(
                    "Cinematic Torch Base",
                    StrategyBuildingLightSpriteFactory.GetBaseSprite(GetTorchSpriteKind()),
                    TorchBaseSortingOffset);
            }

            torchFlameRenderer ??= CreateRenderer(
                "Cinematic Torch Flame",
                StrategyBuildingLightSpriteFactory.GetFlameSprite(GetTorchSpriteKind(), 0),
                TorchFlameSortingOffset);
        }

        private void UpdateTorchAnchor()
        {
            if (torchBaseRenderer == null && torchFlameRenderer == null)
            {
                return;
            }

            Vector3 anchor = GetTorchAnchorWorld();
            PositionTorchRenderer(torchBaseRenderer, anchor, TorchBaseSortingOffset);
            PositionTorchRenderer(torchFlameRenderer, anchor, TorchFlameSortingOffset);
        }

        private void ApplyTorchLighting(Color lightColor, float activity, float flicker)
        {
            if (!CanRenderTorch())
            {
                return;
            }

            ApplyTorchBase();
            if (torchFlameRenderer == null)
            {
                return;
            }

            float lit = GetDarkTimeLightFactor() * GetLightStateFactor();
            float visibility = Mathf.Clamp01(
                lit * Mathf.Lerp(0.82f, 1.06f, activity) * flicker * LocalLightStrengthMultiplier);

            if (visibility <= 0.035f)
            {
                torchFlameRenderer.enabled = false;
                return;
            }

            int frame = Mathf.FloorToInt((Time.unscaledTime + flickerTimeOffset) * GetTorchFrameSpeed() * flickerFrameSpeedScale + flickerSeed)
                % StrategyBuildingLightSpriteFactory.FrameCount;
            torchFlameRenderer.sprite = StrategyBuildingLightSpriteFactory.GetFlameSprite(GetTorchSpriteKind(), frame);
            torchFlameRenderer.enabled = true;
            torchFlameRenderer.color = new Color(
                Mathf.Lerp(1f, lightColor.r, 0.22f),
                Mathf.Lerp(1f, lightColor.g, 0.22f),
                Mathf.Lerp(1f, lightColor.b, 0.22f),
                1f);
            float pulse = Mathf.Lerp(0.92f, 1.09f, flicker);
            float flameScale = Mathf.Lerp(0.20f, 1f, Mathf.Clamp01(visibility));
            torchFlameRenderer.transform.localScale = new Vector3(pulse * flameScale, pulse * flameScale, 1f);
        }

        private void DisableTorchVisuals()
        {
            if (torchBaseRenderer != null)
            {
                torchBaseRenderer.enabled = false;
            }

            if (torchFlameRenderer != null)
            {
                torchFlameRenderer.enabled = false;
            }
        }

        private void ApplyTorchBase()
        {
            if (torchBaseRenderer == null)
            {
                return;
            }

            torchBaseRenderer.sprite = StrategyBuildingLightSpriteFactory.GetBaseSprite(GetTorchSpriteKind());
            torchBaseRenderer.enabled = true;
            torchBaseRenderer.color = Color.white;
            torchBaseRenderer.transform.localScale = Vector3.one;
        }

        private void PositionTorchRenderer(SpriteRenderer renderer, Vector3 anchor, int offset)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = anchor;
            renderer.sortingOrder = GetTorchSortingOrder(offset);
        }

        private bool CanRenderTorch()
        {
            return (building != null || roadsideLight != null) && kind != StrategyCinematicLightKind.Campfire;
        }

        private float GetLightStateFactor()
        {
            if (kind == StrategyCinematicLightKind.Campfire && campfire != null)
            {
                return campfire.LightIntensityFactor;
            }

            return CanRenderTorch()
                ? nightLightSource != null ? nightLightSource.LitVisibilityFactor : 0f
                : 1f;
        }

        private float GetDarkTimeLightFactor()
        {
            if (kind == StrategyCinematicLightKind.Campfire)
            {
                return 1f;
            }

            float phase = StrategyDayNightCycleController.CurrentDayPhase;
            float night = StrategyCinematicVisualMath.NightFactor(phase);
            return CanRenderTorch()
                ? Mathf.Max(night, StrategyCinematicVisualMath.DawnToNoonFadeOutFactor(phase))
                : night;
        }

        private Vector3 GetLightSourceWorld()
        {
            return CanRenderTorch() ? GetTorchAnchorWorld() : GetAnchorWorld();
        }

        private void RefreshNightLightSource()
        {
            if (!CanRenderTorch())
            {
                nightLightSource = null;
                return;
            }

            if (nightLightSource == null && !TryGetComponent(out nightLightSource))
            {
                nightLightSource = gameObject.AddComponent<StrategyNightLightSource>();
            }

            if (nightLightSource == null)
            {
                return;
            }

            if (building != null)
            {
                nightLightSource.ConfigureForBuilding(building, GetTorchAnchorWorld());
            }
            else if (roadsideLight != null)
            {
                nightLightSource.ConfigureForRoadside(roadsideLight);
            }
        }

        private Vector3 GetCoreVisualWorld(Vector3 defaultWorld)
        {
            if (kind == StrategyCinematicLightKind.House && building != null)
            {
                return building.transform.position;
            }

            return CanRenderTorch() && kind != StrategyCinematicLightKind.House
                ? GetTorchAnchorWorld()
                : defaultWorld;
        }

        internal bool TryGetNightMaskLight(
            out Vector3 world,
            out float radius,
            out float strength,
            out float edgeFlicker)
        {
            world = GetLightSourceWorld();
            radius = 0f;
            strength = 0f;
            edgeFlicker = 1f;
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
            strength = Mathf.Clamp01(
                GetBaseIntensity()
                * GetTorchIntensityBoost()
                * LocalLightStrengthMultiplier
                * activity
                * GetLightStateFactor()
                * GetDarkTimeLightFactor());
            if (strength <= 0.035f)
            {
                return false;
            }

            radius = GetBaseRadius()
                * GetTorchRadiusBoost()
                * LocalLightRadiusMultiplier
                * Mathf.Lerp(1.06f, 1.36f, strength);
            edgeFlicker = GetFlicker();
            return radius > 0.2f;
        }

        private int GetTorchSortingOrder(int offset)
        {
            buildingRenderer ??= building != null ? building.GetComponent<SpriteRenderer>() : null;
            if (buildingRenderer != null)
            {
                return buildingRenderer.sortingOrder + offset;
            }

            return StrategyWorldSorting.ForPosition(transform.position, offset);
        }

        private StrategyBuildingLightSpriteKind GetTorchSpriteKind()
        {
            if (building != null && building.Tool == StrategyBuildTool.ForagerCamp)
            {
                return StrategyBuildingLightSpriteKind.Lantern;
            }

            return kind switch
            {
                StrategyCinematicLightKind.Mine => StrategyBuildingLightSpriteKind.Lantern,
                StrategyCinematicLightKind.CoalPit => StrategyBuildingLightSpriteKind.Brazier,
                StrategyCinematicLightKind.Kiln => StrategyBuildingLightSpriteKind.Brazier,
                StrategyCinematicLightKind.Forge => StrategyBuildingLightSpriteKind.Brazier,
                StrategyCinematicLightKind.Storage => StrategyBuildingLightSpriteKind.Lantern,
                StrategyCinematicLightKind.Granary => StrategyBuildingLightSpriteKind.Lantern,
                StrategyCinematicLightKind.Bridge => StrategyBuildingLightSpriteKind.BridgeLamp,
                StrategyCinematicLightKind.RoadsideTorch => StrategyBuildingLightSpriteKind.Lantern,
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
                StrategyBuildTool.ForagerCamp => StrategyForagerCampVisualProfile.GetTorchAnchorWorld(bounds),
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
