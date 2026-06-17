using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyStoneDepositKind
    {
        Boulder,
        RockCluster,
        Cliff
    }

    [DisallowMultipleComponent]
    public sealed class StrategyStoneDeposit : MonoBehaviour, IStrategyWorldInspectable
    {
        private const float HitShakeSeconds = 0.18f;
        private const float ShakeDistance = 0.035f;

        private StrategyStoneResourceController controller;
        private CityMapController map;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer damageRenderer;
        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private object reservedBy;
        private bool walkabilityBlocked;
        private float hitShakeTimer;
        private int hitCount;
        private int damageLevel;

        public Vector2Int Cell { get; private set; }
        public Vector2Int Footprint { get; private set; }
        public StrategyStoneDepositKind Kind { get; private set; }
        public int StoneAmount { get; private set; }
        public bool IsReserved => reservedBy != null;
        public bool IsDepleted => StoneAmount <= 0;

        public void Configure(
            StrategyStoneResourceController stoneController,
            CityMapController mapController,
            Vector2Int cell,
            Vector2Int footprint,
            StrategyStoneDepositKind kind,
            int stoneAmount)
        {
            controller = stoneController;
            map = mapController;
            Cell = cell;
            Footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            Kind = kind;
            StoneAmount = Mathf.Max(0, stoneAmount);
            spriteRenderer = GetComponent<SpriteRenderer>();
            CaptureBaseTransform();
            EnsureWorldShadow();

            BlockWalkability();
            controller?.RegisterDeposit(this);
        }

        public bool TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
        {
            info = StrategyWorldInspectInfoFactory.CreateStoneDeposit(
                this,
                spriteRenderer != null ? spriteRenderer.sprite : null);
            return true;
        }

        public bool TryReserve(object owner)
        {
            if (owner == null || IsDepleted)
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

        public bool TryTakeStone(object owner, int requestedAmount, out int taken)
        {
            taken = 0;
            if (IsDepleted || requestedAmount <= 0)
            {
                return false;
            }

            if (reservedBy != null && reservedBy != owner)
            {
                return false;
            }

            taken = Mathf.Min(requestedAmount, StoneAmount);
            StoneAmount -= taken;
            StrategyDebugLogger.Info(
                "Stone",
                "StoneTaken",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("kind", Kind),
                StrategyDebugLogger.F("taken", taken),
                StrategyDebugLogger.F("remaining", StoneAmount),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));

            if (StoneAmount <= 0)
            {
                reservedBy = null;
                ReleaseWalkability();
                controller?.UnregisterDeposit(this);
                Destroy(gameObject);
            }

            return taken > 0;
        }

        public bool ReceivePickHit(object owner, Vector3 hitterWorld, out int minedAmount)
        {
            minedAmount = 0;
            if (owner == null || IsDepleted)
            {
                return false;
            }

            if (reservedBy != null && reservedBy != owner)
            {
                return false;
            }

            reservedBy = owner;
            hitCount++;
            hitShakeTimer = HitShakeSeconds;
            ApplyDamageSprite();
            StrategyStonecutEffectAnimator.Spawn(GetHitEffectWorld(hitterWorld), spriteRenderer != null ? spriteRenderer.sortingOrder + 3 : 6, hitCount + (int)Kind * 17);
            StrategyDebugLogger.Info(
                "Stone",
                "PickHit",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("kind", Kind),
                StrategyDebugLogger.F("hit", hitCount),
                StrategyDebugLogger.F("required", GetRequiredHits()),
                StrategyDebugLogger.F("remaining", StoneAmount),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));

            if (hitCount < GetRequiredHits())
            {
                return false;
            }

            minedAmount = Mathf.Min(GetChunkAmount(), StoneAmount);
            StoneAmount -= minedAmount;
            hitCount = 0;
            damageLevel++;
            ApplyDamageSprite();
            StrategyDebugLogger.Info(
                "Stone",
                "ChunkMined",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("kind", Kind),
                StrategyDebugLogger.F("amount", minedAmount),
                StrategyDebugLogger.F("remaining", StoneAmount),
                StrategyDebugLogger.F("owner", GetOwnerName(owner)));

            if (StoneAmount <= 0)
            {
                Deplete();
            }
            else
            {
                reservedBy = null;
            }

            return minedAmount > 0;
        }

        private void Update()
        {
            UpdateHitShake();
        }

        private void BlockWalkability()
        {
            if (walkabilityBlocked || map == null)
            {
                return;
            }

            map.SetCellsWalkable(Cell, Footprint, false);
            walkabilityBlocked = true;
        }

        private void ReleaseWalkability()
        {
            if (!walkabilityBlocked || map == null)
            {
                return;
            }

            map.SetCellsWalkable(Cell, Footprint, true);
            walkabilityBlocked = false;
        }

        private void Deplete()
        {
            reservedBy = null;
            ReleaseWalkability();
            controller?.UnregisterDeposit(this);
            StrategyStonecutEffectAnimator.Spawn(GetHitEffectWorld(transform.position), spriteRenderer != null ? spriteRenderer.sortingOrder + 4 : 7, 99 + (int)Kind * 11);
            StrategyDebugLogger.Info(
                "Stone",
                "DepositDepleted",
                StrategyDebugLogger.F("cell", Cell),
                StrategyDebugLogger.F("kind", Kind));
            Destroy(gameObject);
        }

        private void UpdateHitShake()
        {
            if (hitShakeTimer <= 0f)
            {
                return;
            }

            hitShakeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(hitShakeTimer / HitShakeSeconds);
            float direction = ((hitCount + damageLevel) % 2 == 0) ? 1f : -1f;
            transform.localPosition = baseLocalPosition + new Vector3(Mathf.Sin(Time.time * 75f) * ShakeDistance * t * direction, 0f, 0f);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 58f) * 2.2f * t * direction);
            if (hitShakeTimer <= 0f)
            {
                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
            }
        }

        private void ApplyDamageSprite()
        {
            EnsureDamageRenderer();
            if (damageRenderer == null)
            {
                return;
            }

            int level = Mathf.Clamp(damageLevel + (hitCount > 0 ? 1 : 0), 1, 4);
            damageRenderer.sprite = GetDamageSprite(Kind, level);
            damageRenderer.enabled = true;
        }

        private void EnsureDamageRenderer()
        {
            if (damageRenderer != null)
            {
                return;
            }

            GameObject damageObject = new GameObject("Stone Damage");
            damageObject.transform.SetParent(transform, false);
            damageObject.transform.localPosition = new Vector3(0f, 0.05f, -0.02f);
            damageRenderer = damageObject.AddComponent<SpriteRenderer>();
            damageRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 5;
            damageRenderer.color = Color.white;
            damageRenderer.enabled = false;
        }

        private void CaptureBaseTransform()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
        }

        private void EnsureWorldShadow()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Vector2 scale = Kind switch
            {
                StrategyStoneDepositKind.Cliff => new Vector2(Footprint.x * 0.58f, Footprint.y * 0.24f),
                StrategyStoneDepositKind.RockCluster => new Vector2(Footprint.x * 0.46f, Mathf.Max(0.18f, Footprint.y * 0.18f)),
                _ => new Vector2(0.38f, 0.14f)
            };
            float opacity = Kind == StrategyStoneDepositKind.Cliff ? 0.26f : 0.21f;

            StrategyShadowCaster2D.Attach(
                spriteRenderer,
                StrategyShadowShape.CastOval,
                new Vector2(0.07f, -0.04f),
                scale,
                opacity,
                -5,
                -6f,
                true);
        }

        private Vector3 GetHitEffectWorld(Vector3 hitterWorld)
        {
            Vector3 center = spriteRenderer != null ? spriteRenderer.bounds.center : transform.position;
            Vector3 towardHitter = hitterWorld - center;
            if (towardHitter.sqrMagnitude < 0.001f)
            {
                towardHitter = Vector3.up;
            }

            towardHitter.Normalize();
            return center + new Vector3(towardHitter.x * 0.12f, towardHitter.y * 0.08f, -0.02f);
        }

        private int GetRequiredHits()
        {
            return Kind switch
            {
                StrategyStoneDepositKind.Cliff => 8,
                StrategyStoneDepositKind.RockCluster => 6,
                _ => 3
            };
        }

        private int GetChunkAmount()
        {
            return Kind switch
            {
                StrategyStoneDepositKind.Cliff => 4,
                StrategyStoneDepositKind.RockCluster => 2,
                _ => 1
            };
        }

        private void OnDestroy()
        {
            ReleaseWalkability();
            controller?.UnregisterDeposit(this);
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

        private static string GetStoneTitle(StrategyStoneDepositKind kind)
        {
            return kind switch
            {
                StrategyStoneDepositKind.Boulder => "Boulder",
                StrategyStoneDepositKind.RockCluster => "Rock Cluster",
                StrategyStoneDepositKind.Cliff => "Stone Cliff",
                _ => kind.ToString()
            };
        }

        private static Sprite GetDamageSprite(StrategyStoneDepositKind kind, int level)
        {
            int cacheKey = 87040 + ((int)kind * 16) + Mathf.Clamp(level, 1, 4);
            return StoneDamageSpriteCache.Get(cacheKey, kind, level);
        }

        private static class StoneDamageSpriteCache
        {
            private static readonly System.Collections.Generic.Dictionary<int, Sprite> Cache = new();

            public static Sprite Get(int key, StrategyStoneDepositKind kind, int level)
            {
                if (!Cache.TryGetValue(key, out Sprite sprite) || sprite == null)
                {
                    sprite = Create(kind, level);
                    Cache[key] = sprite;
                }

                return sprite;
            }

            private static Sprite Create(StrategyStoneDepositKind kind, int level)
            {
                int width = kind == StrategyStoneDepositKind.Cliff ? 86 : kind == StrategyStoneDepositKind.RockCluster ? 62 : 40;
                int height = kind == StrategyStoneDepositKind.Cliff ? 58 : kind == StrategyStoneDepositKind.RockCluster ? 36 : 28;
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Stone Damage " + kind + " " + level,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixels(new Color[width * height]);

                Color crack = new Color(0.08f, 0.09f, 0.08f, 0.82f);
                Color bright = new Color(0.76f, 0.80f, 0.76f, 0.52f);
                int centerX = width / 2;
                int centerY = height / 2;
                for (int i = 0; i < level + 1; i++)
                {
                    int startX = centerX - 8 + i * 5;
                    int startY = centerY + 8 - i * 3;
                    int endX = startX + ((i % 2 == 0) ? 12 : -10);
                    int endY = startY - 9 - level * 2;
                    DrawLine(texture, P(startX, startY), P(endX, endY), crack);
                    DrawLine(texture, P(startX + 1, startY), P(endX + 1, endY), crack);

                    if (level >= 3)
                    {
                        DrawLine(texture, P(startX, startY - 2), P(startX - 6, startY - 8), crack);
                    }
                }

                for (int i = 0; i < level * 3; i++)
                {
                    int x = 5 + ((level * 13 + i * 11) % Mathf.Max(1, width - 10));
                    int y = 5 + ((level * 17 + i * 7) % Mathf.Max(1, height - 10));
                    SetPixelSafe(texture, x, y, i % 2 == 0 ? bright : crack);
                }

                texture.Apply(false, false);
                return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.18f), 30f);
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

            private static Vector2Int P(int x, int y)
            {
                return new Vector2Int(x, y);
            }
        }
    }
}
