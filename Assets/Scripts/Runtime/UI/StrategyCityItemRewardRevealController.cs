using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    public enum StrategyCityItemRewardRevealState
    {
        Hidden,
        Revealing,
        AwaitingConfirmation,
        FlyingToChest
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyCityItemRewardRevealController : MonoBehaviour
    {
        private const string PauseReason = "CityItemRewardReveal";
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";

        private StrategyTimeScaleController timeScale;
        private StrategyInputRouter inputRouter;
        private StrategyCityInventoryHudController inventoryHud;
        private StrategyInputContextHandle inputContext;
        private StrategyCityItemDefinition activeDefinition;
        private Sprite activeArtwork;
        private Action completedCallback;
        private GameObject previousSelectedObject;
        private bool configured;
        private bool pauseHeld;
        private bool reducedMotion;
        private bool completionDelivered;

        public StrategyCityItemRewardRevealState State { get; private set; } =
            StrategyCityItemRewardRevealState.Hidden;
        public bool IsOpen => State != StrategyCityItemRewardRevealState.Hidden;
        public bool IsAwaitingConfirmation =>
            State == StrategyCityItemRewardRevealState.AwaitingConfirmation;
        public bool HoldsInputContext => inputContext != null && !inputContext.IsDisposed;
        public bool HoldsPauseLock => pauseHeld;
        public bool ReducedMotion => reducedMotion;
        public bool IsConfirmInteractable => confirmButton != null && confirmButton.interactable;
        public string ActiveItemId => activeDefinition != null ? activeDefinition.Id : string.Empty;
        public string DisplayedTitle => rewardTitleText != null ? rewardTitleText.text : string.Empty;
        public string DisplayedEffect => rewardEffectText != null ? rewardEffectText.text : string.Empty;
        public Canvas RewardCanvas => rewardCanvas;
        public RectTransform CardRoot => cardRoot;

        public void Configure(
            StrategyTimeScaleController timeScaleController,
            StrategyInputRouter router,
            StrategyCityInventoryHudController cityInventoryHud)
        {
            if (IsOpen)
            {
                CloseWithoutCompletion();
            }

            timeScale = timeScaleController;
            inputRouter = router;
            inventoryHud = cityInventoryHud;
            configured = timeScale != null && inputRouter != null;
            reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            EnsureView();
            ApplyReducedMotionToView();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            ApplyReducedMotionToView();
            if (IsAwaitingConfirmation)
            {
                ApplyAwaitingPose();
            }
        }

        public bool TryShow(
            StrategyCityItemDefinition definition,
            Sprite artwork,
            Action onCompleted = null)
        {
            if (!configured
                || definition == null
                || IsOpen
                || !isActiveAndEnabled
                || inputRouter == null
                || !inputRouter.IsAvailable)
            {
                return false;
            }

            EnsureView();
            HoldInput();
            if (!HoldsInputContext)
            {
                return false;
            }

            timeScale.SetRequestedScale(1f);
            HoldPauseLock();
            activeDefinition = definition;
            activeArtwork = artwork;
            completedCallback = onCompleted;
            completionDelivered = false;
            previousSelectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            PopulateRewardView(definition, artwork);
            rewardCanvasRoot.SetActive(true);
            BeginRevealAnimation();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
            StrategyDebugLogger.Info(
                "CityInventory",
                "RewardRevealOpened",
                StrategyDebugLogger.F("itemId", definition.Id));
            return true;
        }

        public bool TryConfirm()
        {
            if (!IsAwaitingConfirmation)
            {
                return false;
            }

            BeginFlightAnimation();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            return true;
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        internal void Tick(float unscaledDeltaTime)
        {
            if (!IsOpen)
            {
                return;
            }

            AdvanceAnimation(Mathf.Max(0f, unscaledDeltaTime));
        }

        private void FinishPresentation()
        {
            if (!IsOpen || completionDelivered)
            {
                return;
            }

            completionDelivered = true;
            string itemId = ActiveItemId;
            Action callback = completedCallback;
            completedCallback = null;
            inventoryHud?.PlayRewardReceivedFeedback();
            ResetPresentationState();
            ReleaseInput();
            ReleasePauseLock();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
            StrategyDebugLogger.Info(
                "CityInventory",
                "RewardRevealCompleted",
                StrategyDebugLogger.F("itemId", itemId));
            callback?.Invoke();
        }

        private void CloseWithoutCompletion()
        {
            completedCallback = null;
            completionDelivered = false;
            ResetPresentationState();
            ReleaseInput();
            ReleasePauseLock();
        }

        private void ResetPresentationState()
        {
            State = StrategyCityItemRewardRevealState.Hidden;
            activeDefinition = null;
            activeArtwork = null;
            HideView();
            RestorePreviousSelection();
            ResetAnimationVisuals();
        }

        private void HoldInput()
        {
            if (inputContext == null
                && inputRouter != null
                && inputRouter.IsAvailable)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Swallow);
            }
        }

        private void ReleaseInput()
        {
            inputContext?.Dispose();
            inputContext = null;
        }

        private void HoldPauseLock()
        {
            if (pauseHeld || timeScale == null)
            {
                return;
            }

            timeScale.PushPauseLock(PauseReason);
            pauseHeld = true;
        }

        private void ReleasePauseLock()
        {
            if (!pauseHeld)
            {
                return;
            }

            timeScale?.PopPauseLock(PauseReason);
            pauseHeld = false;
        }

        private void RestorePreviousSelection()
        {
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
            if (rewardCanvasRoot != null)
            {
                rewardCanvasRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            CloseWithoutCompletion();
            DisposeView();
        }
    }
}
