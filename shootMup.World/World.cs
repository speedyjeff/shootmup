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
            Ephemerial = new List<EphemerialElement>();
            ZoomFactor = 0;

            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(TranslateCoordinates);

            // sounds
            Sounds = sounds;

            // TODO - initialize based on on disk artifact

            // initialize the world
            WindowX = 200;
            WindowY = 200;
            All = new Dictionary<int, Element>();
            Player = new Player() { X = WindowX, Y = WindowY };
            // player
            All.Add(Player.Id, Player);
            if (false)
            {
                // test world
                foreach (var elem in WorldGenerator.Test(out Width, out Height))
                {
                    All.Add(elem.Id, elem);
                }
            }
            else if (false)
            {
                // random gen
                Width = 10000;
                Height = 10000;
                foreach (var elem in WorldGenerator.Randomgen(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }
            }
            else
            {
                // hunger games
                Width = 1000;
                Height = 1000;
                foreach (var elem in WorldGenerator.HungerGames(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }
            }

            // initially render all the elements
            Paint();
        }

        public void Paint()
        {
            // draw all the elements
            Surface.Clear(new RGBA() { R = 70, G = 169, B = 52, A = 255 });

            // add any bullets
            var toremove = new List<EphemerialElement>();
            foreach (var b in Ephemerial)
            {
                b.Draw(Surface);
                b.Duration--;
                if (b.Duration < 0) toremove.Add(b);
            }
            foreach (var b in toremove)
            {
                Ephemerial.Remove(b);
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
                case Constants.S:
                case Constants.s:
                case Constants.DownArrow:
                    ydelta = speed;
                    break;
                case Constants.A:
                case Constants.a:
                case Constants.LeftArrow:
                    xdelta = -1* speed;
                    break;
                case Constants.D:
                case Constants.d:
                case Constants.RightArrow:
                    xdelta = speed;
                    break;
                case Constants.W:
                case Constants.w:
                case Constants.UpArrow:
                    ydelta = -1* speed;
                    break;

                case Constants.x1:
                    SwitchWeapon();
                    break;

                case Constants.F:
                case Constants.f:
                    Pickup();
                    break;

                case Constants.Q:
                case Constants.x2:
                case Constants.q:
                case Constants.x0:
                    Drop();
                    break;

                case Constants.R:
                case Constants.r:
                    Reload();
                    break;

                case Constants.Space:
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

        public void Zoom(float delta)
        {
            ZoomFactor += delta;
        }

        // player actions
        public bool Turn(float angle)
        {
            if (angle < 0 || angle > 360) throw new Exception("Invalid angle : " + angle);
            Player.Angle = angle;
            return true;
        }

        public bool Move(float xdelta, float ydelta)
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

        public bool Pickup()
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

        public bool Reload()
        {
            var state = Player.Reload();
            switch (state)
            {
                case GunStateEnum.Reloaded: Sounds.Play(Player.Primary.ReloadSoundPath()); return true;
                case GunStateEnum.None:
                case GunStateEnum.NoRounds: Sounds.Play(NothingSoundPath); break;
                default: throw new Exception("Unknown GunState : " + state);
            }

            return false;
        }

        public bool Shoot()
        {
            var state = Player.Shoot();

            // apply state change
            switch (state)
            {
                case GunStateEnum.Fired:
                    // show the bullet
                    ApplyBulletTrajectory(Player.Primary, Player.X, Player.Y, Player.Angle);
                    if (Player.Primary.Spread != 0)
                    {
                        ApplyBulletTrajectory(Player.Primary, Player.X, Player.Y, Player.Angle - (Player.Primary.Spread / 2));
                        ApplyBulletTrajectory(Player.Primary, Player.X, Player.Y, Player.Angle + (Player.Primary.Spread / 2));
                    }

                    // play sound
                    Sounds.Play(Player.Primary.FiredSoundPath());

                    return true;

                // just play the sound
                case GunStateEnum.NeedsReload: Sounds.Play(Player.Primary.EmptySoundPath()); break;
                case GunStateEnum.None: Sounds.Play(NothingSoundPath); break;
                default: throw new Exception("Unknown GunState : " + state);
            }

            return false;
        }

        public bool SwitchWeapon()
        {
            return Player.SwitchWeapon();
        }

        public bool Drop()
        {
            var item = Player.DropPrimary();

            if (item != null)
            {
                item.X = Player.X;
                item.Y = Player.Y;
                All.Add(item.Id, item);

                return true;
            }

            return false;
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
        private List<EphemerialElement> Ephemerial;

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

        private float DistanceBetweenPoints(float x1, float y1, float x2, float y2)
        {
            // a^2 + b^2 = c^2
            //  a = |x1 - x2|
            //  b = |y1 - y2|
            //  c = result
            return (float)Math.Sqrt(
                Math.Pow(Math.Abs(x1 - x2), 2) + Math.Pow(Math.Abs(y1- y2), 2)
                );
        }

        private float DistanceToObject(Element elem1, Element elem2)
        {
            // this is an approximation, consider the shortest distance between any two points in these objects
            var e1 = new Tuple<float, float>[]
            {
                new Tuple<float,float>(elem1.X, elem1.Y),
                new Tuple<float,float>(elem1.X - (elem1.Width / 2), elem1.Y - (elem1.Height / 2)),
                new Tuple<float,float>(elem1.X + (elem1.Width / 2),elem1.Y + (elem1.Height / 2))
            };

            var e2 = new Tuple<float, float>[]
            {
                new Tuple<float,float>(elem2.X, elem2.Y),
                new Tuple<float,float>(elem2.X - (elem2.Width / 2), elem2.Y - (elem2.Height / 2)),
                new Tuple<float,float>(elem2.X + (elem2.Width / 2),elem2.Y + (elem2.Height / 2))
            };

            var minDistance = float.MaxValue;
            for(int i=0; i<e1.Length; i++)
                for(int j=i+1; j<e2.Length; j++)
                    minDistance = Math.Min(DistanceBetweenPoints(e1[i].Item1, e1[i].Item2, e2[j].Item1, e2[j].Item2), minDistance);
            return minDistance;
        }

        private Element IntersectingLine(float x11, float y11, float x12, float y12)
        {
            // must ensure to find the closest object that intersects
            Element item = null;
            float prvDistance = 0;

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

                // https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect

                // top
                collision = IntersectingLine(x11, y11, x12, y12,
                    x21, y21, x22, y21);
                // bottom
                collision |= IntersectingLine(x11, y11, x12, y12,
                    x21, y22, x22, y22);
                // left
                collision |= IntersectingLine(x11, y11, x12, y12,
                    x21, y21, x21, y22);
                // left
                collision |= IntersectingLine(x11, y11, x12, y12,
                    x22, y21, x22, y22);

                if (collision)
                {
                    // check if this is the closest collision
                    if (item == null)
                    {
                        item = elem;
                        prvDistance = DistanceToObject(Player, elem);
                    }
                    else
                    {
                        var distance = DistanceToObject(Player, elem);
                        if (distance < prvDistance)
                        {
                            item = elem;
                            prvDistance = distance;
                        }
                    }
                }
            }

            return item;
        }

        private bool IntersectingLine(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            // https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect
            if (CalcCcw(x1, y1, x3, y3, x4, y4) != CalcCcw(x2, y2, x3, y3, x4, y4)
                && CalcCcw(x1, y1, x2, y2, x3, y3) != CalcCcw(x1, y1, x2, y2, x4, y4))
                return true;
            return false;
        }

        private bool CalcCcw(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            return (y3 - y1) * (x2 - x1) > (y2 - y1) * (x3 - x1);
        }

        private void GetBulletTrajectory(float x, float y, float angle, float distance, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = x;
            y1 = y;
            float a = (float)Math.Cos(angle * Math.PI / 180) * distance;
            float o = (float)Math.Sin(angle * Math.PI / 180) * distance;
            x2 = x1 + o;
            y2 = y1 - a;
        }

        private bool ApplyBulletTrajectory(Gun gun, float x, float y, float angle)
        {
            float x1, y1, x2, y2;
            GetBulletTrajectory(x, y, angle, gun.Distance, out x1, out y1, out x2, out y2);

            // determine damage
            var elem = IntersectingLine(x1, y1, x2, y2);

            if (elem != null)
            {
                // apply damage
                if (elem.TakesDamage)
                {
                    elem.ReduceHealth(gun.Damage);

                    if (elem.IsDead)
                    {
                        Ephemerial.Add(new Message()
                        {
                            X = Player.X,
                            Y = Player.Y + (Height / 3) - 16,
                            Text = string.Format("player {0} killed {0}", Player.Name, elem.Name),
                            Duration = Constants.EphemerialElementDuration
                        });
                    }
                }

                // reduce the visual shot on screen based on where the bullet hit
                var distance = DistanceToObject(Player, elem);
                GetBulletTrajectory(x, y, angle, distance, out x1, out y1, out x2, out y2);
            }

            // add bullet effect
            Ephemerial.Add(new BulletTrajectory()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Damage = gun.Damage,
                Duration = Constants.EphemerialElementDuration
            });

            return elem != null;
        }
        #endregion
    }
}
