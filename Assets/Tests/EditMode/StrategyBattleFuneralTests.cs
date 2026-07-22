using System;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyBattleFuneralTests
    {
        private GameObject root;
        private StrategyBattleLifecycleController battle;
        private StrategyFuneralController funerals;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Battle Funeral Tests");
            battle = root.AddComponent<StrategyBattleLifecycleController>();
            funerals = root.AddComponent<StrategyFuneralController>();
            funerals.Configure(null, null);
            funerals.ConfigureBattleLifecycle(battle);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PeacefulDeathStartsFuneralImmediately()
        {
            funerals.NotifyResidentDeath(CreateSnapshot(1, "Peaceful Settler"));

            Assert.That(funerals.ActiveFuneralCount, Is.EqualTo(1));
            Assert.That(funerals.AwaitingBattleEndCount, Is.Zero);
            Assert.That(root.GetComponentInChildren<StrategyCorpse>(), Is.Not.Null);
        }

        [Test]
        public void ActiveAndSecuringBattleKeepCorpseWithoutStartingFuneral()
        {
            StrategyBattleThreatLease threat = battle.RegisterThreat(this, "Wolf attack");

            funerals.NotifyResidentDeath(CreateSnapshot(2, "Fallen Defender"));

            Assert.That(funerals.ActiveFuneralCount, Is.EqualTo(1));
            Assert.That(funerals.AwaitingBattleEndCount, Is.EqualTo(1));
            Assert.That(root.GetComponentInChildren<StrategyCorpse>(), Is.Not.Null);

            threat.Dispose();
            battle.Advance(StrategyBattleLifecycleController.SecuringDurationSeconds - 0.01f);

            Assert.That(battle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));
            Assert.That(funerals.AwaitingBattleEndCount, Is.EqualTo(1));

            battle.Advance(0.01f);

            Assert.That(battle.Phase, Is.EqualTo(StrategyBattlePhase.Peaceful));
            Assert.That(funerals.AwaitingBattleEndCount, Is.Zero);
        }

        [Test]
        public void NewBattleSuspendsExistingFuneralAndGroundsCorpse()
        {
            funerals.NotifyResidentDeath(CreateSnapshot(3, "Interrupted Settler"));
            StrategyCorpse corpse = root.GetComponentInChildren<StrategyCorpse>();
            Assert.That(corpse, Is.Not.Null);
            corpse.StartBurial();
            corpse.SetBurialProgress(0.5f);

            StrategyBattleThreatLease threat = battle.RegisterThreat(this, "Reinforcements");

            Assert.That(funerals.AwaitingBattleEndCount, Is.EqualTo(1));
            Assert.That(corpse.IsBurialStarted, Is.False);

            threat.Dispose();
            battle.Advance(StrategyBattleLifecycleController.SecuringDurationSeconds);

            Assert.That(funerals.AwaitingBattleEndCount, Is.Zero);
            Assert.That(corpse.IsBurialStarted, Is.False);
        }

        [Test]
        public void DeathDuringSecuringWaitsUntilPeaceful()
        {
            StrategyBattleThreatLease threat = battle.RegisterThreat(this, "Wolf attack");
            threat.Dispose();
            Assert.That(battle.Phase, Is.EqualTo(StrategyBattlePhase.Securing));

            funerals.NotifyResidentDeath(CreateSnapshot(4, "Late Casualty"));

            Assert.That(funerals.AwaitingBattleEndCount, Is.EqualTo(1));

            battle.Advance(StrategyBattleLifecycleController.SecuringDurationSeconds);

            Assert.That(battle.Phase, Is.EqualTo(StrategyBattlePhase.Peaceful));
            Assert.That(funerals.AwaitingBattleEndCount, Is.Zero);
        }

        private static StrategyResidentDeathSnapshot CreateSnapshot(int id, string name)
        {
            return new StrategyResidentDeathSnapshot(
                id,
                name,
                StrategyResidentGender.Male,
                StrategyResidentLifeStage.Adult,
                0,
                30,
                0,
                0,
                "TestFamily",
                new Vector3(2f, 3f, 0f),
                new Vector2Int(2, 3),
                Vector2Int.zero,
                "guard",
                "relative",
                Array.Empty<int>(),
                Array.Empty<int>());
        }
    }
}
