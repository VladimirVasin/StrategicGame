using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryMoveToTree(StrategyForestryTree tree)
        {
            if (tree == null || !TryFindTreeWorkCell(tree, out Vector2Int workCell))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "LumberMoveRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", "tree"),
                    StrategyDebugLogger.F("reason", tree == null ? "tree_missing" : "no_work_cell"),
                    StrategyDebugLogger.F("treeCell", tree != null ? tree.Cell : Vector2Int.zero));
                return false;
            }

            activeTree = tree;
            activity = ResidentActivity.MovingToTree;
            if (TryBuildPathTo(workCell))
            {
                hasTarget = true;
                waitTimer = Random.Range(0.05f, 0.22f);
                StrategyDebugLogger.Info(
                    "Population",
                    "LumberMoveStarted",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("target", "tree"),
                    StrategyDebugLogger.F("treeCell", tree.Cell),
                    StrategyDebugLogger.F("workCell", workCell));
                return true;
            }

            activeTree = null;
            activity = ResidentActivity.Idle;
            if (WasLastPathBuildDeferred)
            {
                lumberWorkCooldown = Random.Range(0.18f, 0.38f);
                return false;
            }

            lumberWorkCooldown = Random.Range(2.0f, 4.0f);
            StrategyDebugLogger.Warn(
                "Population",
                "LumberMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("target", "tree"),
                StrategyDebugLogger.F("reason", "no_path"),
                StrategyDebugLogger.F("treeCell", tree.Cell),
                StrategyDebugLogger.F("workCell", workCell));
            return false;
        }
    }
}
