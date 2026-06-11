using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyRabbitSpritePose
    {
        Idle,
        Hop,
        Nibble,
        Alert,
        Flee,
        Groom,
        Rest,
        Hit,
        Death,
        Carcass
    }

    internal static class StrategyRabbitSpriteFactory
    {
        private const float PixelsPerUnit = 38f;
        public const int IdleFrameCount = 6;
        public const int HopFrameCount = 8;
        public const int NibbleFrameCount = 6;
        public const int AlertFrameCount = 6;
        public const int FleeFrameCount = 10;
        public const int GroomFrameCount = 8;
        public const int RestFrameCount = 4;
        public const int HitFrameCount = 4;
        public const int DeathFrameCount = 8;

        private static readonly Dictionary<int, Sprite> CachedSprites = new();

        public static Sprite GetIdleSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Idle, NormalizeFrame(frame, IdleFrameCount));
        }

        public static Sprite GetHopSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Hop, NormalizeFrame(frame, HopFrameCount));
        }

        public static Sprite GetNibbleSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Nibble, NormalizeFrame(frame, NibbleFrameCount));
        }

        public static Sprite GetAlertSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Alert, NormalizeFrame(frame, AlertFrameCount));
        }

        public static Sprite GetFleeSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Flee, NormalizeFrame(frame, FleeFrameCount));
        }

        public static Sprite GetGroomSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Groom, NormalizeFrame(frame, GroomFrameCount));
        }

        public static Sprite GetRestSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Rest, NormalizeFrame(frame, RestFrameCount));
        }

        public static Sprite GetHitSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Hit, NormalizeFrame(frame, HitFrameCount));
        }

        public static Sprite GetDeathSprite(StrategyRabbitSex sex, int frame)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Death, Mathf.Clamp(frame, 0, DeathFrameCount - 1));
        }

        public static Sprite GetCarcassSprite(StrategyRabbitSex sex)
        {
            return GetSprite(sex, StrategyRabbitSpritePose.Carcass, 0);
        }

        private static Sprite GetSprite(StrategyRabbitSex sex, StrategyRabbitSpritePose pose, int frame)
        {
            int cacheKey = ((int)sex * 4096) + ((int)pose * 128) + frame;
            if (!CachedSprites.TryGetValue(cacheKey, out Sprite sprite) || sprite == null)
            {
                sprite = CreateSprite(sex, pose, frame);
                CachedSprites[cacheKey] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateSprite(StrategyRabbitSex sex, StrategyRabbitSpritePose pose, int frame)
        {
            const int width = 54;
            const int height = 40;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"Rabbit {sex} {pose} Frame {frame + 1}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(new Color[width * height]);

            RabbitFrame rabbitFrame = GetFrame(pose, frame);
            RabbitPalette palette = GetPalette(sex);

            if (pose == StrategyRabbitSpritePose.Carcass)
            {
                DrawCarcassRabbit(texture, rabbitFrame, palette, DeathFrameCount - 1, true);
                texture.Apply(false, false);
                return Sprite.Create(texture, new Rect(3f, 3f, 48f, 34f), new Vector2(0.5f, 0.12f), PixelsPerUnit);
            }

            if (pose == StrategyRabbitSpritePose.Death)
            {
                DrawCarcassRabbit(texture, rabbitFrame, palette, frame, false);
                texture.Apply(false, false);
                return Sprite.Create(texture, new Rect(3f, 3f, 48f, 34f), new Vector2(0.5f, 0.12f), PixelsPerUnit);
            }

            if (pose == StrategyRabbitSpritePose.Rest)
            {
                DrawRestingRabbit(texture, rabbitFrame, palette);
                texture.Apply(false, false);
                return Sprite.Create(texture, new Rect(3f, 3f, 48f, 34f), new Vector2(0.5f, 0.12f), PixelsPerUnit);
            }

            DrawStandingRabbit(texture, sex, pose, frame, rabbitFrame, palette);
            if (pose == StrategyRabbitSpritePose.Hit)
            {
                DrawArrowWound(texture, frame, palette);
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(3f, 3f, 48f, 34f), new Vector2(0.5f, 0.12f), PixelsPerUnit);
        }

        private static void DrawStandingRabbit(
            Texture2D texture,
            StrategyRabbitSex sex,
            StrategyRabbitSpritePose pose,
            int spriteFrame,
            RabbitFrame frame,
            RabbitPalette palette)
        {
            int bodyCenterX = 24;
            int bodyCenterY = 17 + frame.BodyY + frame.HopY;
            int bodyRadiusX = 11 + frame.BodyStretchX + (sex == StrategyRabbitSex.Male ? 1 : 0);
            int bodyRadiusY = 7 + frame.BodyStretchY;
            int headX = bodyCenterX + 12 + frame.HeadX;
            int headY = bodyCenterY + 5 + frame.HeadY;

            FillEllipse(texture, bodyCenterX - 1, 7 + Mathf.Max(0, frame.HopY / 2), 14, 4, new Color(0f, 0f, 0f, 0.22f));

            DrawFeet(texture, bodyCenterX, bodyCenterY, frame, palette);

            FillEllipse(texture, bodyCenterX, bodyCenterY, bodyRadiusX + 1, bodyRadiusY + 1, palette.Outline);
            FillEllipse(texture, bodyCenterX, bodyCenterY + 1, bodyRadiusX, bodyRadiusY, palette.BodyDark);
            FillEllipse(texture, bodyCenterX - 2, bodyCenterY + 1, bodyRadiusX - 2, bodyRadiusY - 1, palette.Body);
            FillEllipse(texture, bodyCenterX + 5, bodyCenterY + 2, Mathf.Max(3, bodyRadiusX - 5), Mathf.Max(3, bodyRadiusY - 3), palette.BodyLight);
            FillEllipse(texture, bodyCenterX - 10, bodyCenterY + 2 + frame.TailY, 4, 4, palette.Outline);
            FillEllipse(texture, bodyCenterX - 11, bodyCenterY + 3 + frame.TailY, 3, 3, palette.Tail);

            DrawEars(texture, headX, headY, frame, palette);
            FillEllipse(texture, headX, headY, 7, 6, palette.Outline);
            FillEllipse(texture, headX, headY, 6, 5, palette.FaceDark);
            FillEllipse(texture, headX + 2, headY + 1, 4, 4, palette.Face);
            FillEllipse(texture, headX + 6, headY - 1, 4, 3, palette.Outline);
            FillEllipse(texture, headX + 7, headY - 1, 3, 2, palette.Muzzle);

            SetPixelSafe(texture, headX + 2, headY + 2, palette.Outline);
            SetPixelSafe(texture, headX + 8, headY, palette.Outline);
            SetPixelSafe(texture, headX + 9, headY - 1, palette.Nose);
            DrawWhiskers(texture, headX + 7, headY - 1, palette.Whisker);

            if (pose == StrategyRabbitSpritePose.Nibble)
            {
                DrawNibbleGrass(texture, headX + 9, headY - 5, spriteFrame, palette);
            }

            if (pose == StrategyRabbitSpritePose.Groom)
            {
                DrawGroomingPaw(texture, headX - 4, headY - 2 + (spriteFrame % 2), palette);
            }

            AddFurDetails(texture, pose, spriteFrame, palette);
        }

        private static void DrawRestingRabbit(Texture2D texture, RabbitFrame frame, RabbitPalette palette)
        {
            int bodyCenterX = 25;
            int bodyCenterY = 13 + frame.BodyY;
            int headX = bodyCenterX + 11 + frame.HeadX;
            int headY = bodyCenterY + 4 + frame.HeadY;

            FillEllipse(texture, bodyCenterX - 1, 7, 16, 4, new Color(0f, 0f, 0f, 0.20f));
            FillEllipse(texture, bodyCenterX - 1, bodyCenterY, 14, 7, palette.Outline);
            FillEllipse(texture, bodyCenterX - 1, bodyCenterY + 1, 13, 6, palette.BodyDark);
            FillEllipse(texture, bodyCenterX - 3, bodyCenterY + 1, 10, 5, palette.Body);
            FillEllipse(texture, bodyCenterX - 13, bodyCenterY + 2, 4, 3, palette.Outline);
            FillEllipse(texture, bodyCenterX - 14, bodyCenterY + 3, 3, 2, palette.Tail);

            DrawFoldedEars(texture, headX, headY, frame, palette);
            FillEllipse(texture, headX, headY, 7, 5, palette.Outline);
            FillEllipse(texture, headX, headY, 6, 4, palette.FaceDark);
            FillEllipse(texture, headX + 2, headY + 1, 4, 3, palette.Face);
            FillEllipse(texture, headX + 6, headY - 1, 3, 2, palette.Muzzle);
            SetPixelSafe(texture, headX + 2, headY + 1, palette.Outline);
            SetPixelSafe(texture, headX + 8, headY - 1, palette.Nose);
        }

        private static void DrawCarcassRabbit(Texture2D texture, RabbitFrame frame, RabbitPalette palette, int spriteFrame, bool finalFrame)
        {
            int slide = Mathf.Clamp(spriteFrame, 0, DeathFrameCount - 1);
            int bodyCenterX = 25 - Mathf.Min(4, slide / 2);
            int bodyCenterY = 12 + frame.BodyY - Mathf.Min(2, slide / 3);
            int headX = bodyCenterX + 13 - Mathf.Min(5, slide);
            int headY = bodyCenterY + 2 - Mathf.Min(2, slide / 2);
            Color blood = finalFrame ? Rgb(112, 42, 36) : Rgb(147, 50, 42);

            FillEllipse(texture, bodyCenterX - 1, 7, 17, 4, new Color(0f, 0f, 0f, 0.22f));
            FillEllipse(texture, bodyCenterX, bodyCenterY, 15, 6, palette.Outline);
            FillEllipse(texture, bodyCenterX, bodyCenterY + 1, 14, 5, palette.BodyDark);
            FillEllipse(texture, bodyCenterX - 3, bodyCenterY + 1, 11, 4, palette.Body);
            FillEllipse(texture, bodyCenterX + 7, bodyCenterY + 1, 6, 3, palette.BodyLight);

            DrawFoldedEars(texture, headX, headY, frame, palette);
            FillEllipse(texture, headX, headY, 7, 4, palette.Outline);
            FillEllipse(texture, headX + 1, headY, 6, 3, palette.FaceDark);
            FillEllipse(texture, headX + 4, headY + 1, 3, 2, palette.Face);
            FillEllipse(texture, headX + 7, headY - 1, 3, 2, palette.Muzzle);
            SetPixelSafe(texture, headX + 1, headY + 1, palette.Outline);

            FillEllipse(texture, bodyCenterX - 5, bodyCenterY - 2, 4, 2, blood);
            DrawLine(texture, P(bodyCenterX - 8, bodyCenterY - 2), P(bodyCenterX - 14, bodyCenterY - 3), blood);
            DrawLine(texture, P(bodyCenterX - 3, bodyCenterY - 3), P(bodyCenterX + 4, bodyCenterY - 5), Rgb(83, 50, 32));
            SetPixelSafe(texture, bodyCenterX + 5, bodyCenterY - 6, Rgb(210, 185, 125));
        }

        private static void DrawArrowWound(Texture2D texture, int frame, RabbitPalette palette)
        {
            int y = 19 + (frame % 2);
            DrawLine(texture, P(19, y), P(8, y + 6), palette.Outline);
            DrawLine(texture, P(18, y), P(9, y + 5), Rgb(128, 83, 43));
            FillRect(texture, 6, y + 5, 3, 2, Rgb(218, 198, 136));
            FillEllipse(texture, 21, y - 1, 2, 2, Rgb(139, 44, 39));
        }

        private static void DrawFeet(Texture2D texture, int bodyCenterX, int bodyCenterY, RabbitFrame frame, RabbitPalette palette)
        {
            int footY = 8 + frame.HopY;
            FillEllipse(texture, bodyCenterX - 8 + frame.BackFootX, footY, 6, 2, palette.Outline);
            FillEllipse(texture, bodyCenterX - 8 + frame.BackFootX, footY + 1, 5, 1, palette.Foot);
            FillEllipse(texture, bodyCenterX + 7 + frame.FrontFootX, footY + 1, 4, 2, palette.Outline);
            FillEllipse(texture, bodyCenterX + 7 + frame.FrontFootX, footY + 2, 3, 1, palette.FootLight);
        }

        private static void DrawEars(Texture2D texture, int headX, int headY, RabbitFrame frame, RabbitPalette palette)
        {
            Vector2Int leftBase = P(headX - 3, headY + 4);
            Vector2Int rightBase = P(headX + 2, headY + 4);
            Vector2Int leftTip = P(headX - 7 + frame.EarTilt, headY + 15 + frame.EarY);
            Vector2Int rightTip = P(headX + 4 + frame.EarTilt, headY + 15 - frame.EarY);

            DrawThickLine(texture, leftBase, leftTip, palette.Outline, 2);
            DrawThickLine(texture, rightBase, rightTip, palette.Outline, 2);
            DrawThickLine(texture, leftBase + P(0, 1), leftTip + P(1, -1), palette.Ear, 1);
            DrawThickLine(texture, rightBase + P(0, 1), rightTip + P(-1, -1), palette.Ear, 1);
            DrawLine(texture, leftBase + P(1, 2), leftTip + P(1, -4), palette.EarInner);
            DrawLine(texture, rightBase + P(0, 2), rightTip + P(-1, -4), palette.EarInner);
        }

        private static void DrawFoldedEars(Texture2D texture, int headX, int headY, RabbitFrame frame, RabbitPalette palette)
        {
            Vector2Int basePoint = P(headX - 2, headY + 4);
            DrawThickLine(texture, basePoint, P(headX - 11, headY + 8 + frame.EarY), palette.Outline, 2);
            DrawThickLine(texture, basePoint + P(2, 0), P(headX + 7, headY + 7 - frame.EarY), palette.Outline, 2);
            DrawThickLine(texture, basePoint, P(headX - 10, headY + 8 + frame.EarY), palette.Ear, 1);
            DrawThickLine(texture, basePoint + P(2, 0), P(headX + 6, headY + 7 - frame.EarY), palette.Ear, 1);
        }

        private static void DrawWhiskers(Texture2D texture, int x, int y, Color color)
        {
            DrawLine(texture, P(x, y), P(x + 6, y + 2), color);
            DrawLine(texture, P(x, y), P(x + 7, y), color);
            DrawLine(texture, P(x, y), P(x + 5, y - 2), color);
        }

        private static void DrawNibbleGrass(Texture2D texture, int x, int y, int frame, RabbitPalette palette)
        {
            Color grass = frame % 2 == 0 ? Rgb(91, 139, 60) : Rgb(121, 166, 78);
            DrawLine(texture, P(x, y), P(x + 1, y + 5), grass);
            DrawLine(texture, P(x + 2, y), P(x + 4, y + 4), grass);
            SetPixelSafe(texture, x + 3, y + 2, palette.BodyLight);
        }

        private static void DrawGroomingPaw(Texture2D texture, int x, int y, RabbitPalette palette)
        {
            FillEllipse(texture, x, y, 3, 2, palette.Outline);
            FillEllipse(texture, x + 1, y, 2, 1, palette.FootLight);
        }

        private static void AddFurDetails(Texture2D texture, StrategyRabbitSpritePose pose, int frame, RabbitPalette palette)
        {
            int count = pose == StrategyRabbitSpritePose.Flee ? 7 : 5;
            for (int i = 0; i < count; i++)
            {
                int x = 16 + ((frame * 7 + i * 5 + (int)pose * 3) % 18);
                int y = 17 + ((frame * 3 + i * 4) % 7);
                SetPixelSafe(texture, x, y, i % 2 == 0 ? palette.BodyLight : palette.BodyDark);
            }
        }

        private static RabbitFrame GetFrame(StrategyRabbitSpritePose pose, int frame)
        {
            int normalized;
            switch (pose)
            {
                case StrategyRabbitSpritePose.Hop:
                    normalized = NormalizeFrame(frame, HopFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(1, 1, 0, 0, 1, 0, -1, 1, 1, 0, 0),
                        2 => new RabbitFrame(2, 3, 1, 0, 2, 1, -2, 3, 1, -1, 1),
                        3 => new RabbitFrame(1, 4, 2, 0, 2, 1, -3, 5, 2, -1, 1),
                        4 => new RabbitFrame(0, 1, 1, -1, 1, 0, -1, 2, 0, 0, 0),
                        5 => new RabbitFrame(0, 0, 0, 0, 0, -1, 1, -2, -1, 1, -1),
                        6 => new RabbitFrame(1, 2, 0, 0, 1, 0, -1, 1, 0, 0, 0),
                        _ => new RabbitFrame(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
                    };
                case StrategyRabbitSpritePose.Flee:
                    normalized = NormalizeFrame(frame, FleeFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(1, 2, 1, 0, 2, 1, -2, 4, 2, -1, 1),
                        2 => new RabbitFrame(2, 4, 3, 0, 3, 1, -4, 6, 2, -1, 1),
                        3 => new RabbitFrame(1, 3, 1, -1, 1, 0, -2, 2, 1, 0, 0),
                        4 => new RabbitFrame(0, 0, -1, 0, -1, -1, 2, -4, -1, 1, -1),
                        5 => new RabbitFrame(1, 2, 0, 0, 0, 0, 1, -2, 0, 0, 0),
                        6 => new RabbitFrame(2, 5, 2, 0, 2, 1, -3, 5, 2, -1, 1),
                        7 => new RabbitFrame(1, 3, 1, -1, 1, 0, -2, 2, 1, 0, 0),
                        8 => new RabbitFrame(0, 0, -1, 0, -1, -1, 2, -4, -1, 1, -1),
                        _ => new RabbitFrame(0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0)
                    };
                case StrategyRabbitSpritePose.Nibble:
                    normalized = NormalizeFrame(frame, NibbleFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(0, 0, 1, -2, -2, -1, 0, 0, -1, 0, 0),
                        2 => new RabbitFrame(0, 0, 2, -4, -3, -2, 0, 0, -1, 0, 0),
                        3 => new RabbitFrame(0, 0, 2, -5, -3, -2, 0, 0, -1, 0, 0),
                        4 => new RabbitFrame(0, 0, 1, -3, -2, -1, 0, 0, -1, 0, 0),
                        _ => new RabbitFrame(0, 0, 0, -1, -1, 0, 0, 0, 0, 0, 0)
                    };
                case StrategyRabbitSpritePose.Alert:
                    normalized = NormalizeFrame(frame, AlertFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(1, 0, -1, 3, 0, 2, 0, 0, -1, 1, 0),
                        3 => new RabbitFrame(0, 0, -1, 4, 1, 1, 0, 0, -1, 1, 0),
                        5 => new RabbitFrame(1, 0, -1, 3, -1, 2, 0, 0, -1, 1, 0),
                        _ => new RabbitFrame(0, 0, -1, 3, 0, 2, 0, 0, -1, 1, 0)
                    };
                case StrategyRabbitSpritePose.Groom:
                    normalized = NormalizeFrame(frame, GroomFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(0, 0, -1, 1, 1, 0, 1, 0, 0, 0, 0),
                        2 => new RabbitFrame(0, 0, -2, 1, 1, 0, 2, 0, 0, 0, 0),
                        3 => new RabbitFrame(0, 0, -1, 0, 1, -1, 2, 0, 0, 0, 0),
                        5 => new RabbitFrame(0, 0, -2, 1, 0, 0, 2, 0, 0, 0, 0),
                        6 => new RabbitFrame(0, 0, -1, 1, 1, 0, 1, 0, 0, 0, 0),
                        _ => new RabbitFrame(0, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0)
                    };
                case StrategyRabbitSpritePose.Rest:
                    normalized = NormalizeFrame(frame, RestFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(0, 0, 0, -1, 0, -1, 0, 0, 0, 0, 0),
                        3 => new RabbitFrame(0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0),
                        _ => new RabbitFrame(0, 0, 0, -1, 0, 0, 0, 0, 0, 0, 0)
                    };
                case StrategyRabbitSpritePose.Hit:
                    normalized = NormalizeFrame(frame, HitFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(1, 1, -1, 2, -1, 2, 0, 0, 0, 0, 0),
                        2 => new RabbitFrame(0, 0, -2, 1, -2, 1, 1, -1, 0, -1, 0),
                        _ => new RabbitFrame(0, 0, -1, 3, 0, 2, 0, 0, -1, 1, 0)
                    };
                case StrategyRabbitSpritePose.Death:
                case StrategyRabbitSpritePose.Carcass:
                    normalized = Mathf.Clamp(frame, 0, DeathFrameCount - 1);
                    return normalized switch
                    {
                        0 => new RabbitFrame(0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0),
                        1 => new RabbitFrame(-1, 0, -1, -1, 0, -1, 0, 0, 0, -1, 0),
                        2 => new RabbitFrame(-2, 0, -2, -1, -1, -1, 0, 0, 0, -1, 0),
                        3 => new RabbitFrame(-2, 0, -3, -2, -1, -2, 0, 0, 0, -1, 0),
                        _ => new RabbitFrame(-3, 0, -4, -2, -1, -2, 0, 0, 0, -1, 0)
                    };
                default:
                    normalized = NormalizeFrame(frame, IdleFrameCount);
                    return normalized switch
                    {
                        1 => new RabbitFrame(1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0),
                        3 => new RabbitFrame(0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 0),
                        5 => new RabbitFrame(1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0),
                        _ => new RabbitFrame(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
                    };
            }
        }

        private static RabbitPalette GetPalette(StrategyRabbitSex sex)
        {
            if (sex == StrategyRabbitSex.Male)
            {
                Color body = Rgb(129, 113, 89);
                return new RabbitPalette(
                    Rgb(42, 39, 34),
                    body,
                    Shift(body, -0.16f),
                    Shift(body, 0.17f),
                    Rgb(151, 133, 101),
                    Rgb(189, 164, 131),
                    Rgb(212, 179, 159),
                    Rgb(87, 70, 55),
                    Rgb(200, 190, 172),
                    Rgb(225, 211, 188),
                    Rgb(36, 29, 25),
                    Rgb(222, 216, 197));
            }

            Color doeBody = Rgb(151, 132, 103);
            return new RabbitPalette(
                Rgb(46, 40, 34),
                doeBody,
                Shift(doeBody, -0.14f),
                Shift(doeBody, 0.16f),
                Rgb(173, 149, 113),
                Rgb(210, 184, 145),
                Rgb(223, 184, 171),
                Rgb(96, 74, 54),
                Rgb(222, 208, 184),
                Rgb(238, 222, 197),
                Rgb(42, 31, 27),
                Rgb(232, 226, 207));
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    SetPixelSafe(texture, px, py, color);
                }
            }
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            int radiusXSqr = radiusX * radiusX;
            int radiusYSqr = radiusY * radiusY;
            int radiusProduct = radiusXSqr * radiusYSqr;

            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    if (x * x * radiusYSqr + y * y * radiusXSqr <= radiusProduct)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void DrawThickLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color, int radius)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int sx = from.x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - from.y);
            int sy = from.y < to.y ? 1 : -1;
            int err = dx + dy;
            int x = from.x;
            int y = from.y;

            while (true)
            {
                FillRect(texture, x - radius, y - radius, radius * 2 + 1, radius * 2 + 1, color);
                if (x == to.x && y == to.y)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color color)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int sx = from.x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - from.y);
            int sy = from.y < to.y ? 1 : -1;
            int err = dx + dy;
            int x = from.x;
            int y = from.y;

            while (true)
            {
                SetPixelSafe(texture, x, y, color);
                if (x == to.x && y == to.y)
                {
                    break;
                }

                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Color Shift(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private static Vector2Int P(int x, int y)
        {
            return new Vector2Int(x, y);
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }

        private static int NormalizeFrame(int frame, int frameCount)
        {
            if (frameCount <= 0)
            {
                return 0;
            }

            int normalized = frame % frameCount;
            return normalized < 0 ? normalized + frameCount : normalized;
        }

        private readonly struct RabbitFrame
        {
            public RabbitFrame(
                int bodyY,
                int hopY,
                int headX,
                int headY,
                int earTilt,
                int earY,
                int frontFootX,
                int backFootX,
                int bodyStretchX,
                int bodyStretchY,
                int tailY)
            {
                BodyY = bodyY;
                HopY = hopY;
                HeadX = headX;
                HeadY = headY;
                EarTilt = earTilt;
                EarY = earY;
                FrontFootX = frontFootX;
                BackFootX = backFootX;
                BodyStretchX = bodyStretchX;
                BodyStretchY = bodyStretchY;
                TailY = tailY;
            }

            public int BodyY { get; }
            public int HopY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
            public int EarTilt { get; }
            public int EarY { get; }
            public int FrontFootX { get; }
            public int BackFootX { get; }
            public int BodyStretchX { get; }
            public int BodyStretchY { get; }
            public int TailY { get; }
        }

        private readonly struct RabbitPalette
        {
            public RabbitPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color faceDark,
                Color face,
                Color earInner,
                Color ear,
                Color muzzle,
                Color tail,
                Color nose,
                Color foot)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                FaceDark = faceDark;
                Face = face;
                EarInner = earInner;
                Ear = ear;
                Muzzle = muzzle;
                Tail = tail;
                Nose = nose;
                Foot = foot;
                FootLight = Shift(foot, 0.06f);
                Whisker = new Color(0.88f, 0.84f, 0.74f, 0.82f);
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color FaceDark { get; }
            public Color Face { get; }
            public Color EarInner { get; }
            public Color Ear { get; }
            public Color Muzzle { get; }
            public Color Tail { get; }
            public Color Nose { get; }
            public Color Foot { get; }
            public Color FootLight { get; }
            public Color Whisker { get; }
        }
    }
}
