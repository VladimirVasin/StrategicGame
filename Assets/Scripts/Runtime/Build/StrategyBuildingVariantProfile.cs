using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBuildingVariantProfile
    {
        public static int NormalizeVariant(StrategyBuildTool tool, int variant)
        {
            return tool == StrategyBuildTool.ForagerCamp
                || tool == StrategyBuildTool.ChickenCoop
                || tool == StrategyBuildTool.ScoutLodge
                    ? 0
                    : Mathf.Max(0, variant);
        }
    }
}
