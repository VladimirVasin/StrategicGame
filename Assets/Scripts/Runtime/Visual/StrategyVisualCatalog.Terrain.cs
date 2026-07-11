using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyVisualCatalog
    {
        [SerializeField] private TerrainSpriteSet[] terrainSprites = Array.Empty<TerrainSpriteSet>();

        public bool TryGetTerrainSprite(CityMapCellKind kind, int variant, out Sprite sprite)
        {
            for (int i = 0; i < terrainSprites.Length; i++)
            {
                TerrainSpriteSet set = terrainSprites[i];
                if (set != null && set.Kind == kind && TryGetVariant(set.Variants, variant, out sprite))
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        [Serializable]
        public sealed class TerrainSpriteSet
        {
            [SerializeField] private CityMapCellKind kind;
            [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

            public TerrainSpriteSet(CityMapCellKind kind, Sprite[] variants)
            {
                this.kind = kind;
                this.variants = variants ?? Array.Empty<Sprite>();
            }

            public CityMapCellKind Kind => kind;
            public Sprite[] Variants => variants;
        }
    }
}
