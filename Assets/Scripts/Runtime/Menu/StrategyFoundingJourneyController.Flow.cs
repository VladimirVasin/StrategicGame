using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFoundingJourneyController
    {
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";
        private const float PreparedMapTimeoutSeconds = 90f;

        private enum JourneyPage
        {
            Story,
            Question,
            Summary,
            Launching
        }

        private JourneyPage page;
        private int storyIndex;
        private int questionIndex;
        private bool journeyViewConfigured;
        private bool reducedMotion;
        private string waterChoice = StrategyFoundingChoiceIds.WaterNoPreference;
        private string landscapeChoice = StrategyFoundingChoiceIds.LandscapeMixed;
        private string livelihoodChoice = StrategyFoundingChoiceIds.LivelihoodBalanced;
        private string priorityChoice = StrategyFoundingChoiceIds.PriorityBalanced;
        private StrategyInputContextHandle inputContext;
        private Coroutine launchRoutine;

        internal bool IsLaunching => page == JourneyPage.Launching;

        internal bool BeginBalancedSettlementForVerification()
        {
            if (!journeyViewConfigured || IsLaunching || launchRoutine != null)
            {
                return false;
            }

            UseBalancedDefaults();
            BeginSettlement();
            return launchRoutine != null;
        }

        private void ConfigureJourney()
        {
            if (journeyViewConfigured || preloader == null)
            {
                return;
            }

            journeyViewConfigured = true;
            reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            BuildJourneyView();
            BindJourneyActions();
            reducedMotionToggle.SetIsOnWithoutNotify(reducedMotion);
            RefreshInputContext();
            ShowStory(0, true);
        }

        private void Update()
        {
            if (!journeyViewConfigured)
            {
                return;
            }

            RefreshInputContext();
            RefreshPreparationStatus();
            UpdateBackgroundMotion();
            if (page == JourneyPage.Launching
                && preloader != null
                && !string.IsNullOrEmpty(preloader.LaunchFailureReason))
            {
                ShowLaunchFailure(preloader.LaunchFailureReason);
            }

            if (page != JourneyPage.Launching
                && inputRouter != null
                && inputRouter.TryConsumeCancel(this))
            {
                NavigateBack();
            }
        }

        private void BindJourneyActions()
        {
            backButton.onClick.AddListener(NavigateBack);
            nextButton.onClick.AddListener(AdvanceStory);
            skipStoryButton.onClick.AddListener(() => ShowQuestion(0));
            balancedDefaultsButton.onClick.AddListener(UseBalancedDefaults);
            changeAnswersButton.onClick.AddListener(() => ShowQuestion(0));
            beginButton.onClick.AddListener(BeginSettlement);
            reducedMotionToggle.onValueChanged.AddListener(SetReducedMotion);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int optionIndex = i;
                optionButtons[i].onClick.AddListener(() => SelectQuestionOption(optionIndex));
            }
        }

        private void AdvanceStory()
        {
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
            if (storyIndex + 1 < StrategyFoundingJourneyCatalog.StoryPanels.Length)
            {
                ShowStory(storyIndex + 1, false);
                return;
            }

            ShowQuestion(0);
        }

        private void NavigateBack()
        {
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
            switch (page)
            {
                case JourneyPage.Story when storyIndex > 0:
                    ShowStory(storyIndex - 1, false);
                    break;
                case JourneyPage.Story:
                    ReturnToMainMenu();
                    break;
                case JourneyPage.Question when questionIndex > 0:
                    ShowQuestion(questionIndex - 1);
                    break;
                case JourneyPage.Question:
                    ShowStory(StrategyFoundingJourneyCatalog.StoryPanels.Length - 1, false);
                    break;
                case JourneyPage.Summary:
                    ShowQuestion(StrategyFoundingJourneyCatalog.Questions.Length - 1);
                    break;
            }
        }

        private void ShowStory(int index, bool immediate)
        {
            page = JourneyPage.Story;
            storyIndex = Mathf.Clamp(index, 0, StrategyFoundingJourneyCatalog.StoryPanels.Length - 1);
            StrategyFoundingStoryPanel panel = StrategyFoundingJourneyCatalog.StoryPanels[storyIndex];
            SetJourneyBackground(panel, immediate);
            storyRoot.SetActive(true);
            questionRoot.SetActive(false);
            summaryRoot.SetActive(false);
            loadingRoot.SetActive(false);
            storyChapterText.text = panel.Chapter;
            storyTitleText.text = panel.Title;
            storyBodyText.text = panel.Body;
            storyProgressText.text = (storyIndex + 1) + " / " + StrategyFoundingJourneyCatalog.StoryPanels.Length;
            nextButtonLabel.text = storyIndex + 1 < StrategyFoundingJourneyCatalog.StoryPanels.Length
                ? "Continue"
                : "Choose our refuge";
            backButton.interactable = true;
            SelectUi(nextButton);
        }

        private void ShowQuestion(int index)
        {
            page = JourneyPage.Question;
            questionIndex = Mathf.Clamp(index, 0, StrategyFoundingJourneyCatalog.Questions.Length - 1);
            StrategyFoundingQuestion question = StrategyFoundingJourneyCatalog.Questions[questionIndex];
            StrategyFoundingStoryPanel council = StrategyFoundingJourneyCatalog.StoryPanels[^1];
            SetJourneyBackground(council, false);
            storyRoot.SetActive(false);
            questionRoot.SetActive(true);
            summaryRoot.SetActive(false);
            loadingRoot.SetActive(false);
            questionChapterText.text = "FOUNDING CHOICE  " + (questionIndex + 1) + " / " + StrategyFoundingJourneyCatalog.Questions.Length;
            questionTitleText.text = question.Prompt;
            questionContextText.text = question.Context;
            string selected = GetAnswer(question.Id);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                StrategyFoundingAnswerOption option = question.Options[i];
                optionLabels[i].text = option.Label;
                optionDescriptions[i].text = option.Description;
                optionIds[i] = option.Id;
                SetOptionSelected(i, option.Id == selected);
            }

            backButton.interactable = true;
            SelectUi(optionButtons[0]);
        }

        private void SelectQuestionOption(int optionIndex)
        {
            if (page != JourneyPage.Question
                || optionIndex < 0
                || optionIndex >= optionIds.Length)
            {
                return;
            }

            StrategyFoundingQuestion question = StrategyFoundingJourneyCatalog.Questions[questionIndex];
            SetAnswer(question.Id, optionIds[optionIndex]);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            if (questionIndex + 1 < StrategyFoundingJourneyCatalog.Questions.Length)
            {
                ShowQuestion(questionIndex + 1);
            }
            else
            {
                ShowSummary();
            }
        }

        private void UseBalancedDefaults()
        {
            StrategyFoundingPreferences balanced = StrategyFoundingPreferences.Balanced;
            waterChoice = balanced.WaterChoiceId;
            landscapeChoice = balanced.LandscapeChoiceId;
            livelihoodChoice = balanced.LivelihoodChoiceId;
            priorityChoice = balanced.PriorityChoiceId;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            ShowSummary();
        }

        private void ShowSummary()
        {
            page = JourneyPage.Summary;
            storyRoot.SetActive(false);
            questionRoot.SetActive(false);
            summaryRoot.SetActive(true);
            loadingRoot.SetActive(false);
            summaryBodyText.text = BuildSummaryText();
            summaryNoteText.text = waterChoice == StrategyFoundingChoiceIds.WaterInland
                && livelihoodChoice == StrategyFoundingChoiceIds.LivelihoodFishing
                ? "The scouts will compromise between dry ground and reachable fishing water."
                : "Safety remains the first rule. Preferences guide the best playable site.";
            backButton.interactable = true;
            SelectUi(beginButton);
        }

        private void BeginSettlement()
        {
            if (page == JourneyPage.Launching || launchRoutine != null)
            {
                return;
            }

            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            launchRoutine = StartCoroutine(ResolveStartAndLaunch());
        }

        private IEnumerator ResolveStartAndLaunch()
        {
            page = JourneyPage.Launching;
            storyRoot.SetActive(false);
            questionRoot.SetActive(false);
            summaryRoot.SetActive(false);
            loadingRoot.SetActive(true);
            backButton.interactable = false;
            SetControlsInteractable(false);

            CityMapController map;
            float mapWaitStarted = Time.realtimeSinceStartup;
            while (!TryGetPreparedMap(out map))
            {
                if (preloader == null)
                {
                    ShowLaunchFailure("The prepared valley was lost. Return to the main menu and try again.");
                    yield break;
                }

                if (Time.realtimeSinceStartup - mapWaitStarted >= PreparedMapTimeoutSeconds)
                {
                    StrategyDebugLogger.Error(
                        "FoundingJourney",
                        "PreparedMapTimeout",
                        StrategyDebugLogger.F("phase", PreloadPhase),
                        StrategyDebugLogger.F("stage", PreparationStage));
                    ShowLaunchFailure("Preparing the valley took too long. Return to the main menu and try again.");
                    yield break;
                }

                yield return null;
            }

            loadingTitleText.text = "Finding a safe place";
            loadingDetailText.text = "Comparing water, shelter and room to build";
            yield return null;

            StrategyFoundingPreferences preferences = CreatePreferences();
            StrategyStartSiteMapSnapshot snapshot;
            float snapshotStarted = Time.realtimeSinceStartup;
            try
            {
                snapshot = StrategyStartSiteMapSnapshot.Capture(map);
            }
            catch (System.Exception exception)
            {
                HandleStartSiteException("SnapshotFailed", exception);
                yield break;
            }

            float snapshotMs = (Time.realtimeSinceStartup - snapshotStarted) * 1000f;
            yield return null;
            StrategyStarterLayout layout;
            float selectionStarted = Time.realtimeSinceStartup;
            try
            {
                layout = StrategyStartSiteSelector.Select(snapshot, preferences);
            }
            catch (System.Exception exception)
            {
                HandleStartSiteException("SelectionFailed", exception);
                yield break;
            }

            float selectionMs = (Time.realtimeSinceStartup - selectionStarted) * 1000f;
            if (!layout.IsValid)
            {
                ShowLaunchFailure("No safe starting site could be resolved.");
                yield break;
            }

            StrategyFoundingStartState state = StrategyFoundingStartState.GetOrCreate(map);
            state.Configure(preferences, layout);
            StrategyDebugLogger.Info(
                "FoundingJourney",
                "StartSiteSelected",
                StrategyDebugLogger.F("seed", layout.MapSeed),
                StrategyDebugLogger.F("profile", layout.ProfileId),
                StrategyDebugLogger.F("campCell", layout.CampCell),
                StrategyDebugLogger.F("caravanOrigin", layout.CaravanOrigin),
                StrategyDebugLogger.F("score", layout.Diagnostics.Score),
                StrategyDebugLogger.F("fallback", layout.FallbackLevel),
                StrategyDebugLogger.F("snapshotMs", snapshotMs),
                StrategyDebugLogger.F("selectionMs", selectionMs));

            loadingTitleText.text = "Here we begin again";
            loadingDetailText.text = "Opening the valley";
            yield return new WaitForSecondsRealtime(reducedMotion ? 0.05f : 0.35f);
            if (!CompleteJourney())
            {
                ShowLaunchFailure("The settlement could not be opened.");
            }
        }

        private void HandleStartSiteException(string eventName, System.Exception exception)
        {
            StrategyDebugLogger.Error(
                "FoundingJourney",
                eventName,
                StrategyDebugLogger.F("exception", exception.GetType().Name),
                StrategyDebugLogger.F("message", exception.Message));
            ShowLaunchFailure("The scouts could not resolve a safe site. Return to the main menu and try again.");
        }

        private void ShowLaunchFailure(string message)
        {
            launchRoutine = null;
            page = JourneyPage.Summary;
            loadingRoot.SetActive(false);
            summaryRoot.SetActive(true);
            summaryNoteText.text = message;
            backButton.interactable = true;
            SetControlsInteractable(true);
            SelectUi(beginButton);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
        }

        private StrategyFoundingPreferences CreatePreferences()
        {
            bool balanced = waterChoice == StrategyFoundingChoiceIds.WaterNoPreference
                && landscapeChoice == StrategyFoundingChoiceIds.LandscapeMixed
                && livelihoodChoice == StrategyFoundingChoiceIds.LivelihoodBalanced
                && priorityChoice == StrategyFoundingChoiceIds.PriorityBalanced;
            return new StrategyFoundingPreferences(
                waterChoice,
                landscapeChoice,
                livelihoodChoice,
                priorityChoice,
                balanced
                    ? StrategyFoundingChoiceIds.BalancedProfile
                    : StrategyFoundingChoiceIds.CustomProfile);
        }

        private string BuildSummaryText()
        {
            return "WATER\n" + GetOptionLabel(StrategyFoundingChoiceIds.WaterQuestion, waterChoice)
                + "\n\nLANDSCAPE\n" + GetOptionLabel(StrategyFoundingChoiceIds.LandscapeQuestion, landscapeChoice)
                + "\n\nFIRST LIVELIHOOD\n" + GetOptionLabel(StrategyFoundingChoiceIds.LivelihoodQuestion, livelihoodChoice)
                + "\n\nPRIORITY\n" + GetOptionLabel(StrategyFoundingChoiceIds.PriorityQuestion, priorityChoice);
        }

        private static string GetOptionLabel(string questionId, string answerId)
        {
            StrategyFoundingQuestion[] questions = StrategyFoundingJourneyCatalog.Questions;
            for (int i = 0; i < questions.Length; i++)
            {
                if (questions[i].Id != questionId)
                {
                    continue;
                }

                for (int option = 0; option < questions[i].Options.Length; option++)
                {
                    if (questions[i].Options[option].Id == answerId)
                    {
                        return questions[i].Options[option].Label;
                    }
                }
            }

            return answerId == StrategyFoundingChoiceIds.WaterNoPreference
                || answerId == StrategyFoundingChoiceIds.LivelihoodBalanced
                ? "No strong preference"
                : "Balanced";
        }

        private string GetAnswer(string questionId)
        {
            return questionId switch
            {
                StrategyFoundingChoiceIds.WaterQuestion => waterChoice,
                StrategyFoundingChoiceIds.LandscapeQuestion => landscapeChoice,
                StrategyFoundingChoiceIds.LivelihoodQuestion => livelihoodChoice,
                StrategyFoundingChoiceIds.PriorityQuestion => priorityChoice,
                _ => string.Empty
            };
        }

        private void SetAnswer(string questionId, string answerId)
        {
            switch (questionId)
            {
                case StrategyFoundingChoiceIds.WaterQuestion:
                    waterChoice = answerId;
                    break;
                case StrategyFoundingChoiceIds.LandscapeQuestion:
                    landscapeChoice = answerId;
                    break;
                case StrategyFoundingChoiceIds.LivelihoodQuestion:
                    livelihoodChoice = answerId;
                    break;
                case StrategyFoundingChoiceIds.PriorityQuestion:
                    priorityChoice = answerId;
                    break;
            }
        }

        private void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            PlayerPrefs.SetInt(ReducedMotionKey, value ? 1 : 0);
            PlayerPrefs.Save();
            atmosphereController?.SetReducedMotion(value);
            if (value)
            {
                backgroundMotionRoot.anchoredPosition = Vector2.zero;
                backgroundMotionRoot.localScale = Vector3.one;
            }
        }

        private void RefreshInputContext()
        {
            if (inputRouter == null || !inputRouter.IsAvailable)
            {
                inputContext?.Dispose();
                inputContext = null;
                return;
            }

            if (inputContext == null || inputContext.IsDisposed)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Close);
            }
        }

        private void OnDisable()
        {
            inputContext?.Dispose();
            inputContext = null;
        }

        private static void SelectUi(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }
    }
}
