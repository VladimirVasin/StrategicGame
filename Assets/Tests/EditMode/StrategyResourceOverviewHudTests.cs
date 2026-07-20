using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResourceOverviewHudTests
    {
        private GameObject root;
        private StrategyInputRouter inputRouter;
        private StrategyResourceOverviewHudController hud;
        private float previousTimeScale;
        private Locale previousLocale;
        private bool hadLanguagePreference;
        private string previousLanguagePreference;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            previousLocale = LocalizationSettings.SelectedLocale;
            hadLanguagePreference = PlayerPrefs.HasKey(
                StrategyLocalization.LanguagePreferenceKey);
            previousLanguagePreference = PlayerPrefs.GetString(
                StrategyLocalization.LanguagePreferenceKey,
                StrategyLocalization.RussianLocaleCode);
            Assert.That(
                StrategyLocalization.SetLanguage(StrategyGameLanguage.English),
                Is.True);
            root = new GameObject("Resource Overview HUD Test Root");

            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);

            GameObject routerObject = new("Input Router");
            routerObject.transform.SetParent(root.transform, false);
            inputRouter = routerObject.AddComponent<StrategyInputRouter>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            Assert.That(inputRouter.Configure(actions), Is.True, inputRouter.ConfigurationError);

            GameObject hudObject = new("Resource Overview HUD");
            hudObject.transform.SetParent(root.transform, false);
            hud = hudObject.AddComponent<StrategyResourceOverviewHudController>();
            hud.Configure(inputRouter);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = previousTimeScale;
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            StrategyGameLanguage restoreLanguage = previousLocale != null
                ? StrategyLocalization.FromLocaleCode(previousLocale.Identifier.Code)
                : StrategyLocalization.FromLocaleCode(previousLanguagePreference);
            StrategyLocalization.SetLanguage(restoreLanguage);
            LocalizationSettings.SelectedLocale = previousLocale;
            if (hadLanguagePreference)
            {
                PlayerPrefs.SetString(
                    StrategyLocalization.LanguagePreferenceKey,
                    previousLanguagePreference);
            }
            else
            {
                PlayerPrefs.DeleteKey(StrategyLocalization.LanguagePreferenceKey);
            }
        }

        [Test]
        public void LauncherUsesExpectedSlotAndOnlyShowsLogsAndStone()
        {
            Assert.That(hud.LauncherRoot.anchoredPosition, Is.EqualTo(new Vector2(16f, -5f)));
            Assert.That(hud.LauncherRoot.sizeDelta, Is.EqualTo(new Vector2(270f, 60f)));

            Button launcher = hud.LauncherRoot.GetComponent<Button>();
            Assert.That(launcher, Is.Not.Null);
            Assert.That(launcher.interactable, Is.True);
            Assert.That(launcher.targetGraphic, Is.Not.Null);
            Assert.That(hud.LauncherSummary, Does.Contain("Logs"));
            Assert.That(hud.LauncherSummary, Does.Contain("Stone"));
            Assert.That(hud.LauncherSummary, Does.Not.Contain("Planks"));
        }

        [Test]
        public void PopupRendersEveryResourceAndKeepsZeroRowsVisible()
        {
            hud.SetOpen(true, true, false);

            RectTransform panel = hud.PanelRoot;
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.gameObject.activeInHierarchy, Is.True);

            int expectedCount = 0;
            foreach (StrategyResourceType resource in Enum.GetValues(typeof(StrategyResourceType)))
            {
                if (resource == StrategyResourceType.None)
                {
                    continue;
                }

                expectedCount++;
                Transform row = panel.Find("ResourceRow_" + resource);
                Assert.That(row, Is.Not.Null, "Missing row for " + resource);
                Assert.That(row.gameObject.activeInHierarchy, Is.True, resource + " row is hidden");
                Assert.That(
                    row.Find("Stored")?.GetComponent<Text>()?.text,
                    Is.EqualTo("0"),
                    resource + " zero value is not visible");
                Assert.That(
                    hud.TryGetDisplayedCounts(resource, out int stored, out int available),
                    Is.True,
                    resource + " is not registered by the HUD");
                Assert.That(stored, Is.Zero, resource + " stored value");
                Assert.That(available, Is.Zero, resource + " available value");
            }

            int renderedRowCount = 0;
            for (int index = 0; index < panel.childCount; index++)
            {
                if (panel.GetChild(index).name.StartsWith(
                        "ResourceRow_",
                        StringComparison.Ordinal))
                {
                    renderedRowCount++;
                }
            }

            Assert.That(expectedCount, Is.EqualTo(20));
            Assert.That(hud.VisibleResourceCount, Is.EqualTo(expectedCount));
            Assert.That(renderedRowCount, Is.EqualTo(expectedCount));
        }

        [Test]
        public void ImmediateOpenBlocksWorldChannelsWithoutChangingTimeScale()
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
        public void BackdropIsSeparateFromPanelAndCannotCatchPanelClicks()
        {
            Transform overlay = hud.transform.Find(
                "StrategyResourceOverviewHudCanvas/ResourceOverviewOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.GetComponent<Button>(), Is.Null);
            Assert.That(overlay.Find("OutsideClickShield")?.GetComponent<Button>(), Is.Not.Null);

            Transform panel = overlay.Find("ResourceOverviewPanel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponentInParent<Button>(), Is.Null);
        }

        [Test]
        public void PanelFitsInsideLogical1280By720Viewport()
        {
            RectTransform panel = hud.PanelRoot;
            float left = panel.anchoredPosition.x;
            float top = -panel.anchoredPosition.y;
            float right = left + panel.sizeDelta.x;
            float bottom = top + panel.sizeDelta.y;

            Assert.That(left, Is.GreaterThanOrEqualTo(0f));
            Assert.That(top, Is.GreaterThanOrEqualTo(0f));
            Assert.That(right, Is.LessThanOrEqualTo(1280f));
            Assert.That(bottom, Is.LessThanOrEqualTo(720f));
        }

        [Test]
        public void RepeatedLauncherClickTogglesPopupOpenAndClosed()
        {
            Button launcher = hud.LauncherRoot.GetComponent<Button>();
            Assert.That(launcher, Is.Not.Null);

            launcher.onClick.Invoke();
            Assert.That(hud.IsOpen, Is.True);

            launcher.onClick.Invoke();
            Assert.That(hud.IsOpen, Is.False);
            Assert.That(hud.IsClosing, Is.True);
        }

        [Test]
        public void LifecycleDisableReleasesInputContextAndHidesCanvas()
        {
            hud.SetOpen(true, true, false);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));

            MethodInfo onDisable = typeof(StrategyResourceOverviewHudController).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDisable, Is.Not.Null);
            onDisable.Invoke(hud, null);

            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(hud.HoldsInputContext, Is.False);
            Assert.That(hud.IsOpen, Is.False);
            Assert.That(hud.HudCanvas.gameObject.activeSelf, Is.False);
        }
    }
}
