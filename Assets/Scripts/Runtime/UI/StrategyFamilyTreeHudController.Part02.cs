using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFamilyTreeHudController
    {
        private void ClearContent()
        {
            relationshipLabelsById.Clear();
            hoveredResidentId = 0;
            if (contentRoot == null)
            {
                return;
            }

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }

        private static void AddLine(Transform parent, float x, float y, float width, float height)
        {
            if (width <= 0.1f || height <= 0.1f)
            {
                return;
            }

            RectTransform line = CreateUiObject("Line", parent).GetComponent<RectTransform>();
            SetTopLeft(line, x, y, width, height);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = new Color(0.66f, 0.55f, 0.32f, 0.55f);
            image.raycastTarget = false;
        }

        private static Button CreateButton(string name, Transform parent, string label, int size, Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", root, label, size, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform, 4f, 0f, 4f, 0f);
            return button;
        }

        private static Scrollbar CreateVerticalScrollbar(Transform parent)
        {
            RectTransform root = CreateUiObject("VerticalScrollbar", parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(1f, 0f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 0.5f);
            root.offsetMin = new Vector2(-48f, 56f);
            root.offsetMax = new Vector2(-30f, -104f);

            Image track = root.gameObject.AddComponent<Image>();
            track.color = new Color(0.10f, 0.13f, 0.14f, 0.95f);

            Scrollbar scrollbar = root.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            RectTransform handleArea = CreateUiObject("HandleArea", root).GetComponent<RectTransform>();
            Stretch(handleArea, 2f, 2f, 2f, 2f);

            RectTransform handle = CreateUiObject("Handle", handleArea).GetComponent<RectTransform>();
            Stretch(handle, 0f, 0f, 0f, 0f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.72f, 0.58f, 0.31f, 0.95f);

            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static Scrollbar CreateHorizontalScrollbar(Transform parent)
        {
            RectTransform root = CreateUiObject("HorizontalScrollbar", parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.offsetMin = new Vector2(30f, 30f);
            root.offsetMax = new Vector2(-56f, 48f);

            Image track = root.gameObject.AddComponent<Image>();
            track.color = new Color(0.10f, 0.13f, 0.14f, 0.95f);

            Scrollbar scrollbar = root.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.LeftToRight;

            RectTransform handleArea = CreateUiObject("HandleArea", root).GetComponent<RectTransform>();
            Stretch(handleArea, 2f, 2f, 2f, 2f);

            RectTransform handle = CreateUiObject("Handle", handleArea).GetComponent<RectTransform>();
            Stretch(handle, 0f, 0f, 0f, 0f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.72f, 0.58f, 0.31f, 0.95f);

            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = root.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
