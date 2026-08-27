using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Roof : Obstacle
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
            ArenaArt.DrawShadow(g, X + 12, Y + 16, Width, Height);
            g.Rectangle(ArenaArt.Ink, X - Width / 2, Y - Height / 2, Width, Height, true, false);
            g.Rectangle(ArenaArt.Steel, X - Width / 2 + 10, Y - Height / 2 + 10, Width - 20, Height - 20, true, false);
            g.Line(ArenaArt.SteelLight, X - Width / 2 + 18, Y - Height / 4, X + Width / 2 - 18, Y - Height / 4, 6);
            g.Line(ArenaArt.SteelLight, X - Width / 2 + 18, Y + Height / 4, X + Width / 2 - 18, Y + Height / 4, 6);
            g.Line(ArenaArt.Rust, X - Width / 4, Y - Height / 2 + 18, X - Width / 4, Y + Height / 2 - 18, 8);
            g.Line(ArenaArt.Rust, X + Width / 4, Y - Height / 2 + 18, X + Width / 4, Y + Height / 2 - 18, 8);
            g.Rectangle(ArenaArt.Ink, X - 42, Y - 36, 84, 72, true, false);
            g.Rectangle(ArenaArt.Cyan, X - 32, Y - 26, 64, 52, true, false);
            g.Line(ArenaArt.Ink, X - 22, Y, X + 22, Y, 7);
            g.Line(ArenaArt.Ink, X, Y - 18, X, Y + 18, 7);
            base.Draw(g);
        }
    }
}
