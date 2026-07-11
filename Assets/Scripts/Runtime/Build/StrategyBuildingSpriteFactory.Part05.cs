using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        public static Sprite GetLumberjackCampStockSprite(int logsStored)
        {
            if (logsStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((logsStored + 1) / 2, 1, 5);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.LumberjackLogs, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 32768 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateLumberjackCampStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStonecutterCampStockSprite(int stoneStored)
        {
            if (stoneStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stoneStored + 2) / 3, 1, 5);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.StonecutterStone, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 36864 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStonecutterCampStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetHunterCampStockSprite(int gameStored)
        {
            if (gameStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((gameStored + 1) / 2, 1, 5);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.HunterGame, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 38912 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateHunterCampStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetFisherHutStockSprite(int fishStored)
        {
            if (fishStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((fishStored + 1) / 2, 1, 5);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.FisherFish, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 43008 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateFisherHutStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStorageYardStockSprite(int logsStored)
        {
            if (logsStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((logsStored + 2) / 3, 1, 6);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.StorageLogs, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 40960 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStorageYardStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStorageYardStoneStockSprite(int stoneStored)
        {
            if (stoneStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((stoneStored + 3) / 4, 1, 6);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.StorageStone, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 45056 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateStorageYardStoneStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetGranaryGameStockSprite(int gameStored)
        {
            if (gameStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((gameStored + 1) / 2, 1, 6);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.GranaryGame, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 49152 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateGranaryGameStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetGranaryFishStockSprite(int fishStored)
        {
            if (fishStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((fishStored + 1) / 2, 1, 6);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.GranaryFish, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 53248 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateGranaryFishStockSprite(level);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetMineIronStockSprite(int ironStored)
        {
            if (ironStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((ironStored + 2) / 3, 1, 5);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.MineIron, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 59392 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateIronStockSprite(level, "Mine Iron Stock");
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStorageYardIronStockSprite(int ironStored)
        {
            if (ironStored <= 0)
            {
                return null;
            }

            int level = Mathf.Clamp((ironStored + 3) / 4, 1, 6);
            if (TryGetBakedLayer(StrategyVisualSequenceIds.StorageIron, level - 1, out Sprite baked))
            {
                return baked;
            }

            int cacheKey = 61440 + level;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateIronStockSprite(level, "Storage Iron Stock");
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }
    }
}
