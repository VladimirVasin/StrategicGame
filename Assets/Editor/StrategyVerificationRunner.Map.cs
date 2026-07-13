using System;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        internal static void VerifyExplicitMapSeed()
        {
            const int expectedSeed = 314159;
            GameObject mapObject = new("Explicit Seed Verification Map");
            try
            {
                CityMapController map = mapObject.AddComponent<CityMapController>();
                SerializedObject serializedMap = new(map);
                serializedMap.FindProperty("width").intValue = 8;
                serializedMap.FindProperty("height").intValue = 8;
                serializedMap.FindProperty("tilePixels").intValue = 8;
                serializedMap.FindProperty("randomizeSeedOnGenerate").boolValue = true;
                serializedMap.ApplyModifiedPropertiesWithoutUndo();

                map.GenerateMap(expectedSeed);
                Require(map.ActiveSeed == expectedSeed, "Explicit map seed was replaced during generation");
                ulong firstHash = HashMapCells(map);

                map.GenerateMap(expectedSeed);
                Require(map.ActiveSeed == expectedSeed, "Explicit map seed changed during regeneration");
                Require(HashMapCells(map) == firstHash, "Identical explicit seeds produced different map cells");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapObject);
            }
        }

        private static ulong HashMapCells(CityMapController map)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Require(map.TryGetCell(x, y, out CityMapCell cell), "Generated map cell is missing");
                    hash = (hash ^ (uint)cell.Kind) * prime;
                    hash = (hash ^ (uint)cell.WaterKind) * prime;
                    hash = (hash ^ (uint)BitConverter.SingleToInt32Bits(cell.ReliefHeight)) * prime;
                }
            }

            return hash;
        }
    }
}
