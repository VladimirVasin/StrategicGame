using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyFirstNightFaunaStoryController : MonoBehaviour
    {
        private const string PauseReason = "FirstNightFaunaStory";

        private StrategyTimeScaleController timeScale;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;
        private Coroutine preloadRoutine;
        private Sprite[] preloadedArtwork;
        private Action completedCallback;
        private int frameIndex;
        private bool configured;
        private bool pauseHeld;
        private bool isOpen;
        private bool storyResolved;
        private bool completionDelivered;

        public bool IsOpen => isOpen;

        public bool CanOpenWithoutStacking => configured
            && isActiveAndEnabled
            && !isOpen
            && !storyResolved
            && inputRouter != null
            && inputRouter.IsAvailable
            && inputRouter.ActiveContextCount == 0
            && (timeScale == null || !timeScale.IsPausedByLock);

        public void Configure(
            StrategyTimeScaleController timeScaleController,
            StrategyInputRouter router)
        {
            if (isOpen)
            {
                CloseWithoutCompletion();
            }

            timeScale = timeScaleController;
            inputRouter = router;
            configured = timeScale != null && inputRouter != null;
            EnsureView();
        }

        public void PreloadArtwork()
        {
            if (storyResolved
                || preloadRoutine != null
                || preloadedArtwork != null
                || !isActiveAndEnabled)
            {
                return;
            }

            preloadRoutine = StartCoroutine(PreloadArtworkRoutine());
        }

        public void RestoreResolvedState(bool resolved)
        {
            if (isOpen)
            {
                CloseWithoutCompletion();
            }

            if (resolved)
            {
                StopPreload();
                DisposeView();
            }

            storyResolved = resolved;
            completionDelivered = false;
            completedCallback = null;
            frameIndex = 0;
        }

        public bool TryShow(Action onCompleted)
        {
            if (!CanOpenWithoutStacking)
            {
                return false;
            }

            EnsureView();
            PreloadArtwork();
            HoldInput();
            if (inputContext == null || inputContext.IsDisposed)
            {
                return false;
            }

            timeScale.SetRequestedScale(1f);
            PushPauseLock();
            completedCallback = onCompleted;
            completionDelivered = false;
            frameIndex = 0;
            isOpen = true;
            storyCanvasRoot.SetActive(true);
            ShowFrame(0, true);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
            StrategyDebugLogger.Info("FirstNightFauna", "StoryOpened");
            return true;
        }

        private IEnumerator PreloadArtworkRoutine()
        {
            StrategyFoundingStoryPanel[] frames = StrategyFirstNightFaunaStoryCatalog.Frames;
            ResourceRequest[] requests = new ResourceRequest[frames.Length];
            preloadedArtwork = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                requests[i] = Resources.LoadAsync<Sprite>(frames[i].ResourcePath);
            }

            for (int i = 0; i < requests.Length; i++)
            {
                yield return requests[i];
                preloadedArtwork[i] = requests[i].asset as Sprite;
                if (preloadedArtwork[i] == null)
                {
                    StrategyDebugLogger.Warn(
                        "FirstNightFauna",
                        "StoryArtMissing",
                        StrategyDebugLogger.F("path", frames[i].ResourcePath));
                }
            }

            preloadRoutine = null;
        }

        private void ShowFrame(int index, bool immediate)
        {
            StrategyFoundingStoryPanel[] frames = StrategyFirstNightFaunaStoryCatalog.Frames;
            frameIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            StrategyFoundingStoryPanel frame = frames[frameIndex];
            storyChapterText.text = frame.Chapter;
            storyTitleText.text = frame.Title;
            storyBodyText.text = frame.Body;
            storyProgressText.text = (frameIndex + 1) + " / " + frames.Length;
            continueButtonLabel.text = frameIndex == frames.Length - 1
                ? "Let the hunter loose"
                : "Continue";
            presentationController.ShowBackground(frame, immediate);
            presentationController.RevealStory();
            if (immediate)
            {
                continueButtonFeedback?.SuppressNextFocusCue();
            }

            SelectUi(continueButton);
        }

        private void AdvanceStory()
        {
            if (!isOpen)
            {
                return;
            }

            if (frameIndex + 1 < StrategyFirstNightFaunaStoryCatalog.Frames.Length)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
                ShowFrame(frameIndex + 1, false);
                return;
            }

            CompleteStory();
        }

        private void SkipStory()
        {
            if (isOpen)
            {
                CompleteStory();
            }
        }

        private void CompleteStory()
        {
            if (!isOpen || completionDelivered)
            {
                return;
            }

            completionDelivered = true;
            storyResolved = true;
            Action callback = completedCallback;
            completedCallback = null;
            HideView();
            ReleaseInput();
            ReleasePauseLock();
            isOpen = false;
            StopPreload();
            DisposeView();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            StrategyDebugLogger.Info(
                "FirstNightFauna",
                "StoryCompleted",
                StrategyDebugLogger.F("finalFrame", frameIndex + 1));
            callback?.Invoke();
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

        private void PushPauseLock()
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

        private void CloseWithoutCompletion()
        {
            completedCallback = null;
            completionDelivered = false;
            isOpen = false;
            HideView();
            ReleaseInput();
            ReleasePauseLock();
            StopPreload();
            DisposeView();
        }

        private void StopPreload()
        {
            if (preloadRoutine != null)
            {
                StopCoroutine(preloadRoutine);
                preloadRoutine = null;
            }

            preloadedArtwork = null;
        }

        private void OnDisable()
        {
            CloseWithoutCompletion();
            StopPreload();
        }

        private void OnDestroy()
        {
            CloseWithoutCompletion();
            StopPreload();
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
