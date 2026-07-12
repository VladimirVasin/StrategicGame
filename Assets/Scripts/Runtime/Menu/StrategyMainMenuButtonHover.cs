using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyMainMenuButtonHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private const float AnimationSpeed = 9f;
        private static readonly Color DisabledColor = new(0.09f, 0.11f, 0.10f, 0.78f);
        private static readonly Color PressedColor = new(0.24f, 0.35f, 0.27f, 1f);

        private Button button;
        private Image background;
        private Image accent;
        private Text label;
        private RectTransform root;
        private Color normalColor;
        private Color hoverColor;
        private Color accentColor;
        private Vector2 basePosition;
        private float hoverT;
        private bool pointerInside;
        private bool selected;
        private bool pressed;
        private bool configured;

        public void Configure(
            Button owner,
            Image backgroundImage,
            Image accentImage,
            Text labelText,
            Color normal,
            Color hover,
            Color accentTint)
        {
            button = owner;
            background = backgroundImage;
            accent = accentImage;
            label = labelText;
            root = transform as RectTransform;
            normalColor = normal;
            hoverColor = hover;
            accentColor = accentTint;
            basePosition = root != null ? root.anchoredPosition : Vector2.zero;
            configured = button != null && background != null && accent != null && label != null && root != null;
            ApplyVisuals();
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            bool activeHover = button.interactable && (pointerInside || selected);
            hoverT = Mathf.MoveTowards(hoverT, activeHover ? 1f : 0f, Time.unscaledDeltaTime * AnimationSpeed);
            ApplyVisuals();
        }

        private void OnDisable()
        {
            pointerInside = false;
            selected = false;
            pressed = false;
            hoverT = 0f;
            ApplyVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!configured || !button.interactable || pointerInside)
            {
                return;
            }

            pointerInside = true;
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step, 0.55f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = configured && button.interactable;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pressed = false;
        }

        private void ApplyVisuals()
        {
            if (!configured)
            {
                return;
            }

            float smooth = hoverT * hoverT * (3f - 2f * hoverT);
            if (!button.interactable)
            {
                background.color = DisabledColor;
                label.color = new Color(0.62f, 0.66f, 0.63f, 0.62f);
                accent.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
                root.anchoredPosition = basePosition;
                root.localScale = Vector3.one;
                return;
            }

            background.color = pressed ? PressedColor : Color.Lerp(normalColor, hoverColor, smooth);
            label.color = Color.Lerp(Color.white, new Color(1f, 0.91f, 0.68f, 1f), smooth);
            accent.color = new Color(accentColor.r, accentColor.g, accentColor.b, smooth);
            accent.rectTransform.localScale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, smooth), 1f);
            root.anchoredPosition = basePosition + new Vector2(Mathf.Lerp(0f, pressed ? 6f : 10f, smooth), 0f);
            root.localScale = Vector3.one * Mathf.Lerp(1f, pressed ? 1.012f : 1.025f, smooth);
        }
    }
}
