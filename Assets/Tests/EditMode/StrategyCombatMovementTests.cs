using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCombatMovementTests
    {
        [TestCase(2f, 0.38f, 0.25f, 0.25f)]
        [TestCase(0.5f, 0.38f, 1f, 0.12f)]
        [TestCase(0.38f, 0.38f, 1f, 0f)]
        [TestCase(0.2f, 0.38f, 1f, 0f)]
        [TestCase(2f, 0.38f, -1f, 0f)]
        public void ClampApproachTravelNeverCrossesStopDistance(
            float currentDistance,
            float stopDistance,
            float requestedTravel,
            float expectedTravel)
        {
            float travel = StrategyCombatRules.ClampApproachTravel(
                currentDistance,
                stopDistance,
                requestedTravel);

            Assert.That(travel, Is.EqualTo(expectedTravel).Within(0.0001f));
            Assert.That(
                currentDistance - travel,
                Is.GreaterThanOrEqualTo(Mathf.Min(currentDistance, stopDistance) - 0.0001f));
        }

        [Test]
        public void OversizedFrameStepStopsAtAttackReach()
        {
            Vector2 attacker = Vector2.zero;
            Vector2 target = Vector2.right;
            const float attackReach = 0.38f;

            float requestedTravel = 3.15f * 0.5f;
            float allowedTravel = StrategyCombatRules.ClampApproachTravel(
                Vector2.Distance(attacker, target),
                attackReach,
                requestedTravel);
            attacker = Vector2.MoveTowards(attacker, target, allowedTravel);

            Assert.That(
                Vector2.Distance(attacker, target),
                Is.EqualTo(attackReach).Within(0.0001f));
        }

        [Test]
        public void RepeatedRecoveryStepsHoldAtAttackReach()
        {
            Vector2 attacker = Vector2.zero;
            Vector2 target = Vector2.right;
            const float attackReach = 0.38f;

            for (int i = 0; i < 12; i++)
            {
                float allowedTravel = StrategyCombatRules.ClampApproachTravel(
                    Vector2.Distance(attacker, target),
                    attackReach,
                    0.2f);
                attacker = Vector2.MoveTowards(attacker, target, allowedTravel);
            }

            Assert.That(
                Vector2.Distance(attacker, target),
                Is.EqualTo(attackReach).Within(0.0001f));
        }

        [Test]
        public void InvalidDistancesFailClosed()
        {
            Assert.That(
                StrategyCombatRules.ClampApproachTravel(float.NaN, 0.38f, 1f),
                Is.Zero);
            Assert.That(
                StrategyCombatRules.ClampApproachTravel(1f, float.PositiveInfinity, 1f),
                Is.Zero);
            Assert.That(
                StrategyCombatRules.ClampApproachTravel(1f, 0.38f, float.NaN),
                Is.Zero);
        }

        [Test]
        public void MovingResidentStopsWhenTargetEntersShotRange()
        {
            GameObject residentObject = new("Combat movement resident");
            try
            {
                StrategyResidentAgent resident = residentObject.AddComponent<StrategyResidentAgent>();
                ConfigureMovingResident(resident, new TestCombatant(new Vector3(4f, 0f, 0f)));

                bool handled = InvokeCombatApproachCheck(resident);

                Assert.That(handled, Is.True);
                Assert.That(
                    resident.Activity,
                    Is.EqualTo(StrategyResidentAgent.ResidentActivity.AimingCombatBow));
            }
            finally
            {
                Object.DestroyImmediate(residentObject);
            }
        }

        [Test]
        public void MovingResidentKeepsApproachingTargetOutsideShotRange()
        {
            GameObject residentObject = new("Combat movement resident");
            try
            {
                StrategyResidentAgent resident = residentObject.AddComponent<StrategyResidentAgent>();
                ConfigureMovingResident(resident, new TestCombatant(new Vector3(5f, 0f, 0f)));

                bool handled = InvokeCombatApproachCheck(resident);

                Assert.That(handled, Is.False);
                Assert.That(
                    resident.Activity,
                    Is.EqualTo(StrategyResidentAgent.ResidentActivity.MovingToCombatRange));
            }
            finally
            {
                Object.DestroyImmediate(residentObject);
            }
        }

        private static void ConfigureMovingResident(
            StrategyResidentAgent resident,
            IStrategyCombatant target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo targetField = typeof(StrategyResidentAgent).GetField(
                "activeCombatTarget",
                flags);
            PropertyInfo activityProperty = typeof(StrategyResidentAgent).GetProperty(
                "activity",
                flags);

            Assert.That(targetField, Is.Not.Null);
            Assert.That(activityProperty, Is.Not.Null);
            targetField.SetValue(resident, target);
            activityProperty.SetValue(
                resident,
                StrategyResidentAgent.ResidentActivity.MovingToCombatRange);
        }

        private static bool InvokeCombatApproachCheck(StrategyResidentAgent resident)
        {
            MethodInfo method = typeof(StrategyResidentAgent).GetMethod(
                "TryHandleMovingToCombatRangeBeforeStep",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(resident, null);
        }

        private sealed class TestCombatant : IStrategyCombatant
        {
            public TestCombatant(Vector3 worldPosition)
            {
                CombatWorldPosition = worldPosition;
            }

            public StrategyCombatFaction CombatFaction =>
                StrategyCombatFaction.HostileWildlife;
            public bool IsCombatAlive => true;
            public bool CanBeCombatTargeted => true;
            public int CurrentCombatHealth => 100;
            public int MaxCombatHealth => 100;
            public Vector3 CombatWorldPosition { get; }

            public bool TryGetCombatCell(out Vector2Int cell)
            {
                cell = Vector2Int.zero;
                return true;
            }

            public StrategyCombatDamageResult ReceiveCombatDamage(
                in StrategyCombatDamage damage)
            {
                return StrategyCombatDamageResult.Rejected(100, 100);
            }
        }
    }
}
