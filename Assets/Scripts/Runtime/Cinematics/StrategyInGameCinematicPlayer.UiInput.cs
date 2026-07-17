using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyInGameCinematicPlayer
    {
        private EventSystem capturedEventSystem;
        private GameObject previousSelectedGameObject;
        private bool uiInputHeld;

        private void HoldUiInput()
        {
            if (uiInputHeld)
            {
                return;
            }

            capturedEventSystem = EventSystem.current;
            previousSelectedGameObject = capturedEventSystem != null
                ? capturedEventSystem.currentSelectedGameObject
                : null;
            capturedEventSystem?.SetSelectedGameObject(null);
            letterbox?.SetInputShieldActive(true);
            uiInputHeld = true;
        }

        private void ReleaseUiShield()
        {
            letterbox?.SetInputShieldActive(false);
        }

        private void ResumeUiShieldForCameraReturn()
        {
            if (!uiInputHeld || capturedEventSystem == null)
            {
                letterbox?.SetInputShieldActive(true);
                return;
            }

            if (EventSystem.current == capturedEventSystem
                && capturedEventSystem.currentSelectedGameObject == null)
            {
                letterbox?.SetInputShieldActive(true);
            }
        }

        private void RestoreUiSelectionIfUnclaimed()
        {
            if (!uiInputHeld)
            {
                return;
            }

            uiInputHeld = false;
            EventSystem eventSystem = capturedEventSystem;
            GameObject previousSelection = previousSelectedGameObject;
            capturedEventSystem = null;
            previousSelectedGameObject = null;
            if (eventSystem == null
                || !eventSystem.isActiveAndEnabled
                || EventSystem.current != eventSystem
                || eventSystem.currentSelectedGameObject != null
                || inputRouter != null && inputRouter.ActiveContextCount > 0
                || previousSelection == null
                || !previousSelection.activeInHierarchy)
            {
                return;
            }

            Selectable selectable = previousSelection.GetComponent<Selectable>();
            if (selectable == null || selectable.IsActive() && selectable.IsInteractable())
            {
                eventSystem.SetSelectedGameObject(previousSelection);
            }
        }
    }
}
