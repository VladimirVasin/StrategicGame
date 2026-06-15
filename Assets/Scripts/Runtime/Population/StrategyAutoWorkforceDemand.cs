using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyAutoWorkforceDemand
    {
        public StrategyAutoWorkforceDemand(
            StrategyProfessionType profession,
            StrategyAutoWorkforceCategory category,
            Component target,
            Vector3 world,
            int needed,
            float score,
            string reason)
        {
            Profession = profession;
            Category = category;
            Target = target;
            World = world;
            Needed = Mathf.Max(0, needed);
            Score = score;
            Reason = reason;
        }

        public StrategyProfessionType Profession { get; }
        public StrategyAutoWorkforceCategory Category { get; }
        public Component Target { get; }
        public Vector3 World { get; }
        public int Needed { get; set; }
        public float Score { get; }
        public string Reason { get; }
    }
}
