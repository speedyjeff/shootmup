using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Pistol : RangeWeapon
    {
        public override string FiredSoundPath() => "pistol";
        public override ImageSource Image => new ImageSource(path: "pistol");

        public Pistol() : base()
        {
            // looks
            Width = 100;
            Height = 20;
            Name = "Pistol";

            // capacity
            ClipCapacity = 6;

            // damage
            Damage = 20;
            Distance = 600;
            Spread = 0;
            Delay = Constants.GlobalClock * 10;
        }
    }
}
