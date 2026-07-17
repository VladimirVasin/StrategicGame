using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyFirstNightRatCinematicTests
    {
        [Test]
        public void CleanupRestoresStagedHiddenResidentAndRemovesTransientRat()
        {
            GameObject actorObject = new("Rat Cinematic Resident");
            GameObject transientRoot = new("Rat Cinematic Transients");
            Texture2D originalTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            originalTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            originalTexture.Apply(false, false);
            Sprite originalSprite = Sprite.Create(
                originalTexture,
                new Rect(0f, 0f, 2f, 2f),
                Vector2.one * 0.5f,
                32f);
            SpriteRenderer main = actorObject.AddComponent<SpriteRenderer>();
            main.sprite = originalSprite;
            main.enabled = false;
            main.flipX = true;
            actorObject.transform.position = new Vector3(2f, 3f, -0.08f);
            actorObject.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            actorObject.transform.localScale = new Vector3(1.1f, 0.9f, 1f);

            StrategyResidentAgent resident = actorObject.AddComponent<StrategyResidentAgent>();
            SetField(resident, "spriteRenderer", main);
            SetField(resident, "hiddenInsideHome", true);
            SetField(resident, "sleepingInsideHome", true);
            Vector3 originalPosition = actorObject.transform.position;
            Quaternion originalRotation = actorObject.transform.localRotation;
            Vector3 originalScale = actorObject.transform.localScale;

            Vector3 stagedResident = new Vector3(10f, 10.35f, -0.08f);
            StrategyFirstNightRatShotPlan shot = new(
                resident,
                new Vector2Int(10, 10),
                new Vector2Int(7, 10),
                new Vector2Int(10, 10),
                new Vector2Int(13, 10),
                stagedResident,
                new Vector3(7f, 10f, -0.082f),
                new Vector3(10f, 10f, -0.082f),
                new Vector3(13f, 10f, -0.082f),
                new Vector3(10f, 10.5f, -0.08f),
                5f,
                0,
                true,
                true);
            StrategyFirstNightRatCinematic cinematic = new(
                null,
                null,
                transientRoot.transform);
            SetField(cinematic, "shot", shot);
            SetField(cinematic, "prepared", true);
            StrategyInGameCinematicContext context = new(null, null, () => false);
            cinematic.Begin(context);

            try
            {
                StrategyCinematicRatActor rat = cinematic.ActiveRat;
                StrategyCinematicParticipantHighlight residentHighlight =
                    cinematic.ResidentHighlight;
                StrategyCinematicParticipantHighlight ratHighlight = cinematic.RatHighlight;
                Assert.That(rat, Is.Not.Null);
                Assert.That(rat.Renderer.enabled, Is.True);
                Assert.That(rat.IsPlaying, Is.False);
                Assert.That(rat.transform.position, Is.EqualTo(shot.StartWorld));
                Assert.That(
                    rat.Renderer.sprite,
                    Is.SameAs(StrategySettlementFaunaSpriteFactory.GetMouseSprite(shot.RatVariant)));
                Assert.That(rat.transform.localScale.x, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.MouseWorldScale));
                Assert.That(rat.transform.localScale.y, Is.EqualTo(
                    StrategySettlementFaunaSpriteFactory.MouseWorldScale));
                Assert.That(residentHighlight, Is.Not.Null);
                Assert.That(ratHighlight, Is.Not.Null);
                Assert.That(residentHighlight, Is.Not.SameAs(ratHighlight));
                Assert.That(residentHighlight.Target, Is.SameAs(resident.transform));
                Assert.That(ratHighlight.Target, Is.SameAs(rat.transform));
                Assert.That(residentHighlight.IsVisible, Is.True);
                Assert.That(ratHighlight.IsVisible, Is.True);
                Assert.That(resident.IsCinematicVisualOverrideActive, Is.True);
                Assert.That(main.enabled, Is.True);
                Assert.That(main.sprite, Is.Not.SameAs(originalSprite));
                Assert.That(actorObject.transform.position, Is.EqualTo(stagedResident));

                cinematic.Cleanup(context, StrategyInGameCinematicResult.Cancelled);

                Assert.That(cinematic.ActiveRat, Is.Null);
                Assert.That(cinematic.ResidentHighlight, Is.Null);
                Assert.That(cinematic.RatHighlight, Is.Null);
                Assert.That(rat == null || !rat.Renderer.enabled, Is.True);
                Assert.That(
                    residentHighlight == null || !residentHighlight.IsVisible,
                    Is.True);
                Assert.That(ratHighlight == null || !ratHighlight.IsVisible, Is.True);
                Assert.That(resident.IsCinematicVisualOverrideActive, Is.False);
                Assert.That(main.enabled, Is.False);
                Assert.That(main.sprite, Is.SameAs(originalSprite));
                Assert.That(main.flipX, Is.True);
                Assert.That(actorObject.transform.position, Is.EqualTo(originalPosition));
                Assert.That(actorObject.transform.localRotation, Is.EqualTo(originalRotation));
                Assert.That(actorObject.transform.localScale, Is.EqualTo(originalScale));
                Assert.That(GetField<bool>(resident, "hiddenInsideHome"), Is.True);
                Assert.That(GetField<bool>(resident, "sleepingInsideHome"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(transientRoot);
                Object.DestroyImmediate(actorObject);
                Object.DestroyImmediate(originalSprite);
                Object.DestroyImmediate(originalTexture);
            }
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }
    }
}
