using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
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

            bool subcategoryOpen = isOpen && selectedCategoryIndex >= 0
                && selectedCategoryIndex < categoryUis.Count
                && categoryUis[selectedCategoryIndex].Data.HasSubcategories;
            subcategoryT = Mathf.MoveTowards(subcategoryT, subcategoryOpen ? 1f : 0f, Time.unscaledDeltaTime * TrayAnimationSpeed);
            float easedSubcategory = Smooth01(subcategoryT);

            if (subcategoryGroup != null)
            {
                subcategoryGroup.alpha = easedSubcategory;
                subcategoryGroup.blocksRaycasts = subcategoryOpen && easedSubcategory > 0.45f;
                subcategoryGroup.interactable = subcategoryOpen && easedSubcategory > 0.45f;
            }

            bool trayOpen = isOpen
                && selectedCategoryIndex >= 0
                && (!subcategoryOpen || selectedSubcategoryIndex >= 0);
            trayT = Mathf.MoveTowards(trayT, trayOpen ? 1f : 0f, Time.unscaledDeltaTime * TrayAnimationSpeed);
            float easedTray = Smooth01(trayT);

            if (trayGroup != null)
            {
                trayGroup.alpha = easedTray;
                trayGroup.blocksRaycasts = trayOpen && easedTray > 0.45f;
                trayGroup.interactable = trayOpen && easedTray > 0.45f;
            }

            if (paletteRoot != null)
            {
                float contextAmount = Mathf.Max(easedSubcategory, easedTray);
                paletteRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(-44f, 12f, easedMenu));
                paletteRoot.sizeDelta = new Vector2(760f, Mathf.Lerp(48f, 136f, contextAmount));
            }

            if (placementFeedbackRoot != null && placementFeedbackRoot.gameObject.activeSelf)
            {
                const float targetY = 12f;
                float currentY = placementFeedbackRoot.anchoredPosition.y;
                float y = StrategyHudStyle.ReducedMotion
                    ? targetY
                    : Mathf.Lerp(currentY, targetY, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                placementFeedbackRoot.anchoredPosition = new Vector2(0f, y);
            }

        }

        private void RefreshCompactBuildGeometry()
        {
            bool hasSelection = selectedCategoryIndex >= 0
                && selectedCategoryIndex < categoryUis.Count;
            CategoryUi selected = hasSelection ? categoryUis[selectedCategoryIndex] : null;
            bool hasSubcategories = selected != null && selected.Data.HasSubcategories;
            int visibleItemCount = 0;
            if (selected != null)
            {
                for (int i = 0; i < selected.Items.Length; i++)
                {
                    if (IsItemInSelectedLayer(selected, selected.Items[i]))
                    {
                        visibleItemCount++;
                    }
                }
            }

            float trayWidth = visibleItemCount > 0
                ? visibleItemCount * 132f + (visibleItemCount - 1) * 6f
                : 0f;
            float subcategoryWidth = hasSubcategories ? 96f : 0f;
            float gap = subcategoryWidth > 0f && trayWidth > 0f ? 6f : 0f;
            float contextWidth = Mathf.Max(1f, subcategoryWidth + gap + trayWidth);
            contextRoot.sizeDelta = new Vector2(contextWidth, 76f);
            subcategoryRoot.anchoredPosition = Vector2.zero;
            trayRoot.anchoredPosition = new Vector2(subcategoryWidth + gap, 0f);
            trayRoot.sizeDelta = new Vector2(Mathf.Max(1f, trayWidth), 72f);
        }

        private void RefreshBuildButtonVisual()
        {
            if (buildButtonRoot == null || buildButtonText == null)
            {
                return;
            }

            bool placementMode = ActiveTool != StrategyBuildTool.None && !isOpen;
            buildButtonRoot.gameObject.SetActive(!placementMode);
            if (placementMode)
            {
                return;
            }

            buildButtonRoot.anchoredPosition = isOpen
                ? new Vector2(358f, 18f)
                : new Vector2(0f, 12f);
            buildButtonRoot.sizeDelta = isOpen
                ? new Vector2(36f, 36f)
                : new Vector2(132f, 40f);
            buildButtonRoot.SetAsLastSibling();
            buildButtonIconRoot.gameObject.SetActive(!isOpen);
            buildButtonText.text = isOpen ? "×" : "BUILD  [B]";
            buildButtonText.alignment = isOpen ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            buildButtonText.fontSize = isOpen ? 20 : 13;
            buildButtonText.resizeTextMinSize = isOpen ? 18 : 11;
            buildButtonText.resizeTextMaxSize = isOpen ? 20 : 13;
            buildButtonText.rectTransform.offsetMin = isOpen ? Vector2.zero : new Vector2(40f, 0f);
            buildButtonText.rectTransform.offsetMax = isOpen ? Vector2.zero : new Vector2(-8f, 0f);
        }
    }
}
