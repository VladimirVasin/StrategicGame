using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyMapPreloadCoordinator
    {
        private IEnumerator PrewarmContent()
        {
            yield return null;
            StrategyVisualCatalogProvider.Prewarm();
            contentStageKey = "menu.preload.preparing_buildings";
            StrategyBuildTool[] starterBuildings =
            {
                StrategyBuildTool.House,
                StrategyBuildTool.ForagerCamp,
                StrategyBuildTool.LumberjackCamp,
                StrategyBuildTool.StonecutterCamp,
                StrategyBuildTool.StarterCaravanCart
            };
            for (int i = 0; i < starterBuildings.Length; i++)
            {
                StrategyBuildingSpriteFactory.TryGetBuildSprite(starterBuildings[i], out _);
                contentProgress = 0.25f * (i + 1f) / starterBuildings.Length;
                yield return null;
            }

            contentStageKey = "menu.preload.preparing_residents";
            for (int gender = 0; gender < 2; gender++)
            {
                for (int variant = 0; variant < StrategyResidentSpriteFactory.VariantCountPerGender; variant++)
                {
                    StrategyResidentGender residentGender = (StrategyResidentGender)gender;
                    StrategyResidentSpriteFactory.GetSprite(residentGender, variant);
                    StrategyResidentSpriteFactory.GetWalkSprite(residentGender, variant, 0);
                    StrategyResidentSpriteFactory.GetPortraitSprite(residentGender, variant);
                    int index = gender * StrategyResidentSpriteFactory.VariantCountPerGender + variant + 1;
                    contentProgress = Mathf.Lerp(0.25f, 0.60f, index / 10f);
                    yield return null;
                }
            }

            contentStageKey = "menu.preload.preparing_landscape";
            StrategyNatureSpriteFactory.GetSprite(StrategyNaturePropKind.LargeTree, 0);
            yield return null;
            StrategyNatureSpriteFactory.GetSprite(StrategyNaturePropKind.SmallTree, 0);
            yield return null;
            StrategyNatureSpriteFactory.GetSprite(StrategyNaturePropKind.Bush, 0);
            yield return null;
            StrategyCampfireSpriteFactory.GetFrame(0);
            contentProgress = 0.75f;
            yield return null;

            contentStageKey = "menu.preload.preparing_interface_audio";
            Resources.LoadAll<AudioClip>("Audio/HudSfx");
            contentProgress = 0.88f;
            yield return null;
            Resources.LoadAll<AudioClip>("Audio/Footsteps");
            contentProgress = 1f;
            contentStageKey = "menu.preload.content_ready";
        }
    }
}
