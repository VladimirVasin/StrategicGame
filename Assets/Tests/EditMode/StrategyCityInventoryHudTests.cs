using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCityInventoryHudTests
    {
        private GameObject root;
        private StrategyInputRouter inputRouter;
        private StrategyCityInventory inventory;
        private StrategyCityInventoryHudController hud;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            root = new GameObject("City Inventory HUD Test Root");

            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);

            GameObject routerObject = new("Input Router");
            routerObject.transform.SetParent(root.transform, false);
            inputRouter = routerObject.AddComponent<StrategyInputRouter>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            Assert.That(inputRouter.Configure(actions), Is.True, inputRouter.ConfigurationError);

            GameObject inventoryObject = new("City Inventory");
            inventoryObject.transform.SetParent(root.transform, false);
            inventory = inventoryObject.AddComponent<StrategyCityInventory>();
            inventory.Configure(CreateCatalog());

            GameObject hudObject = new("City Inventory HUD");
            hudObject.transform.SetParent(root.transform, false);
            hud = hudObject.AddComponent<StrategyCityInventoryHudController>();
            hud.Configure(inventory, inputRouter);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

        }

        [Test]
        public void EmptyInventoryShowsIntentionalEmptyStateWithoutBadge()
        {
            Assert.That(hud.VisibleItemCount, Is.Zero);
            Assert.That(hud.IsBadgeVisible, Is.False);
            Assert.That(hud.EmptyStateCopy, Does.Contain("The city chest is empty"));
            Assert.That(hud.EmptyStateCopy, Does.Contain("Special finds and keepsakes"));
        }

        [Test]
        public void InventoryChangeRefreshesBadgeAndSelectedDetails()
        {
            Assert.That(inventory.TryAdd("old-king-seal", 1), Is.True);

            Assert.That(hud.VisibleItemCount, Is.EqualTo(1));
            Assert.That(hud.IsBadgeVisible, Is.True);
            Assert.That(hud.DisplayedBadgeCount, Is.EqualTo(1));
            Assert.That(hud.SelectedItemTitle, Is.EqualTo("Old King's Seal"));
            Assert.That(hud.SelectedItemEffect, Is.EqualTo("Council decisions carry more weight."));
        }

        [Test]
        public void OwnedItemsRenderAsVerticalDescriptiveRows()
        {
            Assert.That(inventory.TryAdd("old-king-seal", 1), Is.True);
            Assert.That(inventory.TryAdd("survey-maps", 3), Is.True);

            RectTransform content = hud.transform.Find(
                    "CityInventoryHudCanvas/CityInventoryOverlay/CityInventoryPanel/ItemViewport/ItemContent")
                ?.GetComponent<RectTransform>();
            Assert.That(content, Is.Not.Null);
            Assert.That(content.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(content.GetComponent<GridLayoutGroup>(), Is.Null);

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            RectTransform uniqueRow = content.Find("ItemRow_0")?.GetComponent<RectTransform>();
            RectTransform stackedRow = content.Find("ItemRow_1")?.GetComponent<RectTransform>();
            Assert.That(uniqueRow, Is.Not.Null);
            Assert.That(stackedRow, Is.Not.Null);
            Assert.That(uniqueRow.anchoredPosition.y, Is.GreaterThan(stackedRow.anchoredPosition.y));
            Assert.That(
                uniqueRow.Find("Summary")?.GetComponent<Text>()?.text,
                Is.EqualTo("Council decisions carry more weight."));
            Assert.That(
                uniqueRow.Find("Quantity")?.GetComponent<Text>()?.text,
                Is.EqualTo("UNIQUE"));
            Assert.That(
                stackedRow.Find("Quantity")?.GetComponent<Text>()?.text,
                Is.EqualTo("x3"));
        }

        [Test]
        public void LauncherUsesReservedTopRowSlotBetweenTreasuryAndPopulation()
        {
            Assert.That(hud.LauncherRoot.anchoredPosition, Is.EqualTo(new Vector2(204f, -18f)));
            Assert.That(hud.LauncherRoot.sizeDelta, Is.EqualTo(new Vector2(178f, 42f)));
            Assert.That(hud.HudCanvas.sortingOrder, Is.EqualTo(170));
        }

        [Test]
        public void RewardDestinationUsesChestIconAndPulseResetsWithLifecycle()
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(hud.TryGetRewardDestination(out Vector2 destination), Is.True);

            RectTransform chestIcon = hud.transform.Find(
                    "CityInventoryHudCanvas/CityInventoryButton/ChestIconFrame/ChestIcon")
                ?.GetComponent<RectTransform>();
            Assert.That(chestIcon, Is.Not.Null);
            Vector2 expected = RectTransformUtility.WorldToScreenPoint(
                null,
                chestIcon.TransformPoint(chestIcon.rect.center));
            Assert.That(destination.x, Is.EqualTo(expected.x).Within(0.01f));
            Assert.That(destination.y, Is.EqualTo(expected.y).Within(0.01f));

            Time.timeScale = 0f;
            hud.PlayRewardReceivedFeedback();
            FieldInfo startedAt = typeof(StrategyCityInventoryHudController).GetField(
                "rewardFeedbackStartedAt",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo updateFeedback = typeof(StrategyCityInventoryHudController).GetMethod(
                "UpdateRewardReceivedFeedback",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(startedAt, Is.Not.Null);
            Assert.That(updateFeedback, Is.Not.Null);
            startedAt.SetValue(hud, Time.unscaledTime - 0.15f);
            updateFeedback.Invoke(hud, null);
            Assert.That(chestIcon.localScale.x, Is.GreaterThan(1.05f));

            MethodInfo onDisable = typeof(StrategyCityInventoryHudController).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDisable, Is.Not.Null);
            onDisable.Invoke(hud, null);
            Assert.That(chestIcon.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void BackdropIsSeparateFromPanelAndCannotCatchPanelClicks()
        {
            Transform overlay = hud.transform.Find("CityInventoryHudCanvas/CityInventoryOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.GetComponent<Button>(), Is.Null);
            Assert.That(overlay.Find("Backdrop")?.GetComponent<Button>(), Is.Not.Null);

            Transform panel = overlay.Find("CityInventoryPanel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponentInParent<Button>(), Is.Null);
        }

        [Test]
        public void OpenPanelBlocksWorldChannelsWithoutPausingAndCleansUpContext()
        {
            Time.timeScale = 0.75f;

            hud.SetOpen(true, true, false);

            Assert.That(hud.IsOpen, Is.True);
            Assert.That(hud.HoldsInputContext, Is.True);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
            Assert.That(
                inputRouter.BlockedChannels,
                Is.EqualTo(
                    StrategyInputChannel.Camera
                    | StrategyInputChannel.Gameplay
                    | StrategyInputChannel.Build));
            Assert.That(Time.timeScale, Is.EqualTo(0.75f));

            hud.SetOpen(false, true, false);

            Assert.That(hud.IsOpen, Is.False);
            Assert.That(hud.IsClosing, Is.False);
            Assert.That(hud.HoldsInputContext, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(0.75f));
        }

        [Test]
        public void LifecycleDisableReleasesInputContextAndHidesInteractiveCanvas()
        {
            hud.SetOpen(true, true, false);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));

            MethodInfo onDisable = typeof(StrategyCityInventoryHudController).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDisable, Is.Not.Null);
            onDisable.Invoke(hud, null);

            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(hud.HoldsInputContext, Is.False);
            Assert.That(hud.HudCanvas.gameObject.activeSelf, Is.False);

            MethodInfo onEnable = typeof(StrategyCityInventoryHudController).GetMethod(
                "OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onEnable, Is.Not.Null);
            onEnable.Invoke(hud, null);

            Assert.That(hud.HudCanvas.gameObject.activeSelf, Is.True);
            Assert.That(hud.IsOpen, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
        }

        private static StrategyCityItemCatalog CreateCatalog()
        {
            return new StrategyCityItemCatalog(new[]
            {
                new StrategyCityItemDefinition(
                    "old-king-seal",
                    "Old King's Seal",
                    1,
                    "A worn seal carried from the old capital.",
                    "Council decisions carry more weight."),
                new StrategyCityItemDefinition(
                    "survey-maps",
                    "Survey Maps",
                    5,
                    "Rolled maps copied by the settlement's scouts.",
                    "Nearby routes are easier to plan.")
            });
        }
    }
}
