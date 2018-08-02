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
            Timing = MinTimeing;
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
            if (Timing++ > 0)
            {
                Diameter -= 10;
                if (Diameter < 10) Diameter = 10;
            }

            if (Timing >= MaxTiming) Timing = MinTimeing;

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

        #region private
        private int Timing;
        private const int MinTimeing = -20;  // 2 seconds pause
        private const int MaxTiming = 20; // 2 seconds move
        #endregion
    }
}
