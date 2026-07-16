using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyStarterGoalPhase
    {
        Houses,
        ForagerCamp,
        ProductionCamps,
        ScoutLodge,
        Storage,
        Complete
    }

    internal readonly struct StrategyStarterBuildProgressState
    {
        public StrategyStarterBuildProgressState(
            int completedHouses,
            bool foragerCampCompleted,
            bool lumberjackCampCompleted,
            bool stonecutterCampCompleted,
            bool scoutLodgeCompleted,
            bool storageYardCompleted,
            bool granaryCompleted)
        {
            CompletedHouses = completedHouses;
            ForagerCampCompleted = foragerCampCompleted;
            LumberjackCampCompleted = lumberjackCampCompleted;
            StonecutterCampCompleted = stonecutterCampCompleted;
            ScoutLodgeCompleted = scoutLodgeCompleted;
            StorageYardCompleted = storageYardCompleted;
            GranaryCompleted = granaryCompleted;
        }

        public int CompletedHouses { get; }
        public bool ForagerCampCompleted { get; }
        public bool LumberjackCampCompleted { get; }
        public bool StonecutterCampCompleted { get; }
        public bool ScoutLodgeCompleted { get; }
        public bool StorageYardCompleted { get; }
        public bool GranaryCompleted { get; }
    }

    internal static class StrategyStarterBuildProgression
    {
        internal const int TargetHouseCount = 3;

        private static readonly IReadOnlyList<StrategyBuildTool> BaseToolList = Array.AsReadOnly(new[]
        {
            StrategyBuildTool.House,
            StrategyBuildTool.LumberjackCamp,
            StrategyBuildTool.StonecutterCamp,
            StrategyBuildTool.ForagerCamp,
            StrategyBuildTool.StorageYard,
            StrategyBuildTool.Granary,
            StrategyBuildTool.ScoutLodge
        });

        internal static IReadOnlyList<StrategyBuildTool> BaseTools => BaseToolList;

        internal static StrategyStarterGoalPhase Evaluate(StrategyStarterBuildProgressState state)
        {
            if (state.CompletedHouses < TargetHouseCount)
            {
                return StrategyStarterGoalPhase.Houses;
            }

            if (!state.ForagerCampCompleted)
            {
                return StrategyStarterGoalPhase.ForagerCamp;
            }

            if (!state.LumberjackCampCompleted || !state.StonecutterCampCompleted)
            {
                return StrategyStarterGoalPhase.ProductionCamps;
            }

            if (!state.ScoutLodgeCompleted)
            {
                return StrategyStarterGoalPhase.ScoutLodge;
            }

            if (!state.StorageYardCompleted || !state.GranaryCompleted)
            {
                return StrategyStarterGoalPhase.Storage;
            }

            return StrategyStarterGoalPhase.Complete;
        }
    }
}
