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
        private CanvasGroup subcategoryGroup;
        private CanvasGroup trayGroup;
        private RectTransform buildButtonRoot;
        private RectTransform dockRoot;
        private RectTransform subcategoryRoot;
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
        private int selectedSubcategoryIndex = -1;
        private float menuT;
        private float subcategoryT;
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
            bool hadOpenState = isOpen || selectedCategoryIndex >= 0 || ActiveTool != StrategyBuildTool.None;
            if (hadOpenState)
            {
                StrategyDebugLogger.Info(
                    "BuildMenu",
                    "Closed",
                    StrategyDebugLogger.F("activeTool", ActiveTool),
                    StrategyDebugLogger.F("selectedCategory", selectedCategoryIndex));
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
            }

            isOpen = false;
            selectedCategoryIndex = -1;
            selectedSubcategoryIndex = -1;
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
            font = StrategyUiThemeProvider.Font;
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
                if (selectedCategoryIndex < 0 && ActiveTool == StrategyBuildTool.None)
                {
                    StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
                }

                CloseAll();
                return;
            }

            isDirty = true;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Open);
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
            if (!category.Data.HasSubcategories && category.Items is { Length: 1 })
            {
                BuildItemData item = category.Items[0].Data;
                selectedCategoryIndex = category.Index;
                selectedSubcategoryIndex = -1;
                if (!sameCategory)
                {
                    subcategoryT = 0f;
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
                    StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
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
                StrategyHudSfxAudio.Play(ActiveTool == StrategyBuildTool.None ? StrategyHudSfxKind.Cancel : StrategyHudSfxKind.Select);
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

            bool closingCategory = allowToggle && sameCategory;
            selectedCategoryIndex = closingCategory ? -1 : category.Index;
            selectedSubcategoryIndex = -1;
            StrategyHudSfxAudio.Play(closingCategory ? StrategyHudSfxKind.Cancel : StrategyHudSfxKind.Select);
            if (!sameCategory || closingCategory)
            {
                subcategoryT = 0f;
                trayT = 0f;
            }

            isDirty = true;
        }

        private void SelectSubcategory(CategoryUi category, BuildSubcategoryUi subcategory, bool allowToggle)
        {
            if (category == null || subcategory == null)
            {
                return;
            }

            if (!SubcategoryHasAllowedTool(category, subcategory))
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
                return;
            }

            bool sameSubcategory = selectedCategoryIndex == category.Index
                && selectedSubcategoryIndex == subcategory.Index;
            selectedCategoryIndex = category.Index;
            selectedSubcategoryIndex = allowToggle && sameSubcategory ? -1 : subcategory.Index;
            trayT = 0f;
            StrategyHudSfxAudio.Play(selectedSubcategoryIndex < 0 ? StrategyHudSfxKind.Cancel : StrategyHudSfxKind.Step);
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
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
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
            StrategyHudSfxAudio.Play(ActiveTool == StrategyBuildTool.None ? StrategyHudSfxKind.Cancel : StrategyHudSfxKind.Select);
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

            if (selectedCategoryIndex >= 0 && TrySelectSubcategoryHotkey(number))
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
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Cancel);
                isDirty = true;
                return;
            }

            if (selectedCategoryIndex >= 0)
            {
                if (selectedSubcategoryIndex >= 0)
                {
                    selectedSubcategoryIndex = -1;
                    trayT = 0f;
                    StrategyHudSfxAudio.Play(StrategyHudSfxKind.Cancel);
                }
                else
                {
                    selectedCategoryIndex = -1;
                    subcategoryT = 0f;
                    StrategyHudSfxAudio.Play(StrategyHudSfxKind.Cancel);
                }

                isDirty = true;
                return;
            }

            isOpen = false;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
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

        private bool TrySelectSubcategoryHotkey(int number)
        {
            if (selectedCategoryIndex < 0 || selectedCategoryIndex >= categoryUis.Count)
            {
                return false;
            }

            CategoryUi category = categoryUis[selectedCategoryIndex];
            if (category == null || category.Subcategories == null || category.Subcategories.Length <= 0)
            {
                return false;
            }

            int index = number - 1;
            if (index < 0 || index >= category.Subcategories.Length)
            {
                return false;
            }

            if (!SubcategoryHasAllowedTool(category, category.Subcategories[index]))
            {
                return false;
            }

            SelectSubcategory(category, category.Subcategories[index], true);
            return true;
        }

        private bool TryActivateItemHotkey(int number)
        {
            if (selectedCategoryIndex < 0 || selectedCategoryIndex >= categoryUis.Count)
            {
                return false;
            }

            CategoryUi category = categoryUis[selectedCategoryIndex];
            BuildItemUi[] items = category.Items;
            if (items == null)
            {
                return false;
            }

            int visibleIndex = 0;
            for (int i = 0; i < items.Length; i++)
            {
                BuildItemUi item = items[i];
                if (item == null || !IsItemInSelectedLayer(category, item))
                {
                    continue;
                }

                visibleIndex++;
                if (visibleIndex != number)
                {
                    continue;
                }

                ToggleTool(item.Data);
                return true;
            }

            return false;
        }

    }
}
