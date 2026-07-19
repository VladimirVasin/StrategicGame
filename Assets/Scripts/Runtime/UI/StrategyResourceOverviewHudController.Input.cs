using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResourceOverviewHudController
    {
        private static readonly StrategyInputChannel BlockedInputChannels =
            StrategyInputChannel.Camera
            | StrategyInputChannel.Gameplay
            | StrategyInputChannel.Build;

        private void ApplyOpenState(bool open, bool immediate, bool playSfx)
        {
            if (open)
            {
                if (isOpen && !isClosing)
                {
                    return;
                }

                CapturePreviousSelection();
                isOpen = true;
                isClosing = false;
                closeReleaseTime = 0f;
                RefreshNow();
                panelTransition.SetVisible(true, immediate);
                RefreshInputContext();
                SelectInitialControl();
                if (playSfx && Application.isPlaying)
                {
                    StrategyHudSfxAudio.Play(StrategyHudSfxKind.Open);
                }

                StrategyDebugLogger.Info(
                    "ResourceOverviewHud",
                    "Opened",
                    StrategyDebugLogger.F("resources", resourceRows.Count));
                return;
            }

            if (!isOpen && !isClosing)
            {
                panelTransition.SetVisible(false, true);
                ReleaseInputContext();
                return;
            }

            isOpen = false;
            panelTransition.SetVisible(false, immediate);
            if (immediate)
            {
                CompleteClose();
            }
            else
            {
                isClosing = true;
                closeReleaseTime = Time.unscaledTime + PanelCloseDuration;
                RefreshInputContext();
            }

            if (playSfx && Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
            }

            StrategyDebugLogger.Info("ResourceOverviewHud", "Closed");
        }

        private void UpdateInputAndClosingState()
        {
            RefreshInputContext();
            if (isOpen
                && inputRouter != null
                && inputRouter.IsAvailable
                && inputRouter.TryConsumeCancel(this))
            {
                ApplyOpenState(false, false, true);
                return;
            }

            if (isClosing && Time.unscaledTime >= closeReleaseTime)
            {
                CompleteClose();
            }
        }

        private void RefreshInputContext()
        {
            bool shouldHold = (isOpen || isClosing)
                && inputRouter != null
                && inputRouter.IsAvailable;
            if (!shouldHold)
            {
                ReleaseInputContext();
                return;
            }

            if (inputContext == null || inputContext.IsDisposed)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    BlockedInputChannels,
                    StrategyCancelMode.Close);
            }
        }

        private void CompleteClose()
        {
            isClosing = false;
            closeReleaseTime = 0f;
            ReleaseInputContext();
            RestorePreviousSelection();
        }

        private void ReleaseInputContext()
        {
            inputContext?.Dispose();
            inputContext = null;
        }

        private void CapturePreviousSelection()
        {
            if (hasStoredSelection)
            {
                return;
            }

            previousSelectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            hasStoredSelection = true;
        }

        private void SelectInitialControl()
        {
            if (EventSystem.current != null && closeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }

        private void RestorePreviousSelection()
        {
            if (!hasStoredSelection)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                GameObject target = previousSelectedObject != null
                    && previousSelectedObject.activeInHierarchy
                        ? previousSelectedObject
                        : null;
                EventSystem.current.SetSelectedGameObject(target);
            }

            previousSelectedObject = null;
            hasStoredSelection = false;
        }
    }
}
