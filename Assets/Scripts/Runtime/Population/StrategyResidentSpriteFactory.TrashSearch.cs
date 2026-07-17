using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        public const int TrashSearchFrameCount = 12;

        private static readonly Dictionary<int, Sprite> CachedTrashSearchSprites = new();
        private static readonly ResidentWalkFrame[] TrashSearchBodyFrames =
        {
            new(-1, 0, 0, 0, 0, -1, 1, -2, -2),
            new(-2, 0, 0, 0, 0, -2, 1, -3, -2),
            new(-2, 0, 0, 0, 0, -1, 2, -3, -3),
            new(-1, 0, 0, 0, 0, -2, 1, -2, -3),
            new(-2, 0, 0, 0, 0, -1, 2, -3, -2),
            new(-2, 0, 0, 0, 0, -2, 1, -3, -3),
            new(-1, 0, 0, 0, 0, -1, 2, -2, -3),
            new(-2, 0, 0, 0, 0, -2, 1, -3, -2),
            new(-1, 0, 0, 0, 0, -1, 1, -1, -1),
            new(0, 0, 0, 0, 0, -1, 1, 0, -1),
            ResidentWalkFrame.Idle,
            ResidentWalkFrame.Idle
        };

        public static Sprite GetTrashSearchSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            if (lifeStage != StrategyResidentLifeStage.Adult)
            {
                return GetSprite(gender, variant, lifeStage);
            }

            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, TrashSearchFrameCount);
            if (StrategyVisualCatalogProvider.TryGetResidentSprite(
                    gender,
                    lifeStage,
                    StrategyResidentVisualPose.TrashSearch,
                    normalizedVariant,
                    normalizedFrame,
                    out Sprite catalogSprite))
            {
                return catalogSprite;
            }

            int key = ((int)gender * 1024) + (normalizedVariant * 64) + normalizedFrame;
            if (!CachedTrashSearchSprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = CreateTrashSearchSprite(gender, normalizedVariant, normalizedFrame);
                CachedTrashSearchSprites[key] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateTrashSearchSprite(
            StrategyResidentGender gender,
            int variant,
            int frame)
        {
            Texture2D texture = new(20, 28, TextureFormat.RGBA32, false)
            {
                name = $"{gender} Resident Trash Search V{variant + 1} F{frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[20 * 28]);

            Color outline = Rgb(45, 31, 27);
            Color skin = GetMouseStartleSkinColor(variant);
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color leg = variant == 3 ? Rgb(48, 43, 42) : Rgb(61, 51, 43);
            Color accent = GetAccentColor(gender, variant);
            ResidentWalkFrame body = TrashSearchBodyFrames[frame];

            if (gender == StrategyResidentGender.Male)
            {
                DrawMale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }
            else
            {
                DrawFemale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }

            DrawTrashSearchOverlay(texture, frame, outline, skin);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 20f, 28f),
                new Vector2(0.5f, 0.08f),
                PixelsPerUnit);
        }

        private static void DrawTrashSearchOverlay(
            Texture2D texture,
            int frame,
            Color outline,
            Color skin)
        {
            if (frame < 8)
            {
                int handShift = frame % 3 - 1;
                FillEllipse(texture, 4 + handShift, 10, 2, 2, skin);
                FillEllipse(texture, 15 - handShift, 9 + frame % 2, 2, 2, skin);
                Color scrap = frame % 2 == 0 ? Rgb(118, 77, 48) : Rgb(113, 119, 108);
                SetPixelSafe(texture, 2 + frame % 4, 7 + frame % 3, scrap);
                SetPixelSafe(texture, 17 - frame % 3, 6 + (frame + 1) % 3, outline);
                return;
            }

            int raise = Mathf.Clamp(frame - 8, 0, 3);
            int bowlY = 12 + raise * 2;
            DrawLine(
                texture,
                new Vector2Int(14, 10 + raise),
                new Vector2Int(17, bowlY + 4),
                Rgb(169, 158, 137));
            FillEllipse(texture, 17, bowlY + 5, 2, 2, Rgb(190, 180, 158));
            SetPixelSafe(texture, 17, bowlY + 5, outline);
            SetPixelSafe(texture, 18, bowlY + 5, new Color(0f, 0f, 0f, 0f));
            FillEllipse(texture, 14, 10 + raise, 2, 2, skin);
        }
    }
}
