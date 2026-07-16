using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyScoutAssignmentDialogController : MonoBehaviour
    {
        private const float CandidateRefreshInterval = 0.25f;

        private readonly List<ScoutCandidate> candidates = new();
        private readonly List<StrategyResidentAgent> introductionCandidates = new();
        private readonly List<StrategyScoutAssignmentRowView> rowPool = new();

        private StrategyScoutLodge lodge;
        private StrategyPopulationController population;
        private Func<StrategyResidentAgent, bool> tryAssign;
        private Action deferredCallback;
        private StrategyResidentAgent selectedResident;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;
        private CanvasGroup rootGroup;
        private RectTransform board;
        private RectTransform contentRoot;
        private StrategyUiPanelTransition panelTransition;
        private Text candidateHeadingText;
        private Text titleText;
        private Text subtitleText;
        private Text storyText;
        private Text emptyText;
        private Text actionStatusText;
        private Text confirmLabel;
        private Button confirmButton;
        private Button deferButton;
        private Text deferLabel;
        private GameObject selectionBeforeOpen;
        private bool initialized;
        private bool introductionMode;
        private bool callbackResolved;
        private bool hasStoredSelection;
        private float refreshTimer;

        public bool IsOpen => panelTransition != null
            ? panelTransition.TargetVisible
            : rootGroup != null && rootGroup.blocksRaycasts;

        public bool IsInputShieldActive => (panelTransition != null
                ? panelTransition.IsInputShieldActive
                : rootGroup != null && rootGroup.blocksRaycasts)
            || inputContext != null && !inputContext.IsDisposed;

        public bool CanOpenWithoutStacking => inputRouter == null
            || !inputRouter.IsAvailable
            || inputRouter.ActiveContextCount == 0;

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputContext?.Dispose();
            inputContext = null;
            inputRouter = router;
            RefreshInputContext(ShouldHoldInputContext);
        }

        public void Configure()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            StrategyUiInputModuleBootstrap.Ensure();
            BuildUi();
        }

        public void Show(
            StrategyScoutLodge scoutLodge,
            StrategyPopulationController populationController,
            bool introduction,
            Func<StrategyResidentAgent, bool> assignmentHandler,
            Action onDeferred)
        {
            Configure();
            ResolvePreviousRequestIfNeeded();
            StorePreviousSelection();

            lodge = scoutLodge;
            population = populationController;
            introductionMode = introduction;
            tryAssign = assignmentHandler;
            deferredCallback = onDeferred;
            callbackResolved = false;
            selectedResident = null;
            refreshTimer = CandidateRefreshInterval;
            PrepareIntroductionCandidates();
            ApplyModeCopy();
            SetActionStatus(string.Empty, false);
            panelTransition.SetVisible(true);
            RefreshCandidates();
            RefreshInputContext(true);
            PlaySfx(StrategyHudSfxKind.Notify);
            StrategyDebugLogger.Info(
                "ScoutAssignment",
                "DialogOpened",
                StrategyDebugLogger.F("introduction", introduction),
                StrategyDebugLogger.F("eligible", CountEligibleCandidates()));
        }

        public void Hide()
        {
            Close(false, false);
        }

        public void Dismiss()
        {
            Close(true, true);
        }

        private bool ShouldHoldInputContext => IsOpen
            || panelTransition != null && panelTransition.IsInputShieldActive;

        private void Awake()
        {
            Configure();
        }

        private void Update()
        {
            RefreshInputContext(ShouldHoldInputContext);
            if (!ShouldHoldInputContext)
            {
                RestorePreviousSelection();
            }

            if (!IsOpen)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = CandidateRefreshInterval;
            RefreshCandidates();
        }

        private void OnDisable()
        {
            ResolveDeferredCallback();
            inputContext?.Dispose();
            inputContext = null;
            panelTransition?.SetVisible(false, true);
            RestorePreviousSelection();
            ClearRequestState();
        }

        private void StorePreviousSelection()
        {
            if (hasStoredSelection)
            {
                return;
            }

            selectionBeforeOpen = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            hasStoredSelection = true;
        }

        private void RestorePreviousSelection()
        {
            if (!hasStoredSelection)
            {
                return;
            }

            hasStoredSelection = false;
            if (EventSystem.current == null)
            {
                selectionBeforeOpen = null;
                return;
            }

            GameObject target = selectionBeforeOpen;
            selectionBeforeOpen = null;
            if (target != null)
            {
                target.GetComponent<StrategyUiButtonFeedback>()?.SuppressNextFocusCue();
            }

            EventSystem.current.SetSelectedGameObject(
                target != null && target.activeInHierarchy ? target : null);
        }

        private void RefreshInputContext(bool open)
        {
            if (!open || inputRouter == null || !inputRouter.IsAvailable)
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
                    StrategyCancelMode.Swallow);
            }
        }

        private void Close(bool invokeDeferred, bool playSfx)
        {
            if (!initialized)
            {
                return;
            }

            if (invokeDeferred)
            {
                ResolveDeferredCallback();
            }
            else
            {
                callbackResolved = true;
                deferredCallback = null;
            }

            panelTransition?.SetVisible(false);
            RefreshInputContext(ShouldHoldInputContext);
            ClearRequestState();
            if (playSfx)
            {
                PlaySfx(StrategyHudSfxKind.Cancel);
            }
        }

        private void ResolvePreviousRequestIfNeeded()
        {
            if (!callbackResolved && deferredCallback != null)
            {
                ResolveDeferredCallback();
            }
        }

        private void ResolveDeferredCallback()
        {
            if (callbackResolved)
            {
                return;
            }

            callbackResolved = true;
            Action callback = deferredCallback;
            deferredCallback = null;
            callback?.Invoke();
        }

        private void ClearRequestState()
        {
            lodge = null;
            population = null;
            tryAssign = null;
            selectedResident = null;
            candidates.Clear();
            introductionCandidates.Clear();
        }

        private static void PlaySfx(StrategyHudSfxKind kind)
        {
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(kind);
            }
        }

        private readonly struct ScoutCandidate
        {
            public ScoutCandidate(StrategyResidentAgent resident, bool eligible, string reason)
            {
                Resident = resident;
                Eligible = eligible;
                Reason = reason ?? string.Empty;
            }

            public StrategyResidentAgent Resident { get; }
            public bool Eligible { get; }
            public string Reason { get; }
        }
    }
}
