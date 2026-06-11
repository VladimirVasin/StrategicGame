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

        private AudioSource musicSource;
        private AudioReverbFilter reverbFilter;
        private AudioClip[] playlist = Array.Empty<AudioClip>();
        private int currentTrackIndex = -1;
        private bool configured;
        private bool windowFocused = true;
        private bool applicationPaused;
        private bool hasAudioFocus = true;
        private bool pausedForFocusLoss;

        public void Configure()
        {
            playlist = Resources.LoadAll<AudioClip>(MusicFolderPath);
            if (playlist.Length > 1)
            {
                Array.Sort(playlist, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            }

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
                PlayNextTrack();
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
                PlayNextTrack();
                return;
            }

            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                TargetVolume,
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

        private void PlayNextTrack()
        {
            int nextTrackIndex = PickNextTrackIndex();
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
                StrategyDebugLogger.F("trackCount", playlist.Length));
        }

        private int PickNextTrackIndex()
        {
            if (playlist.Length <= 0)
            {
                return -1;
            }

            if (playlist.Length == 1 || currentTrackIndex < 0)
            {
                return UnityEngine.Random.Range(0, playlist.Length);
            }

            int randomIndex = UnityEngine.Random.Range(0, playlist.Length - 1);
            if (randomIndex >= currentTrackIndex)
            {
                randomIndex++;
            }

            return randomIndex;
        }

        private void EnsureMusicSource()
        {
            if (musicSource != null)
            {
                EnsureReverbFilter();
                return;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
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
            if (reverbFilter == null)
            {
                reverbFilter = GetComponent<AudioReverbFilter>();
            }

            if (reverbFilter == null)
            {
                reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
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
    }
}
