using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyUiButtonFeedbackTests
    {
        private readonly List<GameObject> objects = new();
        private EventSystem eventSystem;

        [SetUp]
        public void SetUp()
        {
            GameObject eventSystemObject = Track(new GameObject("Test Event System", typeof(EventSystem)));
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                if (objects[i] != null)
                {
                    Object.DestroyImmediate(objects[i]);
                }
            }

            objects.Clear();
            StrategyHudSfxAudio[] audioObjects = Object.FindObjectsByType<StrategyHudSfxAudio>(
                FindObjectsInactive.Include);
            for (int i = 0; i < audioObjects.Length; i++)
            {
                Object.DestroyImmediate(audioObjects[i].gameObject);
            }
        }

        [Test]
        public void SharedFeedbackHandsProgrammaticFocusToPointerAndClearsItOnExit()
        {
            Button button = CreateButton("Shared Button", out _);
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.3f, 0.4f, 1f);
            colors.highlightedColor = new Color(0.8f, 0.7f, 0.2f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            StrategyUiButtonFeedback feedback = StrategyUiButtonFeedback.Attach(button);
            feedback.SuppressNextFocusCue();

            eventSystem.SetSelectedGameObject(button.gameObject);
            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(button.gameObject));

            PointerEventData pointer = new(eventSystem);
            feedback.OnPointerEnter(pointer);
            feedback.OnPointerExit(pointer);

            Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
            Assert.That(button.colors.selectedColor, Is.EqualTo(button.colors.normalColor));
        }

        [Test]
        public void SharedFeedbackClearsSelectionWhenItsButtonIsHidden()
        {
            Button button = CreateButton("Hidden Button", out _);
            StrategyUiButtonFeedback feedback = StrategyUiButtonFeedback.Attach(button);
            feedback.SuppressNextFocusCue();
            eventSystem.SetSelectedGameObject(button.gameObject);

            button.gameObject.SetActive(false);

            Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
        }

        [Test]
        public void MainMenuFeedbackHandsProgrammaticFocusToPointerAndClearsItOnExit()
        {
            Button button = CreateButton("Main Menu Button", out Image background);
            button.transition = Selectable.Transition.None;
            Image accent = CreateChildGraphic<Image>(button.transform, "Accent");
            Text label = CreateChildGraphic<Text>(button.transform, "Label");
            StrategyMainMenuButtonHover feedback = button.gameObject.AddComponent<StrategyMainMenuButtonHover>();
            feedback.Configure(
                button,
                background,
                accent,
                label,
                new Color(0.12f, 0.18f, 0.15f, 1f),
                new Color(0.18f, 0.29f, 0.22f, 1f),
                new Color(0.88f, 0.70f, 0.35f, 1f));

            eventSystem.SetSelectedGameObject(button.gameObject);
            PointerEventData pointer = new(eventSystem);
            feedback.OnPointerEnter(pointer);
            feedback.OnPointerExit(pointer);

            Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
        }

        private Button CreateButton(string name, out Image image)
        {
            GameObject root = Track(new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)));
            image = root.GetComponent<Image>();
            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private T CreateChildGraphic<T>(Transform parent, string name) where T : Graphic
        {
            GameObject child = Track(new GameObject(name, typeof(RectTransform), typeof(T)));
            child.transform.SetParent(parent, false);
            return child.GetComponent<T>();
        }

        private GameObject Track(GameObject value)
        {
            objects.Add(value);
            return value;
        }
    }
}
