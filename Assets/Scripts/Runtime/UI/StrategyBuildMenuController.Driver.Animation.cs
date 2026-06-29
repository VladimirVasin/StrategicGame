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

            if (dockRoot != null)
            {
                dockRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(-92f, 84f, easedMenu));
                dockRoot.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, easedMenu);
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

            if (subcategoryRoot != null)
            {
                subcategoryRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(78f, 152f, easedSubcategory) + Mathf.Lerp(-112f, 0f, easedMenu));
                subcategoryRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, easedSubcategory);
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

            if (trayRoot != null)
            {
                float trayOpenY = subcategoryOpen ? 204f : 152f;
                trayRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(78f, trayOpenY, easedTray) + Mathf.Lerp(-112f, 0f, easedMenu));
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

                for (int j = 0; j < category.Subcategories.Length; j++)
                {
                    BuildSubcategoryUi subcategory = category.Subcategories[j];
                    if (!subcategory.Root.gameObject.activeSelf)
                    {
                        continue;
                    }

                    bool active = selectedSubcategoryIndex == subcategory.Index;
                    float subcategoryTarget = subcategory.IsHovered || active ? 1f : 0f;
                    subcategory.HoverT = Mathf.MoveTowards(subcategory.HoverT, subcategoryTarget, Time.unscaledDeltaTime * 8f);
                    subcategory.Root.localScale = Vector3.one * Mathf.Lerp(1f, active ? 1.06f : 1.03f, Smooth01(subcategory.HoverT));
                }

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
    }
}
