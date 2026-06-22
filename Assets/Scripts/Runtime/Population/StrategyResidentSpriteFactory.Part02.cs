using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {

        private static void DrawBowAndArrow(Texture2D texture, int frame, Color outline)
        {
            int normalized = NormalizeVariant(frame, BowFrameCount);
            float draw = normalized <= 7 ? normalized / 7f : Mathf.Max(0f, 1f - ((normalized - 7) / 4f));
            int gripX = 15;
            int gripY = 15;
            int stringX = Mathf.RoundToInt(Mathf.Lerp(18f, 10f, draw));
            Color bowDark = Rgb(77, 47, 30);
            Color bow = Rgb(138, 86, 42);
            Color stringColor = Rgb(218, 202, 156);
            Color arrow = Rgb(180, 120, 58);
            Color feather = Rgb(225, 206, 143);

            DrawThickLine(texture, P(gripX + 2, gripY - 8), P(gripX + 4, gripY), outline, 1);
            DrawThickLine(texture, P(gripX + 4, gripY), P(gripX + 2, gripY + 8), outline, 1);
            DrawLine(texture, P(gripX + 2, gripY - 8), P(gripX + 4, gripY), bow);
            DrawLine(texture, P(gripX + 4, gripY), P(gripX + 2, gripY + 8), bow);
            DrawLine(texture, P(gripX + 3, gripY - 7), P(gripX + 3, gripY + 7), bowDark);
            DrawLine(texture, P(gripX + 2, gripY - 8), P(stringX, gripY), stringColor);
            DrawLine(texture, P(stringX, gripY), P(gripX + 2, gripY + 8), stringColor);
            DrawLine(texture, P(stringX - 1, gripY), P(gripX + 12, gripY), outline);
            DrawLine(texture, P(stringX, gripY), P(gripX + 11, gripY), arrow);
            SetPixelSafe(texture, gripX + 13, gripY, outline);
            FillRect(texture, stringX - 3, gripY - 1, 3, 2, feather);
        }

        private static void DrawButcherKnife(Texture2D texture, int frame, Color outline)
        {
            WoodcutToolFrame tool = GetButcherToolFrame(frame);
            Color handle = Rgb(104, 64, 39);
            Color metal = Rgb(150, 158, 150);
            Color metalLight = Rgb(218, 224, 211);

            DrawThickLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), outline, 1);
            DrawLine(texture, P(tool.HandleFromX, tool.HandleFromY), P(tool.HandleToX, tool.HandleToY), handle);
            int dir = tool.HeadDirection >= 0 ? 1 : -1;
            FillRect(texture, tool.HeadX - 1, tool.HeadY - 1, 4, 3, outline);
            FillRect(texture, tool.HeadX, tool.HeadY, 3, 2, metal);
            DrawLine(texture, P(tool.HeadX + dir * 2, tool.HeadY + 1), P(tool.HeadX + dir * 6, tool.HeadY - 2), metalLight);
            SetPixelSafe(texture, tool.HeadX + dir * 7, tool.HeadY - 3, outline);
        }

        private static void DrawFishingRod(Texture2D texture, int frame, Color outline)
        {
            int normalized = NormalizeVariant(frame, FishingFrameCount);
            float castT = normalized <= 5
                ? normalized / 5f
                : normalized >= 9
                    ? Mathf.Max(0.1f, 1f - ((normalized - 9) / 5f) * 0.45f)
                    : 1f;
            int gripX = 13;
            int gripY = 15;
            int tipX = Mathf.RoundToInt(Mathf.Lerp(12f, 19f, castT));
            int tipY = Mathf.RoundToInt(Mathf.Lerp(26f, 24f, castT));
            int hookX = Mathf.RoundToInt(Mathf.Lerp(11f, 22f, castT));
            int hookY = Mathf.RoundToInt(Mathf.Lerp(24f, 10f, castT)) + (normalized >= 8 ? (normalized % 2 == 0 ? 1 : -1) : 0);
            Color rodDark = Rgb(72, 48, 31);
            Color rod = Rgb(139, 92, 48);
            Color line = new Color(0.86f, 0.90f, 0.84f, 0.85f);
            Color floatRed = Rgb(206, 48, 43);
            Color floatWhite = Rgb(239, 231, 196);

            DrawThickLine(texture, P(gripX, gripY), P(tipX, tipY), outline, 1);
            DrawLine(texture, P(gripX, gripY), P(tipX, tipY), rod);
            DrawLine(texture, P(gripX + 1, gripY), P(tipX, tipY), rodDark);
            DrawLine(texture, P(tipX, tipY), P(hookX, hookY), line);
            FillRect(texture, hookX - 1, hookY - 1, 3, 4, outline);
            SetPixelSafe(texture, hookX, hookY + 1, floatRed);
            SetPixelSafe(texture, hookX, hookY, floatWhite);
            SetPixelSafe(texture, hookX + 1, hookY - 1, outline);
        }

        private static void DrawAxeHead(Texture2D texture, int x, int y, int direction, Color outline, Color metal, Color metalLight)
        {
            int dir = direction >= 0 ? 1 : -1;
            FillRect(texture, x - 1, y - 1, 3, 3, outline);
            FillRect(texture, x, y, 2, 2, metal);
            FillRect(texture, dir > 0 ? x + dir : x - 3, y + 1, 3, 2, metal);
            FillRect(texture, dir > 0 ? x + dir : x - 3, y - 2, 3, 2, metal);
            SetPixelSafe(texture, x + dir * 3, y + 1, metalLight);
            SetPixelSafe(texture, x + dir * 3, y - 1, metalLight);
        }

        private static void DrawPickHead(Texture2D texture, int x, int y, int direction, Color outline, Color metal, Color metalLight)
        {
            int dir = direction >= 0 ? 1 : -1;
            DrawThickLine(texture, P(x - dir * 5, y + 2), P(x + dir * 5, y - 2), outline, 1);
            DrawLine(texture, P(x - dir * 5, y + 2), P(x + dir * 5, y - 2), metal);
            SetPixelSafe(texture, x - dir * 6, y + 3, metalLight);
            SetPixelSafe(texture, x + dir * 6, y - 3, metalLight);
            FillRect(texture, x - 1, y - 1, 3, 3, outline);
            FillRect(texture, x, y, 2, 2, metal);
        }

        private static void DrawHammerHead(Texture2D texture, int x, int y, int direction, Color outline, Color metal, Color metalLight)
        {
            int dir = direction >= 0 ? 1 : -1;
            FillRect(texture, x - 3, y - 2, 7, 5, outline);
            FillRect(texture, x - 2, y - 1, 5, 3, metal);
            FillRect(texture, x + dir * 2, y, 3, 2, metalLight);
        }

        private static void DrawThickLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color, int radius)
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
                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        if (Mathf.Abs(ox) + Mathf.Abs(oy) <= radius)
                        {
                            SetPixelSafe(texture, x + ox, y + oy, color);
                        }
                    }
                }

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

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
        {
            DrawThickLine(texture, from, to, color, 0);
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static string GetSpriteName(string genderName, int variant, ResidentSpritePose pose, int frame)
        {
            return pose switch
            {
                ResidentSpritePose.Idle => $"{genderName} Resident Sprite {variant + 1}",
                ResidentSpritePose.Portrait => $"{genderName} Resident Portrait {variant + 1}",
                ResidentSpritePose.Woodcut => $"{genderName} Resident Woodcut {variant + 1}-{frame + 1}",
                ResidentSpritePose.Stonecut => $"{genderName} Resident Stonecut {variant + 1}-{frame + 1}",
                ResidentSpritePose.CoalMine => $"{genderName} Resident Coal Mine {variant + 1}-{frame + 1}",
                ResidentSpritePose.Construction => $"{genderName} Resident Construction {variant + 1}-{frame + 1}",
                ResidentSpritePose.Bow => $"{genderName} Resident Bow {variant + 1}-{frame + 1}",
                ResidentSpritePose.Butcher => $"{genderName} Resident Butcher {variant + 1}-{frame + 1}",
                ResidentSpritePose.Fishing => $"{genderName} Resident Fishing {variant + 1}-{frame + 1}",
                ResidentSpritePose.Crying => $"{genderName} Resident Crying {variant + 1}-{frame + 1}",
                ResidentSpritePose.Forage => $"{genderName} Resident Forage {variant + 1}-{frame + 1}",
                _ => $"{genderName} Resident Walk {variant + 1}-{frame + 1}"
            };
        }

        private static ResidentWalkFrame GetWalkFrame(int frame)
        {
            return WalkFrames[NormalizeVariant(frame, WalkFrameCount)];
        }

        private static ResidentWalkFrame GetWoodcutBodyFrame(int frame)
        {
            return WoodcutBodyFrames[NormalizeVariant(frame, WoodcutFrameCount)];
        }

        private static ResidentWalkFrame GetStonecutBodyFrame(int frame)
        {
            return StonecutBodyFrames[NormalizeVariant(frame, StonecutFrameCount)];
        }

        private static ResidentWalkFrame GetConstructionBodyFrame(int frame)
        {
            return ConstructionBodyFrames[NormalizeVariant(frame, ConstructionFrameCount)];
        }

        private static ResidentWalkFrame GetBowBodyFrame(int frame)
        {
            return BowBodyFrames[NormalizeVariant(frame, BowFrameCount)];
        }

        private static ResidentWalkFrame GetButcherBodyFrame(int frame)
        {
            return ButcherBodyFrames[NormalizeVariant(frame, ButcherFrameCount)];
        }

        private static ResidentWalkFrame GetFishingBodyFrame(int frame)
        {
            return FishingBodyFrames[NormalizeVariant(frame, FishingFrameCount)];
        }

        private static ResidentWalkFrame GetCryingBodyFrame(int frame)
        {
            return CryingBodyFrames[NormalizeVariant(frame, CryFrameCount)];
        }

        private static WoodcutToolFrame GetWoodcutToolFrame(int frame)
        {
            return WoodcutToolFrames[NormalizeVariant(frame, WoodcutFrameCount)];
        }

        private static WoodcutToolFrame GetStonecutToolFrame(int frame)
        {
            return StonecutToolFrames[NormalizeVariant(frame, StonecutFrameCount)];
        }

        private static WoodcutToolFrame GetConstructionToolFrame(int frame)
        {
            return ConstructionToolFrames[NormalizeVariant(frame, ConstructionFrameCount)];
        }

        private static WoodcutToolFrame GetButcherToolFrame(int frame)
        {
            return ButcherToolFrames[NormalizeVariant(frame, ButcherFrameCount)];
        }

        private static int GetCacheKey(
            StrategyResidentGender gender,
            int variant,
            ResidentSpritePose pose,
            int frame,
            StrategyResidentLifeStage lifeStage)
        {
            return ((int)lifeStage * 16384)
                + ((int)gender * 8192)
                + (variant * 1024)
                + ((int)pose * 64)
                + frame;
        }

        private static int NormalizeVariant(int variant, int variantCount)
        {
            if (variantCount <= 0)
            {
                return 0;
            }

            int normalized = variant % variantCount;
            return normalized < 0 ? normalized + variantCount : normalized;
        }

        private enum ResidentSpritePose
        {
            Idle,
            Walk,
            Portrait,
            Woodcut,
            Stonecut,
            CoalMine,
            Construction,
            Bow,
            Butcher,
            Fishing,
            Crying,
            Forage
        }

        private static readonly ResidentWalkFrame[] WalkFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 1, -1, 1, -1),
            new ResidentWalkFrame(1, -2, 2, -2, 2, 1, -1, 1, -1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, 0, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0),
            new ResidentWalkFrame(0, 1, -1, 1, -1, -1, 1, -1, 1),
            new ResidentWalkFrame(1, 2, -2, 2, -2, -1, 1, -1, 1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, 0, 0, 0)
        };

        private static readonly ResidentWalkFrame[] CryingBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 3, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 2, 3),
            new ResidentWalkFrame(1, 0, 0, 0, 0, 2, -2, 3, 3),
            new ResidentWalkFrame(1, 0, 0, 0, 0, 2, -1, 4, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -2, 3, 4),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 2, 3)
        };

        private static readonly ResidentWalkFrame[] WoodcutBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, -1, 1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, -1, -2, 2, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] WoodcutToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(8, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(9, 12, 13, 16, 21, 16, 21, 1)
        };

        private static readonly ResidentWalkFrame[] StonecutBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, -1, 1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, -1, -2, 2, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] StonecutToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(8, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(9, 12, 13, 16, 21, 16, 21, 1)
        };

        private static readonly ResidentWalkFrame[] ConstructionBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 3, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(-1, 0, 0, 0, 0, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 1, 1, -1, -1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, 0, -1, 1, 2),
            new ResidentWalkFrame(1, 0, 0, 0, 0, -1, -2, 3, 3),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -1, 1, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] ConstructionToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(1, 11, 15, 13, 25, 13, 25, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 26, 15, 26, 1),
            new WoodcutToolFrame(3, 6, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 9, 15, 16, 18, 16, 18, 1),
            new WoodcutToolFrame(8, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(9, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(10, 12, 14, 17, 20, 17, 20, 1),
            new WoodcutToolFrame(11, 12, 13, 16, 21, 16, 21, 1)
        };

        private static readonly ResidentWalkFrame[] BowBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -2, 2, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -2, 2, 1, 1),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -2, 2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -3, 2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -3, 3, 2, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -3, 3, 2, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -3, 3, 2, 2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 2, 1, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 1, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0)
        };

        private static readonly ResidentWalkFrame[] ButcherBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, -1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, 0, -2, 1, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -1, -2, 2, 3),
            new ResidentWalkFrame(1, -2, 2, -2, 2, -2, -1, 3, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -1, 1, 1, -1),
            new ResidentWalkFrame(-1, -1, 1, -1, 1, 1, 2, -2, -2),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 1, 1, -1, -1),
            new ResidentWalkFrame(0, 1, -1, 1, -1, 0, -1, 1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, -1, -2, 2, 3),
            new ResidentWalkFrame(0, 0, 0, 0, 0, 0, -1, 1, 1)
        };

        private static readonly WoodcutToolFrame[] ButcherToolFrames =
        {
            new WoodcutToolFrame(0, 12, 13, 15, 18, 15, 18, 1),
            new WoodcutToolFrame(1, 11, 15, 14, 23, 14, 23, -1),
            new WoodcutToolFrame(2, 9, 17, 15, 25, 15, 25, 1),
            new WoodcutToolFrame(3, 7, 18, 16, 24, 16, 24, 1),
            new WoodcutToolFrame(4, 8, 18, 17, 16, 17, 16, 1),
            new WoodcutToolFrame(5, 7, 18, 18, 10, 18, 10, 1),
            new WoodcutToolFrame(6, 8, 16, 18, 12, 18, 12, 1),
            new WoodcutToolFrame(7, 11, 14, 16, 21, 16, 21, 1),
            new WoodcutToolFrame(8, 10, 16, 14, 25, 14, 25, -1),
            new WoodcutToolFrame(9, 12, 13, 15, 18, 15, 18, 1)
        };

        private static readonly ResidentWalkFrame[] FishingBodyFrames =
        {
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 1, 1),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -2, 1, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -3, 2, 2, 2),
            new ResidentWalkFrame(1, -1, 1, -1, 1, -3, 3, 2, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -2, 3, 1, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 2, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0),
            new ResidentWalkFrame(0, 1, -1, 1, -1, -2, 2, 1, 1),
            new ResidentWalkFrame(1, 1, -1, 1, -1, -3, 3, 2, 2),
            new ResidentWalkFrame(1, 0, 0, 0, 0, -2, 3, 2, 2),
            new ResidentWalkFrame(0, -1, 1, -1, 1, -2, 2, 1, 1),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0),
            new ResidentWalkFrame(0, 0, 0, 0, 0, -1, 1, 0, 0)
        };
    }
}
