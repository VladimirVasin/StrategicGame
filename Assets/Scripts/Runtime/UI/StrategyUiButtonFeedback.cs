using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public enum StrategyUiButtonFeedbackProfile
    {
        Standard,
        Compact,
        SoundOnly,
        Cinematic
    }

    [DisallowMultipleComponent]
    public sealed class StrategyUiButtonFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private const float LocalCueCooldown = 0.10f;
        private const string ReducedMotionKey = "ProjectUnknown.Founding.ReducedMotion";

        private Button button;
        private RectTransform root;
        private Image tintOverlay;
        private StrategyUiButtonFeedbackProfile profile;
        private StrategyHudSfxKind? clickSfx;
        private Vector2 basePosition;
        private Vector3 baseScale = Vector3.one;
        private float hoverT;
        private float focusT;
        private float pressT;
        private float nextCueTime;
        private bool pointerInside;
        private bool pointerSelected;
        private bool selected;
        private bool pressed;
        private bool configured;
        private bool reducedMotion;
        private bool suppressNextFocusCue;

        public static StrategyUiButtonFeedback Attach(
            Button button,
            StrategyUiButtonFeedbackProfile profile = StrategyUiButtonFeedbackProfile.Standard,
            StrategyHudSfxKind? clickSfx = null)
        {
            if (button == null)
            {
                return null;
            }

            StrategyUiButtonFeedback feedback = button.GetComponent<StrategyUiButtonFeedback>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<StrategyUiButtonFeedback>();
            }

            feedback.Configure(button, profile, clickSfx);
            return feedback;
        }

        private void Configure(
            Button owner,
            StrategyUiButtonFeedbackProfile feedbackProfile,
            StrategyHudSfxKind? clickKind)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayConfiguredClick);
            }

            button = owner;
            root = owner.transform as RectTransform;
            profile = feedbackProfile;
            clickSfx = clickKind;
            reducedMotion = PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;
            configured = button != null && root != null;
            if (!configured)
            {
                return;
            }

            basePosition = root.anchoredPosition;
            baseScale = root.localScale;
            if (button.targetGraphic == null)
            {
                button.targetGraphic = button.GetComponent<Graphic>();
            }

            EnsureTintOverlay();
            NormalizeNativeSelectionVisual();
            if (clickSfx.HasValue)
            {
                button.onClick.AddListener(PlayConfiguredClick);
            }

            ApplyVisuals();
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            bool interactable = button != null && button.IsInteractable();
            if (!interactable)
            {
                ResetInteractionState();
            }

            if (profile == StrategyUiButtonFeedbackProfile.SoundOnly)
            {
                return;
            }

            float hoverTarget = interactable && pointerInside ? 1f : 0f;
            float focusTarget = interactable && selected && !pointerInside ? 1f : 0f;
            float pressTarget = interactable && pressed ? 1f : 0f;
            if (reducedMotion)
            {
                hoverT = hoverTarget;
                focusT = focusTarget;
                pressT = pressTarget;
            }
            else
            {
                float delta = Time.unscaledDeltaTime * GetAnimationSpeed();
                hoverT = Mathf.MoveTowards(hoverT, hoverTarget, delta);
                focusT = Mathf.MoveTowards(focusT, focusTarget, delta);
                pressT = Mathf.MoveTowards(pressT, pressTarget, delta * 1.35f);
            }

            ApplyVisuals();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            ApplyVisuals();
        }

        public void SuppressNextFocusCue()
        {
            suppressNextFocusCue = true;
        }

        private void OnDisable()
        {
            ResetInteractionState();
            ApplyVisuals();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayConfiguredClick);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!configured || button == null || !button.IsInteractable() || pointerInside)
            {
                return;
            }

            pointerInside = true;
            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                pointerSelected = true;
                selected = false;
            }

            TryPlayHoverCue();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pressed = false;
            bool clearPointerSelection = pointerSelected
                && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject;
            pointerSelected = false;
            if (clearPointerSelection)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = configured
                && button != null
                && button.IsInteractable()
                && eventData.button == PointerEventData.InputButton.Left;
            if (pressed)
            {
                pointerSelected = true;
                selected = false;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!configured || button == null || !button.IsInteractable())
            {
                return;
            }

            bool pointerOrigin = eventData is PointerEventData;
            pointerSelected = pointerOrigin;
            selected = !pointerOrigin;
            if (selected)
            {
                if (suppressNextFocusCue)
                {
                    suppressNextFocusCue = false;
                }
                else
                {
                    TryPlayHoverCue();
                }
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pointerSelected = false;
            pressed = false;
        }

        private void TryPlayHoverCue()
        {
            float now = Time.unscaledTime;
            if (now < nextCueTime)
            {
                return;
            }

            nextCueTime = now + LocalCueCooldown;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Hover);
        }

        private void PlayConfiguredClick()
        {
            if (clickSfx.HasValue)
            {
                StrategyHudSfxAudio.Play(clickSfx.Value);
            }
        }

        private void EnsureTintOverlay()
        {
            bool wantsVisuals = profile != StrategyUiButtonFeedbackProfile.SoundOnly
                && button.targetGraphic is Image;
            if (!wantsVisuals)
            {
                if (tintOverlay != null)
                {
                    tintOverlay.gameObject.SetActive(false);
                }

                return;
            }

            if (tintOverlay == null)
            {
                GameObject overlayObject = new GameObject(
                    "ButtonFeedbackTint",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
                overlayRect.SetParent(root, false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                overlayRect.SetAsFirstSibling();
                tintOverlay = overlayObject.GetComponent<Image>();
                tintOverlay.raycastTarget = false;
            }

            if (button.targetGraphic is Image targetImage)
            {
                tintOverlay.sprite = targetImage.sprite;
                tintOverlay.type = targetImage.type;
                tintOverlay.preserveAspect = targetImage.preserveAspect;
                tintOverlay.fillCenter = targetImage.fillCenter;
                tintOverlay.pixelsPerUnitMultiplier = targetImage.pixelsPerUnitMultiplier;
            }

            tintOverlay.gameObject.SetActive(true);
        }

        private void NormalizeNativeSelectionVisual()
        {
            if (profile == StrategyUiButtonFeedbackProfile.SoundOnly
                || button.transition != Selectable.Transition.ColorTint)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.selectedColor = tintOverlay != null
                ? colors.normalColor
                : Color.Lerp(colors.normalColor, colors.highlightedColor, 0.35f);
            button.colors = colors;
        }

        private void ApplyVisuals()
        {
            if (!configured || root == null)
            {
                return;
            }

            if (profile == StrategyUiButtonFeedbackProfile.SoundOnly)
            {
                return;
            }

            float hover = Smooth01(hoverT);
            float focus = Smooth01(focusT);
            float press = Smooth01(pressT);
            SyncTintShape();
            if (reducedMotion)
            {
                root.localScale = baseScale;
                root.anchoredPosition = basePosition;
                ApplyTint(hover, focus, press);
                return;
            }

            float scale = Mathf.Lerp(1f, GetHoverScale(), hover);
            scale = Mathf.Lerp(scale, GetPressedScale(), press);
            root.localScale = baseScale * scale;

            float y = Mathf.Lerp(0f, GetHoverOffset(), hover);
            y = Mathf.Lerp(y, -GetPressOffset(), press);
            root.anchoredPosition = basePosition + new Vector2(0f, y);

            if (tintOverlay == null)
            {
                return;
            }

            ApplyTint(hover, focus, press);
        }

        private void SyncTintShape()
        {
            if (tintOverlay == null || button.targetGraphic is not Image targetImage)
            {
                return;
            }

            tintOverlay.sprite = targetImage.sprite;
            tintOverlay.type = targetImage.type;
            tintOverlay.preserveAspect = targetImage.preserveAspect;
            tintOverlay.fillCenter = targetImage.fillCenter;
            tintOverlay.pixelsPerUnitMultiplier = targetImage.pixelsPerUnitMultiplier;
        }

        private void ApplyTint(float hover, float focus, float press)
        {
            if (tintOverlay == null)
            {
                return;
            }

            Color hoverTint = GetHoverTint();
            Color pressTint = new Color(0f, 0f, 0f, 0.12f);
            Color tint = Color.Lerp(hoverTint, pressTint, press);
            float focusStrength = focus * 0.42f;
            tint.a = Mathf.Max(hoverTint.a * Mathf.Max(hover, focusStrength), pressTint.a * press);
            tintOverlay.color = tint;
        }

        private void ResetInteractionState()
        {
            if (EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            pointerInside = false;
            pointerSelected = false;
            selected = false;
            pressed = false;
            hoverT = 0f;
            focusT = 0f;
            pressT = 0f;
        }

        private float GetAnimationSpeed()
        {
            return profile switch
            {
                StrategyUiButtonFeedbackProfile.Compact => 15f,
                StrategyUiButtonFeedbackProfile.Cinematic => 9f,
                _ => 12f
            };
        }

        private float GetHoverScale()
        {
            return profile switch
            {
                StrategyUiButtonFeedbackProfile.Compact => 1.016f,
                StrategyUiButtonFeedbackProfile.Cinematic => 1.032f,
                _ => 1.024f
            };
        }

        private float GetPressedScale()
        {
            return profile == StrategyUiButtonFeedbackProfile.Compact ? 0.975f : 0.97f;
        }

        private float GetHoverOffset()
        {
            return profile switch
            {
                StrategyUiButtonFeedbackProfile.Compact => 0.75f,
                StrategyUiButtonFeedbackProfile.Cinematic => 2f,
                _ => 1.25f
            };
        }

        private float GetPressOffset()
        {
            return profile == StrategyUiButtonFeedbackProfile.Compact ? 0.5f : 1f;
        }

        private Color GetHoverTint()
        {
            return profile switch
            {
                StrategyUiButtonFeedbackProfile.Compact => new Color(1f, 1f, 1f, 0.075f),
                StrategyUiButtonFeedbackProfile.Cinematic => new Color(1f, 0.78f, 0.38f, 0.13f),
                _ => new Color(1f, 0.88f, 0.58f, 0.10f)
            };
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
