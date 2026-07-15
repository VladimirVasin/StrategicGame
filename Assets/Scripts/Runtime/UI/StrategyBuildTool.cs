using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyBuildTool
    {
        None,
        House,
        LumberjackCamp,
        StonecutterCamp,
        Sawmill,
        Mine,
        CoalPit,
        ClayPit,
        Kiln,
        Forge,
        HunterCamp,
        FisherHut,
        ForagerCamp,
        ChickenCoop,
        TradingPost,
        StarterCaravanCart,
        StorageYard,
        Granary,
        Bridge,
        ScoutLodge
    }

    public readonly struct StrategyBuildToolInfo
    {
        public StrategyBuildToolInfo(StrategyBuildTool tool, string title, StrategyConstructionResourceCost cost, Color color, Vector2Int footprint)
        {
            Tool = tool;
            Title = title;
            Cost = cost;
            Color = color;
            Footprint = footprint;
        }

        public StrategyBuildTool Tool { get; }
        public string Title { get; }
        public StrategyConstructionResourceCost Cost { get; }
        public Color Color { get; }
        public Vector2Int Footprint { get; }
    }
}
