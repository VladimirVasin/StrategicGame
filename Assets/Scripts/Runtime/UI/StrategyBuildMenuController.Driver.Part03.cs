using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private bool IsItemInSelectedLayer(CategoryUi category, BuildItemUi item)
        {
            if (category == null || item == null || selectedCategoryIndex != category.Index)
            {
                return false;
            }

            if (!category.Data.HasSubcategories)
            {
                return true;
            }

            return selectedSubcategoryIndex >= 0 && item.SubcategoryIndex == selectedSubcategoryIndex;
        }

        private bool IsPointerOverBuildUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static void EnsureEventSystem()
        {
            StrategyUiInputModuleBootstrap.Ensure();
        }

        internal static Vector2Int GetFootprint(StrategyBuildTool tool)
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
                StrategyBuildTool.ChickenCoop => new Vector2Int(4, 4),
                StrategyBuildTool.TradingPost => new Vector2Int(3, 2),
                StrategyBuildTool.StorageYard => new Vector2Int(3, 2),
                StrategyBuildTool.Granary => new Vector2Int(3, 2),
                StrategyBuildTool.Bridge => Vector2Int.one,
                StrategyBuildTool.ScoutLodge => new Vector2Int(2, 4),
                _ => Vector2Int.one
            };
        }
    }
}
