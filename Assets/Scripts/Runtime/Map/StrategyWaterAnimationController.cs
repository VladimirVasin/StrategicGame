using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWaterAnimationController : MonoBehaviour
    {
        private const int PixelsPerCell = 8;
        private const int FrameCount = 8;
        private const float FrameDuration = 0.14f;

        private CityMapController map;
        private SpriteRenderer overlayRenderer;
        private Texture2D overlayTexture;
        private Color[] pixels;
        private int frameIndex;
        private float frameTimer;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            frameIndex = Mathf.Abs(map != null ? map.ActiveSeed : 0) % FrameCount;
            frameTimer = Random.Range(0f, FrameDuration);
            EnsureOverlay();
            PaintFrame();
        }

        private void Update()
        {
            if (map == null || overlayTexture == null)
            {
                return;
            }

            frameTimer += Time.deltaTime;
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

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!map.TryGetCell(x, y, out CityMapCell cell))
                    {
                        continue;
                    }

                    if (cell.Kind == CityMapCellKind.Water)
                    {
                        PaintWaterCell(x, y);
                    }
                    else if (cell.Kind == CityMapCellKind.Shore)
                    {
                        PaintShoreCell(x, y);
                    }
                }
            }

            overlayTexture.SetPixels(pixels);
            overlayTexture.Apply(false, false);
        }

        private void PaintWaterCell(int cellX, int cellY)
        {
            for (int py = 0; py < PixelsPerCell; py++)
            {
                for (int px = 0; px < PixelsPerCell; px++)
                {
                    float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 101);
                    int wave = PositiveModulo(py + cellX * 2 + cellY + frameIndex, 6);
                    if (wave == 0 && noise > 0.28f)
                    {
                        float alpha = 0.20f + noise * 0.22f;
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.63f, 0.86f, 0.95f, alpha));
                        continue;
                    }

                    int sparkle = PositiveModulo(px * 3 + py * 5 + frameIndex * 2 + cellX - cellY, 23);
                    if (sparkle == 0 && noise > 0.56f)
                    {
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.78f, 0.96f, 1f, 0.26f));
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
                    bool nearWater = (waterLeft && px <= 1)
                        || (waterRight && px >= PixelsPerCell - 2)
                        || (waterDown && py <= 1)
                        || (waterUp && py >= PixelsPerCell - 2);
                    if (!nearWater)
                    {
                        continue;
                    }

                    int ripple = PositiveModulo(px + py * 2 + frameIndex + cellX * 3 + cellY, 5);
                    if (ripple <= 1)
                    {
                        float noise = Hash01(map.ActiveSeed, cellX, cellY, px, py, 211);
                        SetOverlayPixel(cellX, cellY, px, py, new Color(0.86f, 0.94f, 0.86f, 0.18f + noise * 0.14f));
                    }
                }
            }
        }

        private bool IsWater(int x, int y)
        {
            return map.TryGetCell(x, y, out CityMapCell cell) && cell.Kind == CityMapCellKind.Water;
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
