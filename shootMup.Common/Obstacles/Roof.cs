using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Roof : Thing
    {
        public Roof() : base()
        {
            IsSolid = false;
            IsTransparent = true;

            Height = 400;
            Width = 400;
        }

        public override void Draw(IGraphics g)
        {
            g.Rectangle(new RGBA() { R =160, G =113, B =61, A = 255 }, X-(Width/2), Y-(Height/2), Width, Height);
            g.Line(RGBA.Black, X - (Width / 2), Y - (Height / 2), X + (Width / 2), Y + (Height / 2), 5f);
            g.Line(RGBA.Black, X + (Width / 2), Y - (Height / 2), X - (Width / 2), Y + (Height / 2), 5f);
            base.Draw(g);
        }
    }
}
