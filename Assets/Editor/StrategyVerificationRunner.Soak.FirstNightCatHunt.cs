using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static bool TryVerifyFirstNightCatHuntCinematic(
            StrategyFirstNightFaunaEventController firstNightEvent,
            StrategyInputRouter router)
        {
            if (firstNightEvent == null || !firstNightEvent.IsCatHuntCinematicPlaying)
            {
                return false;
            }

            StrategyTimeScaleController timeScale =
                Object.FindAnyObjectByType<StrategyTimeScaleController>();
            StrategyCityItemRewardRevealController reward =
                Object.FindAnyObjectByType<StrategyCityItemRewardRevealController>();
            Require(
                reward == null || !reward.IsOpen,
                "Cat-hunt cinematic began before the Cats reward reveal closed");
            Require(
                Object.FindObjectsByType<StrategyCatAgent>().Length >= 1,
                "Cat-hunt cinematic began without the first live settlement cat");
            Require(
                Object.FindObjectsByType<StrategyMouseAgent>().Length >= 3,
                "Cat-hunt cinematic unexpectedly consumed live settlement mice");
            Require(
                router.ActiveContextCount == 1,
                "Cat-hunt cinematic created an unexpected input-context stack");
            Require(
                router.BlockedChannels == StrategyInputChannel.All,
                "Cat-hunt cinematic did not block all input channels");
            Require(
                router.TopCancelMode == StrategyCancelMode.Swallow,
                "Cat-hunt cinematic did not swallow cancellation");
            Require(
                timeScale != null && timeScale.IsPausedByLock,
                "Cat-hunt cinematic did not hold a pause lock");
            Require(
                Mathf.Approximately(timeScale.CurrentScale, 1f),
                "Cat-hunt cinematic did not retain requested speed x1");

            StrategyCinematicCatActor[] cats =
                Object.FindObjectsByType<StrategyCinematicCatActor>();
            StrategyCinematicRatActor[] mice =
                Object.FindObjectsByType<StrategyCinematicRatActor>();
            if (firstNightEvent.HasVisibleCatHuntActors)
            {
                Require(cats.Length == 1, "Cat-hunt cinematic must stage exactly one cat");
                Require(mice.Length == 1, "Cat-hunt cinematic must stage exactly one mouse");
                Require(
                    cats[0].Renderer != null && cats[0].Renderer.enabled,
                    "Cat-hunt cinematic lost its standard cat visual");
                Require(
                    mice[0].Renderer != null && mice[0].Renderer.enabled,
                    "Cat-hunt cinematic lost its standard mouse visual");
                Require(
                    Mathf.Abs(
                        cats[0].transform.localScale.x
                        - StrategySettlementFaunaSpriteFactory.CatWorldScale)
                    <= StrategySettlementFaunaSpriteFactory.CatWorldScale * 0.15f,
                    "Cat-hunt cinematic changed the standard cat scale");
                Require(
                    Mathf.Approximately(
                        mice[0].transform.localScale.x,
                        StrategySettlementFaunaSpriteFactory.MouseWorldScale),
                    "Cat-hunt cinematic changed the standard mouse scale");
            }

            return true;
        }
    }
}
