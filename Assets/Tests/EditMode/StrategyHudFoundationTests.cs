using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.Tests
{
    public sealed class StrategyHudFoundationTests
    {
        [TestCase("not_affordable", "Not enough construction materials.")]
        [TestCase("no_builder_access", "Builders cannot reach this site.")]
        [TestCase("foundation_unexplored@12,9", "Explore this area before building here.")]
        [TestCase("foundation_occupied@3,4", "Another structure already occupies this space.")]
        [TestCase("no_iron_deposit_under_mine", "Place the Mine over a discovered Iron deposit.")]
        [TestCase("no_water_access", "The Fisher Hut needs accessible fishing water.")]
        public void PlacementReasons_ArePlayerReadable(string reason, string expected)
        {
            Assert.That(StrategyBuildPlacementFeedbackText.FormatFailureReason(reason), Is.EqualTo(expected));
        }

        [Test]
        public void HudScaler_UsesResponsiveSixteenByNineReference()
        {
            GameObject root = new("HudScalerTest", typeof(Canvas), typeof(CanvasScaler));
            try
            {
                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                StrategyHudStyle.ConfigureScaler(scaler);

                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
                Assert.That(scaler.referenceResolution.x / scaler.referenceResolution.y, Is.EqualTo(16f / 9f).Within(0.001f));
                Assert.That(scaler.referenceResolution.x, Is.InRange(1280f, 1883f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HudPalette_HasDistinctStatusColors()
        {
            Assert.That(StrategyHudStyle.Success, Is.Not.EqualTo(StrategyHudStyle.Danger));
            Assert.That(StrategyHudStyle.Primary, Is.Not.EqualTo(StrategyHudStyle.Secondary));
            Assert.That(StrategyHudStyle.TextPrimary.grayscale, Is.GreaterThan(StrategyHudStyle.Background.grayscale));
        }

        [Test]
        public void HearthLedgerPanelFrame_IsImportedAsSlicedSprite()
        {
            Sprite frame = Resources.Load<Sprite>("UI/HearthLedger/PanelFrame");

            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.border.x, Is.GreaterThan(0f));
            Assert.That(frame.border.y, Is.GreaterThan(0f));
            Assert.That(frame.border.z, Is.GreaterThan(0f));
            Assert.That(frame.border.w, Is.GreaterThan(0f));
        }

        [Test]
        public void PanelAndInsetStyles_UseSeparateVisualLayers()
        {
            GameObject root = new("HudStyleTest", typeof(RectTransform), typeof(Image));
            try
            {
                Image image = root.GetComponent<Image>();
                StrategyHudStyle.StylePanel(image, new Color(0.1f, 0.2f, 0.3f, 0.77f));
                Sprite panelSprite = image.sprite;

                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(image.color.a, Is.EqualTo(0.77f).Within(0.001f));
                Assert.That(image.color.r, Is.EqualTo(1f).Within(0.001f));

                StrategyHudStyle.StyleInset(image, StrategyHudStyle.Surface);
                Assert.That(image.sprite, Is.Not.SameAs(panelSprite));
                Assert.That(image.color, Is.EqualTo(StrategyHudStyle.Surface));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RailModuleStyle_KeepsAnExplicitCompactSprite()
        {
            GameObject root = new("RailStyleTest", typeof(RectTransform), typeof(Image));
            try
            {
                Image image = root.GetComponent<Image>();
                StrategyHudStyle.StyleRailModule(image, true);

                Assert.That(image.sprite, Is.Not.Null);
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(image.pixelsPerUnitMultiplier, Is.GreaterThan(1f));
                Assert.That(image.raycastTarget, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildButtonTooltip_IsDisabledWhileMenuIsOpen()
        {
            GameObject root = new("BuildMenuHudTest");
            GameObject eventSystem = new("EventSystem", typeof(EventSystem));
            eventSystem.transform.SetParent(root.transform, false);
            StrategyHudSfxAudio existingAudio = Object.FindAnyObjectByType<StrategyHudSfxAudio>();
            try
            {
                StrategyBuildMenuController menu = root.AddComponent<StrategyBuildMenuController>();
                menu.ToggleMenu();
                StrategyHudTooltip tooltip = FindNamed<StrategyHudTooltip>(root, "BuildButton");

                Assert.That(menu.IsMenuOpen, Is.True);
                Assert.That(tooltip.enabled, Is.False);

                menu.CloseAll();
                Assert.That(menu.IsMenuOpen, Is.False);
                Assert.That(tooltip.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                DestroyNewHudSfxAudio(existingAudio);
            }
        }

        [Test]
        public void BuildItemTooltip_PrefersAreaAboveCards()
        {
            GameObject root = new("BuildItemTooltipTest");
            try
            {
                StrategyBuildMenuController menu =
                    root.AddComponent<StrategyBuildMenuController>();
                menu.ToggleMenu();

                StrategyHudTooltip tooltip =
                    FindNamed<StrategyHudTooltip>(root, "BuildItem_LumberjackCamp");

                Assert.That(
                    tooltip.Placement,
                    Is.EqualTo(StrategyHudTooltipPlacement.Above));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildHud_UsesCompactTwoRowBottomFootprint()
        {
            GameObject root = new("CompactBuildHudTest");
            StrategyHudSfxAudio existingAudio = Object.FindAnyObjectByType<StrategyHudSfxAudio>();
            try
            {
                StrategyBuildMenuController menu = root.AddComponent<StrategyBuildMenuController>();
                menu.ToggleMenu();
                FindNamed<Button>(root, "BuildCategory_Extraction").onClick.Invoke();
                RectTransform buildButton = FindNamed<RectTransform>(root, "BuildButton");
                RectTransform categoryDock = FindNamed<RectTransform>(root, "BuildCategoryDock");
                RectTransform subcategoryDock = FindNamed<RectTransform>(root, "BuildSubcategoryDock");
                RectTransform itemTray = FindNamed<RectTransform>(root, "BuildItemTray");

                float closedTop = TopFromBottom(buildButton);
                float categoryTop = TopFromBottom(categoryDock);
                float subcategoryBottom = BottomFromBottom(subcategoryDock);
                float trayBottom = BottomFromBottom(itemTray);
                float browseTop = Mathf.Max(TopFromBottom(subcategoryDock), TopFromBottom(itemTray));

                Assert.That(closedTop, Is.LessThanOrEqualTo(56f));
                Assert.That(subcategoryBottom, Is.GreaterThanOrEqualTo(categoryTop));
                Assert.That(trayBottom, Is.EqualTo(subcategoryBottom).Within(1f));
                Assert.That(browseTop, Is.LessThanOrEqualTo(240f), "Build browsing exceeded the absolute HUD footprint cap.");
                Assert.That(browseTop, Is.LessThanOrEqualTo(160f), "Persistent build controls should fit the minimal two-row target.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                DestroyNewHudSfxAudio(existingAudio);
            }
        }

        [TestCase(1280f, 720f, 1f)]
        [TestCase(1484f, 839f, 1f)]
        [TestCase(1280f, 720f, 1.25f)]
        public void BuildItemTooltip_DoesNotOverlapBuildNavigation(
            float screenWidth,
            float screenHeight,
            float uiScale)
        {
            float referenceWidth = StrategyHudStyle.ReferenceResolution.x / uiScale;
            float referenceHeight = StrategyHudStyle.ReferenceResolution.y / uiScale;
            float scale = Mathf.Sqrt(
                screenWidth / referenceWidth * screenHeight / referenceHeight);
            float canvasWidth = screenWidth / scale;
            float canvasHeight = screenHeight / scale;
            float canvasBottom = canvasHeight * -0.5f;
            Rect item = new(-260f, canvasBottom + 72f, 126f, 68f);
            Rect itemTray = new(-270f, canvasBottom + 66f, 542f, 80f);
            Rect subcategoryDock = new(-380f, canvasBottom + 66f, 104f, 80f);
            Rect categoryDock = new(-380f, canvasBottom + 14f, 760f, 48f);
            Vector2 panelSize = new(StrategyHudTooltipPresenter.Width, 148f);

            Rect tooltip = StrategyHudTooltipPresenter.ResolvePanelRect(
                item,
                new Vector2(canvasWidth, canvasHeight),
                panelSize,
                StrategyHudTooltipPlacement.Above);

            Assert.That(
                tooltip.yMin,
                Is.GreaterThanOrEqualTo(item.yMax + 8f));
            Assert.That(tooltip.Overlaps(itemTray), Is.False);
            Assert.That(tooltip.Overlaps(subcategoryDock), Is.False);
            Assert.That(tooltip.Overlaps(categoryDock), Is.False);
            Assert.That(
                tooltip.yMax,
                Is.LessThanOrEqualTo(canvasHeight * 0.5f - StrategyHudTooltipPresenter.EdgeMargin));
        }

        [Test]
        public void HousingRequiresExplicitHouseSelection_ThenBrowsingExtractionClearsIt()
        {
            GameObject root = new("BuildMenuCategoryTest");
            StrategyHudSfxAudio existingAudio = Object.FindAnyObjectByType<StrategyHudSfxAudio>();
            bool previousInstant = StrategyDebugOptions.InstantConstructionEnabled;
            try
            {
                StrategyDebugOptions.SetInstantConstructionEnabled(true);
                StrategyBuildMenuController menu = root.AddComponent<StrategyBuildMenuController>();
                menu.ToggleMenu();

                FindNamed<Button>(root, "BuildCategory_Housing").onClick.Invoke();
                Assert.That(menu.ActiveTool, Is.EqualTo(StrategyBuildTool.None));
                Assert.That(menu.IsMenuOpen, Is.True, "Housing should only reveal its item card.");

                FindNamed<Button>(root, "BuildItem_House").onClick.Invoke();
                Assert.That(menu.ActiveTool, Is.EqualTo(StrategyBuildTool.House));
                Assert.That(menu.IsMenuOpen, Is.False, "Placement should replace the browsing palette.");

                if (!menu.IsMenuOpen)
                {
                    menu.ToggleMenu();
                }

                FindNamed<Button>(root, "BuildCategory_Extraction").onClick.Invoke();
                Assert.That(menu.ActiveTool, Is.EqualTo(StrategyBuildTool.None));
            }
            finally
            {
                StrategyDebugOptions.SetInstantConstructionEnabled(previousInstant);
                Object.DestroyImmediate(root);
                DestroyNewHudSfxAudio(existingAudio);
            }
        }

        [Test]
        public void CalendarPanel_UsesTwoTextRowsAndKeepsProgressClear()
        {
            GameObject root = new("TopStatusHudTest");
            try
            {
                StrategyTopStatusHudController controller =
                    root.AddComponent<StrategyTopStatusHudController>();
                controller.Configure(null);
                RectTransform panel = root.transform.Find(
                    "TopStatusHudCanvas/CalendarTimePanel") as RectTransform;
                RectTransform time = panel?.Find("CalendarTimeText") as RectTransform;
                RectTransform phase = panel?.Find("CalendarPhaseText") as RectTransform;
                RectTransform track = panel?.Find("DayProgressTrack") as RectTransform;
                Assert.That(panel, Is.Not.Null);
                Assert.That(time, Is.Not.Null);
                Assert.That(phase, Is.Not.Null);
                Assert.That(track, Is.Not.Null);
                Assert.That(panel.Find("CalendarReadinessText"), Is.Null);

                Vector3[] phaseCorners = new Vector3[4];
                Vector3[] trackCorners = new Vector3[4];
                phase.GetWorldCorners(phaseCorners);
                track.GetWorldCorners(trackCorners);
                float phaseBottom = panel.InverseTransformPoint(phaseCorners[0]).y;
                float trackTop = panel.InverseTransformPoint(trackCorners[1]).y;

                Assert.That(phaseBottom, Is.GreaterThan(trackTop + 1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ProfessionPanel_TextStaysInsideOrnateFrame()
        {
            GameObject root = new("ProfessionHudLayoutTest");
            EventSystem existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
            StrategyHudSfxAudio existingAudio = Object.FindAnyObjectByType<StrategyHudSfxAudio>();
            try
            {
                StrategyProfessionHudController professions =
                    root.AddComponent<StrategyProfessionHudController>();
                professions.Configure(null);
                RectTransform panel = root.transform.Find(
                    "ProfessionHudCanvas/ProfessionPanel") as RectTransform;
                RectTransform title = panel?.Find("Title") as RectTransform;
                RectTransform subtitle = panel?.Find("Subtitle") as RectTransform;
                RectTransform autoWorkforce = panel?.Find("AutoWorkforce") as RectTransform;
                RectTransform viewport = panel?.Find("ListViewport") as RectTransform;
                RectTransform scrollbar = panel?.Find("Scrollbar") as RectTransform;
                Text freeWorkers = panel?.Find("FreeWorkers")?.GetComponent<Text>();
                Text actionStatus = panel?.Find("ActionStatus")?.GetComponent<Text>();
                Assert.That(panel, Is.Not.Null);
                Assert.That(title, Is.Not.Null);
                Assert.That(subtitle, Is.Not.Null);
                Assert.That(autoWorkforce, Is.Not.Null);
                Assert.That(viewport, Is.Not.Null);
                Assert.That(scrollbar, Is.Not.Null);
                Assert.That(freeWorkers, Is.Not.Null);
                Assert.That(actionStatus, Is.Not.Null);

                Assert.That(title.offsetMin.x, Is.GreaterThanOrEqualTo(40f));
                Assert.That(subtitle.offsetMin.x, Is.GreaterThanOrEqualTo(40f));
                Assert.That(autoWorkforce.offsetMin.x, Is.GreaterThanOrEqualTo(28f));
                Assert.That(autoWorkforce.offsetMax.x, Is.LessThanOrEqualTo(-28f));
                Assert.That(viewport.offsetMin.x, Is.GreaterThanOrEqualTo(32f));
                Assert.That(viewport.offsetMax.x, Is.LessThanOrEqualTo(-61f));
                Assert.That(scrollbar.offsetMax.x, Is.LessThanOrEqualTo(-41f));
                Assert.That(scrollbar.offsetMin.y, Is.EqualTo(viewport.offsetMin.y).Within(0.01f));
                Assert.That(scrollbar.offsetMax.y, Is.EqualTo(viewport.offsetMax.y).Within(0.01f));
                Assert.That(freeWorkers.rectTransform.offsetMin.x, Is.GreaterThanOrEqualTo(40f));
                Assert.That(freeWorkers.rectTransform.offsetMax.x, Is.LessThanOrEqualTo(-310f));
                Assert.That(actionStatus.rectTransform.offsetMin.x, Is.GreaterThanOrEqualTo(310f));
                Assert.That(actionStatus.rectTransform.offsetMax.x, Is.LessThanOrEqualTo(-40f));
                Assert.That(freeWorkers.rectTransform.offsetMin.y, Is.GreaterThanOrEqualTo(48f));
                Assert.That(actionStatus.rectTransform.offsetMin.y, Is.GreaterThanOrEqualTo(48f));
                Assert.That(
                    viewport.offsetMin.y - freeWorkers.rectTransform.offsetMax.y,
                    Is.GreaterThanOrEqualTo(20f));
                Assert.That(freeWorkers.resizeTextForBestFit, Is.True);
                Assert.That(actionStatus.resizeTextForBestFit, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                DestroyNewHudSfxAudio(existingAudio);
                if (existingEventSystem == null)
                {
                    EventSystem created = Object.FindAnyObjectByType<EventSystem>();
                    if (created != null)
                    {
                        Object.DestroyImmediate(created.gameObject);
                    }
                }
            }
        }

        private static T FindNamed<T>(GameObject root, string name)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.name == name)
                {
                    return components[i];
                }
            }

            Assert.Fail("Missing " + typeof(T).Name + ": " + name);
            return null;
        }

        private static float BottomFromBottom(RectTransform rect)
        {
            return EdgeFromCanvasBottom(rect, 0);
        }

        private static float TopFromBottom(RectTransform rect)
        {
            return EdgeFromCanvasBottom(rect, 1);
        }

        private static float EdgeFromCanvasBottom(RectTransform rect, int cornerIndex)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float localY = canvasRect.InverseTransformPoint(corners[cornerIndex]).y;
            return localY - canvasRect.rect.yMin;
        }

        private static void DestroyNewHudSfxAudio(StrategyHudSfxAudio existing)
        {
            if (existing != null)
            {
                return;
            }

            StrategyHudSfxAudio created = Object.FindAnyObjectByType<StrategyHudSfxAudio>();
            if (created != null)
            {
                Object.DestroyImmediate(created.gameObject);
            }
        }
    }
}
