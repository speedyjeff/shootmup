using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class BulletTrajectory : Thing
    {
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public float Damage { get; set; }
        public int Duration { get; set; }

        public BulletTrajectory() : base()
        { 
        }

        public override void Draw(IGraphics g)
        {
            // determine the thickness of the bullet by the damage (1..5)
            var thickness = (Damage/ 100f) * 20;
            g.Line(new RGBA() { A = 255, R = 255 }, X1, Y1, X2, Y2, thickness);
            base.Draw(g);
        }
    }
}
