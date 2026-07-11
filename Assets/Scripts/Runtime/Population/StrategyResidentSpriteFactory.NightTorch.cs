using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        public const int NightTorchWalkFrameCount = WalkFrameCount;
        public const int NightTorchLightFrameCount = 12;

        public static Sprite GetNightTorchWalkSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, NightTorchWalkFrameCount);
            if (StrategyVisualCatalogProvider.TryGetResidentSprite(
                    gender,
                    lifeStage,
                    StrategyResidentVisualPose.NightTorchWalk,
                    normalizedVariant,
                    normalizedFrame,
                    out Sprite authored))
            {
                return authored;
            }

            int cacheKey = GetNightTorchCacheKey(gender, normalizedVariant, lifeStage, normalizedFrame, 0);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateNightTorchSprite(gender, normalizedVariant, lifeStage, normalizedFrame, false);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetNightTorchLightSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, NightTorchLightFrameCount);
            if (StrategyVisualCatalogProvider.TryGetResidentSprite(
                    gender,
                    lifeStage,
                    StrategyResidentVisualPose.NightTorchLight,
                    normalizedVariant,
                    normalizedFrame,
                    out Sprite authored))
            {
                return authored;
            }

            int cacheKey = GetNightTorchCacheKey(gender, normalizedVariant, lifeStage, normalizedFrame, 1);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateNightTorchSprite(gender, normalizedVariant, lifeStage, normalizedFrame, true);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateNightTorchSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame,
            bool lighting)
        {
            Texture2D texture = new Texture2D(20, 30, TextureFormat.RGBA32, false)
            {
                name = $"{gender} Resident Night Torch {(lighting ? "Light" : "Walk")} {variant + 1}-{frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[20 * 30]);

            Color outline = Rgb(45, 31, 27);
            Color skin = GetSkinColor(variant);
            Color hair = GetHairColor(gender, variant);
            Color tunic = GetTunicColor(gender, variant);
            Color tunicDark = GetTunicDarkColor(gender, variant);
            Color leg = variant == 3 ? Rgb(48, 43, 42) : Rgb(61, 51, 43);
            Color accent = GetAccentColor(gender, variant);
            ResidentWalkFrame body = lighting
                ? NightTorchLightBodyFrames[NormalizeVariant(frame, NightTorchLightFrameCount)]
                : GetNightTorchWalkBodyFrame(frame);

            if (gender == StrategyResidentGender.Male)
            {
                DrawMale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }
            else
            {
                DrawFemale(texture, variant, outline, skin, hair, tunic, tunicDark, leg, accent, body);
            }

            DrawNightTorchOverlay(texture, frame, body.BodyYOffset, lighting, outline, skin);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 20f, 30f), new Vector2(0.5f, 0.075f), PixelsPerUnit);
        }

        private static ResidentWalkFrame GetNightTorchWalkBodyFrame(int frame)
        {
            ResidentWalkFrame walk = GetWalkFrame(frame);
            int torchArmY = NormalizeVariant(frame, NightTorchWalkFrameCount) switch
            {
                1 or 2 => 1,
                5 or 6 => -1,
                _ => 0
            };
            return new ResidentWalkFrame(
                walk.BodyYOffset,
                walk.LeftLegX,
                walk.RightLegX,
                walk.LeftFootX,
                walk.RightFootX,
                walk.LeftArmX,
                1,
                walk.LeftArmY,
                torchArmY);
        }

        private static void DrawNightTorchOverlay(
            Texture2D texture,
            int frame,
            int bodyY,
            bool lighting,
            Color outline,
            Color skin)
        {
            if (lighting)
            {
                DrawNightTorchLightingOverlay(texture, frame, bodyY, outline, skin);
                return;
            }

            int normalized = NormalizeVariant(frame, NightTorchWalkFrameCount);
            int sway = normalized switch
            {
                1 or 2 => 1,
                5 or 6 => -1,
                _ => 0
            };
            int bob = normalized is 2 or 6 ? 1 : 0;
            Vector2Int hand = P(15, 15 + bodyY + Mathf.Clamp(sway, -1, 1));
            Vector2Int grip = P(16 + Mathf.Clamp(sway, -1, 1), 18 + bodyY);
            Vector2Int tip = P(17 + Mathf.Clamp(sway, -1, 1), 25 + bob);

            DrawTorchHandle(texture, hand, tip, outline);
            FillRect(texture, grip.x - 1, grip.y - 1, 2, 2, skin);
            DrawTorchFlame(texture, tip.x, tip.y, normalized, 1f);
        }

        private static void DrawNightTorchLightingOverlay(
            Texture2D texture,
            int frame,
            int bodyY,
            Color outline,
            Color skin)
        {
            int normalized = NormalizeVariant(frame, NightTorchLightFrameCount);
            int reach = normalized <= 5 ? normalized : 11 - normalized;
            int lean = Mathf.Clamp(reach, 0, 4);
            Vector2Int hand = P(14 + Mathf.Min(2, lean), 15 + bodyY - Mathf.Min(2, lean / 2));
            Vector2Int tip = P(16 + Mathf.Min(3, lean), 25 - Mathf.Min(7, reach + 1));

            DrawTorchHandle(texture, hand, tip, outline);
            FillRect(texture, hand.x - 1, hand.y - 1, 2, 2, skin);
            DrawTorchFlame(texture, tip.x, tip.y, normalized, Mathf.Lerp(0.86f, 1.18f, reach / 5f));

            if (normalized >= 4 && normalized <= 9)
            {
                DrawNightTorchSparks(texture, tip.x, tip.y, normalized);
            }
        }

        private static void DrawTorchHandle(Texture2D texture, Vector2Int from, Vector2Int to, Color outline)
        {
            Color wood = Rgb(92, 54, 28);
            Color woodLight = Rgb(143, 88, 43);
            DrawThickLine(texture, from, to, outline, 1);
            DrawLine(texture, from, to, wood);
            Vector2Int mid = P((from.x + to.x) / 2, (from.y + to.y) / 2);
            SetPixelSafe(texture, mid.x, mid.y, woodLight);
            SetPixelSafe(texture, to.x, to.y - 1, Rgb(48, 42, 35));
        }

        private static void DrawTorchFlame(Texture2D texture, int x, int y, int frame, float scale)
        {
            int sway = frame switch
            {
                1 or 6 or 10 => -1,
                3 or 5 or 8 => 1,
                _ => 0
            };
            int outerHeight = Mathf.Max(3, Mathf.RoundToInt(4f * scale));
            int midHeight = Mathf.Max(2, Mathf.RoundToInt(3f * scale));
            Color outer = new(0.93f, 0.18f, 0.06f, 0.88f);
            Color mid = new(1f, 0.54f, 0.11f, 0.96f);
            Color inner = new(1f, 0.88f, 0.32f, 0.98f);
            Color core = new(1f, 0.98f, 0.68f, 0.94f);

            FillEllipse(texture, x + sway, y + 1, 2, outerHeight, outer);
            FillEllipse(texture, x, y, 1, midHeight, mid);
            FillEllipse(texture, x, y - 1, 1, Mathf.Max(1, midHeight - 1), inner);
            SetPixelSafe(texture, x, y, core);
            SetPixelSafe(texture, x + sway, y + outerHeight, outer);
        }

        private static void DrawNightTorchSparks(Texture2D texture, int x, int y, int frame)
        {
            Color spark = Rgb(255, 218, 92);
            Color sparkWarm = Rgb(232, 92, 30);
            int drift = frame - 6;
            SetPixelSafe(texture, x + 1, y + 1, spark);
            SetPixelSafe(texture, x + 2, y - 1 + Mathf.Abs(drift % 2), sparkWarm);
            SetPixelSafe(texture, x - 1, y + 2, spark);
            if (frame % 2 == 0)
            {
                SetPixelSafe(texture, x + 3, y, spark);
                SetPixelSafe(texture, x, y + 3, sparkWarm);
            }
        }

        private static int GetNightTorchCacheKey(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            int frame,
            int pose)
        {
            return 300000
                + ((int)lifeStage * 8192)
                + ((int)gender * 4096)
                + (variant * 512)
                + (pose * 128)
                + frame;
        }

        private static readonly ResidentWalkFrame[] NightTorchLightBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 1, 0, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 2, 0, 2),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 0, 2, 0, 2),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, 0, 3),
            new ResidentWalkFrame(-2, -1, 1, -1, 1, 1, 3, 0, 3),
            new ResidentWalkFrame(-2, 0, 0, 0, 0, 1, 3, 0, 4),
            new ResidentWalkFrame(-2, 0, 0, 0, 0, 1, 3, 0, 4),
            new ResidentWalkFrame(-2, 0, 0, 0, 0, 1, 3, 0, 3),
            new ResidentWalkFrame(-1, 1, -1, 1, -1, 0, 2, 0, 2),
            new ResidentWalkFrame(-1, 1, -1, 1, -1, 0, 2, 0, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 1, 0, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 1, 0, 0)
        };
    }
}
