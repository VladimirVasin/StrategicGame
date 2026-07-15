using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyPointOfInterestPlacementTests
    {
        [Test]
        public void PlacementIsDeterministicAndRespectsMvpSpacing()
        {
            Vector2Int camp = new Vector2Int(48, 48);
            List<Vector2Int> first = StrategyPointOfInterestPlacement.SelectCells(
                96,
                96,
                481516,
                camp,
                StrategyPointOfInterestPlacement.DefaultPointCount,
                _ => true,
                _ => true);
            List<Vector2Int> second = StrategyPointOfInterestPlacement.SelectCells(
                96,
                96,
                481516,
                camp,
                StrategyPointOfInterestPlacement.DefaultPointCount,
                _ => true,
                _ => true);

            CollectionAssert.AreEqual(first, second);
            Assert.That(first, Has.Count.EqualTo(StrategyPointOfInterestPlacement.DefaultPointCount));
            Assert.That(new HashSet<Vector2Int>(first), Has.Count.EqualTo(first.Count));
            int campRadiusSquared = StrategyPointOfInterestPlacement.CampExclusionRadius
                * StrategyPointOfInterestPlacement.CampExclusionRadius;
            int spacingSquared = StrategyPointOfInterestPlacement.MinimumSpacing
                * StrategyPointOfInterestPlacement.MinimumSpacing;
            for (int i = 0; i < first.Count; i++)
            {
                Vector2Int cell = first[i];
                Assert.That(cell.x, Is.InRange(
                    StrategyPointOfInterestPlacement.EdgeMargin,
                    95 - StrategyPointOfInterestPlacement.EdgeMargin));
                Assert.That(cell.y, Is.InRange(
                    StrategyPointOfInterestPlacement.EdgeMargin,
                    95 - StrategyPointOfInterestPlacement.EdgeMargin));
                Assert.That((cell - camp).sqrMagnitude, Is.GreaterThanOrEqualTo(campRadiusSquared));
                for (int other = i + 1; other < first.Count; other++)
                {
                    Assert.That(
                        (cell - first[other]).sqrMagnitude,
                        Is.GreaterThanOrEqualTo(spacingSquared));
                }
            }
        }

        [Test]
        public void PlacementStaysInCampConnectedWalkableComponent()
        {
            List<Vector2Int> points = StrategyPointOfInterestPlacement.SelectCells(
                96,
                96,
                90210,
                new Vector2Int(20, 48),
                5,
                cell => cell.x != 50,
                cell => cell.x != 24);

            Assert.That(points, Is.Not.Empty);
            for (int i = 0; i < points.Count; i++)
            {
                Assert.That(points[i].x, Is.LessThan(50));
                Assert.That(points[i].x, Is.Not.EqualTo(24));
            }
        }

        [Test]
        public void PlacementReturnsEmptyWhenCampHasNoWalkableSeed()
        {
            List<Vector2Int> points = StrategyPointOfInterestPlacement.SelectCells(
                64,
                64,
                7,
                new Vector2Int(32, 32),
                5,
                _ => false,
                _ => true);

            Assert.That(points, Is.Empty);
        }
    }
}
