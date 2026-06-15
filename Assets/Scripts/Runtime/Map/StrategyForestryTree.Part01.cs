using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyForestryTree
    {

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

        private string GetStageTitle()
        {
            return Stage switch
            {
                0 => "sapling",
                1 => "young",
                _ => "mature"
            };
        }

        private string GetTreeStateTitle()
        {
            return treeState switch
            {
                ForestryTreeState.Growing => "growing",
                ForestryTreeState.Standing => "standing",
                ForestryTreeState.BeingChopped => "being chopped",
                ForestryTreeState.Falling => "falling",
                ForestryTreeState.Felled => "felled",
                ForestryTreeState.BuckingFelled => "being bucked",
                ForestryTreeState.SplitLogs => "split logs",
                _ => treeState.ToString()
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
