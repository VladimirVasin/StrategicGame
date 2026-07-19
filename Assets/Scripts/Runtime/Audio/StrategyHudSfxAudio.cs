using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyHudSfxKind
    {
        Click,
        Open,
        Close,
        Select,
        Deny,
        Confirm,
        Cancel,
        Step,
        Notify,
        Hover
    }

    [DisallowMultipleComponent]
    public sealed class StrategyHudSfxAudio : MonoBehaviour
    {
        private const string ClickPath = "Audio/HudSfx/HudClick";
        private const string OpenPath = "Audio/HudSfx/HudOpen";
        private const string ClosePath = "Audio/HudSfx/HudClose";
        private const string SelectPath = "Audio/HudSfx/HudSelect";
        private const string DenyPath = "Audio/HudSfx/HudDeny";
        private const string ConfirmPath = "Audio/HudSfx/HudConfirm";
        private const string CancelPath = "Audio/HudSfx/HudCancel";
        private const string StepPath = "Audio/HudSfx/HudStep";
        private const string NotifyPath = "Audio/HudSfx/HudNotify";

        private static StrategyHudSfxAudio instance;
        private static AudioClip[] clickClips;
        private static AudioClip[] openClips;
        private static AudioClip[] closeClips;
        private static AudioClip[] selectClips;
        private static AudioClip[] denyClips;
        private static AudioClip[] confirmClips;
        private static AudioClip[] cancelClips;
        private static AudioClip[] stepClips;
        private static AudioClip[] notifyClips;
        private static bool clipsLoaded;

        private AudioSource source;
        private int clipCursor;
        private float nextAnyTime;
        private float nextClickTime;
        private float nextOpenTime;
        private float nextCloseTime;
        private float nextSelectTime;
        private float nextDenyTime;
        private float nextConfirmTime;
        private float nextCancelTime;
        private float nextStepTime;
        private float nextNotifyTime;
        private float nextHoverTime;

        public static void Play(StrategyHudSfxKind kind, float volumeScale = 1f)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            StrategyHudSfxAudio audio = EnsureInstance();
            audio?.PlayInternal(kind, volumeScale);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSource();
            EnsureClipsLoaded();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private static StrategyHudSfxAudio EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<StrategyHudSfxAudio>();
            if (instance == null)
            {
                GameObject audioObject = new GameObject("Strategy HUD SFX Audio");
                DontDestroyOnLoad(audioObject);
                instance = audioObject.AddComponent<StrategyHudSfxAudio>();
            }

            instance.EnsureSource();
            EnsureClipsLoaded();
            return instance;
        }

        private void PlayInternal(StrategyHudSfxKind kind, float volumeScale)
        {
            EnsureClipsLoaded();
            AudioClip[] clips = GetClips(kind);
            if (clips == null || clips.Length <= 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            bool isHover = kind == StrategyHudSfxKind.Hover;
            if (now < nextAnyTime || now < GetNextAllowedTime(kind))
            {
                return;
            }

            EnsureSource();
            if (source == null)
            {
                return;
            }

            SetNextAllowedTime(kind, now + GetCooldown(kind));
            if (!isHover)
            {
                nextAnyTime = now + 0.012f;
            }

            int clipIndex = Mathf.Abs(((int)kind + 1) * 23 + clipCursor * 7) % clips.Length;
            clipCursor++;
            source.pitch = UnityEngine.Random.Range(GetPitchMin(kind), GetPitchMax(kind));
            source.PlayOneShot(
                clips[clipIndex],
                GetVolume(kind)
                    * Mathf.Clamp(volumeScale, 0f, 2f)
                    * StrategyAudioMixController.GetVolume(StrategyAudioBus.Hud));
        }

        private static void EnsureClipsLoaded()
        {
            if (clipsLoaded)
            {
                return;
            }

            clipsLoaded = true;
            clickClips = LoadSorted(ClickPath);
            openClips = LoadSorted(OpenPath);
            closeClips = LoadSorted(ClosePath);
            selectClips = LoadSorted(SelectPath);
            denyClips = LoadSorted(DenyPath);
            confirmClips = LoadSorted(ConfirmPath);
            cancelClips = LoadSorted(CancelPath);
            stepClips = LoadSorted(StepPath);
            notifyClips = LoadSorted(NotifyPath);
        }

        private static AudioClip[] LoadSorted(string path)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>(path);
            if (clips != null && clips.Length > 1)
            {
                Array.Sort(clips, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            }

            return clips;
        }

        private static AudioClip[] GetClips(StrategyHudSfxKind kind)
        {
            return kind switch
            {
                StrategyHudSfxKind.Click => clickClips,
                StrategyHudSfxKind.Open => openClips,
                StrategyHudSfxKind.Close => closeClips,
                StrategyHudSfxKind.Select => selectClips,
                StrategyHudSfxKind.Deny => denyClips,
                StrategyHudSfxKind.Confirm => confirmClips,
                StrategyHudSfxKind.Cancel => cancelClips,
                StrategyHudSfxKind.Step => stepClips,
                StrategyHudSfxKind.Notify => notifyClips,
                StrategyHudSfxKind.Hover => stepClips,
                _ => clickClips
            };
        }

        private static float GetCooldown(StrategyHudSfxKind kind)
        {
            return kind switch
            {
                StrategyHudSfxKind.Deny => 0.08f,
                StrategyHudSfxKind.Notify => 0.18f,
                StrategyHudSfxKind.Open => 0.05f,
                StrategyHudSfxKind.Close => 0.05f,
                StrategyHudSfxKind.Hover => 0.075f,
                _ => 0.025f
            };
        }

        private static float GetVolume(StrategyHudSfxKind kind)
        {
            return kind switch
            {
                StrategyHudSfxKind.Step => 0.12f,
                StrategyHudSfxKind.Hover => 0.065f,
                StrategyHudSfxKind.Click => 0.14f,
                StrategyHudSfxKind.Close => 0.15f,
                StrategyHudSfxKind.Cancel => 0.15f,
                StrategyHudSfxKind.Deny => 0.17f,
                StrategyHudSfxKind.Confirm => 0.18f,
                StrategyHudSfxKind.Notify => 0.18f,
                _ => 0.16f
            };
        }

        private static float GetPitchMin(StrategyHudSfxKind kind)
        {
            if (kind == StrategyHudSfxKind.Hover)
            {
                return 1.01f;
            }

            return kind == StrategyHudSfxKind.Deny ? 0.94f : 0.97f;
        }

        private static float GetPitchMax(StrategyHudSfxKind kind)
        {
            if (kind == StrategyHudSfxKind.Hover)
            {
                return 1.08f;
            }

            return kind == StrategyHudSfxKind.Confirm ? 1.04f : 1.06f;
        }

        private float GetNextAllowedTime(StrategyHudSfxKind kind)
        {
            return kind switch
            {
                StrategyHudSfxKind.Click => nextClickTime,
                StrategyHudSfxKind.Open => nextOpenTime,
                StrategyHudSfxKind.Close => nextCloseTime,
                StrategyHudSfxKind.Select => nextSelectTime,
                StrategyHudSfxKind.Deny => nextDenyTime,
                StrategyHudSfxKind.Confirm => nextConfirmTime,
                StrategyHudSfxKind.Cancel => nextCancelTime,
                StrategyHudSfxKind.Step => nextStepTime,
                StrategyHudSfxKind.Notify => nextNotifyTime,
                StrategyHudSfxKind.Hover => nextHoverTime,
                _ => nextClickTime
            };
        }

        private void SetNextAllowedTime(StrategyHudSfxKind kind, float value)
        {
            switch (kind)
            {
                case StrategyHudSfxKind.Click:
                    nextClickTime = value;
                    break;
                case StrategyHudSfxKind.Open:
                    nextOpenTime = value;
                    break;
                case StrategyHudSfxKind.Close:
                    nextCloseTime = value;
                    break;
                case StrategyHudSfxKind.Select:
                    nextSelectTime = value;
                    break;
                case StrategyHudSfxKind.Deny:
                    nextDenyTime = value;
                    break;
                case StrategyHudSfxKind.Confirm:
                    nextConfirmTime = value;
                    break;
                case StrategyHudSfxKind.Cancel:
                    nextCancelTime = value;
                    break;
                case StrategyHudSfxKind.Step:
                    nextStepTime = value;
                    break;
                case StrategyHudSfxKind.Notify:
                    nextNotifyTime = value;
                    break;
                case StrategyHudSfxKind.Hover:
                    nextHoverTime = value;
                    break;
            }
        }

        private void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            source = StrategyAudioMixController.CreateRuntimeSource(transform, "HUD SFX Source", StrategyAudioBus.Hud);
            source.loop = false;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.ignoreListenerPause = true;
            source.priority = 64;
        }
    }
}
