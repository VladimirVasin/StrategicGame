using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyPointOfInterestMineralTests
    {
        private const int MapWidth = 192;
        private const int MapHeight = 192;

        private static readonly Vector2Int CampCell = new(96, 96);

        [TestCase(
            StrategyPointOfInterestResourceKind.Iron,
            "Iron Deposits Found",
            "Iron deposits were found near this point.\n\nA Mine can be built over the deposit.")]
        [TestCase(
            StrategyPointOfInterestResourceKind.Coal,
            "Coal Deposits Found",
            "Coal deposits were found near this point.\n\nA Coal Pit can be built over the deposit.")]
        [TestCase(
            StrategyPointOfInterestResourceKind.None,
            "Point Investigated",
            "No useful mineral deposits were found near this point.")]
        public void InvestigationMessageStatesTheDiscoveredResource(
            StrategyPointOfInterestResourceKind kind,
            string expectedTitle,
            string expectedResult)
        {
            Assert.That(
                StrategyPointOfInterestController.GetInvestigationTitle(kind),
                Is.EqualTo(expectedTitle));
            Assert.That(
                StrategyPointOfInterestController.GetInvestigationResult(kind),
                Is.EqualTo(expectedResult));
        }

        [TestCase(101)]
        [TestCase(74123)]
        [TestCase(481516)]
        public void OwnedMineralPlacementIsDeterministicCompleteAndAlternating(int seed)
        {
            List<StrategyPointOfInterestPlan> first = SelectPlans(seed, _ => true, _ => true);
            List<StrategyPointOfInterestPlan> second = SelectPlans(seed, _ => true, _ => true);

            Assert.That(first, Has.Count.EqualTo(StrategyPointOfInterestPlacement.DefaultPointCount));
            Assert.That(second, Has.Count.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Cell, Is.EqualTo(first[i].Cell));
                Assert.That(second[i].ResourceKind, Is.EqualTo(first[i].ResourceKind));
                Assert.That(second[i].HasMineralSite, Is.EqualTo(first[i].HasMineralSite));
                Assert.That(second[i].MineralOrigin, Is.EqualTo(first[i].MineralOrigin));
            }

            Assert.That(first[0].ResourceKind, Is.EqualTo(StrategyPointOfInterestResourceKind.None));
            Assert.That(first[0].HasMineralSite, Is.False);
            Assert.That(first[0].MineralOrigin, Is.EqualTo(Vector2Int.zero));
            Assert.That(CountKind(first, StrategyPointOfInterestResourceKind.None), Is.EqualTo(1));

            int coal = CountKind(first, StrategyPointOfInterestResourceKind.Coal);
            int iron = CountKind(first, StrategyPointOfInterestResourceKind.Iron);
            CollectionAssert.AreEquivalent(new[] { 4, 5 }, new[] { coal, iron });
            for (int i = 1; i < first.Count; i++)
            {
                Assert.That(first[i].HasMineralSite, Is.True, $"POI {i} has no owned mineral site.");
                if (i > 1)
                {
                    Assert.That(first[i].ResourceKind, Is.Not.EqualTo(first[i - 1].ResourceKind));
                }
            }

            AssertOwnedMineralConstraints(first, _ => true, _ => true);
        }

        [Test]
        public void ConstrainedMapNeverCreatesOrphanOrGlobalFallbackMinerals()
        {
            const int width = 64;
            const int height = 64;
            Vector2Int camp = new(32, 32);
            Func<Vector2Int, bool> walkable = _ => true;
            Func<Vector2Int, bool> buildable = cell => cell.y == camp.y;

            List<StrategyPointOfInterestPlan> plans = StrategyPointOfInterestPlacement.SelectMineralPlans(
                width,
                height,
                90210,
                camp,
                StrategyPointOfInterestPlacement.DefaultPointCount,
                walkable,
                buildable);

            Assert.That(plans, Has.Count.EqualTo(1));
            Assert.That(plans[0].ResourceKind, Is.EqualTo(StrategyPointOfInterestResourceKind.None));
            Assert.That(plans[0].HasMineralSite, Is.False);
            Assert.That(plans[0].MineralOrigin, Is.EqualTo(Vector2Int.zero));
        }

        private static List<StrategyPointOfInterestPlan> SelectPlans(
            int seed,
            Func<Vector2Int, bool> walkable,
            Func<Vector2Int, bool> buildable)
        {
            return StrategyPointOfInterestPlacement.SelectMineralPlans(
                MapWidth,
                MapHeight,
                seed,
                CampCell,
                StrategyPointOfInterestPlacement.DefaultPointCount,
                walkable,
                buildable);
        }

        private static void AssertOwnedMineralConstraints(
            IReadOnlyList<StrategyPointOfInterestPlan> plans,
            Func<Vector2Int, bool> walkable,
            Func<Vector2Int, bool> buildable)
        {
            HashSet<Vector2Int> pointCells = new();
            HashSet<Vector2Int> mineralOrigins = new();
            int spacingSquared = StrategyPointOfInterestPlacement.MinimumSpacing
                * StrategyPointOfInterestPlacement.MinimumSpacing;
            for (int i = 0; i < plans.Count; i++)
            {
                StrategyPointOfInterestPlan plan = plans[i];
                Assert.That(pointCells.Add(plan.Cell), Is.True, $"Duplicate POI cell {plan.Cell}.");
                for (int other = i + 1; other < plans.Count; other++)
                {
                    Assert.That(
                        (plan.Cell - plans[other].Cell).sqrMagnitude,
                        Is.GreaterThanOrEqualTo(spacingSquared),
                        $"POIs {i} and {other} violate minimum spacing.");
                }

                if (!plan.HasMineralSite)
                {
                    continue;
                }

                Assert.That(mineralOrigins.Add(plan.MineralOrigin), Is.True, $"Duplicate origin {plan.MineralOrigin}.");
                AssertMineralZone(plan, plans);
                AssertExtractionBlock(plan.MineralOrigin, walkable, buildable);
            }

            for (int i = 1; i < plans.Count; i++)
            {
                for (int other = i + 1; other < plans.Count; other++)
                {
                    Assert.That(
                        FootprintsTouchOrOverlap(plans[i].MineralOrigin, plans[other].MineralOrigin),
                        Is.False,
                        $"Mineral sites {i} and {other} touch or overlap.");
                }
            }
        }

        private static void AssertMineralZone(
            StrategyPointOfInterestPlan plan,
            IReadOnlyList<StrategyPointOfInterestPlan> plans)
        {
            int pointDistance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                plan.Cell,
                plan.MineralOrigin,
                StrategyPointOfInterestPlacement.MineralFootprint);
            Assert.That(pointDistance, Is.InRange(
                StrategyPointOfInterestPlacement.MineralPointMinDistance,
                StrategyPointOfInterestPlacement.MineralPointMaxDistance));

            int campDistance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                CampCell,
                plan.MineralOrigin,
                StrategyPointOfInterestPlacement.MineralFootprint);
            Assert.That(
                campDistance,
                Is.GreaterThan(StrategyPointOfInterestPlacement.CampMineralExclusionRadius));

            int neutralDistance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                plans[0].Cell,
                plan.MineralOrigin,
                StrategyPointOfInterestPlacement.MineralFootprint);
            Assert.That(neutralDistance, Is.GreaterThan(StrategyPointOfInterestPlacement.MineralFreeRadius));

            int owners = 0;
            for (int i = 1; i < plans.Count; i++)
            {
                StrategyPointOfInterestPlan candidate = plans[i];
                if (candidate.MineralOrigin == plan.MineralOrigin)
                {
                    owners++;
                    Assert.That(candidate.ResourceKind, Is.EqualTo(plan.ResourceKind));
                }
            }

            Assert.That(owners, Is.EqualTo(1), $"Mineral origin {plan.MineralOrigin} must have one owner.");
        }

        private static void AssertExtractionBlock(
            Vector2Int origin,
            Func<Vector2Int, bool> walkable,
            Func<Vector2Int, bool> buildable)
        {
            Vector2Int footprint = StrategyPointOfInterestPlacement.ExtractionBlockFootprint;
            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, y);
                    Assert.That(cell.x, Is.InRange(0, MapWidth - 1));
                    Assert.That(cell.y, Is.InRange(0, MapHeight - 1));
                    Assert.That(walkable(cell), Is.True, $"Extraction cell {cell} is not walkable.");
                    Assert.That(buildable(cell), Is.True, $"Extraction cell {cell} is not buildable.");
                }
            }
        }

        private static bool FootprintsTouchOrOverlap(Vector2Int left, Vector2Int right)
        {
            Vector2Int size = StrategyPointOfInterestPlacement.MineralFootprint;
            return left.x <= right.x + size.x
                && left.x + size.x >= right.x
                && left.y <= right.y + size.y
                && left.y + size.y >= right.y;
        }

        private static int CountKind(
            IReadOnlyList<StrategyPointOfInterestPlan> plans,
            StrategyPointOfInterestResourceKind kind)
        {
            int count = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].ResourceKind == kind)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
