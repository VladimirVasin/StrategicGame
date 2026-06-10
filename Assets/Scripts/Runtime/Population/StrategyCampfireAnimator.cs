using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCampfireAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.12f;
        private const float AmbientFrameDuration = 0.16f;

        private SpriteRenderer spriteRenderer;
        private SpriteRenderer ambientRenderer;
        private int frameIndex;
        private int ambientFrameIndex;
        private float frameTimer;
        private float ambientFrameTimer;

        public void Configure(SpriteRenderer renderer)
        {
            spriteRenderer = renderer;
            frameIndex = Random.Range(0, StrategyCampfireSpriteFactory.FrameCount);
            ambientFrameIndex = Random.Range(0, StrategyCampfireAmbientSpriteFactory.FrameCount);
            frameTimer = Random.Range(0f, FrameDuration);
            ambientFrameTimer = Random.Range(0f, AmbientFrameDuration);
            EnsureAmbientRenderer();
            ApplyFrame();
            ApplyAmbientFrame();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            while (frameTimer >= FrameDuration)
            {
                frameTimer -= FrameDuration;
                frameIndex = (frameIndex + 1) % StrategyCampfireSpriteFactory.FrameCount;
                ApplyFrame();
            }

            EnsureAmbientRenderer();
            ambientFrameTimer += Time.deltaTime;
            while (ambientFrameTimer >= AmbientFrameDuration)
            {
                ambientFrameTimer -= AmbientFrameDuration;
                ambientFrameIndex = (ambientFrameIndex + 1) % StrategyCampfireAmbientSpriteFactory.FrameCount;
                ApplyAmbientFrame();
            }
        }

        private void ApplyFrame()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = StrategyCampfireSpriteFactory.GetFrame(frameIndex);
            }
        }

        private void EnsureAmbientRenderer()
        {
            if (ambientRenderer != null)
            {
                return;
            }

            GameObject ambient = new GameObject("Campfire Smoke Sparks");
            ambient.transform.SetParent(transform, false);
            ambient.transform.localPosition = Vector3.zero;
            ambient.transform.localScale = Vector3.one;
            ambientRenderer = ambient.AddComponent<SpriteRenderer>();
            ambientRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 7;
            ambientRenderer.color = Color.white;
        }

        private void ApplyAmbientFrame()
        {
            if (ambientRenderer != null)
            {
                ambientRenderer.sprite = StrategyCampfireAmbientSpriteFactory.GetFrame(ambientFrameIndex);
            }
        }
    }
}
