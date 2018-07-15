using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using shootMup.Common;

namespace shootMup
{
    public class World
    {
        public World(IGraphics surface, ISounds sounds)
        {
            // init
            Bullets = new List<BulletTrajectory>();
            ZoomFactor = 0;

            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(TranslateCoordinates);

            // sounds
            Sounds = sounds;

            // TODO - initialize based on on disk artifact

            // initialize the world
            Height = 10000;
            Width = 10000;
            WindowX = 200;
            WindowY = 200;
            All = new Dictionary<int, Element>();
            Player = new Player() { X = WindowX, Y = WindowY };
            // player
            All.Add(Player.Id, Player);
            // items
            var roof = new Roof() { X = 800, Y = 800 };
            foreach (var elem in new Element[] {
                                    new Tree() { X = 125, Y = 125 },
                                    new Wall(WallDirection.Horiztonal, Width, 20) { X = Width / 2, Y = 10 },
                                    new Wall(WallDirection.Vertical, Height, 20) { X = 10, Y = Height / 2 },
                                    new Wall(WallDirection.Horiztonal, Width, 20) { X = Width / 2, Y = Height - 10 },
                                    new Wall(WallDirection.Vertical, Height, 20) { X = Width - 10, Y = Height / 2 },
                                    new Pistol() { X = 350, Y = 200 },
                                    new AK47() { X = 350, Y = 400 },
                                    new Shotgun() { X = 350, Y = 350 },
                                    new Ammo() { X = 350, Y = 150 },
                                    new Ammo() { X = 400, Y = 150 },
                                    new Ammo() { X = 450, Y = 150 },
                                    new Helmet() { X = 200, Y = 300 },
                                    new Rock() { X = 750, Y = 250 },
                                    // a hut
                                    new Wall(WallDirection.Vertical, roof.Height/2, 20) { X = roof.X - roof.Width / 2 + 40, Y = roof.Y - 80  },
                                    new Wall(WallDirection.Vertical, roof.Height/2, 20) { X = roof.X + roof.Width / 2 - 40, Y = roof.Y - 80 },
                                    new Wall(WallDirection.Horiztonal, roof.Width-40, 20) { X = roof.X, Y = roof.Y + roof.Height / 2 - 40 },
                                    roof,
                                    new Bandage() { X = 350, Y = 800}
            })
            {
                All.Add(elem.Id, elem);
            }

            // initially render all the elements
            Paint();
        }

        public void Paint()
        {
            // draw all the elements
            Surface.Clear(new RGBA() { R = 70, G = 169, B = 52, A = 255 });

            // add any bullets
            var toremove = new List<BulletTrajectory>();
            foreach (var b in Bullets)
            {
                b.Draw(Surface);
                b.Duration--;
                if (b.Duration < 0) toremove.Add(b);
            }
            foreach (var b in toremove)
            {
                Bullets.Remove(b);
            }

            // TODO find a better way to avoid drawing all the elements
            foreach (var elem in All.Values)
            {
                if (elem.Id == Player.Id) continue;
                if (elem.IsDead) continue;
                if (elem.IsTransparent)
                {
                    float x11 = (Player.X) - (Player.Width / 2);
                    float y11 = (Player.Y) - (Player.Height / 2);
                    float x12 = (Player.X) + (Player.Width / 2);
                    float y12 = (Player.Y) + (Player.Height / 2);
                    // if the player is intersecting with this item, then do not display it
                    if (IntersectingRectangles(x11, y11, x12, y12, elem) != null) continue;
                }
                elem.Draw(Surface);
            }
            // draw the player last
            Player.Draw(Surface);
        }

        public void KeyPress(char key)
        {
            float xdelta = 0;
            float ydelta = 0;
            float speed = Constants.Speed * SpeedFactor;

            switch (key)
            {
                // move
                case Constants.Down:
                case Constants.DownArrow:
                    ydelta = speed;
                    break;
                case Constants.Left:
                case Constants.LeftArrow:
                    xdelta = -1* speed;
                    break;
                case Constants.Right:
                case Constants.RightArrow:
                    xdelta = speed;
                    break;
                case Constants.Up:
                case Constants.UpArrow:
                    ydelta = -1* speed;
                    break;

                case Constants.Switch:
                    SwitchWeapon();
                    break;

                case Constants.Pickup:
                    Pickup();
                    break;

                case Constants.Drop:
                    Drop();
                    break;

                case Constants.Reload:
                    Reload();
                    break;

                case Constants.LeftMouse:
                    Shoot();
                    break;

                case Constants.RightMouse:
                    // use the mouse to move in the direction of the angle
                    float r = (Player.Angle % 90) / 90f;
                    xdelta = speed * r;
                    ydelta = speed * (1 - r);
                    if (Player.Angle > 0 && Player.Angle < 90) ydelta *= -1;
                    else if (Player.Angle > 180 && Player.Angle <= 270) xdelta *= -1;
                    else if (Player.Angle > 270) { ydelta *= -1; xdelta *= -1; }
                    break;
            }

            if (xdelta != 0 || ydelta != 0)
            {
                if (!Move(xdelta, ydelta))
                {
                    // TODO may want to move back a bit in the opposite direction
                }
            }
        }

        public void Angle(float angle)
        {
            if (angle < 0 || angle > 360) throw new Exception("Invalid angle : " + angle);
            Player.Angle = angle;
        }

        public void Zoom(float delta)
        {
            ZoomFactor += delta;
        }

        #region private
        private IGraphics Surface;
        private Dictionary<int, Element> All;
        private Player Player;
        private int Width;
        private int Height;
        private float WindowX;
        private float WindowY;
        private int SpeedFactor = 2;
        private float ZoomFactor;
        private const string NothingSoundPath = "media/nothing.wav";
        private const string PickupSoundPath = "media/pickup.wav";
        private ISounds Sounds;
        private List<BulletTrajectory> Bullets;

        private bool TranslateCoordinates(float x, float y, float width, float height, out float tx, out float ty, out float twidth, out float theight)
        {
            // translate the x and y based on the current window
            // Surface.Width & Suface.Height are the current windows width & height
            float windowHWidth = Surface.Width / 2.0f;
            float windowHHeight = Surface.Height / 2.0f;

            float x1 = WindowX - windowHWidth;
            float y1 = WindowY - windowHHeight;
            float x2 = WindowX + windowHWidth;
            float y2 = WindowY + windowHHeight;

            tx = ty = twidth = theight = 0;

            // check if inside the window
            if (x < (x1-width) || x > (x2+width)) return false;
            if (y < (y1-height) || y > (y2+height)) return false;

            // now translate to the window
            tx = x - x1;
            ty = y - y1;

            // scale the input
            // TODO!
            twidth = width;
            theight = height;

            return true;
        }

        private bool HasCollision(float xdelta, float ydelta)
        {
            // object 1 will be player
            float x11 = (Player.X + xdelta) - (Player.Width / 2);
            float y11 = (Player.Y + ydelta) - (Player.Height / 2);
            float x12 = (Player.X + xdelta) + (Player.Width / 2);
            float y12 = (Player.Y + ydelta) + (Player.Height / 2);

            return IntersectingRectangles(x11, y11, x12, y12) != null;
        }

        private Element IntersectingRectangles()
        {
            // object 1 will be player
            float x11 = (Player.X) - (Player.Width / 2);
            float y11 = (Player.Y) - (Player.Height / 2);
            float x12 = (Player.X) + (Player.Width / 2);
            float y12 = (Player.Y) + (Player.Height / 2);

            return IntersectingRectangles(x11, y11, x12, y12, true);
        }

        private Element IntersectingRectangles(float x11, float y11, float x12, float y12, bool considerAquireable = false)
        {
            // check collisions
            foreach (var elem in All.Values)
            {
                if (elem.Id == Player.Id) continue;
                if (elem.IsDead) continue;
                if (!considerAquireable)
                {
                    if (!elem.IsSolid || elem.CanAcquire) continue;
                }
                else
                {
                    if (!elem.CanAcquire) continue;
                }

                // check if these collide
                var item = IntersectingRectangles(x11, y11, x12, y12, elem);
                if (item != null) return item;
            }

            return null;
        }

        private Element IntersectingRectangles(float x11, float y11, float x12, float y12, Element elem)
        {
            float x21 = elem.X - (elem.Width / 2);
            float y21 = elem.Y - (elem.Height / 2);
            float x22 = elem.X + (elem.Width / 2);
            float y22 = elem.Y + (elem.Height / 2);

            if (x21 > x11 && x21 < x12)
            {
                if (y21 > y11 && y21 < y12) return elem;
                if (y21 < y11 && y22 > y12) return elem;
                if (y22 > y11 && y22 < y12) return elem;
            }
            else if (x22 > x11 && x22 < x12)
            {
                if (y22 > y11 && y22 < y12) return elem;
                if (y21 < y11 && y22 > y12) return elem;
                if (y21 > y11 && y21 < y12) return elem;
            }
            else if ((y21 > y11 && y21 < y12) || (y22 > y11 && y22 < y12))
            {
                if (x21 < x11 && x22 > x12) return elem;
            }
            else if (y21 < y11 && x21 < x11 && y22 > y12 && x22 > x12) return elem;

            return null;
        }

        private Element IntersectingLine(float x11, float y11, float x12, float y12)
        {
            // must ensure to find the closest object that intersects
            Element item = null;

            // check collisions
            foreach (var elem in All.Values)
            {
                bool collision = false;
                if (elem.Id == Player.Id) continue;
                if (!elem.IsSolid || elem.CanAcquire) continue;
              
                // check if these would collide if moved
                float x21 = elem.X - (elem.Width / 2);
                float y21 = elem.Y - (elem.Height / 2);
                float x22 = elem.X + (elem.Width / 2);
                float y22 = elem.Y + (elem.Height / 2);

                // TODO! This only checks a diagnoal line and is incomplete

                // https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect
                if (CalcCcw(x11, y11, x21, y21, x22, y22) != CalcCcw(x12, y12, x21, y21, x22, y22)
                    && CalcCcw(x11, y11, x12, y12, x21, y21) != CalcCcw(x11, y11, x12, y12, x22, y22))
                    collision = true;

                if (collision)
                {
                    if (item == null) item = elem;
                    else
                    {
                        //throw new Exception("NYI - multiple collisions");
                        System.Diagnostics.Debug.WriteLine("Multiple objects found on collision");
                    }
                }
            }

            return item;
        }

        private bool CalcCcw(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            return (y3 - y1) * (x2 - x1) > (y2 - y1) * (x3 - x1);
        }

        public bool ApplyBulletTrajectory(Gun gun, float x, float y, float angle)
        {
            float x1 = x;
            float y1 = y;
            float a = (float)Math.Cos(angle * Math.PI / 180) * gun.Distance;
            float o = (float)Math.Sin(angle * Math.PI / 180) * gun.Distance;
            float x2 = x1 + o;
            float y2 = y1 - a;

            Bullets.Add(new BulletTrajectory()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Damage = gun.Damage,
                Duration = Constants.BulletDuration
            });

            // determine damage
            var elem = IntersectingLine(x1, y1, x2, y2);

            if (elem != null && elem.TakesDamage)
            {
                elem.ReduceHealth(gun.Damage);
                return true;
            }

            return false;
        }

        // player actions
        private bool Move(float xdelta, float ydelta)
        {
            if (Player.IsDead) return false;

            // check for a collision first
            if (HasCollision(xdelta, ydelta))
            {
                return false;
            }

            // move the player
            Player.Move(xdelta, ydelta);

            // move the screen
            WindowX += xdelta;
            WindowY += ydelta;

            return true;
        }

        private bool Pickup()
        {
            // see if we are over an item
            Element item = IntersectingRectangles();

            if (item != null)
            {
                // pickup the item
                if (Player.Take(item))
                {
                    // remove the item from the playing field
                    // TODO! dangeour remove!
                    All.Remove(item.Id);

                    // play sound
                    Sounds.Play(PickupSoundPath);

                    return true;
                }
            }

            return false;
        }

        public void Reload()
        {
            var state = Player.Reload();
            switch (state)
            {
                case GunStateEnum.Reloaded: Sounds.Play(Player.Primary.ReloadSoundPath()); break;
                case GunStateEnum.None:
                case GunStateEnum.NoRounds: Sounds.Play(NothingSoundPath); break;
                default: throw new Exception("Unknown GunState : " + state);
            }
        }

        public void Shoot()
        {
            var state = Player.Shoot();

            // apply state change
            switch(state)
            {
                case GunStateEnum.Fired:
                    // show the bullet
                    ApplyBulletTrajectory(Player.Primary, Player.X, Player.Y, Player.Angle);
                    if (Player.Primary.Spread != 0)
                    {
                        ApplyBulletTrajectory(Player.Primary, Player.X, Player.Y, Player.Angle - (Player.Primary.Spread/2));
                        ApplyBulletTrajectory(Player.Primary, Player.X, Player.Y, Player.Angle + (Player.Primary.Spread / 2));
                    }

                    // play sound
                    Sounds.Play(Player.Primary.FiredSoundPath());
                    break;

                // just play the sound
                case GunStateEnum.NeedsReload: Sounds.Play(Player.Primary.EmptySoundPath()); break;
                case GunStateEnum.None: Sounds.Play(NothingSoundPath); break;
                default: throw new Exception("Unknown GunState : " + state);
            }
        }

        private void SwitchWeapon()
        {
            Player.SwitchWeapon();
        }

        private void Drop()
        {
            var item = Player.DropPrimary();

            if (item != null)
            {
                item.X = Player.X;
                item.Y = Player.Y;
                All.Add(item.Id, item);
            }
        }

        #endregion
    }
}
