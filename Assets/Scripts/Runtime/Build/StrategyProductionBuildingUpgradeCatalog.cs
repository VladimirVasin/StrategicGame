using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyProductionBuildingUpgradeCatalog
    {
        private static readonly StrategyProductionBuildingUpgradeDefinition[] Definitions =
        {
            Localized(StrategyProductionBuildingUpgradeType.IronAxes, StrategyBuildTool.LumberjackCamp, "Iron Axes", "Faster chopping and bucking.", new StrategyProductionUpgradeCost(1, 1), 1.25f, "iron_axes"),
            Localized(StrategyProductionBuildingUpgradeType.IronPickaxes, StrategyBuildTool.StonecutterCamp, "Iron Pickaxes", "Faster stonecutting.", new StrategyProductionUpgradeCost(1, 1), 1.25f, "iron_pickaxes"),
            Localized(StrategyProductionBuildingUpgradeType.ReinforcedMiningTools, StrategyBuildTool.Mine, "Reinforced Mining Tools", "Faster underground iron mining.", new StrategyProductionUpgradeCost(2, 2), 1.25f, "reinforced_mining_tools"),
            Localized(StrategyProductionBuildingUpgradeType.PitToolRack, StrategyBuildTool.CoalPit, "Pit Tool Rack", "Faster coal extraction.", new StrategyProductionUpgradeCost(2, 1), 1.25f, "pit_tool_rack"),
            Localized(StrategyProductionBuildingUpgradeType.IronShovels, StrategyBuildTool.ClayPit, "Iron Shovels", "Faster clay digging.", new StrategyProductionUpgradeCost(1, 1), 1.25f, "iron_shovels"),
            Localized(StrategyProductionBuildingUpgradeType.SawBladeSet, StrategyBuildTool.Sawmill, "Saw Blade Set", "Faster plank sawing.", new StrategyProductionUpgradeCost(2, 2), 1.25f, "saw_blade_set"),
            Localized(StrategyProductionBuildingUpgradeType.KilnToolSet, StrategyBuildTool.Kiln, "Kiln Tool Set", "Faster pottery firing.", new StrategyProductionUpgradeCost(1, 0, 2), 1.20f, "kiln_tool_set"),
            Localized(StrategyProductionBuildingUpgradeType.BetterAnvilTools, StrategyBuildTool.Forge, "Better Anvil Tools", "Faster tool forging.", new StrategyProductionUpgradeCost(2, 1, 2), 1.20f, "better_anvil_tools"),
            Localized(StrategyProductionBuildingUpgradeType.DeerHuntingKit, StrategyBuildTool.HunterCamp, "Deer Hunting Kit", "Unlocks adult deer hunting.", new StrategyProductionUpgradeCost(2, 1), 1.00f, "deer_hunting_kit"),
            Localized(StrategyProductionBuildingUpgradeType.HooksAndLineKit, StrategyBuildTool.FisherHut, "Hooks and Line Kit", "Faster fishing cycle.", new StrategyProductionUpgradeCost(1, 1), 1.25f, "hooks_and_line_kit")
        };

        public static bool TryGetForTool(
            StrategyBuildTool tool,
            out StrategyProductionBuildingUpgradeDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Tool == tool)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public static bool HasInstalledUpgrade(
            MonoBehaviour component,
            StrategyProductionBuildingUpgradeType type)
        {
            StrategyPlacedBuilding building = component != null
                ? component.GetComponent<StrategyPlacedBuilding>()
                : null;
            return building != null && building.HasProductionUpgrade(type);
        }

        public static float GetWorkSpeedMultiplier(MonoBehaviour component)
        {
            StrategyPlacedBuilding building = component != null
                ? component.GetComponent<StrategyPlacedBuilding>()
                : null;
            return TryGetInstalledDefinition(building, out StrategyProductionBuildingUpgradeDefinition definition)
                ? definition.WorkSpeedMultiplier
                : 1f;
        }

        public static bool TryInstall(
            StrategyPlacedBuilding building,
            out StrategyProductionUpgradeInstallFailureReason failureReason)
        {
            failureReason = StrategyProductionUpgradeInstallFailureReason.None;
            if (!TryGetForTool(building != null ? building.Tool : StrategyBuildTool.None, out StrategyProductionBuildingUpgradeDefinition definition))
            {
                failureReason = StrategyProductionUpgradeInstallFailureReason.InvalidTarget;
                return false;
            }

            if (building.HasProductionUpgrade(definition.Type))
            {
                failureReason = StrategyProductionUpgradeInstallFailureReason.AlreadyInstalled;
                return false;
            }

            if (!StrategyStorageYard.TrySpendProductionUpgradeResources(
                    definition.Cost,
                    building.FootprintBounds.center,
                    "production_upgrade_" + definition.Type))
            {
                failureReason = StrategyProductionUpgradeInstallFailureReason.NotEnoughResources;
                return false;
            }

            if (!building.TryRegisterProductionUpgrade(definition.Type))
            {
                failureReason = StrategyProductionUpgradeInstallFailureReason.AlreadyInstalled;
                return false;
            }

            StrategyDebugLogger.Info(
                "BuildingUpgrade",
                "ProductionUpgradeInstalled",
                StrategyDebugLogger.F("building", building.Tool),
                StrategyDebugLogger.F("origin", building.Origin),
                StrategyDebugLogger.F("upgrade", definition.Type),
                StrategyDebugLogger.F("cost", definition.Cost.ToDisplayText()));
            return true;
        }

        private static bool TryGetInstalledDefinition(
            StrategyPlacedBuilding building,
            out StrategyProductionBuildingUpgradeDefinition definition)
        {
            if (building != null
                && TryGetForTool(building.Tool, out definition)
                && building.HasProductionUpgrade(definition.Type))
            {
                return true;
            }

            definition = default;
            return false;
        }

        private static StrategyProductionBuildingUpgradeDefinition Localized(
            StrategyProductionBuildingUpgradeType type,
            StrategyBuildTool tool,
            string title,
            string effectText,
            StrategyProductionUpgradeCost cost,
            float workSpeedMultiplier,
            string keyPrefix)
        {
            return new StrategyProductionBuildingUpgradeDefinition(
                type,
                tool,
                title,
                effectText,
                cost,
                workSpeedMultiplier,
                StrategyLocalizationTables.Buildings,
                "upgrade." + keyPrefix + ".title",
                "upgrade." + keyPrefix + ".effect");
        }
    }
}
