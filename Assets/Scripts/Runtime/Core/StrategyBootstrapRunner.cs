using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyBootstrapRunner : MonoBehaviour
    {
        private StrategyGameContext context;
        private bool finished;

        public void Run(IEnumerator routine, StrategyGameContext gameContext)
        {
            context = gameContext;
            if (routine == null || context == null)
            {
                Destroy(gameObject);
                return;
            }

            StartCoroutine(RunSafely(routine));
        }

        private IEnumerator RunSafely(IEnumerator routine)
        {
            Stack<IEnumerator> routines = new();
            routines.Push(routine);
            while (routines.Count > 0)
            {
                IEnumerator currentRoutine = routines.Peek();
                bool movedNext = false;
                object yielded = null;
                Exception failure = null;
                try
                {
                    movedNext = currentRoutine.MoveNext();
                    if (movedNext)
                    {
                        yielded = currentRoutine.Current;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                if (failure != null)
                {
                    DisposeRoutines(routines);
                    context.FailBootstrap(failure);
                    StrategyDebugLogger.Error(
                        "Bootstrap",
                        "Failed",
                        StrategyDebugLogger.F("error", context.FailureReason));
                    finished = true;
                    Destroy(gameObject);
                    yield break;
                }

                if (!movedNext)
                {
                    (routines.Pop() as IDisposable)?.Dispose();
                    continue;
                }

                if (yielded is IEnumerator nestedRoutine)
                {
                    routines.Push(nestedRoutine);
                    continue;
                }

                yield return yielded;
            }

            context.CompleteBootstrap();
            finished = true;
            Destroy(gameObject);
        }

        private static void DisposeRoutines(Stack<IEnumerator> routines)
        {
            while (routines.Count > 0)
            {
                (routines.Pop() as IDisposable)?.Dispose();
            }
        }

        private void OnDestroy()
        {
            if (!finished && context != null)
            {
                context.CancelBootstrap("bootstrap_runner_destroyed");
            }
        }
    }
}
