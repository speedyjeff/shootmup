using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class ShootMPlayer : Player
    {
        public ShootMPlayer() : base()
        {
        }

        public override void Draw(IGraphics g)
        {
            if (Z > Constants.Ground)
            {
                g.DisableTranslation(TranslationOptions.Translation);
                {
                    var body = PlayerColor();
                    ArenaArt.DrawShadow(g, X, Y + Height * .8f, Width * 1.8f, Height * .45f);
                    g.Ellipse(ArenaArt.Ink, X - Width * 1.25f, Y - Height * .8f, Width * 2.5f, Height * .75f, true, false);
                    g.Ellipse(ArenaArt.Coral, X - Width * 1.15f, Y - Height * .72f, Width * 2.3f, Height * .58f, true, false);
                    g.Line(ArenaArt.Ink, X - Width, Y - Height * .35f, X - Width * .3f, Y, 3f);
                    g.Line(ArenaArt.Ink, X + Width, Y - Height * .35f, X + Width * .3f, Y, 3f);
                    DrawBody(g, body);
                }
                g.EnableTranslation();
            }
            else
            {
                var body = PlayerColor();
                ArenaArt.DrawShadow(g, X + 4, Y + Height * .42f, Width * 1.05f, Height * .45f);
                DrawBody(g, body);

                if (Primary != null)
                {
                    ArenaArt.DrawHeldWeapon(g, this, ArenaArt.StyleFor(Primary));
                }

                if (Primary == null)
                {
                    Collision.CalculateLineByAngle(X, Y, Angle, Width / 2, out _, out _, out var x2, out var y2);
                    g.Ellipse(ArenaArt.Ink, x2 - Width / 5, y2 - Width / 5, Width * .4f, Width * .4f, true, false);
                    g.Ellipse(body, x2 - Width / 7, y2 - Width / 7, Width * .28f, Width * .28f, true, false);
                }
            }
        }

        #region private
        private void DrawBody(IGraphics g, RGBA body)
        {
            g.Ellipse(ArenaArt.Ink, X - Width * .55f, Y - Height * .55f, Width * 1.1f, Height * 1.1f, true, false);
            g.Ellipse(body, X - Width * .43f, Y - Height * .43f, Width * .86f, Height * .86f, true, false);

            Collision.CalculateLineByAngle(X, Y, Angle, Width * .3f, out _, out _, out var visorX, out var visorY);
            g.Ellipse(ArenaArt.Ink, visorX - Width * .17f, visorY - Width * .12f, Width * .34f, Width * .24f, true, false);
            g.Ellipse(ArenaArt.Cyan, visorX - Width * .11f, visorY - Width * .07f, Width * .22f, Width * .14f, true, false);

            if (Shield > 0)
            {
                g.Ellipse(ArenaArt.Cyan, X - Width * .68f, Y - Height * .68f, Width * 1.36f, Height * 1.36f, false, false, 4);
            }
        }

        private RGBA PlayerColor()
        {
            if (string.Equals(Name, "You", StringComparison.OrdinalIgnoreCase)) return ArenaArt.Cyan;
            switch (Id % 3)
            {
                case 0: return ArenaArt.Coral;
                case 1: return ArenaArt.Gold;
                default: return ArenaArt.RustLight;
            }
        }
        #endregion
    }
}
