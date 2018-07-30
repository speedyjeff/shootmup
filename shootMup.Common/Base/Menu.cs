using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Menu : Element
    {
        public Menu() : base()
        {
            CanMove = false;
            TakesDamage = false;
            ShowDamage = false;
            IsSolid = false;
            CanAcquire = false;
            Health = 0;
        }
    }
}
