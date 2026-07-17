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
        public void LauncherUsesReservedTopRowSlotBetweenTreasuryAndPopulation()
        {
            Assert.That(hud.LauncherRoot.anchoredPosition, Is.EqualTo(new Vector2(204f, -18f)));
            Assert.That(hud.LauncherRoot.sizeDelta, Is.EqualTo(new Vector2(178f, 42f)));
            Assert.That(hud.HudCanvas.sortingOrder, Is.EqualTo(170));
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
                    "Council decisions carry more weight.")
            });
        }
    }
}
