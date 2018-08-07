using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class RandomAI : AI
    {
        public RandomAI() : base()
        {
            Rand = new Random();
        }

        public override ActionEnum Action(List<Element> elements, ref float xdelta, ref float ydelta, ref float angle)
        {
            xdelta = ydelta = angle = 0;

            // choose a tile to move too
            // split the circle into 8 slices (45 degrees)
            switch(Rand.Next() % 8)
            {
                case 0:
                    xdelta = 0;
                    ydelta = -1;
                    angle = 0;
                    break;
                case 1:
                    xdelta = 0.5f;
                    ydelta = -0.5f;
                    angle = 45;
                    break;
                case 2:
                    xdelta = 1;
                    ydelta = 0;
                    angle = 90;
                    break;
                case 3:
                    xdelta = 0.5f;
                    ydelta = 0.5f;
                    angle = 135;
                    break;
                case 4:
                    xdelta = 0;
                    ydelta = 1;
                    angle = 180;
                    break;
                case 5:
                    xdelta = -0.5f;
                    ydelta = 0.5f;
                    angle = 225;
                    break;
                case 6:
                    xdelta = -1;
                    ydelta = 0;
                    angle = 270;
                    break;
                case 7:
                    xdelta = -0.5f;
                    ydelta = -0.5f;
                    angle = 315;
                    break;
                default: throw new Exception("Unknown angle");
            }

            // choose action
            switch(Rand.Next() % 6)
            {
                case 0:
                    return ActionEnum.SwitchWeapon;
                case 1:
                    return ActionEnum.Pickup;
                case 2:
                    // drop
                    return ActionEnum.None;
                case 3:
                    return ActionEnum.Reload;
                case 4:
                    return ActionEnum.Attack;
                case 5:
                    return ActionEnum.Move;
                default:
                    throw new Exception("Unknown action");
            }
        }

        #region private
        private Random Rand;
        #endregion
    }
}
