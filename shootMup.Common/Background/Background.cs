using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Background : Element
    {
        public Background(int width, int height) : base()
        {
            CanMove = false;
            TakesDamage = false;
            ShowDamage = false;
            IsSolid = false;
            CanAcquire = false;
            IsTransparent = false;

            // dimensions
            Width = width;
            Height = height;

            // center
            X = width / 2;
            Y = height / 2;
        }

        public RGBA GroundColor => new RGBA() { R = 70, G = 169, B = 52, A = 255 };

        public override void Draw(IGraphics g)
        {
            g.Clear(GroundColor);
            base.Draw(g);
        }

        public virtual void Update()
        {
        }

        public virtual float Pace(float x, float y)
        {
            return 2;
        }

        public virtual float Damage(float x, float y)
        {
            return 0;
        }
    }
}
