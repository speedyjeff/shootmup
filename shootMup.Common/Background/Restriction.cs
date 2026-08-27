using System;
using System.Collections.Generic;
using System.Text;
using engine.Common;
using engine.Common.Entities;

namespace shootMup.Common
{
    public class Restriction : Background
    {
        public Restriction() : base()
        {
        }

        public Restriction(int width, int height) : base(width, height)
        {
            // base config
            GroundColor = new RGBA() { R = 91, G = 132, B = 87, A = 255 };
            X = (width / 2);
            Y = (height / 2);
            Width = width;
            Height = height;

            // setup diameter
            Diameter = width + height;
            DiameterDecrease = (int)Width / 500;

            // establish timing (Update runs every GlobalClock ms)
            var callsPerSecond = 1000 / Constants.GlobalClock;
            MinTiming = -1 * callsPerSecond;
            MaxTiming = 2 * callsPerSecond;
            Timing = MinTiming;
        }

        public RGBA DangerColor => new RGBA() { R = 116, G = 40, B = 53, A = 255 };

        public float Diameter { get; set; }

        public override void Draw(IGraphics g)
        {
            // draw the zone
            g.Clear(DangerColor);
            g.Ellipse(GroundColor, X - (Diameter / 2), Y - (Diameter / 2), Diameter, Diameter, true);
            g.Ellipse(ArenaArt.Gold, X - (Diameter / 2), Y - (Diameter / 2), Diameter, Diameter, false, false, 18);
            g.Ellipse(ArenaArt.Sand, X - (Diameter / 2) + 24, Y - (Diameter / 2) + 24, Diameter - 48, Diameter - 48, false, false, 5);
        }

        public override void Update()
        {
            if (Timing++ > 0)
            {
                Diameter -= DiameterDecrease;
                if (Diameter < DiameterDecrease) Diameter = DiameterDecrease;
            }

            if (Timing >= MaxTiming) Timing = MinTiming;

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
        private int MinTiming = -20;  // 2 seconds pause
        private int MaxTiming = 20; // 2 seconds move
        private int DiameterDecrease = 10;
        #endregion
    }
}
