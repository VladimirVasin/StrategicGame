using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private StrategyResidentCinematicVisualOverride cinematicVisualOverride;
        private CinematicVisualSnapshot cinematicVisualSnapshot;

        public bool IsCinematicVisualOverrideActive => cinematicVisualOverride != null;

        public bool CanBeTemporarilyRevealedForCinematic =>
            CanBeginCinematicVisualOverride
            && !IsOnScoutExpedition;

        private bool CanBeginCinematicVisualOverride =>
            IsAdult
            && isActiveAndEnabled
            && gameObject.activeInHierarchy
            && !deathRequested
            && !IsPendingRefugee
            && !IsRefugeeTraveling
            && !IsFuneralDutyActive
            && cinematicVisualOverride == null
            && spriteRenderer != null;

        public bool CanParticipateInCinematic =>
            CanBeTemporarilyRevealedForCinematic
            && !hiddenInsideHome
            && !hiddenUnderground
            && !sleepingInsideHome
            && !IsSleepingAtCampfire
            && spriteRenderer.enabled
            && spriteRenderer.gameObject.activeInHierarchy;

        public bool TryBeginCinematicVisualOverride(
            out StrategyResidentCinematicVisualOverride visualOverride)
        {
            visualOverride = null;
            if (!CanBeTemporarilyRevealedForCinematic)
            {
                return false;
            }

            return BeginCinematicVisualOverride(out visualOverride);
        }

        internal bool TryBeginCommittedStoryCinematicVisualOverride(
            out StrategyResidentCinematicVisualOverride visualOverride)
        {
            visualOverride = null;
            if (!CanBeginCinematicVisualOverride
                || !IsOnScoutExpedition
                || !HasCommittedStoryPointOfInterest
                || hiddenInsideHome
                || hiddenUnderground
                || sleepingInsideHome
                || IsSleepingAtCampfire
                || !spriteRenderer.enabled
                || !spriteRenderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            return BeginCinematicVisualOverride(out visualOverride);
        }

        private bool BeginCinematicVisualOverride(
            out StrategyResidentCinematicVisualOverride visualOverride)
        {
            visualOverride = null;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            CinematicRendererSnapshot[] rendererSnapshots =
                new CinematicRendererSnapshot[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                rendererSnapshots[i] = new CinematicRendererSnapshot(renderers[i], transform);
            }

            cinematicVisualSnapshot = new CinematicVisualSnapshot(
                transform.position,
                transform.localRotation,
                transform.localScale,
                rendererSnapshots);
            visualOverride = new StrategyResidentCinematicVisualOverride(this);
            cinematicVisualOverride = visualOverride;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null
                    && renderer != spriteRenderer
                    && renderer != outlineRenderer
                    && renderer != shadowRenderer)
                {
                    renderer.enabled = false;
                }
            }

            spriteRenderer.enabled = true;
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = true;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = true;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            return true;
        }

        internal bool OwnsCinematicVisualOverride(
            StrategyResidentCinematicVisualOverride visualOverride)
        {
            return visualOverride != null && cinematicVisualOverride == visualOverride;
        }

        internal bool TryApplyCinematicVisualPose(
            StrategyResidentCinematicVisualOverride visualOverride,
            StrategyResidentVisualPose pose,
            int frame,
            bool flipX)
        {
            if (!OwnsCinematicVisualOverride(visualOverride) || spriteRenderer == null)
            {
                return false;
            }

            Sprite sprite = StrategyVisualBakeSource.GetResidentSprite(
                gender,
                VisualVariant,
                lifeStage,
                pose,
                frame);
            if (sprite == null)
            {
                return false;
            }

            spriteRenderer.enabled = true;
            spriteRenderer.sprite = sprite;
            spriteRenderer.flipX = flipX;
            SyncReadabilityRenderers();
            return true;
        }

        internal bool TryApplyCinematicTransformPose(
            StrategyResidentCinematicVisualOverride visualOverride,
            Vector3 worldPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            if (!OwnsCinematicVisualOverride(visualOverride))
            {
                return false;
            }

            transform.position = worldPosition;
            transform.localRotation = localRotation;
            transform.localScale = localScale;
            return true;
        }

        internal void EndCinematicVisualOverride(
            StrategyResidentCinematicVisualOverride visualOverride)
        {
            if (!OwnsCinematicVisualOverride(visualOverride))
            {
                return;
            }

            cinematicVisualOverride = null;
            RestoreCinematicVisualSnapshot();
            visualOverride.Invalidate();
        }

        private void AbortCinematicVisualOverride()
        {
            StrategyResidentCinematicVisualOverride visualOverride = cinematicVisualOverride;
            if (visualOverride == null)
            {
                return;
            }

            cinematicVisualOverride = null;
            RestoreCinematicVisualSnapshot();
            visualOverride.Invalidate();
        }

        private void RestoreCinematicVisualSnapshot()
        {
            CinematicVisualSnapshot snapshot = cinematicVisualSnapshot;
            cinematicVisualSnapshot = null;
            if (snapshot == null)
            {
                return;
            }

            transform.position = snapshot.WorldPosition;
            transform.localRotation = snapshot.LocalRotation;
            transform.localScale = snapshot.LocalScale;
            for (int i = 0; i < snapshot.Renderers.Length; i++)
            {
                snapshot.Renderers[i].Restore(transform);
            }
        }

        private sealed class CinematicVisualSnapshot
        {
            public CinematicVisualSnapshot(
                Vector3 worldPosition,
                Quaternion localRotation,
                Vector3 localScale,
                CinematicRendererSnapshot[] renderers)
            {
                WorldPosition = worldPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                Renderers = renderers;
            }

            public Vector3 WorldPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public CinematicRendererSnapshot[] Renderers { get; }
        }

        private readonly struct CinematicRendererSnapshot
        {
            private readonly Renderer renderer;
            private readonly bool enabled;
            private readonly Sprite sprite;
            private readonly bool flipX;
            private readonly bool flipY;
            private readonly Color color;
            private readonly int sortingLayerId;
            private readonly int sortingOrder;
            private readonly bool sharesActorTransform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public CinematicRendererSnapshot(Renderer renderer, Transform actorTransform)
            {
                this.renderer = renderer;
                SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
                enabled = renderer != null && renderer.enabled;
                sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
                flipX = spriteRenderer != null && spriteRenderer.flipX;
                flipY = spriteRenderer != null && spriteRenderer.flipY;
                color = spriteRenderer != null ? spriteRenderer.color : Color.white;
                sortingLayerId = renderer != null ? renderer.sortingLayerID : 0;
                sortingOrder = renderer != null ? renderer.sortingOrder : 0;
                sharesActorTransform = renderer != null && renderer.transform == actorTransform;
                localPosition = renderer != null ? renderer.transform.localPosition : Vector3.zero;
                localRotation = renderer != null ? renderer.transform.localRotation : Quaternion.identity;
                localScale = renderer != null ? renderer.transform.localScale : Vector3.one;
            }

            public void Restore(Transform actorTransform)
            {
                if (renderer == null)
                {
                    return;
                }

                if (renderer is SpriteRenderer spriteRenderer)
                {
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.flipX = flipX;
                    spriteRenderer.flipY = flipY;
                    spriteRenderer.color = color;
                }

                renderer.sortingLayerID = sortingLayerId;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = enabled;
                if (!sharesActorTransform && renderer.transform != actorTransform)
                {
                    renderer.transform.localPosition = localPosition;
                    renderer.transform.localRotation = localRotation;
                    renderer.transform.localScale = localScale;
                }
            }
        }
    }
}
