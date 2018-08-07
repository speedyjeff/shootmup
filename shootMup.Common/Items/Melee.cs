using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Melee : Gun
    {
        public Melee(int range) : base()
        {
            // this is a special gun that enables hand to hand combat
            //  it is not acquirable
            CanAcquire = false;
            IsSolid = false;
            Name = "fists";

            // capacity
            ClipCapacity = 0;

            // damage
            Damage = 10;
            Distance = range;
            Spread = 0;
            Delay = Constants.GlobalClock;
        }
    }
}
