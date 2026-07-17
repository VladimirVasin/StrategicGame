using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyFirstNightRatShotPlannerTests
    {
        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                Object.DestroyImmediate(objects[i]);
            }

            objects.Clear();
        }

        [Test]
        public void ChoosesClosestPrimaryResidentAndBuildsWalkableAxisAlignedDash()
        {
            StrategyResidentAgent near = CreateResident("Near", new Vector3(11f, 10f));
            StrategyResidentAgent far = CreateResident("Far", new Vector3(20f, 20f));
            Dictionary<StrategyResidentAgent, Vector2Int> cells = new()
            {
                [near] = new Vector2Int(11, 10),
                [far] = new Vector2Int(20, 20)
            };

            bool created = CreatePlan(
                new[] { far, near },
                cells,
                resident => true,
                resident => true,
                _ => true,
                out StrategyFirstNightRatShotPlan plan);

            Assert.That(created, Is.True);
            Assert.That(plan.Resident, Is.SameAs(near));
            int length = Mathf.Abs(plan.EndCell.x - plan.StartCell.x)
                + Mathf.Abs(plan.EndCell.y - plan.StartCell.y);
            Assert.That(length, Is.InRange(4, 6));
            Assert.That(
                plan.StartCell.x == plan.EndCell.x || plan.StartCell.y == plan.EndCell.y,
                Is.True);
            Assert.That(plan.RequiresResidentStaging, Is.False);
        }

        [Test]
        public void RevealFallbackStagesResidentOnNearbyWalkableCorridor()
        {
            StrategyResidentAgent hidden = CreateResident("Hidden", new Vector3(30f, 30f));
            Dictionary<StrategyResidentAgent, Vector2Int> cells = new()
            {
                [hidden] = new Vector2Int(30, 30)
            };

            bool created = CreatePlan(
                new[] { hidden },
                cells,
                _ => false,
                _ => true,
                cell => cell.y == 10 && cell.x >= 7 && cell.x <= 13,
                out StrategyFirstNightRatShotPlan plan);

            Assert.That(created, Is.True);
            Assert.That(plan.RequiresResidentStaging, Is.True);
            Assert.That(plan.PassCell.y, Is.EqualTo(10));
            Assert.That(plan.ResidentCell, Is.EqualTo(plan.PassCell));
        }

        [Test]
        public void FocusUsesLetterboxApertureAndRemainsWithinZoomLimits()
        {
            StrategyResidentAgent resident = CreateResident("Framed", new Vector3(10f, 10f));
            Dictionary<StrategyResidentAgent, Vector2Int> cells = new()
            {
                [resident] = new Vector2Int(10, 10)
            };

            bool created = CreatePlan(
                new[] { resident },
                cells,
                _ => true,
                _ => true,
                _ => true,
                out StrategyFirstNightRatShotPlan plan,
                16f / 9f);

            Assert.That(created, Is.True);
            Assert.That(
                plan.FocusOrthographicSize,
                Is.InRange(
                    StrategyFirstNightRatShotPlanner.MinimumFocusSize,
                    StrategyFirstNightRatShotPlanner.MaximumFocusSize));
            float apertureHeight = plan.FocusOrthographicSize * 2f
                * ((16f / 9f) / StrategyFirstNightRatShotPlanner.LetterboxAspect);
            float minY = Mathf.Min(plan.ResidentWorld.y, plan.StartWorld.y, plan.EndWorld.y);
            float maxY = Mathf.Max(plan.ResidentWorld.y, plan.StartWorld.y, plan.EndWorld.y);
            Assert.That(apertureHeight, Is.GreaterThan(maxY - minY));
        }

        private bool CreatePlan(
            IReadOnlyList<StrategyResidentAgent> residents,
            IReadOnlyDictionary<StrategyResidentAgent, Vector2Int> cells,
            System.Func<StrategyResidentAgent, bool> canParticipate,
            System.Func<StrategyResidentAgent, bool> canReveal,
            System.Func<Vector2Int, bool> isWalkable,
            out StrategyFirstNightRatShotPlan plan,
            float aspect = 16f / 9f)
        {
            return StrategyFirstNightRatShotPlanner.TryCreate(
                residents,
                new Vector2Int(10, 10),
                12345,
                1f,
                aspect,
                canParticipate,
                canReveal,
                resident => cells.TryGetValue(resident, out Vector2Int cell)
                    ? (Vector2Int?)cell
                    : null,
                resident => resident.transform.position,
                resident => resident.name == "Near" ? 1 : resident.name == "Far" ? 2 : 3,
                isWalkable,
                cell => new Vector3(cell.x, cell.y),
                out plan);
        }

        private StrategyResidentAgent CreateResident(string objectName, Vector3 position)
        {
            GameObject residentObject = new(objectName);
            objects.Add(residentObject);
            residentObject.transform.position = position;
            return residentObject.AddComponent<StrategyResidentAgent>();
        }
    }
}
