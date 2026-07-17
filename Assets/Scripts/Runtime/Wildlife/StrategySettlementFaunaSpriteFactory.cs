using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategySettlementFaunaSpriteFactory
    {
        private static readonly Dictionary<int, Sprite> Cats = new();
        private static readonly Dictionary<int, Sprite> Mice = new();
        public const int CatFrameCount = 4;
        public const float CatWorldScale = 0.70f;
        public const float MouseWorldScale = 0.72f;

        public static Sprite GetCatSprite(StrategyCatCoat coat)
        {
            return GetCatSprite(coat, StrategyCatSpritePose.Idle, 0);
        }

        public static Sprite GetCatSprite(StrategyCatCoat coat, StrategyCatSpritePose pose, int frame)
        {
            frame = Mathf.Abs(frame) % CatFrameCount;
            int key = (int)coat * 100 + (int)pose * 10 + frame;
            if (!Cats.TryGetValue(key, out Sprite sprite) || sprite == null) Cats[key] = sprite = CreateCat(coat, pose, frame);
            return sprite;
        }

        public static Sprite GetMouseSprite(int variant)
        {
            int key = Mathf.Abs(variant) % 3;
            if (!Mice.TryGetValue(key, out Sprite sprite) || sprite == null) Mice[key] = sprite = CreateMouse(key);
            return sprite;
        }

        private static Sprite CreateCat(StrategyCatCoat coat, StrategyCatSpritePose pose, int frame)
        {
            Texture2D t = NewTexture(30, 24, $"Settlement Cat {coat} {pose} {frame + 1}");
            Color body = CatColor(coat); Color dark = Color.Lerp(body, Color.black, 0.38f); Color light = Color.Lerp(body, Color.white, 0.28f);
            if (pose == StrategyCatSpritePose.Pounce)
            {
                DrawPouncingCat(t, frame, body, dark, light);
            }
            else if (pose == StrategyCatSpritePose.Joy)
            {
                DrawJoyfulCat(t, frame, body, dark, light);
            }
            else
            {
                DrawStandardCat(t, pose, frame, body, dark, light);
            }

            t.Apply(false, false); return Sprite.Create(t, new Rect(1, 3, 26, 20), new Vector2(0.5f, 0.16f), 28f);
        }

        private static void DrawStandardCat(
            Texture2D t,
            StrategyCatSpritePose pose,
            int frame,
            Color body,
            Color dark,
            Color light)
        {
            int bodyY = pose == StrategyCatSpritePose.Stalk ? 5 : pose == StrategyCatSpritePose.Rest ? 4 : 6 + (pose == StrategyCatSpritePose.Idle && frame == 2 ? 1 : 0);
            int headX = pose == StrategyCatSpritePose.Stalk ? 20 : 18;
            Fill(t, 7, bodyY, 21, bodyY + 8, dark); Fill(t, 8, bodyY + 1, 22, bodyY + 9, body);
            Fill(t, headX, bodyY + 5, headX + 7, bodyY + 13, dark); Fill(t, headX + 1, bodyY + 6, headX + 6, bodyY + 13, body);
            int stride = pose is StrategyCatSpritePose.Walk or StrategyCatSpritePose.Stalk ? (frame % 2 == 0 ? -1 : 1) : 0;
            Fill(t, 9 + stride, bodyY - 2, 12 + stride, bodyY + 2, dark); Fill(t, 18 - stride, bodyY - 2, 21 - stride, bodyY + 2, dark);
            Fill(t, headX + 1, bodyY + 12, headX + 3, bodyY + 16, dark); Fill(t, headX + 5, bodyY + 12, headX + 7, bodyY + 16, dark);
            int tailLift = pose == StrategyCatSpritePose.Rest ? 0 : frame % 3;
            Fill(t, 3, bodyY + 5, 8, bodyY + 7, dark); Fill(t, 2, bodyY + 7, 4, bodyY + 11 + tailLift, dark); Fill(t, 4, bodyY + 3, 7, bodyY + 5, body);
            Set(t, headX + 2, bodyY + 10, new Color(0.8f, 0.95f, 0.35f)); Set(t, headX + 5, bodyY + 10, new Color(0.8f, 0.95f, 0.35f));
            Set(t, headX + 4, bodyY + 8, new Color(0.92f, 0.55f, 0.58f)); Fill(t, 11, bodyY + 2, 17, bodyY + 3, light);
        }

        private static void DrawPouncingCat(
            Texture2D t,
            int frame,
            Color body,
            Color dark,
            Color light)
        {
            int lift = frame == 1 ? 2 : frame == 2 ? 1 : 0;
            int bodyY = 5 + lift;
            int headX = frame == 0 ? 19 : 21;
            int rearX = frame == 0 ? 7 : 5;
            int frontX = frame >= 2 ? 23 : 20;

            Fill(t, 6, bodyY, 20, bodyY + 7, dark);
            Fill(t, 7, bodyY + 1, 21, bodyY + 8, body);
            Fill(t, headX, bodyY + 4, headX + 6, bodyY + 11, dark);
            Fill(t, headX + 1, bodyY + 5, headX + 5, bodyY + 11, body);
            Fill(t, headX + 1, bodyY + 10, headX + 2, bodyY + 13, dark);
            Fill(t, headX + 5, bodyY + 10, headX + 6, bodyY + 13, dark);

            Fill(t, rearX, bodyY - 2, rearX + 5, bodyY + 2, dark);
            Fill(t, frontX, bodyY - 1, Mathf.Min(26, frontX + 3), bodyY + 1, dark);
            if (frame == 2)
            {
                Fill(t, 20, bodyY - 2, 25, bodyY, body);
            }
            else
            {
                Fill(t, 17, bodyY - 2, 20, bodyY + 1, body);
            }

            int tailY = bodyY + (frame == 0 ? 3 : 6);
            Fill(t, 2, tailY, 8, tailY + 2, dark);
            Fill(t, 1, tailY + 1, 3, tailY + 4, dark);
            Fill(t, 8, bodyY + 2, 14, bodyY + 3, light);
            Set(t, headX + 2, bodyY + 8, new Color(0.8f, 0.95f, 0.35f));
            Set(t, headX + 5, bodyY + 8, new Color(0.8f, 0.95f, 0.35f));
            Set(t, headX + 4, bodyY + 6, new Color(0.92f, 0.55f, 0.58f));
        }

        private static void DrawJoyfulCat(
            Texture2D t,
            int frame,
            Color body,
            Color dark,
            Color light)
        {
            int bounce = frame == 1 ? 1 : frame == 2 ? 2 : frame == 3 ? 1 : 0;
            int bodyY = 4 + bounce;
            int pawLift = frame % 2 == 0 ? 0 : 2;
            int tailWag = frame is 1 or 2 ? 1 : 0;

            Fill(t, 7, bodyY, 20, bodyY + 8, dark);
            Fill(t, 8, bodyY + 1, 21, bodyY + 9, body);
            Fill(t, 17, bodyY + 6, 24, bodyY + 14, dark);
            Fill(t, 18, bodyY + 7, 23, bodyY + 14, body);
            Fill(t, 18, bodyY + 13, 20, bodyY + 16, dark);
            Fill(t, 22, bodyY + 13, 24, bodyY + 16, dark);

            Fill(t, 9, bodyY - 2 + pawLift, 12, bodyY + 2 + pawLift, dark);
            Fill(t, 17, bodyY - pawLift, 20, bodyY + 2, dark);
            Fill(t, 10, bodyY + 2, 16, bodyY + 3, light);

            Fill(t, 4, bodyY + 5, 8, bodyY + 7, dark);
            Fill(t, 2 + tailWag, bodyY + 7, 5 + tailWag, bodyY + 13, dark);
            Fill(t, 3 + tailWag, bodyY + 12, 7 + tailWag, bodyY + 15, dark);
            Fill(t, 6 + tailWag, bodyY + 14, 9 + tailWag, bodyY + 16, body);

            Set(t, 19, bodyY + 11, dark);
            Set(t, 22, bodyY + 11, dark);
            Set(t, 21, bodyY + 9, new Color(0.92f, 0.55f, 0.58f));
            Set(t, 20, bodyY + 8, light);
            Set(t, 22, bodyY + 8, light);
        }

        private static Sprite CreateMouse(int variant)
        {
            Texture2D t = NewTexture(20, 14, $"Settlement Mouse {variant}");
            Color body = variant == 0 ? new Color(0.45f, 0.40f, 0.36f) : variant == 1 ? new Color(0.62f, 0.58f, 0.51f) : new Color(0.30f, 0.29f, 0.28f);
            Color dark = Color.Lerp(body, Color.black, 0.45f); Color ear = new Color(0.73f, 0.43f, 0.45f);
            Fill(t, 5, 4, 14, 9, dark); Fill(t, 6, 5, 15, 10, body); Fill(t, 13, 7, 17, 10, body);
            Fill(t, 13, 10, 15, 12, dark); Set(t, 14, 11, ear); Set(t, 16, 9, Color.black);
            for (int i = 0; i < 5; i++) Set(t, 4 - i, 5 + i / 2, new Color(0.55f, 0.35f, 0.36f));
            t.Apply(false, false); return Sprite.Create(t, new Rect(0, 2, 19, 11), new Vector2(0.5f, 0.18f), 25f);
        }

        private static Texture2D NewTexture(int w, int h, string name)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(new Color[w * h]); return t;
        }
        private static void Fill(Texture2D t, int x0, int y0, int x1, int y1, Color c) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(t, x, y, c); }
        private static void Set(Texture2D t, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, y, c); }
        private static Color CatColor(StrategyCatCoat coat) => coat switch
        {
            StrategyCatCoat.Ginger => new Color(0.76f, 0.40f, 0.15f), StrategyCatCoat.GrayTabby => new Color(0.43f, 0.45f, 0.44f),
            StrategyCatCoat.Black => new Color(0.12f, 0.13f, 0.14f), StrategyCatCoat.BlackAndWhite => new Color(0.25f, 0.26f, 0.26f),
            StrategyCatCoat.Calico => new Color(0.72f, 0.54f, 0.34f), StrategyCatCoat.BrownTabby => new Color(0.45f, 0.29f, 0.18f),
            _ => new Color(0.79f, 0.70f, 0.52f)
        };
    }
}
