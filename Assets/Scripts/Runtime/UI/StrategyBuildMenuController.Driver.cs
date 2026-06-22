using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
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
        private RectTransform buildButtonRoot;
        private RectTransform dockRoot;
        private RectTransform trayRoot;
        private Image buildButtonImage;
        private Text buildButtonText;
        private Text treasuryText;
        private readonly Button[] speedButtons = new Button[3];
        private readonly Image[] speedButtonImages = new Image[3];
        private readonly Text[] speedButtonTexts = new Text[3];
        private StrategyTimeScaleController timeScale;
        private Font font;
        private bool initialized;
        private bool isOpen;
        private bool isDirty = true;
        private int selectedCategoryIndex = -1;
        private float menuT;
        private float trayT;

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

            if (!IsToolAllowed(ActiveTool))
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
                && CanAffordBuildCost(info.Cost);
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

            if (TryRefreshBuildHud())
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

                if (!IsToolAllowed(item.Tool))
                {
                    RejectLockedTool(item.Tool);
                    return;
                }

                if (!CanAffordBuildCost(item.Cost))
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

            if (!IsToolAllowed(item.Tool))
            {
                RejectLockedTool(item.Tool);
                return;
            }

            if (!CanAffordBuildCost(item.Cost))
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

            if (!CategoryHasAllowedTool(categoryUis[index]))
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
    }
}
