using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace shootMup.Common
{
    public enum GunStateEnum { None, NeedsReload, Fired, NoRounds, Reloaded };

    public class Gun : Thing
    {
        // ammo
        public int Ammo { get; private set; }
        public int ClipCapacity { get; protected set; }
        public int Clip { get; private set; }

        // damage
        public int Distance { get; protected set; }
        public float Spread { get; protected set; } // degrees
        public int Damage { get; protected set; }
        public int Delay { get; protected set; } // ms

        public virtual string EmptySoundPath() => "media/empty.wav";
        public virtual string ReloadSoundPath() => "media/reload.wav";
        public virtual string FiredSoundPath() => "media/pistol.wav";
        public virtual string ImagePath => "media/pistol.png";

        public Gun() : base()
        {
            CanAcquire = true;
            IsSolid = false;
            Shotdelay = new Stopwatch();
            ResetShotdelay();
        }

        public void AddAmmo(int ammo)
        {
            if (ammo > 0)
            {
                Ammo += ammo;
            }
        }

        public void ChangeClipCapacity(int capacity)
        {
            ClipCapacity += capacity;
            if (ClipCapacity <= 0) throw new Exception("Must have a positive clip capacity");
        }

        public virtual bool CanReload()
        {
            if (Ammo <= 0) return false;
            if (Clip >= ClipCapacity) return false;
            return true;
        }

        public virtual bool Reload()
        {
            if (!CanReload()) return false;
            int delta = ClipCapacity - Clip;
            if (delta > Ammo) delta = Ammo;
            Clip += delta;
            Ammo -= delta;
            ResetShotdelay();
            return true;
        }

        public virtual bool CanShoot()
        {
            return (Clip > 0) && CheckShotdelay();
        }

        public virtual bool Shoot()
        {
            if (!CanShoot()) return false;
            Clip--;
            ResetShotdelay();
            return true;
        }

        #region private
        private Stopwatch Shotdelay;

        private void ResetShotdelay()
        {
            Shotdelay.Stop(); Shotdelay.Reset(); Shotdelay.Start();
        }

        private bool CheckShotdelay()
        {
            return (Shotdelay.ElapsedMilliseconds > Delay);
        }

        public override void Draw(IGraphics g)
        {
            g.Image(ImagePath, X - Width / 2, Y - Height / 2);
            base.Draw(g);
        }
        #endregion
    }
}
