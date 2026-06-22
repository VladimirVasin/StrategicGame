using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float ForageAnimationFrameRate = 10.0f;
        private const int ForagePrimaryImpactFrame = 4;
        private const int ForageSecondaryImpactFrame = 9;

        private void AnimateForageWork(StrategyResourceType resource, bool loosePickup)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;

            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * ForageAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            if (frameSteps > 0)
            {
                for (int i = 0; i < frameSteps; i++)
                {
                    workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.ForageFrameCount;
                    if (workFrame == ForagePrimaryImpactFrame || (!loosePickup && workFrame == ForageSecondaryImpactFrame))
                    {
                        PlayForageWorkImpact(resource, loosePickup);
                    }
                }

                workFrameTimer -= frameSteps;
            }

            ApplyForageFrame(resource, workFrame);
        }

        private void ApplyForageFrame(StrategyResourceType resource, int frame)
        {
            Sprite forageSprite = StrategyResidentSpriteFactory.GetForageSprite(
                gender,
                VisualVariant,
                lifeStage,
                NormalizeForageResource(resource),
                frame);

            if (spriteRenderer.sprite == forageSprite && appliedWorkFrame == frame)
            {
                return;
            }

            spriteRenderer.sprite = forageSprite;
            appliedWorkFrame = frame;
            usingWorkSprite = true;
            SyncReadabilityRenderers();
        }

        private void PlayForageWorkImpact(StrategyResourceType resource, bool loosePickup)
        {
            int seed = ResidentId + Mathf.RoundToInt(Time.time * 10f) + workFrame * 17;
            if (!loosePickup && activeForageNode != null)
            {
                activeForageNode.PlayGatherImpact(this, seed);
                return;
            }

            Bounds bounds = activeLooseForageSource != null
                ? activeLooseForageSource.FootprintBounds
                : home != null
                    ? home.FootprintBounds
                    : new Bounds(transform.position, Vector3.one);
            Vector3 world = bounds.center + new Vector3(0f, 0.08f, -0.02f);
            StrategyWorldEffectAnimator.Spawn(
                GetForageEffectKind(resource),
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                seed,
                0.52f);
        }

        private static StrategyWorldEffectKind GetForageEffectKind(StrategyResourceType resource)
        {
            return resource switch
            {
                StrategyResourceType.Berries => StrategyWorldEffectKind.Leaves,
                StrategyResourceType.Mushrooms => StrategyWorldEffectKind.Spores,
                StrategyResourceType.Roots => StrategyWorldEffectKind.Dust,
                _ => StrategyWorldEffectKind.Leaves
            };
        }

        private static StrategyResourceType NormalizeForageResource(StrategyResourceType resource)
        {
            return resource == StrategyResourceType.Mushrooms
                || resource == StrategyResourceType.Roots
                || resource == StrategyResourceType.Berries
                    ? resource
                    : StrategyResourceType.Berries;
        }
    }
}
