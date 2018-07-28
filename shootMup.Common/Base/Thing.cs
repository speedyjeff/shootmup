﻿using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Thing : Element
    {
        public Thing() : base()
        {
            CanMove = false;
            TakesDamage = false;
            ShowDamage = true;
            IsSolid = true;
            Health = 0;
        }
    }
}
