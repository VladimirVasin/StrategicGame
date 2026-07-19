using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public static class StrategyUiThemeProvider
    {
        private const string FontPath = "Fonts/Inter-Regular";
        private const string HearthLedgerFramePath = "UI/HearthLedger/PanelFrame";

        private static Font font;
        private static Sprite panelSprite;
        private static Sprite insetSprite;
        private static Sprite buttonSprite;

        public static Font Font => font ??= Resources.Load<Font>(FontPath)
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Color PanelColor => StrategyHudStyle.Surface;
        public static Color ButtonColor => StrategyHudStyle.Elevated;
        public static Color TextColor => StrategyHudStyle.TextPrimary;
        public static Color AccentColor => StrategyHudStyle.Primary;

        public static void EnsureRuntime()
        {
            StrategyUiThemeRuntime.Ensure();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureRuntime()
        {
            EnsureRuntime();
        }

        internal static Sprite GetPanelSprite()
        {
            if (panelSprite == null)
            {
                panelSprite = Resources.Load<Sprite>(HearthLedgerFramePath)
                    ?? CreateFrameSprite("Hearth and Ledger Panel Fallback", false);
            }

            return panelSprite;
        }

        internal static Sprite GetInsetSprite()
        {
            return insetSprite ??= CreateFrameSprite("Hearth and Ledger Inset", false);
        }

        internal static Sprite GetButtonSprite()
        {
            return buttonSprite ??= CreateFrameSprite("UI Pixel Button", true);
        }

        private static Sprite CreateFrameSprite(string name, bool button)
        {
            const int size = 12;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            Color32 center = new(255, 255, 255, 255);
            Color32 inner = button ? new Color32(226, 232, 218, 255) : new Color32(238, 240, 224, 255);
            Color32 edge = button ? new Color32(142, 126, 92, 255) : new Color32(111, 101, 78, 255);
            Color32 corner = new(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool outerCorner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                    bool border = x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2;
                    pixels[y * size + x] = outerCorner ? corner : border ? edge : x == 2 || y == 2 ? inner : center;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                12f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(3f, 3f, 3f, 3f));
            sprite.name = name;
            return sprite;
        }

        private sealed class StrategyUiThemeRuntime : MonoBehaviour
        {
            private static StrategyUiThemeRuntime instance;
            private int scansRemaining;
            private int frameDelay;

            public static void Ensure()
            {
                if (instance != null || !StrategyRuntimeObjectCreationGuard.CanCreateSceneObjects)
                {
                    return;
                }

                GameObject root = new("Strategy UI Theme");
                DontDestroyOnLoad(root);
                instance = root.AddComponent<StrategyUiThemeRuntime>();
            }

            private void OnEnable()
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                ScheduleScans();
            }

            private void OnDisable()
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                if (instance == this)
                {
                    instance = null;
                }
            }

            private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                ScheduleScans();
            }

            private void ScheduleScans()
            {
                scansRemaining = 5;
                frameDelay = 1;
            }

            private void Update()
            {
                if (scansRemaining <= 0 || --frameDelay > 0)
                {
                    return;
                }

                ApplyTheme();
                scansRemaining--;
                frameDelay = scansRemaining > 1 ? 15 : 45;
            }

            private static void ApplyTheme()
            {
                CanvasScaler[] scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include);
                for (int i = 0; i < scalers.Length; i++)
                {
                    if (scalers[i].GetComponent<StrategyCinematicLetterboxView>() != null)
                    {
                        continue;
                    }

                    StrategyHudStyle.ConfigureScaler(scalers[i], scalers[i].matchWidthOrHeight);
                }

                Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i].font = Font;
                }

                Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include);
                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i];
                    if (image == null
                        || image.sprite != null
                        || image.color.a < 0.20f
                        || IsDecorativeImage(image.name))
                    {
                        continue;
                    }

                    bool button = image.GetComponent<Button>() != null || image.name.Contains("Button");
                    bool panel = image.name.Contains("Panel")
                        || image.name.Contains("Dock")
                        || image.name.Contains("Tray")
                        || image.name.Contains("Card")
                        || image.name.Contains("Dialog");
                    if (!button && !panel)
                    {
                        continue;
                    }

                    bool inset = image.name.Contains("Dock")
                        || image.name.Contains("Tray")
                        || image.name.Contains("Card");
                    image.sprite = button
                        ? GetButtonSprite()
                        : inset ? GetInsetSprite() : GetPanelSprite();
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 2f;
                }
            }

            private static bool IsDecorativeImage(string objectName)
            {
                return objectName.Contains("Icon")
                    || objectName.Contains("Portrait")
                    || objectName.Contains("Shade")
                    || objectName.Contains("Accent")
                    || objectName.Contains("Line");
            }
        }
    }
}
