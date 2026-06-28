using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private static bool IsAuxiliaryInspectRenderer(SpriteRenderer renderer)
        {
            string objectName = renderer.gameObject.name;
            return objectName.Contains("Shadow")
                || objectName.Contains("Outline")
                || objectName.Contains("Line")
                || objectName.Contains("Damage")
                || objectName.Contains("Readability");
        }

        private StrategyWorldInspectInfo BuildGraveInspectInfo(StrategyGraveMarker grave)
        {
            bool hasCell = TryGetCellForWorld(grave.transform.position, out Vector2Int cell);
            string body = grave.DeceasedName
                + "\n"
                + grave.GetLifeText().Replace("\n", " / ");
            return new StrategyWorldInspectInfo("Grave", grave.FamilyRole, body, grave.PreviewSprite, cell, hasCell);
        }

        private static string GetBuildingTitle(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => "House",
                StrategyBuildTool.LumberjackCamp => "Lumberjack Camp",
                StrategyBuildTool.StonecutterCamp => "Stonecutter Camp",
                StrategyBuildTool.Sawmill => "Sawmill",
                StrategyBuildTool.Mine => "Mine",
                StrategyBuildTool.CoalPit => "Coal Pit",
                StrategyBuildTool.ClayPit => "Clay Pit",
                StrategyBuildTool.Kiln => "Kiln",
                StrategyBuildTool.Forge => "Forge",
                StrategyBuildTool.HunterCamp => "Hunter Camp",
                StrategyBuildTool.FisherHut => "Fisher Hut",
                StrategyBuildTool.ForagerCamp => "Forager Camp",
                StrategyBuildTool.ChickenCoop => "Chicken Coop",
                StrategyBuildTool.TradingPost => "Trading Post",
                StrategyBuildTool.StorageYard => "Storage Yard",
                StrategyBuildTool.Granary => "Granary",
                StrategyBuildTool.Bridge => "Bridge",
                _ => tool.ToString()
            };
        }
    }
}
