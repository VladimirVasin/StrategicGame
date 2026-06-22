namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private void UpdateWorkCooldowns(float deltaTime)
        {
            TickCooldown(ref gardenWorkCooldown, deltaTime);
            TickCooldown(ref lumberWorkCooldown, deltaTime);
            TickCooldown(ref stoneWorkCooldown, deltaTime);
            TickCooldown(ref mineWorkCooldown, deltaTime);
            TickCooldown(ref coalWorkCooldown, deltaTime);
            TickCooldown(ref clayWorkCooldown, deltaTime);
            TickCooldown(ref sawmillWorkCooldown, deltaTime);
            TickCooldown(ref kilnWorkCooldown, deltaTime);
            TickCooldown(ref forgeWorkCooldown, deltaTime);
            TickCooldown(ref logisticsWorkCooldown, deltaTime);
            TickCooldown(ref huntingWorkCooldown, deltaTime);
            TickCooldown(ref fishingWorkCooldown, deltaTime);
            TickCooldown(ref householdFoodWorkCooldown, deltaTime);
        }

        private static void TickCooldown(ref float cooldown, float deltaTime)
        {
            if (cooldown > 0f)
            {
                cooldown -= deltaTime;
            }
        }
    }
}
