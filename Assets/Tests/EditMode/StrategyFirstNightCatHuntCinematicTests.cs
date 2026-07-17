using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyFirstNightCatHuntCinematicTests
    {
        [Test]
        public void ShotPlannerIsDeterministicAndUsesOnlyWalkableCells()
        {
            Vector2Int anchor = new(12, 9);
            const int Seed = 74123;

            bool firstResult = StrategyFirstNightCatHuntShotPlanner.TryCreate(
                anchor,
                Seed,
                cell => cell.x >= 1 && cell.x < 24 && cell.y >= 1 && cell.y < 18,
                cell => new Vector3(cell.x + 0.5f, cell.y + 0.5f),
                out StrategyFirstNightCatHuntShotPlan first);
            bool secondResult = StrategyFirstNightCatHuntShotPlanner.TryCreate(
                anchor,
                Seed,
                cell => cell.x >= 1 && cell.x < 24 && cell.y >= 1 && cell.y < 18,
                cell => new Vector3(cell.x + 0.5f, cell.y + 0.5f),
                out StrategyFirstNightCatHuntShotPlan second);

            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.True);
            Assert.That(second.StartCell, Is.EqualTo(first.StartCell));
            Assert.That(second.EndCell, Is.EqualTo(first.EndCell));
            Assert.That(second.Direction, Is.EqualTo(first.Direction));
            Assert.That(second.Coat, Is.EqualTo(first.Coat));
            Assert.That(second.MouseVariant, Is.EqualTo(first.MouseVariant));

            Vector2Int cursor = first.StartCell;
            int steps = 0;
            while (cursor != first.EndCell)
            {
                Assert.That(
                    cursor.x >= 1 && cursor.x < 24 && cursor.y >= 1 && cursor.y < 18,
                    Is.True);
                cursor += first.Direction;
                steps++;
                Assert.That(steps, Is.LessThanOrEqualTo(6));
            }

            Assert.That(steps, Is.InRange(4, 6));
            Assert.That(first.CatStartWorld.z, Is.EqualTo(-0.073f).Within(0.0001f));
            Assert.That(first.MouseStartWorld.z, Is.EqualTo(-0.071f).Within(0.0001f));
        }

        [Test]
        public void ShotPlannerFindsAStableNearbyFallbackCorridor()
        {
            Vector2Int anchor = new(8, 8);
            bool Result(Vector2Int cell)
            {
                return cell.y == 10 && cell.x >= 5 && cell.x <= 11;
            }

            bool created = StrategyFirstNightCatHuntShotPlanner.TryCreate(
                anchor,
                99,
                Result,
                cell => new Vector3(cell.x, cell.y),
                out StrategyFirstNightCatHuntShotPlan plan);

            Assert.That(created, Is.True);
            Assert.That(plan.StartCell.y, Is.EqualTo(10));
            Assert.That(plan.EndCell.y, Is.EqualTo(10));
            Assert.That(Vector2Int.Distance(anchor, plan.StartCell), Is.LessThanOrEqualTo(6f));
        }

        [Test]
        public void MissingMapNeverCreatesActorsAndCleanupIsIdempotent()
        {
            GameObject root = new("Cat Hunt Lifecycle Test");
            try
            {
                StrategyFirstNightCatHuntCinematic cinematic = new(null, null, root.transform);

                Assert.That(cinematic.TryPrepare(out _), Is.False);
                Assert.That(cinematic.Phase, Is.EqualTo(StrategyFirstNightCatHuntPhase.Unprepared));
                Assert.That(cinematic.ActiveCat, Is.Null);
                Assert.That(cinematic.ActiveMouse, Is.Null);

                cinematic.Cleanup(null, StrategyInGameCinematicResult.Failed);
                cinematic.Cleanup(null, StrategyInGameCinematicResult.Cancelled);

                Assert.That(cinematic.Phase, Is.EqualTo(StrategyFirstNightCatHuntPhase.Cleaned));
                Assert.That(cinematic.ActiveCat, Is.Null);
                Assert.That(cinematic.ActiveMouse, Is.Null);
                Assert.That(root.transform.childCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BeginUsesStandardActorsAndCancelledCleanupRemovesEveryTransient()
        {
            GameObject root = new("Cat Hunt Actor Test");
            try
            {
                StrategyFirstNightCatHuntShotPlan shot = new(
                    new Vector2Int(2, 4),
                    new Vector2Int(7, 4),
                    Vector2Int.right,
                    new Vector3(2.4f, 4.5f, -0.073f),
                    new Vector3(4.9f, 4.5f, -0.073f),
                    new Vector3(4.75f, 4.5f, -0.071f),
                    new Vector3(5.6f, 4.5f, -0.071f),
                    new Vector3(6.3f, 4.5f, -0.072f),
                    StrategyCatCoat.GrayTabby,
                    1);
                StrategyFirstNightCatHuntCinematic cinematic = new(null, null, root.transform);
                SetPrivateField(cinematic, "shot", shot);
                SetPrivateField(cinematic, "prepared", true);

                cinematic.Begin(null);

                Assert.That(cinematic.Phase, Is.EqualTo(StrategyFirstNightCatHuntPhase.Staged));
                Assert.That(cinematic.HasVisibleActors, Is.True);
                Assert.That(cinematic.AreBothActorsVisible, Is.True);
                Assert.That(cinematic.ActiveCat, Is.Not.Null);
                Assert.That(cinematic.ActiveMouse, Is.Not.Null);
                Assert.That(cinematic.CatHighlight, Is.Not.Null);
                Assert.That(cinematic.CatHighlight.IsVisible, Is.True);
                Assert.That(cinematic.MouseHighlight, Is.Not.Null);
                Assert.That(cinematic.MouseHighlight.IsVisible, Is.True);
                Assert.That(
                    cinematic.ActiveCat.Renderer.sprite,
                    Is.SameAs(StrategySettlementFaunaSpriteFactory.GetCatSprite(
                        StrategyCatCoat.GrayTabby,
                        StrategyCatSpritePose.Idle,
                        0)));
                Assert.That(
                    cinematic.ActiveMouse.Renderer.sprite,
                    Is.SameAs(StrategySettlementFaunaSpriteFactory.GetMouseSprite(1)));
                Assert.That(
                    cinematic.ActiveCat.transform.localScale,
                    Is.EqualTo(Vector3.one * StrategySettlementFaunaSpriteFactory.CatWorldScale));
                Assert.That(
                    cinematic.ActiveMouse.transform.localScale,
                    Is.EqualTo(Vector3.one * StrategySettlementFaunaSpriteFactory.MouseWorldScale));

                cinematic.Cleanup(null, StrategyInGameCinematicResult.Cancelled);
                cinematic.Cleanup(null, StrategyInGameCinematicResult.Cancelled);

                Assert.That(cinematic.Phase, Is.EqualTo(StrategyFirstNightCatHuntPhase.Cleaned));
                Assert.That(cinematic.HasVisibleActors, Is.False);
                Assert.That(cinematic.ActiveCat, Is.Null);
                Assert.That(cinematic.ActiveMouse, Is.Null);
                Assert.That(cinematic.CatHighlight, Is.Null);
                Assert.That(cinematic.MouseHighlight, Is.Null);
                Assert.That(root.transform.childCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
