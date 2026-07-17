using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyScoutExpeditionDialogTests
    {
        private readonly List<GameObject> roots = new();
        private EventSystem preExistingEventSystem;

        [SetUp]
        public void SetUp()
        {
            preExistingEventSystem = Object.FindAnyObjectByType<EventSystem>();
            StrategyScoutProvisionService.GetAvailableRations();
        }

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
            StrategyScoutProvisionService.GetAvailableRations();
            if (preExistingEventSystem == null)
            {
                EventSystem created = Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    Object.DestroyImmediate(created.gameObject);
                }
            }
        }

        [Test]
        public void DurationControlsClampAndConfirmPassesSelectedSevenDays()
        {
            StrategyScoutLodge lodge = CreateLodge(true);
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent resident = CreateResident(population, 1, "Eira Wayfinder");
            StrategyScoutAssignmentDialogController dialog = CreateDialog();
            StrategyResidentAgent confirmedResident = null;
            int confirmedDays = 0;

            dialog.Show(
                lodge,
                population,
                false,
                (selected, days) =>
                {
                    confirmedResident = selected;
                    confirmedDays = days;
                    return true;
                },
                null);
            FindCandidateButton(dialog, resident.FullName).onClick.Invoke();
            Button plus = FindButton(dialog, "DurationPlusButton");
            for (int i = 0; i < 10; i++)
            {
                plus.onClick.Invoke();
            }

            Assert.That(FindText(dialog, "DurationDays").text, Is.EqualTo("7 DAYS"));
            Assert.That(FindText(dialog, "RationSummary").text, Does.Contain("COST 7 RATIONS"));
            Button confirm = FindButton(dialog, "ConfirmButton");
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();

            Assert.That(confirmedResident, Is.SameAs(resident));
            Assert.That(confirmedDays, Is.EqualTo(7));
        }

        [Test]
        public void InsufficientFirstExpeditionDisablesConfirmAndOffersDecideLater()
        {
            StrategyScoutLodge lodge = CreateLodge(false);
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent resident = CreateResident(population, 2, "Sigrid Northroad");
            StrategyScoutAssignmentDialogController dialog = CreateDialog();
            int confirmations = 0;

            dialog.Show(
                lodge,
                population,
                true,
                (_, _) =>
                {
                    confirmations++;
                    return true;
                },
                null);
            FindCandidateButton(dialog, resident.FullName).onClick.Invoke();

            Assert.That(FindButton(dialog, "ConfirmButton").interactable, Is.False);
            Assert.That(FindButton(dialog, "DeferButton").gameObject.activeSelf, Is.True);
            Assert.That(FindText(dialog, "DurationWarning").text, Does.Contain("Not enough"));
            Assert.That(confirmations, Is.Zero);
        }

        [Test]
        public void CandidateThatBecomesStaleDisablesConfirmBeforeCallback()
        {
            StrategyScoutLodge lodge = CreateLodge(true);
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent resident = CreateResident(population, 5, "Freya Mistpath");
            StrategyScoutAssignmentDialogController dialog = CreateDialog();
            int confirmations = 0;
            dialog.Show(
                lodge,
                population,
                false,
                (_, _) =>
                {
                    confirmations++;
                    return true;
                },
                null);
            FindCandidateButton(dialog, resident.FullName).onClick.Invoke();
            Assert.That(lodge.AssignWorker(resident), Is.True);

            MethodInfo refresh = typeof(StrategyScoutAssignmentDialogController).GetMethod(
                "RefreshCandidates",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(refresh, Is.Not.Null);
            refresh.Invoke(dialog, null);

            Assert.That(FindButton(dialog, "ConfirmButton").interactable, Is.False);
            Assert.That(FindText(dialog, "ActionStatus").text, Does.Contain("no longer available"));
            Assert.That(confirmations, Is.Zero);
        }

        [Test]
        public void ReadyScoutUsesSameDurationFlowWithoutReassignment()
        {
            StrategyScoutLodge lodge = CreateLodge(true);
            StrategyResidentAgent resident = CreateResident(null, 3, "Astrid Fartrail");
            Assert.That(lodge.AssignWorker(resident), Is.True);
            StrategyScoutAssignmentDialogController dialog = CreateDialog();
            int confirmedDays = 0;

            dialog.ShowExpedition(
                lodge,
                resident,
                (selected, days) =>
                {
                    Assert.That(selected, Is.SameAs(resident));
                    confirmedDays = days;
                    return true;
                },
                null);
            FindButton(dialog, "DurationPlusButton").onClick.Invoke();
            FindButton(dialog, "DurationPlusButton").onClick.Invoke();
            Button confirm = FindButton(dialog, "ConfirmButton");

            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();
            Assert.That(confirmedDays, Is.EqualTo(3));
            Assert.That(lodge.Workers[0], Is.SameAs(resident));
        }

        [Test]
        public void SelectedLodgeStatusCopyCoversStatesAndLiveCountdown()
        {
            StrategyScoutLodge lodge = CreateLodge(true);
            StrategyResidentAgent resident = CreateResident(null, 4, "Runa Longstep");
            Assert.That(lodge.AssignWorker(resident), Is.True);
            MethodInfo formatter = typeof(StrategyWorldSelectionController).GetMethod(
                "GetScoutWorkerStatus",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(formatter, Is.Not.Null);

            Assert.That(
                (string)formatter.Invoke(null, new object[] { lodge, resident }),
                Does.Contain("ready"));
            float originalElapsed = StrategyDayNightCycleController.CurrentElapsedSeconds;
            try
            {
                Assert.That(lodge.TryStartExpedition(1), Is.True);
                Assert.That(lodge.ExpeditionState, Is.EqualTo(StrategyScoutExpeditionState.Exploring));
                string initialCountdown = (string)formatter.Invoke(
                    null,
                    new object[] { lodge, resident });
                Assert.That(initialCountdown, Does.Contain("exploring"));
                StrategyDayNightCycleController.RestoreElapsedSeconds(
                    originalElapsed + StrategyDayNightCycleController.DayLengthSeconds * 0.5f);
                string advancedCountdown = (string)formatter.Invoke(
                    null,
                    new object[] { lodge, resident });
                Assert.That(advancedCountdown, Is.Not.EqualTo(initialCountdown));
                Assert.That(advancedCountdown, Does.Contain("left"));

                Assert.That(lodge.RequestRecall(), Is.True);
                Assert.That(lodge.ExpeditionState, Is.EqualTo(StrategyScoutExpeditionState.Returning));
                Assert.That(
                    (string)formatter.Invoke(null, new object[] { lodge, resident }),
                    Does.Contain("returning"));
            }
            finally
            {
                StrategyDayNightCycleController.RestoreElapsedSeconds(originalElapsed);
            }
        }

        [Test]
        public void ScoutHudAppointmentAvailabilityIncludesAnEmployedAdult()
        {
            StrategyScoutLodge lodge = CreateLodge(true);
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent resident = CreateResident(
                population,
                6,
                "Solveig Stonehand");
            Assert.That(resident.AssignSettlementBuilderRole(), Is.True);

            StrategyWorldSelectionController selection = CreateRoot("Test Selection")
                .AddComponent<StrategyWorldSelectionController>();
            SetPrivateField(selection, "population", population);
            MethodInfo selectionCheck = typeof(StrategyWorldSelectionController).GetMethod(
                "HasAppointableScoutCandidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(selectionCheck, Is.Not.Null);
            Assert.That(selectionCheck.Invoke(selection, new object[] { lodge }), Is.True);

            StrategyProfessionHudController professions = CreateRoot("Test Professions")
                .AddComponent<StrategyProfessionHudController>();
            SetPrivateField(professions, "population", population);
            MethodInfo professionCount = typeof(StrategyProfessionHudController).GetMethod(
                "CountAppointableScoutCandidates",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(professionCount, Is.Not.Null);
            Assert.That(
                professionCount.Invoke(professions, new object[] { new[] { lodge } }),
                Is.EqualTo(1));
        }

        private StrategyScoutLodge CreateLodge(bool provideRations)
        {
            GameObject root = CreateRoot("Test Scout Lodge");
            StrategyScoutLodge lodge = root.AddComponent<StrategyScoutLodge>();
            lodge.Configure(null, null, null, null);
            if (provideRations)
            {
                StrategyResourceStore provisions = new();
                provisions.Bind(root, StrategyResourceStoreScope.Settlement);
                provisions.Add(StrategyResourceType.Dish, 10);
            }

            return lodge;
        }

        private StrategyPopulationController CreatePopulation()
        {
            return CreateRoot("Test Population").AddComponent<StrategyPopulationController>();
        }

        private StrategyResidentAgent CreateResident(
            StrategyPopulationController population,
            int id,
            string fullName)
        {
            GameObject root = new("Test Resident " + id);
            if (population != null)
            {
                root.transform.SetParent(population.transform, false);
            }
            else
            {
                roots.Add(root);
            }

            StrategyResidentAgent resident = root.AddComponent<StrategyResidentAgent>();
            resident.Configure(
                null,
                null,
                StrategyResidentGender.Female,
                id,
                fullName,
                Vector3.zero,
                null,
                Vector2Int.zero,
                Vector2Int.one,
                id,
                24f,
                StrategyResidentLifeStage.Adult);
            if (population != null)
            {
                GetResidentList(population).Add(resident);
            }

            return resident;
        }

        private StrategyScoutAssignmentDialogController CreateDialog()
        {
            StrategyScoutAssignmentDialogController dialog = CreateRoot("Test Dialog")
                .AddComponent<StrategyScoutAssignmentDialogController>();
            dialog.Configure();
            return dialog;
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new(name);
            roots.Add(root);
            return root;
        }

        private static List<StrategyResidentAgent> GetResidentList(
            StrategyPopulationController population)
        {
            FieldInfo field = typeof(StrategyPopulationController).GetField(
                "residents",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (List<StrategyResidentAgent>)field.GetValue(population);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static Button FindCandidateButton(
            StrategyScoutAssignmentDialogController dialog,
            string residentName)
        {
            Button[] buttons = dialog.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Text name = buttons[i].transform.Find("Name")?.GetComponent<Text>();
                if (name != null && name.text == residentName)
                {
                    return buttons[i];
                }
            }

            Assert.Fail("Candidate row not found for " + residentName);
            return null;
        }

        private static Button FindButton(
            StrategyScoutAssignmentDialogController dialog,
            string objectName)
        {
            Button[] buttons = dialog.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == objectName)
                {
                    return buttons[i];
                }
            }

            Assert.Fail("Button not found: " + objectName);
            return null;
        }

        private static Text FindText(
            StrategyScoutAssignmentDialogController dialog,
            string objectName)
        {
            Text[] texts = dialog.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == objectName)
                {
                    return texts[i];
                }
            }

            Assert.Fail("Text not found: " + objectName);
            return null;
        }
    }
}
