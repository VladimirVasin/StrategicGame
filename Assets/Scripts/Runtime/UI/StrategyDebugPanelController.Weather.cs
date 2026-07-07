using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDebugPanelController
    {
        private void BuildWeatherButtons(RectTransform parent)
        {
            StrategyWeatherKind[] kinds =
            {
                StrategyWeatherKind.Clear,
                StrategyWeatherKind.Cloudy,
                StrategyWeatherKind.LightRain,
                StrategyWeatherKind.HeavyRain,
                StrategyWeatherKind.Fog,
                StrategyWeatherKind.Storm,
                StrategyWeatherKind.Snow,
                StrategyWeatherKind.Blizzard
            };

            const float startX = 18f;
            const float startY = 48f;
            const float buttonWidth = 148f;
            const float buttonHeight = 38f;
            const float gapX = 14f;
            const float gapY = 12f;

            for (int i = 0; i < kinds.Length; i++)
            {
                StrategyWeatherKind kind = kinds[i];
                Button button = CreateButton("Weather_" + kind, parent, GetWeatherLabel(kind), 13, ButtonColor);
                int col = i % 3;
                int row = i / 3;
                SetTopLeft(
                    button.GetComponent<RectTransform>(),
                    startX + col * (buttonWidth + gapX),
                    startY + row * (buttonHeight + gapY),
                    buttonWidth,
                    buttonHeight);

                Image image = button.GetComponent<Image>();
                Text label = button.GetComponentInChildren<Text>();
                button.onClick.AddListener(() => ForceWeatherSmooth(kind));
                weatherButtons.Add(new WeatherButtonView(kind, image, label));
            }
        }

        private void ForceWeatherSmooth(StrategyWeatherKind kind)
        {
            if (weather == null)
            {
                weather = Object.FindAnyObjectByType<StrategyWeatherController>();
            }

            weather?.ForceWeatherSmooth(kind);
            RefreshControls();
            StrategyDebugLogger.Info("DebugPanel", "WeatherSmoothForced", StrategyDebugLogger.F("state", kind));
        }

        private static string GetWeatherLabel(StrategyWeatherKind kind)
        {
            switch (kind)
            {
                case StrategyWeatherKind.Clear:
                    return "Clear";
                case StrategyWeatherKind.Cloudy:
                    return "Cloudy";
                case StrategyWeatherKind.LightRain:
                    return "Light Rain";
                case StrategyWeatherKind.HeavyRain:
                    return "Heavy Rain";
                case StrategyWeatherKind.Fog:
                    return "Fog";
                case StrategyWeatherKind.Storm:
                    return "Storm";
                case StrategyWeatherKind.Snow:
                    return "Snow";
                case StrategyWeatherKind.Blizzard:
                    return "Blizzard";
                default:
                    return kind.ToString();
            }
        }

        private readonly struct WeatherButtonView
        {
            public WeatherButtonView(StrategyWeatherKind kind, Image image, Text label)
            {
                Kind = kind;
                Image = image;
                Label = label;
            }

            public StrategyWeatherKind Kind { get; }
            public Image Image { get; }
            public Text Label { get; }
        }
    }
}
