using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWaterAnimationController : MonoBehaviour
    {
        private const int PixelsPerCell = 4;
        private const int FrameCount = 8;
        private const float FrameDuration = 0.22f;

        private CityMapController map;
        private readonly List<Vector2Int> animatedCells = new();
        private SpriteRenderer overlayRenderer;
        private Texture2D overlayTexture;
        private Color[] pixels;
        private int frameIndex;
        private float frameTimer;
        private float rainRippleIntensity;
        private float stormRippleIntensity;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            frameIndex = Mathf.Abs(map != null ? map.ActiveSeed : 0) % FrameCount;
            frameTimer = UnityEngine.Random.Range(0f, FrameDuration);
            EnsureOverlay();
            RebuildAnimatedCells();
            PaintFrame();
        }

        private void Update()
        {
            if (map == null || overlayTexture == null)
            {
                return;
            }

            frameTimer += Time.unscaledDeltaTime;
            if (frameTimer < FrameDuration)
            {
                return;
            }

            frameTimer -= FrameDuration;
            frameIndex = (frameIndex + 1) % FrameCount;
            PaintFrame();
        }

        private void EnsureOverlay()
        {
            if (map == null)
            {
                return;
            }

            int textureWidth = map.Width * PixelsPerCell;
            int textureHeight = map.Height * PixelsPerCell;
            bool recreateSprite = false;
            if (overlayTexture == null
                || overlayTexture.width != textureWidth
                || overlayTexture.height != textureHeight)
            {
                if (overlayTexture != null)
                {
                    Destroy(overlayTexture);
                }

                if (overlayRenderer != null && overlayRenderer.sprite != null)
                {
                    Destroy(overlayRenderer.sprite);
                    overlayRenderer.sprite = null;
                }

                overlayTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
                {
                    name = "Strategy Water Animation Overlay",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                pixels = new Color[textureWidth * textureHeight];
                recreateSprite = true;
            }

            if (overlayRenderer == null)
            {
                GameObject overlayObject = new GameObject("Water Animation Overlay");
                overlayObject.transform.SetParent(transform, false);
                overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
                overlayRenderer.sortingOrder = StrategyWorldSorting.WaterOverlayOrder;
            }

            if (overlayRenderer.sprite == null || recreateSprite)
            {
                float pixelsPerUnit = PixelsPerCell / map.CellSize;
                Sprite sprite = Sprite.Create(
                    overlayTexture,
                    new Rect(0f, 0f, textureWidth, textureHeight),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit);
                sprite.name = "Strategy Water Animation Overlay Sprite";
                overlayRenderer.sprite = sprite;
            }

            overlayRenderer.color = Color.white;
            overlayRenderer.transform.position = new Vector3(map.WorldBounds.center.x, map.WorldBounds.center.y, -0.04f);
            overlayRenderer.transform.localScale = Vector3.one;
        }

        private void PaintFrame()
        {
            if (map == null || overlayTexture == null || pixels == null)
            {
                return;
            }

            StrategyWeatherController weather = StrategyWeatherController.Active;
            rainRippleIntensity = weather != null ? weather.RainIntensity : 0f;
            stormRippleIntensity = weather != null ? weather.StormIntensity : 0f;

            Array.Clear(pixels, 0, pixels.Length);

            for (int i = 0; i < animatedCells.Count; i++)
            {
                Vector2Int cellPosition = animatedCells[i];
                if (!map.TryGetCell(cellPosition.x, cellPosition.y, out CityMapCell cell))
                {
                    continue;
                }

                if (cell.Kind == CityMapCellKind.Water)
                {
                    PaintWaterCell(cellPosition.x, cellPosition.y, cell);
                }
                else if (cell.Kind == CityMapCellKind.Shore)
                {
                    PaintShoreCell(cellPosition.x, cellPosition.y);
                }
            }

            overlayTexture.SetPixels(pixels);
            overlayTexture.Apply(false, false);
        }

        private void RebuildAnimatedCells()
        {
            animatedCells.Clear();
            if (map == null)
            {
                return;
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.TryGetCell(x, y, out CityMapCell cell)
                        && (cell.Kind == CityMapCellKind.Water || cell.Kind == CityMapCellKind.Shore))
                    {
                        animatedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        private void PaintWaterCell(int cellX, int cellY, CityMapCell cell)
        {
            PaintWaterDepthTint(cellX, cellY, cell);
            if (cell.IsRiver)
            {
                PaintRiverWaterCell(cellX, cellY);
            }
            else
            {
                PaintLakeWaterCell(cellX, cellY);
            }

            PaintWaterEdgeFoam(cellX, cellY, cell);
        }

        private void PaintWaterDepthTint(int cellX, int cellY, CityMapCell cell)
        {
            int waterNeighbors = CountWaterNeighbors(cellX, cellY);
            int shoreNeighbors = CountShoreNeighbors(cellX, cellY);
            bool shallow = waterNeighbors <= 4 || shoreNeighbors > 0;
            Color shallowColor = cell.IsRiver
                ? new Color(0.24f, 0.62f, 0.68f, 0.055f + rainRippleIntensity * 0.020f)
                : new Color(0.23f, 0.58f, 0.66f, 0.065f + rainRippleIntensity * 0.018f);
            Color deepColor = cell.IsRiver
                ? new Color(0.06f, 0.22f, 0.34f, 0.105f + stormRippleIntensity * 0.045f)
                : new Color(0.05f, 0.18f, 0.32f, 0.120f + stormRippleIntensity * 0.038f);
            Color baseColor = shallow ? shallowColor : deepColor;
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 317);
                    float edgeLift = shallow ? 0.020f * noise : -0.018f * noise;
                    Color color = baseColor;
                    color.a = Mathf.Clamp01(color.a + edgeLift);
                    SetOverlayPixel(cellX, cellY, px, py, color);
                }
            }
        }

        private void PaintWaterEdgeFoam(int cellX, int cellY, CityMapCell cell)
        {
            bool shoreLeft = IsShoreLike(cellX - 1, cellY);
            bool shoreRight = IsShoreLike(cellX + 1, cellY);
            bool shoreDown = IsShoreLike(cellX, cellY - 1);
            bool shoreUp = IsShoreLike(cellX, cellY + 1);
            if (!shoreLeft && !shoreRight && !shoreDown && !shoreUp)
            {
                return;
            }

            float weatherBoost = rainRippleIntensity * 0.06f + stormRippleIntensity * 0.10f;
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    int edgeDistance = GetNearestFoamEdgeDistance(px, py, shoreLeft, shoreRight, shoreDown, shoreUp);
                    if (edgeDistance > 2)
                    {
                        continue;
                    }

                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 409);
                    int brokenLine = PositiveModulo(px * 3 + py * 5 + frameIndex + cellX * 7 - cellY * 2, 7);
                    if (brokenLine > 1 || noise < 0.26f + edgeDistance * 0.14f)
                    {
                        continue;
                    }

                    float alpha = (edgeDistance == 0 ? 0.30f : 0.18f) + weatherBoost + noise * 0.10f;
                    Color foam = cell.IsRiver
                        ? new Color(0.86f, 0.96f, 0.95f, alpha)
                        : new Color(0.82f, 0.94f, 0.88f, alpha * 0.85f);
                    SetOverlayPixel(cellX, cellY, px, py, foam);
                }
            }
        }

        private void PaintLakeWaterCell(int cellX, int cellY)
        {
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 101);
                    int wave = PositiveModulo(py + cellX * 2 + cellY + frameIndex, 6);
                    if (wave == 0 && noise > 0.24f)
                    {
                        float alpha = 0.21f + noise * 0.24f + rainRippleIntensity * 0.08f;
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.64f, 0.88f, 0.96f, alpha));
                        continue;
                    }

                    int sparkle = PositiveModulo(px * 3 + py * 5 + frameIndex * 2 + cellX - cellY, 23);
                    if (sparkle == 0 && noise > 0.56f)
                    {
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.78f, 0.96f, 1f, 0.26f));
                    }

                    if (rainRippleIntensity > 0.08f)
                    {
                        int rainHit = PositiveModulo(px * 7 + py * 11 + frameIndex * 5 + cellX * 13 - cellY * 3, Mathf.RoundToInt(Mathf.Lerp(29f, 9f, rainRippleIntensity)));
                        if (rainHit == 0 && noise > 0.46f)
                        {
                            float alpha = 0.08f + rainRippleIntensity * 0.16f + stormRippleIntensity * 0.05f;
                            SetOverlayPixel(cellX, cellY, px, py, new Color(0.82f, 0.96f, 1f, alpha));
                        }
                    }
                }
            }
        }

        private void PaintRiverWaterCell(int cellX, int cellY)
        {
            Vector2Int flow = map != null ? map.RiverFlowDirection : Vector2Int.right;
            bool horizontal = Mathf.Abs(flow.x) >= Mathf.Abs(flow.y);
            int flowSign = horizontal
                ? flow.x >= 0 ? 1 : -1
                : flow.y >= 0 ? 1 : -1;

            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 101);
                    int worldPx = cellX * PixelsPerCell + px;
                    int worldPy = cellY * PixelsPerCell + py;
                    int along = horizontal ? worldPx : worldPy;
                    int across = horizontal ? worldPy : worldPx;
                    int wave = PositiveModulo(along * flowSign - frameIndex + across / 3, 7);
                    if (wave <= (stormRippleIntensity > 0.35f ? 1 : 0) && noise > 0.18f)
                    {
                        float alpha = 0.24f + noise * 0.27f + rainRippleIntensity * 0.06f + stormRippleIntensity * 0.06f;
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.64f, 0.90f, 1f, alpha));
                        continue;
                    }

                    int currentFleck = PositiveModulo(along * flowSign - frameIndex * 2 + across * 5 + cellX - cellY, 23);
                    if (currentFleck == 0 && noise > 0.50f)
                    {
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.80f, 0.97f, 1f, 0.34f));
                    }

                    if (rainRippleIntensity > 0.08f)
                    {
                        int rainHit = PositiveModulo(along * 3 + across * 7 + frameIndex * 4 + cellX * 5, Mathf.RoundToInt(Mathf.Lerp(31f, 10f, rainRippleIntensity)));
                        if (rainHit == 0 && noise > 0.42f)
                        {
                            float alpha = 0.08f + rainRippleIntensity * 0.14f + stormRippleIntensity * 0.06f;
                            SetOverlayPixel(cellX, cellY, px, py, new Color(0.82f, 0.97f, 1f, alpha));
                        }
                    }
                }
            }
        }

        private void PaintShoreCell(int cellX, int cellY)
        {
            bool waterLeft = IsWater(cellX - 1, cellY);
            bool waterRight = IsWater(cellX + 1, cellY);
            bool waterDown = IsWater(cellX, cellY - 1);
            bool waterUp = IsWater(cellX, cellY + 1);
            if (!waterLeft && !waterRight && !waterDown && !waterUp)
            {
                return;
            }

            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    int edgeDistance = GetNearestFoamEdgeDistance(px, py, waterLeft, waterRight, waterDown, waterUp);
                    if (edgeDistance > 2)
                    {
                        continue;
                    }

                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 211);
                    float wetAlpha = 0.10f + rainRippleIntensity * 0.10f + stormRippleIntensity * 0.08f + noise * 0.04f;
                    SetOverlayPixel(cellX, cellY, px, py, new Color(0.24f, 0.28f, 0.18f, wetAlpha));

                    int ripple = PositiveModulo(px + py * 2 + frameIndex + cellX * 3 + cellY, 6);
                    if (ripple <= 1 && noise > 0.34f + edgeDistance * 0.10f)
                    {
                        float foamAlpha = 0.15f + noise * 0.12f + rainRippleIntensity * 0.05f;
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.84f, 0.92f, 0.82f, foamAlpha));
                    }
                }
            }
        }

        private bool IsWater(int x, int y)
        {
            return map.TryGetCell(x, y, out CityMapCell cell) && cell.Kind == CityMapCellKind.Water;
        }

        private bool IsShoreLike(int x, int y)
        {
            return map.TryGetCell(x, y, out CityMapCell cell) && cell.Kind != CityMapCellKind.Water;
        }

        private int CountWaterNeighbors(int cellX, int cellY)
        {
            int count = 0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if ((x != 0 || y != 0) && IsWater(cellX + x, cellY + y))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountShoreNeighbors(int cellX, int cellY)
        {
            int count = 0;
            count += IsShoreLike(cellX - 1, cellY) ? 1 : 0;
            count += IsShoreLike(cellX + 1, cellY) ? 1 : 0;
            count += IsShoreLike(cellX, cellY - 1) ? 1 : 0;
            count += IsShoreLike(cellX, cellY + 1) ? 1 : 0;
            return count;
        }

        private static int GetNearestFoamEdgeDistance(
            int px,
            int py,
            bool shoreLeft,
            bool shoreRight,
            bool shoreDown,
            bool shoreUp)
        {
            int distance = PixelsPerCell;
            if (shoreLeft)
            {
                distance = Mathf.Min(distance, px);
            }

            if (shoreRight)
            {
                distance = Mathf.Min(distance, PixelsPerCell - 1 - px);
            }

            if (shoreDown)
            {
                distance = Mathf.Min(distance, py);
            }

            if (shoreUp)
            {
                distance = Mathf.Min(distance, PixelsPerCell - 1 - py);
            }

            return distance;
        }

        private void SetOverlayPixel(int cellX, int cellY, int px, int py, Color color)
        {
            int textureX = cellX * PixelsPerCell + px;
            int textureY = cellY * PixelsPerCell + py;
            int index = textureY * overlayTexture.width + textureX;
            if (index >= 0 && index < pixels.Length)
            {
                pixels[index] = color;
            }
        }

        private void OnDestroy()
        {
            if (overlayRenderer != null && overlayRenderer.sprite != null)
            {
                Destroy(overlayRenderer.sprite);
            }

            if (overlayTexture != null)
            {
                Destroy(overlayTexture);
                overlayTexture = null;
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static float Hash01(int seed, int a, int b, int c, int d, int salt)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h = h * 1597334677 + d * 381201580;
                h ^= salt * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & int.MaxValue) / (float)int.MaxValue;
            }
        }
    }
}
