using engine.Common;
using engine.Common.Entities;

namespace shootMup.Common
{
    public class ArenaAmmo : Ammo
    {
        public ArenaAmmo()
        {
            ShowDefaultDrawing = false;
            Width = 34;
            Height = 34;
        }

        public override void Draw(IGraphics g)
        {
            ArenaArt.DrawShadow(g, X + 3, Y + 5, Width, Height / 2);
            g.Rectangle(ArenaArt.Ink, X - 16, Y - 13, 32, 26, true, false);
            g.Rectangle(ArenaArt.Gold, X - 12, Y - 9, 24, 18, true, false);
            g.Rectangle(ArenaArt.Rust, X - 8, Y - 7, 5, 14, true, false);
            g.Rectangle(ArenaArt.Rust, X + 3, Y - 7, 5, 14, true, false);
            base.Draw(g);
        }
    }

    public class ArenaHealth : Health
    {
        public ArenaHealth()
        {
            ShowDefaultDrawing = false;
            Width = 46;
            Height = 38;
        }

        public override void Draw(IGraphics g)
        {
            ArenaArt.DrawShadow(g, X + 3, Y + 6, Width, Height / 2);
            g.Rectangle(ArenaArt.Ink, X - 23, Y - 18, 46, 36, true, false);
            g.Rectangle(ArenaArt.Sand, X - 19, Y - 14, 38, 28, true, false);
            g.Rectangle(ArenaArt.Coral, X - 4, Y - 11, 8, 22, true, false);
            g.Rectangle(ArenaArt.Coral, X - 12, Y - 3, 24, 8, true, false);
            base.Draw(g);
        }
    }

    public class ArenaShield : Shield
    {
        public ArenaShield()
        {
            ShowDefaultDrawing = false;
            Width = 38;
            Height = 42;
        }

        public override void Draw(IGraphics g)
        {
            ArenaArt.DrawShadow(g, X + 3, Y + 7, Width, Height / 2);
            g.Ellipse(ArenaArt.Ink, X - 19, Y - 21, 38, 42, true, false);
            g.Ellipse(ArenaArt.Cyan, X - 15, Y - 17, 30, 34, true, false);
            g.Triangle(ArenaArt.Steel, X, Y + 13, X - 10, Y - 8, X + 10, Y - 8, true, false);
            base.Draw(g);
        }
    }
}
