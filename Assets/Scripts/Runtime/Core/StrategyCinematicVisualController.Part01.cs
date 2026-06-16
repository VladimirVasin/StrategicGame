using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCinematicVisualController
    {
        private static Vector3 GetForegroundPosition(Rect view, int anchor)
        {
            return anchor switch
            {
                0 => new Vector3(view.xMin + view.width * 0.12f, view.yMax - view.height * 0.08f, -0.11f),
                1 => new Vector3(view.xMax - view.width * 0.14f, view.yMax - view.height * 0.10f, -0.11f),
                2 => new Vector3(view.xMin + view.width * 0.08f, view.yMin + view.height * 0.30f, -0.11f),
                _ => new Vector3(view.xMax - view.width * 0.10f, view.yMin + view.height * 0.24f, -0.11f)
            };
        }

        private static float GetForegroundRotation(int anchor)
        {
            return anchor switch
            {
                0 => -10f,
                1 => 188f,
                2 => 66f,
                _ => -112f
            };
        }

        private static Vector3 GetForegroundScale(int anchor)
        {
            float sign = anchor == 1 || anchor == 3 ? -1f : 1f;
            float scale = anchor < 2 ? 1.70f : 1.36f;
            return new Vector3(scale * sign, scale, 1f);
        }

        private sealed class PuddleVisual
        {
            public SpriteRenderer Renderer;
            public Vector2 Normalized;
            public float Scale;
            public float Phase;
        }

        private sealed class ForegroundVisual
        {
            public SpriteRenderer Renderer;
            public int Anchor;
            public float Phase;
        }
    }
}
