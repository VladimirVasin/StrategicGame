using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private static Sprite GetBuildingPreviewSprite(StrategyPlacedBuilding building)
        {
            if (building != null && building.Tool == StrategyBuildTool.Bridge)
            {
                return StrategyBuildingSpriteFactory.GetBridgeSprite(building.Footprint);
            }

            if (building != null && StrategyBuildingSpriteFactory.TryGetBuildSprite(building.Tool, building.VisualVariant, out Sprite sprite))
            {
                return sprite;
            }

            return null;
        }

        private static string GetResidentRoleTitle(StrategyResidentAgent resident)
        {
            return StrategyResidentHudText.GetRoleTitle(resident);
        }

        private static string FormatCell(Vector2Int cell)
        {
            return cell.x + ", " + cell.y;
        }

        private static string GetUpgradeTitle(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds
                ? "Garden Beds"
                : "Chicken Coop";
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Eggs => "Eggs",
                StrategyResourceType.Turnip => "Turnip",
                StrategyResourceType.Cabbage => "Cabbage",
                StrategyResourceType.Onion => "Onion",
                StrategyResourceType.Carrot => "Carrot",
                StrategyResourceType.Potato => "Potato",
                StrategyResourceType.Berries => "Berries",
                StrategyResourceType.Roots => "Roots",
                StrategyResourceType.Mushrooms => "Mushrooms",
                StrategyResourceType.Game => "Game",
                StrategyResourceType.Fish => "Fish",
                StrategyResourceType.Iron => "Iron",
                StrategyResourceType.Coal => "Coal",
                StrategyResourceType.Planks => "Planks",
                _ => "none"
            };
        }

        private static string GetResidentStatus(StrategyResidentAgent resident)
        {
            return StrategyResidentHudText.GetStatusText(resident);
        }

        private static string GetConstructionBuildersText(StrategyConstructionSite site)
        {
            if (site == null || site.BuilderCount <= 0)
            {
                return "no builders assigned";
            }

            string text = string.Empty;
            for (int i = 0; i < site.BuilderCount; i++)
            {
                if (!site.TryGetBuilder(i, out StrategyResidentAgent builder) || builder == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(text))
                {
                    text += "\n";
                }

                text += builder.FullName + " - " + GetResidentStatus(builder);
            }

            return string.IsNullOrEmpty(text) ? "no builders assigned" : text;
        }

        private static string GetResidentGenderTitle(StrategyResidentGender gender)
        {
            return StrategyResidentHudText.GetGenderTitle(gender);
        }

        private static string GetResidentLifeStageTitle(StrategyResidentAgent resident)
        {
            return StrategyResidentHudText.GetLifeStageTitle(resident);
        }
    }
}
