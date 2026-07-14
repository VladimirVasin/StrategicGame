using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyForagerCampVisualProfileTests
    {
        [Test]
        public void SingleVariantAndLegacyVariantsNormalizeToAuthoredCamp()
        {
            Assert.That(StrategyBuildingSpriteFactory.ForagerCampVariantCount, Is.EqualTo(1));
            foreach (int legacyVariant in new[] { -3, -1, 0, 1, 2, 7 })
            {
                Assert.That(
                    StrategyForagerCampVisualProfile.NormalizeVariant(
                        StrategyBuildTool.ForagerCamp,
                        legacyVariant),
                    Is.EqualTo(0));
            }
        }

        [Test]
        public void ConstructionAndRuntimeOverlayAnchorsMatchAuthoredGeometry()
        {
            Assert.That(StrategyForagerCampVisualProfile.SpriteWidth, Is.EqualTo(176));
            Assert.That(StrategyForagerCampVisualProfile.SpriteHeight, Is.EqualTo(116));
            Assert.That(StrategyForagerCampVisualProfile.ConstructionFrameWidth, Is.EqualTo(184));
            Assert.That(StrategyForagerCampVisualProfile.ConstructionFrameHeight, Is.EqualTo(164));
            Assert.That(StrategyForagerCampVisualProfile.ConstructionFrameCount, Is.EqualTo(7));
            Assert.That(StrategyForagerCampVisualProfile.PixelsPerUnit, Is.EqualTo(48f));
            Assert.That(
                StrategyForagerCampVisualProfile.SpriteWidth / StrategyForagerCampVisualProfile.PixelsPerUnit,
                Is.EqualTo(88f / 24f).Within(0.001f));
            Assert.That(
                StrategyForagerCampVisualProfile.SpriteHeight / StrategyForagerCampVisualProfile.PixelsPerUnit,
                Is.EqualTo(58f / 24f).Within(0.001f));

            Vector2 pivotPixels = Vector2.Scale(
                StrategyForagerCampVisualProfile.ConstructionPivotNormalized,
                new Vector2(
                    StrategyForagerCampVisualProfile.ConstructionFrameWidth,
                    StrategyForagerCampVisualProfile.ConstructionFrameHeight));
            Assert.That(Vector2.Distance(pivotPixels, new Vector2(92f, 23.2f)), Is.LessThan(0.001f));
            Assert.That(
                Vector2.Distance(
                    pivotPixels / StrategyForagerCampVisualProfile.PixelsPerUnit,
                    new Vector2(46f, 11.6f) / 24f),
                Is.LessThan(0.001f));

            Bounds footprint = new(Vector3.zero, new Vector3(2f, 2f, 0f));
            Assert.That(
                Vector3.Distance(
                    StrategyForagerCampVisualProfile.GetTorchAnchorWorld(footprint),
                    new Vector3(1.2f, 0.42f, -0.22f)),
                Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(
                    StrategyForagerCampVisualProfile.GetStockAnchorWorld(footprint),
                    new Vector3(0.54f, -0.38f, -0.13f)),
                Is.LessThan(0.001f));
        }
    }
}
