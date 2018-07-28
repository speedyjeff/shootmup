﻿using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public enum AIActionEnum { None, SwitchWeapon, Pickup, Drop, Reload, Shoot, Move };

    public class AI : Player
    {
        public AI() : base()
        {
            ShowDamage = true;
            DisplayHud = false;
            Color = new RGBA() { R = 0, G = 0, B = 255, A = 255 };
        }
        
        public virtual AIActionEnum Action(List<Element> elements, ref float xdelta, ref float ydelta, ref float angle)
        {
            return AIActionEnum.None;
        }

        public virtual void Feedback(AIActionEnum action, object item, bool result)
        {
        }
    }
}
