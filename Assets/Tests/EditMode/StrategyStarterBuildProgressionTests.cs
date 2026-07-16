using System.Linq;
using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyStarterBuildProgressionTests
    {
        [Test]
        public void BaseToolsContainOnlyUniversalSettlementMinimum()
        {
            StrategyBuildTool[] expected =
            {
                StrategyBuildTool.House,
                StrategyBuildTool.LumberjackCamp,
                StrategyBuildTool.StonecutterCamp,
                StrategyBuildTool.ForagerCamp,
                StrategyBuildTool.StorageYard,
                StrategyBuildTool.Granary,
                StrategyBuildTool.ScoutLodge
            };

            CollectionAssert.AreEqual(expected, StrategyStarterBuildProgression.BaseTools);
            Assert.That(StrategyStarterBuildProgression.BaseTools.Distinct().Count(), Is.EqualTo(expected.Length));
        }

        [Test]
        public void GoalPhasesPlaceScoutAfterResourceCampsAndBeforeStorage()
        {
            AssertPhase(StrategyStarterGoalPhase.Houses, 2, false, false, false, false, false, false);
            AssertPhase(StrategyStarterGoalPhase.ForagerCamp, 3, false, false, false, false, false, false);
            AssertPhase(StrategyStarterGoalPhase.ProductionCamps, 3, true, true, false, false, false, false);
            AssertPhase(StrategyStarterGoalPhase.ScoutLodge, 3, true, true, true, false, false, false);
            AssertPhase(StrategyStarterGoalPhase.Storage, 3, true, true, true, true, false, false);
            AssertPhase(StrategyStarterGoalPhase.Complete, 3, true, true, true, true, true, true);
        }

        [Test]
        public void EarlierOutOfOrderBuildingsDoNotSkipTheFirstMissingPhase()
        {
            AssertPhase(StrategyStarterGoalPhase.ForagerCamp, 3, false, true, true, true, true, true);
            AssertPhase(StrategyStarterGoalPhase.ProductionCamps, 3, true, false, true, true, true, true);
            AssertPhase(StrategyStarterGoalPhase.ScoutLodge, 3, true, true, true, false, true, true);
            AssertPhase(StrategyStarterGoalPhase.Storage, 3, true, true, true, true, true, false);
        }

        [Test]
        public void AdvancedToolsRemainOutsideTheBaseSet()
        {
            StrategyBuildTool[] advancedTools =
            {
                StrategyBuildTool.Sawmill,
                StrategyBuildTool.Mine,
                StrategyBuildTool.CoalPit,
                StrategyBuildTool.ClayPit,
                StrategyBuildTool.Kiln,
                StrategyBuildTool.Forge,
                StrategyBuildTool.HunterCamp,
                StrategyBuildTool.FisherHut,
                StrategyBuildTool.ChickenCoop,
                StrategyBuildTool.TradingPost,
                StrategyBuildTool.Bridge
            };

            foreach (StrategyBuildTool tool in advancedTools)
            {
                CollectionAssert.DoesNotContain(StrategyStarterBuildProgression.BaseTools, tool, tool.ToString());
            }
        }

        private static void AssertPhase(
            StrategyStarterGoalPhase expected,
            int houses,
            bool forager,
            bool lumberjack,
            bool stonecutter,
            bool scout,
            bool storage,
            bool granary)
        {
            StrategyStarterBuildProgressState state = new(
                houses,
                forager,
                lumberjack,
                stonecutter,
                scout,
                storage,
                granary);

            Assert.That(StrategyStarterBuildProgression.Evaluate(state), Is.EqualTo(expected));
        }
    }
}
