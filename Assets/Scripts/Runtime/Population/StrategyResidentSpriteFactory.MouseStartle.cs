using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        public const int MouseStartleFrameCount = 8;

        private static readonly Dictionary<int, Sprite> CachedMouseStartleSprites = new();

        private static readonly ResidentWalkFrame[] MouseStartleBodyFrames =
        {
            ResidentWalkFrame.Idle,
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 1, 1),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, 1, 3, 3),
            new ResidentWalkFrame(1, -1, 1, -2, 2, -2, 2, 4, 4),
            new ResidentWalkFrame(0, -1, 1, -2, 2, -2, 2, 4, 4),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -2, 2, 3, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 2, 2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 1, 1)
        };

        public static Sprite GetMouseStartleSprite(
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
            int normalizedFrame = NormalizeVariant(frame, MouseStartleFrameCount);
            if (StrategyVisualCatalogProvider.TryGetResidentSprite(
                    gender,
                    lifeStage,
                    StrategyResidentVisualPose.MouseStartle,
                    normalizedVariant,
                    normalizedFrame,
                    out Sprite catalogSprite))
            {
                return catalogSprite;
            }

            int cacheKey = ((int)gender * 1024) + (normalizedVariant * 64) + normalizedFrame;
            if (!CachedMouseStartleSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateMouseStartleSprite(gender, normalizedVariant, normalizedFrame);
                CachedMouseStartleSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateMouseStartleSprite(
            StrategyResidentGender gender,
            int variant,
            int frame)
        {
            Texture2D texture = new Texture2D(20, 28, TextureFormat.RGBA32, false)
            {
                name = $"{gender} Resident Mouse Startle V{variant + 1} F{frame + 1}",
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
            ResidentWalkFrame body = MouseStartleBodyFrames[frame];

            if (gender == StrategyResidentGender.Male)
            {
                DrawMale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }
            else
            {
                DrawFemale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }

            DrawMouseStartleExpression(texture, frame, body.BodyYOffset, outline, skin, accent);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 20f, 28f),
                new Vector2(0.5f, 0.08f),
                PixelsPerUnit);
        }

        private static void DrawMouseStartleExpression(
            Texture2D texture,
            int frame,
            int bodyY,
            Color outline,
            Color skin,
            Color accent)
        {
            if (frame == 0)
            {
                return;
            }

            Color eyeWhite = Rgb(238, 228, 194);
            FillRect(texture, 7, 21 + bodyY, 2, 2, eyeWhite);
            FillRect(texture, 11, 21 + bodyY, 2, 2, eyeWhite);
            SetPixelSafe(texture, 8, 21 + bodyY, outline);
            SetPixelSafe(texture, 11, 21 + bodyY, outline);
            SetPixelSafe(texture, 7, 23 + bodyY, outline);
            SetPixelSafe(texture, 12, 23 + bodyY, outline);

            if (frame >= 2 && frame <= 6)
            {
                FillRect(texture, 9, 18 + bodyY, 3, 2, outline);
                SetPixelSafe(texture, 10, 18 + bodyY, Rgb(135, 64, 55));
            }
            else
            {
                FillRect(texture, 9, 19 + bodyY, 3, 1, outline);
            }

            if (frame >= 2 && frame <= 5)
            {
                int handY = 18 + bodyY + (frame == 3 || frame == 4 ? 1 : 0);
                FillEllipse(texture, 3, handY, 2, 2, skin);
                FillEllipse(texture, 17, handY, 2, 2, skin);
                SetPixelSafe(texture, 2, 23, accent);
                SetPixelSafe(texture, 1, 22, outline);
                SetPixelSafe(texture, 17, 24, accent);
                SetPixelSafe(texture, 18, 23, outline);
            }

            if (frame == 3 || frame == 4)
            {
                Color sweat = Rgb(124, 190, 220);
                SetPixelSafe(texture, 15, 23 + bodyY, sweat);
                SetPixelSafe(texture, 15, 22 + bodyY, sweat);
            }
        }

        private static Color GetMouseStartleSkinColor(int variant)
        {
            return variant switch
            {
                1 => Rgb(183, 126, 84),
                2 => Rgb(226, 177, 124),
                3 => Rgb(156, 105, 78),
                4 => Rgb(217, 151, 96),
                _ => Rgb(207, 158, 106)
            };
        }
    }
}
