using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResidentCinematicVisualTests
    {
        [Test]
        public void MouseStartleProvidesEightAdultFramesAndNoChildBakeRow()
        {
            Assert.That(
                (int)StrategyResidentVisualPose.MouseStartle,
                Is.EqualTo((int)StrategyResidentVisualPose.GroundSleep + 1));
            Assert.That(
                StrategyVisualBakeSource.GetResidentFrameCount(
                    StrategyResidentVisualPose.MouseStartle,
                    StrategyResidentLifeStage.Adult),
                Is.EqualTo(StrategyResidentSpriteFactory.MouseStartleFrameCount));
            Assert.That(
                StrategyVisualBakeSource.GetResidentFrameCount(
                    StrategyResidentVisualPose.MouseStartle,
                    StrategyResidentLifeStage.Child),
                Is.Zero);

            for (int genderValue = 0; genderValue < 2; genderValue++)
            {
                StrategyResidentGender gender = (StrategyResidentGender)genderValue;
                for (int variant = 0; variant < StrategyResidentSpriteFactory.VariantCountPerGender; variant++)
                {
                    Sprite previous = null;
                    for (int frame = 0; frame < StrategyResidentSpriteFactory.MouseStartleFrameCount; frame++)
                    {
                        Sprite sprite = StrategyVisualBakeSource.GetResidentSprite(
                            gender,
                            variant,
                            StrategyResidentLifeStage.Adult,
                            StrategyResidentVisualPose.MouseStartle,
                            frame);
                        Assert.That(sprite, Is.Not.Null, $"{gender}/V{variant}/F{frame}");
                        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(32f));
                        Assert.That(sprite, Is.Not.SameAs(previous), $"{gender}/V{variant}/F{frame}");
                        previous = sprite;
                    }
                }
            }
        }

        [Test]
        public void OverrideRestoresExactActorAndChildRendererState()
        {
            Sprite original = CreateTestSprite("Original");
            GameObject actorObject = new GameObject("Resident Cinematic Test");
            GameObject outlineObject = new GameObject("Outline");
            GameObject shadowObject = new GameObject("Shadow");
            GameObject accessoryObject = new GameObject("Accessory");
            outlineObject.transform.SetParent(actorObject.transform, false);
            shadowObject.transform.SetParent(actorObject.transform, false);
            accessoryObject.transform.SetParent(actorObject.transform, false);

            SpriteRenderer main = actorObject.AddComponent<SpriteRenderer>();
            SpriteRenderer outline = outlineObject.AddComponent<SpriteRenderer>();
            SpriteRenderer shadow = shadowObject.AddComponent<SpriteRenderer>();
            SpriteRenderer accessory = accessoryObject.AddComponent<SpriteRenderer>();
            main.sprite = original;
            main.flipX = false;
            main.color = new Color(0.8f, 0.7f, 0.6f, 0.9f);
            main.sortingOrder = 14;
            outline.sprite = original;
            outline.flipX = false;
            outline.enabled = true;
            shadow.enabled = false;
            accessory.sprite = original;
            accessory.flipX = true;
            accessory.enabled = true;
            accessory.sortingOrder = 19;
            accessory.transform.localPosition = new Vector3(0.3f, 0.4f, -0.2f);
            accessory.transform.localRotation = Quaternion.Euler(0f, 0f, 17f);
            accessory.transform.localScale = new Vector3(0.8f, 1.2f, 1f);

            actorObject.transform.position = new Vector3(2f, 3f, -0.08f);
            actorObject.transform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            actorObject.transform.localScale = new Vector3(1.1f, 0.9f, 1f);
            StrategyResidentAgent agent = actorObject.AddComponent<StrategyResidentAgent>();
            SetField(agent, "spriteRenderer", main);
            SetField(agent, "outlineRenderer", outline);
            SetField(agent, "shadowRenderer", shadow);

            Vector3 originalPosition = actorObject.transform.position;
            Quaternion originalRotation = actorObject.transform.localRotation;
            Vector3 originalScale = actorObject.transform.localScale;
            Vector3 accessoryPosition = accessory.transform.localPosition;
            Quaternion accessoryRotation = accessory.transform.localRotation;
            Vector3 accessoryScale = accessory.transform.localScale;
            StrategyResidentAgent.ResidentActivity originalActivity = agent.Activity;
            StrategyResidentTaskKind originalTaskKind = agent.TaskKind;

            try
            {
                Assert.That(agent.CanParticipateInCinematic, Is.True);
                Assert.That(agent.TryBeginCinematicVisualOverride(out var visualOverride), Is.True);
                Assert.That(accessory.enabled, Is.False);
                Assert.That(main.enabled, Is.True);
                Assert.That(outline.enabled, Is.True);
                Assert.That(shadow.enabled, Is.True);
                Assert.That(
                    visualOverride.ApplyPose(StrategyResidentVisualPose.MouseStartle, 3, true),
                    Is.True);
                Assert.That(main.sprite, Is.Not.SameAs(original));
                Assert.That(main.flipX, Is.True);
                Assert.That(
                    visualOverride.SetTransformPose(
                        new Vector3(7f, 8f, -0.08f),
                        Quaternion.identity,
                        new Vector3(0.9f, 1.1f, 1f)),
                    Is.True);

                visualOverride.Dispose();

                Assert.That(agent.IsCinematicVisualOverrideActive, Is.False);
                Assert.That(actorObject.transform.position, Is.EqualTo(originalPosition));
                Assert.That(actorObject.transform.localRotation, Is.EqualTo(originalRotation));
                Assert.That(actorObject.transform.localScale, Is.EqualTo(originalScale));
                Assert.That(main.sprite, Is.SameAs(original));
                Assert.That(main.flipX, Is.False);
                Assert.That(main.color, Is.EqualTo(new Color(0.8f, 0.7f, 0.6f, 0.9f)));
                Assert.That(main.sortingOrder, Is.EqualTo(14));
                Assert.That(outline.sprite, Is.SameAs(original));
                Assert.That(outline.flipX, Is.False);
                Assert.That(outline.enabled, Is.True);
                Assert.That(shadow.enabled, Is.False);
                Assert.That(accessory.sprite, Is.SameAs(original));
                Assert.That(accessory.flipX, Is.True);
                Assert.That(accessory.enabled, Is.True);
                Assert.That(accessory.sortingOrder, Is.EqualTo(19));
                Assert.That(accessory.transform.localPosition, Is.EqualTo(accessoryPosition));
                Assert.That(accessory.transform.localRotation, Is.EqualTo(accessoryRotation));
                Assert.That(accessory.transform.localScale, Is.EqualTo(accessoryScale));
                Assert.That(agent.Activity, Is.EqualTo(originalActivity));
                Assert.That(agent.TaskKind, Is.EqualTo(originalTaskKind));
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
                Object.DestroyImmediate(original.texture);
                Object.DestroyImmediate(original);
            }
        }

        [Test]
        public void HiddenSleepingAdultCanBeTemporarilyRevealedAndRestored()
        {
            GameObject actorObject = new GameObject("Hidden Resident Cinematic Test");
            SpriteRenderer main = actorObject.AddComponent<SpriteRenderer>();
            StrategyResidentAgent agent = actorObject.AddComponent<StrategyResidentAgent>();
            SetField(agent, "spriteRenderer", main);
            SetField(agent, "hiddenInsideHome", true);
            SetField(agent, "sleepingInsideHome", true);
            main.enabled = false;

            try
            {
                Assert.That(agent.CanParticipateInCinematic, Is.False);
                Assert.That(agent.CanBeTemporarilyRevealedForCinematic, Is.True);
                Assert.That(agent.TryBeginCinematicVisualOverride(out var visualOverride), Is.True);
                Assert.That(main.enabled, Is.True);

                visualOverride.Dispose();

                Assert.That(main.enabled, Is.False);
                Assert.That(GetField<bool>(agent, "hiddenInsideHome"), Is.True);
                Assert.That(GetField<bool>(agent, "sleepingInsideHome"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
            }
        }

        private static Sprite CreateTestSprite(string name)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name + " Texture"
            };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f, 32f);
            sprite.name = name;
            return sprite;
        }

        private static void SetField<T>(StrategyResidentAgent agent, string fieldName, T value)
        {
            FieldInfo field = typeof(StrategyResidentAgent).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(agent, value);
        }

        private static T GetField<T>(StrategyResidentAgent agent, string fieldName)
        {
            FieldInfo field = typeof(StrategyResidentAgent).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(agent);
        }
    }
}
