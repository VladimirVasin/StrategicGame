using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyScoutLodgeCameraFocusTests
    {
        private readonly List<GameObject> roots = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                if (roots[i] != null)
                {
                    Object.DestroyImmediate(roots[i]);
                }
            }

            roots.Clear();
        }

        [Test]
        public void CompletingIntroductionRestoresCapturedViewAndReleasesFocus()
        {
            GameObject cameraRoot = CreateRoot("Test Camera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            StrategyCameraController cameraController =
                cameraRoot.AddComponent<StrategyCameraController>();
            Vector3 originalCenter = new(18f, -12f, -10f);
            const float OriginalSize = 16f;
            cameraRoot.transform.position = originalCenter;
            camera.orthographicSize = OriginalSize;

            StrategyScoutLodgeOnboardingController onboarding =
                CreateRoot("Test Scout Onboarding")
                    .AddComponent<StrategyScoutLodgeOnboardingController>();
            onboarding.Configure(null, null, cameraController, null, null, null, null);
            InvokePrivate(onboarding, "CaptureReturnCameraView");

            cameraController.FocusOnAnimated(new Vector3(47f, -66f, 0f), 7f, 0.55f);
            InvokePrivate(onboarding, "CompleteFlow");
            CompleteAnimatedFocus(cameraController);

            Assert.That(onboarding.IsActive, Is.False);
            Assert.That(cameraController.TryGetView(out Vector3 restoredCenter, out float restoredSize), Is.True);
            Assert.That(restoredCenter.x, Is.EqualTo(originalCenter.x).Within(0.001f));
            Assert.That(restoredCenter.y, Is.EqualTo(originalCenter.y).Within(0.001f));
            Assert.That(restoredSize, Is.EqualTo(OriginalSize).Within(0.001f));

            Vector3 manualCenter = new(-8f, 9f, -10f);
            cameraRoot.transform.position = manualCenter;
            camera.orthographicSize = 11f;
            InvokePrivate(cameraController, "LateUpdate");
            Assert.That(cameraRoot.transform.position, Is.EqualTo(manualCenter));
            Assert.That(camera.orthographicSize, Is.EqualTo(11f));
        }

        [Test]
        public void CompletingManualPickerDoesNotMoveCamera()
        {
            GameObject cameraRoot = CreateRoot("Manual Picker Camera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            StrategyCameraController cameraController =
                cameraRoot.AddComponent<StrategyCameraController>();
            Vector3 currentCenter = new(-4f, 13f, -10f);
            cameraRoot.transform.position = currentCenter;
            camera.orthographicSize = 12f;

            StrategyScoutLodgeOnboardingController onboarding =
                CreateRoot("Manual Scout Picker")
                    .AddComponent<StrategyScoutLodgeOnboardingController>();
            onboarding.Configure(null, null, cameraController, null, null, null, null);
            InvokePrivate(onboarding, "CompleteFlow");

            Assert.That(cameraRoot.transform.position, Is.EqualTo(currentCenter));
            Assert.That(camera.orthographicSize, Is.EqualTo(12f));
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new(name);
            roots.Add(root);
            return root;
        }

        private static void CompleteAnimatedFocus(StrategyCameraController cameraController)
        {
            FieldInfo startedAt = typeof(StrategyCameraController).GetField(
                "animatedFocusStartedAtUnscaledTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            startedAt.SetValue(cameraController, Time.unscaledTime - 10f);
            InvokePrivate(cameraController, "UpdateAnimatedFocus");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " should exist");
            method.Invoke(target, null);
        }
    }
}
