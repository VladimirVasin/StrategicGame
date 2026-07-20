using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityInventoryHudController
    {
        private static Sprite GetChestSprite()
        {
            if (chestSprite != null)
            {
                return chestSprite;
            }

            Texture2D texture = new(24, 20, TextureFormat.RGBA32, false)
            {
                name = "City Inventory Chest Icon",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[24 * 20]);
            Color dark = new Color32(62, 39, 25, 255);
            Color wood = new Color32(142, 89, 42, 255);
            Color light = new Color32(204, 142, 63, 255);
            Color metal = new Color32(218, 174, 77, 255);
            FillPixels(texture, 3, 3, 18, 10, dark);
            FillPixels(texture, 4, 4, 16, 8, wood);
            FillPixels(texture, 3, 12, 18, 4, dark);
            FillPixels(texture, 5, 13, 14, 3, light);
            FillPixels(texture, 10, 4, 4, 12, metal);
            FillPixels(texture, 11, 7, 2, 3, dark);
            texture.Apply(false, false);
            chestSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 24f, 20f),
                new Vector2(0.5f, 0.5f),
                24f);
            return chestSprite;
        }

        private static void FillPixels(
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }
}
