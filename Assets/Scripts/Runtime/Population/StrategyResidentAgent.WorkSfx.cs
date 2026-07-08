namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void EnsureWorkSfxAudio()
        {
            if (workSfxAudio != null)
            {
                return;
            }

            workSfxAudio = GetComponent<StrategyResidentWorkSfxAudio>();
            if (workSfxAudio == null)
            {
                workSfxAudio = gameObject.AddComponent<StrategyResidentWorkSfxAudio>();
            }

            workSfxAudio.Configure(this);
        }

        private void PlayAxeHitSfx()
        {
            workSfxAudio?.PlayAxeHit();
        }

        private void PlayHammerHitSfx()
        {
            workSfxAudio?.PlayHammerHit();
        }

        private void PlayPickaxeHitSfx()
        {
            workSfxAudio?.PlayPickaxeHit();
        }

        private void PlayFishingCastSfx()
        {
            workSfxAudio?.PlayFishingCast();
        }

        private void PlayFishingCatchSfx()
        {
            workSfxAudio?.PlayFishingCatch();
        }

        private void PlayBowShotSfx()
        {
            workSfxAudio?.PlayBowShot();
        }
    }
}
