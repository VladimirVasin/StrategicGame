using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const int HudCaptureSortingBase = 30000;

        private static void CaptureGameplayRender(string fileName)
        {
            CaptureGameplayRender(fileName, 1600, 900);
        }

        private static void CaptureGameplayRender(string fileName, int width, int height)
        {
            Require(
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null,
                "Gameplay visual capture requires a graphics device");
            Camera camera = Camera.main;
            Require(camera != null, "Gameplay camera missing");

            RenderTexture renderTexture = new(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            List<OverlayCanvasCaptureState> states = new();
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                states = PrepareOverlayCanvasesForCapture(camera);
                camera.Render();
                screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                screenshot.Apply(false, false);
                File.WriteAllBytes(GetResultPath(fileName), screenshot.EncodeToPNG());
            }
            finally
            {
                RestoreOverlayCanvases(states);
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(screenshot);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static List<OverlayCanvasCaptureState> PrepareOverlayCanvasesForCapture(Camera camera)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            List<OverlayCanvasCaptureState> states = new();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                states.Add(new OverlayCanvasCaptureState(
                    canvas,
                    canvas.worldCamera,
                    canvas.planeDistance,
                    canvas.overrideSorting,
                    canvas.sortingOrder));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.overrideSorting = true;
                canvas.sortingOrder = HudCaptureSortingBase + Mathf.Clamp(canvas.sortingOrder, -500, 500);
            }

            Canvas.ForceUpdateCanvases();
            StrategyHudTooltipPresenter.RefreshVisible();
            Canvas.ForceUpdateCanvases();
            return states;
        }

        private static void RestoreOverlayCanvases(List<OverlayCanvasCaptureState> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                OverlayCanvasCaptureState state = states[i];
                if (state.Canvas == null)
                {
                    continue;
                }

                state.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                state.Canvas.worldCamera = state.WorldCamera;
                state.Canvas.planeDistance = state.PlaneDistance;
                state.Canvas.overrideSorting = state.OverrideSorting;
                state.Canvas.sortingOrder = state.SortingOrder;
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void CaptureProfessionHudFrames()
        {
            StrategyProfessionHudController professions =
                UnityEngine.Object.FindAnyObjectByType<StrategyProfessionHudController>();
            Require(professions != null, "Profession HUD missing");
            UnityEngine.UI.Button toggle = FindCaptureComponent<UnityEngine.UI.Button>(
                professions.gameObject,
                "ProfessionButton");
            Require(toggle != null, "Profession HUD button missing");
            System.Reflection.MethodInfo updateAnimation =
                typeof(StrategyProfessionHudController).GetMethod(
                    "UpdateAnimation",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Require(updateAnimation != null, "Profession HUD animation hook missing");

            bool opened = false;
            try
            {
                toggle.onClick.Invoke();
                opened = true;
                updateAnimation.Invoke(professions, new object[] { true });
                Canvas.ForceUpdateCanvases();
                CaptureGameplayRender("VisualProfessions_1280x720.png", 1280, 720);
                CaptureGameplayRender("VisualProfessions_1484x839.png", 1484, 839);
            }
            finally
            {
                if (opened)
                {
                    toggle.onClick.Invoke();
                    updateAnimation.Invoke(professions, new object[] { true });
                }
            }

            CaptureBuildingHudMatrix();
        }

        private readonly struct OverlayCanvasCaptureState
        {
            public OverlayCanvasCaptureState(
                Canvas canvas,
                Camera worldCamera,
                float planeDistance,
                bool overrideSorting,
                int sortingOrder)
            {
                Canvas = canvas;
                WorldCamera = worldCamera;
                PlaneDistance = planeDistance;
                OverrideSorting = overrideSorting;
                SortingOrder = sortingOrder;
            }

            public Canvas Canvas { get; }
            public Camera WorldCamera { get; }
            public float PlaneDistance { get; }
            public bool OverrideSorting { get; }
            public int SortingOrder { get; }
        }
    }
}
