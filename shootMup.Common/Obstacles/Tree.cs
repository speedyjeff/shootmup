using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Tree : Obstacle
    {
        public Tree() : base()
        {
            Name = "Tree";
            CanMove = false;
            TakesDamage = true;
            IsSolid = true;
            Health = 100;
            Height = 50;
            Width = 50;
        }

        public override void Draw(IGraphics g)
        {
            // draw three circles
            g.Ellipse(Green, X+ (Width / 4), Y - (Height / 4), 3*Width / 4, 3*Height / 4);
            g.Ellipse(Green, X, Y + (Height / 4), 3*Width / 4, 3*Height / 4);
            g.Ellipse(Green, X - (Width / 2), Y - (Height / 4), 3 * Width / 4, 3 * Height / 4);

            base.Draw(g);
        }

        #region private
        private static RGBA Green = new RGBA() { R = 32, G = 125, B = 44, A = 255 };
        #endregion
    }
}
