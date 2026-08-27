using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public enum WallDirection { Horiztonal, Vertical };

    public class Wall : Obstacle
    {
        public Wall() : base()
        {
        }

        public Wall(WallDirection dir, float length, float thickness) : base()
        {
            IsSolid = true;
            if (dir == WallDirection.Horiztonal)
            {
                Width = length;
                Height = thickness;
            }
            else if (dir == WallDirection.Vertical)
            {
                Width = thickness;
                Height = length;
            }
            else throw new Exception("Unknown wall direction : " + dir);
        }

        public override void Draw(IGraphics g)
        {
            ArenaArt.DrawShadow(g, X + 5, Y + 7, Width, Height);
            g.Rectangle(ArenaArt.Ink, X - Width / 2, Y - Height / 2, Width, Height, true, false);
            g.Rectangle(ArenaArt.Rust, X - Width / 2 + 4, Y - Height / 2 + 4, Width - 8, Height - 8, true, false);

            if (Width > Height)
            {
                for (var offset = -Width / 2 + 35; offset < Width / 2; offset += 55)
                {
                    g.Line(ArenaArt.Sand, X + offset, Y - Height / 2 + 5, X + offset, Y + Height / 2 - 5, 3);
                }
            }
            else
            {
                for (var offset = -Height / 2 + 35; offset < Height / 2; offset += 55)
                {
                    g.Line(ArenaArt.Sand, X - Width / 2 + 5, Y + offset, X + Width / 2 - 5, Y + offset, 3);
                }
            }
            base.Draw(g);
        }
    }
}
