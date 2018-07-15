using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Player : Element
    {
        public Player() : base()
        {
            CanMove = true;
            TakesDamage = true;
            IsSolid = true;
            Health = 100;
            Sheld = 0;

            // hit box
            Height = 100;
            Width = 100;
        }

        // weapons
        public Gun Primary { get; private set; }
        public Gun Secondary { get; private set; }

        public override void Draw(IGraphics g)
        {
            g.RotateTransform(Angle);
            {
                if (Primary != null) g.Rectangle(RGBA.Black, X - 10, Y - Height, 20, Height);
                g.Ellipse(new RGBA() { R = 255, G = 0, B = 0, A = 255 }, X - (Width / 2), Y - (Width / 2), Width, Height);
                if (Sheld > 0) g.Ellipse(new RGBA() { R = 85, G = 85, B = 85, A = 255 }, X - 25, Y - 25, 50, 50);
            }
            g.RotateTransform(-1*Angle);
            base.Draw(g);
        }

        public bool Take(Element item)
        {
            if (item is Gun)
            {
                if (Primary != null && Secondary == null) Secondary = Primary;
                Primary = item as Gun;
                return true;
            }
            else if (item is Ammo)
            {
                if (Primary != null)
                {
                    Primary.AddAmmo((int)item.Health);
                    return true;
                }
            }
            else if (item is Helmet)
            {
                if (Sheld < Constants.MaxSheld)
                {
                    Sheld += item.Sheld;
                    if (Sheld > Constants.MaxSheld) Sheld = Constants.MaxSheld;
                    return true;
                }
            }
            else if (item is Bandage)
            {
                if (Health < Constants.MaxHealth)
                {
                    Health += item.Health;
                    if (Health > Constants.MaxHealth) Health = Constants.MaxHealth;
                    return true;
                }
            }
            else throw new Exception("Unknow item : " + item.GetType());

            return false;
        }

        public GunStateEnum Shoot()
        {
            // check if we have a primary weapon
            if (Primary == null) return GunStateEnum.None;
            // check if there are rounds
            if (!Primary.CanShoot()) return GunStateEnum.NeedsReload;

            bool fired = Primary.Shoot();
            if (fired) return GunStateEnum.Fired;
            else throw new Exception("Failed to fire");
        }

        public GunStateEnum Reload()
        {
            // check if we have a primary weapon
            if (Primary == null) return GunStateEnum.None;
            // check if there are rounds
            if (!Primary.CanReload()) return GunStateEnum.NoRounds;

            bool reload = Primary.Reload();
            if (reload) return GunStateEnum.Reloaded;
            else throw new Exception("Failed to reload");
        }
    }
}
