using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {
        public static Sprite GetCoalMineSprite(StrategyResidentGender gender, int variant, int frame)
        {
            int normalizedVariant = NormalizeVariant(variant, VariantCountPerGender);
            int normalizedFrame = NormalizeVariant(frame, CoalMineFrameCount);
            int cacheKey = GetCacheKey(
                gender,
                normalizedVariant,
                ResidentSpritePose.CoalMine,
                normalizedFrame,
                StrategyResidentLifeStage.Adult);
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(gender, normalizedVariant, ResidentSpritePose.CoalMine, normalizedFrame, StrategyResidentLifeStage.Adult);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static ResidentWalkFrame GetCoalMineBodyFrame(int frame)
        {
            return CoalMineBodyFrames[NormalizeVariant(frame, CoalMineFrameCount)];
        }

        private static WoodcutToolFrame GetCoalMineToolFrame(int frame)
        {
            return CoalMineToolFrames[NormalizeVariant(frame, CoalMineFrameCount)];
        }

        private static void DrawCoalMinePickaxe(Texture2D texture, int frame, Color outline)
        {
            WoodcutToolFrame tool = GetCoalMineToolFrame(frame);
            Color handleDark = Rgb(54, 38, 30);
            Color handle = Rgb(111, 73, 42);
            Color metal = Rgb(96, 111, 112);
            Color metalLight = Rgb(172, 186, 180);
            Color coal = Rgb(20, 24, 27);
            Color coalLight = Rgb(78, 90, 94);

            DrawThickLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), outline, 1);
            DrawLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), handle);
            DrawLine(texture, P(tool.HandleFromX + 1, tool.HandleFromY), P(tool.HandleToX + 1, tool.HandleToY), handleDark);
            DrawPickHead(texture, tool.HeadX, tool.HeadY, tool.HeadDirection, outline, metal, metalLight);

            int impact = NormalizeVariant(frame, CoalMineFrameCount);
            if (impact >= 5 && impact <= 7)
            {
                FillRect(texture, 13, 2, 5, 2, outline);
                FillRect(texture, 14, 3, 3, 1, coal);
                SetPixelSafe(texture, 16, 4, coalLight);
                SetPixelSafe(texture, 18, 5, coalLight);
                SetPixelSafe(texture, 12, 5, coal);
            }
        }

        private static readonly ResidentWalkFrame[] CoalMineBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 4, 3),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 2, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 2, 1, -3, -2),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 1, -1, -1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, 0, -2, 1, 3),
            new ResidentWalkFrame(1, 0, 0, 0, 0, -1, -2, 3, 3),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -1, 1, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] CoalMineToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 10, 15, 18, 13, 18, 13, 1),
            new WoodcutToolFrame(8, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(9, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(10, 12, 14, 17, 20, 17, 20, 1),
            new WoodcutToolFrame(11, 12, 13, 16, 21, 16, 21, 1)
        };
    }
}
