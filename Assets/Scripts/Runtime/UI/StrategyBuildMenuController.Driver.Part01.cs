using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private void CreateBuildButton(Transform parent)
        {
            buildButtonRoot = CreateUiObject("BuildButton", parent).GetComponent<RectTransform>();
            buildButtonRoot.anchorMin = new Vector2(0.5f, 0f);
            buildButtonRoot.anchorMax = new Vector2(0.5f, 0f);
            buildButtonRoot.pivot = new Vector2(0.5f, 0f);
            buildButtonRoot.anchoredPosition = new Vector2(0f, 12f);
            buildButtonRoot.sizeDelta = new Vector2(132f, 40f);

            buildButtonImage = buildButtonRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(buildButtonImage, StrategyHudStyle.Elevated, true);
            StrategyHudStyle.AddShadow(buildButtonRoot.gameObject, 0.48f);

            buildButtonIconRoot = CreateUiObject("BuildIcon", buildButtonRoot).GetComponent<RectTransform>();
            buildButtonIconRoot.anchorMin = new Vector2(0f, 0.5f);
            buildButtonIconRoot.anchorMax = new Vector2(0f, 0.5f);
            buildButtonIconRoot.pivot = new Vector2(0f, 0.5f);
            buildButtonIconRoot.anchoredPosition = new Vector2(10f, 0f);
            buildButtonIconRoot.sizeDelta = new Vector2(24f, 24f);
            StrategyBuildMenuCatalog.DrawBuildIcon(buildButtonIconRoot, new Color(0.95f, 0.72f, 0.28f), new Color(0.35f, 0.42f, 0.46f));

            buildButtonText = CreateText("BuildButtonText", buildButtonRoot, "BUILD  [B]", 13, TextAnchor.MiddleLeft, StrategyHudStyle.TextPrimary);
            buildButtonText.fontStyle = FontStyle.Bold;
            buildButtonText.resizeTextForBestFit = true;
            buildButtonText.resizeTextMinSize = 11;
            buildButtonText.resizeTextMaxSize = 13;
            buildButtonText.rectTransform.anchorMin = new Vector2(0f, 0f);
            buildButtonText.rectTransform.anchorMax = new Vector2(1f, 1f);
            buildButtonText.rectTransform.offsetMin = new Vector2(40f, 0f);
            buildButtonText.rectTransform.offsetMax = new Vector2(-8f, 0f);

            Button button = buildButtonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = buildButtonImage;
            button.onClick.AddListener(ToggleOpen);
            ConfigureButtonColors(button);
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.SoundOnly);
            buildButtonTooltip = StrategyHudTooltip.Attach(
                buildButtonRoot.gameObject,
                "Open construction palette [B].");
        }

        private void CreateMenuLayer(Transform parent)
        {
            RectTransform menuRoot = CreateUiObject("BuildMenuLayer", parent).GetComponent<RectTransform>();
            Stretch(menuRoot, 0f, 0f, 0f, 0f);
            menuGroup = menuRoot.gameObject.AddComponent<CanvasGroup>();
            menuGroup.alpha = 0f;
            menuGroup.blocksRaycasts = false;
            menuGroup.interactable = false;

            paletteRoot = CreateUiObject("BuildPalette", menuRoot).GetComponent<RectTransform>();
            paletteRoot.anchorMin = new Vector2(0.5f, 0f);
            paletteRoot.anchorMax = new Vector2(0.5f, 0f);
            paletteRoot.pivot = new Vector2(0.5f, 0f);
            paletteRoot.anchoredPosition = new Vector2(0f, 12f);
            paletteRoot.sizeDelta = new Vector2(760f, 48f);
            Image paletteBackground = paletteRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(paletteBackground, new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.94f));
            StrategyHudStyle.AddShadow(paletteRoot.gameObject, 0.58f);

            contextRoot = CreateUiObject("BuildContextRow", paletteRoot).GetComponent<RectTransform>();
            contextRoot.anchorMin = new Vector2(0.5f, 0f);
            contextRoot.anchorMax = new Vector2(0.5f, 0f);
            contextRoot.pivot = new Vector2(0.5f, 0f);
            contextRoot.anchoredPosition = new Vector2(0f, 48f);
            contextRoot.sizeDelta = new Vector2(660f, 76f);

            trayRoot = CreateUiObject("BuildItemTray", contextRoot).GetComponent<RectTransform>();
            trayRoot.anchorMin = new Vector2(0f, 0.5f);
            trayRoot.anchorMax = new Vector2(0f, 0.5f);
            trayRoot.pivot = new Vector2(0f, 0.5f);
            trayRoot.anchoredPosition = Vector2.zero;
            trayRoot.sizeDelta = new Vector2(132f, 72f);
            trayGroup = trayRoot.gameObject.AddComponent<CanvasGroup>();
            trayGroup.alpha = 0f;
            trayGroup.blocksRaycasts = false;
            trayGroup.interactable = false;
            HorizontalLayoutGroup trayLayout = trayRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            trayLayout.spacing = 6f;
            trayLayout.childAlignment = TextAnchor.MiddleLeft;
            trayLayout.childControlWidth = false;
            trayLayout.childControlHeight = false;
            trayLayout.childForceExpandWidth = false;
            trayLayout.childForceExpandHeight = false;

            subcategoryRoot = CreateUiObject("BuildSubcategoryDock", contextRoot).GetComponent<RectTransform>();
            subcategoryRoot.anchorMin = new Vector2(0f, 0.5f);
            subcategoryRoot.anchorMax = new Vector2(0f, 0.5f);
            subcategoryRoot.pivot = new Vector2(0f, 0.5f);
            subcategoryRoot.anchoredPosition = Vector2.zero;
            subcategoryRoot.sizeDelta = new Vector2(96f, 72f);
            subcategoryGroup = subcategoryRoot.gameObject.AddComponent<CanvasGroup>();
            subcategoryGroup.alpha = 0f;
            subcategoryGroup.blocksRaycasts = false;
            subcategoryGroup.interactable = false;
            VerticalLayoutGroup subcategoryLayout = subcategoryRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            subcategoryLayout.spacing = 3f;
            subcategoryLayout.childAlignment = TextAnchor.UpperLeft;
            subcategoryLayout.childControlWidth = false;
            subcategoryLayout.childControlHeight = false;
            subcategoryLayout.childForceExpandWidth = false;
            subcategoryLayout.childForceExpandHeight = false;

            dockRoot = CreateUiObject("BuildCategoryDock", paletteRoot).GetComponent<RectTransform>();
            dockRoot.anchorMin = new Vector2(0f, 0f);
            dockRoot.anchorMax = new Vector2(0f, 0f);
            dockRoot.pivot = new Vector2(0f, 0f);
            dockRoot.anchoredPosition = new Vector2(8f, 7f);
            dockRoot.sizeDelta = new Vector2(704f, 34f);
            HorizontalLayoutGroup dockLayout = dockRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            dockLayout.spacing = 4f;
            dockLayout.childAlignment = TextAnchor.MiddleLeft;
            dockLayout.childControlWidth = false;
            dockLayout.childControlHeight = false;
            dockLayout.childForceExpandWidth = false;
            dockLayout.childForceExpandHeight = false;

            for (int i = 0; i < categories.Length; i++)
            {
                categoryUis.Add(CreateCategoryUi(categories[i], i));
            }
        }

        private CategoryUi CreateCategoryUi(BuildCategoryData data, int index)
        {
            RectTransform root = CreateUiObject("BuildCategory_" + data.Label, dockRoot).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(114f, 34f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 114f;
            layout.preferredHeight = 34f;

            Image background = root.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(background, StrategyHudStyle.Elevated, true);
            RectTransform accent = CreateUiObject("Accent", root).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.offsetMin = new Vector2(0f, 0f);
            accent.offsetMax = new Vector2(3f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = data.AccentColor;
            accentImage.raycastTarget = false;

            Text label = CreateText("Label", root, data.Label, 12, TextAnchor.MiddleLeft, new Color(0.86f, 0.91f, 0.96f));
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 12;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(10f, 3f);
            label.rectTransform.offsetMax = new Vector2(-5f, -3f);

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
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.SoundOnly);
            List<BuildItemUi> items = new();
            if (data.HasSubcategories)
            {
                category.Subcategories = new BuildSubcategoryUi[data.Subcategories.Length];
                for (int i = 0; i < data.Subcategories.Length; i++)
                {
                    BuildSubcategoryData subcategoryData = data.Subcategories[i];
                    category.Subcategories[i] = CreateSubcategoryUi(category, subcategoryData, i);
                    for (int j = 0; j < subcategoryData.Items.Length; j++)
                    {
                        items.Add(CreateItemUi(subcategoryData.Items[j], i));
                    }
                }
            }
            else
            {
                category.Subcategories = Array.Empty<BuildSubcategoryUi>();
                for (int i = 0; i < data.Items.Length; i++)
                {
                    items.Add(CreateItemUi(data.Items[i], -1));
                }
            }

            category.Items = items.ToArray();

            return category;
        }

        private BuildSubcategoryUi CreateSubcategoryUi(CategoryUi category, BuildSubcategoryData data, int index)
        {
            RectTransform root = CreateUiObject("BuildSubcategory_" + data.Label, subcategoryRoot).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(96f, 22f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 96f;
            layout.preferredHeight = 22f;

            Image background = root.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(background, StrategyHudStyle.Elevated, true);
            RectTransform accent = CreateUiObject("Accent", root).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.offsetMin = new Vector2(0f, 0f);
            accent.offsetMax = new Vector2(3f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = data.AccentColor;
            accentImage.raycastTarget = false;

            Text label = CreateText("Label", root, data.Label, 11, TextAnchor.MiddleLeft, new Color(0.86f, 0.91f, 0.96f));
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 11;
            Stretch(label.rectTransform, 9f, 1f, 4f, 1f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ConfigureButtonColors(button);

            BuildSubcategoryUi subcategory = new BuildSubcategoryUi
            {
                Data = data,
                Index = index,
                Root = root,
                Background = background,
                Label = label,
                Button = button
            };

            button.onClick.AddListener(() => SelectSubcategory(category, subcategory, true));
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.SoundOnly);
            return subcategory;
        }

        private BuildItemUi CreateItemUi(BuildItemData data, int subcategoryIndex)
        {
            RectTransform root = CreateUiObject("BuildItem_" + data.Tool, trayRoot).GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(132f, 72f);
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 132f;
            layout.preferredHeight = 72f;

            Image background = root.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(background, StrategyHudStyle.Elevated, true);
            RectTransform icon = CreateUiObject("Icon", root).GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.anchoredPosition = new Vector2(6f, 0f);
            icon.sizeDelta = new Vector2(46f, 46f);
            Image iconBackground = icon.gameObject.AddComponent<Image>();
            iconBackground.color = data.Color;
            iconBackground.raycastTarget = false;
            StrategyBuildMenuCatalog.DrawItemIcon(icon, data);

            Text title = CreateText("Title", root, data.Title, 12, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 12;
            title.rectTransform.anchorMin = Vector2.zero;
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(58f, 23f);
            title.rectTransform.offsetMax = new Vector2(-5f, -5f);

            RectTransform badge = CreateUiObject("CostBadge", root).GetComponent<RectTransform>();
            badge.anchorMin = Vector2.zero;
            badge.anchorMax = Vector2.zero;
            badge.pivot = Vector2.zero;
            badge.anchoredPosition = new Vector2(58f, 5f);
            badge.sizeDelta = new Vector2(68f, 17f);
            Image badgeBackground = badge.gameObject.AddComponent<Image>();
            badgeBackground.color = new Color(0.18f, 0.13f, 0.06f, 0.98f);
            badgeBackground.raycastTarget = false;

            Text badgeText = CreateText("CostText", badge, "L0 S0", 10, TextAnchor.MiddleCenter, Color.white);
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.resizeTextForBestFit = true;
            badgeText.resizeTextMinSize = 9;
            badgeText.resizeTextMaxSize = 10;
            Stretch(badgeText.rectTransform, 3f, 0f, 3f, 0f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ConfigureButtonColors(button);

            BuildItemUi item = new BuildItemUi
            {
                Data = data,
                SubcategoryIndex = subcategoryIndex,
                Root = root,
                Background = background,
                IconBackground = iconBackground,
                Title = title,
                BadgeBackground = badgeBackground,
                BadgeText = badgeText,
                Button = button
            };

            button.onClick.AddListener(() => ToggleTool(data));
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.SoundOnly);
            Vector2Int footprint = GetFootprint(data.Tool);
            StrategyHudTooltip.Attach(
                root.gameObject,
                BuildHudText("build.tooltip.footprint", footprint.x, footprint.y),
                StrategyHudTooltipPlacement.Above);
            return item;
        }

        private void RefreshUi()
        {
            RefreshSpeedControls();
            RefreshPlacementFeedback();

            if (buildButtonImage != null)
            {
                buildButtonImage.color = isOpen
                    ? new Color(0.23f, 0.36f, 0.38f, 0.98f)
                    : new Color(0.12f, 0.19f, 0.17f, 0.98f);
            }

            RefreshBuildButtonVisual();
            RefreshBuildButtonTooltip();

            for (int i = 0; i < categoryUis.Count; i++)
            {
                CategoryUi category = categoryUis[i];
                bool selected = selectedCategoryIndex == i;
                bool categoryAllowed = CategoryHasAllowedTool(category);
                category.Button.interactable = categoryAllowed;
                category.Background.color = !categoryAllowed
                    ? new Color(0.06f, 0.07f, 0.08f, 0.88f)
                    : selected
                    ? new Color(0.17f, 0.32f, 0.36f, 0.99f)
                    : new Color(0.11f, 0.17f, 0.15f, 0.98f);
                category.Label.color = !categoryAllowed
                    ? new Color(0.44f, 0.47f, 0.50f, 1f)
                    : selected ? Color.white : new Color(0.86f, 0.91f, 0.96f);

                for (int j = 0; j < category.Subcategories.Length; j++)
                {
                    BuildSubcategoryUi subcategory = category.Subcategories[j];
                    bool subcategoryVisible = selected && category.Data.HasSubcategories;
                    subcategory.Root.gameObject.SetActive(subcategoryVisible);
                    if (!subcategoryVisible)
                    {
                        continue;
                    }

                    bool subcategoryAllowed = SubcategoryHasAllowedTool(category, subcategory);
                    bool subcategorySelected = selectedSubcategoryIndex == subcategory.Index;
                    subcategory.Button.interactable = subcategoryAllowed;
                    subcategory.Background.color = !subcategoryAllowed
                        ? new Color(0.06f, 0.07f, 0.08f, 0.88f)
                        : subcategorySelected
                            ? new Color(0.18f, 0.32f, 0.32f, 0.98f)
                            : new Color(0.11f, 0.17f, 0.15f, 0.98f);
                    subcategory.Label.color = subcategoryAllowed
                        ? Color.white
                        : new Color(0.48f, 0.51f, 0.54f, 1f);
                }

                for (int j = 0; j < category.Items.Length; j++)
                {
                    BuildItemUi item = category.Items[j];
                    bool visible = IsItemInSelectedLayer(category, item);
                    item.Root.gameObject.SetActive(visible);
                    if (!visible)
                    {
                        continue;
                    }

                    bool allowed = IsToolAllowed(item.Data.Tool);
                    bool active = allowed && ActiveTool == item.Data.Tool;
                    bool affordable = CanAffordBuildCost(item.Data.Cost);
                    item.Button.interactable = allowed && affordable;
                    item.Background.color = !allowed
                        ? new Color(0.07f, 0.08f, 0.09f, 0.90f)
                        : !affordable
                        ? new Color(0.10f, 0.11f, 0.13f, 0.88f)
                        : active
                            ? new Color(0.24f, 0.30f, 0.38f, 1f)
                            : new Color(0.13f, 0.20f, 0.18f, 0.98f);
                    item.IconBackground.color = allowed && affordable ? item.Data.Color : new Color(0.24f, 0.25f, 0.27f, 0.92f);
                    item.Title.color = allowed && affordable ? Color.white : new Color(0.62f, 0.66f, 0.70f, 1f);
                    Vector2Int footprint = GetFootprint(item.Data.Tool);
                    item.Root.GetComponent<StrategyHudTooltip>()?.SetText(
                        BuildHudText("build.tooltip.footprint", footprint.x, footprint.y));
                    item.BadgeBackground.color = active
                        ? new Color(0.60f, 0.36f, 0.10f, 0.96f)
                        : allowed && affordable
                            ? new Color(0.18f, 0.13f, 0.06f, 0.98f)
                            : new Color(0.24f, 0.06f, 0.05f, 0.98f);
                    item.BadgeText.text = GetBuildItemBadgeText(item.Data.Cost);
                }
            }

            RefreshCompactBuildGeometry();
            LayoutRebuilder.ForceRebuildLayoutImmediate(dockRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(subcategoryRoot);
            LayoutRebuilder.ForceRebuildLayoutImmediate(trayRoot);
            isDirty = false;
        }

    }
}
