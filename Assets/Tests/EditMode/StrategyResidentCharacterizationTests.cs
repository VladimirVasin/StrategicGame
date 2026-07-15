using System;
using System.Reflection;
using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResidentCharacterizationTests
    {
        [Test]
        public void ActivityProfileCoversEveryKnownActivity()
        {
            Array activities = Enum.GetValues(typeof(StrategyResidentAgent.ResidentActivity));
            Assert.That(activities.Length, Is.EqualTo(StrategyResidentTaskState.ProfiledActivityCount));
            foreach (StrategyResidentAgent.ResidentActivity activity in activities)
            {
                Assert.That(
                    Enum.IsDefined(typeof(StrategyResidentTaskKind),
                        StrategyResidentTaskState.GetFallbackKind(activity)),
                    Is.True,
                    activity.ToString());
            }
        }

        [Test]
        public void StorageLogActivitiesAreLogistics()
        {
            StrategyResidentTaskState state = new();
            state.SetActivity(StrategyResidentAgent.ResidentActivity.MovingToStoragePickup);
            Assert.That(state.Kind, Is.EqualTo(StrategyResidentTaskKind.Logistics));
            Assert.That(state.IsLogistics, Is.True);
        }

        [Test]
        public void PlannedKindRemainsAuthoritativeUntilRest()
        {
            StrategyResidentTaskState state = new();
            state.SetActivity(StrategyResidentAgent.ResidentActivity.MovingToForage);
            state.BeginPlannedTask(StrategyResidentTaskKind.Extraction);
            state.SetActivity(StrategyResidentAgent.ResidentActivity.GatheringForage);
            Assert.That(state.Kind, Is.EqualTo(StrategyResidentTaskKind.Extraction));
            Assert.That(state.IsWork, Is.True);
            state.SetActivity(StrategyResidentAgent.ResidentActivity.Idle);
            Assert.That(state.Kind, Is.EqualTo(StrategyResidentTaskKind.Rest));
        }

        [TestCase(StrategyResidentAgent.ResidentActivity.StandingByAtSawmill)]
        [TestCase(StrategyResidentAgent.ResidentActivity.StandingByAtKiln)]
        [TestCase(StrategyResidentAgent.ResidentActivity.StandingByAtForge)]
        public void ProductionStandbyRemainsWork(StrategyResidentAgent.ResidentActivity activity)
        {
            StrategyResidentTaskState state = new();
            state.SetActivity(activity);

            Assert.That(state.Kind, Is.EqualTo(StrategyResidentTaskKind.Production));
            Assert.That(state.IsWork, Is.True);
        }

        [TestCase(StrategyResidentAgent.ResidentActivity.StandingByAtSawmill, true)]
        [TestCase(StrategyResidentAgent.ResidentActivity.StandingByAtKiln, true)]
        [TestCase(StrategyResidentAgent.ResidentActivity.StandingByAtForge, true)]
        [TestCase(StrategyResidentAgent.ResidentActivity.SawingLogs, false)]
        [TestCase(StrategyResidentAgent.ResidentActivity.FiringPottery, false)]
        [TestCase(StrategyResidentAgent.ResidentActivity.ForgingTools, false)]
        public void ProductionStandbyLeavesAtNightButActiveCyclesFinish(
            StrategyResidentAgent.ResidentActivity activity,
            bool expected)
        {
            MethodInfo method = typeof(StrategyResidentAgent).GetMethod(
                "IsInterruptibleNightWorkActivity",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { activity }), Is.EqualTo(expected));
        }

        [Test]
        public void TaskExecutionSeparatesPhasesAndResetsIdempotently()
        {
            StrategyResidentTaskExecution execution = new();
            int normalRuns = 0;
            int arrivalRuns = 0;
            execution.Register(
                StrategyResidentAgent.ResidentActivity.MovingToTree,
                StrategyResidentTaskExecutionPhase.Normal,
                () => normalRuns++);
            execution.Register(
                StrategyResidentAgent.ResidentActivity.MovingToTree,
                StrategyResidentTaskExecutionPhase.PathCompleted,
                () => arrivalRuns++);

            Assert.That(execution.TryExecute(
                StrategyResidentAgent.ResidentActivity.MovingToTree,
                StrategyResidentTaskExecutionPhase.Normal), Is.True);
            Assert.That(normalRuns, Is.EqualTo(1));
            Assert.That(arrivalRuns, Is.Zero);
            Assert.That(execution.TryExecute(
                StrategyResidentAgent.ResidentActivity.MovingToTree,
                StrategyResidentTaskExecutionPhase.PathCompleted), Is.True);
            Assert.That(arrivalRuns, Is.EqualTo(1));

            execution.RegisterPlannedTask(StrategyResidentTaskKind.Forestry, () => false);
            execution.Reset();
            Assert.That(execution.PlannedTaskCount, Is.Zero);
            Assert.That(execution.TryExecute(
                StrategyResidentAgent.ResidentActivity.MovingToTree,
                StrategyResidentTaskExecutionPhase.Normal), Is.False);
        }

        [Test]
        public void PlannedTaskReturnsTheFirstSuccessfulKindAndHonorsStop()
        {
            StrategyResidentTaskExecution execution = new();
            int attempts = 0;
            execution.RegisterPlannedTask(StrategyResidentTaskKind.Household, () => { attempts++; return false; });
            execution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, () => { attempts++; return true; });
            Assert.That(execution.TryStartPlannedTask(
                () => false,
                out StrategyResidentTaskKind kind), Is.True);
            Assert.That(kind, Is.EqualTo(StrategyResidentTaskKind.Extraction));
            Assert.That(attempts, Is.EqualTo(2));

            execution.Reset();
            attempts = 0;
            execution.RegisterPlannedTask(StrategyResidentTaskKind.Household, () => { attempts++; return false; });
            execution.RegisterPlannedTask(StrategyResidentTaskKind.Extraction, () => { attempts++; return true; });
            Assert.That(execution.TryStartPlannedTask(
                () => attempts >= 1,
                out _), Is.False);
            Assert.That(attempts, Is.EqualTo(1));
        }

        [Test]
        public void InventoryTracksEveryResourceAndKeepsReturnMetadataSeparate()
        {
            StrategyResidentInventory inventory = new();
            inventory.Logs = 1;
            inventory.Stone = 1;
            inventory.Iron = 1;
            inventory.Coal = 1;
            inventory.Clay = 1;
            inventory.Planks = 1;
            inventory.Pottery = 1;
            inventory.Tools = 1;
            inventory.Game = 1;
            inventory.Fish = 1;
            inventory.Forage = 1;
            inventory.ConstructionReturnResource = StrategyConstructionResourceKind.Stone;
            Assert.That(inventory.HasAnyResource, Is.True);
            inventory.ClearAmounts();
            Assert.That(inventory.HasAnyResource, Is.False);
            Assert.That(inventory.ConstructionReturnResource,
                Is.EqualTo(StrategyConstructionResourceKind.Stone));
            inventory.ClearConstructionReturn();
            Assert.That(inventory.ConstructionReturnResource,
                Is.EqualTo(StrategyConstructionResourceKind.None));
        }

        [Test]
        public void HouseholdCookingTaskOwnsItsTimerAndRationRequest()
        {
            StrategyResidentHouseholdCookingTask task = new();
            int requested = task.CalculateRequestedDishes(4f, 1f);
            Assert.That(requested, Is.GreaterThan(0));
            task.Begin(2f);
            Assert.That(task.IsRunning, Is.True);
            Assert.That(task.Tick(1f), Is.False);
            Assert.That(task.Tick(1f), Is.True);
            Assert.That(task.IsRunning, Is.False);
        }
    }
}
