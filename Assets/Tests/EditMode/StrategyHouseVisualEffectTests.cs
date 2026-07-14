using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyHouseVisualEffectTests
    {
        private static readonly Vector2[] ChimneyMouthPixels =
        {
            new(95f, 151f),
            new(99f, 151f),
            new(92f, 151f),
            new(92f, 151f),
            new(100f, 149f)
        };

        private static readonly RectInt[,] LowerWindowRects =
        {
            { new(62, 41, 8, 13), new(93, 39, 9, 15) },
            { new(66, 41, 6, 14), new(97, 40, 8, 15) },
            { new(60, 41, 7, 14), new(92, 40, 8, 14) },
            { new(62, 40, 6, 14), new(92, 39, 8, 14) },
            { new(67, 40, 6, 14), new(98, 39, 9, 14) }
        };

        private static readonly int[] RightWindowMullionOffsets = { 4, 3, 3, 3, 4 };

        [Test]
        public void SmokeOverlayUsesAuthoredChimneyAnchorsAndUnclippedSprite()
        {
            for (int variant = 0; variant < ChimneyMouthPixels.Length; variant++)
            {
                Texture2D authoredHouse = LoadAuthoredHouse(variant);
                GameObject house = new($"House V{variant + 1:00}");
                try
                {
                    SpriteRenderer baseRenderer = house.AddComponent<SpriteRenderer>();
                    baseRenderer.sortingOrder = 120;
                    StrategyHouseAmbientAnimator animator = house.AddComponent<StrategyHouseAmbientAnimator>();
                    animator.Configure(baseRenderer, variant);

                    Transform smoke = house.transform.Find("House Chimney Smoke");
                    Assert.That(smoke, Is.Not.Null, $"House V{variant + 1:00} smoke child is missing");
                    Vector2 anchoredPixels = new(
                        smoke.localPosition.x * 48f + 80f,
                        smoke.localPosition.y * 48f + 16f);
                    Assert.That(
                        Vector2.Distance(anchoredPixels, ChimneyMouthPixels[variant]),
                        Is.LessThan(0.001f),
                        $"House V{variant + 1:00} chimney anchor changed");
                    SpriteRenderer smokeRenderer = smoke.GetComponent<SpriteRenderer>();
                    Assert.That(smokeRenderer, Is.Not.Null);
                    Assert.That(smokeRenderer.sortingOrder, Is.EqualTo(121));

                    int capX = Mathf.FloorToInt(ChimneyMouthPixels[variant].x);
                    int mouthY = Mathf.CeilToInt(ChimneyMouthPixels[variant].y);
                    Assert.That(
                        authoredHouse.GetPixel(capX, mouthY - 1).a,
                        Is.GreaterThan(0f),
                        $"House V{variant + 1:00} chimney anchor is not above the cap");
                    Assert.That(
                        authoredHouse.GetPixel(capX, mouthY).a,
                        Is.EqualTo(0f),
                        $"House V{variant + 1:00} chimney anchor intersects the authored sprite");

                    for (int frame = 0; frame < StrategyHouseAmbientSpriteFactory.FrameCount; frame++)
                    {
                        Sprite smokeSprite = StrategyHouseAmbientSpriteFactory.GetSprite(variant, frame);
                        AssertSmokeSpriteIsUnclipped(smokeSprite, variant, frame);
                        Assert.That(
                            smokeSprite,
                            Is.SameAs(StrategyHouseAmbientSpriteFactory.GetSprite(0, frame)),
                            "Variant-independent smoke frames should share the cache");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(house);
                    Object.DestroyImmediate(authoredHouse);
                }
            }
        }

        [Test]
        public void WindowMasksAlignWithAuthoredLowerPanes()
        {
            for (int variant = 0; variant < ChimneyMouthPixels.Length; variant++)
            {
                Sprite mask = StrategyHouseAmbientSpriteFactory.GetWindowMaskSprite(variant);
                Texture2D authoredHouse = LoadAuthoredHouse(variant);
                try
                {
                    Assert.That(mask.rect.size, Is.EqualTo(new Vector2(160f, 160f)));
                    Assert.That(mask.pixelsPerUnit, Is.EqualTo(48f));
                    Assert.That(mask.pivot, Is.EqualTo(new Vector2(80f, 16f)));

                    AssertWindowMaskGeometry(mask.texture, authoredHouse, variant);
                    Assert.That(
                        mask.texture.GetPixel(
                            LowerWindowRects[variant, 1].x + RightWindowMullionOffsets[variant],
                            LowerWindowRects[variant, 1].yMin).a,
                        Is.EqualTo(0.30f).Within(0.01f),
                        $"House V{variant + 1:00} right-window mullion moved");
                    Assert.That(
                        mask.texture.GetPixel(
                            LowerWindowRects[variant, 1].x + RightWindowMullionOffsets[variant] + 1,
                            LowerWindowRects[variant, 1].yMin).a,
                        Is.EqualTo(0.30f).Within(0.01f),
                        $"House V{variant + 1:00} right-window mullion was not doubled");
                }
                finally
                {
                    Object.DestroyImmediate(authoredHouse);
                }
            }
        }

        private static void AssertSmokeSpriteIsUnclipped(Sprite sprite, int variant, int frame)
        {
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(32f, 24f)));
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(24f));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(16.5f, 0f)));

            RectInt bounds = GetAlphaBounds(sprite.texture);
            string context = $"House V{variant + 1:00} smoke frame {frame + 1}";
            Assert.That(bounds.yMin, Is.EqualTo(0), $"{context} detached from the chimney");
            Assert.That(bounds.xMin, Is.GreaterThan(0), $"{context} clipped on the left");
            Assert.That(bounds.xMax, Is.LessThan(sprite.texture.width), $"{context} clipped on the right");
            Assert.That(bounds.yMax, Is.LessThan(sprite.texture.height), $"{context} clipped at the top");
        }

        private static void AssertWindowMaskGeometry(Texture2D mask, Texture2D authoredHouse, int variant)
        {
            int maskedPixels = 0;
            int glassPixels = 0;
            for (int y = 0; y < mask.height; y++)
            {
                for (int x = 0; x < mask.width; x++)
                {
                    if (mask.GetPixel(x, y).a <= 0f)
                    {
                        continue;
                    }

                    Vector2Int pixel = new(x, y);
                    bool insidePane = LowerWindowRects[variant, 0].Contains(pixel)
                        || LowerWindowRects[variant, 1].Contains(pixel);
                    Assert.That(insidePane, Is.True, $"House V{variant + 1:00} has mask pixels outside its panes");

                    Color32 source = authoredHouse.GetPixel(x, y);
                    maskedPixels++;
                    if (source.g > source.r + 5
                        && source.b > source.r + 5
                        && source.g > 35
                        && source.b > 35)
                    {
                        glassPixels++;
                    }
                }
            }

            Assert.That(maskedPixels, Is.GreaterThan(0));
            Assert.That(
                (float)glassPixels / maskedPixels,
                Is.GreaterThanOrEqualTo(0.45f),
                $"House V{variant + 1:00} window mask no longer follows the authored glass");
        }

        private static RectInt GetAlphaBounds(Texture2D texture)
        {
            int minX = texture.width;
            int minY = texture.height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (texture.GetPixel(x, y).a <= 0f)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            Assert.That(maxX, Is.GreaterThanOrEqualTo(0), "Expected at least one visible pixel");
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Texture2D LoadAuthoredHouse(int variant)
        {
            string path = Path.Combine(
                Application.dataPath,
                $"Resources/Visual/Authored/Buildings/House/V{variant + 1:00}.png");
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            Assert.That(
                texture.LoadImage(File.ReadAllBytes(path), false),
                Is.True,
                $"Could not load authored House V{variant + 1:00}");
            return texture;
        }
    }
}
