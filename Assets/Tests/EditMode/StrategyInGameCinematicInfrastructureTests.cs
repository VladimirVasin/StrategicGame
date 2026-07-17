using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyInGameCinematicMathTests
    {
        [Test]
        public void LetterboxBuilds239SafeApertureOnSixteenByNineViewport()
        {
            const float ViewportAspect = 16f / 9f;
            const float TargetAspect = 2.39f;
            float barFraction = StrategyInGameCinematicMath.CalculateLetterboxBarFraction(
                ViewportAspect,
                TargetAspect,
                StrategyCinematicLetterboxView.DefaultMinimumBarFraction);
            float safeHeight = StrategyInGameCinematicMath.CalculateSafeHeightFraction(
                barFraction);

            Assert.That(barFraction, Is.EqualTo(0.12808065f).Within(0.0001f));
            Assert.That(safeHeight, Is.EqualTo(1f - barFraction * 2f).Within(0.0001f));
            Assert.That(ViewportAspect / safeHeight, Is.EqualTo(TargetAspect).Within(0.0001f));
        }

        [Test]
        public void LetterboxKeepsVisibleBarsWhenViewportIsWiderThanTarget()
        {
            float barFraction = StrategyInGameCinematicMath.CalculateLetterboxBarFraction(
                32f / 9f,
                StrategyCinematicLetterboxView.DefaultTargetAspectRatio,
                StrategyCinematicLetterboxView.DefaultMinimumBarFraction);

            Assert.That(
                barFraction,
                Is.EqualTo(StrategyCinematicLetterboxView.DefaultMinimumBarFraction)
                    .Within(0.0001f));
        }

        [Test]
        public void FramingFitsBoundsInsideTheUnoccludedSafeAperture()
        {
            const float CameraAspect = 16f / 9f;
            float barFraction = StrategyInGameCinematicMath.CalculateLetterboxBarFraction(
                CameraAspect,
                2.39f,
                StrategyCinematicLetterboxView.DefaultMinimumBarFraction);
            float safeHeight = StrategyInGameCinematicMath.CalculateSafeHeightFraction(
                barFraction);
            StrategyInGameCinematicFraming framing = new(
                new Bounds(Vector3.zero, new Vector3(8f, 4f, 1f)),
                new Vector2(1f, 0.5f));

            float size = StrategyInGameCinematicMath.CalculateTargetOrthographicSize(
                framing,
                CameraAspect,
                safeHeight);

            Assert.That(size * CameraAspect, Is.GreaterThanOrEqualTo(5f - 0.0001f));
            Assert.That(size * safeHeight, Is.GreaterThanOrEqualTo(2.5f - 0.0001f));
            Assert.That(size, Is.EqualTo(2.5f / safeHeight).Within(0.0001f));
        }

        [Test]
        public void LetterboxProvidesAFullScreenTransparentRaycastShield()
        {
            GameObject viewObject = new(
                "Letterbox Shield Test",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            try
            {
                StrategyCinematicLetterboxView view =
                    viewObject.AddComponent<StrategyCinematicLetterboxView>();
                view.Configure(2.39f, 0.055f, 287);
                view.SetInputShieldActive(true);

                Image shield = view.InputShieldImage;
                Assert.That(viewObject.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(shield, Is.Not.Null);
                Assert.That(shield.raycastTarget, Is.True);
                Assert.That(shield.color.a, Is.Zero);
                Assert.That(shield.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(shield.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(viewObject.GetComponent<Canvas>().sortingOrder, Is.EqualTo(287));
                Assert.That(view.IsInputShieldActive, Is.True);

                view.SetInputShieldActive(false);
                Assert.That(view.IsInputShieldActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }
    }

    public sealed partial class StrategyInGameCinematicPlayerTests
    {
        private const string TestPauseReason = "CinematicTestHandoff";

        private GameObject root;
        private GameObject cameraRoot;
        private InputActionAsset inputAsset;
        private StrategyInputRouter inputRouter;
        private StrategyTimeScaleController timeScale;
        private StrategyCameraController cameraController;
        private StrategyInGameCinematicPlayer player;
        private StrategyCinematicLetterboxView letterbox;
        private StrategyInputContextHandle handoffInput;
        private EventSystem manuallyRegisteredEventSystem;
        private bool handoffPauseHeld;
        private float originalTimeScale;
        private float originalFixedDeltaTime;
        private Vector3 originalCameraCenter;
        private float originalCameraSize;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            root = new GameObject("In-Game Cinematic Test Root");
            inputRouter = root.AddComponent<StrategyInputRouter>();
            InputActionAsset projectAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(projectAsset, Is.Not.Null);
            inputAsset = Object.Instantiate(projectAsset);
            Assert.That(inputRouter.Configure(inputAsset), Is.True, inputRouter.ConfigurationError);

            timeScale = root.AddComponent<StrategyTimeScaleController>();
            timeScale.SetInputRouter(inputRouter);
            timeScale.Configure();
            timeScale.SetRequestedScale(2f);

            cameraRoot = new GameObject("In-Game Cinematic Test Camera");
            cameraRoot.transform.SetParent(root.transform, false);
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.orthographic = true;
            originalCameraCenter = new Vector3(3f, -4f, -10f);
            originalCameraSize = 14f;
            cameraRoot.transform.position = originalCameraCenter;
            camera.orthographicSize = originalCameraSize;
            cameraController = cameraRoot.AddComponent<StrategyCameraController>();

            GameObject playerRoot = new("In-Game Cinematic Test Player");
            playerRoot.transform.SetParent(root.transform, false);
            player = playerRoot.AddComponent<StrategyInGameCinematicPlayer>();
            player.Configure(cameraController, timeScale, inputRouter);
        }

        [TearDown]
        public void TearDown()
        {
            player?.Cancel(false);
            handoffInput?.Dispose();
            handoffInput = null;
            if (handoffPauseHeld && timeScale != null && timeScale.IsPausedByLock)
            {
                timeScale.PopPauseLock(TestPauseReason);
            }

            handoffPauseHeld = false;
            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && root != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(root.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (manuallyRegisteredEventSystem != null)
            {
                InvokeEventSystemLifecycle(manuallyRegisteredEventSystem, "OnDisable");
                manuallyRegisteredEventSystem = null;
            }

            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (inputAsset != null)
            {
                Object.DestroyImmediate(inputAsset);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        [Test]
        public void CompletionAtomicallyHandsOwnershipOffThenRestoresAndReleasesFocus()
        {
            BlockingSequence sequence = new();
            StrategyInGameCinematicOptions options = CreateOpeningOptions();
            EventSystem eventSystem = EnsureEventSystem();
            GameObject previousSelection = CreateSelectable("Previous Cinematic Selection");
            GameObject handoffSelection = CreateSelectable("Handoff Cinematic Selection");
            eventSystem.SetSelectedGameObject(previousSelection);
            int callbackCount = 0;
            int contextsAtCallback = -1;
            bool pausedAtCallback = true;
            float timeScaleAtCallback = -1f;
            bool cleanupAtCallback = false;
            float letterboxAtCallback = -1f;
            Vector3 cameraCenterAtCallback = default;
            float cameraSizeAtCallback = 0f;

            Assert.That(player.CanPlay, Is.True);
            Assert.That(player.TryPlay(sequence, options, _ =>
            {
                callbackCount++;
                contextsAtCallback = inputRouter.ActiveContextCount;
                pausedAtCallback = timeScale.IsPausedByLock;
                timeScaleAtCallback = Time.timeScale;
                cleanupAtCallback = sequence.CleanupCalled;
                sequence.Order.Add("Handoff");
                letterboxAtCallback = letterbox.Reveal;
                cameraController.TryGetView(
                    out cameraCenterAtCallback,
                    out cameraSizeAtCallback);
                handoffInput = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Swallow);
                timeScale.PushPauseLock(TestPauseReason);
                handoffPauseHeld = true;
                eventSystem.SetSelectedGameObject(handoffSelection);
            }), Is.True);
            EnsureBeginStarted(sequence);

            letterbox = player.GetComponentInChildren<StrategyCinematicLetterboxView>(true);
            Assert.That(letterbox, Is.Not.Null);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
            Assert.That(inputRouter.BlockedChannels, Is.EqualTo(StrategyInputChannel.All));
            Assert.That(inputRouter.TopCancelMode, Is.EqualTo(StrategyCancelMode.Swallow));
            Assert.That(timeScale.IsPausedByLock, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.CanPlay, Is.False);
            Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
            Assert.That(letterbox.IsInputShieldActive, Is.True);
            CollectionAssert.AreEqual(new[] { "Begin" }, sequence.Order);

            cameraController.FocusOn(sequence.FocusCenter, sequence.FocusSize);
            letterbox.SetReveal(1f);
            sequence.MarkOpeningComplete();
            InvokePrivate(player, "StopPlaybackRoutine");
            DrivePlayStart(sequence);
            InvokePrivate(
                player,
                "CompletePlayback",
                StrategyInGameCinematicResult.Completed,
                true);

            Assert.That(callbackCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(
                new[] { "Begin", "Opening", "Play", "Cleanup", "Handoff" },
                sequence.Order);
            Assert.That(cleanupAtCallback, Is.True);
            Assert.That(sequence.CleanupResult, Is.EqualTo(StrategyInGameCinematicResult.Completed));
            Assert.That(contextsAtCallback, Is.Zero);
            Assert.That(pausedAtCallback, Is.False);
            Assert.That(timeScaleAtCallback, Is.EqualTo(2f));
            Assert.That(letterboxAtCallback, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(cameraCenterAtCallback.x, Is.EqualTo(sequence.FocusCenter.x).Within(0.001f));
            Assert.That(cameraCenterAtCallback.y, Is.EqualTo(sequence.FocusCenter.y).Within(0.001f));
            Assert.That(cameraSizeAtCallback, Is.EqualTo(sequence.FocusSize).Within(0.001f));
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
            Assert.That(timeScale.IsPausedByLock, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(player.CanPlay, Is.False);
            Assert.That(letterbox.Reveal, Is.Zero);
            Assert.That(letterbox.IsInputShieldActive, Is.False);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(handoffSelection));
            AssertCameraRestored();

            handoffInput.Dispose();
            handoffInput = null;
            timeScale.PopPauseLock(TestPauseReason);
            handoffPauseHeld = false;
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            Assert.That(player.CanPlay, Is.True);
            AssertCameraFocusReleased();
        }

        [Test]
        public void CancellationCleansSequenceAndReleasesEveryOwnedResource()
        {
            BlockingSequence sequence = new();
            EventSystem eventSystem = EnsureEventSystem();
            GameObject previousSelection = CreateSelectable("Cancelled Cinematic Selection");
            eventSystem.SetSelectedGameObject(previousSelection);
            int callbackCount = 0;
            Assert.That(
                player.TryPlay(sequence, CreateOpeningOptions(), _ => callbackCount++),
                Is.True);
            EnsureBeginStarted(sequence);
            letterbox = player.GetComponentInChildren<StrategyCinematicLetterboxView>(true);
            Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
            Assert.That(letterbox.IsInputShieldActive, Is.True);
            cameraController.FocusOn(sequence.FocusCenter, sequence.FocusSize);
            letterbox.SetReveal(1f);

            Assert.That(player.Cancel(sequence, false), Is.True);

            Assert.That(callbackCount, Is.Zero);
            CollectionAssert.AreEqual(new[] { "Begin", "Cleanup" }, sequence.Order);
            Assert.That(sequence.CleanupCalled, Is.True);
            Assert.That(sequence.CleanupResult, Is.EqualTo(StrategyInGameCinematicResult.Cancelled));
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(2f));
            Assert.That(letterbox.Reveal, Is.Zero);
            Assert.That(letterbox.IsInputShieldActive, Is.False);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(previousSelection));
            Assert.That(player.CanPlay, Is.True);
            AssertCameraRestored();
            AssertCameraFocusReleased();
            Assert.That(player.Cancel(sequence, false), Is.False);
        }

        private static StrategyInGameCinematicOptions CreateOpeningOptions()
        {
            return new StrategyInGameCinematicOptions(
                10f,
                0f,
                0f,
                2.39f,
                StrategyCinematicLetterboxView.DefaultMinimumBarFraction,
                0f);
        }

        private EventSystem EnsureEventSystem()
        {
            if (EventSystem.current != null && EventSystem.current.isActiveAndEnabled)
            {
                return EventSystem.current;
            }

            GameObject eventSystemObject = new("Cinematic Test EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            InvokeEventSystemLifecycle(eventSystem, "OnEnable");
            manuallyRegisteredEventSystem = eventSystem;
            EventSystem.current = eventSystem;
            return eventSystem;
        }

        private static void InvokeEventSystemLifecycle(
            EventSystem eventSystem,
            string methodName)
        {
            MethodInfo lifecycle = typeof(EventSystem).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lifecycle, Is.Not.Null, methodName);
            lifecycle.Invoke(eventSystem, null);
        }

        private GameObject CreateSelectable(string objectName)
        {
            GameObject selectable = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            selectable.transform.SetParent(root.transform, false);
            return selectable;
        }

        private void DrivePlayStart(BlockingSequence sequence)
        {
            FieldInfo contextField = typeof(StrategyInGameCinematicPlayer).GetField(
                "activeContext",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(contextField, Is.Not.Null);
            StrategyInGameCinematicContext context =
                (StrategyInGameCinematicContext)contextField.GetValue(player);
            Assert.That(context, Is.Not.Null);
            IEnumerator routine = sequence.Play(context);
            Assert.That(routine.MoveNext(), Is.True);
            (routine as System.IDisposable)?.Dispose();
        }

        private void EnsureBeginStarted(BlockingSequence sequence)
        {
            if (sequence.Order.Count == 0)
            {
                InvokePrivate(player, "TryBeginSequence");
            }
        }

        private void AssertCameraRestored()
        {
            Assert.That(
                cameraController.TryGetView(out Vector3 center, out float size),
                Is.True);
            Assert.That(center.x, Is.EqualTo(originalCameraCenter.x).Within(0.001f));
            Assert.That(center.y, Is.EqualTo(originalCameraCenter.y).Within(0.001f));
            Assert.That(size, Is.EqualTo(originalCameraSize).Within(0.001f));
        }

        private void AssertCameraFocusReleased()
        {
            Vector3 manualCenter = new(-8f, 9f, -10f);
            cameraRoot.transform.position = manualCenter;
            cameraRoot.GetComponent<Camera>().orthographicSize = 11f;
            InvokePrivate(cameraController, "LateUpdate");
            Assert.That(cameraRoot.transform.position, Is.EqualTo(manualCenter));
            Assert.That(cameraRoot.GetComponent<Camera>().orthographicSize, Is.EqualTo(11f));
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " should exist");
            method.Invoke(target, args);
        }

        private sealed class BlockingSequence : IStrategyInGameCinematicSequence
        {
            public string DebugName => "Blocking Test Sequence";
            public Vector3 FocusCenter { get; } = new(25f, 18f, 0f);
            public float FocusSize => 6f;
            public bool CleanupCalled { get; private set; }
            public StrategyInGameCinematicResult CleanupResult { get; private set; }
            public List<string> Order { get; } = new();

            public bool TryPrepare(out StrategyInGameCinematicFraming framing)
            {
                framing = new StrategyInGameCinematicFraming(
                    new Bounds(FocusCenter, new Vector3(4f, 2f, 1f)),
                    Vector2.zero,
                    FocusSize,
                    FocusSize);
                return true;
            }

            public void Begin(StrategyInGameCinematicContext context)
            {
                Order.Add("Begin");
            }

            public IEnumerator Play(StrategyInGameCinematicContext context)
            {
                Order.Add("Play");
                while (!context.IsCancellationRequested)
                {
                    yield return null;
                }
            }

            public void Cleanup(
                StrategyInGameCinematicContext context,
                StrategyInGameCinematicResult result)
            {
                CleanupCalled = true;
                CleanupResult = result;
                Order.Add("Cleanup");
            }

            public void MarkOpeningComplete()
            {
                Order.Add("Opening");
            }
        }
    }
}
