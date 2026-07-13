using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectUnknown.Strategy
{
    public enum StrategyGameContextState
    {
        Created,
        Configuring,
        Ready,
        Failed,
        Disposed
    }

    [DisallowMultipleComponent]
    public sealed class StrategyGameContext : MonoBehaviour
    {
        private readonly Dictionary<Type, UnityEngine.Object> services = new();
        private StrategyTimeScaleController bootstrapTimeScale;
        private bool bootstrapPauseHeld;

        public static StrategyGameContext Current { get; private set; }
        public StrategyGameContextState State { get; private set; } = StrategyGameContextState.Created;
        public string FailureReason { get; private set; } = string.Empty;
        public bool IsReady => State == StrategyGameContextState.Ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCurrent()
        {
            Current = null;
        }

        internal static StrategyGameContext GetOrCreateForScene(Scene scene)
        {
            StrategyGameContext[] contexts = FindObjectsByType<StrategyGameContext>(FindObjectsInactive.Include);
            for (int i = 0; i < contexts.Length; i++)
            {
                if (contexts[i] != null && contexts[i].gameObject.scene == scene)
                {
                    Current = contexts[i];
                    return contexts[i];
                }
            }

            GameObject contextObject = new("Strategy Game Context");
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(contextObject, scene);
            }

            StrategyGameContext context = contextObject.AddComponent<StrategyGameContext>();
            Current = context;
            return context;
        }

        public T Register<T>(T service) where T : UnityEngine.Object
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type serviceType = typeof(T);
            if (services.TryGetValue(serviceType, out UnityEngine.Object existing)
                && existing != null
                && existing != service)
            {
                throw new InvalidOperationException("A different service is already registered for " + serviceType.Name + ".");
            }

            services[serviceType] = service;
            return service;
        }

        public bool TryResolve<T>(out T service) where T : UnityEngine.Object
        {
            if (services.TryGetValue(typeof(T), out UnityEngine.Object registered)
                && registered is T resolved
                && resolved != null)
            {
                service = resolved;
                return true;
            }

            services.Remove(typeof(T));
            service = null;
            return false;
        }

        public T GetOrCreate<T>(string objectName) where T : Component
        {
            if (TryResolve(out T registered))
            {
                return registered;
            }

            T service = null;
            T[] candidates = FindObjectsByType<T>(FindObjectsInactive.Include);
            Scene contextScene = gameObject.scene;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] == null || candidates[i].gameObject.scene != contextScene)
                {
                    continue;
                }

                if (service != null && service != candidates[i])
                {
                    throw new InvalidOperationException("Multiple scene services found for " + typeof(T).Name + ".");
                }

                service = candidates[i];
            }

            if (service == null)
            {
                GameObject serviceObject = new(string.IsNullOrWhiteSpace(objectName) ? typeof(T).Name : objectName);
                serviceObject.transform.SetParent(transform, false);
                service = serviceObject.AddComponent<T>();
            }

            return Register(service);
        }

        internal bool BeginBootstrap()
        {
            if (State != StrategyGameContextState.Created)
            {
                return false;
            }

            Current = this;
            State = StrategyGameContextState.Configuring;
            FailureReason = string.Empty;
            StrategyEventLogHudController.ResetSessionState();
            return true;
        }

        internal void HoldBootstrapPause(StrategyTimeScaleController timeScale)
        {
            if (bootstrapPauseHeld || timeScale == null)
            {
                return;
            }

            bootstrapTimeScale = timeScale;
            bootstrapTimeScale.PushPauseLock("Bootstrap");
            bootstrapPauseHeld = true;
        }

        internal void CompleteBootstrap()
        {
            if (State != StrategyGameContextState.Configuring)
            {
                return;
            }

            ReleaseBootstrapPause();
            State = StrategyGameContextState.Ready;
        }

        internal void FailBootstrap(Exception exception)
        {
            if (State != StrategyGameContextState.Configuring)
            {
                return;
            }

            FailureReason = exception != null ? exception.GetType().Name + ": " + exception.Message : "unknown_failure";
            ReleaseBootstrapPause();
            State = StrategyGameContextState.Failed;
        }

        internal void CancelBootstrap(string reason)
        {
            if (State != StrategyGameContextState.Configuring)
            {
                return;
            }

            FailureReason = string.IsNullOrWhiteSpace(reason) ? "cancelled" : reason;
            ReleaseBootstrapPause();
            State = StrategyGameContextState.Failed;
        }

        private void ReleaseBootstrapPause()
        {
            if (bootstrapPauseHeld && bootstrapTimeScale != null)
            {
                bootstrapTimeScale.PopPauseLock("Bootstrap");
            }

            bootstrapPauseHeld = false;
            bootstrapTimeScale = null;
        }

        private void OnDestroy()
        {
            ReleaseBootstrapPause();
            services.Clear();
            State = StrategyGameContextState.Disposed;
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}
