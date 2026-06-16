using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyWorldEffectKind
    {
        Dust,
        Sawdust,
        StoneChips,
        CoalChips,
        IronSparks,
        WaterSplash
    }

    [DisallowMultipleComponent]
    public sealed class StrategyWorldEffectAnimator : MonoBehaviour
    {
        private const int FrameCount = 5;
        private const float FrameDuration = 0.07f;

        private SpriteRenderer spriteRenderer;
        private StrategyWorldEffectKind kind;
        private StrategyResourceType resource;
        private Vector3 drift;
        private bool resourcePop;
        private int variant;
        private int frame;
        private float frameTimer;
        private float scale;

        public static void Spawn(
            StrategyWorldEffectKind effectKind,
            Vector3 world,
            int sortingOrder,
            int seed = 0,
            float effectScale = 1f)
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
            {
                return;
            }

            GameObject effect = new GameObject("World " + effectKind + " Effect");
            effect.transform.position = world;

            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            StrategyWorldEffectAnimator animator = effect.AddComponent<StrategyWorldEffectAnimator>();
            animator.ConfigureEffect(renderer, effectKind, seed, effectScale);
        }

        public static void SpawnResourcePlaced(
            StrategyResourceType placedResource,
            Vector3 world,
            int sortingOrder,
            int amount = 1,
            int seed = 0)
        {
            if (placedResource == StrategyResourceType.None || amount <= 0)
            {
                return;
            }

            SpawnResourcePop(placedResource, world + new Vector3(0f, 0.12f, -0.02f), sortingOrder + 2, amount, seed);
            Spawn(GetDropEffectKind(placedResource), world, sortingOrder + 1, seed + amount * 17, GetDropEffectScale(placedResource, amount));
        }

        public static void SpawnConstructionResourcePlaced(
            StrategyConstructionResourceKind placedResource,
            Vector3 world,
            int sortingOrder,
            int amount = 1,
            int seed = 0)
        {
            SpawnResourcePlaced(ToResourceType(placedResource), world, sortingOrder, amount, seed);
        }

        public static void SpawnConstructionHit(Vector3 world, int sortingOrder, int seed = 0)
        {
            Spawn(StrategyWorldEffectKind.Dust, world, sortingOrder, seed, 0.85f);
            if (Mathf.Abs(seed) % 3 == 0)
            {
                Spawn(StrategyWorldEffectKind.IronSparks, world + new Vector3(0.02f, 0.06f, -0.01f), sortingOrder + 1, seed + 41, 0.70f);
            }
        }

        private static void SpawnResourcePop(
            StrategyResourceType placedResource,
            Vector3 world,
            int sortingOrder,
            int amount,
            int seed)
        {
            if (!StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects || GetResourceSprite(placedResource) == null)
            {
                return;
            }

            GameObject effect = new GameObject("World " + placedResource + " Pop");
            effect.transform.position = world;

            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            StrategyWorldEffectAnimator animator = effect.AddComponent<StrategyWorldEffectAnimator>();
            animator.ConfigureResourcePop(renderer, placedResource, amount, seed);
        }

        private void ConfigureEffect(
            SpriteRenderer renderer,
            StrategyWorldEffectKind effectKind,
            int seed,
            float effectScale)
        {
            spriteRenderer = renderer;
            kind = effectKind;
            resource = StrategyResourceType.None;
            resourcePop = false;
            variant = Mathf.Abs(seed) % 11;
            frame = 0;
            frameTimer = 0f;
            scale = Mathf.Max(0.25f, effectScale);
            drift = GetEffectDrift(effectKind, variant);
            ApplyFrame();
        }

        private void ConfigureResourcePop(
            SpriteRenderer renderer,
            StrategyResourceType placedResource,
            int amount,
            int seed)
        {
            spriteRenderer = renderer;
            kind = StrategyWorldEffectKind.Dust;
            resource = placedResource;
            resourcePop = true;
            variant = Mathf.Abs(seed) % 11;
            frame = 0;
            frameTimer = 0f;
            scale = Mathf.Clamp(0.55f + amount * 0.08f, 0.60f, 0.90f);
            drift = new Vector3(((variant % 3) - 1) * 0.025f, 0.20f + (variant % 2) * 0.04f, 0f);
            ApplyFrame();
        }

        private void Update()
        {
            transform.position += drift * Time.deltaTime;
            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
            {
                return;
            }

            frameTimer -= FrameDuration;
            frame++;
            if (frame >= FrameCount)
            {
                Destroy(gameObject);
                return;
            }

            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            float progress = frame / (float)(FrameCount - 1);
            if (resourcePop)
            {
                spriteRenderer.sprite = GetResourceSprite(resource);
                spriteRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.95f, 0.05f, progress));
                transform.localScale = Vector3.one * Mathf.Lerp(scale, scale * 1.25f, progress);
                return;
            }

            spriteRenderer.sprite = StrategyWorldEffectSpriteFactory.GetSprite(kind, variant, frame);
            spriteRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.95f, 0.08f, progress));
            transform.localScale = Vector3.one * Mathf.Lerp(scale, scale * 1.08f, progress);
        }

        private static StrategyWorldEffectKind GetDropEffectKind(StrategyResourceType placedResource)
        {
            return placedResource switch
            {
                StrategyResourceType.Logs => StrategyWorldEffectKind.Sawdust,
                StrategyResourceType.Planks => StrategyWorldEffectKind.Sawdust,
                StrategyResourceType.Stone => StrategyWorldEffectKind.StoneChips,
                StrategyResourceType.Iron => StrategyWorldEffectKind.IronSparks,
                StrategyResourceType.Coal => StrategyWorldEffectKind.CoalChips,
                StrategyResourceType.Fish => StrategyWorldEffectKind.WaterSplash,
                _ => StrategyWorldEffectKind.Dust
            };
        }

        private static float GetDropEffectScale(StrategyResourceType placedResource, int amount)
        {
            float baseScale = placedResource == StrategyResourceType.Fish ? 0.70f : 0.82f;
            return Mathf.Clamp(baseScale + amount * 0.05f, 0.70f, 1.15f);
        }

        private static StrategyResourceType ToResourceType(StrategyConstructionResourceKind resourceKind)
        {
            return resourceKind switch
            {
                StrategyConstructionResourceKind.Logs => StrategyResourceType.Logs,
                StrategyConstructionResourceKind.Stone => StrategyResourceType.Stone,
                StrategyConstructionResourceKind.Planks => StrategyResourceType.Planks,
                _ => StrategyResourceType.None
            };
        }

        private static Vector3 GetEffectDrift(StrategyWorldEffectKind effectKind, int effectVariant)
        {
            float x = ((effectVariant % 3) - 1) * 0.05f;
            float y = effectKind == StrategyWorldEffectKind.WaterSplash ? 0.10f : 0.18f;
            return new Vector3(x, y + (effectVariant % 2) * 0.04f, 0f);
        }

        private static Sprite GetResourceSprite(StrategyResourceType placedResource)
        {
            return placedResource switch
            {
                StrategyResourceType.Logs => StrategyNatureSpriteFactory.GetCarriedLogsSprite(),
                StrategyResourceType.Stone => StrategyNatureSpriteFactory.GetCarriedStoneSprite(),
                StrategyResourceType.Iron => StrategyNatureSpriteFactory.GetCarriedIronSprite(),
                StrategyResourceType.Coal => StrategyNatureSpriteFactory.GetCarriedCoalSprite(),
                StrategyResourceType.Planks => StrategyNatureSpriteFactory.GetCarriedPlanksSprite(),
                StrategyResourceType.Game => StrategyNatureSpriteFactory.GetCarriedGameSprite(),
                StrategyResourceType.Fish => StrategyNatureSpriteFactory.GetCarriedFishSprite(),
                StrategyResourceType.Berries => StrategyForageSpriteFactory.GetCarriedSprite(placedResource),
                StrategyResourceType.Roots => StrategyForageSpriteFactory.GetCarriedSprite(placedResource),
                StrategyResourceType.Mushrooms => StrategyForageSpriteFactory.GetCarriedSprite(placedResource),
                _ => null
            };
        }
    }

    internal static class StrategyWorldEffectSpriteFactory
    {
        private const int FrameCount = 5;
        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(StrategyWorldEffectKind kind, int variant, int frame)
        {
            int normalizedVariant = Mathf.Abs(variant) % 11;
            int normalizedFrame = Mathf.Abs(frame) % FrameCount;
            int key = ((int)kind * 1024) + normalizedVariant * 32 + normalizedFrame;
            if (!CachedSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(kind, normalizedVariant, normalizedFrame);
                CachedSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyWorldEffectKind kind, int variant, int frame)
        {
            const int width = 42;
            const int height = 30;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "World " + kind + " Effect " + variant + "-" + frame,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            DrawEffect(texture, kind, variant, frame);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.20f), 32f);
        }

        private static void DrawEffect(Texture2D texture, StrategyWorldEffectKind kind, int variant, int frame)
        {
            Color primary = GetPrimaryColor(kind);
            Color secondary = GetSecondaryColor(kind);
            Color haze = GetHazeColor(kind);
            int spread = 2 + frame * 3;
            int rise = frame * 2;
            int particles = kind == StrategyWorldEffectKind.WaterSplash ? 12 : 15;

            for (int i = 0; i < particles; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                int x = 21 + side * (2 + ((variant * 7 + i * 5 + frame * 3) % Mathf.Max(3, spread + 3)));
                int y = 7 + rise + ((variant * 11 + i * 3) % 9);
                Color color = i % 4 == 0 ? secondary : primary;
                SetPixelSafe(texture, x, y, color);
                if (i % 5 == frame % 5)
                {
                    SetPixelSafe(texture, x + side, y + 1, color);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                int x = 13 + ((variant * 13 + i * 5 + frame * 4) % 18);
                int y = 5 + ((variant * 3 + i * 2 + frame) % 7);
                SetPixelSafe(texture, x, y, haze);
                if (frame < 3)
                {
                    SetPixelSafe(texture, x + 1, y, haze);
                }
            }
        }

        private static Color GetPrimaryColor(StrategyWorldEffectKind kind)
        {
            return kind switch
            {
                StrategyWorldEffectKind.Sawdust => Rgb(206, 145, 70),
                StrategyWorldEffectKind.StoneChips => Rgb(150, 157, 149),
                StrategyWorldEffectKind.CoalChips => Rgb(56, 53, 50),
                StrategyWorldEffectKind.IronSparks => Rgb(238, 157, 63),
                StrategyWorldEffectKind.WaterSplash => new Color(0.55f, 0.82f, 0.96f, 0.78f),
                _ => new Color(0.62f, 0.54f, 0.40f, 0.58f)
            };
        }

        private static Color GetSecondaryColor(StrategyWorldEffectKind kind)
        {
            return kind switch
            {
                StrategyWorldEffectKind.Sawdust => Rgb(121, 78, 42),
                StrategyWorldEffectKind.StoneChips => Rgb(214, 218, 204),
                StrategyWorldEffectKind.CoalChips => Rgb(96, 91, 86),
                StrategyWorldEffectKind.IronSparks => Rgb(255, 226, 122),
                StrategyWorldEffectKind.WaterSplash => new Color(0.82f, 0.94f, 1f, 0.70f),
                _ => new Color(0.78f, 0.69f, 0.50f, 0.42f)
            };
        }

        private static Color GetHazeColor(StrategyWorldEffectKind kind)
        {
            return kind switch
            {
                StrategyWorldEffectKind.WaterSplash => new Color(0.45f, 0.70f, 0.90f, 0.22f),
                StrategyWorldEffectKind.IronSparks => new Color(0.95f, 0.56f, 0.18f, 0.20f),
                StrategyWorldEffectKind.CoalChips => new Color(0.12f, 0.11f, 0.10f, 0.35f),
                _ => new Color(0.66f, 0.58f, 0.44f, 0.30f)
            };
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
