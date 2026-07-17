using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCinematicCatTests
    {
        [Test]
        public void ActorUsesExactStandardCatSpritesForEveryAnimation()
        {
            GameObject actorObject = new("Cinematic Cat Sprite Test", typeof(SpriteRenderer));
            try
            {
                SpriteRenderer renderer = actorObject.GetComponent<SpriteRenderer>();
                StrategyCinematicCatActor actor =
                    actorObject.AddComponent<StrategyCinematicCatActor>();
                actor.Configure(renderer, StrategyCatCoat.Calico);
                actor.SetAutomaticTick(false);
                Assert.That(actor.transform.localScale.x, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.CatWorldScale).Within(0.001f));

                foreach (StrategyCinematicCatAnimation animation in
                         System.Enum.GetValues(typeof(StrategyCinematicCatAnimation)))
                {
                    actor.Play(animation, 8f, true);
                    for (int frame = 0; frame < actor.FrameCount; frame++)
                    {
                        actor.SetFrame(frame);
                        Assert.That(
                            renderer.sprite,
                            Is.SameAs(StrategySettlementFaunaSpriteFactory.GetCatSprite(
                                StrategyCatCoat.Calico,
                                ResolvePose(animation),
                                frame)));
                    }
                }

            }
            finally
            {
                Object.DestroyImmediate(actorObject);
            }
        }

        [Test]
        public void ManualAdvanceAndMovementUseProvidedUnscaledDelta()
        {
            GameObject actorObject = new("Cinematic Cat Motion Test", typeof(SpriteRenderer));
            try
            {
                SpriteRenderer renderer = actorObject.GetComponent<SpriteRenderer>();
                StrategyCinematicCatActor actor =
                    actorObject.AddComponent<StrategyCinematicCatActor>();
                actor.Configure(renderer, StrategyCatCoat.GrayTabby);
                actor.SetAutomaticTick(false);
                actor.Play(StrategyCinematicCatAnimation.Stalk, 10f, true);

                actor.Advance(0.11f);
                Assert.That(actor.FrameIndex, Is.EqualTo(1));
                Assert.That(actor.transform.localScale.y, Is.LessThan(
                    StrategySettlementFaunaSpriteFactory.CatWorldScale));

                actor.SetWorldPosition(Vector3.zero);
                bool reached = actor.MoveTowards(
                    new Vector3(2f, 0f, 0f),
                    2f,
                    0.25f);
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
        public void NonLoopingJoyStopsAtFinalFrameAndRestoresPose()
        {
            GameObject actorObject = new("Cinematic Cat Joy Test", typeof(SpriteRenderer));
            try
            {
                StrategyCinematicCatActor actor =
                    actorObject.AddComponent<StrategyCinematicCatActor>();
                actor.Configure(
                    actorObject.GetComponent<SpriteRenderer>(),
                    StrategyCatCoat.Ginger);
                actor.SetAutomaticTick(false);
                actor.Play(StrategyCinematicCatAnimation.Joy, 12f, false);

                actor.Advance(1f);

                Assert.That(actor.FrameIndex, Is.EqualTo(actor.FrameCount - 1));
                Assert.That(actor.IsPlaying, Is.False);
                Assert.That(actor.transform.localScale, Is.EqualTo(
                    Vector3.one * StrategySettlementFaunaSpriteFactory.CatWorldScale));
                Assert.That(actor.transform.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
            }
        }

        private static StrategyCatSpritePose ResolvePose(
            StrategyCinematicCatAnimation animation)
        {
            return animation switch
            {
                StrategyCinematicCatAnimation.Stalk => StrategyCatSpritePose.Stalk,
                StrategyCinematicCatAnimation.Pounce => StrategyCatSpritePose.Pounce,
                StrategyCinematicCatAnimation.Joy => StrategyCatSpritePose.Joy,
                _ => StrategyCatSpritePose.Idle
            };
        }
    }
}
