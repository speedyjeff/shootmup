using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Shotgun : Gun
    {
        public Shotgun() : base()
        {
            // looks
            Width = 50;
            Height = 5;
            Name = "Shotgun";

            // capacity
            ClipCapacity = 2;

            // damage
            Damage = 10;
            Distance = 300;
            Spread = 30;
            Delay = 1500;
        }

        public override string FiredSoundPath() => "media/shotgun.wav";

        public override void Draw(IGraphics g)
        {
            g.Rectangle(RGBA.Black, X - Width / 2, Y - Height / 2, Width, Height / 2);
            g.Ellipse(RGBA.Black, X - Width / 2, Y, 10, 10);
            base.Draw(g);
        }
    }
}
