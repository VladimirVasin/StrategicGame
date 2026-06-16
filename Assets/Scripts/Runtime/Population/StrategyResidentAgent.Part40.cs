using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private float coalPitWorkEffectTimer;
        private float mineWorkEffectTimer;

        private void ResetCoalPitWorkEffectTimer(bool immediate)
        {
            coalPitWorkEffectTimer = immediate ? 0.05f : Random.Range(0.42f, 0.82f);
        }

        private void UpdateCoalPitWorkEffects()
        {
            if (activeCoalPit == null)
            {
                return;
            }

            coalPitWorkEffectTimer -= Time.deltaTime;
            if (coalPitWorkEffectTimer > 0f)
            {
                return;
            }

            activeCoalPit.PlayMiningWorkEffect(ResidentId + Mathf.RoundToInt(Time.time * 10f));
            ResetCoalPitWorkEffectTimer(false);
        }

        private void ResetMineWorkEffectTimer(bool immediate)
        {
            mineWorkEffectTimer = immediate ? 0.15f : Random.Range(0.90f, 1.55f);
        }

        private void UpdateMineUndergroundEffects()
        {
            if (activeMine == null)
            {
                return;
            }

            mineWorkEffectTimer -= Time.deltaTime;
            if (mineWorkEffectTimer > 0f)
            {
                return;
            }

            activeMine.PlayUndergroundWorkEffect(ResidentId + Mathf.RoundToInt(Time.time * 7f));
            ResetMineWorkEffectTimer(false);
        }

        private void PlayWorksiteResourceDepositEffect(StrategyResourceType resource, Bounds bounds, int amount)
        {
            if (resource == StrategyResourceType.None || amount <= 0)
            {
                return;
            }

            Vector3 world = GetWorksiteResourceEffectWorld(resource, bounds);
            StrategyWorldEffectAnimator.SpawnResourcePlaced(
                resource,
                world,
                StrategyWorldSorting.ForPosition(world, 4),
                amount,
                ResidentId + amount * 13);
        }

        private void PlayFishCaughtEffect(Vector3 fishWorld)
        {
            Vector3 world = new Vector3(fishWorld.x, fishWorld.y, -0.12f);
            StrategyWorldEffectAnimator.Spawn(
                StrategyWorldEffectKind.WaterSplash,
                world,
                StrategyWorldSorting.ForPosition(world, 5),
                ResidentId + Mathf.RoundToInt(Time.time * 9f),
                0.82f);
        }

        private static Vector3 GetWorksiteResourceEffectWorld(StrategyResourceType resource, Bounds bounds)
        {
            float xOffset = resource switch
            {
                StrategyResourceType.Logs => -0.18f,
                StrategyResourceType.Stone => 0.22f,
                StrategyResourceType.Fish => 0.18f,
                StrategyResourceType.Game => -0.12f,
                _ => 0f
            };
            return new Vector3(bounds.center.x + xOffset, bounds.min.y + 0.42f, -0.15f);
        }
    }
}
