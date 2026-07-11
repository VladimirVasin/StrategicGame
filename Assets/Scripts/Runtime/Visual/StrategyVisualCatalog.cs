using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [CreateAssetMenu(fileName = "StrategyVisualCatalog", menuName = "ProjectUnknown/Visual Catalog")]
    public sealed partial class StrategyVisualCatalog : ScriptableObject
    {
        [SerializeField] private BuildingSpriteSet[] buildingSprites = Array.Empty<BuildingSpriteSet>();
        [SerializeField] private ResidentSpriteSet[] residentSprites = Array.Empty<ResidentSpriteSet>();
        [SerializeField] private BuildingGroundSpriteSet[] buildingGroundSprites = Array.Empty<BuildingGroundSpriteSet>();

        public bool TryGetBuildingSprite(StrategyBuildTool tool, int variant, out Sprite sprite)
        {
            for (int i = 0; i < buildingSprites.Length; i++)
            {
                BuildingSpriteSet set = buildingSprites[i];
                if (set != null && set.Tool == tool && TryGetVariant(set.Variants, variant, out sprite))
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        public bool TryGetResidentSprite(
            StrategyResidentGender gender,
            StrategyResidentLifeStage lifeStage,
            StrategyResidentVisualPose pose,
            int variant,
            int frame,
            out Sprite sprite)
        {
            for (int i = 0; i < residentSprites.Length; i++)
            {
                ResidentSpriteSet set = residentSprites[i];
                if (set == null
                    || set.Gender != gender
                    || set.LifeStage != lifeStage
                    || set.Pose != pose
                    || set.Variant != variant)
                {
                    continue;
                }

                return TryGetVariant(set.Frames, frame, out sprite);
            }

            return TryGetResidentAtlasSprite(gender, lifeStage, pose, variant, frame, out sprite);
        }

        public bool TryGetBuildingGroundSprite(
            StrategyBuildTool tool,
            Vector2Int footprint,
            int variant,
            out Sprite sprite)
        {
            BuildingGroundSpriteSet footprintFallback = null;
            for (int i = 0; i < buildingGroundSprites.Length; i++)
            {
                BuildingGroundSpriteSet set = buildingGroundSprites[i];
                if (set == null || set.Tool != tool)
                {
                    continue;
                }

                if (set.Footprint == footprint && TryGetVariant(set.Variants, variant, out sprite))
                {
                    return true;
                }

                if (set.Footprint == Vector2Int.zero)
                {
                    footprintFallback = set;
                }
            }

            return footprintFallback != null
                ? TryGetVariant(footprintFallback.Variants, variant, out sprite)
                : ReturnMissing(out sprite);
        }

        private static bool TryGetVariant(Sprite[] sprites, int index, out Sprite sprite)
        {
            if (sprites == null || sprites.Length == 0)
            {
                sprite = null;
                return false;
            }

            int normalized = index % sprites.Length;
            if (normalized < 0)
            {
                normalized += sprites.Length;
            }

            sprite = sprites[normalized];
            return sprite != null;
        }

        private static bool ReturnMissing(out Sprite sprite)
        {
            sprite = null;
            return false;
        }

        [Serializable]
        public sealed class BuildingSpriteSet
        {
            [SerializeField] private StrategyBuildTool tool = StrategyBuildTool.House;
            [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

            public StrategyBuildTool Tool => tool;
            public Sprite[] Variants => variants;

            public BuildingSpriteSet(StrategyBuildTool tool, Sprite[] variants)
            {
                this.tool = tool;
                this.variants = variants ?? Array.Empty<Sprite>();
            }
        }

        [Serializable]
        public sealed class ResidentSpriteSet
        {
            [SerializeField] private StrategyResidentGender gender = StrategyResidentGender.Male;
            [SerializeField] private StrategyResidentLifeStage lifeStage = StrategyResidentLifeStage.Adult;
            [SerializeField] private StrategyResidentVisualPose pose = StrategyResidentVisualPose.Idle;
            [SerializeField, Min(0)] private int variant = 0;
            [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();

            public StrategyResidentGender Gender => gender;
            public StrategyResidentLifeStage LifeStage => lifeStage;
            public StrategyResidentVisualPose Pose => pose;
            public int Variant => variant;
            public Sprite[] Frames => frames;

            public ResidentSpriteSet(
                StrategyResidentGender gender,
                StrategyResidentLifeStage lifeStage,
                StrategyResidentVisualPose pose,
                int variant,
                Sprite[] frames)
            {
                this.gender = gender;
                this.lifeStage = lifeStage;
                this.pose = pose;
                this.variant = Mathf.Max(0, variant);
                this.frames = frames ?? Array.Empty<Sprite>();
            }
        }

        [Serializable]
        public sealed class BuildingGroundSpriteSet
        {
            [SerializeField] private StrategyBuildTool tool = StrategyBuildTool.House;
            [SerializeField] private Vector2Int footprint = Vector2Int.zero;
            [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

            public StrategyBuildTool Tool => tool;
            public Vector2Int Footprint => footprint;
            public Sprite[] Variants => variants;

            public BuildingGroundSpriteSet(
                StrategyBuildTool tool,
                Vector2Int footprint,
                Sprite[] variants)
            {
                this.tool = tool;
                this.footprint = footprint;
                this.variants = variants ?? Array.Empty<Sprite>();
            }
        }
    }
}
