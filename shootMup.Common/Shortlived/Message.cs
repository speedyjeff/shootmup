using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Message : EphemerialElement
    {
        public string Text { get; set; }

        public override void Draw(IGraphics g)
        {
            g.Text(RGBA.Black, X - (Text.Length), Y, Text);
            base.Draw(g);
        }
    }
}
