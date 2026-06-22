using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        public const int ForageFrameCount = 12;

        public static Sprite GetForageSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            StrategyResourceType resource,
            int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int encodedFrame = GetForageEncodedFrame(resource, frame);
            int cacheKey = GetCacheKey(gender, normalizedVariant, ResidentSpritePose.Forage, encodedFrame, lifeStage);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.Forage, encodedFrame, lifeStage);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static ResidentWalkFrame GetForageBodyFrame(int frame)
        {
            return ForageBodyFrames[GetForageFrame(frame)];
        }

        private static void DrawForageTools(Texture2D texture, int encodedFrame, Color outline, Color skin)
        {
            int frame = GetForageFrame(encodedFrame);
            StrategyResourceType resource = GetForageResource(encodedFrame);
            int reach = GetForageReach(frame);
            int handX = 14 + Mathf.Min(3, reach);
            int handY = 14 - Mathf.Min(5, reach);

            DrawLine(texture, P(13, 15), P(handX, handY), skin);
            FillRect(texture, handX - 1, handY - 1, 3, 2, skin);
            DrawForageBasket(texture, 10, 5, frame, resource, outline);
            DrawForageHint(texture, 15, 4, frame, resource, outline);
        }

        private static void DrawChildForageTools(Texture2D texture, int encodedFrame, Color outline, Color skin)
        {
            int frame = GetForageFrame(encodedFrame);
            StrategyResourceType resource = GetForageResource(encodedFrame);
            int reach = GetForageReach(frame);
            int handX = 12 + Mathf.Min(3, reach);
            int handY = 10 - Mathf.Min(4, reach);

            DrawLine(texture, P(11, 12), P(handX, handY), skin);
            FillRect(texture, handX - 1, handY - 1, 3, 2, skin);
            DrawForageBasket(texture, 8, 3, frame, resource, outline);
            DrawForageHint(texture, 13, 2, frame, resource, outline);
        }

        private static void DrawForageBasket(
            Texture2D texture,
            int x,
            int y,
            int frame,
            StrategyResourceType resource,
            Color outline)
        {
            Color basket = Rgb(132, 84, 43);
            Color light = Rgb(190, 128, 64);
            FillRect(texture, x, y, 7, 4, basket);
            DrawRectOutline(texture, x, y, 7, 4, outline);
            DrawLine(texture, P(x + 1, y + 4), P(x + 5, y + 6), light);
            DrawLine(texture, P(x + 5, y + 6), P(x + 7, y + 4), outline);

            Color item = GetForageItemColor(resource, frame);
            SetPixelSafe(texture, x + 2, y + 4, item);
            SetPixelSafe(texture, x + 4, y + 5, item);
            SetPixelSafe(texture, x + 5, y + 4, item);
        }

        private static void DrawForageHint(
            Texture2D texture,
            int x,
            int y,
            int frame,
            StrategyResourceType resource,
            Color outline)
        {
            Color item = GetForageItemColor(resource, frame);
            if (frame == 4 || frame == 9)
            {
                FillRect(texture, x, y, 3, 2, outline);
                SetPixelSafe(texture, x + 1, y + 1, item);
                return;
            }

            SetPixelSafe(texture, x, y, item);
            SetPixelSafe(texture, x + 2, y + 1, item);
        }

        private static int GetForageEncodedFrame(StrategyResourceType resource, int frame)
        {
            return GetForageResourceIndex(resource) * ForageFrameCount + GetForageFrame(frame);
        }

        private static int GetForageFrame(int encodedFrame)
        {
            return NormalizeVariant(encodedFrame, ForageFrameCount);
        }

        private static StrategyResourceType GetForageResource(int encodedFrame)
        {
            return (Mathf.Abs(encodedFrame) / ForageFrameCount) switch
            {
                1 => StrategyResourceType.Mushrooms,
                2 => StrategyResourceType.Roots,
                _ => StrategyResourceType.Berries
            };
        }

        private static int GetForageResourceIndex(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Mushrooms => 1,
                StrategyResourceType.Roots => 2,
                _ => 0
            };
        }

        private static int GetForageReach(int frame)
        {
            return frame switch
            {
                2 => 2,
                3 => 4,
                4 => 5,
                5 => 3,
                7 => 2,
                8 => 4,
                9 => 5,
                10 => 2,
                _ => 0
            };
        }

        private static Color GetForageItemColor(StrategyResourceType resource, int frame)
        {
            return resource switch
            {
                StrategyResourceType.Mushrooms => frame % 2 == 0 ? Rgb(210, 84, 63) : Rgb(226, 201, 164),
                StrategyResourceType.Roots => frame % 2 == 0 ? Rgb(145, 94, 54) : Rgb(101, 152, 72),
                _ => frame % 2 == 0 ? Rgb(175, 47, 86) : Rgb(104, 138, 56)
            };
        }

        private static readonly ResidentWalkFrame[] ForageBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 2, -2, -3),
            new ResidentWalkFrame(-2, 0, 0, 0, 0, 2, 3, -3, -4),
            new ResidentWalkFrame(-2, 1, -1, 1, -1, 2, 3, -4, -5),
            new ResidentWalkFrame(-1, 1, -1, 1, -1, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 1, 0, -1),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 2, -1, -3),
            new ResidentWalkFrame(-2, 0, 0, 0, 0, 2, 3, -3, -4),
            new ResidentWalkFrame(-2, 1, -1, 1, -1, 2, 3, -4, -5),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 1, -1, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0)
        };
    }
}
