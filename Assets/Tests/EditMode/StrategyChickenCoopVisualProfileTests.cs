using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyChickenCoopVisualProfileTests
    {
        [SetUp]
        public void ResetVisualCaches()
        {
            StrategyVisualBakeSource.ResetRuntimeVisualCaches();
        }

        [Test]
        public void SingleVariantNormalizesLegacyBuildingAndConstructionValues()
        {
            Assert.That(StrategyBuildingSpriteFactory.ChickenCoopVariantCount, Is.EqualTo(1));
            foreach (int legacyVariant in new[] { -7, -1, 0, 1, 5, 12 })
            {
                Assert.That(
                    StrategyBuildingVariantProfile.NormalizeVariant(
                        StrategyBuildTool.ChickenCoop,
                        legacyVariant),
                    Is.EqualTo(0));
            }

            Assert.That(
                StrategyBuildingSpriteFactory.TryGetBuildSprite(
                    StrategyBuildTool.ChickenCoop,
                    0,
                    out Sprite completed),
                Is.True);
            Sprite construction = StrategyConstructionSpriteFactory.GetConstructionSprite(
                StrategyBuildTool.ChickenCoop,
                0,
                0);
            foreach (int legacyVariant in new[] { -7, -1, 1, 5, 12 })
            {
                Assert.That(
                    StrategyBuildingSpriteFactory.TryGetBuildSprite(
                        StrategyBuildTool.ChickenCoop,
                        legacyVariant,
                        out Sprite normalizedCompleted),
                    Is.True);
                Assert.That(normalizedCompleted, Is.SameAs(completed));
                Assert.That(
                    StrategyConstructionSpriteFactory.GetConstructionSprite(
                        StrategyBuildTool.ChickenCoop,
                        legacyVariant,
                        0),
                    Is.SameAs(construction));
            }
        }

        [Test]
        public void BakeSourceRemainsProceduralAtLegacyWorldScale()
        {
            for (int frame = 0; frame < StrategyChickenCoopVisualProfile.AnimationFrameCount; frame++)
            {
                Sprite source = StrategyVisualBakeSource.GetChickenCoopProductionSprite(frame);
                Assert.That(
                    source.rect.size,
                    Is.EqualTo(new Vector2(
                        StrategyChickenCoopVisualProfile.AuthoredFrameWidth / 2f,
                        StrategyChickenCoopVisualProfile.AuthoredFrameHeight / 2f)));
                AssertSpriteScale(
                    source,
                    StrategyChickenCoopVisualProfile.ProceduralStandalonePixelsPerUnit,
                    StrategyChickenCoopVisualProfile.StandalonePivotY);
                Assert.That(
                    source.rect.width / source.pixelsPerUnit,
                    Is.EqualTo(
                        StrategyChickenCoopVisualProfile.AuthoredFrameWidth
                        / StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit)
                        .Within(0.001f));
            }
        }

        [Test]
        public void AuthoredProductionSequenceServesStandaloneAndHouseUpgradeScales()
        {
            Assert.That(
                StrategyVisualBakeSource.ChickenCoopAnimationFrameCount,
                Is.EqualTo(6));
            Assert.That(
                StrategyVisualSequenceIds.ChickenCoopProduction,
                Is.EqualTo("BuildingAnimation/ChickenCoop/V0"));
            Assert.That(
                StrategyBuildingSpriteFactory.TryGetBuildSprite(
                    StrategyBuildTool.ChickenCoop,
                    0,
                    out Sprite completed),
                Is.True);
            Assert.That(
                completed.rect.size,
                Is.EqualTo(new Vector2(
                    StrategyChickenCoopVisualProfile.AuthoredFrameWidth,
                    StrategyChickenCoopVisualProfile.AuthoredFrameHeight)));
            AssertSpriteScale(
                completed,
                StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit,
                StrategyChickenCoopVisualProfile.StandalonePivotY);

            for (int frame = 0; frame < StrategyChickenCoopVisualProfile.AnimationFrameCount; frame++)
            {
                Assert.That(
                    StrategyChickenCoopVisualProfile.TryGetAuthoredStandaloneSprite(
                        frame,
                        out Sprite source),
                    Is.True,
                    $"Authored Chicken Coop production frame {frame} is missing or malformed");
                AssertFrameRect(source, frame);
                AssertSpriteScale(
                    source,
                    StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit,
                    StrategyChickenCoopVisualProfile.StandalonePivotY);

                Sprite standalone = StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(frame);
                Sprite upgrade = StrategyBuildingUpgradeSpriteFactory.GetAnimatedSprite(
                    StrategyBuildingUpgradeType.ChickenCoop,
                    frame);
                Assert.That(standalone.texture, Is.SameAs(source.texture));
                Assert.That(upgrade.texture, Is.SameAs(source.texture));
                Assert.That(standalone.rect, Is.EqualTo(source.rect));
                Assert.That(upgrade.rect, Is.EqualTo(source.rect));
                AssertSpriteScale(
                    standalone,
                    StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit,
                    StrategyChickenCoopVisualProfile.StandalonePivotY);
                AssertSpriteScale(
                    upgrade,
                    StrategyChickenCoopVisualProfile.HouseUpgradePixelsPerUnit,
                    StrategyChickenCoopVisualProfile.UpgradePivotY);
            }

            Assert.That(
                StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(-1),
                Is.SameAs(StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(5)));
            Assert.That(
                StrategyBuildingUpgradeSpriteFactory.GetAnimatedSprite(
                    StrategyBuildingUpgradeType.ChickenCoop,
                    -1),
                Is.SameAs(StrategyBuildingUpgradeSpriteFactory.GetAnimatedSprite(
                    StrategyBuildingUpgradeType.ChickenCoop,
                    5)));
        }

        [Test]
        public void RuntimeVisualResetInvalidatesBothDerivedFrameCaches()
        {
            Sprite standaloneBefore = StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(0);
            Sprite upgradeBefore = StrategyBuildingUpgradeSpriteFactory.GetAnimatedSprite(
                StrategyBuildingUpgradeType.ChickenCoop,
                0);

            StrategyVisualBakeSource.ResetRuntimeVisualCaches();

            Sprite standaloneAfter = StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(0);
            Sprite upgradeAfter = StrategyBuildingUpgradeSpriteFactory.GetAnimatedSprite(
                StrategyBuildingUpgradeType.ChickenCoop,
                0);
            Assert.That(standaloneAfter, Is.Not.SameAs(standaloneBefore));
            Assert.That(upgradeAfter, Is.Not.SameAs(upgradeBefore));
            AssertSpriteScale(
                standaloneAfter,
                StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit,
                StrategyChickenCoopVisualProfile.StandalonePivotY);
            AssertSpriteScale(
                upgradeAfter,
                StrategyChickenCoopVisualProfile.HouseUpgradePixelsPerUnit,
                StrategyChickenCoopVisualProfile.UpgradePivotY);
        }

        private static void AssertFrameRect(Sprite sprite, int frame)
        {
            Assert.That(
                sprite.rect,
                Is.EqualTo(new Rect(
                    frame * StrategyChickenCoopVisualProfile.AuthoredFrameWidth,
                    0f,
                    StrategyChickenCoopVisualProfile.AuthoredFrameWidth,
                    StrategyChickenCoopVisualProfile.AuthoredFrameHeight)));
            Assert.That(
                sprite.texture.width,
                Is.EqualTo(
                    StrategyChickenCoopVisualProfile.AuthoredFrameWidth
                    * StrategyChickenCoopVisualProfile.AnimationFrameCount));
            Assert.That(
                sprite.texture.height,
                Is.EqualTo(StrategyChickenCoopVisualProfile.AuthoredFrameHeight));
        }

        private static void AssertSpriteScale(Sprite sprite, float pixelsPerUnit, float pivotY)
        {
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(pixelsPerUnit).Within(0.001f));
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(pivotY).Within(0.001f));
        }
    }
}
