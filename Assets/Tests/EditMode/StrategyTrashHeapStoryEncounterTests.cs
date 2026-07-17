using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyTrashHeapStoryEncounterTests
    {
        private GameObject root;
        private StrategyInputRouter inputRouter;
        private StrategyTimeScaleController timeScale;
        private float previousTimeScale;
        private float previousFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            root = new GameObject("Trash Heap Encounter Tests");
            new GameObject("EventSystem", typeof(EventSystem)).transform.SetParent(root.transform, false);

            inputRouter = root.AddComponent<StrategyInputRouter>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(inputRouter.Configure(actions), Is.True, inputRouter.ConfigurationError);
            timeScale = root.AddComponent<StrategyTimeScaleController>();
            timeScale.SetInputRouter(inputRouter);
            timeScale.Configure();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            Time.timeScale = previousTimeScale;
            Time.fixedDeltaTime = previousFixedDeltaTime;
        }

        [Test]
        public void AuthoredSpritesImportAsPointFilteredTransparentSprites()
        {
            AssertSprite(
                "Assets/Resources/Visual/StoryPoints/TrashHeap.png",
                new Vector2(0.5f, 0.08f));
            AssertSprite(
                "Assets/Resources/Visual/StoryPoints/TrashHeapSearched.png",
                new Vector2(0.5f, 0.08f));
            AssertSprite(
                "Assets/Resources/Visual/ResidentItems/HoleySpoon.png",
                new Vector2(0.5f, 0.5f));
        }

        [Test]
        public void ChoiceButtonReleasesDialogOwnershipBeforeCallback()
        {
            StrategyPointOfInterestDialogController dialog =
                root.AddComponent<StrategyPointOfInterestDialogController>();
            dialog.SetInputRouter(inputRouter);
            bool callbackCalled = false;
            int contextsAtCallback = -1;
            bool shieldAtCallback = true;
            dialog.ShowChoice(
                "Гора мусора",
                "Обыскать её?",
                "Да",
                "Нет",
                () =>
                {
                    callbackCalled = true;
                    contextsAtCallback = inputRouter.ActiveContextCount;
                    shieldAtCallback = dialog.IsInputShieldActive;
                },
                null);

            Button yes = dialog.transform.Find(
                "PointOfInterestDialogCanvas/Root/Panel/YesButton").GetComponent<Button>();
            Assert.That(yes, Is.Not.Null);
            yes.onClick.Invoke();

            Assert.That(callbackCalled, Is.True);
            Assert.That(contextsAtCallback, Is.Zero);
            Assert.That(shieldAtCallback, Is.False);
        }

        [Test]
        public void PersonalRewardCardOwnsPauseUntilExactConfirmation()
        {
            StrategyResidentItemRewardRevealController reveal =
                root.AddComponent<StrategyResidentItemRewardRevealController>();
            reveal.Configure(timeScale, inputRouter);
            StrategyResidentAgent resident = new GameObject("Scout").AddComponent<StrategyResidentAgent>();
            resident.transform.SetParent(root.transform, false);
            StrategyResidentItemDefinition definition =
                StrategyResidentItemCatalog.Production.Definitions[0];
            Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/Visual/ResidentItems/HoleySpoon.png");
            int completed = 0;

            Assert.That(
                reveal.TryShow(
                    definition,
                    resident,
                    artwork,
                    "Вы нашли дырявую ложку",
                    () => completed++),
                Is.True);
            Assert.That(reveal.DisplayedTitle, Is.EqualTo("Вы нашли дырявую ложку"));
            Assert.That(reveal.DisplayedDescription, Is.EqualTo(definition.Description));
            Assert.That(reveal.HoldsInputContext, Is.True);
            Assert.That(reveal.HoldsPauseLock, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            reveal.Tick(1f);
            Assert.That(reveal.IsAwaitingConfirmation, Is.True);
            Assert.That(reveal.IsConfirmInteractable, Is.True);
            Assert.That(reveal.TryConfirm(), Is.True);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(reveal.IsOpen, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
        }

        [Test]
        public void SearchSequenceOwnsFiveSecondRummageBeat()
        {
            Assert.That(StrategyTrashHeapSearchCinematic.SearchDurationSeconds, Is.EqualTo(5f));
            Assert.That(StrategyResidentSpriteFactory.TrashSearchFrameCount, Is.EqualTo(12));
        }

        private static void AssertSprite(string path, Vector2 expectedPivot)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.That(sprite, Is.Not.Null, path);
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Vector2 normalizedPivot = new(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height);
            Assert.That(normalizedPivot.x, Is.EqualTo(expectedPivot.x).Within(0.01f));
            Assert.That(normalizedPivot.y, Is.EqualTo(expectedPivot.y).Within(0.01f));
        }
    }
}
