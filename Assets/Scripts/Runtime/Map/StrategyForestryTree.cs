using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyForestryTree : MonoBehaviour, IStrategyWorldInspectable
    {
        private const int MatureStage = 2;
        private const float SaplingGrowSecondsMin = 24f;
        private const float SaplingGrowSecondsMax = 36f;
        private const float YoungGrowSecondsMin = 36f;
        private const float YoungGrowSecondsMax = 54f;
        private const int RequiredChopHits = 6;
        private const int RequiredBuckHits = 4;
        public const int LogsPerTree = 3;
        private const float ChopShakeSeconds = 0.22f;
        private const float BuckShakeSeconds = 0.16f;
        private const float FallSeconds = 1.25f;

        private StrategyForestryController controller;
        private CityMapController map;
        private StrategyWindController wind;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer damageRenderer;
        private StrategyWindSway windSway;
        private StrategyNatureFrameAnimator ambientAnimator;
        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private Quaternion baseLocalRotation;
        private object reservedBy;
        private ForestryTreeState treeState = ForestryTreeState.Standing;
        private bool useGrowthSprites;
        private bool walkabilityBlocked;
        private float growthTimer;
        private float chopShakeTimer;
        private float buckShakeTimer;
        private float fallTimer;
        private int chopHitCount;
        private int buckHitCount;
        private int fallDirection = 1;

        public Vector2Int Cell { get; private set; }
        public int Stage { get; private set; }
        public int VisualVariant { get; private set; }
        public bool IsMature => Stage >= MatureStage;
        public bool IsReserved => reservedBy != null;
        public bool CanBeChopped => IsMature
            && (treeState == ForestryTreeState.Standing || treeState == ForestryTreeState.BeingChopped);
        public bool IsFalling => treeState == ForestryTreeState.Falling;
        public bool IsFelled => treeState == ForestryTreeState.Felled
            || treeState == ForestryTreeState.BuckingFelled
            || treeState == ForestryTreeState.SplitLogs;
        public bool CanBeBucked => IsMature
            && (treeState == ForestryTreeState.Felled || treeState == ForestryTreeState.BuckingFelled);
        public bool HasLogsReady => treeState == ForestryTreeState.SplitLogs;

        public void Configure(
            StrategyForestryController forestryController,
            CityMapController mapController,
            StrategyWindController windController,
            Vector2Int treeCell,
            int initialStage,
            int visualVariant,
            SpriteRenderer renderer,
            bool shouldUseGrowthSprites)
        {
            controller = forestryController;
            map = mapController;
            wind = windController;
            Cell = treeCell;
            Stage = Mathf.Clamp(initialStage, 0, MatureStage);
            VisualVariant = visualVariant;
            spriteRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            useGrowthSprites = shouldUseGrowthSprites;
            growthTimer = GetNextGrowthSeconds(Stage);
            treeState = IsMature ? ForestryTreeState.Standing : ForestryTreeState.Growing;
            CaptureBaseTransform();

            if (useGrowthSprites)
            {
                ApplyGrowthSprite();
            }

            EnsureWindSway();
            EnsureStandingWorldShadow();
            BlockWalkability();
            controller?.RegisterTree(this);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            string body = "Stage: "
                + GetStageTitle()
                + "\nState: "
                + GetTreeStateTitle()
                + "\nLogs: "
                + (HasLogsReady ? LogsPerTree.ToString() : "not ready")
                + "\nReserved: "
                + (IsReserved ? "yes" : "no");
            info = new StrategyWorldInspectInfo(
                IsMature ? "Tree" : "Young Tree",
                "Forest resource",
                body,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                Cell,
                true);
            return true;
        }

        public bool ReceiveChopHit(Vector3 hitterWorld)
        {
            if (!CanBeChopped)
            {
                return false;
            }

            if (treeState == ForestryTreeState.Standing)
            {
                treeState = ForestryTreeState.BeingChopped;
                DisableAmbientMotion();
            }

            chopHitCount++;
            chopShakeTimer = ChopShakeSeconds;
            ApplyChopDamageSprite();
            StrategyWoodcutEffectAnimator.Spawn(GetChopEffectWorld(), spriteRenderer != null ? spriteRenderer.sortingOrder + 3 : 6, chopHitCount);

            if (chopHitCount >= RequiredChopHits)
            {
                StartFalling(hitterWorld);
                return true;
            }

            return false;
        }

        public bool ReceiveBuckHit(Vector3 hitterWorld)
        {
            if (!CanBeBucked)
            {
                return false;
            }

            if (treeState == ForestryTreeState.Felled)
            {
                treeState = ForestryTreeState.BuckingFelled;
            }

            buckHitCount++;
            buckShakeTimer = BuckShakeSeconds;
            StrategyWoodcutEffectAnimator.Spawn(GetBuckEffectWorld(), spriteRenderer != null ? spriteRenderer.sortingOrder + 3 : 6, buckHitCount + 30);

            if (buckHitCount >= RequiredBuckHits)
            {
                CompleteBucking();
                return true;
            }

            return false;
        }

        public bool TryTakeLogs(object owner, out int amount)
        {
            amount = 0;
            if (!HasLogsReady)
            {
                return false;
            }

            if (reservedBy != null && reservedBy != owner)
            {
                return false;
            }

            amount = LogsPerTree;
            StrategyDebugLogger.Info(
                "Forestry",
                "LogsTaken",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));
            reservedBy = null;
            controller?.UnregisterTree(this);
            ReleaseWalkability();
            Destroy(gameObject);
            return true;
        }

        public bool TryReserve(object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (reservedBy != null && reservedBy != owner)
            {
                return false;
            }

            reservedBy = owner;
            return true;
        }

        public void Release(object owner)
        {
            if (owner == null || reservedBy == owner)
            {
                reservedBy = null;
            }
        }

        public void Cut()
        {
            controller?.UnregisterTree(this);
            reservedBy = null;
            ReleaseWalkability();
            Destroy(gameObject);
        }

        private void Update()
        {
            if (treeState == ForestryTreeState.Falling)
            {
                UpdateFalling();
                return;
            }

            if (treeState == ForestryTreeState.Felled)
            {
                UpdateBuckShake();
                return;
            }

            if (treeState == ForestryTreeState.BuckingFelled)
            {
                UpdateBuckShake();
                return;
            }

            if (treeState == ForestryTreeState.SplitLogs)
            {
                return;
            }

            UpdateChopShake();

            if (!useGrowthSprites || IsMature)
            {
                return;
            }

            growthTimer -= Time.deltaTime;
            if (growthTimer > 0f)
            {
                return;
            }

            Stage = Mathf.Min(MatureStage, Stage + 1);
            growthTimer = GetNextGrowthSeconds(Stage);
            if (IsMature)
            {
                treeState = ForestryTreeState.Standing;
                StrategyDebugLogger.Info(
                    "Forestry",
                    "TreeMatured",
                    StrategyDebugLogger.F("cell", Cell),
                    StrategyDebugLogger.F("variant", VisualVariant));
            }

            ApplyGrowthSprite();
        }

        private void ApplyGrowthSprite()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = StrategyNatureSpriteFactory.GetTreeGrowthSprite(Stage, VisualVariant);
            spriteRenderer.color = Color.white;
            StrategyWorldSorting.Apply(spriteRenderer, transform.position);
            SyncDamageSorting();
            EnsureStandingWorldShadow();
        }

        private void EnsureStandingWorldShadow()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Vector2 scale = Stage switch
            {
                0 => new Vector2(0.34f, 0.12f),
                1 => new Vector2(0.52f, 0.17f),
                _ => new Vector2(0.78f, 0.24f)
            };
            float opacity = Stage switch
            {
                0 => 0.13f,
                1 => 0.18f,
                _ => 0.25f
            };

            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.CastOval,
                new Vector2(0.13f, -0.06f),
                scale,
                opacity,
                -6,
                -7f,
                true);
        }

        private void EnsureFelledWorldShadow(bool splitLogs)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0.04f, -0.04f),
                splitLogs ? new Vector2(0.56f, 0.15f) : new Vector2(0.78f, 0.19f),
                splitLogs ? 0.16f : 0.18f,
                -5,
                0f,
                false);
        }

        private void EnsureWindSway()
        {
            if (GetComponent<StrategyWindSway>() != null)
            {
                windSway = GetComponent<StrategyWindSway>();
                return;
            }

            StrategyWindSway sway = gameObject.AddComponent<StrategyWindSway>();
            float phase = Mathf.Repeat((Cell.x * 17.31f) + (Cell.y * 29.73f), Mathf.PI * 2f);
            float bend = Stage <= 0 ? 1.3f : Stage == 1 ? 2.4f : 2.8f;
            sway.Configure(wind, phase, bend, 0.014f, 0.008f);
            windSway = sway;
        }

        private void StartFalling(Vector3 hitterWorld)
        {
            treeState = ForestryTreeState.Falling;
            DisableAmbientMotion();
            if (damageRenderer != null)
            {
                damageRenderer.enabled = false;
            }

            fallTimer = 0f;
            fallDirection = transform.position.x >= hitterWorld.x ? 1 : -1;
            StrategyWoodcutEffectAnimator.Spawn(GetChopEffectWorld(), spriteRenderer != null ? spriteRenderer.sortingOrder + 4 : 7, chopHitCount + 11);
            StrategyDebugLogger.Info(
                "Forestry",
                "TreeFalling",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("variant", VisualVariant),
                StrategyDebugLogger.F("hits", chopHitCount),
                StrategyDebugLogger.F("direction", fallDirection));
        }

        private void UpdateFalling()
        {
            fallTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fallTimer / FallSeconds);
            float eased = t * t * (3f - 2f * t);
            float targetAngle = fallDirection > 0 ? -84f : 84f;
            float bounce = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 3.5f;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, targetAngle, eased) + bounce);
            transform.localPosition = baseLocalPosition + new Vector3(fallDirection * 0.18f * eased, -0.05f * eased, 0f);
            transform.localScale = baseLocalScale;

            if (t >= 1f)
            {
                FinishFalling();
            }
        }

        private void FinishFalling()
        {
            treeState = ForestryTreeState.Felled;
            buckHitCount = 0;
            buckShakeTimer = 0f;
            BlockWalkability();

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
            transform.localScale = baseLocalScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = StrategyNatureSpriteFactory.GetFelledTreeSprite(VisualVariant);
                spriteRenderer.flipX = fallDirection < 0;
                StrategyWorldSorting.Apply(spriteRenderer, transform.position);
                SyncDamageSorting();
                EnsureFelledWorldShadow(false);
            }

            StrategyDebugLogger.Info(
                "Forestry",
                "TreeFelled",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("variant", VisualVariant),
                StrategyDebugLogger.F("direction", fallDirection));
        }

        private void CompleteBucking()
        {
            treeState = ForestryTreeState.SplitLogs;
            buckShakeTimer = 0f;

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
            transform.localScale = baseLocalScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = StrategyNatureSpriteFactory.GetSplitLogsSprite(VisualVariant);
                spriteRenderer.flipX = fallDirection < 0;
                StrategyWorldSorting.Apply(spriteRenderer, transform.position);
                SyncDamageSorting();
                EnsureFelledWorldShadow(true);
            }

            StrategyWoodcutEffectAnimator.Spawn(GetBuckEffectWorld(), spriteRenderer != null ? spriteRenderer.sortingOrder + 4 : 7, buckHitCount + 50);
            StrategyDebugLogger.Info(
                "Forestry",
                "LogsReady",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("variant", VisualVariant),
                StrategyDebugLogger.F("buckHits", buckHitCount),
                StrategyDebugLogger.F("amount", LogsPerTree));
        }

        private void UpdateChopShake()
        {
            if (chopShakeTimer <= 0f || treeState != ForestryTreeState.BeingChopped)
            {
                return;
            }

            chopShakeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(chopShakeTimer / ChopShakeSeconds);
            float shake = Mathf.Sin(Time.time * 52f) * t;
            transform.localPosition = baseLocalPosition + new Vector3(shake * 0.028f, 0f, 0f);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, shake * 2.2f);
            transform.localScale = baseLocalScale;

            if (chopShakeTimer <= 0f)
            {
                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
                transform.localScale = baseLocalScale;
            }
        }

        private void UpdateBuckShake()
        {
            if (buckShakeTimer <= 0f
                || (treeState != ForestryTreeState.BuckingFelled && treeState != ForestryTreeState.Felled))
            {
                return;
            }

            buckShakeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(buckShakeTimer / BuckShakeSeconds);
            float shake = Mathf.Sin(Time.time * 64f) * t;
            transform.localPosition = baseLocalPosition + new Vector3(shake * 0.018f, 0f, 0f);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, shake * 1.8f);
            transform.localScale = baseLocalScale;

            if (buckShakeTimer <= 0f)
            {
                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
                transform.localScale = baseLocalScale;
            }
        }
    }
}
