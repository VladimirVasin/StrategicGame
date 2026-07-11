using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static partial class StrategyVisualCatalogBaker
    {
        private static List<StrategyVisualCatalog.TerrainSpriteSet> BakeTerrain()
        {
            List<StrategyVisualCatalog.TerrainSpriteSet> result = new();
            foreach (CityMapCellKind kind in Enum.GetValues(typeof(CityMapCellKind)))
            {
                Sprite[] variants = new Sprite[6];
                for (int variant = 0; variant < variants.Length; variant++)
                {
                    Sprite source = StrategyVisualBakeSource.GetTerrainSprite(kind, variant);
                    string path = $"{BakedRoot}/Terrain/{kind}/V{variant + 1:00}.png";
                    variants[variant] = BakeSpriteAsset(source, path, true);
                }

                result.Add(new StrategyVisualCatalog.TerrainSpriteSet(kind, variants));
            }

            return result;
        }
    }
}
