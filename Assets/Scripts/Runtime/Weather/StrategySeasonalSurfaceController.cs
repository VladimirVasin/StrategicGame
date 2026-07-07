using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategySeasonalSurfaceController : MonoBehaviour
    {
        private const int PixelsPerCell = 4;
        private const float SurfaceRepaintSeconds = 0.65f;
        private const float BuildingRefreshSeconds = 0.45f;
        private const float GameplayFrozenThreshold = 0.58f;

        private readonly Dictionary<StrategyPlacedBuilding, SpriteRenderer> buildingSnow = new();
        private readonly List<StrategyPlacedBuilding> staleBuildings = new();
        private CityMapController map;
        private StrategyWeatherController weather;
        private SpriteRenderer snowRenderer;
        private SpriteRenderer iceRenderer;
        private Texture2D snowTexture;
        private Texture2D iceTexture;
        private Color[] snowPixels;
        private Color[] icePixels;
        private float snowCoverFactor;
        private float iceCoverFactor;
        private float surfaceTimer;
        private float buildingTimer;
        private float lastPaintedSnow = -1f;
        private float lastPaintedIce = -1f;
        private bool lastGameplayFrozen;

        public static StrategySeasonalSurfaceController Active { get; private set; }
        public static bool IsWaterFrozenForGameplay => Active != null && Active.IsWaterGameplayFrozen;
        public float SnowCoverFactor => snowCoverFactor;
        public float IceCoverFactor => iceCoverFactor;
        public bool IsWaterGameplayFrozen => iceCoverFactor >= GameplayFrozenThreshold;

        public void Configure(CityMapController mapController, StrategyWeatherController weatherController)
        {
            Active = this;
            map = mapController;
            weather = weatherController;
            EnsureRenderers();
            PaintSurfaces(true);
            RefreshBuildingSnow(true);
            lastGameplayFrozen = IsWaterGameplayFrozen;
            StrategyDebugLogger.Info(
                "Weather",
                "SeasonalSurfacesConfigured",
                StrategyDebugLogger.F("pixelsPerCell", PixelsPerCell),
                StrategyDebugLogger.F("snowOrder", StrategyWorldSorting.SeasonalGroundOverlayOrder),
                StrategyDebugLogger.F("iceOrder", StrategyWorldSorting.SeasonalIceOverlayOrder));
        }

        private void Update()
        {
            if (map == null)
            {
                return;
            }

            UpdateCoverageFactors(Mathf.Max(0f, Time.deltaTime));
            EnsureRenderers();

            float visualDt = Mathf.Max(0f, Time.unscaledDeltaTime);
            surfaceTimer += visualDt;
            buildingTimer += visualDt;

            bool surfaceDirty = surfaceTimer >= SurfaceRepaintSeconds
                || Mathf.Abs(snowCoverFactor - lastPaintedSnow) > 0.018f
                || Mathf.Abs(iceCoverFactor - lastPaintedIce) > 0.018f;
            if (surfaceDirty)
            {
                surfaceTimer = 0f;
                PaintSurfaces(false);
            }

            if (buildingTimer >= BuildingRefreshSeconds || surfaceDirty)
            {
                buildingTimer = 0f;
                RefreshBuildingSnow(false);
            }
        }

        private void UpdateCoverageFactors(float dt)
        {
            StrategyCalendarSnapshot calendar = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            StrategyTemperatureSnapshot temperature = StrategyTemperatureModel.Evaluate(calendar, weather);
            float snowTarget = CalculateSnowTarget(calendar, temperature.Celsius);
            float iceTarget = CalculateIceTarget(calendar, temperature.Celsius, snowTarget);
            float snowSpeed = snowTarget > snowCoverFactor
                ? 0.020f + GetSnowIntensity() * 0.095f
                : 0.014f + Mathf.Max(GetRainIntensity() * 0.055f, Mathf.InverseLerp(0f, 8f, temperature.Celsius) * 0.045f);
            float iceSpeed = iceTarget > iceCoverFactor ? 0.028f : 0.040f;

            snowCoverFactor = Mathf.MoveTowards(snowCoverFactor, snowTarget, snowSpeed * dt);
            iceCoverFactor = Mathf.MoveTowards(iceCoverFactor, iceTarget, iceSpeed * dt);
            LogGameplayFreezeChange();
        }

        private float CalculateSnowTarget(StrategyCalendarSnapshot calendar, float celsius)
        {
            float cold = Mathf.InverseLerp(4f, -3f, celsius);
            float snowfall = GetSnowIntensity();
            float target = calendar.Season == StrategySeason.Winter ? Mathf.Lerp(0.24f, 0.72f, cold) : 0f;
            target = Mathf.Max(target, snowfall * Mathf.Lerp(0.40f, 1f, cold));
            target -= GetRainIntensity() * Mathf.Lerp(0.16f, 0.45f, Mathf.InverseLerp(0f, 8f, celsius));
            if (celsius > 5f && snowfall <= 0.05f)
            {
                target = 0f;
            }

            return Mathf.Clamp01(target);
        }

        private float CalculateIceTarget(StrategyCalendarSnapshot calendar, float celsius, float snowTarget)
        {
            if (calendar.Season != StrategySeason.Winter)
            {
                return 0f;
            }

            float cold = Mathf.InverseLerp(2f, -5f, celsius);
            float target = cold * Mathf.Lerp(0.52f, 0.82f, snowTarget);
            target -= GetRainIntensity() * 0.20f;
            return Mathf.Clamp01(target);
        }

        private void LogGameplayFreezeChange()
        {
            bool frozen = IsWaterGameplayFrozen;
            if (frozen == lastGameplayFrozen)
            {
                return;
            }

            lastGameplayFrozen = frozen;
            StrategyDebugLogger.Info(
                "Weather",
                "WaterFreezeChanged",
                StrategyDebugLogger.F("frozen", frozen),
                StrategyDebugLogger.F("ice", iceCoverFactor),
                StrategyDebugLogger.F("snow", snowCoverFactor),
                StrategyDebugLogger.F("threshold", GameplayFrozenThreshold));
        }

        private void EnsureRenderers()
        {
            EnsureSurfaceRenderer(
                ref snowRenderer,
                ref snowTexture,
                ref snowPixels,
                "Seasonal Snow Ground Overlay",
                StrategyWorldSorting.SeasonalGroundOverlayOrder);
            EnsureSurfaceRenderer(
                ref iceRenderer,
                ref iceTexture,
                ref icePixels,
                "Seasonal Ice Water Overlay",
                StrategyWorldSorting.SeasonalIceOverlayOrder);
        }

        private void EnsureSurfaceRenderer(
            ref SpriteRenderer renderer,
            ref Texture2D texture,
            ref Color[] pixels,
            string objectName,
            int sortingOrder)
        {
            if (map == null)
            {
                return;
            }

            int width = map.Width * PixelsPerCell;
            int height = map.Height * PixelsPerCell;
            bool recreateSprite = false;
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }

                if (renderer != null && renderer.sprite != null)
                {
                    Destroy(renderer.sprite);
                    renderer.sprite = null;
                }

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = objectName + " Texture"
                };
                pixels = new Color[width * height];
                recreateSprite = true;
            }

            if (renderer == null)
            {
                GameObject overlayObject = new(objectName);
                overlayObject.transform.SetParent(transform, false);
                renderer = overlayObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = sortingOrder;
            }

            if (renderer.sprite == null || recreateSprite)
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f),
                    PixelsPerCell / map.CellSize);
                sprite.name = objectName + " Sprite";
                renderer.sprite = sprite;
            }

            Bounds bounds = map.WorldBounds;
            renderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.055f);
            renderer.transform.localScale = Vector3.one;
        }

        private void PaintSurfaces(bool force)
        {
            if (map == null || snowPixels == null || icePixels == null)
            {
                return;
            }

            if (!force
                && Mathf.Abs(snowCoverFactor - lastPaintedSnow) <= 0.002f
                && Mathf.Abs(iceCoverFactor - lastPaintedIce) <= 0.002f)
            {
                return;
            }

            System.Array.Clear(snowPixels, 0, snowPixels.Length);
            System.Array.Clear(icePixels, 0, icePixels.Length);
            bool snowVisible = snowCoverFactor > 0.01f;
            bool iceVisible = iceCoverFactor > 0.01f;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!map.TryGetCell(x, y, out CityMapCell cell))
                    {
                        continue;
                    }

                    if (snowVisible && cell.Kind != CityMapCellKind.Water)
                    {
                        PaintSnowCell(x, y, cell);
                    }

                    if (iceVisible && cell.Kind == CityMapCellKind.Water)
                    {
                        PaintIceCell(x, y, cell);
                    }
                }
            }

            ApplySurfaceTexture(snowRenderer, snowTexture, snowPixels, snowVisible);
            ApplySurfaceTexture(iceRenderer, iceTexture, icePixels, iceVisible);
            lastPaintedSnow = snowCoverFactor;
            lastPaintedIce = iceCoverFactor;
        }

        private void PaintSnowCell(int cellX, int cellY, CityMapCell cell)
        {
            float strength = GetSnowTerrainStrength(cell.Kind);
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 991);
                    float detail = Hash01(map.ActiveSeed, cellX, cellY, px, py, 1171);
                    float alpha = snowCoverFactor * strength * Mathf.Lerp(0.36f, 0.78f, noise);
                    if (detail > 0.88f && snowCoverFactor < 0.86f)
                    {
                        alpha *= 0.22f;
                    }

                    Color color = Color.Lerp(
                        new Color(0.70f, 0.82f, 0.88f, 1f),
                        new Color(0.96f, 1f, 1f, 1f),
                        noise);
                    color.a = Mathf.Clamp01(alpha);
                    SetPixel(snowPixels, cellX, cellY, px, py, color);
                }
            }
        }

        private void PaintIceCell(int cellX, int cellY, CityMapCell cell)
        {
            bool river = cell.IsRiver;
            float strength = river ? 0.54f : 0.78f;
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 1601);
                    float crack = Hash01(map.ActiveSeed, cellX, cellY, px, py, 1867);
                    float alpha = iceCoverFactor * strength * Mathf.Lerp(0.46f, 0.86f, noise);
                    if (river && (px == py || crack > 0.82f))
                    {
                        alpha *= 0.34f;
                    }
                    else if (!river && crack > 0.93f)
                    {
                        alpha *= 0.48f;
                    }

                    Color color = Color.Lerp(
                        new Color(0.63f, 0.82f, 0.91f, 1f),
                        new Color(0.93f, 1f, 1f, 1f),
                        noise);
                    color.a = Mathf.Clamp01(alpha);
                    SetPixel(icePixels, cellX, cellY, px, py, color);
                }
            }
        }

        private void RefreshBuildingSnow(bool force)
        {
            float alpha = Mathf.Clamp01(snowCoverFactor * 1.18f);
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null)
                {
                    continue;
                }

                SpriteRenderer renderer = GetOrCreateBuildingSnowRenderer(building);
                if (renderer == null)
                {
                    continue;
                }

                SpriteRenderer baseRenderer = building.GetComponent<SpriteRenderer>();
                renderer.enabled = alpha > 0.025f;
                renderer.color = new Color(1f, 1f, 1f, alpha);
                renderer.sortingOrder = baseRenderer != null
                    ? baseRenderer.sortingOrder + 2
                    : StrategyWorldSorting.ForPosition(building.FootprintBounds.center, 2);
            }

            if (force || buildingSnow.Count != buildings.Count)
            {
                CleanupDestroyedBuildingSnow();
            }
        }

        private SpriteRenderer GetOrCreateBuildingSnowRenderer(StrategyPlacedBuilding building)
        {
            if (buildingSnow.TryGetValue(building, out SpriteRenderer existing) && existing != null)
            {
                return existing;
            }

            SpriteRenderer baseRenderer = building.GetComponent<SpriteRenderer>();
            if (baseRenderer == null || baseRenderer.sprite == null)
            {
                return null;
            }

            Sprite snowSprite = StrategyBuildingSnowSpriteFactory.GetSnowCapSprite(baseRenderer.sprite);
            if (snowSprite == null)
            {
                return null;
            }

            GameObject snowObject = new("Seasonal Building Snow");
            snowObject.transform.SetParent(building.transform, false);
            SpriteRenderer snowCap = snowObject.AddComponent<SpriteRenderer>();
            snowCap.sprite = snowSprite;
            snowCap.enabled = false;
            buildingSnow[building] = snowCap;
            return snowCap;
        }

        private void CleanupDestroyedBuildingSnow()
        {
            staleBuildings.Clear();
            foreach (KeyValuePair<StrategyPlacedBuilding, SpriteRenderer> pair in buildingSnow)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    staleBuildings.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleBuildings.Count; i++)
            {
                StrategyPlacedBuilding stale = staleBuildings[i];
                if (!object.ReferenceEquals(stale, null))
                {
                    buildingSnow.Remove(stale);
                }
            }
        }

        private static void ApplySurfaceTexture(
            SpriteRenderer renderer,
            Texture2D texture,
            Color[] pixels,
            bool visible)
        {
            if (renderer == null || texture == null)
            {
                return;
            }

            renderer.enabled = visible;
            texture.SetPixels(pixels);
            texture.Apply(false, false);
        }

        private void SetPixel(Color[] pixels, int cellX, int cellY, int px, int py, Color color)
        {
            int x = cellX * PixelsPerCell + px;
            int y = cellY * PixelsPerCell + py;
            pixels[y * map.Width * PixelsPerCell + x] = color;
        }

        private static float GetSnowTerrainStrength(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Meadow => 0.88f,
                CityMapCellKind.Grass => 0.80f,
                CityMapCellKind.Forest => 0.58f,
                CityMapCellKind.Dirt => 0.48f,
                CityMapCellKind.Shore => 0.64f,
                _ => 0.70f
            };
        }

        private float GetSnowIntensity()
        {
            return weather != null ? weather.SnowIntensity : 0f;
        }

        private float GetRainIntensity()
        {
            return weather != null ? weather.RainIntensity : 0f;
        }

        private static float Hash01(int seed, int x, int y, int px, int py, int salt)
        {
            unchecked
            {
                int n = seed;
                n = n * 73856093 ^ x * 19349663 ^ y * 83492791 ^ px * 265443576 ^ py * 1597334677 ^ salt;
                n = (n << 13) ^ n;
                return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            DestroySurface(snowRenderer, snowTexture);
            DestroySurface(iceRenderer, iceTexture);
            buildingSnow.Clear();
        }

        private static void DestroySurface(SpriteRenderer renderer, Texture2D texture)
        {
            if (renderer != null && renderer.sprite != null)
            {
                Destroy(renderer.sprite);
            }

            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
