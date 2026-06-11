using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyHuntingArrowProjectile : MonoBehaviour
    {
        private const float Speed = 8.2f;
        private static Sprite arrowSprite;

        private StrategyRabbitAgent target;
        private object owner;
        private SpriteRenderer spriteRenderer;
        private Vector3 startWorld;
        private Vector3 targetWorld;
        private float duration;
        private float elapsed;

        public static void Launch(Vector3 fromWorld, StrategyRabbitAgent rabbit, object owner)
        {
            if (rabbit == null || owner == null)
            {
                return;
            }

            GameObject arrowObject = new GameObject("Hunting Arrow");
            StrategyHuntingArrowProjectile projectile = arrowObject.AddComponent<StrategyHuntingArrowProjectile>();
            projectile.Configure(fromWorld, rabbit, owner);
        }

        private void Configure(Vector3 fromWorld, StrategyRabbitAgent rabbit, object projectileOwner)
        {
            target = rabbit;
            owner = projectileOwner;
            startWorld = new Vector3(fromWorld.x, fromWorld.y, -0.11f);
            targetWorld = GetTargetWorld();
            float distance = Vector2.Distance(startWorld, targetWorld);
            duration = Mathf.Clamp(distance / Speed, 0.18f, 0.55f);
            elapsed = 0f;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetArrowSprite();
            spriteRenderer.color = Color.white;
            transform.position = startWorld;
            UpdateRotation(startWorld, targetWorld);
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 4);
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            targetWorld = GetTargetWorld();
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            Vector3 next = Vector3.Lerp(startWorld, targetWorld, Smooth01(t));
            next.z = -0.11f;
            transform.position = next;
            UpdateRotation(transform.position, targetWorld);
            if (spriteRenderer != null)
            {
                StrategyWorldSorting.Apply(spriteRenderer, transform.position, 4);
            }

            if (t < 1f)
            {
                return;
            }

            target.ReceiveArrowHit(owner, transform.position);
            Destroy(gameObject);
        }

        private Vector3 GetTargetWorld()
        {
            if (target == null)
            {
                return transform.position;
            }

            Vector3 world = target.transform.position;
            world.y += 0.16f;
            world.z = -0.11f;
            return world;
        }

        private void UpdateRotation(Vector3 from, Vector3 to)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static Sprite GetArrowSprite()
        {
            if (arrowSprite != null)
            {
                return arrowSprite;
            }

            Texture2D texture = new Texture2D(30, 10, TextureFormat.RGBA32, false)
            {
                name = "Hunting Arrow Projectile",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[30 * 10]);

            Color outline = Rgb(45, 31, 24);
            Color shaft = Rgb(130, 83, 42);
            Color shaftLight = Rgb(183, 124, 62);
            Color metal = Rgb(148, 154, 143);
            Color feather = Rgb(224, 205, 145);

            DrawLine(texture, 3, 5, 24, 5, outline);
            DrawLine(texture, 4, 5, 23, 5, shaft);
            DrawLine(texture, 8, 4, 20, 4, shaftLight);
            FillRect(texture, 23, 3, 4, 5, metal);
            SetPixelSafe(texture, 28, 5, metal);
            DrawLine(texture, 3, 5, 0, 2, feather);
            DrawLine(texture, 3, 5, 0, 8, feather);
            DrawLine(texture, 5, 5, 1, 3, outline);
            DrawLine(texture, 5, 5, 1, 7, outline);

            texture.Apply(false, false);
            arrowSprite = Sprite.Create(texture, new Rect(0f, 0f, 30f, 10f), new Vector2(0.82f, 0.5f), 30f);
            return arrowSprite;
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixelSafe(texture, px, py, color);
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                SetPixelSafe(texture, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = err * 2;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
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

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
