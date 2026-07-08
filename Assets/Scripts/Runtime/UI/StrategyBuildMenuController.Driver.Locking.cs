using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private readonly HashSet<StrategyBuildTool> allowedTools = new();
        private bool hasToolLock;

        public void SetAllowedTools(IEnumerable<StrategyBuildTool> tools)
        {
            allowedTools.Clear();
            if (tools != null)
            {
                foreach (StrategyBuildTool tool in tools)
                {
                    if (tool != StrategyBuildTool.None)
                    {
                        allowedTools.Add(tool);
                    }
                }
            }

            hasToolLock = allowedTools.Count > 0;
            DropDisallowedSelection();
            isDirty = true;
            StrategyDebugLogger.Info("BuildMenu", "ToolLockSet", StrategyDebugLogger.F("count", allowedTools.Count));
        }

        public void ClearAllowedTools()
        {
            if (!hasToolLock && allowedTools.Count == 0)
            {
                return;
            }

            allowedTools.Clear();
            hasToolLock = false;
            isDirty = true;
            StrategyDebugLogger.Info("BuildMenu", "ToolLockCleared");
        }

        public bool IsToolAllowedForBuild(StrategyBuildTool tool)
        {
            return IsToolAllowed(tool);
        }

        private bool IsToolAllowed(StrategyBuildTool tool)
        {
            return tool != StrategyBuildTool.None
                && (!hasToolLock || allowedTools.Contains(tool));
        }

        private bool CategoryHasAllowedTool(CategoryUi category)
        {
            if (category == null || category.Items == null)
            {
                return false;
            }

            if (!hasToolLock)
            {
                return true;
            }

            for (int i = 0; i < category.Items.Length; i++)
            {
                BuildItemUi item = category.Items[i];
                if (item != null && IsToolAllowed(item.Data.Tool))
                {
                    return true;
                }
            }

            return false;
        }

        private bool SubcategoryHasAllowedTool(CategoryUi category, BuildSubcategoryUi subcategory)
        {
            if (category == null || subcategory == null || subcategory.Data == null)
            {
                return false;
            }

            if (!hasToolLock)
            {
                return true;
            }

            BuildItemData[] items = subcategory.Data.Items;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && IsToolAllowed(items[i].Tool))
                {
                    return true;
                }
            }

            return false;
        }

        private void RejectLockedTool(StrategyBuildTool tool)
        {
            ActiveTool = StrategyBuildTool.None;
            isDirty = true;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
            StrategyDebugLogger.Warn(
                "BuildMenu",
                "ToolSelectionRejected",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("reason", "locked_by_goal"));
        }

        private void DropDisallowedSelection()
        {
            if (ActiveTool != StrategyBuildTool.None && !IsToolAllowed(ActiveTool))
            {
                StrategyDebugLogger.Info(
                    "BuildMenu",
                    "ToolCleared",
                    StrategyDebugLogger.F("tool", ActiveTool),
                    StrategyDebugLogger.F("reason", "locked_by_goal"));
                ActiveTool = StrategyBuildTool.None;
            }

            if (selectedCategoryIndex >= 0
                && selectedCategoryIndex < categoryUis.Count
                && !CategoryHasAllowedTool(categoryUis[selectedCategoryIndex]))
            {
                selectedCategoryIndex = -1;
                selectedSubcategoryIndex = -1;
                subcategoryT = 0f;
                trayT = 0f;
            }

            if (selectedCategoryIndex >= 0
                && selectedCategoryIndex < categoryUis.Count
                && selectedSubcategoryIndex >= 0)
            {
                CategoryUi category = categoryUis[selectedCategoryIndex];
                if (category.Subcategories == null
                    || selectedSubcategoryIndex >= category.Subcategories.Length
                    || !SubcategoryHasAllowedTool(category, category.Subcategories[selectedSubcategoryIndex]))
                {
                    selectedSubcategoryIndex = -1;
                    trayT = 0f;
                }
            }
        }

        private bool TryRefreshBuildHud()
        {
            if (isDirty || (isOpen && Time.frameCount % 12 == 0))
            {
                return true;
            }

            if (Time.frameCount % 30 == 0)
            {
                RefreshPassiveBuildHud();
            }

            return false;
        }

        private void RefreshPassiveBuildHud()
        {
            if (treasuryText != null)
            {
                treasuryText.text = FormatConstructionResourceCounter(StrategyStorageYard.GetTotalConstructionResources());
            }

            RefreshSpeedControls();
        }
    }
}
