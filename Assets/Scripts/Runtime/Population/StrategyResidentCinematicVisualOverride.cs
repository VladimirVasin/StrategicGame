using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyResidentCinematicVisualOverride : IDisposable
    {
        private StrategyResidentAgent owner;

        internal StrategyResidentCinematicVisualOverride(StrategyResidentAgent owner)
        {
            this.owner = owner;
        }

        public bool IsActive => owner != null && owner.OwnsCinematicVisualOverride(this);

        public bool ApplyPose(StrategyResidentVisualPose pose, int frame, bool flipX)
        {
            return owner != null
                && owner.TryApplyCinematicVisualPose(this, pose, frame, flipX);
        }

        public bool SetTransformPose(
            Vector3 worldPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            return owner != null
                && owner.TryApplyCinematicTransformPose(
                    this,
                    worldPosition,
                    localRotation,
                    localScale);
        }

        public void Dispose()
        {
            StrategyResidentAgent current = owner;
            owner = null;
            current?.EndCinematicVisualOverride(this);
        }

        internal void Invalidate()
        {
            owner = null;
        }
    }
}
