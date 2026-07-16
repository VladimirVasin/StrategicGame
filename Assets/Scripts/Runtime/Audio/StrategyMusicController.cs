using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyMusicController : MonoBehaviour
    {
        private const string MusicFolderPath = "Audio/Music";
        private const float TargetVolume = 0.12f;
        private const float FadeInSpeed = 0.18f;
        private const float TrackEndFadeSeconds = 5f;

        private AudioSource musicSource;
        private AudioReverbFilter reverbFilter;
        private readonly StrategyMusicShuffleBag shuffleBag = new();
        private AudioClip[] playlist = Array.Empty<AudioClip>();
        private int currentTrackIndex = -1;
        private bool configured;
        private bool windowFocused = true;
        private bool applicationPaused;
        private bool hasAudioFocus = true;
        private bool pausedForFocusLoss;
        private float nextTrackTime;
        private bool waitingBetweenTracks;

        public void Configure()
        {
            windowFocused = Application.isFocused;
            hasAudioFocus = windowFocused && !applicationPaused;
            playlist = Resources.LoadAll<AudioClip>(MusicFolderPath);
            if (playlist.Length > 1)
            {
                Array.Sort(playlist, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            }

            currentTrackIndex = -1;
            waitingBetweenTracks = false;
            shuffleBag.Reset(playlist.Length);

            EnsureMusicSource();
            configured = true;

            if (playlist.Length <= 0)
            {
                StrategyDebugLogger.Warn(
                    "Audio",
                    "InGameMusicPlaylistMissing",
                    StrategyDebugLogger.F("resourcesPath", MusicFolderPath),
                    StrategyDebugLogger.F("folder", "Assets/Resources/Audio/Music"),
                    StrategyDebugLogger.F("exampleFile", "Music_01.mp3"));
                return;
            }

            StrategyDebugLogger.Info(
                "Audio",
                "InGameMusicPlaylistLoaded",
                StrategyDebugLogger.F("trackCount", playlist.Length),
                StrategyDebugLogger.F("targetVolume", TargetVolume));
            if (hasAudioFocus)
            {
                PlayNextTrack(GetDesiredMood());
            }
        }

        private void Update()
        {
            if (!configured || musicSource == null || playlist.Length <= 0)
            {
                return;
            }

            if (!hasAudioFocus || pausedForFocusLoss)
            {
                return;
            }

            if (!musicSource.isPlaying)
            {
                if (!waitingBetweenTracks)
                {
                    waitingBetweenTracks = true;
                    nextTrackTime = Time.unscaledTime + GetSilenceDuration();
                }

                if (Time.unscaledTime >= nextTrackTime)
                {
                    waitingBetweenTracks = false;
                    PlayNextTrack(GetDesiredMood());
                }

                return;
            }

            float endFade = musicSource.clip != null
                ? Mathf.InverseLerp(0f, TrackEndFadeSeconds, musicSource.clip.length - musicSource.time)
                : 1f;
            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                TargetVolume * endFade * StrategyAudioMixController.GetVolume(StrategyAudioBus.Music),
                FadeInSpeed * Time.unscaledDeltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            windowFocused = hasFocus;
            RefreshAudioFocus("application_focus");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            applicationPaused = pauseStatus;
            RefreshAudioFocus("application_pause");
        }

        private void RefreshAudioFocus(string reason)
        {
            bool focused = windowFocused && !applicationPaused;
            if (hasAudioFocus == focused)
            {
                return;
            }

            hasAudioFocus = focused;
            if (!configured || musicSource == null || playlist.Length <= 0)
            {
                return;
            }

            if (!focused)
            {
                PauseForFocusLoss(reason);
                return;
            }

            ResumeFromFocusLoss(reason);
        }

        private void PauseForFocusLoss(string reason)
        {
            if (pausedForFocusLoss || !musicSource.isPlaying)
            {
                return;
            }

            musicSource.Pause();
            pausedForFocusLoss = true;
            StrategyDebugLogger.Info(
                "Audio",
                "InGameMusicPausedForFocusLoss",
                StrategyDebugLogger.F("clip", musicSource.clip != null ? musicSource.clip.name : string.Empty),
                StrategyDebugLogger.F("trackIndex", currentTrackIndex),
                StrategyDebugLogger.F("timeSamples", musicSource.timeSamples),
                StrategyDebugLogger.F("reason", reason));
        }

        private void ResumeFromFocusLoss(string reason)
        {
            if (!pausedForFocusLoss)
            {
                if (configured
                    && hasAudioFocus
                    && musicSource != null
                    && musicSource.clip == null
                    && playlist.Length > 0)
                {
                    PlayNextTrack(GetDesiredMood());
                }

                return;
            }

            pausedForFocusLoss = false;
            if (musicSource.clip == null)
            {
                return;
            }

            musicSource.UnPause();
            StrategyDebugLogger.Info(
                "Audio",
                "InGameMusicResumedAfterFocusLoss",
                StrategyDebugLogger.F("clip", musicSource.clip.name),
                StrategyDebugLogger.F("trackIndex", currentTrackIndex),
                StrategyDebugLogger.F("timeSamples", musicSource.timeSamples),
                StrategyDebugLogger.F("reason", reason));
        }

        private void PlayNextTrack(StrategyMusicMood mood)
        {
            int nextTrackIndex = PickNextTrackIndex(mood);
            if (nextTrackIndex < 0 || nextTrackIndex >= playlist.Length)
            {
                return;
            }

            currentTrackIndex = nextTrackIndex;
            AudioClip clip = playlist[currentTrackIndex];
            musicSource.clip = clip;
            musicSource.volume = 0f;
            musicSource.Play();
            StrategyDebugLogger.Info(
                "Audio",
                "InGameMusicTrackStarted",
                StrategyDebugLogger.F("clip", clip.name),
                StrategyDebugLogger.F("trackIndex", currentTrackIndex),
                StrategyDebugLogger.F("trackCount", playlist.Length),
                StrategyDebugLogger.F("rotationRemaining", shuffleBag.RemainingCount),
                StrategyDebugLogger.F("mood", mood));
        }

        private int PickNextTrackIndex(StrategyMusicMood mood)
        {
            return shuffleBag.PickNext(
                playlist.Length,
                currentTrackIndex,
                index => MatchesMood(playlist[index], mood));
        }

        private static StrategyMusicMood GetDesiredMood()
        {
            StrategyWeatherController weather = StrategyWeatherController.Active;
            if (weather != null && Mathf.Max(weather.StormIntensity, weather.HeavySnowIntensity) > 0.55f)
            {
                return StrategyMusicMood.Storm;
            }

            StrategyCalendarSnapshot snapshot = StrategyDayNightCycleController.CurrentCalendarSnapshot;
            if (snapshot.Season == StrategySeason.Winter)
            {
                return StrategyMusicMood.Winter;
            }

            return snapshot.Phase == StrategyTimeOfDayPhase.Night
                ? StrategyMusicMood.Night
                : StrategyMusicMood.Calm;
        }

        private static bool MatchesMood(AudioClip clip, StrategyMusicMood mood)
        {
            if (clip == null)
            {
                return false;
            }

            string name = clip.name.ToLowerInvariant();
            string tag = mood.ToString().ToLowerInvariant();
            bool hasKnownTag = name.Contains("calm") || name.Contains("night") || name.Contains("winter") || name.Contains("storm");
            return hasKnownTag && name.Contains(tag);
        }

        private static float GetSilenceDuration()
        {
            StrategyMusicMood mood = GetDesiredMood();
            return mood == StrategyMusicMood.Storm
                ? UnityEngine.Random.Range(8f, 16f)
                : UnityEngine.Random.Range(18f, 38f);
        }

        private void EnsureMusicSource()
        {
            if (musicSource != null)
            {
                EnsureReverbFilter();
                return;
            }

            musicSource = StrategyAudioMixController.CreateRuntimeSource(transform, "Music Source", StrategyAudioBus.Music);
            musicSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 96;
            musicSource.dopplerLevel = 0f;
            musicSource.volume = 0f;
            EnsureReverbFilter();
        }

        private void EnsureReverbFilter()
        {
            GameObject filterHost = musicSource != null ? musicSource.gameObject : gameObject;
            if (reverbFilter == null)
            {
                reverbFilter = filterHost.GetComponent<AudioReverbFilter>();
            }

            if (reverbFilter == null)
            {
                reverbFilter = filterHost.AddComponent<AudioReverbFilter>();
            }

            reverbFilter.reverbPreset = AudioReverbPreset.User;
            reverbFilter.dryLevel = 0f;
            reverbFilter.room = -1800f;
            reverbFilter.roomHF = -650f;
            reverbFilter.decayTime = 1.65f;
            reverbFilter.decayHFRatio = 0.48f;
            reverbFilter.reflectionsLevel = -2200f;
            reverbFilter.reflectionsDelay = 0.035f;
            reverbFilter.reverbLevel = -1250f;
            reverbFilter.reverbDelay = 0.055f;
            reverbFilter.diffusion = 78f;
            reverbFilter.density = 70f;
            reverbFilter.hfReference = 5000f;
            reverbFilter.roomLF = -450f;
            reverbFilter.lfReference = 250f;
        }

        private enum StrategyMusicMood
        {
            Calm,
            Night,
            Winter,
            Storm
        }
    }
}
