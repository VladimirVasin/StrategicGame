using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyBattleLifecycleControllerTests
    {
        private GameObject gameObject;
        private StrategyBattleLifecycleController lifecycle;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Battle Lifecycle Tests");
            lifecycle = gameObject.AddComponent<StrategyBattleLifecycleController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void StartsPeacefulWithoutThreats()
        {
            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Peaceful));
            Assert.That(lifecycle.ActiveThreatCount, Is.Zero);
            Assert.That(lifecycle.HasActiveThreats, Is.False);
            Assert.That(lifecycle.IsBattleInProgress, Is.False);
            Assert.That(lifecycle.SecuringSecondsRemaining, Is.Zero);
        }

        [Test]
        public void SavingIsAllowedOnlyWhenBattleLifecycleIsPeaceful()
        {
            Assert.That(StrategySaveSystem.IsWorldStateSaveable(null), Is.True);
            Assert.That(StrategySaveSystem.IsWorldStateSaveable(lifecycle), Is.True);

            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(
                new object(),
                "Wolf attack");

            Assert.That(StrategySaveSystem.IsWorldStateSaveable(lifecycle), Is.False);

            lease.Dispose();

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(StrategySaveSystem.IsWorldStateSaveable(lifecycle), Is.False);

            lifecycle.Advance(StrategyBattleLifecycleController.SecuringDurationSeconds);

            Assert.That(StrategySaveSystem.IsWorldStateSaveable(lifecycle), Is.True);
        }

        [Test]
        public void RegisteringThreatStartsBattleAndPreservesContext()
        {
            object owner = new();

            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(owner, " Wolf attack ");

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Active));
            Assert.That(lifecycle.ActiveThreatCount, Is.EqualTo(1));
            Assert.That(lifecycle.HasActiveThreats, Is.True);
            Assert.That(lifecycle.IsBattleInProgress, Is.True);
            Assert.That(lease.Owner, Is.SameAs(owner));
            Assert.That(lease.Reason, Is.EqualTo("Wolf attack"));
            Assert.That(lease.IsReleased, Is.False);
        }

        [Test]
        public void OverlappingThreatsMustAllReleaseBeforeSecuring()
        {
            StrategyBattleThreatLease first = lifecycle.RegisterThreat(new object(), "First wolf");
            StrategyBattleThreatLease second = lifecycle.RegisterThreat(new object(), "Second wolf");

            first.Dispose();

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Active));
            Assert.That(lifecycle.ActiveThreatCount, Is.EqualTo(1));

            second.Dispose();

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(lifecycle.ActiveThreatCount, Is.Zero);
            Assert.That(
                lifecycle.SecuringSecondsRemaining,
                Is.EqualTo(StrategyBattleLifecycleController.SecuringDurationSeconds));
        }

        [Test]
        public void LeaseReleaseIsIdempotent()
        {
            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(new object(), "Wolf attack");

            lease.Dispose();
            lease.Dispose();

            Assert.That(lease.IsReleased, Is.True);
            Assert.That(lease.Owner, Is.Null);
            Assert.That(lifecycle.ActiveThreatCount, Is.Zero);
            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
        }

        [Test]
        public void SecuringUsesScaledDeltaAndEndsAfterTwoAndHalfSeconds()
        {
            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(new object(), "Wolf attack");
            lease.Dispose();

            lifecycle.Advance(2.49f);

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(lifecycle.SecuringSecondsRemaining, Is.EqualTo(0.01f).Within(0.001f));

            lifecycle.Advance(0f);
            lifecycle.Advance(-1f);

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));

            lifecycle.Advance(0.01f);

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Peaceful));
            Assert.That(lifecycle.IsBattleInProgress, Is.False);
            Assert.That(lifecycle.SecuringSecondsRemaining, Is.Zero);
        }

        [Test]
        public void ThreatDuringSecuringReturnsToActiveAndRestartsFullDelayAfterRelease()
        {
            StrategyBattleThreatLease first = lifecycle.RegisterThreat(new object(), "First attack");
            first.Dispose();
            lifecycle.Advance(1f);

            StrategyBattleThreatLease second = lifecycle.RegisterThreat(new object(), "Reinforcements");

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Active));
            Assert.That(lifecycle.SecuringSecondsRemaining, Is.Zero);

            second.Dispose();

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(
                lifecycle.SecuringSecondsRemaining,
                Is.EqualTo(StrategyBattleLifecycleController.SecuringDurationSeconds));
        }

        [Test]
        public void PhaseChangedReportsOrderedTransitions()
        {
            List<(StrategyBattlePhase Previous, StrategyBattlePhase Current)> transitions = new();
            lifecycle.PhaseChanged += (previous, current) => transitions.Add((previous, current));

            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(new object(), "Wolf attack");
            lease.Dispose();
            lifecycle.Advance(StrategyBattleLifecycleController.SecuringDurationSeconds);

            Assert.That(
                transitions,
                Is.EqualTo(new[]
                {
                    (StrategyBattlePhase.Peaceful, StrategyBattlePhase.Active),
                    (StrategyBattlePhase.Active, StrategyBattlePhase.Securing),
                    (StrategyBattlePhase.Securing, StrategyBattlePhase.Peaceful)
                }));
        }

        [Test]
        public void DestroyCleanupInvalidatesLeasesWithoutPublishingBattleEnd()
        {
            int transitionCount = 0;
            lifecycle.PhaseChanged += (_, _) => transitionCount++;
            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(new object(), "Wolf attack");
            Assert.That(transitionCount, Is.EqualTo(1));

            lifecycle.CleanupForShutdown();

            Assert.That(lease.IsReleased, Is.True);
            Assert.That(lifecycle.ActiveThreatCount, Is.Zero);
            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Peaceful));
            Assert.That(transitionCount, Is.EqualTo(1));
            Assert.DoesNotThrow(lease.Dispose);
        }

        [Test]
        public void DisablingControllerPreservesExistingBattleUntilReenabled()
        {
            StrategyBattleThreatLease lease = lifecycle.RegisterThreat(
                new object(),
                "Wolf attack");

            lifecycle.enabled = false;

            Assert.That(lease.IsReleased, Is.False);
            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Active));
            Assert.Throws<InvalidOperationException>(
                () => lifecycle.RegisterThreat(new object(), "Reinforcements"));

            lifecycle.enabled = true;
            lease.Dispose();

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
        }

        [Test]
        public void WildlifeThreatReleasesOnlyWhenWolfIsActuallyRemoved()
        {
            StrategyWildlifeController wildlife =
                gameObject.AddComponent<StrategyWildlifeController>();
            wildlife.ConfigureBattleLifecycle(lifecycle);
            StrategyWolfAgent wolf = CreateWolf("Encounter Wolf");

            wildlife.RegisterCombatEncounterThreat(wolf);
            lifecycle.Advance(100f);

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Active));
            Assert.That(lifecycle.ActiveThreatCount, Is.EqualTo(1));

            wildlife.NotifyWolfRemoved(wolf, null);

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(lifecycle.ActiveThreatCount, Is.Zero);
        }

        [Test]
        public void WildlifeRebindsActiveThreatToReplacementLifecycle()
        {
            StrategyWildlifeController wildlife =
                gameObject.AddComponent<StrategyWildlifeController>();
            wildlife.ConfigureBattleLifecycle(lifecycle);
            StrategyWolfAgent wolf = CreateWolf("Rebound Wolf");
            wildlife.RegisterCombatEncounterThreat(wolf);
            GameObject replacementObject = new("Replacement Battle Lifecycle");
            replacementObject.transform.SetParent(gameObject.transform, false);
            StrategyBattleLifecycleController replacement =
                replacementObject.AddComponent<StrategyBattleLifecycleController>();

            wildlife.ConfigureBattleLifecycle(replacement);

            Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(replacement.Phase, Is.EqualTo(StrategyBattlePhase.Active));
            Assert.That(replacement.ActiveThreatCount, Is.EqualTo(1));

            wildlife.NotifyWolfRemoved(wolf, null);

            Assert.That(replacement.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(replacement.ActiveThreatCount, Is.Zero);
        }

        private StrategyWolfAgent CreateWolf(string name)
        {
            GameObject wolfObject = new(name);
            wolfObject.transform.SetParent(gameObject.transform, false);
            return wolfObject.AddComponent<StrategyWolfAgent>();
        }

        [Test]
        public void RegisterThreatRejectsMissingOwnerAndDisabledController()
        {
            Assert.Throws<ArgumentNullException>(() => lifecycle.RegisterThreat(null, "Wolf attack"));

            lifecycle.enabled = false;

            Assert.Throws<InvalidOperationException>(
                () => lifecycle.RegisterThreat(new object(), "Wolf attack"));
        }
    }
}
