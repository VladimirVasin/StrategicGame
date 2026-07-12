using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWindSway : MonoBehaviour
    {
        private static readonly Dictionary<Sprite, SplitSprites> SplitCache = new();

        private StrategyWindController windController;
        private SpriteRenderer baseRenderer;
        private SpriteRenderer foliageRenderer;
        private Transform foliageTransform;
        private Sprite sourceSprite;
        private Sprite stableSprite;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private Quaternion baseLocalRotation;
        private float phase;
        private float bendDegrees;
        private float offsetAmplitude;
        private float stretchAmplitude;

        public Transform FoliageTransform => foliageTransform;

        public void Configure(
            StrategyWindController controller,
            float swayPhase,
            float maxBendDegrees,
            float maxOffsetAmplitude,
            float maxStretchAmplitude)
        {
            windController = controller;
            phase = swayPhase;
            bendDegrees = maxBendDegrees;
            offsetAmplitude = maxOffsetAmplitude;
            stretchAmplitude = maxStretchAmplitude;
            baseRenderer = GetComponent<SpriteRenderer>();
            EnsureFoliageLayer();
            CaptureBaseTransform();
        }

        private void Awake()
        {
            baseRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (baseRenderer != null && baseRenderer.sprite != stableSprite)
            {
                // Growth and felling replace the source sprite at runtime.
                EnsureFoliageLayer();
            }

            if (foliageRenderer == null || foliageTransform == null)
            {
                return;
            }

            SyncRendererSettings();
            if (windController == null)
            {
                windController = StrategyWindController.Active;
            }

            WindZone windZone = windController != null ? windController.WindZone : null;
            if (windController == null || windZone == null)
            {
                ResetFoliageTransform();
                return;
            }

            Vector2 direction = windController.PlanarDirection;
            float time = Time.time;
            float pulseSpeed = Mathf.Max(0.05f, windZone.windPulseFrequency) * 2.2f;
            float turbulence = Mathf.Max(0f, windZone.windTurbulence);
            float pulse = Mathf.Sin(time * pulseSpeed + phase);
            float gust = Mathf.PerlinNoise(phase * 0.37f, time * (0.18f + turbulence * 0.22f)) - 0.5f;
            float strength = Mathf.Max(0f, windZone.windMain + windZone.windPulseMagnitude * pulse + turbulence * gust);
            float flutter = Mathf.Sin(time * (pulseSpeed * 2.7f + 0.35f) + phase * 1.7f) * turbulence;
            float sway = (pulse + flutter * 0.38f) * strength;
            float lean = direction.x * strength * 0.32f;

            foliageTransform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, (sway + lean) * bendDegrees);
            foliageTransform.localPosition = baseLocalPosition + new Vector3(direction.x * sway * offsetAmplitude, 0f, 0f);
            float stretch = 1f + Mathf.Abs(sway) * stretchAmplitude;
            foliageTransform.localScale = new Vector3(
                baseLocalScale.x * (1f + Mathf.Abs(sway) * stretchAmplitude * 0.35f),
                baseLocalScale.y * stretch,
                baseLocalScale.z);
        }

        private void OnDisable()
        {
            ResetFoliageTransform();
        }

        public void RefreshVisual()
        {
            EnsureFoliageLayer();
        }

        private void EnsureFoliageLayer()
        {
            if (baseRenderer == null || baseRenderer.sprite == null)
            {
                return;
            }

            Sprite candidate = baseRenderer.sprite;
            if (sourceSprite != null && candidate == GetSplit(sourceSprite).Stable)
            {
                candidate = sourceSprite;
            }

            sourceSprite = candidate;
            SplitSprites split = GetSplit(sourceSprite);
            stableSprite = split.Stable;
            baseRenderer.sprite = stableSprite;

            if (foliageRenderer == null)
            {
                GameObject foliage = new GameObject("Wind Sway Foliage");
                foliage.transform.SetParent(transform, false);
                foliageTransform = foliage.transform;
                foliageRenderer = foliage.AddComponent<SpriteRenderer>();
            }

            foliageRenderer.sprite = split.Foliage;
            SyncRendererSettings();
        }

        private void SyncRendererSettings()
        {
            foliageRenderer.color = baseRenderer.color;
            foliageRenderer.flipX = baseRenderer.flipX;
            foliageRenderer.flipY = baseRenderer.flipY;
            foliageRenderer.sortingLayerID = baseRenderer.sortingLayerID;
            foliageRenderer.sortingOrder = baseRenderer.sortingOrder + 1;
            foliageRenderer.sharedMaterial = baseRenderer.sharedMaterial;
        }

        private void CaptureBaseTransform()
        {
            if (foliageTransform == null)
            {
                return;
            }

            baseLocalPosition = foliageTransform.localPosition;
            baseLocalScale = foliageTransform.localScale;
            baseLocalRotation = foliageTransform.localRotation;
        }

        private void ResetFoliageTransform()
        {
            if (foliageTransform == null)
            {
                return;
            }

            foliageTransform.localPosition = baseLocalPosition;
            foliageTransform.localScale = baseLocalScale;
            foliageTransform.localRotation = baseLocalRotation;
        }

        private static SplitSprites GetSplit(Sprite sprite)
        {
            if (SplitCache.TryGetValue(sprite, out SplitSprites cached))
            {
                return cached;
            }

            Rect rect = sprite.rect;
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            Color[] source = ReadSpritePixels(sprite, width, height);
            bool[] foliageMask = new bool[source.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color color = source[y * width + x];
                    foliageMask[y * width + x] = IsFoliageColor(color);
                }
            }

            bool[] expandedMask = (bool[])foliageMask.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!foliageMask[y * width + x])
                    {
                        continue;
                    }

                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int nx = x + ox;
                            int ny = y + oy;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height
                                && source[ny * width + nx].a > 0.01f)
                            {
                                expandedMask[ny * width + nx] = true;
                            }
                        }
                    }
                }
            }

            Color[] stable = new Color[source.Length];
            Color[] foliage = new Color[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                if (expandedMask[i])
                {
                    foliage[i] = source[i];
                }
                else
                {
                    stable[i] = source[i];
                }
            }

            cached = new SplitSprites(
                CreateSprite(sprite, stable, width, height, "Stable"),
                CreateSprite(sprite, foliage, width, height, "Foliage"));
            SplitCache[sprite] = cached;
            return cached;
        }

        private static Color[] ReadSpritePixels(Sprite sprite, int width, int height)
        {
            Rect rect = sprite.rect;
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            if (sprite.texture.isReadable)
            {
                return sprite.texture.GetPixels(x, y, width, height);
            }

            RenderTexture temporary = RenderTexture.GetTemporary(
                sprite.texture.width,
                sprite.texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(sprite.texture, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(x, y, width, height), 0, 0, false);
                readable.Apply(false, false);
                return readable.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (readable != null)
                {
                    Destroy(readable);
                }
            }
        }

        private static bool IsFoliageColor(Color color)
        {
            if (color.a <= 0.01f)
            {
                return false;
            }

            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return hue >= 0.15f && hue <= 0.48f && saturation >= 0.18f && value >= 0.12f;
        }

        private static Sprite CreateSprite(Sprite source, Color[] pixels, int width, int height, string suffix)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{source.name} {suffix}",
                filterMode = source.texture.filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            Vector2 pivot = new Vector2(
                source.pivot.x / source.rect.width,
                source.pivot.y / source.rect.height);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, source.pixelsPerUnit);
        }

        private readonly struct SplitSprites
        {
            public SplitSprites(Sprite stable, Sprite foliage)
            {
                Stable = stable;
                Foliage = foliage;
            }

            public Sprite Stable { get; }
            public Sprite Foliage { get; }
        }
    }
}
