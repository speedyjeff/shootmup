using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Rock : Obstacle
    {
        public Rock() : base()
        {
            CanMove = false;
            TakesDamage = false;
            IsSolid = true;
            Height = 200;
            Width = 200;
        }

        public override void Draw(IGraphics g)
        {
            g.Ellipse(Gray, X - Width / 2, Y - Height / 2, 50, 50);
            g.Ellipse(Gray, X + Width / 2, Y - Height / 2, 50, 50);
            g.Ellipse(Gray, X - Width / 2, Y - Height / 2, Width, Height);
            base.Draw(g);
        }

        #region private
        private readonly RGBA Gray = new RGBA() { R = 157, G = 157, B = 157, A = 255 };
        #endregion
    }
}
