using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private const float PixelsPerUnit = 24f;
        public const int HouseVariantCount = 5;
        public const int LumberjackCampVariantCount = 3;
        public const int StonecutterCampVariantCount = 3;
        public const int SawmillVariantCount = 3;
        public const int MineVariantCount = 3;
        public const int CoalPitVariantCount = 3;
        public const int ClayPitVariantCount = 3;
        public const int KilnVariantCount = 3;
        public const int ForgeVariantCount = 3;
        public const int HunterCampVariantCount = 3;
        public const int FisherHutVariantCount = 3;
        public const int ForagerCampVariantCount = 1;
        public const int ScoutLodgeVariantCount = 1;
        public const int ChickenCoopVariantCount = 1;
        public const int TradingPostVariantCount = 3;
        public const int StarterCaravanCartVariantCount = 1;
        public const int StorageYardVariantCount = 3;
        public const int GranaryVariantCount = 3;
        public const int BridgeVariantCount = 1;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetCaches()
        {
            CachedSprites.Clear();
            CachedChickenCoopFrames.Clear();
            StrategyChickenCoopVisualProfile.ResetCache();
            StrategyBridgeVisualProfile.ResetCache();
        }

        private static bool TryGetBakedLayer(string id, int frame, out Sprite sprite)
        {
            return StrategyVisualCatalogProvider.TryGetSequenceSprite(id, frame, out sprite);
        }

        public static int GetVariantCount(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => HouseVariantCount,
                StrategyBuildTool.LumberjackCamp => LumberjackCampVariantCount,
                StrategyBuildTool.StonecutterCamp => StonecutterCampVariantCount,
                StrategyBuildTool.Sawmill => SawmillVariantCount,
                StrategyBuildTool.Mine => MineVariantCount,
                StrategyBuildTool.CoalPit => CoalPitVariantCount,
                StrategyBuildTool.ClayPit => ClayPitVariantCount,
                StrategyBuildTool.Kiln => KilnVariantCount,
                StrategyBuildTool.Forge => ForgeVariantCount,
                StrategyBuildTool.HunterCamp => HunterCampVariantCount,
                StrategyBuildTool.FisherHut => FisherHutVariantCount,
                StrategyBuildTool.ForagerCamp => ForagerCampVariantCount,
                StrategyBuildTool.ScoutLodge => ScoutLodgeVariantCount,
                StrategyBuildTool.ChickenCoop => ChickenCoopVariantCount,
                StrategyBuildTool.TradingPost => TradingPostVariantCount,
                StrategyBuildTool.StarterCaravanCart => StarterCaravanCartVariantCount,
                StrategyBuildTool.StorageYard => StorageYardVariantCount,
                StrategyBuildTool.Granary => GranaryVariantCount,
                StrategyBuildTool.Bridge => BridgeVariantCount,
                _ => 1
            };
        }

        public static bool TryGetBuildSprite(StrategyBuildTool tool, out Sprite sprite)
        {
            return TryGetBuildSprite(tool, 0, out sprite);
        }

        public static bool TryGetBuildSprite(StrategyBuildTool tool, int variant, out Sprite sprite)
        {
            int variantCount = GetVariantCount(tool);
            if (tool != StrategyBuildTool.House
                && tool != StrategyBuildTool.LumberjackCamp
                && tool != StrategyBuildTool.StonecutterCamp
                && tool != StrategyBuildTool.Sawmill
                && tool != StrategyBuildTool.Mine
                && tool != StrategyBuildTool.CoalPit
                && tool != StrategyBuildTool.ClayPit
                && tool != StrategyBuildTool.Kiln
                && tool != StrategyBuildTool.Forge
                && tool != StrategyBuildTool.HunterCamp
                && tool != StrategyBuildTool.FisherHut
                && tool != StrategyBuildTool.ForagerCamp
                && tool != StrategyBuildTool.ScoutLodge
                && tool != StrategyBuildTool.ChickenCoop
                && tool != StrategyBuildTool.TradingPost
                && tool != StrategyBuildTool.StarterCaravanCart
                && tool != StrategyBuildTool.StorageYard
                && tool != StrategyBuildTool.Granary
                && tool != StrategyBuildTool.Bridge)
            {
                sprite = null;
                return false;
            }

            int normalizedVariant = NormalizeVariant(variant, variantCount);
            int cacheKey = GetCacheKey(tool, normalizedVariant);
            if (tool != StrategyBuildTool.Bridge
                && StrategyVisualCatalogProvider.TryGetBuildingSprite(tool, normalizedVariant, out Sprite authored))
            {
                if (!CachedSprites.TryGetValue(cacheKey, out sprite) || sprite == null)
                {
                    sprite = CreateGroundAlignedAuthoredSprite(authored, tool);
                    CachedSprites[cacheKey] = sprite;
                }

                return true;
            }

            if (!CachedSprites.TryGetValue(cacheKey, out sprite) || sprite == null)
            {
                sprite = tool switch
                {
                    StrategyBuildTool.LumberjackCamp => CreateLumberjackCampSprite(normalizedVariant),
                    StrategyBuildTool.StonecutterCamp => CreateStonecutterCampSprite(normalizedVariant),
                    StrategyBuildTool.Sawmill => CreateSawmillSprite(normalizedVariant),
                    StrategyBuildTool.Mine => CreateMineSprite(normalizedVariant),
                    StrategyBuildTool.CoalPit => CreateCoalPitSprite(normalizedVariant),
                    StrategyBuildTool.ClayPit => CreateClayPitSprite(normalizedVariant),
                    StrategyBuildTool.Kiln => CreateKilnSprite(normalizedVariant),
                    StrategyBuildTool.Forge => CreateForgeSprite(normalizedVariant),
                    StrategyBuildTool.HunterCamp => CreateHunterCampSprite(normalizedVariant),
                    StrategyBuildTool.FisherHut => CreateFisherHutSprite(normalizedVariant),
                    StrategyBuildTool.ForagerCamp => CreateForagerCampSprite(normalizedVariant),
                    StrategyBuildTool.ScoutLodge => CreateScoutLodgeSprite(normalizedVariant),
                    StrategyBuildTool.ChickenCoop => CreateChickenCoopSprite(normalizedVariant),
                    StrategyBuildTool.TradingPost => CreateTradingPostSprite(normalizedVariant),
                    StrategyBuildTool.StarterCaravanCart => StrategyTradeCaravanSpriteFactory.GetSprite(),
                    StrategyBuildTool.StorageYard => CreateStorageYardSprite(normalizedVariant),
                    StrategyBuildTool.Granary => CreateGranarySprite(normalizedVariant),
                    StrategyBuildTool.Bridge => GetBridgeSprite(new Vector2Int(3, 1)),
                    _ => CreateHouseSprite(normalizedVariant)
                };
                CachedSprites[cacheKey] = sprite;
            }

            return sprite != null;
        }

        private static Sprite CreateGroundAlignedAuthoredSprite(Sprite source, StrategyBuildTool tool)
        {
            if (source == null || tool == StrategyBuildTool.Bridge)
            {
                return source;
            }

            float pivotY = StrategyBuildingVisualAlignment.GetSpritePivotY(tool);
            Sprite aligned = Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(0.5f, pivotY),
                source.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);
            aligned.name = source.name + " Ground Aligned";
            return aligned;
        }

        public static Sprite GetBridgeSprite(Vector2Int footprint)
        {
            Vector2Int normalizedFootprint = new Vector2Int(
                Mathf.Max(1, footprint.x),
                Mathf.Max(1, footprint.y));
            int cacheKey = 57344 + normalizedFootprint.x * 128 + normalizedFootprint.y;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = StrategyBridgeVisualProfile.TryCreateCompletedSprite(normalizedFootprint, out Sprite authored)
                    ? authored
                    : CreateBridgeSprite(normalizedFootprint);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateHouseSprite(int variant)
        {
            Texture2D texture = CreateTexture(96, 96, $"House 2.5D Sprite {variant + 1}");

            Color outline = Rgb(48, 34, 27);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color stoneDark = variant switch
            {
                1 => Rgb(62, 67, 70),
                4 => Rgb(88, 75, 58),
                _ => Rgb(72, 68, 61)
            };
            Color stoneLight = variant switch
            {
                1 => Rgb(132, 139, 137),
                4 => Rgb(144, 121, 86),
                _ => Rgb(123, 115, 97)
            };
            Color wall = variant switch
            {
                1 => Rgb(156, 153, 139),
                2 => Rgb(213, 186, 130),
                3 => Rgb(198, 169, 129),
                4 => Rgb(222, 212, 179),
                _ => Rgb(205, 173, 122)
            };
            Color wallLight = variant switch
            {
                1 => Rgb(184, 181, 164),
                2 => Rgb(238, 210, 157),
                3 => Rgb(224, 193, 149),
                4 => Rgb(244, 232, 194),
                _ => Rgb(230, 198, 144)
            };
            Color wallSide = variant switch
            {
                1 => Rgb(112, 113, 106),
                2 => Rgb(166, 132, 82),
                3 => Rgb(142, 104, 72),
                4 => Rgb(175, 158, 124),
                _ => Rgb(163, 128, 89)
            };
            Color timber = variant switch
            {
                1 => Rgb(72, 60, 50),
                2 => Rgb(116, 80, 42),
                3 => Rgb(61, 44, 34),
                4 => Rgb(96, 51, 36),
                _ => Rgb(93, 57, 34)
            };
            Color timberLight = variant switch
            {
                1 => Rgb(111, 95, 74),
                2 => Rgb(156, 109, 53),
                3 => Rgb(102, 76, 55),
                4 => Rgb(142, 76, 52),
                _ => Rgb(130, 83, 48)
            };
            Color roof = variant switch
            {
                1 => Rgb(70, 84, 95),
                2 => Rgb(184, 143, 54),
                3 => Rgb(111, 67, 42),
                4 => Rgb(146, 52, 45),
                _ => Rgb(143, 89, 40)
            };
            Color roofLight = variant switch
            {
                1 => Rgb(119, 141, 151),
                2 => Rgb(226, 187, 83),
                3 => Rgb(167, 106, 61),
                4 => Rgb(206, 88, 67),
                _ => Rgb(190, 128, 57)
            };
            Color roofDark = variant switch
            {
                1 => Rgb(42, 53, 64),
                2 => Rgb(125, 91, 39),
                3 => Rgb(69, 43, 31),
                4 => Rgb(92, 37, 39),
                _ => Rgb(92, 56, 32)
            };
            Color window = Rgb(92, 142, 152);
            Color windowLight = Rgb(185, 219, 210);
            Color door = variant switch
            {
                1 => Rgb(68, 50, 39),
                3 => Rgb(76, 49, 35),
                4 => Rgb(102, 45, 35),
                _ => Rgb(92, 55, 32)
            };

            int frontLeft = variant switch { 2 => 20, 3 => 27, _ => 23 };
            int frontRight = variant switch { 2 => 63, 3 => 59, 4 => 62, _ => 60 };
            int wallTop = variant switch { 1 => 51, 3 => 55, _ => 50 };
            int roofPeakY = variant switch { 2 => 72, 3 => 81, 4 => 74, _ => 76 };
            int roofLeft = variant == 2 ? 12 : 15;
            int roofRightFront = variant switch { 3 => 66, 4 => 73, _ => 69 };
            int roofEaveY = variant == 2 ? 49 : 51;
            int roofBaseY = variant == 3 ? 45 : 42;

            FillEllipse(texture, 48, 14, 34, 8, shadow);

            Vector2Int[] platform = variant == 2
                ? new[] { P(14, 17), P(47, 7), P(84, 18), P(51, 31) }
                : new[] { P(17, 17), P(47, 7), P(81, 18), P(51, 30) };
            FillPolygon(texture, platform, stoneDark);
            DrawPolygon(texture, platform, outline);
            Vector2Int[] platformTop = variant == 2
                ? new[] { P(19, 18), P(48, 10), P(79, 19), P(51, 28) }
                : new[] { P(22, 18), P(48, 10), P(76, 19), P(51, 27) };
            FillPolygon(texture, platformTop, stoneLight);
            DrawHouseFoundationDetails(texture, variant, outline, stoneDark, stoneLight);

            Vector2Int[] sideWall = { P(frontRight - 1, 22), P(76, 29), P(76, wallTop + 2), P(frontRight - 1, wallTop - 1) };
            FillPolygon(texture, sideWall, wallSide);
            DrawPolygon(texture, sideWall, outline);

            Vector2Int[] frontWall = { P(frontLeft, 22), P(frontRight, 22), P(frontRight, wallTop), P(frontLeft, wallTop) };
            FillPolygon(texture, frontWall, wall);
            DrawPolygon(texture, frontWall, outline);

            Vector2Int[] gable = { P(frontLeft + 3, wallTop), P(43, variant == 3 ? 69 : 65), P(frontRight + 1, wallTop) };
            FillPolygon(texture, gable, wallLight);
            DrawPolygon(texture, gable, outline);

            DrawWallDetails(texture, variant, outline, timber, timberLight, wallSide, frontLeft, frontRight, wallTop);
            DrawDoorAndWindows(texture, variant, outline, roofLight, window, windowLight, door, frontLeft, frontRight);

            int chimneyLeft = variant switch { 1 => 31, 2 => 60, 3 => 53, 4 => 56, _ => 55 };
            int chimneyBottom = variant == 3 ? 65 : 61;
            Vector2Int[] chimney =
            {
                P(chimneyLeft, chimneyBottom),
                P(chimneyLeft + 7, chimneyBottom + 3),
                P(chimneyLeft + 7, chimneyBottom + 15),
                P(chimneyLeft, chimneyBottom + 12)
            };
            FillPolygon(texture, chimney, variant == 1 ? Rgb(91, 83, 75) : Rgb(111, 78, 62));
            DrawPolygon(texture, chimney, outline);
            DrawLine(texture, P(chimneyLeft + 1, chimneyBottom + 8), P(chimneyLeft + 6, chimneyBottom + 10), stoneLight);

            Vector2Int[] roofFront = { P(roofLeft, roofEaveY), P(43, roofPeakY), P(roofRightFront, roofEaveY + 1), P(frontRight, roofBaseY), P(frontLeft + 3, roofBaseY) };
            FillPolygon(texture, roofFront, roof);
            DrawPolygon(texture, roofFront, outline);
            Vector2Int[] roofSide = { P(43, roofPeakY), P(84, variant == 3 ? 61 : 58), P(76, 44), P(roofRightFront, roofEaveY + 1) };
            FillPolygon(texture, roofSide, roofDark);
            DrawPolygon(texture, roofSide, outline);

            DrawRoofDetails(texture, variant, outline, roofLight, roofDark, roofLeft, roofEaveY, roofPeakY, roofBaseY, frontLeft, frontRight, roofRightFront);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(8f, 6f, 80f, 80f), new Vector2(0.5f, 0.10f), PixelsPerUnit);
        }

        private static void DrawHouseFoundationDetails(
            Texture2D texture,
            int variant,
            Color outline,
            Color stoneDark,
            Color stoneLight)
        {
            Color mortar = Color.Lerp(stoneDark, stoneLight, 0.58f);
            DrawLine(texture, P(24, 18), P(50, 11), mortar);
            DrawLine(texture, P(50, 11), P(74, 18), mortar);
            DrawLine(texture, P(31, 15), P(34, 20), stoneDark);
            DrawLine(texture, P(61, 14), P(59, 21), stoneDark);
            FillRect(texture, variant == 3 ? 36 : 34, 15, 14, 3, stoneLight);
            DrawRectOutline(texture, variant == 3 ? 36 : 34, 15, 14, 3, outline);
        }

        private static void DrawWallDetails(
            Texture2D texture,
            int variant,
            Color outline,
            Color timber,
            Color timberLight,
            Color wallSide,
            int frontLeft,
            int frontRight,
            int wallTop)
        {
            if (variant == 1)
            {
                for (int y = 27; y < wallTop - 2; y += 6)
                {
                    DrawLine(texture, P(frontLeft + 2, y), P(frontRight - 2, y), wallSide);
                }

                for (int x = frontLeft + 5; x < frontRight - 3; x += 10)
                {
                    DrawLine(texture, P(x, 23), P(x, wallTop - 2), wallSide);
                }

                return;
            }

            int timberTop = wallTop - 3;
            FillRect(texture, frontLeft + 1, 22, 4, wallTop - 21, timber);
            FillRect(texture, frontRight - 3, 22, 4, wallTop - 21, timber);
            FillRect(texture, frontLeft + 2, timberTop, frontRight - frontLeft - 3, 4, timber);
            DrawThickLine(texture, P(frontLeft + 4, 23), P(frontRight - 4, timberTop), timberLight, 1);

            if (variant != 2)
            {
                DrawThickLine(texture, P(frontRight - 4, 23), P(frontLeft + 5, timberTop), timber, 1);
            }

            if (variant == 3)
            {
                FillRect(texture, frontLeft + 14, 23, 3, wallTop - 22, timber);
                FillRect(texture, frontLeft + 25, 23, 3, wallTop - 22, timberLight);
            }

            if (variant == 4)
            {
                FillRect(texture, 28, 36, 24, 3, timberLight);
                FillRect(texture, 29, 22, 3, 15, timber);
                FillRect(texture, 49, 22, 3, 15, timber);
            }
        }

        private static void DrawDoorAndWindows(
            Texture2D texture,
            int variant,
            Color outline,
            Color roofLight,
            Color window,
            Color windowLight,
            Color door,
            int frontLeft,
            int frontRight)
        {
            int doorX = variant switch { 3 => 38, 4 => 33, _ => 35 };
            int doorW = variant == 1 ? 11 : 10;
            FillRect(texture, doorX, 22, doorW, 19, door);
            if (variant == 1)
            {
                FillEllipse(texture, doorX + doorW / 2, 40, doorW / 2, 4, door);
            }

            DrawRectOutline(texture, doorX, 22, doorW, 19, outline);
            SetPixelSafe(texture, doorX + doorW - 2, 31, roofLight);

            if (variant == 3)
            {
                DrawWindow(texture, 31, 36, 6, 8, outline, window, windowLight);
                DrawWindow(texture, 51, 36, 6, 8, outline, window, windowLight);
                DrawWindow(texture, 39, 51, 7, 7, outline, window, windowLight);
            }
            else if (variant == 4)
            {
                DrawWindow(texture, 52, 34, 8, 8, outline, window, windowLight);
                FillRect(texture, 64, 31, 5, 12, Rgb(45, 102, 75));
                DrawRectOutline(texture, 64, 31, 5, 12, outline);
            }
            else
            {
                DrawWindow(texture, frontRight - 11, 34, 8, 8, outline, window, windowLight);
            }

            if (variant != 3)
            {
                DrawWindow(texture, 64, 34, 7, 8, outline, Rgb(69, 108, 117), windowLight);
            }

            if (variant == 2)
            {
                FillRect(texture, frontLeft + 5, 34, 7, 7, Rgb(94, 125, 89));
                DrawRectOutline(texture, frontLeft + 5, 34, 7, 7, outline);
            }
        }

        private static void DrawWindow(Texture2D texture, int x, int y, int width, int height, Color outline, Color window, Color windowLight)
        {
            FillRect(texture, x, y, width, height, window);
            FillRect(texture, x + 1, y + height - 3, Mathf.Max(1, width - 2), 2, windowLight);
            DrawRectOutline(texture, x, y, width, height, outline);
            DrawLine(texture, P(x + width / 2, y), P(x + width / 2, y + height - 1), outline);
        }
    }
}
