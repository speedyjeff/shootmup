using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Ammo : Thing
    {
        public Ammo() : base()
        {
            CanAcquire = true;
            Name = "Ammo";
            Width = 25;
            Height = 25;
            Health = 100;
        }

        public override void Draw(IGraphics g)
        {
            var gray = new RGBA() { R = 154, G = 166, B = 173, A = 200 };
            g.Rectangle(gray, X - Width / 2, Y - Height / 2, Width / 3, Height, false);
            g.Rectangle(gray, X - Width / 3, Y - Height / 2, Width / 3, Height);
            g.Rectangle(gray, X + Width / 3, Y - Height / 2, Width / 3, Height, false);
            base.Draw(g);
        }
    }
}
