using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        public bool FollowRefugeePath(IReadOnlyList<Vector3> worldPath, bool leaving)
        {
            path.Clear();
            if (worldPath != null)
            {
                for (int i = 0; i < worldPath.Count; i++)
                {
                    Vector3 point = worldPath[i];
                    path.Add(new Vector3(point.x, point.y, -0.08f));
                }
            }

            pathIndex = 0;
            hasTarget = path.Count > 0;
            activity = leaving ? ResidentActivity.LeavingSettlement : ResidentActivity.ArrivingAsRefugee;
            waitTimer = 0f;
            usingWorkSprite = false;
            appliedWorkFrame = -1;
            if (!hasTarget)
            {
                activity = ResidentActivity.Idle;
                waitTimer = Random.Range(0.35f, 0.85f);
                UseIdleSprite();
            }

            return hasTarget;
        }
    }
}
