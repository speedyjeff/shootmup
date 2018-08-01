using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Restriction : Background
    {
        public Restriction(int width, int height) : base(width, height)
        {
            Diameter = width + width /2 ;
        }

        public RGBA DangerColor => new RGBA() { R =255, G =127, B =39, A = 255 };

        public float Diameter { get; private set; }

        public override void Draw(IGraphics g)
        {
            //base.Draw(g);
            // draw the zone
            g.Clear(DangerColor);
            // add the safe zone
            g.Ellipse(GroundColor, X - (Diameter / 2), Y - (Diameter / 2), Diameter, Diameter, true);
        }

        public override void Update()
        {
            Diameter -= 10;

            if (Diameter < 10) Diameter = 10;

            base.Update();
        }

        public override float Damage(float x, float y)
        {
            // apply damage if within the circle
            var distance = Collision.DistanceBetweenPoints(X, Y, x, y);
            if (distance > (Diameter * 10)) return 10f;
            else if (distance > (Diameter * 2)) return 1f;
            else if (distance > (Diameter/2)) return 0.1f;
            else return 0;
        }
    }
}
