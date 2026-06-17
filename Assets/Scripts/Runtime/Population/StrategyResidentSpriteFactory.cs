using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        private const float PixelsPerUnit = 32f;
        public const int VariantCountPerGender = 5;
        public const int WalkFrameCount = 8;
        public const int WoodcutFrameCount = 10;
        public const int StonecutFrameCount = 10;
        public const int CoalMineFrameCount = 12;
        public const int ConstructionFrameCount = 12;
        public const int BowFrameCount = 12;
        public const int ButcherFrameCount = 10;
        public const int FishingFrameCount = 14;
        public const int CryFrameCount = 6;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetSprite(StrategyResidentGender gender)
        {
            return GetSprite(gender, 0);
        }

        public static Sprite GetSprite(StrategyResidentGender gender, int variant)
        {
            return GetSprite(gender, variant, StrategyResidentLifeStage.Adult);
        }

        public static Sprite GetSprite(StrategyResidentGender gender, int variant, StrategyResidentLifeStage lifeStage)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Idle, 0, lifeStage);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Idle, 0, lifeStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWalkSprite(StrategyResidentGender gender, int variant, int frame)
        {
            return GetWalkSprite(gender, variant, StrategyResidentLifeStage.Adult, frame);
        }

        public static Sprite GetWalkSprite(StrategyResidentGender gender, int variant, StrategyResidentLifeStage lifeStage, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, WalkFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Walk, normalizedFrame, lifeStage);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Walk, normalizedFrame, lifeStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWoodcutSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, WoodcutFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Woodcut, normalizedFrame, StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Woodcut, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStonecutSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, StonecutFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Stonecut, normalizedFrame, StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Stonecut, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetConstructionSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, ConstructionFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Construction, normalizedFrame, StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Construction, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetBowSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, BowFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Bow, normalizedFrame, StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Bow, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetButcherSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, ButcherFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Butcher, normalizedFrame, StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Butcher, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetFishingSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, FishingFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Fishing, normalizedFrame, StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Fishing, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCryingSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, CryFrameCount);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Crying, normalizedFrame, lifeStage);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Crying, normalizedFrame, lifeStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetPortraitSprite(StrategyResidentGender gender, int variant)
        {
            return GetPortraitSprite(gender, variant, StrategyResidentLifeStage.Adult);
        }

        public static Sprite GetPortraitSprite(StrategyResidentGender gender, int variant, StrategyResidentLifeStage lifeStage)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Portrait, 0, lifeStage);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Portrait, 0, lifeStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(
            StrategyResidentGender gender,
            int variant,
            ResidentSpritePose pose,
            int frame,
            StrategyResidentLifeStage lifeStage)
        {
            if (lifeStage == StrategyResidentLifeStage.Child)
            {
                return pose == ResidentSpritePose.Portrait
                    ? CreateChildPortraitSprite(gender, variant)
                    : CreateChildSprite(gender, variant, pose, frame);
            }

            if (pose == ResidentSpritePose.Portrait)
            {
                return CreatePortraitSprite(gender, variant);
            }

            Texture2D texture = new Texture2D(20, 28, TextureFormat.RGBA32, false)
            {
                name = gender == StrategyResidentGender.Male
                    ? GetSpriteName("Male", variant, pose, frame)
                    : GetSpriteName("Female", variant, pose, frame),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[20 * 28]);

            Color outline = Rgb(45, 31, 27);
            Color skin = variant switch
            {
                1 => Rgb(183, 126, 84),
                2 => Rgb(226, 177, 124),
                3 => Rgb(156, 105, 78),
                4 => Rgb(217, 151, 96),
                _ => Rgb(207, 158, 106)
            };
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color leg = variant == 3 ? Rgb(48, 43, 42) : Rgb(61, 51, 43);
            Color accent = GetAccentColor(gender, variant);
            ResidentWalkFrame walkFrame = pose switch
            {
                ResidentSpritePose.Walk => GetWalkFrame(frame),
                ResidentSpritePose.Woodcut => GetWoodcutBodyFrame(frame),
                ResidentSpritePose.Stonecut => GetStonecutBodyFrame(frame),
                ResidentSpritePose.CoalMine => GetCoalMineBodyFrame(frame),
                ResidentSpritePose.Construction => GetConstructionBodyFrame(frame),
                ResidentSpritePose.Bow => GetBowBodyFrame(frame),
                ResidentSpritePose.Butcher => GetButcherBodyFrame(frame),
                ResidentSpritePose.Fishing => GetFishingBodyFrame(frame),
                ResidentSpritePose.Crying => GetCryingBodyFrame(frame),
                _ => ResidentWalkFrame.Idle
            };

            if (gender == StrategyResidentGender.Male)
            {
                DrawMale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, walkFrame);
            }
            else
            {
                DrawFemale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, walkFrame);
            }

            if (pose == ResidentSpritePose.Crying)
            {
                DrawCryingOverlay(texture, frame, outline, skin, walkFrame.BodyYOffset);
            }
            else if (pose == ResidentSpritePose.Woodcut)
            {
                DrawWoodcutAxe(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Stonecut)
            {
                DrawStonecutPickaxe(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.CoalMine)
            {
                DrawCoalMinePickaxe(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Construction)
            {
                DrawConstructionHammer(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Bow)
            {
                DrawBowAndArrow(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Butcher)
            {
                DrawButcherKnife(texture, frame, outline);
            }
            else if (pose == ResidentSpritePose.Fishing)
            {
                DrawFishingRod(texture, frame, outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 20f, 28f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }

        private static Sprite CreatePortraitSprite(StrategyResidentGender gender, int variant)
        {
            Texture2D texture = new Texture2D(40, 40, TextureFormat.RGBA32, false)
            {
                name = gender == StrategyResidentGender.Male
                    ? GetSpriteName("Male", variant, ResidentSpritePose.Portrait, 0)
                    : GetSpriteName("Female", variant, ResidentSpritePose.Portrait, 0),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[40 * 40]);

            Color outline = Rgb(45, 31, 27);
            Color skin = variant switch
            {
                1 => Rgb(183, 126, 84),
                2 => Rgb(226, 177, 124),
                3 => Rgb(156, 105, 78),
                4 => Rgb(217, 151, 96),
                _ => Rgb(207, 158, 106)
            };
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color accent = GetAccentColor(gender, variant);

            FillEllipse(texture, 20, 7, 13, 4, new Color(0f, 0f, 0f, 0.24f));
            FillRect(texture, 12, 10, 16, 11, tunicDark);
            FillRect(texture, 14, 13, 12, 10, tunic);
            FillRect(texture, 17, 20, 6, 5, skin);
            DrawRectOutline(texture, 12, 10, 16, 11, outline);

            if (variant == 1 || variant == 4)
            {
                FillRect(texture, 14, 16, 12, 2, accent);
            }
            else if (variant == 2)
            {
                FillRect(texture, 18, 11, 4, 11, accent);
            }
            else
            {
                FillRect(texture, 13, 13, 3, 8, accent);
                FillRect(texture, 24, 13, 3, 8, accent);
            }

            FillEllipse(texture, 11, 25, 3, 4, skin);
            FillEllipse(texture, 29, 25, 3, 4, skin);
            FillEllipse(texture, 20, 25, 10, 11, outline);
            FillEllipse(texture, 20, 24, 8, 9, skin);

            if (gender == StrategyResidentGender.Male)
            {
                FillRect(texture, 13, 30, 14, 5, hair);
                FillRect(texture, 12, 25, 4, 6, hair);
                FillRect(texture, 24, 25, 4, 6, hair);
                if (variant == 1 || variant == 3 || variant == 4)
                {
                    FillRect(texture, 15, 18, 10, 3, hair);
                    FillRect(texture, 16, 16, 8, 2, hair);
                }
                else
                {
                    FillRect(texture, 18, 17, 4, 1, outline);
                }
            }
            else
            {
                FillRect(texture, 12, 30, 16, 5, hair);
                FillRect(texture, 10, 21, 4, 10, hair);
                FillRect(texture, 26, 21, 4, 10, hair);
                if (variant == 1 || variant == 4)
                {
                    FillRect(texture, 12, 31, 16, 2, accent);
                    FillRect(texture, 11, 27, 3, 3, accent);
                    FillRect(texture, 26, 27, 3, 3, accent);
                }
                else if (variant == 2)
                {
                    FillRect(texture, 28, 18, 2, 7, hair);
                    FillRect(texture, 29, 15, 1, 4, hair);
                }
            }

            SetPixelSafe(texture, 17, 25, outline);
            SetPixelSafe(texture, 23, 25, outline);
            SetPixelSafe(texture, 20, 22, Rgb(135, 89, 67));
            FillRect(texture, 18, 19, 4, 1, outline);
            SetPixelSafe(texture, 16, 22, Rgb(230, 151, 132));
            SetPixelSafe(texture, 24, 22, Rgb(230, 151, 132));

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(2f, 4f, 36f, 34f), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static Sprite CreateChildSprite(StrategyResidentGender gender, int variant, ResidentSpritePose pose, int frame)
        {
            Texture2D texture = new Texture2D(18, 22, TextureFormat.RGBA32, false)
            {
                name = gender == StrategyResidentGender.Male
                    ? GetSpriteName("Boy", variant, pose, frame)
                    : GetSpriteName("Girl", variant, pose, frame),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[18 * 22]);

            Color outline = Rgb(48, 33, 28);
            Color skin = variant switch
            {
                1 => Rgb(188, 132, 92),
                2 => Rgb(228, 183, 132),
                3 => Rgb(161, 111, 84),
                4 => Rgb(220, 157, 104),
                _ => Rgb(211, 164, 113)
            };
            Color hair = GetHairColor(gender, variant);
            Color tunic = Color.Lerp(GetTunicColor(gender, variant), Rgb(213, 177, 95), 0.22f);
            Color tunicDark = Color.Lerp(GetTunicDarkColor(gender, variant), Rgb(84, 61, 44), 0.16f);
            Color leg = Rgb(66, 54, 45);
            Color accent = GetAccentColor(gender, variant);
            ResidentWalkFrame walkFrame = pose switch
            {
                ResidentSpritePose.Walk => GetWalkFrame(frame),
                ResidentSpritePose.Crying => GetCryingBodyFrame(frame),
                _ => ResidentWalkFrame.Idle
            };
            int bodyY = Mathf.Clamp(walkFrame.BodyYOffset, 0, 1);

            FillEllipse(texture, 9, 2, 5, 2, new Color(0f, 0f, 0f, 0.24f));
            FillRect(texture, 6 + Mathf.Clamp(walkFrame.LeftLegX, -1, 1), 2, 2, 5 + bodyY, leg);
            FillRect(texture, 10 + Mathf.Clamp(walkFrame.RightLegX, -1, 1), 2, 2, 5 + bodyY, leg);
            FillRect(texture, 6 + Mathf.Clamp(walkFrame.LeftFootX, -1, 1), 1, 3, 1, outline);
            FillRect(texture, 10 + Mathf.Clamp(walkFrame.RightFootX, -1, 1), 1, 3, 1, outline);

            FillRect(texture, 5, 7 + bodyY, 8, 8, tunic);
            FillRect(texture, 5, 7 + bodyY, 2, 8, tunicDark);
            DrawRectOutline(texture, 5, 7 + bodyY, 8, 8, outline);
            FillRect(texture, 4 + Mathf.Clamp(walkFrame.LeftArmX, -1, 1), 10 + bodyY + Mathf.Clamp(walkFrame.LeftArmY, -1, 1), 2, 5, skin);
            FillRect(texture, 12 + Mathf.Clamp(walkFrame.RightArmX, -1, 1), 10 + bodyY + Mathf.Clamp(walkFrame.RightArmY, -1, 1), 2, 5, skin);

            if (variant == 1 || variant == 4)
            {
                FillRect(texture, 6, 12 + bodyY, 6, 2, accent);
            }
            else if (variant == 2)
            {
                FillRect(texture, 8, 7 + bodyY, 3, 8, accent);
            }
            else
            {
                SetPixelSafe(texture, 8, 13 + bodyY, accent);
                SetPixelSafe(texture, 10, 13 + bodyY, accent);
            }

            FillEllipse(texture, 9, 17 + bodyY, 4, 4, outline);
            FillEllipse(texture, 9, 17 + bodyY, 3, 3, skin);
            FillRect(texture, 6, 19 + bodyY, 7, 2, hair);
            FillRect(texture, 5, 17 + bodyY, 2, 3, hair);
            FillRect(texture, 12, 17 + bodyY, 2, 3, hair);

            if (gender == StrategyResidentGender.Female)
            {
                FillRect(texture, 4, 14 + bodyY, 2, 5, hair);
                FillRect(texture, 13, 14 + bodyY, 2, 5, hair);
                if (variant == 1 || variant == 4)
                {
                    FillRect(texture, 5, 19 + bodyY, 9, 1, accent);
                }
            }
            else if (variant == 1 || variant == 3)
            {
                FillRect(texture, 5, 20 + bodyY, 9, 1, accent);
            }

            SetPixelSafe(texture, 7, 17 + bodyY, outline);
            SetPixelSafe(texture, 11, 17 + bodyY, outline);
            SetPixelSafe(texture, 9, 15 + bodyY, Rgb(138, 92, 70));
            if (pose == ResidentSpritePose.Crying)
            {
                DrawChildCryingOverlay(texture, frame, outline, skin, bodyY);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 18f, 22f), new Vector2(0.5f, 0.08f), PixelsPerUnit);
        }
    }
}
