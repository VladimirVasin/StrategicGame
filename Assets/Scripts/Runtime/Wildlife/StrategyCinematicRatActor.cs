using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCinematicRatActor : MonoBehaviour
    {
        private const float DefaultRunFrameRate = 16f;

        private SpriteRenderer spriteRenderer;
        private StrategyCinematicRatAnimation animation = StrategyCinematicRatAnimation.Run;
        private int visualVariant;
        private int sortingOffset = 2;
        private int frameIndex;
        private float frameProgress;
        private float framesPerSecond = DefaultRunFrameRate;
        private Vector3 baseVisualScale = Vector3.one;
        private Quaternion baseVisualRotation = Quaternion.identity;
        private bool loop = true;
        private bool isPlaying;
        private bool automaticTick = true;
        private bool configured;

        public SpriteRenderer Renderer => spriteRenderer;
        public StrategyCinematicRatAnimation Animation => animation;
        public int FrameIndex => frameIndex;
        public int FrameCount => StrategyCinematicRatSpriteFactory.GetFrameCount(animation);
        public int VisualVariant => visualVariant;
        public bool IsPlaying => isPlaying;
        public bool AutomaticTick => automaticTick;

        public static StrategyCinematicRatActor Create(
            Transform parent,
            Vector3 worldPosition,
            int variant = 0,
            int worldSortingOffset = 2)
        {
            GameObject actorObject = new("Cinematic Rat", typeof(SpriteRenderer));
            if (parent != null)
            {
                actorObject.transform.SetParent(parent, true);
            }

            StrategyCinematicRatActor actor = actorObject.AddComponent<StrategyCinematicRatActor>();
            actor.Configure(
                actorObject.GetComponent<SpriteRenderer>(),
                variant,
                worldSortingOffset);
            actor.SetWorldPosition(worldPosition);
            return actor;
        }

        public void Configure(
            SpriteRenderer targetRenderer,
            int variant = 0,
            int worldSortingOffset = 2)
        {
            ResetVisualPose();
            spriteRenderer = targetRenderer;
            visualVariant = PositiveModulo(
                variant,
                StrategyCinematicRatSpriteFactory.VariantCount);
            sortingOffset = worldSortingOffset;
            configured = spriteRenderer != null;
            frameProgress = 0f;
            animation = StrategyCinematicRatAnimation.Run;
            framesPerSecond = DefaultRunFrameRate;
            loop = true;
            isPlaying = configured;
            if (spriteRenderer != null)
            {
                baseVisualScale = Vector3.one
                    * StrategySettlementFaunaSpriteFactory.MouseWorldScale;
                baseVisualRotation = spriteRenderer.transform.localRotation;
                ResetVisualPose();
            }

            ApplyFrame(0);
            ApplySorting();
        }

        public void Play(
            StrategyCinematicRatAnimation nextAnimation,
            float frameRate,
            bool shouldLoop,
            bool restart = true)
        {
            StrategyCinematicRatAnimation normalizedAnimation =
                nextAnimation == StrategyCinematicRatAnimation.Escape
                    ? StrategyCinematicRatAnimation.Escape
                    : StrategyCinematicRatAnimation.Run;
            bool changed = animation != normalizedAnimation;
            animation = normalizedAnimation;
            framesPerSecond = Mathf.Max(0.01f, frameRate);
            loop = shouldLoop;
            isPlaying = configured;
            if (restart || changed)
            {
                frameProgress = 0f;
                ApplyFrame(0);
            }
            else
            {
                ApplyFrame(frameIndex);
            }
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void SetAutomaticTick(bool enabled)
        {
            automaticTick = enabled;
        }

        public void SetFrame(int nextFrameIndex)
        {
            frameProgress = 0f;
            ApplyFrame(nextFrameIndex);
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (!configured || !isPlaying || unscaledDeltaTime <= 0f)
            {
                return;
            }

            frameProgress += unscaledDeltaTime * framesPerSecond;
            int elapsedFrames = Mathf.FloorToInt(frameProgress);
            if (elapsedFrames <= 0)
            {
                return;
            }

            frameProgress -= elapsedFrames;
            int nextFrame = frameIndex + elapsedFrames;
            if (loop)
            {
                ApplyFrame(nextFrame);
                return;
            }

            int finalFrame = FrameCount - 1;
            if (nextFrame >= finalFrame)
            {
                frameProgress = 0f;
                isPlaying = false;
                ApplyFrame(finalFrame);
                ResetVisualPose();
                return;
            }

            ApplyFrame(nextFrame);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            ApplySorting();
        }

        public bool MoveTowards(
            Vector3 worldTarget,
            float unitsPerSecond,
            float unscaledDeltaTime)
        {
            Vector3 before = transform.position;
            float distance = Mathf.Max(0f, unitsPerSecond) * Mathf.Max(0f, unscaledDeltaTime);
            Vector3 next = Vector3.MoveTowards(before, worldTarget, distance);
            transform.position = next;
            float horizontalMovement = next.x - before.x;
            if (Mathf.Abs(horizontalMovement) > 0.0001f)
            {
                SetFacingLeft(horizontalMovement < 0f);
            }

            ApplySorting();
            return (worldTarget - next).sqrMagnitude <= 0.000001f;
        }

        public void SetFacingLeft(bool faceLeft)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = faceLeft;
            }
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        private void Update()
        {
            if (automaticTick)
            {
                Advance(Time.unscaledDeltaTime);
            }
        }

        private void ApplyFrame(int nextFrameIndex)
        {
            int frameCount = StrategyCinematicRatSpriteFactory.GetFrameCount(animation);
            frameIndex = PositiveModulo(nextFrameIndex, frameCount);
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = StrategyCinematicRatSpriteFactory.GetFrame(
                    animation,
                    visualVariant,
                    frameIndex);
                ApplyMotionPose(frameCount);
            }
        }

        private void ApplyMotionPose(int frameCount)
        {
            float phase = frameCount > 0
                ? frameIndex / (float)frameCount * Mathf.PI * 2f
                : 0f;
            float stride = Mathf.Sin(phase);
            float effort = Mathf.Abs(stride);
            float squash = animation == StrategyCinematicRatAnimation.Escape ? 0.08f : 0.05f;
            float tilt = animation == StrategyCinematicRatAnimation.Escape ? 5f : 3f;
            Transform visual = spriteRenderer.transform;
            visual.localScale = Vector3.Scale(
                baseVisualScale,
                new Vector3(1f, 1f - effort * squash, 1f));
            visual.localRotation = baseVisualRotation
                * Quaternion.Euler(0f, 0f, stride * tilt);
        }

        private void ResetVisualPose()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.transform.localScale = baseVisualScale;
            spriteRenderer.transform.localRotation = baseVisualRotation;
        }

        private void ApplySorting()
        {
            if (spriteRenderer != null)
            {
                StrategyWorldSorting.Apply(spriteRenderer, transform.position, sortingOffset);
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }

        private void OnDisable()
        {
            ResetVisualPose();
        }
    }
}
