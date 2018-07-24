﻿using System;
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
            Health = 50;
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
            // draw player
            g.RotateTransform(Angle);
            {
                if (Primary != null) g.Rectangle(RGBA.Black, X - 2, Y - Height, 4, Height);
                g.Ellipse(new RGBA() { R = 255, G = 0, B = 0, A = 255 }, X - (Width / 2), Y - (Width / 2), Width, Height);
                if (Sheld > 0) g.Ellipse(new RGBA() { R = 85, G = 85, B = 85, A = 255 }, X - 25, Y - 25, 50, 50);
            }
            g.RotateTransform(-1*Angle);

            // draw HUD

            g.DisableTranslation();
            {
                // health
                g.Rectangle(new RGBA() { G = 255, A = 255 }, (g.Width / 4), g.Height - 80, (Health / Constants.MaxHealth) * (g.Width / 2), 20, true);
                g.Rectangle(RGBA.Black, g.Width / 4, g.Height - 80, g.Width / 2, 20, false);

                // sheld
                g.Rectangle(new RGBA() { R = 255, G = 255, A = 255 }, g.Width / 4, g.Height - 90, (Sheld / Constants.MaxSheld) * (g.Width / 4), 10, true);
                g.Rectangle(RGBA.Black, g.Width / 4, g.Height - 90, g.Width / 4, 10, false);

                // primary weapon
                g.Rectangle(RGBA.Black, g.Width - 100, g.Height / 6, 60, 30, false);
                if (Primary != null)
                {
                    g.Text(RGBA.Black, g.Width - 100, (g.Height / 6) - 25, String.Format("{0}/{1}", Primary.Clip, Primary.Ammo));
                    g.Text(RGBA.Black, g.Width - 100, (g.Height / 6) + 2, Primary.Name);
                }

                // secondary weapon
                g.Rectangle(RGBA.Black, g.Width - 100, (g.Height / 4) + 10, 60, 30, false);
                if (Secondary != null)
                {
                    g.Text(RGBA.Black, g.Width - 100, (g.Height / 4) - 15, String.Format("{0}/{1}", Secondary.Clip, Secondary.Ammo));
                    g.Text(RGBA.Black, g.Width - 100, (g.Height / 4) + 12, Secondary.Name);
                }
            }
            g.EnableTranslation();

            base.Draw(g);
        }

        public bool Take(Element item)
        {
            if (item is Gun)
            {
                if (Primary != null && Secondary == null)
                {
                    Secondary = Primary;
                    Primary = null;
                }
                if (Primary == null)
                {
                    Primary = item as Gun;
                    return true;
                }
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
            // check if there is a round in the clip
            int rounds;
            Primary.RoundsInClip(out rounds);
            if (rounds <= 0)
            {
                if (Primary.HasAmmo()) return GunStateEnum.NeedsReload;
                else return GunStateEnum.NoRounds;
            }
            // check if gun ready to fire
            if (!Primary.CanShoot()) return GunStateEnum.LoadingRound;

            bool fired = Primary.Shoot();
            if (fired) return GunStateEnum.Fired;
            else throw new Exception("Failed to fire");
        }

        public GunStateEnum Reload()
        {
            // check if we have a primary weapon
            if (Primary == null) return GunStateEnum.None;
            if (!Primary.HasAmmo()) return GunStateEnum.NoRounds;
            // check if there are rounds
            if (Primary.RoundsInClip(out int rounds)) return GunStateEnum.FullyLoaded;

            bool reload = Primary.Reload();
            if (reload) return GunStateEnum.Reloaded;
            else throw new Exception("Failed to reload");
        }

        public bool SwitchWeapon()
        {
            var tmp = Primary;
            Primary = Secondary;
            Secondary = tmp;
            return (Primary != null || Secondary != null);
        }

        public Element DropPrimary()
        {
            if (Primary == null) return null;
            var tmp = Primary;
            Primary = null;
            return tmp;
        }
    }
}