using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyFoundingJourneyPresentation : MonoBehaviour
    {
        private readonly struct ShotProfile
        {
            public ShotProfile(Vector2 start, Vector2 end, float startScale, float endScale, float duration)
            {
                Start = start;
                End = end;
                StartScale = startScale;
                EndScale = endScale;
                Duration = duration;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
            public float StartScale { get; }
            public float EndScale { get; }
            public float Duration { get; }
        }

        private static readonly float[] RevealStarts = { 0.08f, 0.24f, 0.50f, 0.80f, 0.86f, 0.94f };
        private static readonly float[] RevealOffsets = { 6f, 10f, 12f, 4f, 10f, 10f };

        private Image backgroundA;
        private Image backgroundB;
        private RectTransform atmosphereLayer;
        private StrategyFoundingJourneyAtmosphere atmosphereController;
        private CanvasGroup atmosphereGroup;
        private CanvasGroup curtain;
        private CanvasGroup[] revealGroups;
        private Vector2[] revealPositions;
        private Image activeImage;
        private RectTransform shotTarget;
        private ShotProfile shot;
        private Vector2 activeShotStart;
        private float activeShotStartScale = 1f;
        private string currentPath;
        private float shotStarted;
        private bool backgroundAActive = true;
        private bool reducedMotion;
        private bool configured;
        private bool curtainRevealed;
        private bool atmosphereTransitionActive;
        private bool atmosphereSwitched;
        private StrategyFoundingAtmosphere pendingAtmosphere;
        private Coroutine backgroundFadeRoutine;
        private Coroutine revealRoutine;
        private Coroutine curtainRoutine;

        public void Configure(
            Image firstBackground,
            Image secondBackground,
            RectTransform sceneAtmosphere,
            CanvasGroup sceneCurtain,
            CanvasGroup[] storyRevealGroups,
            bool reduceMotion)
        {
            backgroundA = firstBackground;
            backgroundB = secondBackground;
            atmosphereLayer = sceneAtmosphere;
            atmosphereController = sceneAtmosphere != null
                ? sceneAtmosphere.GetComponent<StrategyFoundingJourneyAtmosphere>()
                : null;
            atmosphereGroup = sceneAtmosphere != null
                ? sceneAtmosphere.gameObject.AddComponent<CanvasGroup>()
                : null;
            curtain = sceneCurtain;
            revealGroups = storyRevealGroups;
            reducedMotion = reduceMotion;
            configured = backgroundA != null && backgroundB != null;
            revealPositions = new Vector2[revealGroups != null ? revealGroups.Length : 0];
            for (int i = 0; i < revealPositions.Length; i++)
            {
                revealPositions[i] = revealGroups[i].transform is RectTransform rect
                    ? rect.anchoredPosition
                    : Vector2.zero;
                SetGroupVisible(i, false);
            }

            if (curtain != null)
            {
                curtain.alpha = 1f;
                curtain.blocksRaycasts = true;
                curtain.interactable = false;
            }
        }

        internal void ShowBackground(StrategyFoundingStoryPanel panel, bool immediate)
        {
            if (!configured || currentPath == panel.ResourcePath)
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(panel.ResourcePath);
            if (sprite == null)
            {
                StrategyDebugLogger.Warn(
                    "FoundingJourney",
                    "StoryArtMissing",
                    StrategyDebugLogger.F("path", panel.ResourcePath));
                if (activeImage == null)
                {
                    RevealCurtain();
                }

                return;
            }

            currentPath = panel.ResourcePath;
            shot = GetShot(panel.Shot);
            if (backgroundFadeRoutine != null)
            {
                CompleteBackgroundFade();
            }

            if (immediate || activeImage == null)
            {
                AssignSprite(backgroundA, sprite);
                backgroundA.color = Color.white;
                backgroundB.color = TransparentWhite();
                backgroundAActive = true;
                activeImage = backgroundA;
                BeginShot(GetShotTarget(backgroundA), true);
                SetAtmosphereImmediate(panel.Atmosphere);
                RevealCurtain();
                return;
            }

            Image incoming = backgroundAActive ? backgroundB : backgroundA;
            AssignSprite(incoming, sprite);
            incoming.color = TransparentWhite();
            PrepareAtmosphereTransition(panel.Atmosphere);
            BeginShot(GetShotTarget(incoming), false);
            backgroundFadeRoutine = StartCoroutine(FadeBackground(incoming));
        }

        public void RevealStory()
        {
            if (revealGroups == null || revealGroups.Length == 0)
            {
                return;
            }

            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
            }

            revealRoutine = StartCoroutine(RevealStoryRoutine());
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (!configured)
            {
                return;
            }

            if (value)
            {
                CompleteBackgroundFade();
                if (revealRoutine != null)
                {
                    StopCoroutine(revealRoutine);
                    revealRoutine = null;
                }

                if (curtainRoutine != null)
                {
                    StopCoroutine(curtainRoutine);
                    curtainRoutine = null;
                }

                for (int i = 0; i < revealPositions.Length; i++)
                {
                    SetGroupVisible(i, true);
                }

                if (curtain != null)
                {
                    curtain.alpha = 0f;
                    curtain.blocksRaycasts = false;
                }

                ApplyNeutral(GetShotTarget(backgroundA));
                ApplyNeutral(GetShotTarget(backgroundB));
            }
            else
            {
                if (shotTarget != null)
                {
                    activeShotStart = shotTarget.anchoredPosition;
                    activeShotStartScale = shotTarget.localScale.x;
                }

                shotStarted = Time.unscaledTime;
                ApplyShot(shotTarget, 0f);
            }
        }

        private void Update()
        {
            if (!configured || reducedMotion || shotTarget == null)
            {
                return;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - shotStarted) / Mathf.Max(0.01f, shot.Duration));
            ApplyShot(shotTarget, Mathf.SmoothStep(0f, 1f, progress));
        }

        private IEnumerator FadeBackground(Image incoming)
        {
            Image outgoing = backgroundAActive ? backgroundA : backgroundB;
            float duration = reducedMotion ? 0.08f : 0.72f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                incoming.color = WithAlpha(Color.white, progress);
                outgoing.color = WithAlpha(Color.white, 1f - progress);
                UpdateAtmosphereTransition(progress, GetShotTarget(incoming));
                yield return null;
            }

            incoming.color = Color.white;
            outgoing.color = TransparentWhite();
            backgroundAActive = !backgroundAActive;
            activeImage = incoming;
            CompleteAtmosphereTransition(GetShotTarget(incoming));
            backgroundFadeRoutine = null;
        }

        private IEnumerator RevealStoryRoutine()
        {
            float duration = reducedMotion ? 0.14f : 1.34f;
            for (int i = 0; i < revealPositions.Length; i++)
            {
                SetGroupVisible(i, false);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < revealGroups.Length; i++)
                {
                    float start = reducedMotion ? 0f : RevealStarts[Mathf.Min(i, RevealStarts.Length - 1)];
                    float localDuration = reducedMotion ? duration : 0.34f;
                    float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - start) / localDuration));
                    CanvasGroup group = revealGroups[i];
                    group.alpha = progress;
                    group.interactable = progress >= 0.98f;
                    group.blocksRaycasts = progress >= 0.98f;
                    if (group.transform is RectTransform rect)
                    {
                        float offset = reducedMotion ? 0f : RevealOffsets[Mathf.Min(i, RevealOffsets.Length - 1)];
                        rect.anchoredPosition = revealPositions[i] + Vector2.down * offset * (1f - progress);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < revealPositions.Length; i++)
            {
                SetGroupVisible(i, true);
            }

            revealRoutine = null;
        }

        private void RevealCurtain()
        {
            if (curtainRevealed || curtain == null)
            {
                return;
            }

            curtainRevealed = true;
            curtainRoutine = StartCoroutine(FadeCurtain());
        }

        private IEnumerator FadeCurtain()
        {
            float duration = reducedMotion ? 0.10f : 1.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                curtain.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            curtain.alpha = 0f;
            curtain.blocksRaycasts = false;
            curtainRoutine = null;
        }

        private void BeginShot(RectTransform target, bool bindAtmosphere)
        {
            shotTarget = target;
            target?.SetAsLastSibling();
            if (bindAtmosphere)
            {
                BindAtmosphere(target);
            }

            activeShotStart = shot.Start;
            activeShotStartScale = shot.StartScale;
            shotStarted = Time.unscaledTime;
            if (reducedMotion)
            {
                ApplyNeutral(target);
            }
            else
            {
                ApplyShot(target, 0f);
            }
        }

        private void CompleteBackgroundFade()
        {
            if (backgroundFadeRoutine == null)
            {
                return;
            }

            StopCoroutine(backgroundFadeRoutine);
            Image outgoing = backgroundAActive ? backgroundA : backgroundB;
            Image incoming = backgroundAActive ? backgroundB : backgroundA;
            incoming.color = Color.white;
            outgoing.color = TransparentWhite();
            backgroundAActive = !backgroundAActive;
            activeImage = incoming;
            CompleteAtmosphereTransition(GetShotTarget(incoming));
            backgroundFadeRoutine = null;
        }

        private void SetAtmosphereImmediate(StrategyFoundingAtmosphere value)
        {
            atmosphereController?.ConfigureAtmosphere(value);
            atmosphereTransitionActive = false;
            atmosphereSwitched = true;
            if (atmosphereGroup != null)
            {
                atmosphereGroup.alpha = 1f;
            }
        }

        private void PrepareAtmosphereTransition(StrategyFoundingAtmosphere value)
        {
            pendingAtmosphere = value;
            atmosphereTransitionActive = true;
            atmosphereSwitched = false;
        }

        private void UpdateAtmosphereTransition(float progress, RectTransform incomingTarget)
        {
            if (!atmosphereTransitionActive || atmosphereGroup == null)
            {
                return;
            }

            if (progress < 0.5f)
            {
                atmosphereGroup.alpha = 1f - progress * 2f;
                return;
            }

            if (!atmosphereSwitched)
            {
                BindAtmosphere(incomingTarget);
                atmosphereController?.ConfigureAtmosphere(pendingAtmosphere);
                atmosphereSwitched = true;
            }

            atmosphereGroup.alpha = (progress - 0.5f) * 2f;
        }

        private void CompleteAtmosphereTransition(RectTransform incomingTarget)
        {
            if (atmosphereTransitionActive && !atmosphereSwitched)
            {
                BindAtmosphere(incomingTarget);
                atmosphereController?.ConfigureAtmosphere(pendingAtmosphere);
            }

            atmosphereTransitionActive = false;
            atmosphereSwitched = true;
            if (atmosphereGroup != null)
            {
                atmosphereGroup.alpha = 1f;
            }
        }

        private void BindAtmosphere(RectTransform target)
        {
            if (target == null || atmosphereLayer == null)
            {
                return;
            }

            RectTransform artwork = target.childCount > 0
                ? target.GetChild(0) as RectTransform
                : target;
            atmosphereLayer.SetParent(artwork != null ? artwork : target, false);
            atmosphereLayer.anchorMin = Vector2.zero;
            atmosphereLayer.anchorMax = Vector2.one;
            atmosphereLayer.offsetMin = Vector2.zero;
            atmosphereLayer.offsetMax = Vector2.zero;
            atmosphereLayer.localRotation = Quaternion.identity;
            atmosphereLayer.localScale = Vector3.one;
            atmosphereLayer.SetAsLastSibling();
        }

        private void SetGroupVisible(int index, bool visible)
        {
            CanvasGroup group = revealGroups[index];
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (group.transform is RectTransform rect)
            {
                rect.anchoredPosition = revealPositions[index];
            }
        }

        private void ApplyShot(RectTransform target, float progress)
        {
            if (target == null)
            {
                return;
            }

            target.anchoredPosition = Vector2.LerpUnclamped(activeShotStart, shot.End, progress);
            float scale = Mathf.LerpUnclamped(activeShotStartScale, shot.EndScale, progress);
            target.localScale = new Vector3(scale, scale, 1f);
        }

        private static void ApplyNeutral(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            target.anchoredPosition = Vector2.zero;
            target.localScale = Vector3.one;
        }

        private static ShotProfile GetShot(StrategyFoundingShot value)
        {
            return value switch
            {
                StrategyFoundingShot.Departure => new ShotProfile(new Vector2(28f, 5f), new Vector2(-22f, -3f), 1.035f, 1.085f, 18f),
                StrategyFoundingShot.LongRoad => new ShotProfile(new Vector2(18f, -7f), new Vector2(-34f, 9f), 1.035f, 1.080f, 19f),
                StrategyFoundingShot.QuietValley => new ShotProfile(new Vector2(-18f, -5f), new Vector2(14f, 3f), 1.085f, 1.025f, 20f),
                _ => new ShotProfile(new Vector2(2f, -4f), new Vector2(-30f, 5f), 1.040f, 1.075f, 20f)
            };
        }

        private static Color TransparentWhite()
        {
            return new Color(1f, 1f, 1f, 0f);
        }

        private static void AssignSprite(Image target, Sprite sprite)
        {
            target.sprite = sprite;
            AspectRatioFitter fitter = target.GetComponent<AspectRatioFitter>();
            if (fitter != null && sprite.rect.height > 0f)
            {
                fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
        }

        private static RectTransform GetShotTarget(Image target)
        {
            return target != null ? target.transform.parent as RectTransform : null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
