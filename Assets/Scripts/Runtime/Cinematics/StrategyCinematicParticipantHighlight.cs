using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCinematicParticipantHighlight : MonoBehaviour
    {
        private const int TextureWidth = 32;
        private const int TextureHeight = 16;
        private const float PixelsPerUnit = 32f;
        private const float PulseSpeed = 4.2f;

        private static Sprite markerSprite;

        private Transform target;
        private SpriteRenderer markerRenderer;
        private Vector2 worldSize;
        private float phaseOffset;
        private bool configured;
        private bool requestedVisible = true;

        public Transform Target => target;
        public SpriteRenderer Renderer => markerRenderer;
        public bool IsVisible => markerRenderer != null && markerRenderer.enabled;

        public static StrategyCinematicParticipantHighlight Create(
            Transform parent,
            Transform participant,
            Vector2 markerWorldSize,
            float pulsePhase = 0f)
        {
            GameObject markerObject = new(
                "Cinematic Participant Highlight",
                typeof(SpriteRenderer));
            if (parent != null)
            {
                markerObject.transform.SetParent(parent, true);
            }

            StrategyCinematicParticipantHighlight highlight =
                markerObject.AddComponent<StrategyCinematicParticipantHighlight>();
            highlight.Configure(
                participant,
                markerWorldSize,
                pulsePhase,
                markerObject.GetComponent<SpriteRenderer>());
            return highlight;
        }

        public void Configure(
            Transform participant,
            Vector2 markerWorldSize,
            float pulsePhase,
            SpriteRenderer renderer)
        {
            target = participant;
            markerRenderer = renderer;
            worldSize = new Vector2(
                Mathf.Max(0.25f, markerWorldSize.x),
                Mathf.Max(0.12f, markerWorldSize.y));
            phaseOffset = pulsePhase;
            requestedVisible = true;
            configured = target != null && markerRenderer != null;
            if (markerRenderer != null)
            {
                markerRenderer.sprite = GetMarkerSprite();
                markerRenderer.color = new Color(1f, 0.78f, 0.20f, 0.58f);
            }

            ApplyVisibility();
            Refresh(0f);
        }

        public void SetVisible(bool visible)
        {
            requestedVisible = visible;
            ApplyVisibility();
        }

        internal void Refresh(float unscaledTime)
        {
            if (!configured || target == null || markerRenderer == null)
            {
                SetVisible(false);
                return;
            }

            float wave = Mathf.Sin(unscaledTime * PulseSpeed + phaseOffset);
            float pulse = 1f + wave * 0.055f;
            Vector3 targetPosition = target.position;
            transform.position = new Vector3(
                targetPosition.x,
                targetPosition.y + 0.015f,
                targetPosition.z + 0.01f);
            transform.localScale = new Vector3(
                worldSize.x * pulse,
                worldSize.y * 2f * pulse,
                1f);
            Color color = markerRenderer.color;
            color.a = 0.50f + (wave + 1f) * 0.07f;
            markerRenderer.color = color;
            StrategyWorldSorting.Apply(markerRenderer, targetPosition, -4);
        }

        private void LateUpdate()
        {
            Refresh(Time.unscaledTime);
        }

        private void OnEnable()
        {
            ApplyVisibility();
        }

        private void OnDisable()
        {
            if (markerRenderer != null)
            {
                markerRenderer.enabled = false;
            }
        }

        private void ApplyVisibility()
        {
            if (markerRenderer != null)
            {
                markerRenderer.enabled = requestedVisible && configured && target != null;
            }
        }

        private static Sprite GetMarkerSprite()
        {
            if (markerSprite != null)
            {
                return markerSprite;
            }

            Texture2D texture = new(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = "Cinematic Participant Ring",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[TextureWidth * TextureHeight]);
            Color ring = Color.white;
            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    float nx = (x - (TextureWidth - 1) * 0.5f) / (TextureWidth * 0.5f);
                    float ny = (y - (TextureHeight - 1) * 0.5f) / (TextureHeight * 0.5f);
                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    if (radius >= 0.76f && radius <= 1.02f)
                    {
                        texture.SetPixel(x, y, ring);
                    }
                }
            }

            AddCardinalTicks(texture, ring);
            texture.Apply(false, false);
            markerSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureWidth, TextureHeight),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            return markerSprite;
        }

        private static void AddCardinalTicks(Texture2D texture, Color color)
        {
            int centerX = TextureWidth / 2;
            int centerY = TextureHeight / 2;
            for (int offset = -2; offset <= 2; offset++)
            {
                texture.SetPixel(centerX + offset, 0, color);
                texture.SetPixel(centerX + offset, TextureHeight - 1, color);
                texture.SetPixel(0, centerY + offset, color);
                texture.SetPixel(TextureWidth - 1, centerY + offset, color);
            }
        }
    }
}
