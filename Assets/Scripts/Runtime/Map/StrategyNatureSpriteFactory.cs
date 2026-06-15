using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyNaturePropKind
    {
        LargeTree,
        SmallTree,
        Bush,
        ForestGroup,
        Boulder,
        RockCluster,
        Cliff
    }

    internal static partial class StrategyNatureSpriteFactory
    {
        private const float TreePixelsPerUnit = 30f;
        private const float BushPixelsPerUnit = 28f;
        private const float StonePixelsPerUnit = 30f;
        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static int GetVariantCount(StrategyNaturePropKind kind)
        {
            return kind switch
            {
                StrategyNaturePropKind.LargeTree => 5,
                StrategyNaturePropKind.SmallTree => 3,
                StrategyNaturePropKind.Bush => 4,
                StrategyNaturePropKind.ForestGroup => 3,
                StrategyNaturePropKind.Boulder => 6,
                StrategyNaturePropKind.RockCluster => 5,
                StrategyNaturePropKind.Cliff => 4,
                _ => 1
            };
        }

        public static Sprite GetSprite(StrategyNaturePropKind kind, int variant)
        {
            int normalizedVariant = NormalizeVariant(variant, GetVariantCount(kind));
            int cacheKey = ((int)kind * 16) + normalizedVariant;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(kind, normalizedVariant);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetTreeGrowthSprite(int stage, int variant)
        {
            int normalizedStage = Mathf.Clamp(stage, 0, 2);
            if (normalizedStage == 1)
            {
                return GetSprite(StrategyNaturePropKind.SmallTree, variant);
            }

            if (normalizedStage >= 2)
            {
                return GetSprite(StrategyNaturePropKind.LargeTree, variant);
            }

            int normalizedVariant = NormalizeVariant(variant, 5);
            int cacheKey = 4096 + normalizedVariant;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSaplingSprite(normalizedVariant);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetFelledTreeSprite(int variant)
        {
            int normalizedVariant = NormalizeVariant(variant, 5);
            int cacheKey = 8192 + normalizedVariant;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateFelledTreeSprite(normalizedVariant);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetSplitLogsSprite(int variant)
        {
            int normalizedVariant = NormalizeVariant(variant, 5);
            int cacheKey = 12288 + normalizedVariant;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSplitLogsSprite(normalizedVariant);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCarriedLogsSprite()
        {
            const int cacheKey = 12360;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedLogsSprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCarriedStoneSprite()
        {
            const int cacheKey = 12420;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedStoneSprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCarriedGameSprite()
        {
            const int cacheKey = 12480;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedGameSprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetCarriedFishSprite()
        {
            const int cacheKey = 12540;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateCarriedFishSprite();
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyNaturePropKind kind, int variant)
        {
            return kind switch
            {
                StrategyNaturePropKind.SmallTree => CreateSmallTreeSprite(variant),
                StrategyNaturePropKind.Bush => CreateBushSprite(variant),
                StrategyNaturePropKind.ForestGroup => CreateForestGroupSprite(variant),
                StrategyNaturePropKind.Boulder => CreateBoulderSprite(variant),
                StrategyNaturePropKind.RockCluster => CreateRockClusterSprite(variant),
                StrategyNaturePropKind.Cliff => CreateCliffSprite(variant),
                _ => CreateLargeTreeSprite(variant)
            };
        }

        private static Sprite CreateLargeTreeSprite(int variant)
        {
            Texture2D texture = CreateTexture(64, 80, $"Large Tree {variant + 1}");
            Color outline = Rgb(32, 31, 23);
            Color shadow = new Color(0f, 0f, 0f, 0.26f);
            Color trunk = GetTrunkColor(variant);
            Color trunkDark = Shift(trunk, -0.16f);
            Color leaf = GetLeafColor(variant);
            Color leafDark = Shift(leaf, -0.20f);
            Color leafLight = Shift(leaf, 0.13f);

            FillEllipse(texture, 32, 9, 19, 5, shadow);
            FillRect(texture, 28, 10, 8, 27, trunkDark);
            FillRect(texture, 30, 10, 7, 28, trunk);
            FillRect(texture, 31, 13, 2, 20, Shift(trunk, 0.10f));
            DrawRectOutline(texture, 28, 10, 10, 28, outline);

            if (variant == 1)
            {
                DrawPineCanopy(texture, 32, 54, 28, 43, leaf, leafDark, leafLight, outline);
            }
            else if (variant == 3)
            {
                DrawTallPineCanopy(texture, 32, 56, leaf, leafDark, leafLight, outline);
            }
            else
            {
                FillEllipse(texture, 31, 49, 22, 18, leafDark);
                FillEllipse(texture, 23, 45, 15, 13, leaf);
                FillEllipse(texture, 38, 46, 18, 15, leaf);
                FillEllipse(texture, 31, 58, 17, 14, leafLight);
                FillEllipse(texture, 42, 58, 11, 10, leaf);
                FillEllipse(texture, 20, 57, 10, 9, leaf);
                DrawCanopyRim(texture, 31, 49, 22, 18, outline);
            }

            AddLeafDetails(texture, variant, leafLight, leafDark);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 4f, 56f, 72f), new Vector2(0.5f, 0.08f), TreePixelsPerUnit);
        }

        private static Sprite CreateSmallTreeSprite(int variant)
        {
            Texture2D texture = CreateTexture(48, 60, $"Small Tree {variant + 1}");
            Color outline = Rgb(35, 32, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color trunk = GetTrunkColor(variant + 2);
            Color leaf = GetLeafColor(variant + 1);
            Color leafDark = Shift(leaf, -0.18f);
            Color leafLight = Shift(leaf, 0.12f);

            FillEllipse(texture, 24, 8, 13, 4, shadow);
            FillRect(texture, 21, 9, 6, 18, Shift(trunk, -0.12f));
            FillRect(texture, 23, 9, 5, 19, trunk);
            DrawRectOutline(texture, 21, 9, 8, 19, outline);

            if (variant == 2)
            {
                DrawPineCanopy(texture, 24, 39, 18, 30, leaf, leafDark, leafLight, outline);
            }
            else
            {
                FillEllipse(texture, 23, 36, 16, 13, leafDark);
                FillEllipse(texture, 18, 34, 10, 10, leaf);
                FillEllipse(texture, 30, 35, 12, 10, leaf);
                FillEllipse(texture, 24, 43, 11, 9, leafLight);
                DrawCanopyRim(texture, 23, 36, 16, 13, outline);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 4f, 38f, 52f), new Vector2(0.5f, 0.08f), TreePixelsPerUnit);
        }

        private static Sprite CreateBushSprite(int variant)
        {
            Texture2D texture = CreateTexture(40, 32, $"Bush {variant + 1}");
            Color outline = Rgb(34, 42, 27);
            Color shadow = new Color(0f, 0f, 0f, 0.20f);
            Color leaf = GetBushColor(variant);
            Color leafDark = Shift(leaf, -0.16f);
            Color leafLight = Shift(leaf, 0.14f);

            FillEllipse(texture, 20, 7, 13, 4, shadow);
            FillEllipse(texture, 14, 15, 9, 7, leafDark);
            FillEllipse(texture, 22, 17, 12, 9, leaf);
            FillEllipse(texture, 28, 14, 8, 7, leafDark);
            FillEllipse(texture, 17, 21, 10, 6, leafLight);
            FillEllipse(texture, 27, 20, 8, 6, leaf);
            DrawCanopyRim(texture, 21, 17, 17, 10, outline);

            for (int i = 0; i < 8; i++)
            {
                int x = 9 + ((variant * 11 + i * 7) % 23);
                int y = 13 + ((variant * 5 + i * 3) % 10);
                SetPixelSafe(texture, x, y, i % 2 == 0 ? leafLight : leafDark);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 32f, 24f), new Vector2(0.5f, 0.16f), BushPixelsPerUnit);
        }

        private static Sprite CreateForestGroupSprite(int variant)
        {
            Texture2D texture = CreateTexture(84, 72, $"Forest Group {variant + 1}");
            Color outline = Rgb(29, 34, 24);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color leafA = GetLeafColor(variant);
            Color leafB = GetLeafColor(variant + 2);
            Color trunk = GetTrunkColor(variant);

            FillEllipse(texture, 42, 9, 30, 6, shadow);
            DrawMiniTree(texture, 20, 11, 0.82f, leafA, trunk, outline);
            DrawMiniTree(texture, 41, 9, 1.05f, leafB, trunk, outline);
            DrawMiniTree(texture, 61, 12, 0.90f, Shift(leafA, -0.05f), trunk, outline);
            DrawMiniTree(texture, 31, 13, 0.70f, Shift(leafB, 0.06f), trunk, outline);
            DrawMiniTree(texture, 53, 14, 0.74f, Shift(leafA, 0.08f), trunk, outline);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 4f, 74f, 64f), new Vector2(0.5f, 0.08f), TreePixelsPerUnit);
        }

        private static Sprite CreateSaplingSprite(int variant)
        {
            Texture2D texture = CreateTexture(32, 36, $"Sapling Tree {variant + 1}");
            Color outline = Rgb(35, 31, 23);
            Color shadow = new Color(0f, 0f, 0f, 0.18f);
            Color trunk = GetTrunkColor(variant);
            Color leaf = GetLeafColor(variant);
            Color leafDark = Shift(leaf, -0.14f);
            Color leafLight = Shift(leaf, 0.15f);

            FillEllipse(texture, 16, 6, 8, 3, shadow);
            FillRect(texture, 15, 7, 3, 13, trunk);
            DrawRectOutline(texture, 15, 7, 3, 13, outline);
            DrawLine(texture, P(16, 15), P(10, 21), trunk);
            DrawLine(texture, P(17, 16), P(23, 22), trunk);
            FillEllipse(texture, 10, 22, 6, 4, leafDark);
            FillEllipse(texture, 22, 23, 6, 4, leaf);
            FillEllipse(texture, 16, 28, 8, 6, leafLight);
            DrawCanopyRim(texture, 16, 26, 10, 7, outline);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 3f, 26f, 30f), new Vector2(0.5f, 0.10f), TreePixelsPerUnit);
        }

        private static Sprite CreateFelledTreeSprite(int variant)
        {
            Texture2D texture = CreateTexture(72, 38, $"Felled Tree {variant + 1}");
            Color outline = Rgb(35, 28, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.20f);
            Color trunk = GetTrunkColor(variant);
            Color trunkDark = Shift(trunk, -0.18f);
            Color trunkLight = Shift(trunk, 0.12f);
            Color cut = Rgb(211, 163, 95);
            Color rings = Rgb(119, 76, 43);

            FillEllipse(texture, 36, 8, 27, 5, shadow);
            FillRect(texture, 17, 12, 38, 8, trunkDark);
            FillRect(texture, 18, 15, 37, 8, trunk);
            DrawRectOutline(texture, 17, 12, 39, 12, outline);
            DrawLine(texture, P(20, 20), P(52, 20), trunkLight);
            DrawLine(texture, P(23, 15), P(49, 15), trunkDark);

            FillEllipse(texture, 16, 18, 6, 7, cut);
            DrawCanopyRim(texture, 16, 18, 6, 7, outline);
            FillEllipse(texture, 16, 18, 3, 4, new Color(rings.r, rings.g, rings.b, 0.78f));
            SetPixelSafe(texture, 16, 18, cut);

            FillRect(texture, 49, 10, 8, 12, trunkDark);
            FillRect(texture, 51, 12, 7, 12, trunk);
            DrawRectOutline(texture, 49, 10, 10, 15, outline);
            FillEllipse(texture, 54, 25, 7, 3, cut);
            DrawLine(texture, P(50, 25), P(58, 25), outline);

            for (int i = 0; i < 9; i++)
            {
                int x = 20 + ((variant * 13 + i * 5) % 33);
                int y = 12 + ((variant * 7 + i * 3) % 10);
                DrawLine(texture, P(x, y), P(x + 4, y + (i % 2 == 0 ? 1 : -1)), i % 3 == 0 ? trunkLight : trunkDark);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 5f, 62f, 28f), new Vector2(0.5f, 0.16f), TreePixelsPerUnit);
        }

        private static Sprite CreateSplitLogsSprite(int variant)
        {
            Texture2D texture = CreateTexture(72, 38, $"Split Logs {variant + 1}");
            Color outline = Rgb(36, 28, 22);
            Color shadow = new Color(0f, 0f, 0f, 0.20f);
            Color trunk = GetTrunkColor(variant);
            Color trunkDark = Shift(trunk, -0.18f);
            Color trunkLight = Shift(trunk, 0.14f);
            Color cut = Rgb(218, 171, 98);
            Color rings = Rgb(112, 71, 42);

            FillEllipse(texture, 36, 8, 24, 5, shadow);

            DrawLogPiece(texture, 15, 14, 22, 7, trunkDark, trunk, trunkLight, cut, rings, outline);
            DrawLogPiece(texture, 35, 18, 24, 7, trunkDark, trunk, trunkLight, cut, rings, outline);
            DrawLogPiece(texture, 22, 23, 18, 6, trunkDark, trunk, trunkLight, cut, rings, outline);
            DrawLogPiece(texture, 43, 11, 16, 6, trunkDark, trunk, trunkLight, cut, rings, outline);

            for (int i = 0; i < 12; i++)
            {
                int x = 12 + ((variant * 11 + i * 7) % 47);
                int y = 9 + ((variant * 5 + i * 3) % 20);
                SetPixelSafe(texture, x, y, i % 2 == 0 ? trunkLight : trunkDark);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 5f, 62f, 28f), new Vector2(0.5f, 0.16f), TreePixelsPerUnit);
        }

        private static Sprite CreateCarriedLogsSprite()
        {
            Texture2D texture = CreateTexture(34, 22, "Carried Logs");
            Color outline = Rgb(36, 28, 22);
            Color trunk = Rgb(126, 78, 42);
            Color trunkDark = Rgb(82, 52, 31);
            Color trunkLight = Rgb(181, 119, 58);
            Color cut = Rgb(219, 171, 98);
            Color rings = Rgb(112, 71, 42);

            DrawLogPiece(texture, 5, 8, 22, 5, trunkDark, trunk, trunkLight, cut, rings, outline);
            DrawLogPiece(texture, 7, 13, 20, 5, trunkDark, trunk, trunkLight, cut, rings, outline);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 5f, 28f, 14f), new Vector2(0.5f, 0.35f), TreePixelsPerUnit);
        }

        private static Sprite CreateCarriedStoneSprite()
        {
            Texture2D texture = CreateTexture(30, 22, "Carried Stone");
            Color outline = Rgb(44, 43, 40);
            Color stoneDark = Rgb(82, 88, 85);
            Color stone = Rgb(126, 133, 128);
            Color stoneLight = Rgb(184, 191, 181);

            FillEllipse(texture, 10, 10, 6, 4, stoneDark);
            FillEllipse(texture, 9, 11, 4, 3, stone);
            FillEllipse(texture, 19, 11, 7, 5, stoneDark);
            FillEllipse(texture, 18, 12, 5, 4, stone);
            FillEllipse(texture, 15, 15, 6, 4, stoneDark);
            FillEllipse(texture, 14, 16, 4, 3, stone);
            SetPixelSafe(texture, 7, 13, stoneLight);
            SetPixelSafe(texture, 16, 15, stoneLight);
            SetPixelSafe(texture, 20, 14, stoneLight);
            DrawCanopyRim(texture, 10, 10, 6, 4, outline);
            DrawCanopyRim(texture, 19, 11, 7, 5, outline);
            DrawCanopyRim(texture, 15, 15, 6, 4, outline);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 5f, 24f, 14f), new Vector2(0.5f, 0.35f), TreePixelsPerUnit);
        }
        private static Sprite CreateCarriedGameSprite()
        {
            Texture2D texture = CreateTexture(32, 22, "Carried Game");
            Color outline = Rgb(58, 34, 27);
            Color meatDark = Rgb(120, 44, 38);
            Color meat = Rgb(174, 72, 54);
            Color meatLight = Rgb(222, 126, 83);
            Color bone = Rgb(226, 206, 164);
            Color cord = Rgb(93, 59, 36);

            DrawLine(texture, P(6, 7), P(25, 15), cord);
            FillEllipse(texture, 11, 10, 6, 4, outline);
            FillEllipse(texture, 11, 10, 5, 3, meat);
            FillEllipse(texture, 19, 14, 7, 5, outline);
            FillEllipse(texture, 19, 14, 6, 4, meatDark);
            FillEllipse(texture, 21, 15, 3, 2, meatLight);
            FillRect(texture, 4, 7, 5, 2, bone);
            FillEllipse(texture, 4, 8, 2, 2, bone);
            FillRect(texture, 24, 14, 5, 2, bone);
            FillEllipse(texture, 29, 15, 2, 2, bone);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 5f, 26f, 14f), new Vector2(0.5f, 0.35f), TreePixelsPerUnit);
        }

        private static Sprite CreateCarriedFishSprite()
        {
            Texture2D texture = CreateTexture(34, 22, "Carried Fish");
            Color outline = Rgb(31, 59, 70);
            Color fishDark = Rgb(59, 117, 139);
            Color fish = Rgb(88, 162, 183);
            Color fishLight = Rgb(151, 215, 224);
            Color fin = Rgb(222, 151, 77);
            Color cord = Rgb(82, 62, 42);

            DrawLine(texture, P(5, 7), P(28, 14), cord);
            FillEllipse(texture, 15, 12, 9, 5, outline);
            FillEllipse(texture, 15, 12, 8, 4, fishDark);
            FillEllipse(texture, 17, 13, 5, 3, fish);
            FillEllipse(texture, 20, 14, 2, 2, fishLight);
            FillTriangle(texture, P(7, 12), P(3, 8), P(3, 16), outline);
            FillTriangle(texture, P(7, 12), P(4, 9), P(4, 15), fin);
            FillTriangle(texture, P(13, 15), P(16, 20), P(18, 15), outline);
            FillTriangle(texture, P(14, 15), P(16, 18), P(17, 15), fin);
            SetPixelSafe(texture, 23, 14, outline);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 5f, 28f, 14f), new Vector2(0.5f, 0.35f), TreePixelsPerUnit);
        }

        private static Sprite CreateBoulderSprite(int variant)
        {
            Texture2D texture = CreateTexture(44, 34, $"Boulder {variant + 1}");
            Color outline = Rgb(45, 43, 40);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color stone = GetStoneColor(variant);
            Color dark = Shift(stone, -0.18f);
            Color light = Shift(stone, 0.16f);

            FillEllipse(texture, 22, 7, 15, 4, shadow);

            Vector2Int[] back = { P(9, 14), P(19, 25), P(33, 23), P(38, 14), P(29, 8), P(15, 9) };
            Vector2Int[] face = { P(8, 13), P(13, 22), P(27, 25), P(37, 17), P(31, 9), P(16, 8) };
            FillPolygon(texture, back, dark);
            FillPolygon(texture, face, stone);
            DrawPolygon(texture, back, outline);
            DrawPolygon(texture, face, outline);

            DrawLine(texture, P(15, 21), P(22, 12), dark);
            DrawLine(texture, P(23, 24), P(30, 12), dark);
            DrawLine(texture, P(17, 11), P(29, 10), light);
            DrawLine(texture, P(12, 15), P(18, 12), light);
            AddStoneSpeckles(texture, variant, 9, 10, 27, 14, light, dark);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 5f, 36f, 25f), new Vector2(0.5f, 0.18f), StonePixelsPerUnit);
        }
    }
}
