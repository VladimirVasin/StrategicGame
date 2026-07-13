using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyInputRouterTests
    {
        private readonly List<InputActionAsset> assets = new();
        private GameObject routerObject;
        private StrategyInputRouter router;

        [SetUp]
        public void SetUp()
        {
            routerObject = new GameObject("Test Strategy Input Router");
            router = routerObject.AddComponent<StrategyInputRouter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (routerObject != null)
            {
                Object.DestroyImmediate(routerObject);
            }

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] != null)
                {
                    Object.DestroyImmediate(assets[i]);
                }
            }

            assets.Clear();
        }

        [Test]
        public void CompleteSuppliedAssetConfiguresTheTypedContract()
        {
            InputActionAsset asset = CreateCompleteAsset();

            Assert.That(router.Configure(asset), Is.True);
            Assert.That(router.IsConfigured, Is.True);
            Assert.That(router.ConfigurationError, Is.Empty);
            Assert.That(router.GlobalSavePressed, Is.False);
            Assert.That(router.CameraPan, Is.EqualTo(Vector2.zero));
            Assert.That(router.BuildSlotPressed, Is.Zero);
        }

        [Test]
        public void ProjectInputAssetSatisfiesTheTypedContract()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");

            Assert.That(asset, Is.Not.Null);
            Assert.That(router.Configure(asset), Is.True, router.ConfigurationError);
        }

        [Test]
        public void MissingActionsProduceAnActionableConfigurationError()
        {
            InputActionAsset asset = CreateAsset();
            AddMap(asset, "Global", "Cancel");

            Assert.That(router.Configure(asset), Is.False);
            Assert.That(router.IsConfigured, Is.False);
            Assert.That(router.ConfigurationError, Does.Contain("Global/Save"));
            Assert.That(router.ConfigurationError, Does.Contain("Camera/Pan"));
            Assert.That(router.ConfigurationError, Does.Contain("Build/Slot9"));
            Assert.That(router.ConfigurationError, Does.Contain("Debug/Toggle"));
        }

        [Test]
        public void BlockedChannelsAreUnionedAndHandlesDisposeOutOfOrder()
        {
            StrategyInputContextState state = new();
            object cameraOwner = new();
            object buildOwner = new();
            StrategyInputContextHandle camera = state.Push(
                cameraOwner,
                StrategyInputChannel.Camera | StrategyInputChannel.Gameplay,
                StrategyCancelMode.None,
                false);
            StrategyInputContextHandle build = state.Push(
                buildOwner,
                StrategyInputChannel.Build,
                StrategyCancelMode.None,
                false);

            Assert.That(
                state.BlockedChannels,
                Is.EqualTo(
                    StrategyInputChannel.Camera
                    | StrategyInputChannel.Gameplay
                    | StrategyInputChannel.Build));

            camera.Dispose();
            camera.Dispose();
            Assert.That(camera.IsDisposed, Is.True);
            Assert.That(state.BlockedChannels, Is.EqualTo(StrategyInputChannel.Build));
            Assert.That(state.Count, Is.EqualTo(1));

            build.Dispose();
            Assert.That(state.BlockedChannels, Is.EqualTo(StrategyInputChannel.None));
            Assert.That(state.Count, Is.Zero);
        }

        [Test]
        public void TopmostCancelContextOwnsOrSwallowsCancel()
        {
            StrategyInputContextState state = new();
            object closeOwner = new();
            object neutralOwner = new();
            object swallowOwner = new();
            StrategyInputContextHandle close = state.Push(
                closeOwner,
                StrategyInputChannel.None,
                StrategyCancelMode.Close,
                false);
            StrategyInputContextHandle neutral = state.Push(
                neutralOwner,
                StrategyInputChannel.Camera,
                StrategyCancelMode.None,
                false);

            Assert.That(state.CancelMode, Is.EqualTo(StrategyCancelMode.Close));
            Assert.That(state.IsTopCancelOwner(closeOwner), Is.True);

            StrategyInputContextHandle swallow = state.Push(
                swallowOwner,
                StrategyInputChannel.All,
                StrategyCancelMode.Swallow,
                false);
            Assert.That(state.CancelMode, Is.EqualTo(StrategyCancelMode.Swallow));
            Assert.That(state.IsTopCancelOwner(swallowOwner), Is.True);
            Assert.That(state.IsTopCancelOwner(closeOwner), Is.False);

            swallow.Dispose();
            Assert.That(state.CancelMode, Is.EqualTo(StrategyCancelMode.Close));
            Assert.That(state.IsTopCancelOwner(closeOwner), Is.True);

            neutral.Dispose();
            close.Dispose();
        }

        [Test]
        public void SecondaryPointerOwnershipFollowsTheNewestLiveOwner()
        {
            StrategyInputContextState state = new();
            object firstOwner = new();
            object secondOwner = new();
            StrategyInputContextHandle first = state.Push(
                firstOwner,
                StrategyInputChannel.None,
                StrategyCancelMode.None,
                true);
            StrategyInputContextHandle second = state.Push(
                secondOwner,
                StrategyInputChannel.None,
                StrategyCancelMode.None,
                true);

            Assert.That(state.IsSecondaryPointerOwnedBy(secondOwner), Is.True);
            first.Dispose();
            Assert.That(state.IsSecondaryPointerOwnedBy(secondOwner), Is.True);
            second.Dispose();
            Assert.That(state.SecondaryPointerOwner, Is.Null);
        }

        [Test]
        public void OwnerReleaseRemovesEveryOwnedContextAndInvalidatesHandles()
        {
            StrategyInputContextState state = new();
            object owner = new();
            StrategyInputContextHandle first = state.Push(
                owner,
                StrategyInputChannel.Camera,
                StrategyCancelMode.Close,
                false);
            StrategyInputContextHandle second = state.Push(
                owner,
                StrategyInputChannel.Build,
                StrategyCancelMode.None,
                true);

            Assert.That(state.ReleaseOwner(owner), Is.EqualTo(2));
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(state.Count, Is.Zero);
            Assert.That(state.BlockedChannels, Is.EqualTo(StrategyInputChannel.None));

            first.Dispose();
            second.Dispose();
        }

        [Test]
        public void LifecycleCleanupClearsSceneContexts()
        {
            object owner = new();
            StrategyInputContextHandle handle = router.PushContext(
                owner,
                StrategyInputChannel.All,
                StrategyCancelMode.Swallow,
                true);
            Assert.That(router.ActiveContextCount, Is.EqualTo(1));

            router.ClearSceneContexts();

            Assert.That(router.ActiveContextCount, Is.Zero);
            Assert.That(router.BlockedChannels, Is.EqualTo(StrategyInputChannel.None));
            Assert.That(handle.IsDisposed, Is.True);
            handle.Dispose();
        }

        [Test]
        public void ModalContextRecoversAfterRouterResetAndRebind()
        {
            InputActionAsset asset = CreateCompleteAsset();
            Assert.That(router.Configure(asset), Is.True, router.ConfigurationError);
            EventSystem existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
            GameObject dialogObject = new("Test Confirmation Dialog");
            GameObject secondRouterObject = new("Second Test Strategy Input Router");
            try
            {
                StrategyInputRouter secondRouter = secondRouterObject.AddComponent<StrategyInputRouter>();
                Assert.That(secondRouter.Configure(asset), Is.True, secondRouter.ConfigurationError);
                StrategyConfirmationDialogController dialog =
                    dialogObject.AddComponent<StrategyConfirmationDialogController>();
                dialog.Configure();
                CanvasGroup dialogRoot = dialog.GetComponentInChildren<CanvasGroup>(true);
                Assert.That(dialogRoot, Is.Not.Null);
                dialogRoot.alpha = 1f;
                dialogRoot.interactable = true;
                dialogRoot.blocksRaycasts = true;
                dialog.SetInputRouter(router);
                Assert.That(router.ActiveContextCount, Is.EqualTo(1));

                MethodInfo updateDialog = typeof(StrategyConfirmationDialogController).GetMethod(
                    "Update",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(updateDialog, Is.Not.Null);
                router.enabled = false;
                updateDialog.Invoke(dialog, null);
                Assert.That(router.ActiveContextCount, Is.Zero);
                router.enabled = true;
                updateDialog.Invoke(dialog, null);
                Assert.That(router.ActiveContextCount, Is.EqualTo(1));

                dialog.SetInputRouter(secondRouter);
                Assert.That(router.ActiveContextCount, Is.Zero);
                Assert.That(secondRouter.ActiveContextCount, Is.EqualTo(1));
                dialog.Hide();
                Assert.That(secondRouter.ActiveContextCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(dialogObject);
                Object.DestroyImmediate(secondRouterObject);
                if (existingEventSystem == null)
                {
                    EventSystem createdEventSystem = Object.FindAnyObjectByType<EventSystem>();
                    if (createdEventSystem != null)
                    {
                        Object.DestroyImmediate(createdEventSystem.gameObject);
                    }
                }
            }
        }

        private InputActionAsset CreateCompleteAsset()
        {
            InputActionAsset asset = CreateAsset();
            AddMap(asset, "Global", "Cancel", "Save", "Load", "Speed1", "Speed2", "Speed3");
            AddMap(
                asset,
                "Camera",
                "Pan",
                "FocusCamp",
                "ZoomKeys",
                "PointerPosition",
                "PointerDelta",
                "Scroll",
                "MiddleDrag",
                "RightDrag");
            AddMap(asset, "Gameplay", "PrimaryClick", "DeleteSelection");
            AddMap(
                asset,
                "Build",
                "Toggle",
                "Place",
                "CancelPointer",
                "Slot1",
                "Slot2",
                "Slot3",
                "Slot4",
                "Slot5",
                "Slot6",
                "Slot7",
                "Slot8",
                "Slot9");
            AddMap(asset, "Debug", "Toggle");
            return asset;
        }

        private InputActionAsset CreateAsset()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "Strategy Input Router Test Actions";
            assets.Add(asset);
            return asset;
        }

        private static void AddMap(InputActionAsset asset, string name, params string[] actionNames)
        {
            InputActionMap map = new(name);
            asset.AddActionMap(map);
            for (int i = 0; i < actionNames.Length; i++)
            {
                map.AddAction(actionNames[i], InputActionType.Button);
            }
        }
    }
}
