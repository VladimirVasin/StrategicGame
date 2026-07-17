using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCinematicRatTests
    {
        [Test]
        public void FactoryUsesExactStandardSettlementMouseSpriteForEveryMotionFrame()
        {
            StrategyCinematicRatAnimation[] animations =
            {
                StrategyCinematicRatAnimation.Run,
                StrategyCinematicRatAnimation.Escape
            };
            foreach (StrategyCinematicRatAnimation animation in animations)
            {
                int frameCount = StrategyCinematicRatSpriteFactory.GetFrameCount(animation);
                for (int variant = 0;
                     variant < StrategyCinematicRatSpriteFactory.VariantCount;
                     variant++)
                {
                    Sprite expected = StrategySettlementFaunaSpriteFactory.GetMouseSprite(variant);
                    for (int frame = -1; frame <= frameCount; frame++)
                    {
                        Sprite actual = StrategyCinematicRatSpriteFactory.GetFrame(
                            animation,
                            variant,
                            frame);
                        Assert.That(actual, Is.SameAs(expected));
                    }
                }
            }

            Sprite standard = StrategySettlementFaunaSpriteFactory.GetMouseSprite(0);
            Assert.That(standard.rect.size, Is.EqualTo(new Vector2(19f, 11f)));
            Assert.That(standard.pixelsPerUnit, Is.EqualTo(25f));
            Assert.That(standard.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(standard.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        }

        [Test]
        public void ManualTickAndMovementUseProvidedUnscaledDelta()
        {
            GameObject actorObject = new("Cinematic Rat Test", typeof(SpriteRenderer));
            try
            {
                SpriteRenderer renderer = actorObject.GetComponent<SpriteRenderer>();
                StrategyCinematicRatActor actor = actorObject.AddComponent<StrategyCinematicRatActor>();
                actor.Configure(renderer);
                actor.SetAutomaticTick(false);
                actor.Play(StrategyCinematicRatAnimation.Run, 10f, true);
                Vector3 standardScale = actor.transform.localScale;
                Quaternion neutralRotation = actor.transform.localRotation;

                actor.Advance(0.11f);
                Assert.That(actor.FrameIndex, Is.EqualTo(1));
                Assert.That(
                    renderer.sprite,
                    Is.SameAs(StrategySettlementFaunaSpriteFactory.GetMouseSprite(0)));
                Assert.That(standardScale.x, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.MouseWorldScale));
                Assert.That(standardScale.y, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.MouseWorldScale));
                Assert.That(actor.transform.localScale.x, Is.LessThanOrEqualTo(standardScale.x));
                Assert.That(actor.transform.localScale.y, Is.LessThanOrEqualTo(standardScale.y));
                Assert.That(
                    Quaternion.Angle(neutralRotation, actor.transform.localRotation),
                    Is.GreaterThan(0f));

                actor.SetWorldPosition(Vector3.zero);
                bool reached = actor.MoveTowards(new Vector3(2f, 0f, 0f), 2f, 0.25f);
                Assert.That(reached, Is.False);
                Assert.That(actor.transform.position.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(renderer.flipX, Is.False);

                actor.MoveTowards(new Vector3(-2f, 0f, 0f), 2f, 0.25f);
                Assert.That(renderer.flipX, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
            }
        }

        [Test]
        public void NonLoopingEscapeStopsOnItsFinalFrame()
        {
            GameObject actorObject = new("Cinematic Rat Escape Test", typeof(SpriteRenderer));
            try
            {
                StrategyCinematicRatActor actor = actorObject.AddComponent<StrategyCinematicRatActor>();
                actor.Configure(actorObject.GetComponent<SpriteRenderer>());
                actor.SetAutomaticTick(false);
                actor.Play(StrategyCinematicRatAnimation.Escape, 12f, false);

                actor.Advance(1f);

                Assert.That(actor.FrameIndex, Is.EqualTo(actor.FrameCount - 1));
                Assert.That(actor.IsPlaying, Is.False);
                Assert.That(actor.transform.localScale.x, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.MouseWorldScale));
                Assert.That(actor.transform.localScale.y, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.MouseWorldScale));
                Assert.That(actor.transform.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
            }
        }
    }
}
