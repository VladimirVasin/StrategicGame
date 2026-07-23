using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private MonoBehaviour activeWorldInspectBehaviour;
        private IStrategyWorldInspectable activeWorldInspectTarget;
        private float nextWorldInspectRefreshTime;

        private void SetActiveWorldInspectTarget(
            MonoBehaviour behaviour,
            IStrategyWorldInspectable target)
        {
            activeWorldInspectBehaviour = behaviour;
            activeWorldInspectTarget = target;
            nextWorldInspectRefreshTime = Time.unscaledTime + HudRefreshInterval;
        }

        private void ClearActiveWorldInspectTarget()
        {
            activeWorldInspectBehaviour = null;
            activeWorldInspectTarget = null;
            nextWorldInspectRefreshTime = 0f;
        }

        private bool RefreshActiveWorldInspectTarget(bool force = false)
        {
            if (selectedTransform != null
                || inspectHud == null
                || activeWorldInspectBehaviour == null
                || activeWorldInspectTarget == null
                || !activeWorldInspectBehaviour.isActiveAndEnabled
                || !HasVisibleWorldInspectRenderer(activeWorldInspectBehaviour))
            {
                if (activeWorldInspectTarget != null)
                {
                    ClearActiveWorldInspectTarget();
                    inspectHud?.Hide();
                }

                return false;
            }

            if (!force && Time.unscaledTime < nextWorldInspectRefreshTime)
            {
                return true;
            }

            nextWorldInspectRefreshTime = Time.unscaledTime + HudRefreshInterval;
            if (activeWorldInspectTarget.TryGetWorldInspectInfo(out StrategyWorldInspectInfo info)
                && info.IsValid)
            {
                inspectHud.Show(info);
                return true;
            }

            ClearActiveWorldInspectTarget();
            inspectHud.Hide();
            return false;
        }

        private static bool HasVisibleWorldInspectRenderer(MonoBehaviour behaviour)
        {
            SpriteRenderer[] renderers = behaviour.GetComponentsInChildren<SpriteRenderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer != null
                    && renderer.enabled
                    && renderer.sprite != null
                    && !IsAuxiliaryInspectRenderer(renderer))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
