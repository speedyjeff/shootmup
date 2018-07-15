using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Element
    {
        // id
        public int Id { get; private set; }

        // center
        public float X { get; set; }
        public float Y { get; set; }
        public float Angle { get; set; }

        // bounds (hit box)
        public float Height { get; protected set; }
        public float Width { get; protected set; }

        // attributes
        public float Health { get; set; } = 0;
        public float Sheld { get; set; } = 0;
        public bool CanMove { get; protected set; } = false;
        public bool TakesDamage { get; protected set; } = false;
        public bool IsSolid { get; protected set; } = false;
        public bool CanAcquire { get; protected set; } = false;
        public bool IsTransparent { get; protected set; } = false;
        public string Name { get; protected set; } = "";
        public static bool Debug_DrawHitBox { get; } = false;

        public bool IsDead => (TakesDamage ? Health <= 0 : false);

        public Element()
        {
            Id = GetNextId();
        }

        public virtual void Draw(IGraphics g)
        {
            if (Debug_DrawHitBox) g.Rectangle(RGBA.Black, X-(Width/2), Y-(Height/2), Width, Height, false);
        }

        public void Move(float xDelta, float yDelta)
        {
            X += xDelta;
            Y += yDelta;
        }

        public void ReduceHealth(float damage)
        {
            if (Sheld > 0)
            {
                if (Sheld > damage)
                {
                    Sheld -= damage;
                    return;
                }
                damage -= Sheld;
                Sheld = 0;
            }
            if (Health > damage)
            {
                Health -= damage;
                return;
            }
            Health = 0;
            return;
        }

        #region private
        private static int NextId = 0;
        private static int GetNextId()
        {
            return System.Threading.Interlocked.Increment(ref NextId);
        }
        #endregion
    }
}
