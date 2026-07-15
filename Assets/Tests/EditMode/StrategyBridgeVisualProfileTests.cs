using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyBridgeVisualProfileTests
    {
        private readonly Dictionary<string, Sprite> sourceSprites = new();
        private readonly List<Sprite> generatedSprites = new();

        [SetUp]
        public void ResetVisualState()
        {
            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            StrategyBridgeVisualProfile.ResetRuntimeState();
        }

        [TearDown]
        public void DestroyGeneratedTextures()
        {
            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            StrategyBridgeVisualProfile.ResetRuntimeState();

            HashSet<EntityId> destroyed = new();
            for (int i = 0; i < generatedSprites.Count; i++)
            {
                DestroySpriteAndTexture(generatedSprites[i], destroyed);
            }

            foreach (Sprite sprite in sourceSprites.Values)
            {
                DestroySpriteAndTexture(sprite, destroyed);
            }

            generatedSprites.Clear();
            sourceSprites.Clear();
        }

        [Test]
        public void ContractUsesStableCatalogIdsResourcePathsAndModuleGeometry()
        {
            Assert.That(StrategyVisualSequenceIds.BridgeFinal, Is.EqualTo("Bridge/Final"));
            Assert.That(StrategyVisualSequenceIds.BridgeConstruction, Is.EqualTo("Bridge/Construction"));
            Assert.That(
                StrategyBridgeVisualProfile.GetCatalogSequenceId(
                    true,
                    StrategyBridgeVisualProfile.Module.Start,
                    false),
                Is.EqualTo("Bridge/Final/Horizontal/Start"));
            Assert.That(
                StrategyBridgeVisualProfile.GetCatalogSequenceId(
                    false,
                    StrategyBridgeVisualProfile.Module.Middle,
                    true),
                Is.EqualTo("Bridge/Construction/Vertical/Middle"));
            Assert.That(
                StrategyBridgeVisualProfile.GetResourcePath(
                    true,
                    StrategyBridgeVisualProfile.Module.End,
                    false,
                    0),
                Is.EqualTo("Visual/Authored/Buildings/Bridge/Horizontal/End"));
            Assert.That(
                StrategyBridgeVisualProfile.GetResourcePath(
                    false,
                    StrategyBridgeVisualProfile.Module.Middle,
                    true,
                    6),
                Is.EqualTo("Visual/Authored/Construction/Bridge/Vertical/S07/Middle"));

            Assert.That(
                StrategyBridgeVisualProfile.GetModulePixelSize(
                    true,
                    StrategyBridgeVisualProfile.Module.Start),
                Is.EqualTo(new Vector2Int(68, 112)));
            Assert.That(
                StrategyBridgeVisualProfile.GetModulePixelSize(
                    true,
                    StrategyBridgeVisualProfile.Module.Middle),
                Is.EqualTo(new Vector2Int(48, 112)));
            Assert.That(
                StrategyBridgeVisualProfile.GetModulePixelSize(
                    false,
                    StrategyBridgeVisualProfile.Module.End),
                Is.EqualTo(new Vector2Int(124, 68)));
            Assert.That(
                StrategyBridgeVisualProfile.GetModulePixelSize(
                    false,
                    StrategyBridgeVisualProfile.Module.Middle),
                Is.EqualTo(new Vector2Int(124, 48)));
        }

        [Test]
        public void CompletedComposerPreservesLegacyWorldGeometryForEverySupportedSpan()
        {
            UseValidCatalogResolver();

            for (int span = StrategyBridgeVisualProfile.MinimumSpanCells;
                 span <= StrategyBridgeVisualProfile.MaximumSpanCells;
                 span++)
            {
                AssertAuthoredGeometry(
                    Track(StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(span, 1))),
                    new Vector2Int(span, 1));
                AssertAuthoredGeometry(
                    Track(StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(1, span))),
                    new Vector2Int(1, span));
            }

            Sprite cached = StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(3, 1));
            Assert.That(
                StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(3, 1)),
                Is.SameAs(cached));
            Assert.That(
                StrategyBuildingSpriteFactory.TryGetBuildSprite(
                    StrategyBuildTool.Bridge,
                    0,
                    out Sprite preview),
                Is.True);
            AssertAuthoredGeometry(Track(preview), new Vector2Int(3, 1));
        }

        [Test]
        public void ConstructionComposerSupportsEveryStageAndEverySupportedSpan()
        {
            UseValidCatalogResolver();

            for (int span = StrategyBridgeVisualProfile.MinimumSpanCells;
                 span <= StrategyBridgeVisualProfile.MaximumSpanCells;
                 span++)
            {
                AssertAuthoredConstructionGeometry(new Vector2Int(span, 1), 0);
                AssertAuthoredConstructionGeometry(new Vector2Int(1, span), 0);
            }

            for (int stage = 1; stage < StrategyConstructionSpriteFactory.StageCount; stage++)
            {
                AssertAuthoredConstructionGeometry(new Vector2Int(3, 1), stage);
                AssertAuthoredConstructionGeometry(new Vector2Int(12, 1), stage);
                AssertAuthoredConstructionGeometry(new Vector2Int(1, 3), stage);
                AssertAuthoredConstructionGeometry(new Vector2Int(1, 12), stage);
            }

            Sprite cached = StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(
                new Vector2Int(12, 1),
                6);
            Assert.That(
                StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(
                    new Vector2Int(12, 1),
                    6),
                Is.SameAs(cached));
        }

        [Test]
        public void ResourceModulesRecoverFromMalformedCatalogModule()
        {
            StrategyBridgeVisualProfile.SetResolversForTests(
                (sequenceId, frame) => sequenceId.EndsWith("/Middle", StringComparison.Ordinal)
                    ? GetSourceSprite(sequenceId + "#malformed", 47f)
                    : GetSourceSprite(sequenceId + "#" + frame),
                resourcePath => GetSourceSprite(resourcePath));

            Sprite completed = Track(
                StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(6, 1)));
            Sprite construction = Track(
                StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(
                    new Vector2Int(1, 6),
                    4));

            AssertAuthoredGeometry(completed, new Vector2Int(6, 1));
            AssertAuthoredGeometry(construction, new Vector2Int(1, 6));
        }

        [Test]
        public void MissingMalformedOrUnsupportedAuthoredKitUsesProceduralFallback()
        {
            StrategyBridgeVisualProfile.SetResolversForTests(null, null);
            Sprite missing = Track(
                StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(3, 1)));
            AssertProceduralGeometry(missing, new Vector2Int(3, 1));

            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            StrategyBridgeVisualProfile.SetResolversForTests(
                (sequenceId, frame) => sequenceId.EndsWith("/Middle", StringComparison.Ordinal)
                    ? GetSourceSprite(sequenceId + "#empty", 48f, true)
                    : GetSourceSprite(sequenceId + "#" + frame),
                null);
            Sprite malformed = Track(
                StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(
                    new Vector2Int(1, 12),
                    3));
            AssertProceduralGeometry(malformed, new Vector2Int(1, 12));

            StrategyBuildingSpriteFactory.ResetCaches();
            UseValidCatalogResolver();
            Sprite tooShort = Track(
                StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(2, 1)));
            Sprite tooLong = Track(
                StrategyBuildingSpriteFactory.GetBridgeSprite(new Vector2Int(1, 13)));
            AssertProceduralGeometry(tooShort, new Vector2Int(2, 1));
            AssertProceduralGeometry(tooLong, new Vector2Int(1, 13));
        }

        private void AssertAuthoredConstructionGeometry(Vector2Int footprint, int stage)
        {
            Sprite sprite = Track(
                StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(footprint, stage));
            AssertAuthoredGeometry(sprite, footprint);
            StringAssert.Contains($"Construction Stage {stage + 1}", sprite.name);
        }

        private static void AssertAuthoredGeometry(Sprite sprite, Vector2Int footprint)
        {
            Vector2Int expected = StrategyBridgeVisualProfile.GetOutputPixelSize(footprint);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.size, Is.EqualTo((Vector2)expected));
            Assert.That(
                sprite.pixelsPerUnit,
                Is.EqualTo(StrategyBridgeVisualProfile.AuthoredPixelsPerUnit).Within(0.001f));
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.5f).Within(0.001f));

            bool horizontal = footprint.x >= footprint.y;
            int span = horizontal ? footprint.x : footprint.y;
            Vector2 legacyPixels = horizontal
                ? new Vector2(Mathf.Max(72, span * 24 + 20), 56)
                : new Vector2(62, Mathf.Max(72, span * 24 + 20));
            Assert.That(
                sprite.rect.width / sprite.pixelsPerUnit,
                Is.EqualTo(legacyPixels.x / 24f).Within(0.001f));
            Assert.That(
                sprite.rect.height / sprite.pixelsPerUnit,
                Is.EqualTo(legacyPixels.y / 24f).Within(0.001f));
        }

        private static void AssertProceduralGeometry(Sprite sprite, Vector2Int footprint)
        {
            bool horizontal = footprint.x >= footprint.y;
            int span = horizontal ? footprint.x : footprint.y;
            Vector2 expected = horizontal
                ? new Vector2(Mathf.Max(72, span * 24 + 20), 56)
                : new Vector2(62, Mathf.Max(72, span * 24 + 20));
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.size, Is.EqualTo(expected));
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(24f).Within(0.001f));
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0.5f).Within(0.001f));
        }

        private void UseValidCatalogResolver()
        {
            StrategyBridgeVisualProfile.SetResolversForTests(
                (sequenceId, frame) => GetSourceSprite(sequenceId + "#" + frame),
                null);
        }

        private Sprite GetSourceSprite(
            string key,
            float pixelsPerUnit = StrategyBridgeVisualProfile.AuthoredPixelsPerUnit,
            bool transparent = false)
        {
            if (sourceSprites.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            bool horizontal = key.Contains("/Horizontal/", StringComparison.Ordinal);
            StrategyBridgeVisualProfile.Module module = key.Contains("/Middle", StringComparison.Ordinal)
                ? StrategyBridgeVisualProfile.Module.Middle
                : key.Contains("/End", StringComparison.Ordinal)
                    ? StrategyBridgeVisualProfile.Module.End
                    : StrategyBridgeVisualProfile.Module.Start;
            Vector2Int size = StrategyBridgeVisualProfile.GetModulePixelSize(horizontal, module);
            Texture2D texture = new(size.x, size.y, TextureFormat.RGBA32, false)
            {
                name = "Test Bridge Module " + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32 color = transparent
                ? new Color32(0, 0, 0, 0)
                : module switch
                {
                    StrategyBridgeVisualProfile.Module.Start => new Color32(176, 96, 48, 255),
                    StrategyBridgeVisualProfile.Module.Middle => new Color32(110, 76, 44, 255),
                    _ => new Color32(214, 158, 82, 255)
                };
            Color32[] pixels = new Color32[size.x * size.y];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size.x, size.y),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + " Sprite";
            sourceSprites[key] = sprite;
            return sprite;
        }

        private Sprite Track(Sprite sprite)
        {
            if (sprite != null && !generatedSprites.Contains(sprite))
            {
                generatedSprites.Add(sprite);
            }

            return sprite;
        }

        private static void DestroySpriteAndTexture(Sprite sprite, ISet<EntityId> destroyed)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            EntityId spriteId = sprite.GetEntityId();
            if (destroyed.Add(spriteId))
            {
                UnityEngine.Object.DestroyImmediate(sprite);
            }

            if (texture != null && destroyed.Add(texture.GetEntityId()))
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
