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
        private readonly string title;
        private readonly string effectText;
        private readonly string localizationTable;
        private readonly string titleKey;
        private readonly string effectTextKey;

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
            this.title = title;
            this.effectText = effectText;
            localizationTable = string.Empty;
            titleKey = string.Empty;
            effectTextKey = string.Empty;
            Cost = cost;
            WorkSpeedMultiplier = Mathf.Max(1f, workSpeedMultiplier);
        }

        internal StrategyProductionBuildingUpgradeDefinition(
            StrategyProductionBuildingUpgradeType type,
            StrategyBuildTool tool,
            string title,
            string effectText,
            StrategyProductionUpgradeCost cost,
            float workSpeedMultiplier,
            string localizationTable,
            string titleKey,
            string effectTextKey)
            : this(type, tool, title, effectText, cost, workSpeedMultiplier)
        {
            this.localizationTable = localizationTable ?? string.Empty;
            this.titleKey = titleKey ?? string.Empty;
            this.effectTextKey = effectTextKey ?? string.Empty;
        }

        public StrategyProductionBuildingUpgradeType Type { get; }
        public StrategyBuildTool Tool { get; }
        public string Title => Resolve(title, titleKey);
        public string EffectText => Resolve(effectText, effectTextKey);
        public StrategyProductionUpgradeCost Cost { get; }
        public float WorkSpeedMultiplier { get; }

        private string Resolve(string fallback, string key)
        {
            if (string.IsNullOrEmpty(localizationTable) || string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            string localized = StrategyLocalization.Get(localizationTable, key);
            return localized == key ? fallback : localized;
        }
    }
}
