using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBuildTool
    {
        None,
        House,
        LumberjackCamp,
        StonecutterCamp,
        HunterCamp,
        FisherHut,
        StorageYard,
        Granary,
        Bridge
    }

    public readonly struct StrategyBuildToolInfo
    {
        public StrategyBuildToolInfo(StrategyBuildTool tool, string title, StrategyConstructionResourceCost cost, Color color, Vector2Int footprint)
        {
            Tool = tool;
            Title = title;
            Cost = cost;
            Color = color;
            Footprint = footprint;
        }

        public StrategyBuildTool Tool { get; }
        public string Title { get; }
        public StrategyConstructionResourceCost Cost { get; }
        public Color Color { get; }
        public Vector2Int Footprint { get; }
    }

    internal sealed class StrategyBuildMenuControllerDriver
    {
        private readonly Transform ownerTransform;

        public StrategyBuildMenuControllerDriver(Transform ownerTransform)
        {
            this.ownerTransform = ownerTransform;
        }
        private const float MenuAnimationSpeed = 7f;
        private const float TrayAnimationSpeed = 8f;

        private readonly List<CategoryUi> categoryUis = new();
        private BuildCategoryData[] categories;
        private Canvas canvas;
        private CanvasGroup menuGroup;
        private CanvasGroup trayGroup;
        private CanvasGroup statusGroup;
        private RectTransform buildButtonRoot;
        private RectTransform dockRoot;
        private RectTransform trayRoot;
        private RectTransform statusRoot;
        private Image buildButtonImage;
        private Text buildButtonText;
        private Text treasuryText;
        private readonly Button[] speedButtons = new Button[3];
        private readonly Image[] speedButtonImages = new Image[3];
        private readonly Text[] speedButtonTexts = new Text[3];
        private StrategyTimeScaleController timeScale;
        private Text statusText;
        private Font font;
        private bool initialized;
        private bool isOpen;
        private bool isDirty = true;
        private int selectedCategoryIndex = -1;
        private float menuT;
        private float trayT;
        private float statusT;

        public StrategyBuildTool ActiveTool { get; private set; }
        public int LastPlacementFrame { get; private set; } = -1;
        public StrategyConstructionResourceCost AvailableConstructionResources => StrategyStorageYard.GetTotalConstructionResources();

        public bool TryGetActiveToolInfo(out StrategyBuildToolInfo info)
        {
            if (ActiveTool == StrategyBuildTool.None)
            {
                info = default;
                return false;
            }

            foreach (BuildCategoryData category in categories)
            {
                foreach (BuildItemData item in category.Items)
                {
                    if (item.Tool != ActiveTool)
                    {
                        continue;
                    }

                    info = new StrategyBuildToolInfo(item.Tool, item.Title, item.Cost, item.Color, GetFootprint(item.Tool));
                    return true;
                }
            }

            info = default;
            return false;
        }

        public bool CanAffordActiveTool()
        {
            return TryGetActiveToolInfo(out StrategyBuildToolInfo info)
                && info.Cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources());
        }

        public bool TrySpendForActiveTool()
        {
            return CanAffordActiveTool();
        }

        public void ClearActiveTool()
        {
            if (ActiveTool == StrategyBuildTool.None)
            {
                return;
            }

            StrategyDebugLogger.Info("BuildMenu", "ToolCleared", StrategyDebugLogger.F("tool", ActiveTool));
            ActiveTool = StrategyBuildTool.None;
            isDirty = true;
        }

        public void CloseAll()
        {
            if (isOpen || selectedCategoryIndex >= 0 || ActiveTool != StrategyBuildTool.None)
            {
                StrategyDebugLogger.Info(
                    "BuildMenu",
                    "Closed",
                    StrategyDebugLogger.F("activeTool", ActiveTool),
                    StrategyDebugLogger.F("selectedCategory", selectedCategoryIndex));
            }

            isOpen = false;
            selectedCategoryIndex = -1;
            ActiveTool = StrategyBuildTool.None;
            isDirty = true;
        }

        public void CloseAfterPlacement()
        {
            LastPlacementFrame = Time.frameCount;
            CloseAll();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            categories = StrategyBuildMenuCatalog.CreateCatalog();
            EnsureEventSystem();
            BuildUi();
            RefreshUi();
        }

        public void Tick()
        {
            Initialize();
            HandleHotkeys();
            HandlePointerDismissal();
            UpdateAnimation();

            if (isDirty || Time.frameCount % 12 == 0)
            {
                RefreshUi();
            }
        }

        private void ToggleOpen()
        {
            isOpen = !isOpen;
            if (!isOpen)
            {
                CloseAll();
                return;
            }

            isDirty = true;
        }

        private void HandlePointerDismissal()
        {
            Mouse mouse = Mouse.current;
            if (!isOpen
                || mouse == null
                || !mouse.leftButton.wasPressedThisFrame
                || IsPointerOverBuildUi()
                || ActiveTool != StrategyBuildTool.None)
            {
                return;
            }

            CloseAll();
        }

        private void SelectCategory(CategoryUi category, bool allowToggle)
        {
            if (category == null)
            {
                return;
            }

            bool sameCategory = selectedCategoryIndex == category.Index;
            if (category.Items is { Length: 1 })
            {
                BuildItemData item = category.Items[0].Data;
                selectedCategoryIndex = category.Index;
                if (!sameCategory)
                {
                    trayT = 0f;
                }

                if (!item.Cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources()))
                {
                    ActiveTool = StrategyBuildTool.None;
                    isDirty = true;
                    StrategyDebugLogger.Warn(
                        "BuildMenu",
                        "ToolSelectionRejected",
                        StrategyDebugLogger.F("tool", item.Tool),
                        StrategyDebugLogger.F("reason", "not_affordable"),
                        StrategyDebugLogger.F("cost", item.Cost),
                        StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                    return;
                }

                ActiveTool = allowToggle && ActiveTool == item.Tool ? StrategyBuildTool.None : item.Tool;
                StrategyDebugLogger.Info(
                    "BuildMenu",
                    "ToolSelected",
                    StrategyDebugLogger.F("tool", ActiveTool),
                    StrategyDebugLogger.F("category", category.Data != null ? category.Data.Label : string.Empty),
                    StrategyDebugLogger.F("cost", item.Cost),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));

                isDirty = true;
                return;
            }

            selectedCategoryIndex = allowToggle && sameCategory ? -1 : category.Index;
            if (!sameCategory)
            {
                trayT = 0f;
            }

            isDirty = true;
        }

        private void ToggleTool(BuildItemData item)
        {
            if (item == null)
            {
                return;
            }

            if (!item.Cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources()))
            {
                ActiveTool = StrategyBuildTool.None;
                StrategyDebugLogger.Warn(
                    "BuildMenu",
                    "ToolSelectionRejected",
                    StrategyDebugLogger.F("tool", item.Tool),
                    StrategyDebugLogger.F("reason", "not_affordable"),
                    StrategyDebugLogger.F("cost", item.Cost),
                    StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
                isDirty = true;
                return;
            }

            ActiveTool = ActiveTool == item.Tool ? StrategyBuildTool.None : item.Tool;
            StrategyDebugLogger.Info(
                "BuildMenu",
                "ToolSelected",
                StrategyDebugLogger.F("tool", ActiveTool),
                StrategyDebugLogger.F("cost", item.Cost),
                StrategyDebugLogger.F("available", StrategyStorageYard.GetTotalConstructionResources()));
            isDirty = true;
        }

        private void HandleHotkeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasPressed(keyboard.bKey))
            {
                ToggleOpen();
                return;
            }

            if (!isOpen)
            {
                return;
            }

            if (WasPressed(keyboard.escapeKey))
            {
                CancelOneLayer();
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame && IsPointerOverBuildUi())
            {
                CancelOneLayer();
                return;
            }

            int number = GetPressedNumber(keyboard);
            if (number <= 0)
            {
                return;
            }

            if (selectedCategoryIndex >= 0 && TryActivateItemHotkey(number))
            {
                return;
            }

            TrySelectCategoryHotkey(number);
        }

        private void CancelOneLayer()
        {
            if (ActiveTool != StrategyBuildTool.None)
            {
                StrategyDebugLogger.Info("BuildMenu", "ToolCleared", StrategyDebugLogger.F("tool", ActiveTool));
                ActiveTool = StrategyBuildTool.None;
                isDirty = true;
                return;
            }

            if (selectedCategoryIndex >= 0)
            {
                selectedCategoryIndex = -1;
                isDirty = true;
                return;
            }

            isOpen = false;
            isDirty = true;
        }

        private bool TrySelectCategoryHotkey(int number)
        {
            int index = number - 1;
            if (index < 0 || index >= categoryUis.Count)
            {
                return false;
            }

            SelectCategory(categoryUis[index], true);
            return true;
        }

        private bool TryActivateItemHotkey(int number)
        {
            if (selectedCategoryIndex < 0 || selectedCategoryIndex >= categoryUis.Count)
            {
                return false;
            }

            BuildItemUi[] items = categoryUis[selectedCategoryIndex].Items;
            int index = number - 1;
            if (items == null || index < 0 || index >= items.Length)
            {
                return false;
            }

            ToggleTool(items[index].Data);
            return true;
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("StrategyBuildMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(ownerTransform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateTreasuryHud(canvasObject.transform);
            CreateSpeedControlsHud(canvasObject.transform);
            CreateBuildButton(canvasObject.transform);
            CreateMenuLayer(canvasObject.transform);
        }

        private void CreateTreasuryHud(Transform parent)
        {
            RectTransform panel = CreateUiObject("TreasuryPanel", parent).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(18f, -18f);
            panel.sizeDelta = new Vector2(178f, 42f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.11f, 0.13f, 0.92f);

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.38f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            treasuryText = CreateText("TreasuryText", panel, "Logs 0  Stone 0", 16, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.62f));
            treasuryText.fontStyle = FontStyle.Bold;
            Stretch(treasuryText.rectTransform, 8f, 0f, 8f, 0f);
        }

        private void CreateSpeedControlsHud(Transform parent)
        {
            RectTransform panel = CreateUiObject("SpeedControlsPanel", parent).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(18f, -66f);
            panel.sizeDelta = new Vector2(178f, 34f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.09f, 0.10f, 0.88f);

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.30f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);

            for (int i = 0; i < speedButtons.Length; i++)
            {
                int index = i;
                RectTransform buttonRoot = CreateUiObject("SpeedX" + (i + 1), panel).GetComponent<RectTransform>();
                buttonRoot.anchorMin = new Vector2(0f, 0.5f);
                buttonRoot.anchorMax = new Vector2(0f, 0.5f);
                buttonRoot.pivot = new Vector2(0f, 0.5f);
                buttonRoot.anchoredPosition = new Vector2(6f + i * 56f, 0f);
                buttonRoot.sizeDelta = new Vector2(52f, 24f);

                Image image = buttonRoot.gameObject.AddComponent<Image>();
                image.color = new Color(0.11f, 0.16f, 0.17f, 0.96f);

                Button button = buttonRoot.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SetSpeedFromHud(index + 1));
                ConfigureButtonColors(button);

                Text label = CreateText("Label", buttonRoot, "x" + (i + 1), 13, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
                Stretch(label.rectTransform, 0f, 0f, 0f, 1f);

                speedButtons[i] = button;
                speedButtonImages[i] = image;
                speedButtonTexts[i] = label;
            }
        }

        private void CreateBuildButton(Transform parent)
        {
            buildButtonRoot = CreateUiObject("BuildButton", parent).GetComponent<RectTransform>();
            buildButtonRoot.anchorMin = new Vector2(0.5f, 0f);
            buildButtonRoot.anchorMax = new Vector2(0.5f, 0f);
            buildButtonRoot.pivot = new Vector2(0.5f, 0f);
            buildButtonRoot.anchoredPosition = new Vector2(0f, 22f);
            buildButtonRoot.sizeDelta = new Vector2(134f, 54f);

            buildButtonImage = buildButtonRoot.gameObject.AddComponent<Image>();
            buildButtonImage.color = new Color(0.13f, 0.20f, 0.24f, 0.96f);

            Outline outline = buildButtonRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.42f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);

            RectTransform icon = CreateUiObject("BuildIcon", buildButtonRoot).GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(14f, 0f);
            icon.sizeDelta = new Vector2(32f, 32f);
            StrategyBuildMenuCatalog.DrawBuildIcon(icon, new Color(0.95f, 0.72f, 0.28f), new Color(0.35f, 0.42f, 0.46f));

            buildButtonText = CreateText("BuildButtonText", buildButtonRoot, "Build", 17, TextAnchor.MiddleLeft, Color.white);
            buildButtonText.fontStyle = FontStyle.Bold;
            buildButtonText.rectTransform.anchorMin = new Vector2(0f, 0f);
            buildButtonText.rectTransform.anchorMax = new Vector2(1f, 1f);
            buildButtonText.rectTransform.offsetMin = new Vector2(54f, 0f);
            buildButtonText.rectTransform.offsetMax = new Vector2(-12f, 0f);

            Button button = buildButtonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = buildButtonImage;
            button.onClick.AddListener(ToggleOpen);
            ConfigureButtonColors(button);
        }

        private void CreateMenuLayer(Transform parent)
        {
            RectTransform menuRoot = CreateUiObject("BuildMenuLayer", parent).GetComponent<RectTransform>();
            Stretch(menuRoot, 0f, 0f, 0f, 0f);
            menuGroup = menuRoot.gameObject.AddComponent<CanvasGroup>();
            menuGroup.alpha = 0f;
            menuGroup.blocksRaycasts = false;
            menuGroup.interactable = false;

            trayRoot = CreateUiObject("BuildItemTray", menuRoot).GetComponent<RectTransform>();
            trayRoot.anchorMin = new Vector2(0.5f, 0f);
            trayRoot.anchorMax = new Vector2(0.5f, 0f);
            trayRoot.pivot = new Vector2(0.5f, 0f);
            trayRoot.anchoredPosition = new Vector2(0f, 122f);
            trayRoot.sizeDelta = new Vector2(1080f, 138f);
            trayGroup = trayRoot.gameObject.AddComponent<CanvasGroup>();
            trayGroup.alpha = 0f;
            trayGroup.blocksRaycasts = false;
            trayGroup.interactable = false;
            HorizontalLayoutGroup trayLayout = trayRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            trayLayout.padding = new RectOffset(12, 12, 8, 8);
            trayLayout.spacing = 10f;
            trayLayout.childAlignment = TextAnchor.MiddleCenter;
            trayLayout.childControlWidth = false;
            trayLayout.childControlHeight = false;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = false;

            dockRoot = CreateUiObject("BuildCategoryDock", menuRoot).GetComponent<RectTransform>();
            dockRoot.anchorMin = new Vector2(0.5f, 0f);
            dockRoot.anchorMax = new Vector2(0.5f, 0f);
            dockRoot.pivot = new Vector2(0.5f, 0f);
            dockRoot.anchoredPosition = new Vector2(0f, 82f);
            dockRoot.sizeDelta = new Vector2(920f, 78f);
            HorizontalLayoutGroup dockLayout = dockRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            dockLayout.padding = new RectOffset(12, 12, 8, 8);
            dockLayout.spacing = 10f;
            dockLayout.childAlignment = TextAnchor.MiddleCenter;
            dockLayout.childControlWidth = false;
            dockLayout.childControlHeight = false;
            dockLayout.childForceExpandWidth = false;
            dockLayout.childForceExpandHeight = false;

            statusRoot = CreateUiObject("BuildStatusPanel", menuRoot).GetComponent<RectTransform>();
            statusRoot.anchorMin = new Vector2(0.5f, 0f);
            statusRoot.anchorMax = new Vector2(0.5f, 0f);
            statusRoot.pivot = new Vector2(0.5f, 0f);
            statusRoot.anchoredPosition = new Vector2(0f, 306f);
            statusRoot.sizeDelta = new Vector2(520f, 42f);
            statusGroup = statusRoot.gameObject.AddComponent<CanvasGroup>();
            statusGroup.alpha = 0f;
            statusGroup.blocksRaycasts = false;
            statusGroup.interactable = false;
            Image statusBackground = statusRoot.gameObject.AddComponent<Image>();
            statusBackground.color = new Color(0.08f, 0.10f, 0.12f, 0.94f);
            Outline statusOutline = statusRoot.gameObject.AddComponent<Outline>();
            statusOutline.effectColor = new Color(0f, 0f, 0f, 0.34f);
            statusOutline.effectDistance = new Vector2(1.2f, -1.2f);
            statusText = CreateText("BuildStatusText", statusRoot, string.Empty, 15, TextAnchor.MiddleCenter, new Color(0.88f, 0.92f, 0.95f));
            statusText.fontStyle = FontStyle.Bold;
            Stretch(statusText.rectTransform, 14f, 0f, 14f, 0f);

            for (int i = 0; i < categories.Length; i++)
            {
                categoryUis.Add(CreateCategoryUi(categories[i], i));
            }
        }

        private CategoryUi CreateCategoryUi(BuildCategoryData data, int index)
        {
            RectTransform root = CreateUiObject("BuildCategory_" + data.Label, dockRoot).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(154f, 62f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 154f;
            layout.preferredHeight = 62f;

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.09f, 0.15f, 0.18f, 0.96f);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.36f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            RectTransform accent = CreateUiObject("Accent", root).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.offsetMin = new Vector2(0f, 0f);
            accent.offsetMax = new Vector2(8f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = data.AccentColor;
            accentImage.raycastTarget = false;

            Text label = CreateText("Label", root, data.Label, 12, TextAnchor.MiddleLeft, new Color(0.86f, 0.91f, 0.96f));
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(18f, 6f);
            label.rectTransform.offsetMax = new Vector2(-10f, -6f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ConfigureButtonColors(button);

            CategoryUi category = new CategoryUi
            {
                Data = data,
                Index = index,
                Root = root,
                Background = background,
                Label = label,
                Button = button
            };

            button.onClick.AddListener(() => SelectCategory(category, true));
            AddHoverRelay(root.gameObject, hovered => category.IsHovered = hovered);

            category.Items = new BuildItemUi[data.Items.Length];
            for (int i = 0; i < data.Items.Length; i++)
            {
                category.Items[i] = CreateItemUi(data.Items[i]);
            }

            return category;
        }

        private BuildItemUi CreateItemUi(BuildItemData data)
        {
            RectTransform root = CreateUiObject("BuildItem_" + data.Tool, trayRoot).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(138f, 116f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 138f;
            layout.preferredHeight = 116f;

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.13f, 0.18f, 0.24f, 0.98f);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.38f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform icon = CreateUiObject("Icon", root).GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(0.5f, 1f);
            icon.anchorMax = new Vector2(0.5f, 1f);
            icon.pivot = new Vector2(0.5f, 1f);
            icon.anchoredPosition = new Vector2(0f, -6f);
            icon.sizeDelta = new Vector2(96f, 64f);
            Image iconBackground = icon.gameObject.AddComponent<Image>();
            iconBackground.color = data.Color;
            iconBackground.raycastTarget = false;
            StrategyBuildMenuCatalog.DrawItemIcon(icon, data);

            Text title = CreateText("Title", root, data.Title, 11, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(1f, 0f);
            title.rectTransform.pivot = new Vector2(0.5f, 0f);
            title.rectTransform.offsetMin = new Vector2(7f, 7f);
            title.rectTransform.offsetMax = new Vector2(-7f, 33f);

            RectTransform badge = CreateUiObject("CostBadge", root).GetComponent<RectTransform>();
            badge.anchorMin = new Vector2(1f, 1f);
            badge.anchorMax = new Vector2(1f, 1f);
            badge.pivot = new Vector2(1f, 1f);
            badge.anchoredPosition = new Vector2(-6f, -6f);
            badge.sizeDelta = new Vector2(70f, 22f);
            Image badgeBackground = badge.gameObject.AddComponent<Image>();
            badgeBackground.color = new Color(0.18f, 0.13f, 0.06f, 0.98f);
            badgeBackground.raycastTarget = false;

            Text badgeText = CreateText("CostText", badge, "L0 S0", 11, TextAnchor.MiddleCenter, Color.white);
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.resizeTextForBestFit = true;
            badgeText.resizeTextMinSize = 8;
            badgeText.resizeTextMaxSize = 11;
            Stretch(badgeText.rectTransform, 4f, 0f, 4f, 0f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ConfigureButtonColors(button);

            BuildItemUi item = new BuildItemUi
            {
                Data = data,
                Root = root,
                Background = background,
                IconBackground = iconBackground,
                Title = title,
                BadgeBackground = badgeBackground,
                BadgeText = badgeText,
                Button = button
            };

            button.onClick.AddListener(() => ToggleTool(data));
            AddHoverRelay(root.gameObject, hovered => item.IsHovered = hovered);
            return item;
        }

        private void RefreshUi()
        {
            if (treasuryText != null)
            {
                StrategyConstructionResourceCost available = StrategyStorageYard.GetTotalConstructionResources();
                treasuryText.text = "Logs " + available.Logs + "  Stone " + available.Stone;
            }

            RefreshSpeedControls();

            if (buildButtonImage != null)
            {
                buildButtonImage.color = isOpen
                    ? new Color(0.23f, 0.36f, 0.38f, 0.98f)
                    : new Color(0.13f, 0.20f, 0.24f, 0.96f);
            }

            if (buildButtonText != null)
            {
                buildButtonText.text = "Build";
            }

            for (int i = 0; i < categoryUis.Count; i++)
            {
                CategoryUi category = categoryUis[i];
                bool selected = selectedCategoryIndex == i;
                category.Background.color = selected
                    ? new Color(0.17f, 0.32f, 0.36f, 0.99f)
                    : new Color(0.09f, 0.15f, 0.18f, 0.96f);
                category.Label.color = selected ? Color.white : new Color(0.86f, 0.91f, 0.96f);

                for (int j = 0; j < category.Items.Length; j++)
                {
                    BuildItemUi item = category.Items[j];
                    bool visible = selected;
                    item.Root.gameObject.SetActive(visible);
                    if (!visible)
                    {
                        continue;
                    }

                    bool active = ActiveTool == item.Data.Tool;
                    bool affordable = item.Data.Cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources());
                    item.Button.interactable = affordable;
                    item.Background.color = !affordable
                        ? new Color(0.10f, 0.11f, 0.13f, 0.88f)
                        : active
                            ? new Color(0.24f, 0.30f, 0.38f, 1f)
                            : new Color(0.13f, 0.18f, 0.24f, 0.98f);
                    item.IconBackground.color = affordable ? item.Data.Color : new Color(0.24f, 0.25f, 0.27f, 0.92f);
                    item.Title.color = affordable ? Color.white : new Color(0.62f, 0.66f, 0.70f, 1f);
                    item.BadgeBackground.color = active
                        ? new Color(0.60f, 0.36f, 0.10f, 0.96f)
                        : affordable
                            ? new Color(0.18f, 0.13f, 0.06f, 0.98f)
                            : new Color(0.24f, 0.06f, 0.05f, 0.98f);
                    item.BadgeText.text = active ? "Active" : item.Data.Cost.ToBadgeText();
                }
            }

            statusText.text = ActiveTool == StrategyBuildTool.None
                ? string.Empty
                : GetActiveToolTitle() + " selected";

            LayoutRebuilder.ForceRebuildLayoutImmediate(dockRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(trayRoot);
            isDirty = false;
        }

        private void SetSpeedFromHud(int speed)
        {
            if (timeScale == null)
            {
                timeScale = UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
            }

            timeScale?.SetRequestedScale(speed);
            RefreshSpeedControls();
            StrategyDebugLogger.Info(
                "BuildMenu",
                "SpeedButtonClicked",
                StrategyDebugLogger.F("speed", speed),
                StrategyDebugLogger.F("timeScaleFound", timeScale != null));
        }

        private void RefreshSpeedControls()
        {
            if (timeScale == null)
            {
                timeScale = UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
            }

            float currentScale = timeScale != null ? timeScale.CurrentScale : 1f;
            for (int i = 0; i < speedButtons.Length; i++)
            {
                bool active = Mathf.RoundToInt(currentScale) == i + 1;
                if (speedButtonImages[i] != null)
                {
                    speedButtonImages[i].color = active
                        ? new Color(0.25f, 0.36f, 0.36f, 0.98f)
                        : new Color(0.11f, 0.16f, 0.17f, 0.96f);
                }

                if (speedButtonTexts[i] != null)
                {
                    speedButtonTexts[i].color = active
                        ? new Color(0.95f, 0.88f, 0.62f)
                        : Color.white;
                }
            }
        }

        private void UpdateAnimation()
        {
            menuT = Mathf.MoveTowards(menuT, isOpen ? 1f : 0f, Time.unscaledDeltaTime * MenuAnimationSpeed);
            float easedMenu = Smooth01(menuT);

            if (menuGroup != null)
            {
                menuGroup.alpha = easedMenu;
                menuGroup.blocksRaycasts = isOpen && easedMenu > 0.15f;
                menuGroup.interactable = isOpen && easedMenu > 0.15f;
            }

            if (dockRoot != null)
            {
                dockRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(-92f, 84f, easedMenu));
                dockRoot.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, easedMenu);
            }

            bool trayOpen = isOpen && selectedCategoryIndex >= 0;
            trayT = Mathf.MoveTowards(trayT, trayOpen ? 1f : 0f, Time.unscaledDeltaTime * TrayAnimationSpeed);
            float easedTray = Smooth01(trayT);

            if (trayGroup != null)
            {
                trayGroup.alpha = easedTray;
                trayGroup.blocksRaycasts = trayOpen && easedTray > 0.45f;
                trayGroup.interactable = trayOpen && easedTray > 0.45f;
            }

            if (trayRoot != null)
            {
                trayRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(78f, 152f, easedTray) + Mathf.Lerp(-112f, 0f, easedMenu));
                trayRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, easedTray);
            }

            bool statusVisible = isOpen && ActiveTool != StrategyBuildTool.None;
            statusT = Mathf.MoveTowards(statusT, statusVisible ? 1f : 0f, Time.unscaledDeltaTime * 9f);
            if (statusGroup != null)
            {
                statusGroup.alpha = Smooth01(statusT);
            }

            UpdateHoverAnimation();
        }

        private void UpdateHoverAnimation()
        {
            for (int i = 0; i < categoryUis.Count; i++)
            {
                CategoryUi category = categoryUis[i];
                bool selected = selectedCategoryIndex == i;
                float target = category.IsHovered || selected ? 1f : 0f;
                category.HoverT = Mathf.MoveTowards(category.HoverT, target, Time.unscaledDeltaTime * 8f);
                category.Root.localScale = Vector3.one * Mathf.Lerp(1f, selected ? 1.08f : 1.04f, Smooth01(category.HoverT));

                for (int j = 0; j < category.Items.Length; j++)
                {
                    BuildItemUi item = category.Items[j];
                    if (!item.Root.gameObject.activeSelf)
                    {
                        continue;
                    }

                    bool active = ActiveTool == item.Data.Tool;
                    float itemTarget = item.IsHovered || active ? 1f : 0f;
                    item.HoverT = Mathf.MoveTowards(item.HoverT, itemTarget, Time.unscaledDeltaTime * 9f);
                    item.Root.localScale = Vector3.one * Mathf.Lerp(1f, active ? 1.08f : 1.04f, Smooth01(item.HoverT));
                }
            }
        }

        private string GetActiveToolTitle()
        {
            foreach (BuildCategoryData category in categories)
            {
                foreach (BuildItemData item in category.Items)
                {
                    if (item.Tool == ActiveTool)
                    {
                        return item.Title;
                    }
                }
            }

            return ActiveTool.ToString();
        }

        private static Vector2Int GetFootprint(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => new Vector2Int(2, 2),
                StrategyBuildTool.LumberjackCamp => new Vector2Int(2, 2),
                StrategyBuildTool.StonecutterCamp => new Vector2Int(2, 2),
                StrategyBuildTool.HunterCamp => new Vector2Int(2, 2),
                StrategyBuildTool.FisherHut => new Vector2Int(2, 2),
                StrategyBuildTool.StorageYard => new Vector2Int(3, 2),
                StrategyBuildTool.Granary => new Vector2Int(3, 2),
                StrategyBuildTool.Bridge => Vector2Int.one,
                _ => Vector2Int.one
            };
        }

        private bool IsPointerOverBuildUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                UnityEngine.Object.Destroy(standalone);
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
        }

        private static void ConfigureButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
        }

        internal static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        internal static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AddHoverRelay(GameObject target, Action<bool> onHoverChanged)
        {
            HoverRelay relay = target.AddComponent<HoverRelay>();
            relay.OnHoverChanged = onHoverChanged;
        }

        private static bool WasPressed(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            return key != null && key.wasPressedThisFrame;
        }

        private static int GetPressedNumber(Keyboard keyboard)
        {
            if (WasPressed(keyboard.digit1Key) || WasPressed(keyboard.numpad1Key)) return 1;
            if (WasPressed(keyboard.digit2Key) || WasPressed(keyboard.numpad2Key)) return 2;
            if (WasPressed(keyboard.digit3Key) || WasPressed(keyboard.numpad3Key)) return 3;
            if (WasPressed(keyboard.digit4Key) || WasPressed(keyboard.numpad4Key)) return 4;
            if (WasPressed(keyboard.digit5Key) || WasPressed(keyboard.numpad5Key)) return 5;
            if (WasPressed(keyboard.digit6Key) || WasPressed(keyboard.numpad6Key)) return 6;
            if (WasPressed(keyboard.digit7Key) || WasPressed(keyboard.numpad7Key)) return 7;
            if (WasPressed(keyboard.digit8Key) || WasPressed(keyboard.numpad8Key)) return 8;
            if (WasPressed(keyboard.digit9Key) || WasPressed(keyboard.numpad9Key)) return 9;
            return 0;
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        internal sealed class BuildCategoryData
        {
            public BuildCategoryData(string label, Color accentColor, BuildItemData[] items)
            {
                Label = label;
                AccentColor = accentColor;
                Items = items;
            }

            public string Label { get; }
            public Color AccentColor { get; }
            public BuildItemData[] Items { get; }
        }

        internal sealed class BuildItemData
        {
            public BuildItemData(StrategyBuildTool tool, string abbrev, string title, StrategyConstructionResourceCost cost, Color color)
            {
                Tool = tool;
                Abbrev = abbrev;
                Title = title;
                Cost = cost;
                Color = color;
            }

            public StrategyBuildTool Tool { get; }
            public string Abbrev { get; }
            public string Title { get; }
            public StrategyConstructionResourceCost Cost { get; }
            public Color Color { get; }
        }

        private sealed class CategoryUi
        {
            public BuildCategoryData Data;
            public int Index;
            public RectTransform Root;
            public Image Background;
            public Text Label;
            public Button Button;
            public BuildItemUi[] Items;
            public bool IsHovered;
            public float HoverT;
        }

        private sealed class BuildItemUi
        {
            public BuildItemData Data;
            public RectTransform Root;
            public Image Background;
            public Image IconBackground;
            public Text Title;
            public Image BadgeBackground;
            public Text BadgeText;
            public Button Button;
            public bool IsHovered;
            public float HoverT;
        }

        private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Action<bool> OnHoverChanged;

            public void OnPointerEnter(PointerEventData eventData)
            {
                OnHoverChanged?.Invoke(true);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                OnHoverChanged?.Invoke(false);
            }
        }
    }
}
