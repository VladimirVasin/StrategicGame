using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyUiInputModuleBootstrap
    {
        internal static void Ensure()
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            EventSystem eventSystem = FindActiveEventSystem(eventSystems);
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }

            RemoveDuplicateEventSystems(eventSystems, eventSystem);
            InputSystemUIInputModule inputModule = EnsureInputModule(eventSystem);
            inputModule.actionsAsset = InputSystem.actions;
            inputModule.enabled = true;
            eventSystem.enabled = true;
        }

        private static EventSystem FindActiveEventSystem(EventSystem[] eventSystems)
        {
            if (EventSystem.current != null && EventSystem.current.isActiveAndEnabled)
            {
                return EventSystem.current;
            }

            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem candidate = eventSystems[i];
                if (candidate != null && candidate.isActiveAndEnabled)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void RemoveDuplicateEventSystems(
            EventSystem[] eventSystems,
            EventSystem retainedEventSystem)
        {
            for (int i = 0; i < eventSystems.Length; i++)
            {
                EventSystem duplicate = eventSystems[i];
                if (duplicate == null || duplicate == retainedEventSystem)
                {
                    continue;
                }

                BaseInputModule[] duplicateModules = duplicate.GetComponents<BaseInputModule>();
                for (int moduleIndex = 0; moduleIndex < duplicateModules.Length; moduleIndex++)
                {
                    BaseInputModule duplicateModule = duplicateModules[moduleIndex];
                    duplicateModule.enabled = false;
                    Object.Destroy(duplicateModule);
                }

                duplicate.enabled = false;
                Object.Destroy(duplicate);
            }
        }

        private static InputSystemUIInputModule EnsureInputModule(EventSystem eventSystem)
        {
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                BaseInputModule module = modules[i];
                if (module == inputModule)
                {
                    continue;
                }

                module.enabled = false;
                Object.Destroy(module);
            }

            return inputModule;
        }
    }
}
