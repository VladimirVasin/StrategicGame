using System;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCombatHealthTests
    {
        [Test]
        public void ConstructorStartsAtMaximumByDefault()
        {
            StrategyCombatHealth health = new(100);

            Assert.That(health.Maximum, Is.EqualTo(100));
            Assert.That(health.Current, Is.EqualTo(100));
            Assert.That(health.IsAlive, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ConstructorRejectsNonPositiveMaximum(int maximum)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrategyCombatHealth(maximum));
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void ConstructorRejectsCurrentOutsideMaximum(int current)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrategyCombatHealth(100, current));
        }

        [Test]
        public void ApplyDamageReducesHealthAndReportsTransition()
        {
            StrategyCombatHealth health = new(100);

            StrategyCombatDamageResult result = health.ApplyDamage(30);

            Assert.That(result.Applied, Is.True);
            Assert.That(result.PreviousHealth, Is.EqualTo(100));
            Assert.That(result.CurrentHealth, Is.EqualTo(70));
            Assert.That(result.MaxHealth, Is.EqualTo(100));
            Assert.That(result.BecameDefeated, Is.False);
            Assert.That(result.DefeatPrevented, Is.False);
            Assert.That(health.Current, Is.EqualTo(70));
            Assert.That(health.IsAlive, Is.True);
        }

        [Test]
        public void OverkillClampsAtZeroAndDefeatsOnlyOnce()
        {
            StrategyCombatHealth health = new(100, 25);

            StrategyCombatDamageResult lethal = health.ApplyDamage(80);
            StrategyCombatDamageResult repeated = health.ApplyDamage(1);

            Assert.That(lethal.Applied, Is.True);
            Assert.That(lethal.PreviousHealth, Is.EqualTo(25));
            Assert.That(lethal.CurrentHealth, Is.Zero);
            Assert.That(lethal.BecameDefeated, Is.True);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(repeated.Applied, Is.False);
            Assert.That(repeated.PreviousHealth, Is.Zero);
            Assert.That(repeated.CurrentHealth, Is.Zero);
            Assert.That(repeated.BecameDefeated, Is.False);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void NonPositiveDamageIsRejectedWithoutMutation(int amount)
        {
            StrategyCombatHealth health = new(100, 70);

            StrategyCombatDamageResult result = health.ApplyDamage(amount);

            Assert.That(result.Applied, Is.False);
            Assert.That(result.PreviousHealth, Is.EqualTo(70));
            Assert.That(result.CurrentHealth, Is.EqualTo(70));
            Assert.That(health.Current, Is.EqualTo(70));
        }

        [Test]
        public void TryRestoreAcceptsBoundsAndRejectsInvalidValuesWithoutMutation()
        {
            StrategyCombatHealth health = new(100, 40);

            Assert.That(health.TryRestore(0), Is.True);
            Assert.That(health.Current, Is.Zero);
            Assert.That(health.TryRestore(100), Is.True);
            Assert.That(health.Current, Is.EqualTo(100));
            Assert.That(health.TryRestore(-1), Is.False);
            Assert.That(health.TryRestore(101), Is.False);
            Assert.That(health.Current, Is.EqualTo(100));
        }

        [Test]
        public void RulesAllowOnlyOpposingImplementedFactions()
        {
            Assert.That(
                StrategyCombatRules.AreHostile(
                    StrategyCombatFaction.Settlement,
                    StrategyCombatFaction.HostileWildlife),
                Is.True);
            Assert.That(
                StrategyCombatRules.AreHostile(
                    StrategyCombatFaction.HostileWildlife,
                    StrategyCombatFaction.Settlement),
                Is.True);
            Assert.That(
                StrategyCombatRules.AreHostile(
                    StrategyCombatFaction.Settlement,
                    StrategyCombatFaction.Settlement),
                Is.False);
            Assert.That(
                StrategyCombatRules.AreHostile(
                    StrategyCombatFaction.None,
                    StrategyCombatFaction.HostileWildlife),
                Is.False);
        }

        [Test]
        public void CanApplyDamageRequiresLivingHostileTargetAndPositiveAmount()
        {
            TestCombatant target = new(StrategyCombatFaction.HostileWildlife, 100);
            TestCombatant untargetable = new(
                StrategyCombatFaction.HostileWildlife,
                100,
                false);
            StrategyCombatDamage hostileDamage = new(
                this,
                StrategyCombatFaction.Settlement,
                40,
                StrategyCombatDamageKind.Piercing,
                new Vector3(2f, 3f, 0f));
            StrategyCombatDamage friendlyDamage = new(
                this,
                StrategyCombatFaction.HostileWildlife,
                40,
                StrategyCombatDamageKind.Bite,
                Vector3.zero);
            StrategyCombatDamage emptyDamage = new(
                this,
                StrategyCombatFaction.Settlement,
                0,
                StrategyCombatDamageKind.Piercing,
                Vector3.zero);

            Assert.That(StrategyCombatRules.CanApplyDamage(hostileDamage, target), Is.True);
            Assert.That(StrategyCombatRules.CanApplyDamage(hostileDamage, untargetable), Is.False);
            Assert.That(StrategyCombatRules.CanApplyDamage(friendlyDamage, target), Is.False);
            Assert.That(StrategyCombatRules.CanApplyDamage(emptyDamage, target), Is.False);

            target.Health.ApplyDamage(100);

            Assert.That(StrategyCombatRules.CanApplyDamage(hostileDamage, target), Is.False);
            Assert.That(StrategyCombatRules.CanApplyDamage(hostileDamage, null), Is.False);
        }

        [Test]
        public void DamageValuePreservesAttackContext()
        {
            object source = new();
            Vector3 hitWorld = new(4f, 5f, -0.1f);
            StrategyCombatDamage damage = new(
                source,
                StrategyCombatFaction.HostileWildlife,
                30,
                StrategyCombatDamageKind.Bite,
                hitWorld);

            Assert.That(damage.Source, Is.SameAs(source));
            Assert.That(damage.SourceFaction, Is.EqualTo(StrategyCombatFaction.HostileWildlife));
            Assert.That(damage.Amount, Is.EqualTo(30));
            Assert.That(damage.Kind, Is.EqualTo(StrategyCombatDamageKind.Bite));
            Assert.That(damage.HitWorld, Is.EqualTo(hitWorld));
        }

        private sealed class TestCombatant : IStrategyCombatant
        {
            public TestCombatant(
                StrategyCombatFaction faction,
                int maximumHealth,
                bool canBeCombatTargeted = true)
            {
                CombatFaction = faction;
                Health = new StrategyCombatHealth(maximumHealth);
                CanBeCombatTargeted = canBeCombatTargeted;
            }

            public StrategyCombatHealth Health { get; }
            public StrategyCombatFaction CombatFaction { get; }
            public bool IsCombatAlive => Health.IsAlive;
            public bool CanBeCombatTargeted { get; }
            public int CurrentCombatHealth => Health.Current;
            public int MaxCombatHealth => Health.Maximum;
            public Vector3 CombatWorldPosition => Vector3.zero;

            public bool TryGetCombatCell(out Vector2Int cell)
            {
                cell = Vector2Int.zero;
                return true;
            }

            public StrategyCombatDamageResult ReceiveCombatDamage(in StrategyCombatDamage damage)
            {
                return StrategyCombatRules.CanApplyDamage(damage, this)
                    ? Health.ApplyDamage(damage.Amount)
                    : StrategyCombatDamageResult.Rejected(Health.Current, Health.Maximum);
            }
        }
    }
}
