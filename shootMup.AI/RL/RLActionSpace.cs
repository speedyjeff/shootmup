using engine.Common;
using engine.Common.Entities;
using System.Collections.Generic;

namespace shootMup.Bots.RL
{
    // Discrete action space (17 actions):
    //   0-7  : Move in 8 compass directions (0, 45, 90, 135, 180, 225, 270, 315)
    //   8    : Stand (None)
    //   9    : Reload
    //   10   : Pickup
    //   11   : Drop
    //   12   : SwitchPrimary
    //   13   : Jump
    //   14   : Attack toward nearest enemy (auto-aim)
    //   15   : Attack at current facing
    //   16   : Move toward nearest enemy (chase)
    public static class RLActionSpace
    {
        public const int ActionCount = 17;

        private static readonly float[] MoveAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        public static ActionEnum Decode(
            int actionIndex,
            Player self,
            List<Element> elements,
            float currentFacing,
            out float xdelta,
            out float ydelta,
            out float angle)
        {
            xdelta = 0f;
            ydelta = 0f;
            angle = currentFacing;

            if (actionIndex >= 0 && actionIndex <= 7)
            {
                angle = MoveAngles[actionIndex];
                SetMoveDelta(angle, out xdelta, out ydelta);
                return ActionEnum.Move;
            }

            switch (actionIndex)
            {
                case 8:
                    return ActionEnum.None;
                case 9:
                    return ActionEnum.Reload;
                case 10:
                    return ActionEnum.Pickup;
                case 11:
                    return ActionEnum.Drop;
                case 12:
                    return ActionEnum.SwitchPrimary;
                case 13:
                    return ActionEnum.Jump;
                case 14:
                    {
                        var enemyAngle = NearestPlayerAngle(self, elements);
                        if (enemyAngle.HasValue) angle = enemyAngle.Value;
                        return ActionEnum.Attack;
                    }
                case 15:
                    return ActionEnum.Attack;
                case 16:
                    {
                        var enemyAngle = NearestPlayerAngle(self, elements);
                        if (enemyAngle.HasValue) angle = enemyAngle.Value;
                        SetMoveDelta(angle, out xdelta, out ydelta);
                        return ActionEnum.Move;
                    }
                default:
                    return ActionEnum.None;
            }
        }

        private static void SetMoveDelta(float angleDeg, out float xdelta, out float ydelta)
        {
            float x1, y1, x2, y2;
            Collision.CalculateLineByAngle(0, 0, angleDeg, 1, out x1, out y1, out x2, out y2);
            xdelta = x2 - x1;
            ydelta = y2 - y1;
            var sum = System.Math.Abs(xdelta) + System.Math.Abs(ydelta);
            if (sum > 0)
            {
                xdelta /= sum;
                ydelta /= sum;
            }
        }

        private static float? NearestPlayerAngle(Player self, List<Element> elements)
        {
            float bestDist = float.MaxValue;
            float? bestAngle = null;
            foreach (var elem in elements)
            {
                if (elem.Id == self.Id) continue;
                if (!(elem is Player)) continue;
                var d = Collision.DistanceBetweenPoints(self.X, self.Y, elem.X, elem.Y);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestAngle = Collision.CalculateAngleFromPoint(self.X, self.Y, elem.X, elem.Y);
                }
            }
            return bestAngle;
        }
    }
}
