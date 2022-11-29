using System;
using System.Collections.Generic;
using System.Text;
using engine.Common;
using engine.Common.Entities;

namespace shootMup.Common
{
    public class AK47 : RangeWeapon
    {
        public AK47() : base()
        {
            // looks
            Width = 100;
            Height = 20;
            Name = "AK47";

            // capacity
            ClipCapacity = 20;

            // damage
            Damage = 15;
            Distance = 500;
            Spread = 0;
            Delay = Constants.GlobalClock;
        }

        public override string FiredSoundPath() => "ak47-2";
        public override ImageSource Image => new ImageSource(path: "ak47");
    }
}
