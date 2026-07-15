using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyBuildingSnowSpriteFactoryTests
    {
        private readonly List<Sprite> sprites = new();
        private readonly List<Texture2D> textures = new();

        [SetUp]
        public void SetUp()
        {
            StrategyBuildingSnowSpriteFactory.ResetCache();
        }

        [TearDown]
        public void TearDown()
        {
            StrategyBuildingSnowSpriteFactory.ResetCache();
            for (int i = sprites.Count - 1; i >= 0; i--)
            {
                if (sprites[i] != null)
                {
                    Object.DestroyImmediate(sprites[i]);
                }
            }

            for (int i = textures.Count - 1; i >= 0; i--)
            {
                if (textures[i] != null)
                {
                    Object.DestroyImmediate(textures[i]);
                }
            }

            sprites.Clear();
            textures.Clear();
        }

        [Test]
        public void NonReadableSourceUsesExactAlphaAndPreservesSpriteGeometry()
        {
            const int textureWidth = 16;
            const int textureHeight = 12;
            const int startX = 3;
            const int startY = 2;
            const int spriteWidth = 10;
            const int spriteHeight = 8;
            Color32[] sourcePixels = CreatePixels(textureWidth, textureHeight);
            for (int x = 0; x < spriteWidth; x++)
            {
                if (x == 4)
                {
                    continue;
                }

                int top = 4 + x % 3;
                for (int y = 1; y <= top; y++)
                {
                    sourcePixels[(startY + y) * textureWidth + startX + x] =
                        new Color32(90, 120, 80, 255);
                }
            }

            Texture2D sourceTexture = Track(new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false));
            sourceTexture.filterMode = FilterMode.Point;
            sourceTexture.SetPixels32(sourcePixels);
            sourceTexture.Apply(false, false);
            Sprite sourceSprite = Track(Sprite.Create(
                sourceTexture,
                new Rect(startX, startY, spriteWidth, spriteHeight),
                new Vector2(0.3f, 0.25f),
                48f));
            sourceTexture.Apply(false, true);
            Assert.That(sourceTexture.isReadable, Is.False);

            Sprite snowSprite = Track(StrategyBuildingSnowSpriteFactory.GetSnowCapSprite(sourceSprite));
            Track(snowSprite.texture);
            Assert.That(snowSprite.rect.size, Is.EqualTo(sourceSprite.rect.size));
            Assert.That(snowSprite.pixelsPerUnit, Is.EqualTo(sourceSprite.pixelsPerUnit));
            Assert.That(Vector2.Distance(snowSprite.pivot, sourceSprite.pivot), Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(snowSprite.bounds.size, sourceSprite.bounds.size),
                Is.LessThan(0.001f));

            Color32[] snowPixels = ReadPixels(snowSprite.texture);
            int visiblePixels = 0;
            for (int y = 0; y < spriteHeight; y++)
            {
                for (int x = 0; x < spriteWidth; x++)
                {
                    if (snowPixels[y * spriteWidth + x].a == 0)
                    {
                        continue;
                    }

                    visiblePixels++;
                    Assert.That(
                        sourcePixels[(startY + y) * textureWidth + startX + x].a,
                        Is.GreaterThan((byte)20),
                        $"Snow escaped authored alpha at ({x}, {y})");
                }
            }

            Assert.That(visiblePixels, Is.GreaterThan(0));
            for (int y = 0; y < spriteHeight; y++)
            {
                Assert.That(
                    snowPixels[y * spriteWidth + 4].a,
                    Is.EqualTo(0),
                    "The transparent authored gap was replaced by the generic fallback cap");
            }
        }

        [Test]
        public void NonReadableAtlasAlphaReadbackIsCachedAcrossSpriteRects()
        {
            Texture2D atlas = Track(new Texture2D(20, 10, TextureFormat.RGBA32, false));
            Color32[] pixels = CreatePixels(atlas.width, atlas.height);
            FillRect(pixels, atlas.width, new RectInt(1, 1, 8, 7));
            FillRect(pixels, atlas.width, new RectInt(11, 1, 8, 7));
            atlas.SetPixels32(pixels);
            atlas.Apply(false, false);
            Sprite left = Track(Sprite.Create(
                atlas,
                new Rect(0f, 0f, 10f, 10f),
                new Vector2(0.5f, 0.2f),
                32f));
            Sprite right = Track(Sprite.Create(
                atlas,
                new Rect(10f, 0f, 10f, 10f),
                new Vector2(0.5f, 0.2f),
                32f));
            atlas.Apply(false, true);

            Sprite leftSnow = Track(StrategyBuildingSnowSpriteFactory.GetSnowCapSprite(left));
            Track(leftSnow.texture);
            Assert.That(StrategyBuildingSnowSpriteFactory.CachedSourceTextureCount, Is.EqualTo(1));

            Sprite rightSnow = Track(StrategyBuildingSnowSpriteFactory.GetSnowCapSprite(right));
            Track(rightSnow.texture);
            Assert.That(StrategyBuildingSnowSpriteFactory.CachedSourceTextureCount, Is.EqualTo(1));
            Assert.That(
                StrategyBuildingSnowSpriteFactory.GetSnowCapSprite(left),
                Is.SameAs(leftSnow));
        }

        private Texture2D Track(Texture2D texture)
        {
            textures.Add(texture);
            return texture;
        }

        private Sprite Track(Sprite sprite)
        {
            sprites.Add(sprite);
            return sprite;
        }

        private static Color32[] CreatePixels(int width, int height)
        {
            return new Color32[width * height];
        }

        private static void FillRect(Color32[] pixels, int textureWidth, RectInt rect)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    pixels[y * textureWidth + x] = new Color32(90, 120, 80, 255);
                }
            }
        }

        private static Color32[] ReadPixels(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            Texture2D readable = null;
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(
                    texture.width,
                    texture.height,
                    TextureFormat.RGBA32,
                    false,
                    true);
                readable.ReadPixels(
                    new Rect(0f, 0f, texture.width, texture.height),
                    0,
                    0,
                    false);
                readable.Apply(false, false);
                return readable.GetPixels32(0);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (readable != null)
                {
                    Object.DestroyImmediate(readable);
                }
            }
        }
    }
}
