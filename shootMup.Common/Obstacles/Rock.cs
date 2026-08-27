using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Rock : Obstacle
    {
        public Rock() : base()
        {
            CanMove = false;
            TakesDamage = false;
            IsSolid = true;
            Height = 200;
            Width = 200;
        }

        public override void Draw(IGraphics g)
        {
            ArenaArt.DrawShadow(g, X + 12, Y + Height * .35f, Width * .9f, Height * .45f);
            g.Polygon(ArenaArt.Ink, new Point[]
            {
                new Point(X - Width * .48f, Y + Height * .3f, 0),
                new Point(X - Width * .34f, Y - Height * .24f, 0),
                new Point(X - Width * .05f, Y - Height * .48f, 0),
                new Point(X + Width * .38f, Y - Height * .22f, 0),
                new Point(X + Width * .48f, Y + Height * .34f, 0)
            }, true, false);
            g.Polygon(ArenaArt.Steel, new Point[]
            {
                new Point(X - Width * .4f, Y + Height * .22f, 0),
                new Point(X - Width * .27f, Y - Height * .18f, 0),
                new Point(X - Width * .03f, Y - Height * .39f, 0),
                new Point(X + Width * .3f, Y - Height * .16f, 0),
                new Point(X + Width * .4f, Y + Height * .25f, 0)
            }, true, false);
            g.Triangle(ArenaArt.SteelLight, X - Width * .27f, Y - Height * .18f, X - Width * .03f, Y - Height * .39f, X + Width * .04f, Y + Height * .12f, true, false);
            g.Line(ArenaArt.Rust, X + Width * .05f, Y + Height * .12f, X + Width * .3f, Y - Height * .16f, 7);
            base.Draw(g);
        }
    }
}
