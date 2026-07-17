using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyScoutExpeditionStateTests
    {
        private readonly List<GameObject> roots = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                if (roots[i] != null)
                {
                    Object.DestroyImmediate(roots[i]);
                }
            }

            roots.Clear();
        }

        [Test]
        public void AssignmentAloneLeavesScoutReadyAndResidentInSettlement()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyResidentAgent resident = CreateResident(1);

            Assert.That(lodge.AssignWorker(resident), Is.True);

            Assert.That(lodge.ExpeditionState, Is.EqualTo(StrategyScoutExpeditionState.Ready));
            Assert.That(lodge.IsExploring, Is.False);
            Assert.That(lodge.IsReturning, Is.False);
            Assert.That(resident.IsOnScoutExpedition, Is.False);
            Assert.That(resident.IsScoutExploring, Is.False);
        }

        [Test]
        public void RestoreActiveExpeditionDoesNotChargeAndRecallIsIdempotent()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyResidentAgent resident = CreateResident(2);
            StrategyResourceStore provisions = new();
            provisions.Bind(lodge.gameObject, StrategyResourceStoreScope.Settlement);
            provisions.Add(StrategyResourceType.Dish, 4);
            int foodBefore = provisions.GetStored(StrategyResourceType.Dish);

            bool restored = lodge.RestorePersistentState(
                resident,
                StrategyScoutExpeditionState.Exploring,
                2,
                0f,
                1000000f,
                2f,
                0.25f,
                -1);

            Assert.That(restored, Is.True);
            Assert.That(provisions.GetStored(StrategyResourceType.Dish), Is.EqualTo(foodBefore));
            Assert.That(lodge.IsExploring, Is.True);
            Assert.That(resident.IsScoutExploring, Is.True);
            Assert.That(lodge.RequestRecall(), Is.True);
            Assert.That(lodge.RequestRecall(), Is.True);
            Assert.That(lodge.IsReturning, Is.True);
            Assert.That(resident.IsScoutReturning, Is.True);
            Assert.That(lodge.RemainingFieldRations, Is.Zero);
        }

        [Test]
        public void EmptyReadyRestorePreservesWholeUnitProvisionCredit()
        {
            StrategyScoutLodge lodge = CreateLodge();

            bool restored = lodge.RestorePersistentState(
                null,
                StrategyScoutExpeditionState.Ready,
                0,
                0f,
                0f,
                0f,
                0.75f,
                -1);

            Assert.That(restored, Is.True);
            Assert.That(lodge.WorkerCount, Is.Zero);
            Assert.That(lodge.ProvisionRationCredit, Is.EqualTo(0.75f));
            Assert.That(lodge.RequestRecall(), Is.False);
        }

        [Test]
        public void DurationExpiryAtNightDoesNotConsumeASecondFieldRation()
        {
            float originalElapsed = StrategyDayNightCycleController.CurrentElapsedSeconds;
            float dayLength = StrategyDayNightCycleController.DayLengthSeconds;
            float startElapsed = dayLength
                * (StrategyDayNightCycleController.NightStartPhase + 0.05f);
            try
            {
                StrategyDayNightCycleController.RestoreElapsedSeconds(startElapsed);
                StrategyScoutLodge lodge = CreateLodge();
                StrategyResidentAgent resident = CreateResident(3);
                StrategyResourceStore provisions = new();
                provisions.Bind(lodge.gameObject, StrategyResourceStoreScope.Settlement);
                provisions.Add(StrategyResourceType.Dish, 2);
                Assert.That(lodge.AssignWorker(resident), Is.True);
                Assert.That(lodge.TryStartExpedition(1), Is.True);

                InvokeLodgeUpdate(lodge);
                int firstProvisionDay = resident.LastNutritionDayIndex;
                Assert.That(
                    firstProvisionDay,
                    Is.EqualTo(StrategyDayNightCycleController.CurrentCalendarSnapshot.DayIndex));

                StrategyDayNightCycleController.RestoreElapsedSeconds(startElapsed + dayLength);
                InvokeLodgeUpdate(lodge);

                Assert.That(
                    lodge.ExpeditionState,
                    Is.EqualTo(StrategyScoutExpeditionState.Returning));
                Assert.That(resident.LastNutritionDayIndex, Is.EqualTo(firstProvisionDay));
            }
            finally
            {
                StrategyDayNightCycleController.RestoreElapsedSeconds(originalElapsed);
            }
        }

        [Test]
        public void AssignedScoutFinishesBlockingDutyBeforeProvisionsCanBeSpent()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyResidentAgent resident = CreateResident(4);
            StrategyResourceStore provisions = new();
            provisions.Bind(lodge.gameObject, StrategyResourceStoreScope.Settlement);
            provisions.Add(StrategyResourceType.Dish, 2);
            Assert.That(lodge.AssignWorker(resident), Is.True);
            SetPrivateField(
                resident,
                "activity",
                StrategyResidentAgent.ResidentActivity.MovingToNightLight);
            int storedBefore = provisions.GetStored(StrategyResourceType.Dish);

            Assert.That(lodge.CanDispatchScout(resident), Is.False);
            Assert.That(lodge.TryStartExpedition(1), Is.False);
            Assert.That(lodge.ExpeditionState, Is.EqualTo(StrategyScoutExpeditionState.Ready));
            Assert.That(provisions.GetStored(StrategyResourceType.Dish), Is.EqualTo(storedBefore));
        }

        private StrategyScoutLodge CreateLodge()
        {
            GameObject root = CreateRoot("Test Scout Lodge");
            StrategyScoutLodge lodge = root.AddComponent<StrategyScoutLodge>();
            lodge.Configure(null, null, null, null);
            return lodge;
        }

        private StrategyResidentAgent CreateResident(int id)
        {
            GameObject root = CreateRoot("Test Resident " + id);
            StrategyResidentAgent resident = root.AddComponent<StrategyResidentAgent>();
            resident.Configure(
                null,
                null,
                StrategyResidentGender.Female,
                id,
                "Scout " + id,
                Vector3.zero,
                null,
                Vector2Int.zero,
                Vector2Int.one,
                id,
                25f,
                StrategyResidentLifeStage.Adult);
            return resident;
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new(name);
            roots.Add(root);
            return root;
        }

        private static void InvokeLodgeUpdate(StrategyScoutLodge lodge)
        {
            MethodInfo update = typeof(StrategyScoutLodge).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(lodge, null);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            PropertyInfo property = target.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            property.SetValue(target, value);
        }
    }
}
