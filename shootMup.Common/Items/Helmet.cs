using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Helmet : Thing
    {
        public Helmet() : base()
        {
            Shield = 20;
            CanAcquire = true;
            Name = "Helmet";
            Height = 25;
            Width = 25;
        }

        public override void Draw(IGraphics g)
        {
            g.Ellipse(new RGBA() { R = 85, G = 85, B = 85, A = 255 }, X - (Width / 2), Y - (Height / 2), Width, Height);
            base.Draw(g);
        }
    }
}
