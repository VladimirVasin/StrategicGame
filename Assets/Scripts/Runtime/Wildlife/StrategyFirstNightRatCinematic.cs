using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyFirstNightRatCinematic : IStrategyInGameCinematicSequence
    {
        private const float DashDurationSeconds = 1.35f;
        private const float StartleDelaySeconds = 0.34f;
        private const float StartleFrameRate = 11f;
        private const float EscapeStartProgress = 0.72f;
        private const float SettleHoldSeconds = 0.16f;
        private const float MaximumBackstep = 0.14f;

        private readonly StrategyPopulationController population;
        private readonly CityMapController map;
        private readonly Transform actorRoot;
        private StrategyFirstNightRatShotPlan shot;
        private StrategyResidentCinematicVisualOverride residentOverride;
        private StrategyCinematicRatActor rat;
        private StrategyCinematicParticipantHighlight residentHighlight;
        private StrategyCinematicParticipantHighlight ratHighlight;
        private bool prepared;

        public StrategyFirstNightRatCinematic(
            StrategyPopulationController populationController,
            CityMapController mapController,
            Transform transientActorRoot)
        {
            population = populationController;
            map = mapController;
            actorRoot = transientActorRoot;
        }

        public string DebugName => "First Night Rat Scare";
        public StrategyResidentAgent Resident => prepared ? shot.Resident : null;
        public StrategyCinematicRatActor ActiveRat => rat;
        public StrategyCinematicParticipantHighlight ResidentHighlight => residentHighlight;
        public StrategyCinematicParticipantHighlight RatHighlight => ratHighlight;

        public bool TryPrepare(out StrategyInGameCinematicFraming framing)
        {
            CleanupTransientActors();
            float viewportAspect = Screen.height > 0
                ? Screen.width / (float)Screen.height
                : 16f / 9f;
            prepared = StrategyFirstNightRatShotPlanner.TryCreate(
                population,
                map,
                viewportAspect,
                out shot);
            if (!prepared)
            {
                framing = default;
                StrategyDebugLogger.Warn("FirstNightFauna", "RatCinematicPlanUnavailable");
                return false;
            }

            framing = new StrategyInGameCinematicFraming(
                new Bounds(shot.FocusCenter, Vector3.zero),
                Vector2.zero,
                shot.FocusOrthographicSize,
                shot.FocusOrthographicSize);
            return true;
        }

        public void Begin(StrategyInGameCinematicContext context)
        {
            if (!prepared
                || shot.Resident == null
                || !shot.Resident.TryBeginCinematicVisualOverride(out residentOverride))
            {
                StrategyDebugLogger.Warn("FirstNightFauna", "RatCinematicActorUnavailable");
                return;
            }

            residentOverride.SetTransformPose(
                shot.ResidentWorld,
                Quaternion.identity,
                Vector3.one);
            bool initiallyFaceLeft = shot.StartWorld.x < shot.ResidentWorld.x;
            residentOverride.ApplyPose(
                StrategyResidentVisualPose.MouseStartle,
                0,
                initiallyFaceLeft);

            rat = StrategyCinematicRatActor.Create(
                actorRoot,
                shot.StartWorld,
                shot.RatVariant,
                3);
            rat.SetAutomaticTick(false);
            rat.SetFacingLeft(shot.EndWorld.x < shot.StartWorld.x);
            rat.Pause();
            residentHighlight = StrategyCinematicParticipantHighlight.Create(
                actorRoot,
                shot.Resident.transform,
                new Vector2(0.92f, 0.34f));
            ratHighlight = StrategyCinematicParticipantHighlight.Create(
                actorRoot,
                rat.transform,
                new Vector2(0.72f, 0.24f),
                1.35f);
        }

        public IEnumerator Play(StrategyInGameCinematicContext context)
        {
            if (residentOverride == null || !residentOverride.IsActive || rat == null)
            {
                yield break;
            }

            rat.Play(StrategyCinematicRatAnimation.Run, 17f, true);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);

            float elapsed = 0f;
            bool escapeStarted = false;
            Vector3 travelDirection = (shot.EndWorld - shot.StartWorld).normalized;
            while (elapsed < DashDurationSeconds && !context.IsCancellationRequested)
            {
                yield return null;
                float delta = context.UnscaledDeltaTime;
                elapsed += delta;
                float progress = Mathf.Clamp01(elapsed / DashDurationSeconds);
                Vector3 ratPosition = EvaluateRatPosition(progress);
                rat.SetWorldPosition(ratPosition);
                rat.Advance(delta);

                if (!escapeStarted && progress >= EscapeStartProgress)
                {
                    escapeStarted = true;
                    rat.Play(StrategyCinematicRatAnimation.Escape, 13f, false);
                }

                if (elapsed >= StartleDelaySeconds)
                {
                    ApplyResidentStartle(elapsed - StartleDelaySeconds, travelDirection, ratPosition);
                }
            }

            if (!context.IsCancellationRequested)
            {
                residentOverride.ApplyPose(
                    StrategyResidentVisualPose.MouseStartle,
                    StrategyResidentSpriteFactory.MouseStartleFrameCount - 1,
                    shot.EndWorld.x < shot.ResidentWorld.x);
                residentOverride.SetTransformPose(
                    shot.ResidentWorld,
                    Quaternion.identity,
                    Vector3.one);
                yield return context.WaitForSecondsUnscaled(SettleHoldSeconds);
            }
        }

        public void Cleanup(
            StrategyInGameCinematicContext context,
            StrategyInGameCinematicResult result)
        {
            CleanupTransientActors();
            prepared = false;
            shot = default;
        }

        private Vector3 EvaluateRatPosition(float progress)
        {
            const float PassProgress = 0.58f;
            if (progress <= PassProgress)
            {
                float approach = Mathf.Clamp01(progress / PassProgress);
                return Vector3.LerpUnclamped(
                    shot.StartWorld,
                    shot.PassWorld,
                    EaseIn(approach));
            }

            float escape = Mathf.Clamp01((progress - PassProgress) / (1f - PassProgress));
            return Vector3.LerpUnclamped(
                shot.PassWorld,
                shot.EndWorld,
                EaseOut(escape));
        }

        private void ApplyResidentStartle(
            float startleElapsed,
            Vector3 travelDirection,
            Vector3 ratPosition)
        {
            if (residentOverride == null || !residentOverride.IsActive)
            {
                return;
            }

            int frame = Mathf.Clamp(
                Mathf.FloorToInt(startleElapsed * StartleFrameRate),
                0,
                StrategyResidentSpriteFactory.MouseStartleFrameCount - 1);
            bool faceLeft = ratPosition.x < shot.ResidentWorld.x;
            residentOverride.ApplyPose(
                StrategyResidentVisualPose.MouseStartle,
                frame,
                faceLeft);

            float reactionDuration = StrategyResidentSpriteFactory.MouseStartleFrameCount
                / StartleFrameRate;
            float reactionProgress = Mathf.Clamp01(startleElapsed / reactionDuration);
            float backstep = Mathf.Sin(reactionProgress * Mathf.PI) * MaximumBackstep;
            Vector3 stagedPosition = shot.ResidentWorld + travelDirection * backstep;
            residentOverride.SetTransformPose(
                stagedPosition,
                Quaternion.identity,
                Vector3.one);
        }

        private void CleanupTransientActors()
        {
            RemoveHighlight(residentHighlight);
            residentHighlight = null;
            RemoveHighlight(ratHighlight);
            ratHighlight = null;
            residentOverride?.Dispose();
            residentOverride = null;
            if (rat != null)
            {
                rat.SetVisible(false);
                DestroyTransientObject(rat.gameObject);
                rat = null;
            }
        }

        private static void RemoveHighlight(StrategyCinematicParticipantHighlight highlight)
        {
            if (highlight == null)
            {
                return;
            }

            highlight.SetVisible(false);
            DestroyTransientObject(highlight.gameObject);
        }

        private static void DestroyTransientObject(GameObject transientObject)
        {
            if (transientObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(transientObject);
            }
            else
            {
                Object.DestroyImmediate(transientObject);
            }
        }

        private static float EaseIn(float progress)
        {
            return progress * progress;
        }

        private static float EaseOut(float progress)
        {
            float inverse = 1f - progress;
            return 1f - inverse * inverse;
        }
    }
}
