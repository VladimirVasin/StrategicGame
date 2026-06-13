using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCampfireAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.12f;
        private const float AmbientFrameDuration = 0.16f;
        private const float BurnoutDelaySeconds = 120f;
        private const float BurnoutDurationSeconds = 90f;
        private const float ExtinguishedScale = 0.12f;

        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer ambientRenderer;
        private Vector2Int blockedCell;
        private Vector3 baseScale = Vector3.one;
        private int frameIndex;
        private int ambientFrameIndex;
        private float frameTimer;
        private float ambientFrameTimer;
        private float burnAge;
        private bool hasBlockedCell;
        private bool walkabilityReleased;
        private bool burnoutStartedLogged;
        private bool destroyingAfterBurnout;

        public void Configure(SpriteRenderer renderer, CityMapController mapController, Vector2Int occupiedCell)
        {
            spriteRenderer = renderer;
            map = mapController;
            blockedCell = occupiedCell;
            hasBlockedCell = map != null;
            baseScale = transform.localScale;
            frameIndex = Random.Range(0, StrategyCampfireSpriteFactory.FrameCount);
            ambientFrameIndex = Random.Range(0, StrategyCampfireAmbientSpriteFactory.FrameCount);
            frameTimer = Random.Range(0f, FrameDuration);
            ambientFrameTimer = Random.Range(0f, AmbientFrameDuration);
            burnAge = 0f;
            walkabilityReleased = false;
            burnoutStartedLogged = false;
            destroyingAfterBurnout = false;
            BlockCampCell();
            EnsureAmbientRenderer();
            ApplyFrame();
            ApplyAmbientFrame();
            ApplyBurnoutVisuals();
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
            if (spriteRenderer == null || destroyingAfterBurnout)
            {
                return;
            }

            burnAge += Time.deltaTime;
            if (TryFinishBurnout())
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

            ApplyBurnoutVisuals();
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

        private void BlockCampCell()
        {
            if (!hasBlockedCell)
            {
                return;
            }

            map.SetCellsWalkable(blockedCell, Vector2Int.one, false);
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireCellBlocked",
                StrategyDebugLogger.F("cell", blockedCell),
                StrategyDebugLogger.F("burnoutDelaySeconds", BurnoutDelaySeconds),
                StrategyDebugLogger.F("burnoutDurationSeconds", BurnoutDurationSeconds));
        }

        private bool TryFinishBurnout()
        {
            if (burnAge < BurnoutDelaySeconds + BurnoutDurationSeconds)
            {
                return false;
            }

            ReleaseCampCell();
            destroyingAfterBurnout = true;
            if (ambientRenderer != null)
            {
                ambientRenderer.enabled = false;
            }

            spriteRenderer.enabled = false;
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireExtinguished",
                StrategyDebugLogger.F("cell", blockedCell),
                StrategyDebugLogger.F("elapsedSeconds", burnAge));
            Destroy(gameObject);
            return true;
        }

        private void ReleaseCampCell()
        {
            if (!hasBlockedCell || walkabilityReleased)
            {
                return;
            }

            map.SetCellsWalkable(blockedCell, Vector2Int.one, true);
            walkabilityReleased = true;
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireCellReleased",
                StrategyDebugLogger.F("cell", blockedCell));
        }

        private void ApplyBurnoutVisuals()
        {
            float burnoutT = Mathf.InverseLerp(
                BurnoutDelaySeconds,
                BurnoutDelaySeconds + BurnoutDurationSeconds,
                burnAge);
            burnoutT = Mathf.Clamp01(burnoutT);

            if (burnoutT > 0f && !burnoutStartedLogged)
            {
                burnoutStartedLogged = true;
                StrategyDebugLogger.Info(
                    "Campfire",
                    "CampfireBurnoutStarted",
                    StrategyDebugLogger.F("cell", blockedCell),
                    StrategyDebugLogger.F("elapsedSeconds", burnAge));
            }

            float alpha = 1f - burnoutT;
            float warmGreen = Mathf.Lerp(1f, 0.58f, burnoutT);
            float warmBlue = Mathf.Lerp(1f, 0.30f, burnoutT);
            spriteRenderer.color = new Color(1f, warmGreen, warmBlue, alpha);
            transform.localScale = baseScale * Mathf.Lerp(1f, ExtinguishedScale, burnoutT);

            if (ambientRenderer != null)
            {
                float ambientAlpha = Mathf.Lerp(1f, 0f, burnoutT);
                ambientRenderer.color = new Color(1f, 1f, 1f, ambientAlpha);
            }
        }
    }
}
