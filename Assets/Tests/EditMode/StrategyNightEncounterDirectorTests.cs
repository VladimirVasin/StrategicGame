using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyNightEncounterDirectorTests
    {
        [Test]
        public void InitializingDuringNightDoesNotQueueRetroactiveAttack()
        {
            StrategyNightEncounterSchedule schedule = new();

            schedule.Initialize(Snapshot(4, StrategyTimeOfDayPhase.Night));
            schedule.Observe(Snapshot(4, StrategyTimeOfDayPhase.Night), false);

            Assert.That(schedule.HasPending, Is.False);
        }

        [Test]
        public void AutomaticAttackSkipsFirstNightAndQueuesSecondNight()
        {
            StrategyNightEncounterSchedule schedule = new();
            schedule.Initialize(Snapshot(0, StrategyTimeOfDayPhase.Dusk));

            schedule.Observe(Snapshot(0, StrategyTimeOfDayPhase.Night), false);

            Assert.That(schedule.HasAutomaticPending, Is.False);

            schedule.Observe(Snapshot(1, StrategyTimeOfDayPhase.Dusk), false);
            schedule.Observe(Snapshot(1, StrategyTimeOfDayPhase.Night), false);

            Assert.That(schedule.HasAutomaticPending, Is.True);
        }

        [Test]
        public void BusyBattleAtTransitionSkipsAutomaticAttackForThatNight()
        {
            StrategyNightEncounterSchedule schedule = new();
            schedule.Initialize(Snapshot(2, StrategyTimeOfDayPhase.Dusk));

            schedule.Observe(Snapshot(2, StrategyTimeOfDayPhase.Night), true);
            schedule.Observe(Snapshot(2, StrategyTimeOfDayPhase.Night), false);

            Assert.That(schedule.HasAutomaticPending, Is.False);
        }

        [Test]
        public void GuaranteedRequestWaitsForNightAndAllSafetyBlocks()
        {
            StrategyNightEncounterSchedule schedule = new();
            schedule.Initialize(Snapshot(0, StrategyTimeOfDayPhase.Noon));
            schedule.ArmGuaranteed();

            Assert.That(
                schedule.CanAttempt(
                    Snapshot(0, StrategyTimeOfDayPhase.Dusk),
                    false,
                    false,
                    0f),
                Is.False);

            StrategyCalendarSnapshot night = Snapshot(0, StrategyTimeOfDayPhase.Night);
            schedule.Observe(night, false);

            Assert.That(schedule.CanAttempt(night, true, false, 0f), Is.False);
            Assert.That(schedule.CanAttempt(night, false, true, 0f), Is.False);
            Assert.That(schedule.CanAttempt(night, false, false, 0f), Is.True);
            Assert.That(schedule.HasGuaranteedPending, Is.True);
        }

        [Test]
        public void FailedAttemptRetriesAfterCooldownAndSuccessClearsRequest()
        {
            StrategyNightEncounterSchedule schedule = new();
            StrategyCalendarSnapshot night = Snapshot(2, StrategyTimeOfDayPhase.Night);
            schedule.Initialize(Snapshot(2, StrategyTimeOfDayPhase.Dusk));
            schedule.ArmGuaranteed();
            schedule.Observe(night, false);

            Assert.That(schedule.CanAttempt(night, false, false, 5f), Is.True);

            schedule.RecordAttempt(false, 5f);

            Assert.That(schedule.HasGuaranteedPending, Is.True);
            Assert.That(schedule.HasAutomaticPending, Is.True);
            Assert.That(schedule.CanAttempt(night, false, false, 5.99f), Is.False);
            Assert.That(schedule.CanAttempt(night, false, false, 6f), Is.True);

            schedule.RecordAttempt(true, 6f);
            schedule.Observe(night, false);

            Assert.That(schedule.HasPending, Is.False);
        }

        [Test]
        public void SuccessfulAutomaticAttackQueuesAtMostOncePerNight()
        {
            StrategyNightEncounterSchedule schedule = new();
            StrategyCalendarSnapshot firstNight = Snapshot(1, StrategyTimeOfDayPhase.Night);
            schedule.Initialize(Snapshot(1, StrategyTimeOfDayPhase.Dusk));
            schedule.Observe(firstNight, false);
            schedule.RecordAttempt(true, 0f);

            schedule.Observe(firstNight, false);

            Assert.That(schedule.HasPending, Is.False);

            schedule.Observe(Snapshot(2, StrategyTimeOfDayPhase.Dusk), false);
            schedule.Observe(Snapshot(2, StrategyTimeOfDayPhase.Night), false);

            Assert.That(schedule.HasAutomaticPending, Is.True);
        }

        [Test]
        public void DawnExpiresAutomaticRequestButPreservesGuaranteedRequest()
        {
            StrategyNightEncounterSchedule schedule = new();
            schedule.Initialize(Snapshot(1, StrategyTimeOfDayPhase.Dusk));
            schedule.ArmGuaranteed();
            schedule.Observe(Snapshot(1, StrategyTimeOfDayPhase.Night), false);

            schedule.Observe(Snapshot(2, StrategyTimeOfDayPhase.Dawn), false);

            Assert.That(schedule.HasAutomaticPending, Is.False);
            Assert.That(schedule.HasGuaranteedPending, Is.True);
            Assert.That(
                schedule.CanAttempt(
                    Snapshot(2, StrategyTimeOfDayPhase.Dawn),
                    false,
                    false,
                    10f),
                Is.False);
        }

        [Test]
        public void FirstNightStoryBlocksGuaranteedAttackUntilFullyResolved()
        {
            StrategyCalendarSnapshot dusk = Snapshot(0, StrategyTimeOfDayPhase.Dusk);
            StrategyCalendarSnapshot night = Snapshot(0, StrategyTimeOfDayPhase.Night);

            Assert.That(
                StrategyNightEncounterDirector.IsFirstNightSequenceBlocking(
                    dusk,
                    StrategyFirstNightFaunaStage.MiceVisible,
                    false,
                    false),
                Is.False);
            Assert.That(
                StrategyNightEncounterDirector.IsFirstNightSequenceBlocking(
                    night,
                    StrategyFirstNightFaunaStage.MiceVisible,
                    false,
                    false),
                Is.True);
            Assert.That(
                StrategyNightEncounterDirector.IsFirstNightSequenceBlocking(
                    night,
                    StrategyFirstNightFaunaStage.StoryCompleted,
                    false,
                    false),
                Is.False);
            Assert.That(
                StrategyNightEncounterDirector.IsFirstNightSequenceBlocking(
                    night,
                    StrategyFirstNightFaunaStage.StoryCompleted,
                    true,
                    false),
                Is.True);
            Assert.That(
                StrategyNightEncounterDirector.IsFirstNightSequenceBlocking(
                    night,
                    StrategyFirstNightFaunaStage.StoryCompleted,
                    false,
                    true),
                Is.True);
        }

        [Test]
        public void ObservingDawnDoesNotEndAnActiveBattleLease()
        {
            GameObject root = new("Night Encounter Dawn Test");
            try
            {
                StrategyBattleLifecycleController lifecycle =
                    root.AddComponent<StrategyBattleLifecycleController>();
                StrategyBattleThreatLease lease = lifecycle.RegisterThreat(
                    new object(),
                    "Wolf attack");
                StrategyNightEncounterSchedule schedule = new();
                schedule.Initialize(Snapshot(1, StrategyTimeOfDayPhase.Night));

                schedule.Observe(Snapshot(2, StrategyTimeOfDayPhase.Dawn), true);

                Assert.That(lifecycle.Phase, Is.EqualTo(StrategyBattlePhase.Active));
                Assert.That(lifecycle.ActiveThreatCount, Is.EqualTo(1));
                lease.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DebugNightApproachTargets2150WithoutRewinding()
        {
            const float dayLength = 360f;
            const float nightApproachOffset = 252.51f;
            float dayThreeNoon = 3f * dayLength + 105f;
            float dayThreeLateDusk = 3f * dayLength + 253.75f;
            float dayThreeNight = 3f * dayLength + 270f;

            Assert.That(
                StrategyDayNightCycleController
                    .CalculateDebugNightApproachElapsedSeconds(dayThreeNoon),
                Is.EqualTo(3f * dayLength + nightApproachOffset).Within(0.001f));
            Assert.That(
                StrategyDayNightCycleController
                    .CalculateDebugNightApproachElapsedSeconds(dayThreeLateDusk),
                Is.EqualTo(dayThreeLateDusk).Within(0.001f));
            Assert.That(
                StrategyDayNightCycleController
                    .CalculateDebugNightApproachElapsedSeconds(dayThreeNight),
                Is.EqualTo(dayThreeNight).Within(0.001f));
        }

        [Test]
        public void F9NightAttackButtonArmsRequestAndMovesClockTo2150()
        {
            float previousElapsedSeconds =
                StrategyDayNightCycleController.CurrentElapsedSeconds;
            GameObject root = new("F9 Night Attack Wiring Test");
            try
            {
                StrategyDayNightCycleController.RestoreElapsedSeconds(
                    3f * StrategyDayNightCycleController.DayLengthSeconds + 105f);
                GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(root.transform, false);

                StrategyInputRouter inputRouter = Add<StrategyInputRouter>(root, "Input Router");
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/InputSystem_Actions.inputactions");
                Assert.That(actions, Is.Not.Null);
                Assert.That(
                    inputRouter.Configure(actions),
                    Is.True,
                    inputRouter.ConfigurationError);

                StrategyBattleLifecycleController lifecycle =
                    Add<StrategyBattleLifecycleController>(root, "Battle Lifecycle");
                StrategyPopulationController population =
                    Add<StrategyPopulationController>(root, "Population");
                StrategyWildlifeController wildlife =
                    Add<StrategyWildlifeController>(root, "Wildlife");
                StrategyCombatEncounterController encounter =
                    Add<StrategyCombatEncounterController>(root, "Combat Encounter");
                encounter.Configure(population, wildlife);
                StrategyDayNightCycleController dayNight =
                    Add<StrategyDayNightCycleController>(root, "Day Night");
                StrategyNightEncounterDirector director =
                    Add<StrategyNightEncounterDirector>(root, "Night Director");
                SetPrivateField(director, "encounter", encounter);
                SetPrivateField(director, "battleLifecycle", lifecycle);
                SetPrivateField(director, "dayNight", dayNight);
                PropertyInfo configuredProperty =
                    typeof(StrategyNightEncounterDirector).GetProperty(
                        nameof(StrategyNightEncounterDirector.IsConfigured));
                Assert.That(configuredProperty, Is.Not.Null);
                configuredProperty.SetValue(director, true);

                StrategyDebugPanelController panel =
                    Add<StrategyDebugPanelController>(root, "Debug Panel");
                panel.SetInputRouter(inputRouter);
                panel.Configure(null, null);
                panel.ConfigureCombat(encounter, director);
                Button nightButton = null;
                foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == "StartNightCombatScenario")
                    {
                        nightButton = button;
                        break;
                    }
                }

                Assert.That(nightButton, Is.Not.Null);

                nightButton.onClick.Invoke();

                StrategyCalendarSnapshot snapshot =
                    StrategyDayNightCycleController.CurrentCalendarSnapshot;
                Assert.That(director.HasGuaranteedNightEncounterPending, Is.True);
                Assert.That(snapshot.DayIndex, Is.EqualTo(3));
                Assert.That(snapshot.Phase, Is.EqualTo(StrategyTimeOfDayPhase.Dusk));
                Assert.That(snapshot.Hour, Is.EqualTo(21));
                Assert.That(snapshot.Minute, Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(root);
                StrategyDayNightCycleController.RestoreElapsedSeconds(
                    previousElapsedSeconds);
            }
        }

        private static StrategyCalendarSnapshot Snapshot(
            int dayIndex,
            StrategyTimeOfDayPhase phase)
        {
            return new StrategyCalendarSnapshot(
                dayIndex,
                0f,
                phase == StrategyTimeOfDayPhase.Night ? 22 : 12,
                0,
                phase,
                0f,
                StrategySeason.Spring,
                1,
                1,
                0f);
        }

        private static T Add<T>(GameObject root, string name)
            where T : Component
        {
            GameObject child = new(name);
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }

        private static void SetPrivateField<T>(
            StrategyNightEncounterDirector director,
            string fieldName,
            T value)
        {
            FieldInfo field = typeof(StrategyNightEncounterDirector).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(director, value);
        }
    }
}
