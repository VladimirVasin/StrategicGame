using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHouseAmbientAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.18f;

        private SpriteRenderer overlayRenderer;
        private int visualVariant;
        private int frameIndex;
        private float frameTimer;

        public void Configure(SpriteRenderer baseRenderer, int variant)
        {
            visualVariant = variant;
            frameIndex = Random.Range(0, StrategyHouseAmbientSpriteFactory.FrameCount);
            frameTimer = Random.Range(0f, FrameDuration);
            EnsureOverlay(baseRenderer);
            ApplyFrame();
        }

        private void Update()
        {
            if (overlayRenderer == null)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            if (frameTimer < FrameDuration)
            {
                return;
            }

            frameTimer -= FrameDuration;
            frameIndex = (frameIndex + 1) % StrategyHouseAmbientSpriteFactory.FrameCount;
            ApplyFrame();
        }

        private void EnsureOverlay(SpriteRenderer baseRenderer)
        {
            if (overlayRenderer != null)
            {
                return;
            }

            GameObject overlay = new GameObject("House Ambient Overlay");
            overlay.transform.SetParent(transform, false);
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localScale = Vector3.one;
            overlayRenderer = overlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sortingOrder = baseRenderer != null ? baseRenderer.sortingOrder + 1 : 6;
            overlayRenderer.color = Color.white;
        }

        private void ApplyFrame()
        {
            if (overlayRenderer != null)
            {
                overlayRenderer.sprite = StrategyHouseAmbientSpriteFactory.GetSprite(visualVariant, frameIndex);
            }
        }
    }
}
