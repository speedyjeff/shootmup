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
            DisplayHud = true;
        }

        public bool DisplayHud { get; set; }

        public override void Draw(IGraphics g)
        {
            // draw player
            if (Z > Constants.Ground)
            {
                g.DisableTranslation(true /* nonScaledTranslation */);
                {
                    // we are in a parachute
                    g.Ellipse(Color, X - (Width / 2), Y - (Height / 2), Width, Height);
                    g.Rectangle(new RGBA() { R = 146, G = 27, B = 167, A = 255 }, X - Width, Y, Width * 2, Height / 2, true);
                    g.Line(RGBA.Black, X - Width, Y, X, Y - (Height / 4), 5f);
                    g.Line(RGBA.Black, X, Y - (Height / 4), X + Width, Y, 5f);
                }
                g.EnableTranslation();
            }
            else
            {
                // on ground
                if (Primary != null)
                {
                    // draw a line in the direction of the weapon
                    float x1, y1, x2, y2;
                    Collision.CalculateLineByAngle(X, Y, Angle, Width, out x1, out y1, out x2, out y2);
                    g.Line(RGBA.Black, x1, y1, x2, y2, 10);
                }
                g.Ellipse(Color, X - (Width / 2), Y - (Height / 2), Width, Height);
                if (Shield > 0) g.Ellipse(new RGBA() { R = 85, G = 85, B = 85, A = 255 }, X - (Width/4), Y - (Height/4), (Width / 2), (Width / 2));

                if (Primary == null)
                {
                    // draw a fist
                    float x1, y1, x2, y2;
                    Collision.CalculateLineByAngle(X, Y, Angle, Width / 2, out x1, out y1, out x2, out y2);
                    g.Ellipse(Color, x2, y2, Width/3, Width/3);
                }
            }

            // draw HUD

            if (DisplayHud)
            {
                g.DisableTranslation();
                {
                    // health
                    g.Rectangle(new RGBA() { G = 255, A = 255 }, (g.Width / 4), g.Height - 80, (Health / Constants.MaxHealth) * (g.Width / 2), 20, true);
                    g.Rectangle(RGBA.Black, g.Width / 4, g.Height - 80, g.Width / 2, 20, false);

                    // shield
                    g.Rectangle(new RGBA() { R = 255, G = 255, A = 255 }, g.Width / 4, g.Height - 90, (Shield / Constants.MaxShield) * (g.Width / 4), 10, true);
                    g.Rectangle(RGBA.Black, g.Width / 4, g.Height - 90, g.Width / 4, 10, false);

                    // primary weapon
                    g.Rectangle(RGBA.Black, g.Width - 100, g.Height / 6, 60, 30, false);
                    if (Primary != null && Primary is RangeWeapon)
                    {
                        g.Text(RGBA.Black, g.Width - 100, (g.Height / 6) - 25, String.Format("{0}/{1}", (Primary as RangeWeapon).Clip, (Primary as RangeWeapon).Ammo));
                        g.Text(RGBA.Black, g.Width - 100, (g.Height / 6) + 2, (Primary as RangeWeapon).Name);
                    }

                    // secondary weapon
                    g.Rectangle(RGBA.Black, g.Width - 100, (g.Height / 4) + 10, 60, 30, false);
                    if (Secondary != null && Secondary.Length >= 1 && Secondary[0] is RangeWeapon)
                    {
                        g.Text(RGBA.Black, g.Width - 100, (g.Height / 4) - 15, String.Format("{0}/{1}", (Secondary[0] as RangeWeapon).Clip, (Secondary[0] as RangeWeapon).Ammo));
                        g.Text(RGBA.Black, g.Width - 100, (g.Height / 4) + 12, (Secondary[0] as RangeWeapon).Name);
                    }
                }
                g.EnableTranslation();
            }
        }
    }
}
