using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        public const int CampfireKindleFrameCount = 8;
        public const int GroundSleepFrameCount = 4;

        public static Sprite GetCampfireKindleSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, CampfireKindleFrameCount);
            int cacheKey = GetCampfirePoseCacheKey(gender, normalizedVariant, lifeStage, normalizedFrame, 0);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCampfireKindleSprite(gender, normalizedVariant, lifeStage, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetGroundSleepSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, GroundSleepFrameCount);
            int cacheKey = GetCampfirePoseCacheKey(gender, normalizedVariant, lifeStage, normalizedFrame, 1);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateGroundSleepSprite(gender, normalizedVariant, lifeStage, normalizedFrame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateCampfireKindleSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            Texture2D texture = new Texture2D(20, 28, TextureFormat.RGBA32, false)
            {
                name = $"Resident Campfire Kindle {variant + 1}-{frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[20 * 28]);

            Color outline = Rgb(45, 31, 27);
            Color skin = GetSkinColor(variant);
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color leg = variant == 3 ? Rgb(48, 43, 42) : Rgb(61, 51, 43);
            Color accent = GetAccentColor(gender, variant);
            ResidentWalkFrame body = KindleBodyFrames[NormalizeVariant(frame, CampfireKindleFrameCount)];

            if (lifeStage == StrategyResidentLifeStage.Child)
            {
                body = new ResidentWalkFrame(body.BodyYOffset, 0, 0, 0, 0, body.LeftArmX, body.RightArmX, body.LeftArmY, body.RightArmY);
            }

            if (gender == StrategyResidentGender.Male)
            {
                DrawMale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }
            else
            {
                DrawFemale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }

            DrawKindlingOverlay(texture, frame, outline);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 20f, 28f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }

        private static Sprite CreateGroundSleepSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int width = lifeStage == StrategyResidentLifeStage.Child ? 24 : 30;
            int height = 18;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Resident Ground Sleep {variant + 1}-{frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            Color outline = Rgb(43, 31, 27);
            Color skin = GetSkinColor(variant);
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color blanket = Color.Lerp(tunic, Rgb(116, 91, 62), 0.38f);
            int breath = frame == 1 || frame == 2 ? 1 : 0;
            int headX = lifeStage == StrategyResidentLifeStage.Child ? 6 : 7;
            int bodyX = lifeStage == StrategyResidentLifeStage.Child ? 9 : 11;
            int bodyWidth = lifeStage == StrategyResidentLifeStage.Child ? 11 : 15;

            FillEllipse(texture, width / 2, 4, width / 2 - 3, 3, new Color(0f, 0f, 0f, 0.25f));
            FillRect(texture, bodyX, 7 + breath, bodyWidth, 6, outline);
            FillRect(texture, bodyX + 1, 8 + breath, bodyWidth - 2, 4, blanket);
            FillRect(texture, bodyX + 2, 11 + breath, bodyWidth - 4, 1, tunicDark);
            FillEllipse(texture, headX, 10 + breath, 5, 4, outline);
            FillEllipse(texture, headX, 10 + breath, 4, 3, skin);
            FillRect(texture, headX - 4, 12 + breath, 8, 2, hair);
            FillRect(texture, headX - 5, 9 + breath, 2, 3, hair);
            SetPixelSafe(texture, headX - 1, 10 + breath, outline);
            SetPixelSafe(texture, headX + 2, 10 + breath, outline);
            FillRect(texture, bodyX + bodyWidth - 2, 8 + breath, 3, 3, tunic);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.14f), PixelsPerUnit);
        }

        private static void DrawKindlingOverlay(Texture2D texture, int frame, Color outline)
        {
            int normalized = NormalizeVariant(frame, CampfireKindleFrameCount);
            Color stick = Rgb(117, 72, 39);
            Color spark = Rgb(255, 206, 75);
            Color sparkWarm = Rgb(233, 93, 30);
            int reach = normalized <= 3 ? normalized : 7 - normalized;

            DrawThickLine(texture, P(11, 12), P(17, 8 + reach / 2), outline, 1);
            DrawLine(texture, P(11, 12), P(17, 8 + reach / 2), stick);
            DrawLine(texture, P(7, 12), P(16, 7 + reach), stick);
            if (normalized >= 2 && normalized <= 6)
            {
                SetPixelSafe(texture, 17, 8 + reach / 2, spark);
                SetPixelSafe(texture, 18, 10, sparkWarm);
                SetPixelSafe(texture, 15, 9, spark);
            }
        }

        private static int GetCampfirePoseCacheKey(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame,
            int pose)
        {
            return 200000
                + ((int)lifeStage * 4096)
                + ((int)gender * 2048)
                + (variant * 128)
                + (pose * 32)
                + frame;
        }

        private static Color GetSkinColor(int variant)
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

        private static readonly ResidentWalkFrame[] KindleBodyFrames =
        {
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 0, -2, 2, 2),
            new ResidentWalkFrame(-2, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(-2, -1, 1, -1, 1, -2, -1, 3, 2),
            new ResidentWalkFrame(-2, 0, 0, 0, 0, -1, 1, 2, -1),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 2, -1, -2),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, -1, 1, 1)
        };
    }
}
