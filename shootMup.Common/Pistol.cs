using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Pistol : Gun
    {
        public override string FiredSoundPath() => "media/pistol.wav";

        public Pistol() : base()
        {
            // looks
            Width = 50;
            Height = 5;
            Name = "Pistol";

            // capacity
            ClipCapacity = 6;

            // damage
            Damage = 25;
            Distance = 300;
            Spread = 0;
        }

        public override void Draw(IGraphics g)
        {
            g.Rectangle(RGBA.Black, X - Width / 2, Y - Height / 2, Width, Height / 2);
            g.Ellipse(RGBA.Black, X - Width/2, Y, 10, 10);
            base.Draw(g);
        }
    }
}
