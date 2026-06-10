using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyForestryTree : MonoBehaviour
    {
        private const int MatureStage = 2;
        private const float SaplingGrowSecondsMin = 24f;
        private const float SaplingGrowSecondsMax = 36f;
        private const float YoungGrowSecondsMin = 36f;
        private const float YoungGrowSecondsMax = 54f;
        private const int RequiredChopHits = 6;
        private const int RequiredBuckHits = 4;
        private const int LogsPerTree = 3;
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
            BlockWalkability();
            controller?.RegisterTree(this);
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

        private void ApplyChopDamageSprite()
        {
            EnsureDamageRenderer();
            if (damageRenderer == null)
            {
                return;
            }

            damageRenderer.sprite = GetChopDamageSprite(chopHitCount);
            damageRenderer.enabled = true;
        }

        private void EnsureDamageRenderer()
        {
            if (damageRenderer != null)
            {
                return;
            }

            GameObject damageObject = new GameObject("Chop Damage");
            damageObject.transform.SetParent(transform, false);
            damageObject.transform.localPosition = new Vector3(0f, 0.36f, -0.01f);
            damageObject.transform.localScale = Vector3.one;
            damageRenderer = damageObject.AddComponent<SpriteRenderer>();
            damageRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 5;
            damageRenderer.color = Color.white;
        }

        private void SyncDamageSorting()
        {
            if (damageRenderer != null && spriteRenderer != null)
            {
                damageRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
            }
        }

        private void DisableAmbientMotion()
        {
            if (windSway == null)
            {
                windSway = GetComponent<StrategyWindSway>();
            }

            if (windSway != null)
            {
                windSway.enabled = false;
            }

            if (ambientAnimator == null)
            {
                ambientAnimator = GetComponent<StrategyNatureFrameAnimator>();
            }

            if (ambientAnimator != null)
            {
                ambientAnimator.SetOverlayVisible(false);
            }

            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
            transform.localScale = baseLocalScale;
        }

        private Vector3 GetChopEffectWorld()
        {
            Vector3 baseWorld = map != null
                ? map.GetCellCenterWorld(Cell.x, Cell.y)
                : transform.position;
            return new Vector3(baseWorld.x, baseWorld.y + 0.24f, -0.16f);
        }

        private Vector3 GetBuckEffectWorld()
        {
            Vector3 baseWorld = map != null
                ? map.GetCellCenterWorld(Cell.x, Cell.y)
                : transform.position;
            return new Vector3(baseWorld.x + fallDirection * 0.16f, baseWorld.y + 0.10f, -0.16f);
        }

        private void CaptureBaseTransform()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalScale = transform.localScale;
            baseLocalRotation = transform.localRotation;
        }

        private void BlockWalkability()
        {
            if (walkabilityBlocked || map == null)
            {
                return;
            }

            map.SetCellsWalkable(Cell, Vector2Int.one, false);
            walkabilityBlocked = true;
        }

        private void ReleaseWalkability()
        {
            if (!walkabilityBlocked || map == null)
            {
                return;
            }

            map.SetCellsWalkable(Cell, Vector2Int.one, true);
            walkabilityBlocked = false;
        }

        private void OnDestroy()
        {
            ReleaseWalkability();
            controller?.UnregisterTree(this);
        }

        private static Sprite GetChopDamageSprite(int hitCount)
        {
            int damage = Mathf.Clamp(hitCount, 1, RequiredChopHits);
            Texture2D texture = new Texture2D(22, 18, TextureFormat.RGBA32, false)
            {
                name = "Tree Chop Damage " + damage,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[22 * 18]);

            Color cut = new Color32(220, 168, 94, 255);
            Color cutDark = new Color32(110, 63, 36, 255);
            Color sap = new Color32(230, 202, 121, 255);
            int marks = Mathf.Min(6, damage);
            for (int i = 0; i < marks; i++)
            {
                int y = 5 + i * 2;
                int x = i % 2 == 0 ? 6 : 10;
                DrawLine(texture, new Vector2Int(x, y), new Vector2Int(x + 6, y + 2), cutDark);
                DrawLine(texture, new Vector2Int(x + 1, y), new Vector2Int(x + 6, y + 1), cut);
                if (i > 2)
                {
                    SetPixelSafe(texture, x + 7, y + 1, sap);
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 22f, 18f), new Vector2(0.5f, 0.20f), 30f);
        }

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int sx = from.x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - from.y);
            int sy = from.y < to.y ? 1 : -1;
            int err = dx + dy;
            int x = from.x;
            int y = from.y;

            while (true)
            {
                SetPixelSafe(texture, x, y, color);
                if (x == to.x && y == to.y)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static float GetNextGrowthSeconds(int stage)
        {
            return stage switch
            {
                0 => Random.Range(SaplingGrowSecondsMin, SaplingGrowSecondsMax),
                1 => Random.Range(YoungGrowSecondsMin, YoungGrowSecondsMax),
                _ => 0f
            };
        }

        private static string GetOwnerName(object owner)
        {
            return owner switch
            {
                StrategyResidentAgent resident => resident.FullName,
                null => "none",
                _ => owner.GetType().Name
            };
        }

        private enum ForestryTreeState
        {
            Growing,
            Standing,
            BeingChopped,
            Falling,
            Felled,
            BuckingFelled,
            SplitLogs
        }
    }
}
