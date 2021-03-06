﻿using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Shotgun : RangeWeapon
    {
        public Shotgun() : base()
        {
            // looks
            Width = 100;
            Height = 20;
            Name = "Shotgun";

            // capacity
            ClipCapacity = 2;

            // damage
            Damage = 30;
            Distance = 300;
            Spread = 30;
            Delay = Constants.GlobalClock * 15;
        }

        public override string FiredSoundPath() => "media/shotgun.wav";
        public override ImageSource Image => new ImageSource(path: "media/shotgun.png");
    }
}
