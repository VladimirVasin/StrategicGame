using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {

        private void ClearProps()
        {
            if (propRoot == null)
            {
                return;
            }

            for (int i = propRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = propRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static float Hash01(int seed, int x, int y, int salt)
        {
            return Hash(seed, x, y, salt, 0) / (float)int.MaxValue;
        }

        private static int Hash(int seed, int a, int b, int c, int d)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h = h * 1597334677 + d * 381201580;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & int.MaxValue;
            }
        }
    }
}
