using engine.Common;
using engine.Common.Entities;
using engine.Common.Entities.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Bots
{
    public class ShootMAI : AI
    {
        public ShootMAI()
        {
            Color = new RGBA() { R = 0, G = 0, B = 255, A = 255 };
        }

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
                if (Shield > 0) g.Ellipse(new RGBA() { R = 85, G = 85, B = 85, A = 255 }, X - (Width / 4), Y - (Height / 4), (Width / 2), (Width / 2));

                if (Primary == null)
                {
                    // draw a fist
                    float x1, y1, x2, y2;
                    Collision.CalculateLineByAngle(X, Y, Angle, Width / 2, out x1, out y1, out x2, out y2);
                    g.Ellipse(Color, x2, y2, Width / 3, Width / 3);
                }
            }
        }
    }
}
