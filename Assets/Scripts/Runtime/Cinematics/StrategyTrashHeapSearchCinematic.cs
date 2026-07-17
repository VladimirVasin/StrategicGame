using System;
using System.Collections;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed class StrategyTrashHeapSearchCinematic : IStrategyInGameCinematicSequence
    {
        public const float SearchDurationSeconds = 5f;
        private const float SearchFrameRate = 8f;
        private const float RevealDurationSeconds = 0.72f;

        private readonly StrategyResidentAgent resident;
        private readonly StrategyStoryPointOfInterestAnchor anchor;
        private readonly Transform actorRoot;
        private readonly Sprite spoonSprite;
        private StrategyResidentCinematicVisualOverride residentOverride;
        private GameObject spoonObject;
        private SpriteRenderer spoonRenderer;
        private Vector3 residentWorld;
        private Vector3 anchorWorld;
        private Vector3 anchorScale;
        private Quaternion anchorRotation;
        private bool faceLeft;
        private bool prepared;
        private bool visualSnapshotCaptured;

        public StrategyTrashHeapSearchCinematic(
            StrategyResidentAgent scout,
            StrategyStoryPointOfInterestAnchor storyAnchor,
            Transform transientActorRoot,
            Sprite rewardSprite)
        {
            resident = scout;
            anchor = storyAnchor;
            actorRoot = transientActorRoot;
            spoonSprite = rewardSprite;
        }

        public string DebugName => "Trash Heap Search";

        public bool TryPrepare(out StrategyInGameCinematicFraming framing)
        {
            Cleanup(null, StrategyInGameCinematicResult.Cancelled);
            if (resident == null
                || anchor == null
                || spoonSprite == null
                || !anchor.IsCommittedTo(resident)
                || anchor.VisualRenderer == null
                || !anchor.VisualRenderer.enabled)
            {
                framing = default;
                return false;
            }

            residentWorld = resident.transform.position;
            anchorWorld = anchor.transform.position;
            anchorScale = anchor.transform.localScale;
            anchorRotation = anchor.transform.localRotation;
            visualSnapshotCaptured = true;
            faceLeft = anchorWorld.x < residentWorld.x;
            Vector3 center = (residentWorld + anchorWorld) * 0.5f;
            center.z = 0f;
            Bounds bounds = new(center, new Vector3(
                Mathf.Max(2.6f, Mathf.Abs(residentWorld.x - anchorWorld.x) + 2.2f),
                2.4f,
                0f));
            framing = new StrategyInGameCinematicFraming(
                bounds,
                new Vector2(0.45f, 0.35f),
                3.2f,
                4.2f);
            prepared = true;
            return true;
        }

        public void Begin(StrategyInGameCinematicContext context)
        {
            if (!prepared
                || resident == null
                || !resident.TryBeginCommittedStoryCinematicVisualOverride(out residentOverride))
            {
                throw new InvalidOperationException("The committed Scout is unavailable for the trash search cinematic.");
            }

            residentOverride.SetTransformPose(residentWorld, Quaternion.identity, Vector3.one);
            residentOverride.ApplyPose(StrategyResidentVisualPose.TrashSearch, 0, faceLeft);
            CreateSpoonActor();
        }

        public IEnumerator Play(StrategyInGameCinematicContext context)
        {
            if (residentOverride == null || !residentOverride.IsActive)
            {
                yield break;
            }

            float elapsed = 0f;
            int lastFrame = -1;
            while (elapsed < SearchDurationSeconds && !context.IsCancellationRequested)
            {
                yield return null;
                elapsed += context.UnscaledDeltaTime;
                int frame = Mathf.FloorToInt(elapsed * SearchFrameRate) % 8;
                if (frame != lastFrame)
                {
                    residentOverride.ApplyPose(
                        StrategyResidentVisualPose.TrashSearch,
                        frame,
                        faceLeft);
                    lastFrame = frame;
                    if (frame == 1 || frame == 5)
                    {
                        StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
                    }
                }

                float wobble = Mathf.Sin(elapsed * 9.4f) * 1.35f;
                float squash = 1f + Mathf.Sin(elapsed * 6.2f) * 0.018f;
                anchor.transform.localRotation = anchorRotation * Quaternion.Euler(0f, 0f, wobble);
                anchor.transform.localScale = new Vector3(
                    anchorScale.x * squash,
                    anchorScale.y / squash,
                    anchorScale.z);
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            for (int frame = 8; frame < StrategyResidentSpriteFactory.TrashSearchFrameCount; frame++)
            {
                residentOverride.ApplyPose(
                    StrategyResidentVisualPose.TrashSearch,
                    frame,
                    faceLeft);
                float frameElapsed = 0f;
                while (frameElapsed < 0.10f && !context.IsCancellationRequested)
                {
                    yield return null;
                    frameElapsed += context.UnscaledDeltaTime;
                }
            }

            if (context.IsCancellationRequested)
            {
                yield break;
            }

            spoonObject.SetActive(true);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            float revealElapsed = 0f;
            while (revealElapsed < RevealDurationSeconds && !context.IsCancellationRequested)
            {
                yield return null;
                revealElapsed += context.UnscaledDeltaTime;
                float progress = Mathf.Clamp01(revealElapsed / RevealDurationSeconds);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                Vector3 from = residentWorld + new Vector3(faceLeft ? -0.16f : 0.16f, 0.55f, -0.20f);
                Vector3 to = residentWorld + new Vector3(faceLeft ? -0.56f : 0.56f, 1.18f, -0.20f);
                spoonObject.transform.position = Vector3.Lerp(from, to, eased);
                float scale = Mathf.Lerp(0.02f, 0.24f, eased);
                spoonObject.transform.localScale = new Vector3(scale, scale, 1f);
                spoonObject.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(faceLeft ? 28f : -28f, faceLeft ? -12f : 12f, eased));
                spoonRenderer.color = new Color(1f, 1f, 1f, eased);
            }

            yield return context.WaitForSecondsUnscaled(0.32f);
        }

        public void Cleanup(
            StrategyInGameCinematicContext context,
            StrategyInGameCinematicResult result)
        {
            residentOverride?.Dispose();
            residentOverride = null;
            if (anchor != null && visualSnapshotCaptured)
            {
                anchor.transform.localScale = anchorScale;
                anchor.transform.localRotation = anchorRotation;
            }

            if (spoonObject != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(spoonObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(spoonObject);
                }
            }

            spoonObject = null;
            spoonRenderer = null;
            prepared = false;
            visualSnapshotCaptured = false;
        }

        private void CreateSpoonActor()
        {
            spoonObject = new GameObject("Cinematic Holey Spoon");
            if (actorRoot != null)
            {
                spoonObject.transform.SetParent(actorRoot, false);
            }

            spoonRenderer = spoonObject.AddComponent<SpriteRenderer>();
            spoonRenderer.sprite = spoonSprite;
            spoonRenderer.sortingOrder = 130;
            spoonRenderer.color = new Color(1f, 1f, 1f, 0f);
            spoonObject.transform.position = residentWorld;
            spoonObject.SetActive(false);
        }
    }
}
