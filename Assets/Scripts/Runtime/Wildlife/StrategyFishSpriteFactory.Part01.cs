using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyFishSpriteFactory
    {

        private readonly struct FishPalette
        {
            public FishPalette(
                Color outline,
                Color body,
                Color bodyDark,
                Color bodyLight,
                Color fin,
                Color finDark,
                Color finLight,
                Color gill,
                Color muzzle,
                Color marking)
            {
                Outline = outline;
                Body = body;
                BodyDark = bodyDark;
                BodyLight = bodyLight;
                Fin = fin;
                FinDark = finDark;
                FinLight = finLight;
                Gill = gill;
                Muzzle = muzzle;
                Marking = marking;
            }

            public Color Outline { get; }
            public Color Body { get; }
            public Color BodyDark { get; }
            public Color BodyLight { get; }
            public Color Fin { get; }
            public Color FinDark { get; }
            public Color FinLight { get; }
            public Color Gill { get; }
            public Color Muzzle { get; }
            public Color Marking { get; }
        }
    }
}
