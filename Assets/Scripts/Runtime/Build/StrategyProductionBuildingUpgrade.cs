using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyProductionBuildingUpgradeType
    {
        None,
        IronAxes,
        IronPickaxes,
        ReinforcedMiningTools,
        PitToolRack,
        IronShovels,
        SawBladeSet,
        KilnToolSet,
        BetterAnvilTools,
        DeerHuntingKit,
        HooksAndLineKit
    }

    public enum StrategyProductionUpgradeInstallFailureReason
    {
        None,
        InvalidTarget,
        AlreadyInstalled,
        NotEnoughResources
    }

    public readonly struct StrategyProductionBuildingUpgradeDefinition
    {
        public StrategyProductionBuildingUpgradeDefinition(
            StrategyProductionBuildingUpgradeType type,
            StrategyBuildTool tool,
            string title,
            string effectText,
            StrategyProductionUpgradeCost cost,
            float workSpeedMultiplier)
        {
            Type = type;
            Tool = tool;
            Title = title;
            EffectText = effectText;
            Cost = cost;
            WorkSpeedMultiplier = Mathf.Max(1f, workSpeedMultiplier);
        }

        public StrategyProductionBuildingUpgradeType Type { get; }
        public StrategyBuildTool Tool { get; }
        public string Title { get; }
        public string EffectText { get; }
        public StrategyProductionUpgradeCost Cost { get; }
        public float WorkSpeedMultiplier { get; }
    }
}
