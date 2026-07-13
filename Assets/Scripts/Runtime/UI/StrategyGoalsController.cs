using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyGoalsController : MonoBehaviour
    {
        private readonly List<StrategyGoalDefinition> activeGoals = new();
        private readonly HashSet<StrategyGoalKind> completedGoals = new();
        private readonly List<StrategyGoalViewState> viewStates = new();
        private readonly Dictionary<StrategyGoalKind, GoalProgress> goalProgress = new();

        private StrategyGoalsHudController hud;
        private bool configured;

        public static StrategyGoalsController Active { get; private set; }
        public IReadOnlyList<StrategyGoalDefinition> ActiveGoals => activeGoals;
        public bool HasActiveGoals => activeGoals.Count > 0;

        public void Configure(StrategyGoalsHudController hudController)
        {
            hud = hudController;
            configured = true;
            Active = this;

            if (hud != null)
            {
                hud.Configure();
            }

            RefreshHud();
            StrategyDebugLogger.Info("Goals", "Configured", StrategyDebugLogger.F("activeGoals", activeGoals.Count));
        }

        public void SetGoals(params StrategyGoalDefinition[] goals)
        {
            SetGoals((IEnumerable<StrategyGoalDefinition>)goals);
        }

        public void SetGoals(IEnumerable<StrategyGoalDefinition> goals)
        {
            activeGoals.Clear();
            completedGoals.Clear();
            goalProgress.Clear();

            if (goals != null)
            {
                foreach (StrategyGoalDefinition goal in goals)
                {
                    if (goal.Kind == StrategyGoalKind.None || string.IsNullOrWhiteSpace(goal.Title))
                    {
                        continue;
                    }

                    if (ContainsGoalKind(goal.Kind))
                    {
                        continue;
                    }

                    activeGoals.Add(goal);
                }
            }

            RefreshHud();
            StrategyDebugLogger.Info("Goals", "GoalsSet", StrategyDebugLogger.F("count", activeGoals.Count));
        }

        public void ClearGoals()
        {
            if (activeGoals.Count == 0 && completedGoals.Count == 0)
            {
                RefreshHud();
                return;
            }

            activeGoals.Clear();
            completedGoals.Clear();
            goalProgress.Clear();
            RefreshHud();
            StrategyDebugLogger.Info("Goals", "GoalsCleared");
        }

        public bool CompleteGoal(StrategyGoalKind kind)
        {
            if (kind == StrategyGoalKind.None || !ContainsGoalKind(kind))
            {
                StrategyDebugLogger.Warn("Goals", "CompleteIgnored", StrategyDebugLogger.F("kind", kind));
                return false;
            }

            if (!completedGoals.Add(kind))
            {
                return false;
            }

            bool allComplete = AreAllGoalsComplete();
            RefreshHud();
            if (hud != null)
            {
                hud.PlayCompletionPulse(allComplete);
            }

            StrategyDebugLogger.Info(
                "Goals",
                "GoalCompleted",
                StrategyDebugLogger.F("kind", kind),
                StrategyDebugLogger.F("allComplete", allComplete));
            return true;
        }

        public bool IsGoalActive(StrategyGoalKind kind)
        {
            return kind != StrategyGoalKind.None && ContainsGoalKind(kind);
        }

        public bool IsGoalComplete(StrategyGoalKind kind)
        {
            return completedGoals.Contains(kind);
        }

        public void SetGoalProgress(StrategyGoalKind kind, float current, float target, string text)
        {
            if (!ContainsGoalKind(kind) || target <= 0f)
            {
                return;
            }

            GoalProgress next = new GoalProgress(current, target, text);
            if (goalProgress.TryGetValue(kind, out GoalProgress previous)
                && previous.IsApproximately(next))
            {
                return;
            }

            goalProgress[kind] = next;
            RefreshHud();
        }

        private void Awake()
        {
            if (Active == null)
            {
                Active = this;
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        private void RefreshHud()
        {
            if (!configured || hud == null)
            {
                return;
            }

            if (activeGoals.Count == 0)
            {
                hud.ClearGoals();
                return;
            }

            viewStates.Clear();
            for (int i = 0; i < activeGoals.Count; i++)
            {
                StrategyGoalDefinition definition = activeGoals[i];
                bool completed = completedGoals.Contains(definition.Kind);
                if (goalProgress.TryGetValue(definition.Kind, out GoalProgress progress))
                {
                    viewStates.Add(new StrategyGoalViewState(
                        definition,
                        completed,
                        completed ? progress.Target : progress.Current,
                        progress.Target,
                        progress.Text));
                }
                else
                {
                    viewStates.Add(new StrategyGoalViewState(definition, completed));
                }
            }

            hud.SetGoals(viewStates);
        }

        private bool AreAllGoalsComplete()
        {
            if (activeGoals.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < activeGoals.Count; i++)
            {
                if (!completedGoals.Contains(activeGoals[i].Kind))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ContainsGoalKind(StrategyGoalKind kind)
        {
            for (int i = 0; i < activeGoals.Count; i++)
            {
                if (activeGoals[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct GoalProgress
        {
            public GoalProgress(float current, float target, string text)
            {
                Current = current;
                Target = target;
                Text = text ?? string.Empty;
            }

            public float Current { get; }
            public float Target { get; }
            public string Text { get; }

            public bool IsApproximately(GoalProgress other)
            {
                return Mathf.Abs(Current - other.Current) < 0.025f
                    && Mathf.Abs(Target - other.Target) < 0.001f
                    && Text == other.Text;
            }
        }
    }
}
