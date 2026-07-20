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

        private static string GetBuildingSubtitle(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return L("building.generic");
            }

            if (building.Tool == StrategyBuildTool.House)
            {
                string familyName = GetHouseFamilyName(building);
                return string.IsNullOrWhiteSpace(familyName)
                    ? L("building.subtitle.house_unoccupied")
                    : L("building.subtitle.house_family", familyName);
            }

            return StrategySelectionLocalization.TextOrLiteral(
                "building.subtitle." + StrategySelectionLocalization.ToKeyToken(
                    building.Tool.ToString()),
                L("building.generic"));
        }

        private static string GetHouseFamilyName(StrategyPlacedBuilding building)
        {
            string householderFamily = building.Householder != null
                ? building.Householder.FamilyName
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(householderFamily))
            {
                return householderFamily;
            }

            for (int i = 0; i < building.Residents.Count; i++)
            {
                StrategyResidentAgent resident = building.Residents[i];
                if (resident != null && !string.IsNullOrWhiteSpace(resident.FamilyName))
                {
                    return resident.FamilyName;
                }
            }

            return string.Empty;
        }

        private static string GetUpgradeTitle(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds
                ? L("upgrade.garden_beds")
                : L("upgrade.chicken_coop");
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return StrategySelectionLocalization.Resource(type);
        }

        private static string GetResidentStatus(StrategyResidentAgent resident)
        {
            return StrategyResidentHudText.GetStatusText(resident);
        }

        private static string GetHouseResidentStatus(
            StrategyPlacedBuilding building,
            StrategyResidentAgent resident)
        {
            bool householder = resident == building.Householder;
            string key = householder
                ? resident.IsHungry ? "house.householder_status_hungry" : "house.householder_status"
                : resident.IsHungry ? "house.resident_status_hungry" : "house.resident_status";
            return resident.IsHungry
                ? L(key, GetResidentLifeStageTitle(resident), resident.DisplayAgeYears,
                    StrategyResidentHudText.GetFoodTitle(resident))
                : L(key, GetResidentLifeStageTitle(resident), resident.DisplayAgeYears);
        }

        private static string GetConstructionBuildersText(StrategyConstructionSite site)
        {
            if (site == null || site.BuilderCount <= 0)
            {
                return L("construction.no_builders");
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

                text += L(
                    "format.resident_status",
                    builder.FullName,
                    GetResidentStatus(builder));
            }

            return string.IsNullOrEmpty(text) ? L("construction.no_builders") : text;
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
