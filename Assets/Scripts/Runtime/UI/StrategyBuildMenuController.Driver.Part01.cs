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
                treasuryText.text = "Logs "
                    + available.Logs
                    + "  Stone "
                    + available.Stone
                    + (available.Planks > 0 ? "  Planks " + available.Planks : string.Empty);
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
                bool categoryAllowed = CategoryHasAllowedTool(category);
                category.Button.interactable = categoryAllowed;
                category.Background.color = !categoryAllowed
                    ? new Color(0.06f, 0.07f, 0.08f, 0.88f)
                    : selected
                    ? new Color(0.17f, 0.32f, 0.36f, 0.99f)
                    : new Color(0.09f, 0.15f, 0.18f, 0.96f);
                category.Label.color = !categoryAllowed
                    ? new Color(0.44f, 0.47f, 0.50f, 1f)
                    : selected ? Color.white : new Color(0.86f, 0.91f, 0.96f);

                for (int j = 0; j < category.Items.Length; j++)
                {
                    BuildItemUi item = category.Items[j];
                    bool visible = selected;
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
                            : new Color(0.13f, 0.18f, 0.24f, 0.98f);
                    item.IconBackground.color = allowed && affordable ? item.Data.Color : new Color(0.24f, 0.25f, 0.27f, 0.92f);
                    item.Title.color = allowed && affordable ? Color.white : new Color(0.62f, 0.66f, 0.70f, 1f);
                    item.BadgeBackground.color = active
                        ? new Color(0.60f, 0.36f, 0.10f, 0.96f)
                        : allowed && affordable
                            ? new Color(0.18f, 0.13f, 0.06f, 0.98f)
                            : new Color(0.24f, 0.06f, 0.05f, 0.98f);
                    item.BadgeText.text = GetBuildItemBadgeText(item.Data.Cost, allowed, active);
                }
            }

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

        private static Vector2Int GetFootprint(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => new Vector2Int(2, 2),
                StrategyBuildTool.LumberjackCamp => new Vector2Int(2, 2),
                StrategyBuildTool.StonecutterCamp => new Vector2Int(2, 2),
                StrategyBuildTool.Sawmill => new Vector2Int(3, 2),
                StrategyBuildTool.Mine => new Vector2Int(2, 2),
                StrategyBuildTool.CoalPit => new Vector2Int(2, 2),
                StrategyBuildTool.ClayPit => new Vector2Int(2, 2),
                StrategyBuildTool.Kiln => new Vector2Int(2, 2),
                StrategyBuildTool.Forge => new Vector2Int(2, 2),
                StrategyBuildTool.HunterCamp => new Vector2Int(2, 2),
                StrategyBuildTool.FisherHut => new Vector2Int(2, 2),
                StrategyBuildTool.ForagerCamp => new Vector2Int(2, 2),
                StrategyBuildTool.ChickenCoop => new Vector2Int(2, 2),
                StrategyBuildTool.TradingPost => new Vector2Int(3, 2),
                StrategyBuildTool.StorageYard => new Vector2Int(3, 2),
                StrategyBuildTool.Granary => new Vector2Int(3, 2),
                StrategyBuildTool.Bridge => Vector2Int.one,
                _ => Vector2Int.one
            };
        }

    }
}
