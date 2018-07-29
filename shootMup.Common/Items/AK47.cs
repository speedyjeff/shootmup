using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class AK47 : Gun
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

        public override string FiredSoundPath() => "media/ak47.2.wav";
        public override string ImagePath => "media/ak47.png";
    }
}
