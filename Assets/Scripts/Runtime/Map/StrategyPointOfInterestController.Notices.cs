using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPointOfInterestController
    {
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
                PointNotice skipped = pendingNotices.Dequeue();
                StrategyDebugLogger.Warn(
                    "PointOfInterest",
                    "NoticeUnavailable",
                    StrategyDebugLogger.F("title", skipped.Title));
                if (pendingNotices.Count <= 0)
                {
                    ReleasePauseLock();
                }

                return;
            }

            if (dialog.IsInputShieldActive || !dialog.CanOpenWithoutStacking)
            {
                return;
            }

            PointNotice notice = pendingNotices.Dequeue();
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
                    "PointOfInterest",
                    "NoticeOpenFailed",
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
            StrategyDebugLogger.Info("PointOfInterest", "NoticeAcknowledged");
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

            timeScale.PushPauseLock(PauseReason);
            pauseHeld = true;
        }

        private void ReleasePauseLock()
        {
            if (!pauseHeld)
            {
                return;
            }

            if (timeScale != null)
            {
                timeScale.PopPauseLock(PauseReason);
            }

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

        private readonly struct PointNotice
        {
            public PointNotice(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; }
            public string Body { get; }
        }
    }
}
