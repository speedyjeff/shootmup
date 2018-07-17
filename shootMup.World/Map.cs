﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using shootMup.Common;

namespace shootMup
{
    public class Map
    {
        public Map(Player human, Player[] otherPlayers)
        {
            // init
            All = new Dictionary<int, Element>();
            Ephemerial = new List<EphemerialElement>();

            // TODO - initialize based on on disk artifact

            // initialize the world
            WindowX = human.X;
            WindowY = human.Y;

            
            // add players
            All.Add(human.Id, human);
            if (otherPlayers != null && otherPlayers.Length > 0)
            {
                foreach (var o in otherPlayers) All.Add(o.Id, o);
            }
            // create the board
            if (false)
            {
                // test world
                int width;
                int height;
                foreach (var elem in WorldGenerator.Test(out width, out height))
                {
                    All.Add(elem.Id, elem);
                }
                Width = width;
                Height = height;
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
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool Turn(Player player, float angle)
        {
            if (angle < 0 || angle > 360) throw new Exception("Invalid angle : " + angle);
            player.Angle = angle;
            return true;
        }

        public bool Move(Player player, float xdelta, float ydelta)
        {
            if (player.IsDead) return false;

            // TODO! check if the delta is legal

            // check for a collision first
            if (HasCollision(player, xdelta, ydelta))
            {
                return false;
            }

            // move the player
            player.Move(xdelta, ydelta);

            // move the screen
            WindowX += xdelta;
            WindowY += ydelta;

            return true;
        }

        public bool Pickup(Player player)
        {
            // see if we are over an item
            Element item = IntersectingRectangles(player);

            if (item != null)
            {
                // pickup the item
                if (player.Take(item))
                {
                    // remove the item from the playing field
                    // TODO! dangeour remove!
                    All.Remove(item.Id);

                    return true;
                }
            }

            return false;
        }

        public GunStateEnum Reload(Player player)
        {
            return player.Reload();
        }

        public GunStateEnum Shoot(Player player)
        {
            var state = player.Shoot();

            // apply state change
            if (state == GunStateEnum.Fired)
            {
                // show the bullet
                ApplyBulletTrajectory(player, player.Primary, player.X, player.Y, player.Angle);
                if (player.Primary.Spread != 0)
                {
                    ApplyBulletTrajectory(player, player.Primary, player.X, player.Y, player.Angle - (player.Primary.Spread / 2));
                    ApplyBulletTrajectory(player, player.Primary, player.X, player.Y, player.Angle + (player.Primary.Spread / 2));
                }
            }

            return state;
        }

        public bool SwitchWeapon(Player player)
        {
            return player.SwitchWeapon();
        }

        public bool Drop(Player player)
        {
            var item = player.DropPrimary();

            if (item != null)
            {
                item.X = player.X;
                item.Y = player.Y;
                All.Add(item.Id, item);

                return true;
            }

            return false;
        }

        // support
        public bool TranslateCoordinates(float windowWidth, float windowHeight, float x, float y, float width, float height, out float tx, out float ty, out float twidth, out float theight)
        {
            // translate the x and y based on the current window
            // windowWidth & windowHeight are the current windows width & height
            float windowHWidth = windowWidth / 2.0f;
            float windowHHeight = windowHeight / 2.0f;

            float x1 = WindowX - windowHWidth;
            float y1 = WindowY - windowHHeight;
            float x2 = WindowX + windowHWidth;
            float y2 = WindowY + windowHHeight;

            tx = ty = twidth = theight = 0;

            // check if inside the window
            if (x < (x1 - width) || x > (x2 + width)) return false;
            if (y < (y1 - height) || y > (y2 + height)) return false;

            // now translate to the window
            tx = x - x1;
            ty = y - y1;

            // scale the input
            // TODO!
            twidth = width;
            theight = height;

            return true;
        }

        public bool IsTouching(Element elem1, Element elem2)
        {
            float x11 = (elem1.X) - (elem1.Width / 2);
            float y11 = (elem1.Y) - (elem1.Height / 2);
            float x12 = (elem1.X) + (elem1.Width / 2);
            float y12 = (elem1.Y) + (elem1.Height / 2);

            return IntersectingRectangles(x11, y11, x12, y12, elem2) != null;
        }

        public Dictionary<int, Element> All { get; private set; }
        public List<EphemerialElement> Ephemerial { get; private set; }

        #region private
        private float WindowX;
        private float WindowY;

        private bool HasCollision(Player player, float xdelta, float ydelta)
        {
            // object 1 will be player
            float x11 = (player.X + xdelta) - (player.Width / 2);
            float y11 = (player.Y + ydelta) - (player.Height / 2);
            float x12 = (player.X + xdelta) + (player.Width / 2);
            float y12 = (player.Y + ydelta) + (player.Height / 2);

            return IntersectingRectangles(player, x11, y11, x12, y12) != null;
        }

        private Element IntersectingRectangles(Player player)
        {
            // object 1 will be player
            float x11 = (player.X) - (player.Width / 2);
            float y11 = (player.Y) - (player.Height / 2);
            float x12 = (player.X) + (player.Width / 2);
            float y12 = (player.Y) + (player.Height / 2);

            return IntersectingRectangles(player, x11, y11, x12, y12, true);
        }

        private Element IntersectingRectangles(Player player, float x11, float y11, float x12, float y12, bool considerAquireable = false)
        {
            // check collisions
            foreach (var elem in All.Values)
            {
                if (elem.Id == player.Id) continue;
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
                Math.Pow(Math.Abs(x1 - x2), 2) + Math.Pow(Math.Abs(y1 - y2), 2)
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
            for (int i = 0; i < e1.Length; i++)
                for (int j = i + 1; j < e2.Length; j++)
                    minDistance = Math.Min(DistanceBetweenPoints(e1[i].Item1, e1[i].Item2, e2[j].Item1, e2[j].Item2), minDistance);
            return minDistance;
        }

        private Element IntersectingLine(Player player, float x11, float y11, float x12, float y12)
        {
            // must ensure to find the closest object that intersects
            Element item = null;
            float prvDistance = 0;

            // check collisions
            foreach (var elem in All.Values)
            {
                bool collision = false;
                if (elem.Id == player.Id) continue;
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
                        prvDistance = DistanceToObject(player, elem);
                    }
                    else
                    {
                        var distance = DistanceToObject(player, elem);
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

        private bool ApplyBulletTrajectory(Player player, Gun gun, float x, float y, float angle)
        {
            float x1, y1, x2, y2;
            GetBulletTrajectory(x, y, angle, gun.Distance, out x1, out y1, out x2, out y2);

            // determine damage
            var elem = IntersectingLine(player, x1, y1, x2, y2);

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
                            X = player.X,
                            Y = player.Y + (Height / 3) - 16,
                            Text = string.Format("player {0} killed {0}", player.Name, elem.Name),
                            Duration = Constants.EphemerialElementDuration
                        });
                    }
                }

                // reduce the visual shot on screen based on where the bullet hit
                var distance = DistanceToObject(player, elem);
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
