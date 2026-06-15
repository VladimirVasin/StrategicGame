using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyDeerSpriteFactory
    {

        private readonly struct DeerFrame
        {
            public DeerFrame(
                int bodyY,
                int frontLegA,
                int frontLegB,
                int backLegA,
                int backLegB,
                int frontHoofA,
                int frontHoofB,
                int tailY,
                int earY,
                int neckX,
                int neckY)
            {
                BodyY = bodyY;
                FrontLegA = frontLegA;
                FrontLegB = frontLegB;
                BackLegA = backLegA;
                BackLegB = backLegB;
                FrontHoofA = frontHoofA;
                FrontHoofB = frontHoofB;
                BackHoofA = backLegA;
                BackHoofB = backLegB;
                TailY = tailY;
                EarY = earY;
                NeckX = neckX;
                NeckY = neckY - 4;
                HeadX = neckX + 7;
                HeadY = neckY;
            }

            public int BodyY { get; }
            public int FrontLegA { get; }
            public int FrontLegB { get; }
            public int BackLegA { get; }
            public int BackLegB { get; }
            public int FrontHoofA { get; }
            public int FrontHoofB { get; }
            public int BackHoofA { get; }
            public int BackHoofB { get; }
            public int TailY { get; }
            public int EarY { get; }
            public int NeckX { get; }
            public int NeckY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
        }

        private readonly struct DeerPalette
        {
            public DeerPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color face,
                Color legDark,
                Color leg,
                Color muzzle,
                Color tail,
                Color antler)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                Face = face;
                LegDark = legDark;
                Leg = leg;
                Muzzle = muzzle;
                Tail = tail;
                Antler = antler;
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color Face { get; }
            public Color LegDark { get; }
            public Color Leg { get; }
            public Color Muzzle { get; }
            public Color Tail { get; }
            public Color Antler { get; }
        }
    }
}
