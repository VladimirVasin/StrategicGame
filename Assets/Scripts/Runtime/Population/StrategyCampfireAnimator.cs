using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyCampfireAnimator : MonoBehaviour
    {
        private const float FrameDuration = 0.12f;
        private const float AmbientFrameDuration = 0.16f;
        private const float BurnoutDelaySeconds = 120f;
        private const float BurnoutDurationSeconds = 90f;
        private const float ExtinguishedFlameScale = 0.12f;

        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer flameRenderer;
        private SpriteRenderer ambientRenderer;
        private Vector2Int blockedCell;
        private Vector3 baseScale = Vector3.one;
        private int frameIndex;
        private int ambientFrameIndex;
        private float frameTimer;
        private float ambientFrameTimer;
        private float burnAge;
        private float relightTimer;
        private float relightDuration;
        private bool hasBlockedCell;
        private bool walkabilityReleased;
        private bool burnoutStartedLogged;
        private bool extinguished;
        private bool relighting;
        private bool initialDaylightFireActive;
        private int daylightExtinguishedDayIndex = -1;

        public bool IsLit => !extinguished && !relighting;
        public bool IsRelighting => relighting;
        public bool NeedsRelight => extinguished && !relighting;
        public float LightIntensityFactor
        {
            get
            {
                if (relighting)
                {
                    return Mathf.Lerp(0.18f, 1f, Mathf.Clamp01(relightTimer / Mathf.Max(0.01f, relightDuration)));
                }

                if (extinguished)
                {
                    StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
                    return daylightExtinguishedDayIndex == snapshot.DayIndex
                        ? StrategyCinematicVisualMath.DawnToNoonFadeOutFactor(snapshot.DayPhase) * 0.42f
                        : 0f;
                }

                float burnoutT = Mathf.InverseLerp(BurnoutDelaySeconds, BurnoutDelaySeconds + BurnoutDurationSeconds, burnAge);
                float intensity = Mathf.Lerp(1f, 0.22f, Mathf.Clamp01(burnoutT));
                return initialDaylightFireActive
                    ? intensity * StrategyCinematicVisualMath.DawnToNoonFadeOutFactor(StrategyDayNightCycleController.CurrentDayPhase)
                    : intensity;
            }
        }

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
            relightTimer = 0f;
            relightDuration = 0f;
            initialDaylightFireActive = IsInitialDaylightFireTime();
            walkabilityReleased = !IsNightFireTime() && !initialDaylightFireActive;
            burnoutStartedLogged = false;
            daylightExtinguishedDayIndex = -1;
            extinguished = walkabilityReleased;
            relighting = false;
            if (!extinguished)
            {
                BlockCampCell();
            }

            EnsureAmbientRenderer();
            if (extinguished)
            {
                ApplyExtinguishedVisuals();
            }
            else
            {
                ApplyFrame();
                ApplyAmbientFrame();
                ApplyBurnoutVisuals();
            }
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

            if (!IsNightFireTime())
            {
                if (initialDaylightFireActive && IsInitialDaylightFireTime())
                {
                    UpdateInitialDaylightFire();
                    return;
                }

                ExtinguishForDaylight();
                UpdateExtinguishedEmbers();
                return;
            }

            if (relighting)
            {
                UpdateRelight();
                return;
            }

            if (extinguished)
            {
                UpdateExtinguishedEmbers();
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
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = StrategyCampfireSpriteFactory.GetBaseFrame(frameIndex);
                spriteRenderer.color = Color.white;
            }

            EnsureFlameRenderer();
            if (flameRenderer != null)
            {
                flameRenderer.enabled = true;
                flameRenderer.sprite = StrategyCampfireSpriteFactory.GetFlameFrame(frameIndex);
                flameRenderer.color = Color.white;
                flameRenderer.transform.localScale = Vector3.one;
            }
            transform.localScale = baseScale;
        }

        private void EnsureFlameRenderer()
        {
            if (flameRenderer != null || spriteRenderer == null)
            {
                return;
            }

            GameObject flame = new GameObject("Campfire Flame");
            flame.transform.SetParent(transform, false);
            flame.transform.localPosition = Vector3.zero;
            flame.transform.localScale = Vector3.one;
            flameRenderer = flame.AddComponent<SpriteRenderer>();
            flameRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            flameRenderer.color = Color.white;
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
            ambientRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 7;
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
            walkabilityReleased = false;
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
            extinguished = true;
            relighting = false;
            if (ambientRenderer != null)
            {
                ambientRenderer.enabled = false;
            }

            if (flameRenderer != null)
            {
                flameRenderer.enabled = false;
            }
            spriteRenderer.enabled = true;
            ApplyExtinguishedVisuals();
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireExtinguished",
                StrategyDebugLogger.F("cell", blockedCell),
                StrategyDebugLogger.F("elapsedSeconds", burnAge));
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
            if (extinguished)
            {
                ApplyExtinguishedVisuals();
                return;
            }

            if (relighting)
            {
                ApplyRelightVisuals();
                return;
            }

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

            float warmGreen = Mathf.Lerp(1f, 0.58f, burnoutT);
            float warmBlue = Mathf.Lerp(1f, 0.30f, burnoutT);
            spriteRenderer.color = Color.white;
            spriteRenderer.sprite = StrategyCampfireSpriteFactory.GetBaseFrame(frameIndex);
            transform.localScale = baseScale;
            EnsureFlameRenderer();
            if (flameRenderer != null)
            {
                float flameScale = Mathf.Lerp(1f, ExtinguishedFlameScale, burnoutT);
                flameRenderer.enabled = burnoutT < 0.995f;
                flameRenderer.sprite = StrategyCampfireSpriteFactory.GetFlameFrame(frameIndex);
                flameRenderer.color = new Color(1f, warmGreen, warmBlue, 1f);
                flameRenderer.transform.localScale = Vector3.one * flameScale;
            }
            if (ambientRenderer != null)
            {
                float ambientAlpha = Mathf.Lerp(1f, 0f, burnoutT);
                ambientRenderer.color = new Color(1f, 1f, 1f, ambientAlpha);
            }
        }

        public bool BeginRelight(float seconds)
        {
            if (!NeedsRelight || !IsNightFireTime())
            {
                return false;
            }

            relighting = true;
            relightTimer = 0f;
            relightDuration = Mathf.Max(0.25f, seconds);
            frameIndex = Random.Range(0, StrategyCampfireRelightSpriteFactory.RelightFrameCount);
            ambientFrameIndex = Random.Range(0, StrategyCampfireAmbientSpriteFactory.FrameCount);
            EnsureAmbientRenderer();
            if (ambientRenderer != null)
            {
                ambientRenderer.enabled = true;
            }

            ApplyRelightVisuals();
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireRelightStarted",
                StrategyDebugLogger.F("cell", blockedCell),
                StrategyDebugLogger.F("seconds", relightDuration));
            return true;
        }

        public void CompleteRelight()
        {
            if (!relighting && !extinguished)
            {
                return;
            }

            if (!IsNightFireTime())
            {
                ExtinguishForDaylight();
                return;
            }

            relighting = false;
            extinguished = false;
            initialDaylightFireActive = false;
            burnAge = 0f;
            relightTimer = 0f;
            burnoutStartedLogged = false;
            frameIndex = Random.Range(0, StrategyCampfireSpriteFactory.FrameCount);
            ambientFrameIndex = Random.Range(0, StrategyCampfireAmbientSpriteFactory.FrameCount);
            BlockCampCell();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            EnsureAmbientRenderer();
            if (ambientRenderer != null)
            {
                ambientRenderer.enabled = true;
            }
            transform.localScale = baseScale;
            ApplyFrame();
            ApplyAmbientFrame();
            ApplyBurnoutVisuals();
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireRelit",
                StrategyDebugLogger.F("cell", blockedCell));
        }

        public void CancelRelight()
        {
            if (!relighting)
            {
                return;
            }

            relighting = false;
            extinguished = true;
            relightTimer = 0f;
            ApplyExtinguishedVisuals();
            StrategyDebugLogger.Info(
                "Campfire",
                "CampfireRelightCancelled",
                StrategyDebugLogger.F("cell", blockedCell));
        }

        private void UpdateRelight()
        {
            if (!IsNightFireTime())
            {
                ExtinguishForDaylight();
                return;
            }

            relightTimer += Time.deltaTime;
            frameTimer += Time.deltaTime;
            while (frameTimer >= FrameDuration)
            {
                frameTimer -= FrameDuration;
                frameIndex = (frameIndex + 1) % StrategyCampfireRelightSpriteFactory.RelightFrameCount;
            }

            EnsureAmbientRenderer();
            ambientFrameTimer += Time.deltaTime;
            while (ambientFrameTimer >= AmbientFrameDuration)
            {
                ambientFrameTimer -= AmbientFrameDuration;
                ambientFrameIndex = (ambientFrameIndex + 1) % StrategyCampfireAmbientSpriteFactory.FrameCount;
            }

            ApplyRelightVisuals();
            if (relightTimer >= relightDuration)
            {
                CompleteRelight();
            }
        }

        private void UpdateExtinguishedEmbers()
        {
            if (!IsFullyDaylightExtinguishedTime())
            {
                frameTimer += Time.deltaTime;
                while (frameTimer >= AmbientFrameDuration)
                {
                    frameTimer -= AmbientFrameDuration;
                    frameIndex = (frameIndex + 1) % StrategyCampfireRelightSpriteFactory.EmberFrameCount;
                }
            }

            ApplyExtinguishedVisuals();
        }

        private void ApplyRelightVisuals()
        {
            float t = Mathf.Clamp01(relightTimer / Mathf.Max(0.01f, relightDuration));
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = StrategyCampfireRelightSpriteFactory.GetBaseFrame(frameIndex);
                spriteRenderer.color = Color.white;
            }

            transform.localScale = baseScale;
            EnsureFlameRenderer();
            if (flameRenderer != null)
            {
                flameRenderer.enabled = true;
                flameRenderer.sprite = StrategyCampfireRelightSpriteFactory.GetRelightFlameFrame(frameIndex);
                flameRenderer.color = new Color(1f, Mathf.Lerp(0.58f, 1f, t), Mathf.Lerp(0.38f, 1f, t), 1f);
                flameRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.20f, 1f, t);
            }
            if (ambientRenderer != null)
            {
                ambientRenderer.sprite = StrategyCampfireAmbientSpriteFactory.GetFrame(ambientFrameIndex);
                ambientRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.16f, 0.82f, t));
            }
        }

    }
}
