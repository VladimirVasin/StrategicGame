using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    public enum StrategyResidentItemRewardRevealState
    {
        Hidden,
        Revealing,
        AwaitingConfirmation
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyResidentItemRewardRevealController : MonoBehaviour
    {
        private const string PauseReason = "ResidentItemRewardReveal";
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";
        private const float RevealDurationSeconds = 0.52f;

        private StrategyTimeScaleController timeScale;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;
        private StrategyResidentItemDefinition activeDefinition;
        private StrategyResidentAgent activeResident;
        private Action completedCallback;
        private GameObject previousSelectedObject;
        private float revealElapsed;
        private bool configured;
        private bool pauseHeld;
        private bool reducedMotion;

        public StrategyResidentItemRewardRevealState State { get; private set; }
        public bool IsOpen => State != StrategyResidentItemRewardRevealState.Hidden;
        public bool IsAwaitingConfirmation =>
            State == StrategyResidentItemRewardRevealState.AwaitingConfirmation;
        public string ActiveItemId => activeDefinition != null ? activeDefinition.Id : string.Empty;
        public string DisplayedTitle => titleText != null ? titleText.text : string.Empty;
        public string DisplayedDescription => descriptionText != null ? descriptionText.text : string.Empty;
        public bool HoldsInputContext => inputContext != null && !inputContext.IsDisposed;
        public bool HoldsPauseLock => pauseHeld;
        public bool IsConfirmInteractable => confirmButton != null && confirmButton.interactable;

        public void Configure(
            StrategyTimeScaleController timeScaleController,
            StrategyInputRouter router)
        {
            CloseWithoutCompletion();
            timeScale = timeScaleController;
            inputRouter = router;
            configured = timeScale != null && inputRouter != null;
            reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            EnsureView();
        }

        public bool TryShow(
            StrategyResidentItemDefinition definition,
            StrategyResidentAgent resident,
            Sprite artwork,
            string headline,
            Action onCompleted)
        {
            if (!configured
                || definition == null
                || resident == null
                || artwork == null
                || IsOpen
                || !isActiveAndEnabled
                || !inputRouter.IsAvailable
                || inputRouter.ActiveContextCount != 0
                || timeScale.IsPausedByLock)
            {
                return false;
            }

            EnsureView();
            inputContext = inputRouter.PushContext(
                this,
                StrategyInputChannel.All,
                StrategyCancelMode.Swallow);
            if (!HoldsInputContext)
            {
                return false;
            }

            timeScale.SetRequestedScale(1f);
            timeScale.PushPauseLock(PauseReason);
            pauseHeld = true;
            activeDefinition = definition;
            activeResident = resident;
            completedCallback = onCompleted;
            previousSelectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            Populate(definition, resident, artwork, headline);
            root.SetActive(true);
            revealElapsed = 0f;
            State = StrategyResidentItemRewardRevealState.Revealing;
            ApplyReveal(0f);
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
            }
            StrategyDebugLogger.Info(
                "ResidentInventory",
                "RewardRevealOpened",
                StrategyDebugLogger.F("itemId", definition.Id),
                StrategyDebugLogger.F("residentId", resident.ResidentId));
            return true;
        }

        public bool TryConfirm()
        {
            if (!IsAwaitingConfirmation)
            {
                return false;
            }

            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            }
            FinishPresentation();
            return true;
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (State != StrategyResidentItemRewardRevealState.Revealing)
            {
                return;
            }

            float duration = reducedMotion ? 0.08f : RevealDurationSeconds;
            revealElapsed += Mathf.Max(0f, unscaledDeltaTime);
            float progress = duration > 0f ? Mathf.Clamp01(revealElapsed / duration) : 1f;
            ApplyReveal(progress * progress * (3f - 2f * progress));
            if (progress < 1f)
            {
                return;
            }

            State = StrategyResidentItemRewardRevealState.AwaitingConfirmation;
            confirmButton.interactable = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
            }
        }

        private void FinishPresentation()
        {
            string itemId = ActiveItemId;
            int residentId = activeResident != null ? activeResident.ResidentId : 0;
            Action callback = completedCallback;
            completedCallback = null;
            ResetPresentation();
            ReleaseOwnership();
            StrategyDebugLogger.Info(
                "ResidentInventory",
                "RewardRevealCompleted",
                StrategyDebugLogger.F("itemId", itemId),
                StrategyDebugLogger.F("residentId", residentId));
            callback?.Invoke();
        }

        private void CloseWithoutCompletion()
        {
            completedCallback = null;
            ResetPresentation();
            ReleaseOwnership();
        }

        private void ResetPresentation()
        {
            State = StrategyResidentItemRewardRevealState.Hidden;
            activeDefinition = null;
            activeResident = null;
            revealElapsed = 0f;
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void ReleaseOwnership()
        {
            inputContext?.Dispose();
            inputContext = null;
            inputRouter?.ReleaseContexts(this);
            if (pauseHeld)
            {
                timeScale?.PopPauseLock(PauseReason);
                pauseHeld = false;
            }

            if (EventSystem.current != null
                && previousSelectedObject != null
                && previousSelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(previousSelectedObject);
            }

            previousSelectedObject = null;
        }

        private void OnDisable()
        {
            CloseWithoutCompletion();
        }

        private void OnDestroy()
        {
            CloseWithoutCompletion();
        }
    }
}
