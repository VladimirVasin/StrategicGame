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

    internal static class StrategyNatureSpriteFactory
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

        private static Sprite CreateRockClusterSprite(int variant)
        {
            Texture2D texture = CreateTexture(78, 48, $"Rock Cluster {variant + 1}");
            Color outline = Rgb(44, 42, 40);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color stoneA = GetStoneColor(variant);
            Color stoneB = GetStoneColor(variant + 2);
            Color stoneC = GetStoneColor(variant + 4);

            FillEllipse(texture, 39, 9, 29, 5, shadow);
            DrawClusterRock(texture, 17, 11, 15, 18, stoneA, outline, variant);
            DrawClusterRock(texture, 34, 10, 19, 23, stoneB, outline, variant + 3);
            DrawClusterRock(texture, 55, 12, 16, 17, stoneC, outline, variant + 6);
            DrawClusterRock(texture, 28, 7, 11, 13, Shift(stoneA, 0.06f), outline, variant + 9);
            DrawClusterRock(texture, 48, 8, 12, 14, Shift(stoneB, -0.07f), outline, variant + 12);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(5f, 5f, 68f, 37f), new Vector2(0.5f, 0.14f), StonePixelsPerUnit);
        }

        private static Sprite CreateCliffSprite(int variant)
        {
            Texture2D texture = CreateTexture(112, 82, $"Stone Cliff {variant + 1}");
            Color outline = Rgb(42, 40, 38);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color stone = GetStoneColor(variant + 1);
            Color dark = Shift(stone, -0.22f);
            Color midDark = Shift(stone, -0.10f);
            Color light = Shift(stone, 0.18f);
            Color moss = Rgb(76, 106, 63);

            FillEllipse(texture, 56, 10, 42, 7, shadow);

            Vector2Int[] silhouette =
            {
                P(11, 14), P(18, 42), P(31, 67), P(55, 75), P(83, 68),
                P(101, 39), P(96, 18), P(73, 10), P(47, 12), P(27, 8)
            };
            FillPolygon(texture, silhouette, dark);
            DrawPolygon(texture, silhouette, outline);

            Vector2Int[] leftFace = { P(13, 15), P(19, 40), P(31, 63), P(49, 72), P(48, 28), P(29, 9) };
            Vector2Int[] centerFace = { P(48, 28), P(50, 72), P(78, 65), P(92, 35), P(73, 11) };
            Vector2Int[] rightFace = { P(73, 11), P(92, 35), P(98, 19), P(91, 15), P(82, 12) };
            FillPolygon(texture, leftFace, midDark);
            FillPolygon(texture, centerFace, stone);
            FillPolygon(texture, rightFace, light);
            DrawPolygon(texture, leftFace, outline);
            DrawPolygon(texture, centerFace, outline);
            DrawPolygon(texture, rightFace, outline);

            DrawLine(texture, P(29, 15), P(39, 57), Shift(midDark, -0.12f));
            DrawLine(texture, P(57, 21), P(60, 69), Shift(stone, -0.18f));
            DrawLine(texture, P(80, 19), P(70, 61), Shift(stone, 0.10f));
            DrawLine(texture, P(21, 40), P(43, 45), light);
            DrawLine(texture, P(56, 51), P(82, 44), light);
            DrawLine(texture, P(32, 66), P(77, 65), Shift(dark, -0.08f));

            FillRect(texture, 30, 63, 8, 3, moss);
            FillRect(texture, 66, 64, 12, 3, moss);
            FillRect(texture, 82, 29, 7, 2, Shift(moss, 0.08f));
            AddStoneSpeckles(texture, variant, 18, 18, 78, 49, light, dark);

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(6f, 5f, 100f, 72f), new Vector2(0.5f, 0.10f), StonePixelsPerUnit);
        }

        private static void DrawClusterRock(Texture2D texture, int centerX, int baseY, int radiusX, int height, Color stone, Color outline, int variant)
        {
            Color dark = Shift(stone, -0.18f);
            Color light = Shift(stone, 0.16f);
            Vector2Int[] points =
            {
                P(centerX - radiusX, baseY + 2),
                P(centerX - radiusX / 2, baseY + height - 2),
                P(centerX + variant % 3 - 1, baseY + height + 4),
                P(centerX + radiusX / 2, baseY + height),
                P(centerX + radiusX, baseY + 3),
                P(centerX + radiusX / 3, baseY)
            };
            FillPolygon(texture, points, stone);
            DrawPolygon(texture, points, outline);
            DrawLine(texture, P(centerX - radiusX / 2, baseY + 4), P(centerX, baseY + height + 1), dark);
            DrawLine(texture, P(centerX + radiusX / 3, baseY + 5), P(centerX, baseY + height + 1), light);
            AddStoneSpeckles(texture, variant, centerX - radiusX + 2, baseY + 4, radiusX * 2 - 4, height - 1, light, dark);
        }

        private static void AddStoneSpeckles(
            Texture2D texture,
            int variant,
            int startX,
            int startY,
            int width,
            int height,
            Color light,
            Color dark)
        {
            int count = Mathf.Max(6, (width * height) / 70);
            for (int i = 0; i < count; i++)
            {
                int x = startX + ((variant * 17 + i * 11) % Mathf.Max(1, width));
                int y = startY + ((variant * 13 + i * 7) % Mathf.Max(1, height));
                SetPixelSafe(texture, x, y, i % 3 == 0 ? light : dark);
                if (i % 5 == 0)
                {
                    SetPixelSafe(texture, x + 1, y, i % 2 == 0 ? light : dark);
                }
            }
        }

        private static void DrawLogPiece(
            Texture2D texture,
            int x,
            int y,
            int width,
            int height,
            Color dark,
            Color mid,
            Color light,
            Color cut,
            Color rings,
            Color outline)
        {
            FillRect(texture, x + 2, y, width - 3, height, dark);
            FillRect(texture, x + 3, y + 1, width - 4, Mathf.Max(1, height - 2), mid);
            DrawRectOutline(texture, x + 2, y, width - 2, height, outline);
            DrawLine(texture, P(x + 5, y + height - 2), P(x + width - 4, y + height - 2), light);
            FillEllipse(texture, x + 2, y + height / 2, 3, Mathf.Max(2, height / 2), cut);
            DrawCanopyRim(texture, x + 2, y + height / 2, 3, Mathf.Max(2, height / 2), outline);
            FillEllipse(texture, x + 2, y + height / 2, 1, 1, rings);
        }

        private static void DrawMiniTree(Texture2D texture, int baseX, int baseY, float scale, Color leaf, Color trunk, Color outline)
        {
            int trunkWidth = Mathf.Max(3, Mathf.RoundToInt(5f * scale));
            int trunkHeight = Mathf.Max(12, Mathf.RoundToInt(20f * scale));
            FillRect(texture, baseX - trunkWidth / 2, baseY, trunkWidth, trunkHeight, Shift(trunk, -0.08f));
            FillRect(texture, baseX - trunkWidth / 2 + 1, baseY, trunkWidth, trunkHeight, trunk);

            int rx = Mathf.RoundToInt(13f * scale);
            int ry = Mathf.RoundToInt(12f * scale);
            int cy = baseY + trunkHeight + Mathf.RoundToInt(9f * scale);
            FillEllipse(texture, baseX, cy, rx, ry, Shift(leaf, -0.16f));
            FillEllipse(texture, baseX - Mathf.RoundToInt(5f * scale), cy + Mathf.RoundToInt(2f * scale), Mathf.RoundToInt(8f * scale), Mathf.RoundToInt(7f * scale), leaf);
            FillEllipse(texture, baseX + Mathf.RoundToInt(6f * scale), cy + Mathf.RoundToInt(3f * scale), Mathf.RoundToInt(9f * scale), Mathf.RoundToInt(7f * scale), Shift(leaf, 0.10f));
            DrawCanopyRim(texture, baseX, cy, rx, ry, outline);
        }

        private static void DrawPineCanopy(
            Texture2D texture,
            int centerX,
            int topY,
            int halfWidth,
            int height,
            Color leaf,
            Color leafDark,
            Color leafLight,
            Color outline)
        {
            Vector2Int[] bottom = { P(centerX - halfWidth, topY - height + 9), P(centerX, topY - 4), P(centerX + halfWidth, topY - height + 9) };
            Vector2Int[] middle = { P(centerX - halfWidth + 6, topY - height + 22), P(centerX, topY + 4), P(centerX + halfWidth - 5, topY - height + 22) };
            Vector2Int[] top = { P(centerX - halfWidth + 12, topY - height + 34), P(centerX, topY + 11), P(centerX + halfWidth - 12, topY - height + 34) };
            FillPolygon(texture, bottom, leafDark);
            FillPolygon(texture, middle, leaf);
            FillPolygon(texture, top, leafLight);
            DrawPolygon(texture, bottom, outline);
            DrawPolygon(texture, middle, outline);
            DrawPolygon(texture, top, outline);
        }

        private static void DrawTallPineCanopy(Texture2D texture, int centerX, int topY, Color leaf, Color leafDark, Color leafLight, Color outline)
        {
            DrawPineCanopy(texture, centerX, topY, 23, 49, leaf, leafDark, leafLight, outline);
            FillRect(texture, centerX - 2, 28, 5, 9, leafDark);
        }

        private static void AddLeafDetails(Texture2D texture, int variant, Color leafLight, Color leafDark)
        {
            for (int i = 0; i < 18; i++)
            {
                int x = 14 + ((variant * 17 + i * 9) % 36);
                int y = 39 + ((variant * 13 + i * 5) % 26);
                SetPixelSafe(texture, x, y, i % 3 == 0 ? leafLight : leafDark);
            }
        }

        private static void DrawCanopyRim(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int i = 0; i < 24; i++)
            {
                float angle = i * (Mathf.PI * 2f / 24f);
                int x = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * radiusX);
                int y = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * radiusY);
                SetPixelSafe(texture, x, y, color);
            }
        }

        private static Color GetLeafColor(int variant)
        {
            return NormalizeVariant(variant, 5) switch
            {
                1 => Rgb(48, 93, 49),
                2 => Rgb(102, 124, 55),
                3 => Rgb(41, 78, 55),
                4 => Rgb(132, 101, 49),
                _ => Rgb(60, 118, 58)
            };
        }

        private static Color GetBushColor(int variant)
        {
            return NormalizeVariant(variant, 4) switch
            {
                1 => Rgb(73, 128, 61),
                2 => Rgb(55, 104, 68),
                3 => Rgb(104, 135, 64),
                _ => Rgb(67, 116, 55)
            };
        }

        private static Color GetTrunkColor(int variant)
        {
            return NormalizeVariant(variant, 5) switch
            {
                1 => Rgb(86, 57, 37),
                2 => Rgb(112, 78, 44),
                3 => Rgb(68, 52, 40),
                4 => Rgb(96, 65, 43),
                _ => Rgb(101, 69, 42)
            };
        }

        private static Color GetStoneColor(int variant)
        {
            return NormalizeVariant(variant, 6) switch
            {
                1 => Rgb(116, 121, 111),
                2 => Rgb(101, 105, 108),
                3 => Rgb(129, 119, 101),
                4 => Rgb(93, 100, 94),
                5 => Rgb(137, 137, 126),
                _ => Rgb(111, 113, 105)
            };
        }

        private static Texture2D CreateTexture(int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);
            return texture;
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixelSafe(texture, px, py, color);
                }
            }
        }

        private static void DrawRectOutline(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            FillRect(texture, x, y, width, 1, color);
            FillRect(texture, x, y + height - 1, width, 1, color);
            FillRect(texture, x, y, 1, height, color);
            FillRect(texture, x + width - 1, y, 1, height, color);
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            int radiusXSqr = radiusX * radiusX;
            int radiusYSqr = radiusY * radiusY;
            int radiusProduct = radiusXSqr * radiusYSqr;

            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    if (x * x * radiusYSqr + y * y * radiusXSqr <= radiusProduct)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void FillPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            int minY = points[0].y;
            int maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            List<int> nodes = new();
            for (int y = minY; y <= maxY; y++)
            {
                nodes.Clear();
                float scanY = y + 0.5f;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2Int a = points[i];
                    Vector2Int b = points[j];
                    bool crosses = (a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY);
                    if (!crosses)
                    {
                        continue;
                    }

                    float t = (scanY - a.y) / (b.y - a.y);
                    nodes.Add(Mathf.RoundToInt(a.x + t * (b.x - a.x)));
                }

                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    for (int x = nodes[i]; x <= nodes[i + 1]; x++)
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            for (int i = 0; i < points.Length; i++)
            {
                DrawLine(texture, points[i], points[(i + 1) % points.Length], color);
            }
        }

        private static void FillTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(b, c, P(x, y));
                    float w1 = Edge(c, a, P(x, y));
                    float w2 = Edge(a, b, P(x, y));
                    if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                    {
                        SetPixelSafe(texture, x, y, color);
                    }
                }
            }
        }

        private static float Edge(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
        }

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
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
                SetPixelSafe(texture, x, y, color);
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

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Color Shift(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
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
    }
}
