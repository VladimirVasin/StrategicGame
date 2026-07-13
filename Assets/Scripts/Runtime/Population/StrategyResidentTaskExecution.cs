using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyResidentTaskExecutionPhase
    {
        BeforeHomeSchedule,
        Normal,
        PathCompleted
    }

    internal sealed class StrategyResidentTaskExecution
    {
        private static readonly int ActivityCount = Enum.GetValues(
            typeof(StrategyResidentAgent.ResidentActivity)).Length;

        private readonly Action[] preHomeHandlers = new Action[ActivityCount];
        private readonly Action[] normalHandlers = new Action[ActivityCount];
        private readonly Action[] pathCompletedHandlers = new Action[ActivityCount];
        private readonly List<PlannedTask> plannedTasks = new();

        public int PlannedTaskCount => plannedTasks.Count;

        public void Reset()
        {
            Array.Clear(preHomeHandlers, 0, preHomeHandlers.Length);
            Array.Clear(normalHandlers, 0, normalHandlers.Length);
            Array.Clear(pathCompletedHandlers, 0, pathCompletedHandlers.Length);
            plannedTasks.Clear();
        }

        public void Register(
            StrategyResidentAgent.ResidentActivity activity,
            StrategyResidentTaskExecutionPhase phase,
            Action handler)
        {
            int index = (int)activity;
            if (index < 0 || index >= ActivityCount)
            {
                return;
            }

            if (phase == StrategyResidentTaskExecutionPhase.BeforeHomeSchedule)
            {
                preHomeHandlers[index] = handler;
            }
            else if (phase == StrategyResidentTaskExecutionPhase.PathCompleted)
            {
                pathCompletedHandlers[index] = handler;
            }
            else
            {
                normalHandlers[index] = handler;
            }
        }

        public void RegisterPlannedTask(StrategyResidentTaskKind kind, Func<bool> tryStart)
        {
            if (tryStart != null)
            {
                plannedTasks.Add(new PlannedTask(kind, tryStart));
            }
        }

        public bool TryExecute(
            StrategyResidentAgent.ResidentActivity activity,
            StrategyResidentTaskExecutionPhase phase)
        {
            int index = (int)activity;
            if (index < 0 || index >= ActivityCount)
            {
                return false;
            }

            Action handler = phase switch
            {
                StrategyResidentTaskExecutionPhase.BeforeHomeSchedule => preHomeHandlers[index],
                StrategyResidentTaskExecutionPhase.PathCompleted => pathCompletedHandlers[index],
                _ => normalHandlers[index]
            };
            if (handler == null)
            {
                return false;
            }

            handler();
            return true;
        }

        public bool TryStartPlannedTask(
            Func<bool> shouldStop,
            out StrategyResidentTaskKind startedKind)
        {
            using var profilerScope = StrategyPerformanceMarkers.ResidentTaskSelection.Auto();
            startedKind = StrategyResidentTaskKind.Rest;
            for (int i = 0; i < plannedTasks.Count; i++)
            {
                if (plannedTasks[i].TryStart())
                {
                    startedKind = plannedTasks[i].Kind;
                    return true;
                }

                if (shouldStop != null && shouldStop())
                {
                    return false;
                }
            }

            return false;
        }

        private readonly struct PlannedTask
        {
            public PlannedTask(StrategyResidentTaskKind kind, Func<bool> tryStart)
            {
                Kind = kind;
                TryStart = tryStart;
            }

            public StrategyResidentTaskKind Kind { get; }
            public Func<bool> TryStart { get; }
        }
    }
}
