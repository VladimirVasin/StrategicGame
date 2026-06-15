using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyResidentSpriteFactory
    {

        private readonly struct ResidentWalkFrame
        {
            public ResidentWalkFrame(
                int bodyYOffset,
                int leftLegX,
                int rightLegX,
                int leftFootX,
                int rightFootX,
                int leftArmX,
                int rightArmX,
                int leftArmY,
                int rightArmY)
            {
                BodyYOffset = bodyYOffset;
                LeftLegX = leftLegX;
                RightLegX = rightLegX;
                LeftFootX = leftFootX;
                RightFootX = rightFootX;
                LeftArmX = leftArmX;
                RightArmX = rightArmX;
                LeftArmY = leftArmY;
                RightArmY = rightArmY;
            }

            public static ResidentWalkFrame Idle => new ResidentWalkFrame(0, 0, 0, 0, 0, 0, 0, 0, 0);

            public int BodyYOffset { get; }
            public int LeftLegX { get; }
            public int RightLegX { get; }
            public int LeftFootX { get; }
            public int RightFootX { get; }
            public int LeftArmX { get; }
            public int RightArmX { get; }
            public int LeftArmY { get; }
            public int RightArmY { get; }
        }

        private readonly struct WoodcutToolFrame
        {
            public WoodcutToolFrame(
                int frameIndex,
                int handleFromX,
                int handleFromY,
                int handleToX,
                int handleToY,
                int headX,
                int headY,
                int headDirection)
            {
                FrameIndex = frameIndex;
                HandleFromX = handleFromX;
                HandleFromY = handleFromY;
                HandleToX = handleToX;
                HandleToY = handleToY;
                HeadX = headX;
                HeadY = headY;
                HeadDirection = headDirection;
            }

            public int FrameIndex { get; }
            public int HandleFromX { get; }
            public int HandleFromY { get; }
            public int HandleToX { get; }
            public int HandleToY { get; }
            public int HeadX { get; }
            public int HeadY { get; }
            public int HeadDirection { get; }
        }
    }
}
