using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyInputRouter : MonoBehaviour
    {
        private const string GlobalMap = "Global/";
        private const string CameraMap = "Camera/";
        private const string GameplayMap = "Gameplay/";
        private const string BuildMap = "Build/";
        private const string DebugMap = "Debug/";

        private readonly StrategyInputContextState contexts = new();
        private readonly InputAction[] buildSlots = new InputAction[9];

        private InputAction globalCancel;
        private InputAction globalSave;
        private InputAction globalLoad;
        private InputAction globalSpeed1;
        private InputAction globalSpeed2;
        private InputAction globalSpeed3;

        private InputAction cameraPan;
        private InputAction cameraFocusCamp;
        private InputAction cameraZoomKeys;
        private InputAction cameraPointerPosition;
        private InputAction cameraPointerDelta;
        private InputAction cameraScroll;
        private InputAction cameraDragMiddle;
        private InputAction cameraDragRight;

        private InputAction gameplayPrimaryClick;
        private InputAction gameplayDeleteSelection;

        private InputAction buildToggle;
        private InputAction buildPlace;
        private InputAction buildCancelPointer;

        private InputAction debugToggle;
        private int cancelConsumedFrame = -1;
        private int secondaryPointerConsumedFrame = -1;

        public bool IsConfigured { get; private set; }
        public bool IsAvailable => isActiveAndEnabled && IsConfigured;
        public string ConfigurationError { get; private set; } = string.Empty;
        public int ActiveContextCount => contexts.Count;
        public StrategyInputChannel BlockedChannels => contexts.BlockedChannels;
        public StrategyCancelMode TopCancelMode => contexts.CancelMode;
        public bool IsCancelSwallowed => contexts.CancelMode == StrategyCancelMode.Swallow;
        public bool IsSecondaryPointerOwned => contexts.SecondaryPointerOwner != null;

        public bool GlobalCancelPressed => CanSurfaceGlobalCancel(
                cancelConsumedFrame,
                Time.frameCount,
                IsChannelEnabled(StrategyInputChannel.Global),
                contexts.CancelMode)
            && WasPressedRaw(globalCancel);
        public bool GlobalSavePressed => WasPressed(globalSave, StrategyInputChannel.Global);
        public bool GlobalLoadPressed => WasPressed(globalLoad, StrategyInputChannel.Global);
        public bool GlobalSpeed1Pressed => WasPressed(globalSpeed1, StrategyInputChannel.Global);
        public bool GlobalSpeed2Pressed => WasPressed(globalSpeed2, StrategyInputChannel.Global);
        public bool GlobalSpeed3Pressed => WasPressed(globalSpeed3, StrategyInputChannel.Global);

        public Vector2 CameraPan => ReadVector2(cameraPan, StrategyInputChannel.Camera);
        public bool CameraFocusCampPressed => WasPressed(cameraFocusCamp, StrategyInputChannel.Camera);
        public float CameraZoomKeys => ReadFloat(cameraZoomKeys, StrategyInputChannel.Camera);
        public bool CameraHasPointer => HasControls(cameraPointerPosition, StrategyInputChannel.Camera);
        public Vector2 CameraPointerPosition => ReadVector2(cameraPointerPosition, StrategyInputChannel.Camera);
        public Vector2 CameraPointerDelta => ReadVector2(cameraPointerDelta, StrategyInputChannel.Camera);
        public Vector2 CameraScroll => ReadVector2(cameraScroll, StrategyInputChannel.Camera);
        public bool CameraMiddleDragHeld => IsPressed(cameraDragMiddle, StrategyInputChannel.Camera);
        public bool CameraRightDragHeld => !IsSecondaryPointerOwned
            && secondaryPointerConsumedFrame != Time.frameCount
            && IsPressed(cameraDragRight, StrategyInputChannel.Camera);

        public bool GameplayPrimaryClickPressed => WasPressed(
            gameplayPrimaryClick,
            StrategyInputChannel.Gameplay);
        public bool GameplayDeleteSelectionPressed => WasPressed(
            gameplayDeleteSelection,
            StrategyInputChannel.Gameplay);

        public bool BuildTogglePressed => WasPressed(buildToggle, StrategyInputChannel.Build);
        public bool BuildPlacePressed => WasPressed(buildPlace, StrategyInputChannel.Build);
        public bool BuildCancelPointerPressed => WasPressed(buildCancelPointer, StrategyInputChannel.Build);
        public int BuildSlotPressed => ReadBuildSlotPressed();

        public bool DebugTogglePressed => WasPressed(debugToggle, StrategyInputChannel.Debug);

        public bool Configure()
        {
            return Configure(InputSystem.actions);
        }

        internal bool Configure(InputActionAsset suppliedActions)
        {
            ResetActionReferences();
            if (suppliedActions == null)
            {
                IsConfigured = false;
                ConfigurationError = "InputSystem.actions is not assigned.";
                return false;
            }

            List<string> missing = new();
            globalCancel = FindRequiredAction(suppliedActions, GlobalMap + "Cancel", missing);
            globalSave = FindRequiredAction(suppliedActions, GlobalMap + "Save", missing);
            globalLoad = FindRequiredAction(suppliedActions, GlobalMap + "Load", missing);
            globalSpeed1 = FindRequiredAction(suppliedActions, GlobalMap + "Speed1", missing);
            globalSpeed2 = FindRequiredAction(suppliedActions, GlobalMap + "Speed2", missing);
            globalSpeed3 = FindRequiredAction(suppliedActions, GlobalMap + "Speed3", missing);

            cameraPan = FindRequiredAction(suppliedActions, CameraMap + "Pan", missing);
            cameraFocusCamp = FindRequiredAction(suppliedActions, CameraMap + "FocusCamp", missing);
            cameraZoomKeys = FindRequiredAction(suppliedActions, CameraMap + "ZoomKeys", missing);
            cameraPointerPosition = FindRequiredAction(suppliedActions, CameraMap + "PointerPosition", missing);
            cameraPointerDelta = FindRequiredAction(suppliedActions, CameraMap + "PointerDelta", missing);
            cameraScroll = FindRequiredAction(suppliedActions, CameraMap + "Scroll", missing);
            cameraDragMiddle = FindRequiredAction(suppliedActions, CameraMap + "MiddleDrag", missing);
            cameraDragRight = FindRequiredAction(suppliedActions, CameraMap + "RightDrag", missing);

            gameplayPrimaryClick = FindRequiredAction(suppliedActions, GameplayMap + "PrimaryClick", missing);
            gameplayDeleteSelection = FindRequiredAction(
                suppliedActions,
                GameplayMap + "DeleteSelection",
                missing);

            buildToggle = FindRequiredAction(suppliedActions, BuildMap + "Toggle", missing);
            buildPlace = FindRequiredAction(suppliedActions, BuildMap + "Place", missing);
            buildCancelPointer = FindRequiredAction(suppliedActions, BuildMap + "CancelPointer", missing);
            for (int i = 0; i < buildSlots.Length; i++)
            {
                buildSlots[i] = FindRequiredAction(
                    suppliedActions,
                    BuildMap + "Slot" + (i + 1),
                    missing);
            }

            debugToggle = FindRequiredAction(suppliedActions, DebugMap + "Toggle", missing);
            IsConfigured = missing.Count == 0;
            ConfigurationError = IsConfigured
                ? string.Empty
                : "Missing input actions: " + string.Join(", ", missing);
            if (IsConfigured)
            {
                suppliedActions.Enable();
            }

            return IsConfigured;
        }

        public StrategyInputContextHandle PushContext(
            object owner,
            StrategyInputChannel blockedChannels = StrategyInputChannel.None,
            StrategyCancelMode cancelMode = StrategyCancelMode.None,
            bool ownsSecondaryPointer = false)
        {
            return contexts.Push(owner, blockedChannels, cancelMode, ownsSecondaryPointer);
        }

        internal static bool CanSurfaceGlobalCancel(
            int consumedFrame,
            int currentFrame,
            bool globalChannelEnabled,
            StrategyCancelMode topCancelMode)
        {
            return consumedFrame != currentFrame
                && globalChannelEnabled
                && topCancelMode == StrategyCancelMode.None;
        }

        public int ReleaseContexts(object owner)
        {
            return contexts.ReleaseOwner(owner);
        }

        public bool IsChannelEnabled(StrategyInputChannel channel)
        {
            return IsAvailable
                && (channel == StrategyInputChannel.None || !contexts.IsBlocked(channel));
        }

        public bool IsTopCancelOwner(object owner)
        {
            return contexts.IsTopCancelOwner(owner);
        }

        public bool IsSecondaryPointerOwnedBy(object owner)
        {
            return contexts.IsSecondaryPointerOwnedBy(owner);
        }

        public bool TryConsumeCancel(object owner)
        {
            if (!IsAvailable
                || contexts.CancelMode != StrategyCancelMode.Close
                || !contexts.IsTopCancelOwner(owner)
                || cancelConsumedFrame == Time.frameCount
                || !WasPressedRaw(globalCancel))
            {
                return false;
            }

            cancelConsumedFrame = Time.frameCount;
            return true;
        }

        public bool TryConsumeBuildCancelPointer(object owner)
        {
            if (!IsAvailable
                || !contexts.IsSecondaryPointerOwnedBy(owner)
                || secondaryPointerConsumedFrame == Time.frameCount
                || !BuildCancelPointerPressed)
            {
                return false;
            }

            secondaryPointerConsumedFrame = Time.frameCount;
            return true;
        }

        private void OnDisable()
        {
            ClearSceneContexts();
        }

        internal void ClearSceneContexts()
        {
            contexts.Clear();
            cancelConsumedFrame = -1;
            secondaryPointerConsumedFrame = -1;
        }

        private int ReadBuildSlotPressed()
        {
            if (!IsConfigured || !IsChannelEnabled(StrategyInputChannel.Build))
            {
                return 0;
            }

            for (int i = 0; i < buildSlots.Length; i++)
            {
                if (WasPressedRaw(buildSlots[i]))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private bool WasPressed(InputAction action, StrategyInputChannel channel)
        {
            return IsConfigured && IsChannelEnabled(channel) && WasPressedRaw(action);
        }

        private bool IsPressed(InputAction action, StrategyInputChannel channel)
        {
            return IsConfigured
                && IsChannelEnabled(channel)
                && action != null
                && action.IsPressed();
        }

        private bool HasControls(InputAction action, StrategyInputChannel channel)
        {
            return IsConfigured
                && IsChannelEnabled(channel)
                && action != null
                && action.controls.Count > 0;
        }

        private Vector2 ReadVector2(InputAction action, StrategyInputChannel channel)
        {
            return IsConfigured && IsChannelEnabled(channel) && action != null
                ? action.ReadValue<Vector2>()
                : Vector2.zero;
        }

        private float ReadFloat(InputAction action, StrategyInputChannel channel)
        {
            return IsConfigured && IsChannelEnabled(channel) && action != null
                ? action.ReadValue<float>()
                : 0f;
        }

        private static bool WasPressedRaw(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

        private static InputAction FindRequiredAction(
            InputActionAsset asset,
            string path,
            ICollection<string> missing)
        {
            InputAction action = asset.FindAction(path, false);
            if (action == null)
            {
                missing.Add(path);
            }

            return action;
        }

        private void ResetActionReferences()
        {
            IsConfigured = false;
            ConfigurationError = string.Empty;
            globalCancel = null;
            globalSave = null;
            globalLoad = null;
            globalSpeed1 = null;
            globalSpeed2 = null;
            globalSpeed3 = null;
            cameraPan = null;
            cameraFocusCamp = null;
            cameraZoomKeys = null;
            cameraPointerPosition = null;
            cameraPointerDelta = null;
            cameraScroll = null;
            cameraDragMiddle = null;
            cameraDragRight = null;
            gameplayPrimaryClick = null;
            gameplayDeleteSelection = null;
            buildToggle = null;
            buildPlace = null;
            buildCancelPointer = null;
            debugToggle = null;
            Array.Clear(buildSlots, 0, buildSlots.Length);
        }
    }
}
