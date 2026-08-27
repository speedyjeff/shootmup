using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Tree : Obstacle
    {
        public Tree() : base()
        {
            Name = "Tree";
            CanMove = false;
            TakesDamage = true;
            IsSolid = true;
            Health = 100;
            Height = 50;
            Width = 50;
        }

        public override void Draw(IGraphics g)
        {
            ArenaArt.DrawShadow(g, X + 3, Y + Height * .42f, Width * 1.2f, Height * .42f);
            g.Rectangle(ArenaArt.Ink, X - Width * .13f, Y - Height * .05f, Width * .26f, Height * .55f, true, false);
            g.Rectangle(ArenaArt.Rust, X - Width * .08f, Y, Width * .16f, Height * .45f, true, false);
            g.Triangle(ArenaArt.Ink, X, Y - Height * .68f, X - Width * .68f, Y + Height * .2f, X + Width * .68f, Y + Height * .2f, true, false);
            g.Triangle(ArenaArt.Leaf, X, Y - Height * .58f, X - Width * .55f, Y + Height * .13f, X + Width * .55f, Y + Height * .13f, true, false);
            g.Triangle(ArenaArt.LeafLight, X, Y - Height * .52f, X - Width * .38f, Y, X + Width * .08f, Y, true, false);
            base.Draw(g);
        }
    }
}
