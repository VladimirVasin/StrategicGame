using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {
        private void UpdateAnimation(bool instant = false)
        {
            float target = isOpen ? 1f : 0f;
            panelT = instant
                ? target
                : Mathf.MoveTowards(panelT, target, Time.unscaledDeltaTime * AnimationSpeed);

            if (panelGroup == null || panelRoot == null)
            {
                return;
            }

            float smooth = Smooth01(panelT);
            panelGroup.alpha = smooth;
            panelGroup.interactable = isOpen;
            panelGroup.blocksRaycasts = isOpen;
            panelRoot.anchoredPosition = new Vector2(0f, -76f - (1f - smooth) * 18f);
            panelRoot.gameObject.SetActive(panelT > 0.001f || isOpen);
        }

        private void HandleManualScroll()
        {
            if (!isOpen
                || professionScroll == null
                || panelRoot == null
                || viewportRoot == null
                || contentRoot == null
                || Mouse.current == null)
            {
                return;
            }

            Vector2 pointer = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRoot, pointer))
            {
                return;
            }

            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) <= 0.01f)
            {
                return;
            }

            float overflow = Mathf.Max(0f, contentRoot.rect.height - viewportRoot.rect.height);
            if (overflow <= 0.01f)
            {
                return;
            }

            float normalizedDelta = wheel * professionScroll.scrollSensitivity / overflow;
            professionScroll.verticalNormalizedPosition = Mathf.Clamp01(
                professionScroll.verticalNormalizedPosition + normalizedDelta);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                UnityEngine.Object.Destroy(standalone);
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
