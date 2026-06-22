using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {
        public string HuntTargetKind => "Rabbit";
        public Vector3 HuntWorldPosition => transform.position;
    }
}
