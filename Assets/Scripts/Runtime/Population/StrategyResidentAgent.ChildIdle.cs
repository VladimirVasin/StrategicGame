using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private ChildPlayKind PickChildSoloAmbientKind()
        {
            float roll = Random.value;
            if (DisplayAgeYears <= 5)
            {
                return roll < 0.55f ? ChildPlayKind.Stick : ChildPlayKind.Sit;
            }

            if (DisplayAgeYears >= 13)
            {
                return roll < 0.48f ? ChildPlayKind.Watch
                    : roll < 0.78f ? ChildPlayKind.Sit
                    : ChildPlayKind.Stick;
            }

            return roll < 0.44f ? ChildPlayKind.Stick
                : roll < 0.70f ? ChildPlayKind.Watch
                : roll < 0.88f ? ChildPlayKind.Sit
                : ChildPlayKind.Solo;
        }

        private void AnimateChildIdleActivity()
        {
            UseIdleSprite();
            footstepAudio?.ResetStepPhase();
            float time = Time.time + bobPhase;
            switch (activity)
            {
                case ResidentActivity.PlayingWithStick:
                    float scratch = Mathf.Sin(time * 7.5f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, scratch * 4.5f);
                    transform.localScale = new Vector3(1.04f, 0.82f + Mathf.Abs(scratch) * 0.05f, 1f);
                    break;
                case ResidentActivity.SittingNearHome:
                    float seatedBreath = 1f + Mathf.Sin(time * 3.2f) * 0.018f;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(1.08f, 0.70f * seatedBreath, 1f);
                    break;
                case ResidentActivity.WatchingActivity:
                    float look = Mathf.Sin(time * 1.15f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, look * 1.8f);
                    transform.localScale = new Vector3(1f, 1f + Mathf.Sin(time * 3.5f) * 0.018f, 1f);
                    if (spriteRenderer != null && Mathf.Abs(look) > 0.92f)
                    {
                        spriteRenderer.flipX = look < 0f;
                    }
                    break;
                case ResidentActivity.PlayingWithChild:
                    float talk = Mathf.Sin(time * 4.2f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, talk * 2.4f);
                    transform.localScale = new Vector3(1f + Mathf.Abs(talk) * 0.025f, 1f - Mathf.Abs(talk) * 0.018f, 1f);
                    break;
                default:
                    AnimateIdle();
                    return;
            }

            SyncReadabilityRenderers();
        }

        private float GetChildPairPlayChance()
        {
            return ageYears >= 7f ? 0.68f : 0.42f;
        }

        private bool IsSiblingOf(StrategyResidentAgent other)
        {
            return other != null
                && ((fatherId != 0 && fatherId == other.fatherId)
                    || (motherId != 0 && motherId == other.motherId));
        }

        private static int GetChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private readonly struct ChildPlayCandidate
        {
            public ChildPlayCandidate(
                StrategyResidentAgent resident,
                Vector2Int selfCell,
                Vector2Int partnerCell,
                int score)
            {
                Resident = resident;
                SelfCell = selfCell;
                PartnerCell = partnerCell;
                Score = score;
            }

            public StrategyResidentAgent Resident { get; }
            public Vector2Int SelfCell { get; }
            public Vector2Int PartnerCell { get; }
            public int Score { get; }
        }
    }
}
