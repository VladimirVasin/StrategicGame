using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyScoutTargetSelectorTests
    {
        [Test]
        public void SelectsNearestUnexploredWalkableFrontier()
        {
            HashSet<Vector2Int> explored = new HashSet<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(5, 2)
            };

            bool found = StrategyScoutTargetSelector.TrySelectTarget(
                8,
                5,
                new Vector2Int(0, 2),
                _ => true,
                explored.Contains,
                _ => false,
                out Vector2Int target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(explored.Contains(target), Is.False);
        }

        [Test]
        public void ExcludesExploredNonWalkableUnavailableAndNonFrontierCells()
        {
            Vector2Int exploredCell = new Vector2Int(3, 2);
            Vector2Int nonWalkable = new Vector2Int(2, 2);
            Vector2Int unavailable = new Vector2Int(3, 1);
            Vector2Int valid = new Vector2Int(4, 2);
            Vector2Int nonFrontier = new Vector2Int(6, 4);
            HashSet<Vector2Int> explored = new HashSet<Vector2Int> { exploredCell };

            bool found = StrategyScoutTargetSelector.TrySelectTarget(
                7,
                5,
                new Vector2Int(6, 2),
                cell => cell != nonWalkable,
                explored.Contains,
                cell => cell == unavailable,
                out Vector2Int target);

            Assert.That(explored.Contains(exploredCell), Is.True);
            Assert.That(explored.Contains(nonFrontier), Is.False);
            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(valid));
        }

        [Test]
        public void FullyExploredMapEdgesDoNotBecomeFrontier()
        {
            bool found = StrategyScoutTargetSelector.TrySelectTarget(
                5,
                4,
                new Vector2Int(2, 2),
                _ => true,
                _ => true,
                _ => false,
                out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void FullyUnexploredMapHasNoConnectedFrontier()
        {
            bool found = StrategyScoutTargetSelector.TrySelectTarget(
                5,
                4,
                new Vector2Int(2, 2),
                _ => true,
                _ => false,
                _ => false,
                out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void EqualDistancePrefersGreaterUnknownCoverage()
        {
            Vector2Int left = new Vector2Int(5, 10);
            Vector2Int right = new Vector2Int(15, 10);
            HashSet<Vector2Int> explored = new HashSet<Vector2Int>();
            for (int y = 5; y <= 15; y++)
            {
                for (int x = 0; x <= 10; x++)
                {
                    explored.Add(new Vector2Int(x, y));
                }
            }

            explored.Remove(left);
            explored.Add(right + Vector2Int.left);

            bool found = StrategyScoutTargetSelector.TrySelectTarget(
                21,
                21,
                new Vector2Int(10, 10),
                cell => cell == left || cell == right,
                explored.Contains,
                _ => false,
                out Vector2Int target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(right));
        }

        [Test]
        public void ScoutLodgeUsesTwoByFourFootprintAndStableVariant()
        {
            Assert.That(
                StrategyBuildMenuControllerDriver.GetFootprint(StrategyBuildTool.ScoutLodge),
                Is.EqualTo(new Vector2Int(2, 4)));
            Assert.That(
                StrategyBuildingVariantProfile.NormalizeVariant(StrategyBuildTool.ScoutLodge, 12),
                Is.Zero);
            Assert.That(
                (int)StrategyBuildTool.ScoutLodge,
                Is.GreaterThan((int)StrategyBuildTool.Bridge));
        }

        [Test]
        public void ScoutActivitiesAreClassifiedAsExplorationWork()
        {
            Assert.That(
                StrategyResidentTaskState.GetFallbackKind(
                    StrategyResidentAgent.ResidentActivity.MovingToScoutFrontier),
                Is.EqualTo(StrategyResidentTaskKind.Exploration));
            Assert.That(
                StrategyResidentTaskState.GetFallbackKind(
                    StrategyResidentAgent.ResidentActivity.SurveyingFrontier),
                Is.EqualTo(StrategyResidentTaskKind.Exploration));

            StrategyResidentTaskState state = new StrategyResidentTaskState();
            state.SetActivity(StrategyResidentAgent.ResidentActivity.SurveyingFrontier);
            Assert.That(state.IsWork, Is.True);
        }

        [Test]
        public void ScoutActivitiesAreNotInterruptedByNightSchedule()
        {
            Assert.That(
                InvokeNightActivityRule(
                    "IsInterruptibleNightWorkActivity",
                    StrategyResidentAgent.ResidentActivity.SurveyingFrontier),
                Is.False);
            Assert.That(
                InvokeNightActivityRule(
                    "IsNightBlockedReachedActivity",
                    StrategyResidentAgent.ResidentActivity.MovingToScoutFrontier),
                Is.False);
            Assert.That(
                InvokeNightActivityRule(
                    "IsInterruptibleNightWorkActivity",
                    StrategyResidentAgent.ResidentActivity.WorkingGarden),
                Is.True);
        }

        private static bool InvokeNightActivityRule(
            string methodName,
            StrategyResidentAgent.ResidentActivity activity)
        {
            MethodInfo method = typeof(StrategyResidentAgent).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, methodName);
            return (bool)method.Invoke(null, new object[] { activity });
        }
    }
}
