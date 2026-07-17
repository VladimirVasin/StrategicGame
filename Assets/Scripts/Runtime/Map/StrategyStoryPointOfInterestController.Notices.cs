using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStoryPointOfInterestController
    {
        private const string StoryPauseReason = "StoryPointOfInterestNotice";

        private void Update()
        {
            if (noticeOpen
                && Time.frameCount > noticeOpenedFrame
                && (dialog == null || !dialog.IsOpen))
            {
                HandleNoticeAcknowledged(activeNoticeToken);
            }

            TryShowNextNotice();
        }

        private void TryShowNextNotice()
        {
            if (noticeOpen || pendingNotices.Count <= 0)
            {
                return;
            }

            if (timeScale != null && timeScale.IsPausedByLock && !pauseHeld)
            {
                return;
            }

            if (dialog == null)
            {
                StoryNotice skipped = pendingNotices.Dequeue();
                StrategyDebugLogger.Warn(
                    "StoryPointOfInterest",
                    "NoticeUnavailable",
                    StrategyDebugLogger.F("definitionId", skipped.DefinitionId));
                return;
            }

            if (dialog.IsInputShieldActive || !dialog.CanOpenWithoutStacking)
            {
                return;
            }

            StoryNotice notice = pendingNotices.Dequeue();
            noticeOpen = true;
            noticeOpenedFrame = Time.frameCount;
            int token = ++activeNoticeToken;
            PushPauseLock();
            try
            {
                dialog.Show(notice.Title, notice.Body, () => HandleNoticeAcknowledged(token));
            }
            catch (Exception exception)
            {
                noticeOpen = false;
                dialog.Dismiss();
                StrategyDebugLogger.Warn(
                    "StoryPointOfInterest",
                    "NoticeOpenFailed",
                    StrategyDebugLogger.F("definitionId", notice.DefinitionId),
                    StrategyDebugLogger.F("error", exception.Message));
                if (pendingNotices.Count <= 0)
                {
                    ReleasePauseLock();
                }
            }
        }

        private void HandleNoticeAcknowledged(int token)
        {
            if (!noticeOpen || token != activeNoticeToken)
            {
                return;
            }

            noticeOpen = false;
            if (pendingNotices.Count > 0)
            {
                TryShowNextNotice();
            }

            if (!noticeOpen && pendingNotices.Count <= 0)
            {
                ReleasePauseLock();
            }
        }

        private void PushPauseLock()
        {
            if (pauseHeld || timeScale == null)
            {
                return;
            }

            timeScale.PushPauseLock(StoryPauseReason);
            pauseHeld = true;
        }

        private void ReleasePauseLock()
        {
            if (!pauseHeld)
            {
                return;
            }

            timeScale?.PopPauseLock(StoryPauseReason);
            pauseHeld = false;
        }

        private void CancelPendingNotices()
        {
            pendingNotices.Clear();
            noticeOpen = false;
            activeNoticeToken++;
            dialog?.Dismiss();
            ReleasePauseLock();
        }
    }
}
