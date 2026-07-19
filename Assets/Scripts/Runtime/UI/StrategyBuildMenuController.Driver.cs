using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private RectTransform buildButtonIconRoot;
        private RectTransform paletteRoot;
        private RectTransform contextRoot;
        private RectTransform dockRoot;
        private RectTransform subcategoryRoot;
        private RectTransform trayRoot;
        private Image buildButtonImage;
        private Text buildButtonText;
        private StrategyHudTooltip buildButtonTooltip;
        private RectTransform placementFeedbackRoot;
        private Image placementFeedbackAccent;
        private Text placementFeedbackTitle;
        private Text placementFeedbackCost;
        private Text placementFeedbackStatus;
        private string placementFeedbackMessage = "Choose a buildable location.";
        private bool placementFeedbackValid;
        private readonly Button[] speedButtons = new Button[3];
        private readonly Image[] speedButtonImages = new Image[3];
        private readonly Text[] speedButtonTexts = new Text[3];
        private StrategyTimeScaleController timeScale;
        private StrategyWorldSelectionController worldSelection;
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
        public bool IsOpen => isOpen;
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

        public void SetPlacementFeedback(bool valid, string message)
        {
            string normalized = string.IsNullOrWhiteSpace(message)
                ? "Choose a buildable location."
                : message;
            if (placementFeedbackValid == valid && placementFeedbackMessage == normalized)
            {
                return;
            }

            placementFeedbackValid = valid;
            placementFeedbackMessage = normalized;
            RefreshPlacementFeedback();
        }

        public void ClearActiveTool()
        {
            if (ActiveTool == StrategyBuildTool.None)
            {
                return;
            }

            bool returnToPalette = !isOpen && selectedCategoryIndex >= 0;
            StrategyDebugLogger.Info("BuildMenu", "ToolCleared", StrategyDebugLogger.F("tool", ActiveTool));
            ActiveTool = StrategyBuildTool.None;
            if (returnToPalette)
            {
                isOpen = true;
                RefreshBuildButtonTooltip();
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Open);
            }

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
            RefreshBuildButtonTooltip();
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
            RefreshInputContext();
            HandleHotkeys();
            HandlePointerDismissal();
            RefreshInputContext();
            UpdateAnimation();

            if (TryRefreshBuildHud())
            {
                RefreshUi();
            }
        }

        internal void ToggleOpen()
        {
            isOpen = !isOpen;
            RefreshBuildButtonTooltip();
            if (!isOpen)
            {
                if (selectedCategoryIndex < 0 && ActiveTool == StrategyBuildTool.None)
                {
                    StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
                }

                CloseAll();
                return;
            }

            worldSelection ??= UnityEngine.Object.FindAnyObjectByType<StrategyWorldSelectionController>();
            worldSelection?.DismissForBuildMode();
            isDirty = true;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Open);
        }

        private void SelectCategory(CategoryUi category, bool allowToggle)
        {
            if (category == null)
            {
                return;
            }

            bool sameCategory = selectedCategoryIndex == category.Index;
            ClearActiveTool();
            bool closingCategory = allowToggle && sameCategory;
            selectedCategoryIndex = closingCategory ? -1 : category.Index;
            selectedSubcategoryIndex = closingCategory || !category.Data.HasSubcategories
                ? -1
                : FindFirstAllowedSubcategory(category);
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

            ClearActiveTool();
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
            if (ActiveTool != StrategyBuildTool.None)
            {
                CollapseForPlacement();
            }

            isDirty = true;
        }

        private void CollapseForPlacement()
        {
            isOpen = false;
            RefreshBuildButtonTooltip();
            StrategyHudTooltipPresenter.Hide(null);
        }

        private int FindFirstAllowedSubcategory(CategoryUi category)
        {
            for (int i = 0; i < category.Subcategories.Length; i++)
            {
                if (SubcategoryHasAllowedTool(category, category.Subcategories[i]))
                {
                    return i;
                }
            }

            return -1;
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
