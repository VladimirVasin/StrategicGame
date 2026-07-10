using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyTimeOfDayPhase
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Dusk,
        Night
    }

    public readonly struct StrategyCalendarSnapshot
    {
        public StrategyCalendarSnapshot(
            int dayIndex,
            float dayPhase,
            int hour,
            int minute,
            StrategyTimeOfDayPhase phase,
            float phaseProgress,
            StrategySeason season,
            int seasonDay,
            int year,
            float seasonProgress)
        {
            DayIndex = dayIndex;
            DayPhase = dayPhase;
            Hour = hour;
            Minute = minute;
            Phase = phase;
            PhaseProgress = phaseProgress;
            Season = season;
            SeasonDay = seasonDay;
            Year = year;
            SeasonProgress = seasonProgress;
        }

        public int DayIndex { get; }
        public int DisplayDay => DayIndex + 1;
        public float DayPhase { get; }
        public int Hour { get; }
        public int Minute { get; }
        public StrategyTimeOfDayPhase Phase { get; }
        public float PhaseProgress { get; }
        public StrategySeason Season { get; }
        public int SeasonDay { get; }
        public int Year { get; }
        public float SeasonProgress { get; }
        public bool IsNight => Phase == StrategyTimeOfDayPhase.Night;
        public string ClockText => Hour.ToString("00") + ":" + Minute.ToString("00");
        public string PhaseLabel => StrategyDayNightCycleController.GetPhaseLabel(Phase);
        public string SeasonLabel => StrategySeasonCalendar.GetSeasonLabel(Season);
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyDayNightCycleController : MonoBehaviour
    {
        private const float CycleSeconds = 360f;
        private const float HoursPerDay = 24f;
        private const float ClockStartHour = 5f;
        private const float DawnEndHour = 7f;
        private const float MorningEndHour = 11f;
        private const float NoonEndHour = 15f;
        private const float AfternoonEndHour = 19f;
        private const float DuskEndHour = 22f;
        private const float DawnEnd = (DawnEndHour - ClockStartHour) / HoursPerDay;
        private const float MorningEnd = (MorningEndHour - ClockStartHour) / HoursPerDay;
        private const float NoonEnd = (NoonEndHour - ClockStartHour) / HoursPerDay;
        private const float AfternoonEnd = (AfternoonEndHour - ClockStartHour) / HoursPerDay;
        private const float DuskEnd = (DuskEndHour - ClockStartHour) / HoursPerDay;

        private static Sprite overlaySprite;
        private static float elapsedTimeOffset;

        private CityMapController map;
        private Camera strategyCamera;
        private SpriteRenderer overlayRenderer;
        private Color baseCameraColor = new(0.09f, 0.12f, 0.14f);
        private StrategyTimeOfDayPhase loggedPhase = (StrategyTimeOfDayPhase)(-1);

        public float DayPhase => Mathf.Repeat(CurrentElapsedSeconds / CycleSeconds, 1f);
        public StrategyCalendarSnapshot CurrentSnapshot => CreateSnapshot(CurrentElapsedSeconds);
        public static float DayLengthSeconds => CycleSeconds;
        public static float NightStartPhase => DuskEnd;
        public static float CurrentElapsedSeconds => Mathf.Max(0f, Time.timeSinceLevelLoad + elapsedTimeOffset);
        public static int CurrentDayIndex => Mathf.FloorToInt(CurrentElapsedSeconds / CycleSeconds);
        public static float CurrentDayPhase => Mathf.Repeat(CurrentElapsedSeconds / CycleSeconds, 1f);
        public static StrategyCalendarSnapshot CurrentCalendarSnapshot => CreateSnapshot(CurrentElapsedSeconds);

        public static void RestoreElapsedSeconds(float elapsedSeconds)
        {
            elapsedTimeOffset = Mathf.Max(0f, elapsedSeconds) - Time.timeSinceLevelLoad;
        }
        public static bool IsSettlementWorkTime
        {
            get
            {
                float phase = CurrentDayPhase;
                return phase < DuskEnd;
            }
        }

        public static bool IsHouseholdOutdoorWorkTime
        {
            get { return IsSettlementWorkTime; }
        }
        public static bool IsResidentEveningHomeTime
        {
            get
            {
                StrategyCalendarSnapshot snapshot = CurrentCalendarSnapshot;
                return snapshot.Phase == StrategyTimeOfDayPhase.Night
                    || (snapshot.Phase == StrategyTimeOfDayPhase.Dusk && snapshot.Hour >= 21);
            }
        }
        public static bool IsHouseholdCookingTime => CurrentCalendarSnapshot.Phase == StrategyTimeOfDayPhase.Dusk;

        public static float ShadowOpacityMultiplier { get; private set; } = 1f;
        public static float ShadowLengthMultiplier { get; private set; } = 1f;

        public void Configure(CityMapController mapController, Camera camera)
        {
            map = mapController;
            strategyCamera = camera;
            if (strategyCamera != null)
            {
                baseCameraColor = strategyCamera.backgroundColor;
            }

            EnsureOverlayRenderer();
            ResizeOverlay();
            ApplyVisuals(true);
            StrategyDebugLogger.Info(
                "DayNight",
                "Configured",
                StrategyDebugLogger.F("cycleSeconds", CycleSeconds),
                StrategyDebugLogger.F("sortingOrder", StrategyWorldSorting.DayNightOverlayOrder));
        }

        private void Update()
        {
            if (map == null || overlayRenderer == null)
            {
                return;
            }

            ResizeOverlay();
            ApplyVisuals(false);
        }

        private void EnsureOverlayRenderer()
        {
            if (overlayRenderer != null)
            {
                return;
            }

            GameObject overlayObject = new GameObject("Day Night Overlay");
            overlayObject.transform.SetParent(transform, false);
            overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = GetOverlaySprite();
            overlayRenderer.sortingOrder = StrategyWorldSorting.DayNightOverlayOrder;
            overlayRenderer.color = Color.clear;
        }

        private void ResizeOverlay()
        {
            if (map == null || overlayRenderer == null)
            {
                return;
            }

            Bounds bounds = map.WorldBounds;
            overlayRenderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, -0.2f);
            overlayRenderer.transform.localScale = new Vector3(
                Mathf.Max(1f, bounds.size.x),
                Mathf.Max(1f, bounds.size.y),
                1f);
        }

        private void ApplyVisuals(bool forceLog)
        {
            StrategyCalendarSnapshot snapshot = CurrentSnapshot;
            float phase = snapshot.DayPhase;
            Color overlayColor = EvaluateOverlayColor(phase);
            overlayRenderer.color = overlayColor;
            ShadowOpacityMultiplier = EvaluateShadowOpacity(phase);
            ShadowLengthMultiplier = EvaluateShadowLength(phase);

            if (strategyCamera != null)
            {
                strategyCamera.backgroundColor = EvaluateCameraColor(phase);
            }

            if (forceLog || snapshot.Phase != loggedPhase)
            {
                loggedPhase = snapshot.Phase;
                StrategyDebugLogger.Info(
                    "DayNight",
                    "PhaseChanged",
                    StrategyDebugLogger.F("day", snapshot.DisplayDay),
                    StrategyDebugLogger.F("clock", snapshot.ClockText),
                    StrategyDebugLogger.F("phase", snapshot.PhaseLabel),
                    StrategyDebugLogger.F("phaseValue", phase),
                    StrategyDebugLogger.F("overlayAlpha", overlayColor.a));
            }

            AnnouncePlayerFacingPhase(snapshot);
        }

        private static Color EvaluateOverlayColor(float phase)
        {
            Color dawn = new Color(0.82f, 0.46f, 0.18f, 0.14f);
            Color day = new Color(0.08f, 0.10f, 0.06f, 0.01f);
            Color dusk = new Color(0.86f, 0.30f, 0.10f, 0.22f);
            Color night = new Color(0.015f, 0.055f, 0.17f, 0.43f);

            if (phase < DawnEnd)
            {
                return Color.Lerp(night, dawn, Smooth01(phase / DawnEnd));
            }

            if (phase < MorningEnd)
            {
                return Color.Lerp(dawn, day, Smooth01((phase - DawnEnd) / (MorningEnd - DawnEnd)));
            }

            if (phase < AfternoonEnd)
            {
                return day;
            }

            if (phase < DuskEnd)
            {
                return EvaluateDuskColor(day, dusk, night, phase);
            }

            return night;
        }

        private static float EvaluateShadowOpacity(float phase)
        {
            if (phase < DawnEnd)
            {
                return Mathf.Lerp(0.32f, 0.80f, Smooth01(phase / DawnEnd));
            }

            if (phase < MorningEnd)
            {
                return Mathf.Lerp(0.80f, 1f, Smooth01((phase - DawnEnd) / (MorningEnd - DawnEnd)));
            }

            if (phase < AfternoonEnd)
            {
                return 1f;
            }

            if (phase < DuskEnd)
            {
                return Mathf.Lerp(1f, 0.32f, Smooth01((phase - AfternoonEnd) / (DuskEnd - AfternoonEnd)));
            }

            return 0.32f;
        }

        private static float EvaluateShadowLength(float phase)
        {
            if (phase < DawnEnd)
            {
                return Mathf.Lerp(0.78f, 1.52f, Smooth01(phase / DawnEnd));
            }

            if (phase < MorningEnd)
            {
                return Mathf.Lerp(1.52f, 0.92f, Smooth01((phase - DawnEnd) / (MorningEnd - DawnEnd)));
            }

            if (phase < AfternoonEnd)
            {
                return 0.92f;
            }

            if (phase < DuskEnd)
            {
                return Mathf.Lerp(0.92f, 0.78f, Smooth01((phase - AfternoonEnd) / (DuskEnd - AfternoonEnd)));
            }

            return 0.78f;
        }

        private Color EvaluateCameraColor(float phase)
        {
            Color nightCamera = new Color(0.018f, 0.026f, 0.058f);
            Color duskCamera = new Color(0.13f, 0.075f, 0.065f);
            Color dawnCamera = new Color(0.12f, 0.095f, 0.070f);

            if (phase < DawnEnd)
            {
                return Color.Lerp(nightCamera, dawnCamera, Smooth01(phase / DawnEnd));
            }

            if (phase < MorningEnd)
            {
                return Color.Lerp(dawnCamera, baseCameraColor, Smooth01((phase - DawnEnd) / (MorningEnd - DawnEnd)));
            }

            if (phase < AfternoonEnd)
            {
                return baseCameraColor;
            }

            if (phase < DuskEnd)
            {
                return EvaluateDuskColor(baseCameraColor, duskCamera, nightCamera, phase);
            }

            return nightCamera;
        }

        private static Color EvaluateDuskColor(Color day, Color dusk, Color night, float phase)
        {
            float t = Smooth01((phase - AfternoonEnd) / (DuskEnd - AfternoonEnd));
            return t < 0.5f
                ? Color.Lerp(day, dusk, Smooth01(t * 2f))
                : Color.Lerp(dusk, night, Smooth01((t - 0.5f) * 2f));
        }

        public static string GetPhaseLabel(StrategyTimeOfDayPhase phase)
        {
            switch (phase)
            {
                case StrategyTimeOfDayPhase.Dawn:
                    return "Dawn";
                case StrategyTimeOfDayPhase.Morning:
                    return "Morning";
                case StrategyTimeOfDayPhase.Noon:
                    return "Noon";
                case StrategyTimeOfDayPhase.Afternoon:
                    return "Afternoon";
                case StrategyTimeOfDayPhase.Dusk:
                    return "Dusk";
                case StrategyTimeOfDayPhase.Night:
                    return "Night";
                default:
                    return "Day";
            }
        }

        public static Color GetPhaseAccentColor(StrategyTimeOfDayPhase phase)
        {
            switch (phase)
            {
                case StrategyTimeOfDayPhase.Dawn:
                    return new Color(1f, 0.76f, 0.42f);
                case StrategyTimeOfDayPhase.Morning:
                    return new Color(0.96f, 0.88f, 0.58f);
                case StrategyTimeOfDayPhase.Noon:
                    return new Color(0.95f, 0.93f, 0.76f);
                case StrategyTimeOfDayPhase.Afternoon:
                    return new Color(0.92f, 0.82f, 0.50f);
                case StrategyTimeOfDayPhase.Dusk:
                    return new Color(1f, 0.55f, 0.32f);
                case StrategyTimeOfDayPhase.Night:
                    return new Color(0.50f, 0.70f, 1f);
                default:
                    return new Color(0.95f, 0.88f, 0.62f);
            }
        }

        private static StrategyCalendarSnapshot CreateSnapshot(float elapsedSeconds)
        {
            int dayIndex = Mathf.FloorToInt(elapsedSeconds / CycleSeconds);
            float phase = Mathf.Repeat(elapsedSeconds / CycleSeconds, 1f);
            float hourValue = Mathf.Repeat(ClockStartHour + phase * 24f, 24f);
            int totalMinutes = Mathf.FloorToInt(hourValue * 60f) % (24 * 60);
            int hour = totalMinutes / 60;
            int minute = totalMinutes % 60;
            StrategyTimeOfDayPhase timePhase = EvaluateTimeOfDayPhase(phase);
            float progress = EvaluatePhaseProgress(phase, timePhase);
            StrategySeason season = StrategySeasonCalendar.GetSeason(dayIndex);
            int seasonDay = StrategySeasonCalendar.GetSeasonDay(dayIndex);
            int year = StrategySeasonCalendar.GetYear(dayIndex);
            float seasonProgress = StrategySeasonCalendar.GetSeasonProgress(dayIndex, phase);
            return new StrategyCalendarSnapshot(
                dayIndex,
                phase,
                hour,
                minute,
                timePhase,
                progress,
                season,
                seasonDay,
                year,
                seasonProgress);
        }

        private static StrategyTimeOfDayPhase EvaluateTimeOfDayPhase(float phase)
        {
            if (phase < DawnEnd)
            {
                return StrategyTimeOfDayPhase.Dawn;
            }

            if (phase < MorningEnd)
            {
                return StrategyTimeOfDayPhase.Morning;
            }

            if (phase < NoonEnd)
            {
                return StrategyTimeOfDayPhase.Noon;
            }

            if (phase < AfternoonEnd)
            {
                return StrategyTimeOfDayPhase.Afternoon;
            }

            if (phase < DuskEnd)
            {
                return StrategyTimeOfDayPhase.Dusk;
            }

            return StrategyTimeOfDayPhase.Night;
        }

        private static float EvaluatePhaseProgress(float phase, StrategyTimeOfDayPhase timePhase)
        {
            switch (timePhase)
            {
                case StrategyTimeOfDayPhase.Dawn:
                    return Mathf.InverseLerp(0f, DawnEnd, phase);
                case StrategyTimeOfDayPhase.Morning:
                    return Mathf.InverseLerp(DawnEnd, MorningEnd, phase);
                case StrategyTimeOfDayPhase.Noon:
                    return Mathf.InverseLerp(MorningEnd, NoonEnd, phase);
                case StrategyTimeOfDayPhase.Afternoon:
                    return Mathf.InverseLerp(NoonEnd, AfternoonEnd, phase);
                case StrategyTimeOfDayPhase.Dusk:
                    return Mathf.InverseLerp(AfternoonEnd, DuskEnd, phase);
                case StrategyTimeOfDayPhase.Night:
                    return Mathf.InverseLerp(DuskEnd, 1f, phase);
                default:
                    return 0f;
            }
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static Sprite GetOverlaySprite()
        {
            if (overlaySprite != null)
            {
                return overlaySprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Day Night Overlay Pixel"
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            overlaySprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            overlaySprite.name = "Day Night Overlay Sprite";
            return overlaySprite;
        }
    }
}
