using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Pistol : Gun
    {
        public override string FiredSoundPath() => "media/pistol.wav";
        public override string ImagePath => "media/pistol.png";

        public Pistol() : base()
        {
            // looks
            Width = 100;
            Height = 20;
            Name = "Pistol";

            // capacity
            ClipCapacity = 6;

            // damage
            Damage = 25;
            Distance = 600;
            Spread = 0;
            Delay = 1000;
        }
    }
}
