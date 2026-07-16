using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyScoutLodgeOnboardingController : MonoBehaviour
    {
        private const string PauseReason = "ScoutLodgeOnboarding";
        private const float FocusZoom = 7f;
        private const float FocusDuration = 0.55f;
        private const float FocusReturnDuration = 0.18f;
        private const float DialogRevealDelay = 0.42f;

        private StrategyBuildPlacementController placement;
        private StrategyPopulationController population;
        private StrategyCameraController cameraController;
        private StrategyWorldSelectionController selection;
        private StrategyScoutAssignmentDialogController dialog;
        private StrategyTimeScaleController timeScale;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle focusInputContext;
        private StrategyPlacedBuilding pendingBuilding;
        private StrategyScoutLodge pendingLodge;
        private FlowStage stage;
        private bool introduction;
        private bool introductionQueuedOrShown;
        private bool pauseHeld;
        private float dialogRevealAt;
        private Vector3 returnCameraCenter;
        private float returnCameraSize;
        private bool hasReturnCameraView;

        public bool IsActive => stage != FlowStage.None;

        public void Configure(
            StrategyBuildPlacementController placementController,
            StrategyPopulationController populationController,
            StrategyCameraController strategyCameraController,
            StrategyWorldSelectionController worldSelection,
            StrategyScoutAssignmentDialogController assignmentDialog,
            StrategyTimeScaleController timeScaleController,
            StrategyInputRouter router)
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }

            CancelFlow(true);
            placement = placementController;
            population = populationController;
            cameraController = strategyCameraController;
            selection = worldSelection;
            dialog = assignmentDialog;
            timeScale = timeScaleController;
            inputRouter = router;
            introductionQueuedOrShown = HasExistingScoutLodge();

            if (placement != null && isActiveAndEnabled)
            {
                placement.BuildingCompleted += HandleBuildingCompleted;
            }

            StrategyDebugLogger.Info(
                "ScoutOnboarding",
                "Configured",
                StrategyDebugLogger.F("existingLodge", introductionQueuedOrShown));
        }

        public bool RequestAssignment(StrategyScoutLodge lodge)
        {
            if (!isActiveAndEnabled
                || lodge == null
                || lodge.WorkerCount >= StrategyScoutLodge.MaxWorkers
                || IsActive
                || (dialog != null && dialog.IsInputShieldActive))
            {
                return false;
            }

            StrategyPlacedBuilding building = lodge.GetComponent<StrategyPlacedBuilding>();
            if (building == null)
            {
                return false;
            }

            QueueFlow(building, lodge, false);
            return true;
        }

        private void Update()
        {
            if (stage == FlowStage.Pending)
            {
                TryBeginPendingFlow();
            }
            else if (stage == FlowStage.Focusing)
            {
                UpdateFocusReveal();
            }
        }

        private void HandleBuildingCompleted(StrategyPlacedBuilding building)
        {
            if (building == null
                || building.Tool != StrategyBuildTool.ScoutLodge
                || introductionQueuedOrShown
                || CountScoutLodges() != 1)
            {
                return;
            }

            StrategyScoutLodge lodge = building.GetComponent<StrategyScoutLodge>();
            if (lodge == null)
            {
                StrategyDebugLogger.Warn("ScoutOnboarding", "CompletedLodgeMissingWorksite");
                return;
            }

            introductionQueuedOrShown = true;
            QueueFlow(building, lodge, true);
            StrategyDebugLogger.Info(
                "ScoutOnboarding",
                "IntroductionQueued",
                StrategyDebugLogger.F("origin", building.Origin));
        }

        private void QueueFlow(
            StrategyPlacedBuilding building,
            StrategyScoutLodge lodge,
            bool showIntroduction)
        {
            ClearReturnCameraView();
            pendingBuilding = building;
            pendingLodge = lodge;
            introduction = showIntroduction;
            stage = FlowStage.Pending;
        }

        private void TryBeginPendingFlow()
        {
            if (!HasUsableTarget())
            {
                CancelFlow(false);
                return;
            }

            if (pendingLodge.WorkerCount >= StrategyScoutLodge.MaxWorkers)
            {
                selection?.SelectBuilding(pendingBuilding);
                CancelFlow(false);
                return;
            }

            if (dialog == null
                || dialog.IsInputShieldActive
                || !dialog.CanOpenWithoutStacking
                || (timeScale != null && timeScale.IsPausedByLock && !pauseHeld))
            {
                return;
            }

            PushPauseLock();
            if (introduction && cameraController != null)
            {
                CaptureReturnCameraView();
                HoldFocusInput();
                cameraController.FocusOnAnimated(
                    pendingBuilding.SelectionBounds.center,
                    FocusZoom,
                    FocusDuration);
                dialogRevealAt = Time.unscaledTime + DialogRevealDelay;
                stage = FlowStage.Focusing;
                return;
            }

            OpenDialog();
        }

        private void UpdateFocusReveal()
        {
            if (!HasUsableTarget())
            {
                CancelFlow(false);
                return;
            }

            if (Time.unscaledTime < dialogRevealAt)
            {
                return;
            }

            ReleaseFocusInput();
            OpenDialog();
        }

        private void OpenDialog()
        {
            if (!HasUsableTarget() || dialog == null)
            {
                CancelFlow(false);
                return;
            }

            stage = FlowStage.Dialog;
            try
            {
                dialog.Show(
                    pendingLodge,
                    population,
                    introduction,
                    TryAssignSelectedResident,
                    HandleDeferred);
                StrategyDebugLogger.Info(
                    "ScoutOnboarding",
                    introduction ? "IntroductionOpened" : "PickerOpened",
                    StrategyDebugLogger.F("origin", pendingBuilding.Origin));
            }
            catch (Exception exception)
            {
                StrategyDebugLogger.Warn(
                    "ScoutOnboarding",
                    "DialogOpenFailed",
                    StrategyDebugLogger.F("error", exception.Message));
                CancelFlow(true);
            }
        }

        private bool TryAssignSelectedResident(StrategyResidentAgent resident)
        {
            bool isFirstScout = CountAssignedScouts() == 0;
            if (!HasUsableTarget()
                || resident == null
                || !pendingLodge.TryAppointWorker(resident))
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
                return false;
            }

            StrategyPlacedBuilding completedBuilding = pendingBuilding;
            selection?.SelectBuilding(completedBuilding);
            StrategyEventLogHudController.Notify(
                isFirstScout
                    ? resident.FullName + " set out beyond the firelight as the settlement's first Scout."
                    : resident.FullName + " took up the Lodge's compass and joined the Scouts.",
                new Color(0.86f, 0.70f, 0.42f));
            StrategyDebugLogger.Info(
                "ScoutOnboarding",
                "ScoutAppointed",
                StrategyDebugLogger.F("resident", resident.FullName),
                StrategyDebugLogger.F("origin", completedBuilding.Origin));
            CompleteFlow();
            return true;
        }

        private void HandleDeferred()
        {
            if (pendingBuilding != null)
            {
                selection?.SelectBuilding(pendingBuilding);
            }

            if (introduction)
            {
                StrategyEventLogHudController.Notify(
                    "The Scout Lodge awaits a free adult for the first expedition.",
                    new Color(0.78f, 0.70f, 0.50f));
            }

            StrategyDebugLogger.Info(
                "ScoutOnboarding",
                introduction ? "AssignmentDeferred" : "PickerCancelled");
            CompleteFlow();
        }

        private int CountAssignedScouts()
        {
            int count = 0;
            if (placement == null || placement.PlacedBuildings == null)
            {
                return count;
            }

            for (int i = 0; i < placement.PlacedBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = placement.PlacedBuildings[i];
                StrategyScoutLodge lodge = building != null
                    ? building.GetComponent<StrategyScoutLodge>()
                    : null;
                if (lodge != null)
                {
                    count += lodge.WorkerCount;
                }
            }

            return count;
        }

        private bool HasUsableTarget()
        {
            return pendingBuilding != null
                && pendingLodge != null
                && pendingLodge.gameObject.activeInHierarchy;
        }

        private bool HasExistingScoutLodge()
        {
            return CountScoutLodges() > 0;
        }

        private int CountScoutLodges()
        {
            int count = 0;
            if (placement == null || placement.PlacedBuildings == null)
            {
                return count;
            }

            for (int i = 0; i < placement.PlacedBuildings.Count; i++)
            {
                StrategyPlacedBuilding building = placement.PlacedBuildings[i];
                if (building != null && building.Tool == StrategyBuildTool.ScoutLodge)
                {
                    count++;
                }
            }

            return count;
        }

        private void HoldFocusInput()
        {
            if (focusInputContext == null
                && inputRouter != null
                && inputRouter.IsAvailable)
            {
                focusInputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Swallow);
            }
        }

        private void ReleaseFocusInput()
        {
            focusInputContext?.Dispose();
            focusInputContext = null;
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

            if (timeScale != null)
            {
                timeScale.PopPauseLock(PauseReason);
            }

            pauseHeld = false;
        }

        private void CompleteFlow()
        {
            StrategyCameraController returnCamera = hasReturnCameraView
                ? cameraController
                : null;
            Vector3 returnCenter = returnCameraCenter;
            float returnSize = returnCameraSize;
            ClearReturnCameraView();
            ReleaseFocusInput();
            ReleasePauseLock();
            pendingBuilding = null;
            pendingLodge = null;
            introduction = false;
            stage = FlowStage.None;

            if (returnCamera != null)
            {
                returnCamera.RestoreViewAnimated(
                    returnCenter,
                    returnSize,
                    FocusReturnDuration);
                StrategyDebugLogger.Info(
                    "ScoutOnboarding",
                    "CameraViewRestoring",
                    StrategyDebugLogger.F("target", returnCenter),
                    StrategyDebugLogger.F("size", returnSize));
            }
        }

        private void CaptureReturnCameraView()
        {
            hasReturnCameraView = cameraController != null
                && cameraController.TryGetView(
                    out returnCameraCenter,
                    out returnCameraSize);
        }

        private void ClearReturnCameraView()
        {
            hasReturnCameraView = false;
            returnCameraCenter = default;
            returnCameraSize = 0f;
        }

        private void CancelFlow(bool dismissDialog)
        {
            if (dismissDialog && stage == FlowStage.Dialog)
            {
                dialog?.Dismiss();
            }

            CompleteFlow();
        }

        private void OnDisable()
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }

            CancelFlow(true);
        }

        private void OnEnable()
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
                placement.BuildingCompleted += HandleBuildingCompleted;
            }
        }

        private void OnDestroy()
        {
            if (placement != null)
            {
                placement.BuildingCompleted -= HandleBuildingCompleted;
            }

            CancelFlow(true);
        }

        private enum FlowStage
        {
            None,
            Pending,
            Focusing,
            Dialog
        }
    }
}
