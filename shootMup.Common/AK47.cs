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
            Width = 50;
            Height = 5;
            Name = "AK47";

            // capacity
            ClipCapacity = 20;

            // damage
            Damage = 15;
            Distance = 500;
            Spread = 0;
            Delay = 100;
        }

        public override string FiredSoundPath() => "media/ak47.wav";

        public override void Draw(IGraphics g)
        {
            g.Rectangle(RGBA.Black, X - Width / 2, Y - Height / 2, Width, Height / 2);
            g.Ellipse(RGBA.Black, X - Width / 2, Y, 10, 10);
            base.Draw(g);
        }
    }
}
