using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyProfessionHudController : MonoBehaviour
    {
        private const float AnimationSpeed = 9f;
        private const float RowHeight = 56f;

        private static readonly StrategyProfessionType[] DisplayOrder =
        {
            StrategyProfessionType.Lumberjack,
            StrategyProfessionType.Stonecutter,
            StrategyProfessionType.Miner,
            StrategyProfessionType.CoalMiner,
            StrategyProfessionType.ClayDigger,
            StrategyProfessionType.Sawyer,
            StrategyProfessionType.Potter,
            StrategyProfessionType.Blacksmith,
            StrategyProfessionType.Hunter,
            StrategyProfessionType.Fisher,
            StrategyProfessionType.Forager,
            StrategyProfessionType.Scout,
            StrategyProfessionType.StorageWorker,
            StrategyProfessionType.Builder
        };

        private readonly ProfessionRow[] rows = new ProfessionRow[DisplayOrder.Length];
        private StrategyPopulationController population;
        private RectTransform panelRoot;
        private RectTransform viewportRoot;
        private RectTransform contentRoot;
        private ScrollRect professionScroll;
        private CanvasGroup panelGroup;
        private Text freeWorkersText;
        private Text actionStatusText;
        private Text buttonText;
        private bool initialized;
        private bool isOpen;
        private bool isDirty = true;
        private float panelT;

        public void Configure(
            StrategyPopulationController populationController,
            StrategyAutoWorkforceController autoController = null)
        {
            population = populationController != null
                ? populationController
                : population ?? UnityEngine.Object.FindAnyObjectByType<StrategyPopulationController>();
            SetAutoWorkforce(autoController);

            if (!initialized)
            {
                initialized = true;
                EnsureEventSystem();
                BuildUi();
            }

            isDirty = true;
            RefreshUi();
        }

        private void Awake()
        {
            Configure(null);
        }

        private void Update()
        {
            if (!initialized)
            {
                Configure(null);
            }

            RefreshInputContext();
            HandleInput();
            HandleManualScroll();
            UpdateAnimation();

            if (isDirty || (isOpen && Time.frameCount % 15 == 0))
            {
                RefreshUi();
            }
        }

        private void ToggleOpen()
        {
            isOpen = !isOpen;
            RefreshInputContext();
            isDirty = true;
            if (isOpen && professionScroll != null)
            {
                professionScroll.verticalNormalizedPosition = 1f;
            }

            StrategyHudSfxAudio.Play(isOpen ? StrategyHudSfxKind.Open : StrategyHudSfxKind.Close);
            StrategyDebugLogger.Info(
                "ProfessionHud",
                isOpen ? "Opened" : "Closed");
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            RefreshInputContext();
            isDirty = true;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
            StrategyDebugLogger.Info("ProfessionHud", "Closed");
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("ProfessionHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateTopButton(canvasObject.transform);
            CreatePanel(canvasObject.transform);
            RefreshUi();
            UpdateAnimation(true);
        }

        private void CreateTopButton(Transform parent)
        {
            RectTransform root = CreateUiObject("ProfessionButton", parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(18f, -106f);
            root.sizeDelta = new Vector2(178f, 42f);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.16f, 0.18f, 0.94f);

            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.36f);
            outline.effectDistance = new Vector2(1.3f, -1.3f);

            RectTransform icon = CreateUiObject("Icon", root).GetComponent<RectTransform>();
            SetTopLeft(icon, 10f, 7f, 28f, 28f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            buttonText = CreateText("Label", root, "Professions", 15, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.62f));
            buttonText.fontStyle = FontStyle.Bold;
            SetOffsets(buttonText.rectTransform, 46f, 0f, 12f, 0f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(ToggleOpen);
            ConfigureButtonColors(button, background.color);
            StrategyUiButtonFeedback.Attach(button);
        }

        private void CreatePanel(Transform parent)
        {
            panelRoot = CreateUiObject("ProfessionPanel", parent).GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 1f);
            panelRoot.anchorMax = new Vector2(0.5f, 1f);
            panelRoot.pivot = new Vector2(0.5f, 1f);
            panelRoot.anchoredPosition = new Vector2(0f, -76f);
            panelRoot.sizeDelta = new Vector2(620f, 620f);

            Image background = panelRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.09f, 0.09f, 0.96f);

            Outline outline = panelRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);

            panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();

            RectTransform accent = CreateUiObject("Accent", panelRoot).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(5f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.86f, 0.63f, 0.28f, 1f);
            accentImage.raycastTarget = false;

            Text title = CreateText("Title", panelRoot, "Professions", 25, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 24f, 18f, 84f, 34f);

            Text subtitle = CreateText("Subtitle", panelRoot, "settlement workers", 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            subtitle.fontStyle = FontStyle.Bold;
            SetTopStretch(subtitle.rectTransform, 24f, 52f, 84f, 20f);

            RectTransform closeRoot = CreateUiObject("Close", panelRoot).GetComponent<RectTransform>();
            closeRoot.anchorMin = new Vector2(1f, 1f);
            closeRoot.anchorMax = new Vector2(1f, 1f);
            closeRoot.pivot = new Vector2(1f, 1f);
            closeRoot.anchoredPosition = new Vector2(-18f, -18f);
            closeRoot.sizeDelta = new Vector2(38f, 34f);
            Image closeImage = closeRoot.gameObject.AddComponent<Image>();
            closeImage.color = new Color(0.10f, 0.14f, 0.15f, 0.95f);
            Button closeButton = closeRoot.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            ConfigureButtonColors(closeButton, closeImage.color);
            StrategyUiButtonFeedback.Attach(closeButton, StrategyUiButtonFeedbackProfile.Compact);
            Text closeText = CreateText("CloseText", closeRoot, "X", 16, TextAnchor.MiddleCenter, Color.white);
            closeText.fontStyle = FontStyle.Bold;
            SetOffsets(closeText.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform line = CreateUiObject("Line", panelRoot).GetComponent<RectTransform>();
            SetTopStretch(line, 24f, 84f, 24f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.22f);
            lineImage.raycastTarget = false;

            CreateAutoControls(panelRoot);

            RectTransform viewport = CreateUiObject("ListViewport", panelRoot).GetComponent<RectTransform>();
            viewportRoot = viewport;
            SetOffsets(viewport, 18f, 210f, 38f, 70f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            contentRoot = CreateUiObject("ListContent", viewport).GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = new Vector2(0f, 0f);
            contentRoot.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform scrollbarRoot = CreateUiObject("Scrollbar", panelRoot).GetComponent<RectTransform>();
            scrollbarRoot.anchorMin = new Vector2(1f, 0f);
            scrollbarRoot.anchorMax = new Vector2(1f, 1f);
            scrollbarRoot.pivot = new Vector2(1f, 0.5f);
            scrollbarRoot.offsetMin = new Vector2(-30f, 70f);
            scrollbarRoot.offsetMax = new Vector2(-18f, -96f);
            Image scrollbarTrack = scrollbarRoot.gameObject.AddComponent<Image>();
            scrollbarTrack.color = new Color(0f, 0f, 0f, 0.32f);

            RectTransform scrollbarHandle = CreateUiObject("Handle", scrollbarRoot).GetComponent<RectTransform>();
            SetOffsets(scrollbarHandle, 2f, 2f, 2f, 2f);
            Image scrollbarHandleImage = scrollbarHandle.gameObject.AddComponent<Image>();
            scrollbarHandleImage.color = new Color(0.86f, 0.70f, 0.42f, 0.88f);

            Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = scrollbarHandleImage;
            scrollbar.handleRect = scrollbarHandle;
            ConfigureScrollbarColors(scrollbar, scrollbarHandleImage.color);

            professionScroll = viewport.gameObject.AddComponent<ScrollRect>();
            professionScroll.content = contentRoot;
            professionScroll.viewport = viewport;
            professionScroll.horizontal = false;
            professionScroll.vertical = true;
            professionScroll.movementType = ScrollRect.MovementType.Clamped;
            professionScroll.inertia = true;
            professionScroll.decelerationRate = 0.12f;
            professionScroll.scrollSensitivity = 34f;
            professionScroll.verticalScrollbar = scrollbar;
            professionScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            professionScroll.verticalScrollbarSpacing = 8f;

            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                rows[i] = CreateRow(DisplayOrder[i], contentRoot);
            }

            freeWorkersText = CreateText("FreeWorkers", panelRoot, string.Empty, 13, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            SetBottomStretch(freeWorkersText.rectTransform, 24f, 30f, 260f, 20f);

            actionStatusText = CreateText("ActionStatus", panelRoot, string.Empty, 13, TextAnchor.UpperRight, new Color(0.86f, 0.70f, 0.42f));
            actionStatusText.fontStyle = FontStyle.Bold;
            SetBottomStretch(actionStatusText.rectTransform, 260f, 30f, 24f, 20f);
        }

        private ProfessionRow CreateRow(StrategyProfessionType type, Transform parent)
        {
            RectTransform root = CreateUiObject(type + "Row", parent).GetComponent<RectTransform>();
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;
            layout.minHeight = RowHeight;

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.055f);

            RectTransform iconFrame = CreateUiObject("IconFrame", root).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 10f, 8f, 40f, 40f);
            Image iconFrameImage = iconFrame.gameObject.AddComponent<Image>();
            iconFrameImage.color = new Color(0f, 0f, 0f, 0.28f);
            iconFrameImage.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 5f, 5f, 5f, 5f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = StrategyProfessionIconFactory.GetIcon(type);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text title = CreateText("Title", root, string.Empty, 14, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 14;
            SetTopStretch(title.rectTransform, 62f, 8f, 174f, 18f);

            Text subtitle = CreateText("Subtitle", root, string.Empty, 11, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            subtitle.resizeTextForBestFit = true;
            subtitle.resizeTextMinSize = 9;
            subtitle.resizeTextMaxSize = 11;
            SetTopStretch(subtitle.rectTransform, 62f, 30f, 174f, 16f);

            Text count = CreateText("Count", root, "0/0", 17, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.62f));
            count.fontStyle = FontStyle.Bold;
            SetRightMiddle(count.rectTransform, 132f, 0f, 58f, 34f);

            Button minusButton = CreateSquareButton("Minus", root, 68f, "-");
            Button plusButton = CreateSquareButton("Plus", root, 24f, "+");

            StrategyProfessionType capturedType = type;
            minusButton.onClick.AddListener(() => ChangeProfession(capturedType, false));
            plusButton.onClick.AddListener(() => ChangeProfession(capturedType, true));

            return new ProfessionRow
            {
                Type = type,
                Root = root,
                Background = background,
                Title = title,
                Subtitle = subtitle,
                Count = count,
                MinusButton = minusButton,
                PlusButton = plusButton
            };
        }

        private Button CreateSquareButton(string name, Transform parent, float right, string label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(1f, 0.5f);
            root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-right, 0f);
            root.sizeDelta = new Vector2(36f, 34f);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.11f, 0.16f, 0.17f, 0.96f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button, image.color);
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.Compact);

            Text text = CreateText("Label", root, label, 22, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetOffsets(text.rectTransform, 0f, 0f, 0f, 2f);
            return button;
        }

        private void RefreshUi()
        {
            if (contentRoot == null)
            {
                isDirty = true;
                return;
            }

            isDirty = false;
            population ??= UnityEngine.Object.FindAnyObjectByType<StrategyPopulationController>();
            EnsureProfessionRows();
            int freeWorkers = CountFreeWorkers();
            int visibleRows = 0;

            for (int i = 0; i < rows.Length; i++)
            {
                ProfessionRow row = rows[i];
                if (row == null || row.Root == null)
                {
                    isDirty = true;
                    continue;
                }

                ProfessionSnapshot snapshot = BuildSnapshot(row.Type, freeWorkers);
                bool visible = snapshot.Capacity > 0;
                row.Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                visibleRows++;
                ApplySnapshot(row, snapshot);
            }

            float contentHeight = Mathf.Max(0f, visibleRows * RowHeight + Mathf.Max(0, visibleRows - 1) * 8f);
            contentRoot.sizeDelta = new Vector2(0f, contentHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            professionScroll?.SetLayoutVertical();
            if (professionScroll != null && !isOpen)
            {
                professionScroll.verticalNormalizedPosition = 1f;
            }

            if (freeWorkersText != null)
            {
                freeWorkersText.text = "Free adults: " + freeWorkers;
            }

            if (actionStatusText != null && string.IsNullOrEmpty(actionStatusText.text))
            {
                actionStatusText.text = visibleRows > 0
                    ? "Available: " + visibleRows
                    : "No workplaces";
            }

            RefreshAutoControls(freeWorkers);
        }

        private void ApplySnapshot(ProfessionRow row, ProfessionSnapshot snapshot)
        {
            row.Title.text = snapshot.Title;
            row.Subtitle.text = snapshot.Subtitle;
            row.Count.text = snapshot.IsUnlimited
                ? snapshot.Assigned + "/\u221e"
                : snapshot.Assigned + "/" + snapshot.Capacity;
            row.Count.color = !snapshot.IsUnlimited && snapshot.Assigned >= snapshot.Capacity
                ? new Color(0.95f, 0.72f, 0.32f)
                : new Color(0.95f, 0.88f, 0.62f);
            row.Background.color = snapshot.Assigned > 0
                ? new Color(snapshot.Accent.r, snapshot.Accent.g, snapshot.Accent.b, 0.18f)
                : new Color(1f, 1f, 1f, 0.055f);
            row.MinusButton.interactable = snapshot.Assigned > 0;
            row.PlusButton.interactable = (snapshot.IsUnlimited || snapshot.Assigned < snapshot.Capacity)
                && snapshot.FreeWorkers > 0;
        }
    }
}
