using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyFogOfWarController : MonoBehaviour
    {
        private const int FogPixelsPerCell = 8;
        private const int FogSortingOrder = StrategyWorldSorting.FogOrder;
        private const float RefreshInterval = 0.18f;
        private const float RevealEdgeSoftness = 1.75f;
        private const float VisibleThreshold = 0.08f;
        private const float CampRevealRadius = 12.5f;
        private const float ResidentRevealRadius = 5.25f;
        private const float BuildingRevealRadius = 6.25f;
        private const float NightCampRevealMultiplier = 0.55f;
        private const float NightResidentRevealMultiplier = 0.45f;
        private const float NightBuildingRevealMultiplier = 0.55f;
        private const float MinimumCampRevealRadius = 5.25f;
        private const float MinimumResidentRevealRadius = 2.35f;
        private const float MinimumBuildingRevealRadius = 3f;
        private const float DayExploredAlpha = 0.50f;
        private const float NightExploredAlpha = 0.86f;

        private static readonly Color UnexploredColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color ExploredColor = new Color(0.018f, 0.028f, 0.032f, DayExploredAlpha);

        private readonly List<RevealSource> revealSources = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyBuildPlacementController placement;
        private StrategyWeatherController weather;
        private SpriteRenderer fogRenderer;
        private Texture2D fogTexture;
        private Color[] fogPixels;
        private bool[,] explored;
        private bool[,] visible;
        private bool[,] daylightVisible;
        private byte[,] weatherFogBands;
        private float[,] visibilityStrength;
        private float refreshTimer;
        private bool isPlayerFogEnabled = true;
        private float nightVisionPressure;
        private float weatherFogPressure;
        private float exploredAlpha = DayExploredAlpha;
        private StrategyTimeOfDayPhase loggedVisionPhase = (StrategyTimeOfDayPhase)(-1);

        public bool IsPlayerFogEnabled => isPlayerFogEnabled;

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyBuildPlacementController placementController,
            StrategyWeatherController weatherController)
        {
            map = mapController;
            population = populationController;
            placement = placementController;
            weather = weatherController;

            EnsureState();
            RequestRefresh();
        }

        public void RequestRefresh()
        {
            refreshTimer = RefreshInterval;
            RefreshFog();
        }

        public void SetPlayerFogEnabled(bool enabled)
        {
            if (isPlayerFogEnabled == enabled)
            {
                return;
            }

            isPlayerFogEnabled = enabled;
            if (fogRenderer != null)
            {
                fogRenderer.gameObject.SetActive(isPlayerFogEnabled);
            }

            if (isPlayerFogEnabled)
            {
                RequestRefresh();
            }

            StrategyDebugLogger.Info(
                "Fog",
                "PlayerFogToggled",
                StrategyDebugLogger.F("enabled", isPlayerFogEnabled));
        }

        public bool IsWorldExplored(Vector3 world)
        {
            return map != null && map.TryWorldToCell(world, out Vector2Int cell) && IsCellExplored(cell);
        }

        public bool IsCellExplored(Vector2Int cell)
        {
            if (map != null && !isPlayerFogEnabled)
            {
                return IsCellInsideMap(cell);
            }

            return HasCellState(cell) && explored[cell.x, cell.y];
        }

        public bool IsCellVisible(Vector2Int cell)
        {
            if (map != null && !isPlayerFogEnabled)
            {
                return IsCellInsideMap(cell);
            }

            return HasCellState(cell) && visible[cell.x, cell.y];
        }

        public bool IsCellVisibleAtDaylightRange(Vector2Int cell)
        {
            if (map != null && !isPlayerFogEnabled)
            {
                return IsCellInsideMap(cell);
            }

            return HasCellState(cell) && daylightVisible != null && daylightVisible[cell.x, cell.y];
        }

        private void Update()
        {
            if (map == null)
            {
                return;
            }

            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = RefreshInterval;
            RefreshFog();
        }

        private void RefreshFog()
        {
            if (map == null)
            {
                return;
            }

            EnsureState();
            UpdateNightVisionTuning();
            UpdateWeatherFogTuning();
            Array.Clear(visible, 0, visible.Length);
            Array.Clear(daylightVisible, 0, daylightVisible.Length);
            Array.Clear(weatherFogBands, 0, weatherFogBands.Length);
            Array.Clear(visibilityStrength, 0, visibilityStrength.Length);

            CollectRevealSources();
            ApplyRevealSources();
            BuildWeatherFogBands();
            PaintFogTexture();
        }

        private void EnsureState()
        {
            if (map == null)
            {
                return;
            }

            if (explored == null
                || visible == null
                || daylightVisible == null
                || weatherFogBands == null
                || visibilityStrength == null
                || explored.GetLength(0) != map.Width
                || explored.GetLength(1) != map.Height)
            {
                explored = new bool[map.Width, map.Height];
                visible = new bool[map.Width, map.Height];
                daylightVisible = new bool[map.Width, map.Height];
                weatherFogBands = new byte[map.Width, map.Height];
                visibilityStrength = new float[map.Width, map.Height];
            }

            int textureWidth = map.Width * FogPixelsPerCell;
            int textureHeight = map.Height * FogPixelsPerCell;
            if (fogTexture == null || fogTexture.width != textureWidth || fogTexture.height != textureHeight)
            {
                if (fogTexture != null)
                {
                    Destroy(fogTexture);
                }

                fogTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
                {
                    name = "Strategy Fog Of War",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                fogPixels = new Color[textureWidth * textureHeight];
                EnsureRenderer(textureWidth, textureHeight);
            }
        }

        private void EnsureRenderer(int textureWidth, int textureHeight)
        {
            if (fogRenderer == null)
            {
                GameObject fogObject = new GameObject("Fog Overlay");
                fogObject.transform.SetParent(transform, false);
                fogRenderer = fogObject.AddComponent<SpriteRenderer>();
                fogRenderer.sortingOrder = FogSortingOrder;
                fogRenderer.gameObject.SetActive(isPlayerFogEnabled);
            }

            if (fogRenderer.sprite != null)
            {
                Destroy(fogRenderer.sprite);
            }

            float pixelsPerUnit = FogPixelsPerCell / map.CellSize;
            Sprite fogSprite = Sprite.Create(
                fogTexture,
                new Rect(0f, 0f, textureWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            fogSprite.name = "Strategy Fog Of War Sprite";
            fogRenderer.sprite = fogSprite;
            fogRenderer.color = Color.white;
            fogRenderer.transform.position = new Vector3(map.WorldBounds.center.x, map.WorldBounds.center.y, -0.02f);
            fogRenderer.transform.localScale = Vector3.one;
        }

        private void CollectRevealSources()
        {
            revealSources.Clear();

            if (population != null && population.TryGetCampCell(out Vector2Int campCell))
            {
                AddRevealSource(campCell, CampRevealRadius, RevealSourceKind.Camp);
            }

            IReadOnlyList<StrategyResidentAgent> residents = population != null ? population.Residents : null;
            if (residents != null)
            {
                for (int i = 0; i < residents.Count; i++)
                {
                    StrategyResidentAgent resident = residents[i];
                    if (resident != null && map.TryWorldToCell(resident.transform.position, out Vector2Int cell))
                    {
                        AddRevealSource(cell, ResidentRevealRadius, RevealSourceKind.Resident);
                    }
                }
            }

            IReadOnlyList<StrategyPlacedBuilding> buildings = placement != null ? placement.PlacedBuildings : null;
            if (buildings == null)
            {
                return;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building == null)
                {
                    continue;
                }

                Vector2 center = new Vector2(
                    building.Origin.x + building.Footprint.x * 0.5f,
                    building.Origin.y + building.Footprint.y * 0.5f);
                revealSources.Add(new RevealSource(center, BuildingRevealRadius, RevealSourceKind.Building));
            }
        }

        private void ApplyRevealSources()
        {
            for (int i = 0; i < revealSources.Count; i++)
            {
                RevealSource source = revealSources[i];
                float currentRadius = GetCurrentRevealRadius(source);
                float currentEdgeSoftness = GetCurrentEdgeSoftness(source);
                float daylightOuterRadius = source.Radius + RevealEdgeSoftness;
                int minX = Mathf.Max(0, Mathf.FloorToInt(source.CellCenter.x - daylightOuterRadius));
                int maxX = Mathf.Min(map.Width - 1, Mathf.CeilToInt(source.CellCenter.x + daylightOuterRadius));
                int minY = Mathf.Max(0, Mathf.FloorToInt(source.CellCenter.y - daylightOuterRadius));
                int maxY = Mathf.Min(map.Height - 1, Mathf.CeilToInt(source.CellCenter.y + daylightOuterRadius));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float dx = x + 0.5f - source.CellCenter.x;
                        float dy = y + 0.5f - source.CellCenter.y;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);

                        float daylightStrength = EvaluateRevealStrength(distance, source.Radius, RevealEdgeSoftness);
                        if (daylightStrength > VisibleThreshold)
                        {
                            daylightVisible[x, y] = true;
                        }

                        float currentStrength = EvaluateRevealStrength(distance, currentRadius, currentEdgeSoftness);
                        if (currentStrength <= 0f)
                        {
                            continue;
                        }

                        if (currentStrength > visibilityStrength[x, y])
                        {
                            visibilityStrength[x, y] = currentStrength;
                        }

                        if (currentStrength > VisibleThreshold)
                        {
                            visible[x, y] = true;
                            explored[x, y] = true;
                        }
                    }
                }
            }
        }

        private void PaintFogTexture()
        {
            if (fogTexture == null || fogPixels == null)
            {
                return;
            }

            int textureWidth = fogTexture.width;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Color color = GetFogCellColor(x, y);

                    int pixelX = x * FogPixelsPerCell;
                    int pixelY = y * FogPixelsPerCell;
                    for (int py = 0; py < FogPixelsPerCell; py++)
                    {
                        int row = (pixelY + py) * textureWidth + pixelX;
                        for (int px = 0; px < FogPixelsPerCell; px++)
                        {
                            fogPixels[row + px] = color;
                        }
                    }
                }
            }

            fogTexture.SetPixels(fogPixels);
            fogTexture.Apply(false, false);
        }

        private void AddRevealSource(Vector2Int cell, float radius, RevealSourceKind kind)
        {
            if (cell.x < 0 || cell.x >= map.Width || cell.y < 0 || cell.y >= map.Height)
            {
                return;
            }

            revealSources.Add(new RevealSource(new Vector2(cell.x + 0.5f, cell.y + 0.5f), radius, kind));
        }

        private bool HasCellState(Vector2Int cell)
        {
            return explored != null
                && cell.x >= 0
                && cell.x < explored.GetLength(0)
                && cell.y >= 0
                && cell.y < explored.GetLength(1);
        }

        private bool IsCellInsideMap(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < map.Width && cell.y >= 0 && cell.y < map.Height;
        }

        private void OnDestroy()
        {
            if (fogRenderer != null && fogRenderer.sprite != null)
            {
                Destroy(fogRenderer.sprite);
            }

            if (fogTexture != null)
            {
                Destroy(fogTexture);
                fogTexture = null;
            }
        }

    }
}
