using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyFirstNightCatHuntCinematic : IStrategyInGameCinematicSequence
    {
        private const float StalkDurationSeconds = 0.92f;
        private const float PounceDurationSeconds = 0.30f;
        private const float JoyFrameRate = 8f;
        private const float JoyHoldSeconds = 0.42f;

        private readonly StrategyPopulationController population;
        private readonly CityMapController map;
        private readonly Transform actorRoot;
        private StrategyFirstNightCatHuntShotPlan shot;
        private StrategyCinematicCatActor cat;
        private StrategyCinematicRatActor mouse;
        private StrategyCinematicParticipantHighlight catHighlight;
        private StrategyCinematicParticipantHighlight mouseHighlight;
        private bool prepared;

        public StrategyFirstNightCatHuntCinematic(
            StrategyPopulationController populationController,
            CityMapController mapController,
            Transform transientActorRoot)
        {
            population = populationController;
            map = mapController;
            actorRoot = transientActorRoot;
        }

        public string DebugName => "First Night Cat Hunt";
        public StrategyFirstNightCatHuntPhase Phase { get; private set; }
        public StrategyCinematicCatActor ActiveCat => cat;
        public StrategyCinematicRatActor ActiveMouse => mouse;
        public StrategyCinematicParticipantHighlight CatHighlight => catHighlight;
        public StrategyCinematicParticipantHighlight MouseHighlight => mouseHighlight;
        public StrategyFirstNightCatHuntShotPlan Shot => shot;
        public bool IsPrepared => prepared;
        public bool HasVisibleActors => IsActorVisible(cat) || IsActorVisible(mouse);
        public bool AreBothActorsVisible => IsActorVisible(cat) && IsActorVisible(mouse);

        public bool TryPrepare(out StrategyInGameCinematicFraming framing)
        {
            // The player calls TryPrepare before it owns input/pause and may reject
            // playback afterward, so this method must never touch scene actors.
            if (cat != null || mouse != null || catHighlight != null || mouseHighlight != null)
            {
                framing = default;
                return false;
            }

            prepared = StrategyFirstNightCatHuntShotPlanner.TryCreate(population, map, out shot);
            Phase = prepared
                ? StrategyFirstNightCatHuntPhase.Prepared
                : StrategyFirstNightCatHuntPhase.Unprepared;
            if (!prepared)
            {
                framing = default;
                StrategyDebugLogger.Warn("FirstNightFauna", "CatHuntPlanUnavailable");
                return false;
            }

            Vector3 start = shot.CatStartWorld;
            Vector3 end = shot.CatchWorld;
            Vector3 center = (start + end) * 0.5f;
            Bounds bounds = new(center, new Vector3(
                Mathf.Abs(end.x - start.x),
                Mathf.Abs(end.y - start.y),
                0f));
            framing = new StrategyInGameCinematicFraming(
                bounds,
                new Vector2(0.9f, 1.15f),
                4.6f,
                7f);
            return true;
        }

        public void Begin(StrategyInGameCinematicContext context)
        {
            if (!prepared)
            {
                return;
            }

            bool faceLeft = shot.EndCell.x < shot.StartCell.x;
            cat = StrategyCinematicCatActor.Create(
                actorRoot,
                shot.CatStartWorld,
                shot.Coat,
                4);
            cat.SetAutomaticTick(false);
            cat.SetFacingLeft(faceLeft);
            cat.Pause();
            mouse = StrategyCinematicRatActor.Create(
                actorRoot,
                shot.MouseStartWorld,
                shot.MouseVariant,
                3);
            mouse.SetAutomaticTick(false);
            mouse.SetFacingLeft(faceLeft);
            mouse.Pause();
            catHighlight = StrategyCinematicParticipantHighlight.Create(
                actorRoot,
                cat.transform,
                new Vector2(0.90f, 0.32f));
            mouseHighlight = StrategyCinematicParticipantHighlight.Create(
                actorRoot,
                mouse.transform,
                new Vector2(0.68f, 0.23f),
                1.35f);
            Phase = StrategyFirstNightCatHuntPhase.Staged;
        }

        public IEnumerator Play(StrategyInGameCinematicContext context)
        {
            if (cat == null || mouse == null)
            {
                yield break;
            }

            Phase = StrategyFirstNightCatHuntPhase.Stalking;
            cat.Play(StrategyCinematicCatAnimation.Stalk, 11.5f, true);
            mouse.Play(StrategyCinematicRatAnimation.Run, 16f, true);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
            yield return AnimateApproach(context);
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            Phase = StrategyFirstNightCatHuntPhase.Pouncing;
            cat.Play(StrategyCinematicCatAnimation.Pounce, 14f, false);
            mouse.Play(StrategyCinematicRatAnimation.Escape, 13f, false);
            yield return AnimatePounce(context);
            if (context.IsCancellationRequested)
            {
                yield break;
            }

            HideCaughtMouse();
            Phase = StrategyFirstNightCatHuntPhase.Celebrating;
            cat.Play(StrategyCinematicCatAnimation.Joy, JoyFrameRate, false);
            float joyDuration = Mathf.Max(0.1f, cat.FrameCount / JoyFrameRate);
            yield return AnimateJoy(context, joyDuration);
            if (!context.IsCancellationRequested)
            {
                yield return context.WaitForSecondsUnscaled(JoyHoldSeconds);
            }

            if (!context.IsCancellationRequested)
            {
                Phase = StrategyFirstNightCatHuntPhase.Completed;
            }
        }

        public void Cleanup(
            StrategyInGameCinematicContext context,
            StrategyInGameCinematicResult result)
        {
            RemoveHighlight(catHighlight);
            catHighlight = null;
            RemoveHighlight(mouseHighlight);
            mouseHighlight = null;
            RemoveActor(cat);
            cat = null;
            RemoveActor(mouse);
            mouse = null;
            prepared = false;
            shot = default;
            Phase = StrategyFirstNightCatHuntPhase.Cleaned;
        }

        private IEnumerator AnimateApproach(StrategyInGameCinematicContext context)
        {
            float elapsed = 0f;
            while (elapsed < StalkDurationSeconds && !context.IsCancellationRequested)
            {
                yield return null;
                float delta = context.UnscaledDeltaTime;
                elapsed += delta;
                float progress = SmoothStep(elapsed / StalkDurationSeconds);
                cat.SetWorldPosition(Vector3.LerpUnclamped(
                    shot.CatStartWorld,
                    shot.CatPounceWorld,
                    progress));
                mouse.SetWorldPosition(Vector3.LerpUnclamped(
                    shot.MouseStartWorld,
                    shot.MousePounceWorld,
                    progress));
                cat.Advance(delta);
                mouse.Advance(delta);
            }
        }

        private IEnumerator AnimatePounce(StrategyInGameCinematicContext context)
        {
            float elapsed = 0f;
            while (elapsed < PounceDurationSeconds && !context.IsCancellationRequested)
            {
                yield return null;
                float delta = context.UnscaledDeltaTime;
                elapsed += delta;
                float progress = Mathf.Clamp01(elapsed / PounceDurationSeconds);
                float catProgress = 1f - (1f - progress) * (1f - progress);
                Vector3 catPosition = Vector3.LerpUnclamped(
                    shot.CatPounceWorld,
                    shot.CatchWorld,
                    catProgress);
                catPosition += Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.18f);
                cat.SetWorldPosition(catPosition);
                mouse.SetWorldPosition(Vector3.LerpUnclamped(
                    shot.MousePounceWorld,
                    shot.CatchWorld,
                    SmoothStep(progress)));
                cat.Advance(delta);
                mouse.Advance(delta);
            }
        }

        private IEnumerator AnimateJoy(
            StrategyInGameCinematicContext context,
            float durationSeconds)
        {
            float elapsed = 0f;
            Vector3 celebrationPosition = shot.CatchWorld;
            celebrationPosition.z = shot.CatStartWorld.z;
            while (elapsed < durationSeconds && !context.IsCancellationRequested)
            {
                yield return null;
                float delta = context.UnscaledDeltaTime;
                elapsed += delta;
                float hop = Mathf.Sin(Mathf.Clamp01(elapsed / durationSeconds) * Mathf.PI) * 0.08f;
                cat.SetWorldPosition(celebrationPosition + Vector3.up * hop);
                cat.Advance(delta);
            }
        }

        private void HideCaughtMouse()
        {
            mouseHighlight?.SetVisible(false);
            mouse?.SetVisible(false);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static bool IsActorVisible(Component actor)
        {
            return actor != null
                && actor.gameObject.activeInHierarchy
                && actor.TryGetComponent(out SpriteRenderer renderer)
                && renderer.enabled;
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

        private static void RemoveActor(Component actor)
        {
            if (actor == null)
            {
                return;
            }

            actor.gameObject.SetActive(false);
            DestroyTransientObject(actor.gameObject);
        }

        private static void DestroyTransientObject(GameObject transientObject)
        {
            if (transientObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(transientObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(transientObject);
            }
        }
    }
}
