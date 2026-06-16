using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyWildlifeRiverCrossing
    {
        private const float SwimSpeedMultiplier = 0.38f;
        private static Sprite rippleSprite;

        public static bool IsLandOrRiverCell(CityMapController map, Vector2Int cell)
        {
            if (map == null)
            {
                return false;
            }

            return map.IsCellWalkable(cell) || IsRiverCell(map, cell);
        }

        public static bool IsLandCell(CityMapController map, Vector2Int cell)
        {
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && mapCell.Kind != CityMapCellKind.Water
                && map.IsCellWalkable(cell);
        }

        public static float GetAdjustedSpeed(CityMapController map, Vector3 fromWorld, Vector3 toWorld, float baseSpeed)
        {
            return IsSwimmingMove(map, fromWorld, toWorld) ? baseSpeed * SwimSpeedMultiplier : baseSpeed;
        }

        public static bool IsSwimmingMove(CityMapController map, Vector3 fromWorld, Vector3 toWorld)
        {
            return IsRiverCell(map, fromWorld) || IsRiverCell(map, toWorld);
        }

        public static bool IsRiverCell(CityMapController map, Vector3 world)
        {
            return map != null && map.TryWorldToCell(world, out Vector2Int cell) && IsRiverCell(map, cell);
        }

        public static bool IsRiverCell(CityMapController map, Vector2Int cell)
        {
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && mapCell.Kind == CityMapCellKind.Water
                && mapCell.WaterKind == CityMapWaterKind.River;
        }

        public static void UpdateSwimRipple(
            Transform parent,
            SpriteRenderer owner,
            ref SpriteRenderer rippleRenderer,
            bool active,
            Vector3 localPosition,
            Vector3 localScale,
            float phase)
        {
            if (owner == null || parent == null)
            {
                return;
            }

            if (rippleRenderer == null && active)
            {
                GameObject rippleObject = new GameObject("Wildlife Swim Ripple");
                rippleObject.transform.SetParent(parent, false);
                rippleRenderer = rippleObject.AddComponent<SpriteRenderer>();
                rippleRenderer.sprite = GetRippleSprite();
            }

            if (rippleRenderer == null)
            {
                return;
            }

            rippleRenderer.gameObject.SetActive(active);
            if (!active)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin((Time.time + phase) * 5.4f) * 0.09f;
            rippleRenderer.transform.localPosition = localPosition;
            rippleRenderer.transform.localScale = new Vector3(localScale.x * pulse, localScale.y, localScale.z);
            rippleRenderer.flipX = owner.flipX;
            rippleRenderer.sortingLayerID = owner.sortingLayerID;
            rippleRenderer.sortingOrder = Mathf.Max(0, owner.sortingOrder - 1);
            rippleRenderer.color = new Color(0.60f, 0.78f, 0.86f, 0.64f);
        }

        private static Sprite GetRippleSprite()
        {
            if (rippleSprite != null)
            {
                return rippleSprite;
            }

            const int width = 44;
            const int height = 14;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Wildlife Swim Ripple",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            DrawRipple(texture, width * 0.50f, height * 0.52f, width * 0.39f, height * 0.25f, 0.50f);
            DrawRipple(texture, width * 0.50f, height * 0.52f, width * 0.24f, height * 0.14f, 0.72f);
            texture.Apply(false, false);
            rippleSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 34f);
            return rippleSprite;
        }

        private static void DrawRipple(Texture2D texture, float centerX, float centerY, float radiusX, float radiusY, float alpha)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float dx = (x - centerX) / radiusX;
                    float dy = (y - centerY) / radiusY;
                    float ring = Mathf.Abs((dx * dx + dy * dy) - 1f);
                    if (ring < 0.20f)
                    {
                        texture.SetPixel(x, y, new Color(0.74f, 0.93f, 1f, alpha * (1f - ring * 4.2f)));
                    }
                }
            }
        }
    }
}
