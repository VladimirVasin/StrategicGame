using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyMapDistributionUtility
    {
        public static int GetShuffledIndex(int seed, int iteration, int count, int salt)
        {
            if (count <= 1)
            {
                return 0;
            }

            int start = PositiveModulo(Hash(seed, salt, count, 17), count);
            int step = BuildCoprimeStep(count, Hash(seed, salt, count, 31));
            return (int)((start + (long)iteration * step) % count);
        }

        public static float ClusterScore(
            int seed,
            int x,
            int y,
            int salt,
            float broadScale,
            float detailScale)
        {
            float broad = Mathf.PerlinNoise(
                seed * 0.0113f + salt * 5.173f + x * broadScale,
                seed * 0.0169f + salt * 7.331f + y * broadScale);
            float detail = Mathf.PerlinNoise(
                seed * 0.0271f + salt * 3.719f + x * detailScale,
                seed * 0.0233f + salt * 4.811f + y * detailScale);
            return Mathf.Clamp01(broad * 0.72f + detail * 0.28f);
        }

        public static float ApplyClusterToRoll(float roll, float cluster, float sparseMultiplier, float denseMultiplier)
        {
            float density = Mathf.Lerp(sparseMultiplier, denseMultiplier, Mathf.SmoothStep(0f, 1f, cluster));
            return Mathf.Clamp01(roll / Mathf.Max(0.05f, density));
        }

        public static float ApplyClusterToChance(float baseChance, float cluster, float sparseMultiplier, float denseMultiplier)
        {
            float density = Mathf.Lerp(sparseMultiplier, denseMultiplier, Mathf.SmoothStep(0f, 1f, cluster));
            return Mathf.Clamp01(baseChance * density);
        }

        private static int BuildCoprimeStep(int count, int seed)
        {
            int step = PositiveModulo(seed, count - 1) + 1;
            if ((step & 1) == 0)
            {
                step++;
            }

            while (GreatestCommonDivisor(step, count) != 1)
            {
                step += 2;
                if (step >= count)
                {
                    step = 1;
                }
            }

            return step;
        }

        private static int GreatestCommonDivisor(int left, int right)
        {
            left = Mathf.Abs(left);
            right = Mathf.Abs(right);
            while (right != 0)
            {
                int remainder = left % right;
                left = right;
                right = remainder;
            }

            return Mathf.Max(1, left);
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
            {
                return 0;
            }

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static int Hash(int seed, int a, int b, int c)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & int.MaxValue;
            }
        }
    }
}
