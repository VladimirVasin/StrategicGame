using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCityItemRewardRevealTests
    {
        private GameObject root;
        private StrategyInputRouter inputRouter;
        private StrategyTimeScaleController timeScale;
        private StrategyCityItemRewardRevealController reveal;
        private float previousTimeScale;
        private float previousFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;

            root = new GameObject("City Item Reward Reveal Test Root");
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);

            GameObject routerObject = new("Input Router");
            routerObject.transform.SetParent(root.transform, false);
            inputRouter = routerObject.AddComponent<StrategyInputRouter>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            Assert.That(inputRouter.Configure(actions), Is.True, inputRouter.ConfigurationError);

            GameObject timeObject = new("Time Scale");
            timeObject.transform.SetParent(root.transform, false);
            timeScale = timeObject.AddComponent<StrategyTimeScaleController>();
            timeScale.Configure();

            GameObject revealObject = new("Reward Reveal");
            revealObject.transform.SetParent(root.transform, false);
            reveal = revealObject.AddComponent<StrategyCityItemRewardRevealController>();
            reveal.Configure(timeScale, inputRouter, null);
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
        public void ConfiguredViewStartsHiddenAboveNarrativeStoryCanvas()
        {
            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.Hidden));
            Assert.That(reveal.IsOpen, Is.False);
            Assert.That(reveal.RewardCanvas, Is.Not.Null);
            Assert.That(reveal.RewardCanvas.sortingOrder, Is.EqualTo(320));
            Assert.That(reveal.RewardCanvas.gameObject.activeSelf, Is.False);
            Transform artwork = reveal.transform.Find(
                "CityItemRewardRevealCanvas/RewardStage/RewardCard/InnerCardFrame/ArtworkFrame/Artwork");
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.GetComponent<Image>().preserveAspect, Is.True);
        }

        [Test]
        public void RejectedRewardLeavesRequestedSpeedUnchanged()
        {
            timeScale.SetRequestedScale(3f);

            Assert.That(reveal.TryShow(null, null), Is.False);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(3f));
            Assert.That(Time.timeScale, Is.EqualTo(3f));
        }

        [Test]
        public void ShowConfirmAndFlightHoldSimulationUntilCompletion()
        {
            int completionCount = 0;
            timeScale.SetRequestedScale(3f);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(3f));

            Assert.That(reveal.TryShow(CreateCatsDefinition(), null, () => completionCount++), Is.True);

            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.Revealing));
            Assert.That(reveal.ActiveItemId, Is.EqualTo("cats"));
            Assert.That(reveal.DisplayedTitle, Is.EqualTo("CATS"));
            Assert.That(reveal.DisplayedEffect, Does.Contain("hunt mice"));
            Assert.That(reveal.HoldsInputContext, Is.True);
            Assert.That(reveal.HoldsPauseLock, Is.True);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
            Assert.That(inputRouter.BlockedChannels, Is.EqualTo(StrategyInputChannel.All));
            Assert.That(inputRouter.IsCancelSwallowed, Is.True);
            Assert.That(timeScale.IsPausedByLock, Is.True);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(reveal.TryConfirm(), Is.False);
            Image backdrop = reveal.transform.Find(
                "CityItemRewardRevealCanvas/CinematicBackdrop").GetComponent<Image>();
            Assert.That(backdrop.color.a, Is.GreaterThanOrEqualTo(0.70f));

            reveal.Tick(1f);

            Assert.That(reveal.IsAwaitingConfirmation, Is.True);
            Assert.That(reveal.IsConfirmInteractable, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(completionCount, Is.Zero);
            Assert.That(reveal.TryConfirm(), Is.True);
            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.FlyingToChest));
            Assert.That(Time.timeScale, Is.Zero);

            reveal.Tick(1f);

            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.Hidden));
            Assert.That(reveal.HoldsInputContext, Is.False);
            Assert.That(reveal.HoldsPauseLock, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(completionCount, Is.EqualTo(1));
        }

        [Test]
        public void ReducedMotionUsesStableAwaitingPoseAndShortFlight()
        {
            reveal.SetReducedMotion(true);
            Assert.That(reveal.TryShow(CreateCatsDefinition(), null), Is.True);

            reveal.Tick(0.13f);

            Assert.That(reveal.IsAwaitingConfirmation, Is.True);
            Vector2 initialPosition = reveal.CardRoot.anchoredPosition;
            reveal.Tick(5f);
            Assert.That(reveal.CardRoot.anchoredPosition, Is.EqualTo(initialPosition));

            Assert.That(reveal.TryConfirm(), Is.True);
            reveal.Tick(0.17f);
            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.Hidden));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DisableWhileOpenReleasesInputAndPauseWithoutDeliveringCallback()
        {
            int completionCount = 0;
            Assert.That(reveal.TryShow(CreateCatsDefinition(), null, () => completionCount++), Is.True);

            reveal.enabled = false;

            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.Hidden));
            Assert.That(reveal.HoldsInputContext, Is.False);
            Assert.That(reveal.HoldsPauseLock, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(completionCount, Is.Zero);
            Assert.That(reveal.RewardCanvas.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SecondRewardCannotStackOverActivePresentation()
        {
            Assert.That(reveal.TryShow(CreateCatsDefinition(), null), Is.True);
            Assert.That(reveal.TryShow(CreateCatsDefinition(), null), Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
        }

        [Test]
        public void FlightApproachesActualHudChestDestinationBeforeCleanup()
        {
            StrategyCityInventoryHudController hud = CreateInventoryHud();
            reveal.Configure(timeScale, inputRouter, hud);
            Assert.That(hud.TryGetRewardDestination(out Vector2 expectedScreenPoint), Is.True);
            Assert.That(reveal.TryShow(CreateCatsDefinition(), null), Is.True);
            reveal.Tick(1f);
            Assert.That(reveal.TryConfirm(), Is.True);

            reveal.Tick(0.61f);

            Vector2 cardScreenPoint = RectTransformUtility.WorldToScreenPoint(
                null,
                reveal.CardRoot.TransformPoint(reveal.CardRoot.rect.center));
            Assert.That(
                Vector2.Distance(cardScreenPoint, expectedScreenPoint),
                Is.LessThan(10f));
            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.FlyingToChest));

            reveal.Tick(0.02f);
            Assert.That(reveal.State, Is.EqualTo(StrategyCityItemRewardRevealState.Hidden));
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
        }

        private StrategyCityInventoryHudController CreateInventoryHud()
        {
            GameObject inventoryObject = new("City Inventory");
            inventoryObject.transform.SetParent(root.transform, false);
            StrategyCityInventory inventory = inventoryObject.AddComponent<StrategyCityInventory>();
            inventory.Configure(new StrategyCityItemCatalog(new[] { CreateCatsDefinition() }));

            GameObject hudObject = new("City Inventory HUD");
            hudObject.transform.SetParent(root.transform, false);
            StrategyCityInventoryHudController hud =
                hudObject.AddComponent<StrategyCityInventoryHudController>();
            hud.Configure(inventory, inputRouter);
            return hud;
        }

        private static StrategyCityItemDefinition CreateCatsDefinition()
        {
            return new StrategyCityItemDefinition(
                "cats",
                "Cats",
                1,
                "Quiet hunters have chosen the settlement as their home.",
                "Cats patrol the stores and hunt mice, keeping their numbers down.");
        }
    }
}
