using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyScoutAssignmentFlowTests
    {
        private readonly List<GameObject> roots = new();
        private readonly List<InputActionAsset> inputAssets = new();
        private EventSystem preExistingEventSystem;
        private EventSystem manuallyRegisteredEventSystem;

        [SetUp]
        public void SetUp()
        {
            preExistingEventSystem = Object.FindAnyObjectByType<EventSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (manuallyRegisteredEventSystem != null)
            {
                InvokeEventSystemLifecycle(manuallyRegisteredEventSystem, "OnDisable");
                manuallyRegisteredEventSystem = null;
            }

            for (int i = roots.Count - 1; i >= 0; i--)
            {
                if (roots[i] != null)
                {
                    Object.DestroyImmediate(roots[i]);
                }
            }

            roots.Clear();
            for (int i = 0; i < inputAssets.Count; i++)
            {
                if (inputAssets[i] != null)
                {
                    Object.DestroyImmediate(inputAssets[i]);
                }
            }

            inputAssets.Clear();
            if (preExistingEventSystem == null)
            {
                EventSystem createdEventSystem = Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    Object.DestroyImmediate(createdEventSystem.gameObject);
                }
            }
        }

        [Test]
        public void IntroductionAssignsTheExactSelectedAdultOnlyOnce()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent first = CreateResident(
                population,
                1,
                "Alfhild Alderborn",
                StrategyResidentLifeStage.Adult,
                28f);
            StrategyResidentAgent selected = CreateResident(
                population,
                2,
                "Ylva Wintermere",
                StrategyResidentLifeStage.Adult,
                24f);
            StrategyInputRouter router = CreateConfiguredRouter();
            StrategyScoutAssignmentDialogController dialog = CreateDialog(router);
            int assignments = 0;
            int deferrals = 0;

            dialog.Show(
                lodge,
                population,
                true,
                resident =>
                {
                    assignments++;
                    return lodge.AssignWorker(resident);
                },
                () => deferrals++);

            Button defer = FindButton(dialog, "DeferButton");
            Assert.That(defer.gameObject.activeSelf, Is.False);
            FindCandidateButton(dialog, selected.FullName).onClick.Invoke();
            Button confirm = FindButton(dialog, "ConfirmButton");
            confirm.onClick.Invoke();
            confirm.onClick.Invoke();

            Assert.That(assignments, Is.EqualTo(1));
            Assert.That(deferrals, Is.Zero);
            Assert.That(lodge.WorkerCount, Is.EqualTo(1));
            Assert.That(lodge.Workers[0], Is.SameAs(selected));
            Assert.That(selected.ScoutWorkplace, Is.SameAs(lodge));
            Assert.That(first.ScoutWorkplace, Is.Null);
        }

        [Test]
        public void DialogReassignsExactSelectedStonecutterAndCleansPreviousWorksite()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent selected = CreateResident(
                population, 10, "Yrsa Stormgard", StrategyResidentLifeStage.Adult, 23f);
            StrategyResidentAgent other = CreateResident(
                population, 11, "Einar Ashfield", StrategyResidentLifeStage.Adult, 25f);
            StrategyStonecutterCamp stonecutter = CreateStonecutterCamp(population);
            Assert.That(stonecutter.AssignWorker(selected), Is.True);
            Assert.That(lodge.CanAssignWorker(selected), Is.False);
            Assert.That(lodge.CanAppointWorker(selected), Is.True);

            StrategyScoutAssignmentDialogController dialog = CreateDialog(CreateConfiguredRouter());
            int assignments = 0;
            dialog.Show(
                lodge,
                population,
                true,
                resident =>
                {
                    assignments++;
                    return lodge.TryAppointWorker(resident);
                },
                null);

            Button selectedRow = FindCandidateButton(dialog, selected.FullName);
            Assert.That(selectedRow.interactable, Is.True);
            selectedRow.onClick.Invoke();
            FindButton(dialog, "ConfirmButton").onClick.Invoke();

            Assert.That(assignments, Is.EqualTo(1));
            Assert.That(stonecutter.WorkerCount, Is.Zero);
            Assert.That(selected.StoneWorkplace, Is.Null);
            Assert.That(lodge.WorkerCount, Is.EqualTo(1));
            Assert.That(lodge.Workers[0], Is.SameAs(selected));
            Assert.That(selected.ScoutWorkplace, Is.SameAs(lodge));
            Assert.That(other.ScoutWorkplace, Is.Null);
        }

        [Test]
        public void IntroductionKeepsThreeRandomHaulerBuilderCandidatesInSeparateRows()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyPopulationController population = CreatePopulation();
            StrategyResidentAgent first = CreateResident(
                population, 20, "Asta River", StrategyResidentLifeStage.Adult, 21f);
            StrategyResidentAgent second = CreateResident(
                population, 21, "Bryn Hill", StrategyResidentLifeStage.Adult, 22f);
            StrategyResidentAgent third = CreateResident(
                population, 22, "Cora Vale", StrategyResidentLifeStage.Adult, 23f);
            StrategyResidentAgent fourth = CreateResident(
                population, 23, "Dagr Pine", StrategyResidentLifeStage.Adult, 24f);
            Assert.That(first.AssignSettlementHaulerRole(), Is.True);
            Assert.That(second.AssignSettlementHaulerRole(), Is.True);
            Assert.That(third.AssignSettlementBuilderRole(), Is.True);
            Assert.That(fourth.AssignSettlementBuilderRole(), Is.True);

            StrategyScoutAssignmentDialogController dialog = CreateDialog(CreateConfiguredRouter());
            dialog.Show(lodge, population, true, lodge.TryAppointWorker, null);
            List<Button> rows = GetActiveCandidateRows(dialog);
            List<string> namesBeforeRefresh = GetCandidateNames(rows);
            Assert.That(rows, Has.Count.EqualTo(3));

            MethodInfo feedbackUpdate = typeof(StrategyUiButtonFeedback).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            HashSet<int> rowPositions = new();
            for (int i = 0; i < rows.Count; i++)
            {
                feedbackUpdate.Invoke(rows[i].GetComponent<StrategyUiButtonFeedback>(), null);
                rowPositions.Add(Mathf.RoundToInt(
                    ((RectTransform)rows[i].transform).anchoredPosition.y));
            }

            Assert.That(rowPositions, Has.Count.EqualTo(3));
            MethodInfo refresh = typeof(StrategyScoutAssignmentDialogController).GetMethod(
                "RefreshCandidates",
                BindingFlags.Instance | BindingFlags.NonPublic);
            refresh.Invoke(dialog, null);
            CollectionAssert.AreEqual(
                namesBeforeRefresh,
                GetCandidateNames(GetActiveCandidateRows(dialog)));
        }

        [Test]
        public void ChildCannotBeAssignedAndHasAnActionableReason()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyResidentAgent child = CreateResident(
                null,
                3,
                "Eira Frosthelm",
                StrategyResidentLifeStage.Child,
                9f);

            Assert.That(child.IsAdult, Is.False);
            Assert.That(lodge.CanAssignWorker(child), Is.False);
            Assert.That(lodge.AssignWorker(child), Is.False);
            Assert.That(
                lodge.GetAssignmentBlockReason(child),
                Is.EqualTo("Only adults can become Scouts"));
            Assert.That(child.ScoutWorkplace, Is.Null);
        }

        [Test]
        public void DialogOwnsOneSwallowContextAndReleasesItAfterCancelOrDisable()
        {
            StrategyScoutLodge lodge = CreateLodge();
            StrategyInputRouter router = CreateConfiguredRouter();
            StrategyScoutAssignmentDialogController dialog = CreateDialog(router);
            GameObject previousSelection = CreateRoot("Previous Selection");
            EventSystem eventSystem = EventSystem.current != null
                ? EventSystem.current
                : CreateRoot("Test Event System").AddComponent<EventSystem>();
            if (EventSystem.current == null)
            {
                InvokeEventSystemLifecycle(eventSystem, "OnEnable");
                manuallyRegisteredEventSystem = eventSystem;
            }

            EventSystem.current = eventSystem;
            eventSystem.SetSelectedGameObject(previousSelection);
            Assert.That(EventSystem.current, Is.SameAs(eventSystem));
            int cancelledRequests = 0;

            dialog.Show(lodge, null, false, _ => false, () => cancelledRequests++);

            Assert.That(EventSystem.current, Is.SameAs(eventSystem));
            Assert.That(router.ActiveContextCount, Is.EqualTo(1));
            Assert.That(router.BlockedChannels, Is.EqualTo(StrategyInputChannel.All));
            Assert.That(router.TopCancelMode, Is.EqualTo(StrategyCancelMode.Swallow));
            Button cancel = FindButton(dialog, "DeferButton");
            Assert.That(cancel.gameObject.activeSelf, Is.True);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(cancel.gameObject));
            cancel.onClick.Invoke();
            cancel.onClick.Invoke();
            CompleteClosingTransition(dialog);

            Assert.That(cancelledRequests, Is.EqualTo(1));
            Assert.That(dialog.IsOpen, Is.False);
            Assert.That(router.ActiveContextCount, Is.Zero);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(previousSelection));

            int disabledRequests = 0;
            dialog.Show(lodge, null, false, _ => false, () => disabledRequests++);
            Assert.That(router.ActiveContextCount, Is.EqualTo(1));
            InvokeDisableLifecycle(dialog);
            InvokeDisableLifecycle(dialog);

            Assert.That(disabledRequests, Is.EqualTo(1));
            Assert.That(router.ActiveContextCount, Is.Zero);
        }

        private StrategyScoutLodge CreateLodge()
        {
            GameObject root = CreateRoot("Test Scout Lodge");
            StrategyScoutLodge lodge = root.AddComponent<StrategyScoutLodge>();
            lodge.Configure(null, null, null, null);
            return lodge;
        }

        private StrategyPopulationController CreatePopulation()
        {
            return CreateRoot("Test Population").AddComponent<StrategyPopulationController>();
        }

        private StrategyStonecutterCamp CreateStonecutterCamp(
            StrategyPopulationController population)
        {
            StrategyStonecutterCamp camp =
                CreateRoot("Test Stonecutter Camp").AddComponent<StrategyStonecutterCamp>();
            camp.Configure(null, null, null, population);
            return camp;
        }

        private StrategyResidentAgent CreateResident(
            StrategyPopulationController population,
            int id,
            string fullName,
            StrategyResidentLifeStage lifeStage,
            float age)
        {
            Transform parent = population != null ? population.transform : null;
            GameObject root = new("Test Resident " + id);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
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
                age,
                lifeStage);
            if (population != null)
            {
                GetResidentList(population).Add(resident);
            }

            return resident;
        }

        private StrategyInputRouter CreateConfiguredRouter()
        {
            GameObject root = CreateRoot("Test Input Router");
            StrategyInputRouter router = root.AddComponent<StrategyInputRouter>();
            InputActionAsset projectAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(projectAsset, Is.Not.Null);
            InputActionAsset testAsset = Object.Instantiate(projectAsset);
            inputAssets.Add(testAsset);
            Assert.That(router.Configure(testAsset), Is.True, router.ConfigurationError);
            return router;
        }

        private StrategyScoutAssignmentDialogController CreateDialog(StrategyInputRouter router)
        {
            GameObject root = CreateRoot("Test Scout Assignment Dialog");
            StrategyScoutAssignmentDialogController dialog =
                root.AddComponent<StrategyScoutAssignmentDialogController>();
            dialog.SetInputRouter(router);
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

        private static Button FindButton(
            StrategyScoutAssignmentDialogController dialog,
            string name)
        {
            Button[] buttons = dialog.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == name)
                {
                    return buttons[i];
                }
            }

            Assert.Fail("Missing button: " + name);
            return null;
        }

        private static Button FindCandidateButton(
            StrategyScoutAssignmentDialogController dialog,
            string residentName)
        {
            Button[] buttons = dialog.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!buttons[i].name.StartsWith("Candidate_"))
                {
                    continue;
                }

                Text[] labels = buttons[i].GetComponentsInChildren<Text>(true);
                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    if (labels[labelIndex].text == residentName)
                    {
                        return buttons[i];
                    }
                }
            }

            Assert.Fail("Missing candidate row for: " + residentName);
            return null;
        }

        private static List<Button> GetActiveCandidateRows(
            StrategyScoutAssignmentDialogController dialog)
        {
            List<Button> rows = new();
            foreach (Button button in dialog.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Candidate_") && button.gameObject.activeSelf)
                {
                    rows.Add(button);
                }
            }

            return rows;
        }

        private static List<string> GetCandidateNames(List<Button> rows)
        {
            List<string> names = new();
            for (int i = 0; i < rows.Count; i++)
            {
                Text name = rows[i].transform.Find("Name")?.GetComponent<Text>();
                names.Add(name != null ? name.text : string.Empty);
            }

            return names;
        }

        private static void CompleteClosingTransition(
            StrategyScoutAssignmentDialogController dialog)
        {
            FieldInfo transitionField = typeof(StrategyScoutAssignmentDialogController).GetField(
                "panelTransition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(transitionField, Is.Not.Null);
            StrategyUiPanelTransition transition =
                (StrategyUiPanelTransition)transitionField.GetValue(dialog);
            Assert.That(transition, Is.Not.Null);
            transition.SetVisible(false, true);

            MethodInfo update = typeof(StrategyScoutAssignmentDialogController).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null);
            update.Invoke(dialog, null);
        }

        private static void InvokeDisableLifecycle(
            StrategyScoutAssignmentDialogController dialog)
        {
            MethodInfo disable = typeof(StrategyScoutAssignmentDialogController).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(disable, Is.Not.Null);
            disable.Invoke(dialog, null);
        }

        private static void InvokeEventSystemLifecycle(EventSystem eventSystem, string methodName)
        {
            MethodInfo lifecycle = typeof(EventSystem).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lifecycle, Is.Not.Null);
            lifecycle.Invoke(eventSystem, null);
        }
    }
}
