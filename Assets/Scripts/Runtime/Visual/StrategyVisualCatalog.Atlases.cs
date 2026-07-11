using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyVisualCatalog
    {
        [SerializeField] private ResidentAtlasSet[] residentAtlases = Array.Empty<ResidentAtlasSet>();
        [SerializeField] private NatureSpriteSet[] natureSprites = Array.Empty<NatureSpriteSet>();
        [SerializeField] private VisualSequenceSet[] visualSequences = Array.Empty<VisualSequenceSet>();

        [NonSerialized] private Dictionary<int, Sprite> atlasSpriteCache;

        public bool TryGetResidentAtlasSprite(
            StrategyResidentGender gender,
            StrategyResidentLifeStage lifeStage,
            StrategyResidentVisualPose pose,
            int variant,
            int frame,
            out Sprite sprite)
        {
            for (int i = 0; i < residentAtlases.Length; i++)
            {
                ResidentAtlasSet set = residentAtlases[i];
                if (set != null
                    && set.Gender == gender
                    && set.LifeStage == lifeStage
                    && set.Variant == variant
                    && set.TryGetFrame(pose, frame, GetOrCreateAtlasSprite, out sprite))
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        public bool TryGetNatureSprite(StrategyNaturePropKind kind, int variant, out Sprite sprite)
        {
            for (int i = 0; i < natureSprites.Length; i++)
            {
                NatureSpriteSet set = natureSprites[i];
                if (set != null && set.Kind == kind && TryGetVariant(set.Variants, variant, out sprite))
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        public bool TryGetSequenceSprite(string id, int frame, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                sprite = null;
                return false;
            }

            for (int i = 0; i < visualSequences.Length; i++)
            {
                VisualSequenceSet set = visualSequences[i];
                if (set != null
                    && string.Equals(set.Id, id, StringComparison.Ordinal)
                    && set.TryGetFrame(frame, GetOrCreateAtlasSprite, out sprite))
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        public void ReplaceBakedContent(
            BuildingSpriteSet[] buildings,
            ResidentSpriteSet[] residents,
            ResidentAtlasSet[] atlases,
            NatureSpriteSet[] nature,
            VisualSequenceSet[] sequences,
            TerrainSpriteSet[] terrain)
        {
            buildingSprites = buildings ?? Array.Empty<BuildingSpriteSet>();
            residentSprites = residents ?? Array.Empty<ResidentSpriteSet>();
            residentAtlases = atlases ?? Array.Empty<ResidentAtlasSet>();
            natureSprites = nature ?? Array.Empty<NatureSpriteSet>();
            visualSequences = sequences ?? Array.Empty<VisualSequenceSet>();
            terrainSprites = terrain ?? Array.Empty<TerrainSpriteSet>();
            atlasSpriteCache?.Clear();
        }

        private Sprite GetOrCreateAtlasSprite(
            Texture2D atlas,
            RectInt rect,
            Vector2 pivot,
            float pixelsPerUnit,
            int cacheSalt)
        {
            atlasSpriteCache ??= new Dictionary<int, Sprite>();
            int key = atlas.GetEntityId().GetHashCode();
            key = key * 397 ^ rect.x;
            key = key * 397 ^ rect.y;
            key = key * 397 ^ rect.width;
            key = key * 397 ^ rect.height;
            key = key * 397 ^ cacheSalt;
            if (!atlasSpriteCache.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = Sprite.Create(
                    atlas,
                    new Rect(rect.x, rect.y, rect.width, rect.height),
                    pivot,
                    pixelsPerUnit);
                sprite.name = atlas.name + " Frame " + cacheSalt;
                atlasSpriteCache[key] = sprite;
            }

            return sprite;
        }

        public delegate Sprite AtlasSpriteResolver(
            Texture2D atlas,
            RectInt rect,
            Vector2 pivot,
            float pixelsPerUnit,
            int cacheSalt);

        [Serializable]
        public sealed class ResidentAtlasSet
        {
            [SerializeField] private StrategyResidentGender gender;
            [SerializeField] private StrategyResidentLifeStage lifeStage;
            [SerializeField] private int variant;
            [SerializeField] private Texture2D atlas;
            [SerializeField] private int frameWidth;
            [SerializeField] private int frameHeight;
            [SerializeField] private float pixelsPerUnit = 32f;
            [SerializeField] private Vector2 pivot = new(0.5f, 0.08f);
            [SerializeField] private ResidentPoseRow[] rows = Array.Empty<ResidentPoseRow>();

            public StrategyResidentGender Gender => gender;
            public StrategyResidentLifeStage LifeStage => lifeStage;
            public int Variant => variant;

            public ResidentAtlasSet(
                StrategyResidentGender gender,
                StrategyResidentLifeStage lifeStage,
                int variant,
                Texture2D atlas,
                int frameWidth,
                int frameHeight,
                float pixelsPerUnit,
                Vector2 pivot,
                ResidentPoseRow[] rows)
            {
                this.gender = gender;
                this.lifeStage = lifeStage;
                this.variant = variant;
                this.atlas = atlas;
                this.frameWidth = frameWidth;
                this.frameHeight = frameHeight;
                this.pixelsPerUnit = pixelsPerUnit;
                this.pivot = pivot;
                this.rows = rows ?? Array.Empty<ResidentPoseRow>();
            }

            public bool TryGetFrame(
                StrategyResidentVisualPose pose,
                int frame,
                AtlasSpriteResolver resolver,
                out Sprite sprite)
            {
                if (atlas == null || frameWidth <= 0 || frameHeight <= 0)
                {
                    sprite = null;
                    return false;
                }

                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i].Pose != pose || rows[i].FrameCount <= 0)
                    {
                        continue;
                    }

                    int normalized = PositiveModulo(frame, rows[i].FrameCount);
                    RectInt rect = new(normalized * frameWidth, rows[i].Row * frameHeight, frameWidth, frameHeight);
                    sprite = resolver(atlas, rect, pivot, pixelsPerUnit, rows[i].Row * 256 + normalized);
                    return sprite != null;
                }

                sprite = null;
                return false;
            }
        }

        [Serializable]
        public struct ResidentPoseRow
        {
            [SerializeField] private StrategyResidentVisualPose pose;
            [SerializeField] private int row;
            [SerializeField] private int frameCount;

            public ResidentPoseRow(StrategyResidentVisualPose pose, int row, int frameCount)
            {
                this.pose = pose;
                this.row = row;
                this.frameCount = frameCount;
            }

            public StrategyResidentVisualPose Pose => pose;
            public int Row => row;
            public int FrameCount => frameCount;
        }

        [Serializable]
        public sealed class NatureSpriteSet
        {
            [SerializeField] private StrategyNaturePropKind kind;
            [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

            public StrategyNaturePropKind Kind => kind;
            public Sprite[] Variants => variants;

            public NatureSpriteSet(StrategyNaturePropKind kind, Sprite[] variants)
            {
                this.kind = kind;
                this.variants = variants ?? Array.Empty<Sprite>();
            }
        }

        [Serializable]
        public sealed class VisualSequenceSet
        {
            [SerializeField] private string id = string.Empty;
            [SerializeField] private Texture2D atlas;
            [SerializeField] private int frameWidth;
            [SerializeField] private int frameHeight;
            [SerializeField] private int frameCount;
            [SerializeField] private float pixelsPerUnit = 24f;
            [SerializeField] private Vector2 pivot = new(0.5f, 0.1f);

            public string Id => id;

            public VisualSequenceSet(
                string id,
                Texture2D atlas,
                int frameWidth,
                int frameHeight,
                int frameCount,
                float pixelsPerUnit,
                Vector2 pivot)
            {
                this.id = id ?? string.Empty;
                this.atlas = atlas;
                this.frameWidth = frameWidth;
                this.frameHeight = frameHeight;
                this.frameCount = frameCount;
                this.pixelsPerUnit = pixelsPerUnit;
                this.pivot = pivot;
            }

            public bool TryGetFrame(int frame, AtlasSpriteResolver resolver, out Sprite sprite)
            {
                if (atlas == null || frameWidth <= 0 || frameHeight <= 0 || frameCount <= 0)
                {
                    sprite = null;
                    return false;
                }

                int normalized = PositiveModulo(frame, frameCount);
                RectInt rect = new(normalized * frameWidth, 0, frameWidth, frameHeight);
                sprite = resolver(atlas, rect, pivot, pixelsPerUnit, normalized);
                return sprite != null;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
