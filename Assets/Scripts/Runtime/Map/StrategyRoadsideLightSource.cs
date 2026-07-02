using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    internal sealed class StrategyRoadsideLightSource : MonoBehaviour
    {
        private static readonly List<StrategyRoadsideLightSource> activeSources = new();

        public Vector2Int RoadCell { get; private set; }
        public Vector2Int SideOffset { get; private set; }

        public static void CopyActiveSources(List<StrategyRoadsideLightSource> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            for (int i = activeSources.Count - 1; i >= 0; i--)
            {
                StrategyRoadsideLightSource source = activeSources[i];
                if (source == null || !source.isActiveAndEnabled)
                {
                    activeSources.RemoveAt(i);
                    continue;
                }

                target.Add(source);
            }
        }

        public void Configure(Vector2Int roadCell, Vector2Int sideOffset)
        {
            RoadCell = roadCell;
            SideOffset = sideOffset;
            RegisterActive();
        }

        private void OnEnable()
        {
            RegisterActive();
        }

        private void OnDisable()
        {
            activeSources.Remove(this);
        }

        private void OnDestroy()
        {
            activeSources.Remove(this);
        }

        private void RegisterActive()
        {
            if (isActiveAndEnabled && !activeSources.Contains(this))
            {
                activeSources.Add(this);
            }
        }
    }
}
