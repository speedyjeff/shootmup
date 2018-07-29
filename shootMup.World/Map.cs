using System;
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

            // TODO - initialize based on on disk artifact

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
            else if (true)
            {
                // random gen
                Width = 10000;
                Height = 10000;
                foreach (var elem in WorldGenerator.Randomgen(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }
            }
            else if (false)
            {
                // hunger games
                Width = 1000;
                Height = 1000;
                foreach (var elem in WorldGenerator.HungerGames(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }
            }
            else
            {
                // empty
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public event Action<EphemerialElement> OnEphemerialEvent;
        public event Action<Element> OnElementHit;
        public event Action<Element> OnElementDied;

        public IEnumerable<Element> WithinWindow(float x, float y, float width, float height)
        {
            // return objects that are within the window
            lock (All)
            {
                var x1 = x - width / 2;
                var y1 = y - height / 2;
                var x2 = x + width / 2;
                var y2 = y + height / 2;

                foreach (var elem in All.Values)
                {
                    if (elem.IsDead) continue;

                    var x3 = elem.X - elem.Width / 2;
                    var y3 = elem.Y - elem.Height / 2;
                    var x4 = elem.X + elem.Width / 2;
                    var y4 = elem.Y + elem.Height / 2;

                    if (Collision.IntersectingRectangles(x1, y1, x2, y2, 
                        x3, y3, x4, y4))
                    {
                        yield return elem;
                    }
                }
            }
        }

        public bool Move(Player player, ref float xdelta, ref float ydelta)
        {
            lock (All)
            {
                float speed = Constants.Speed * SpeedFactor;

                if (player.IsDead) return false;

                // check if the delta is legal
                if (Math.Abs(xdelta) + Math.Abs(ydelta) > 1.00001) return false;

                // adjust for speed
                xdelta *= speed;
                ydelta *= speed;

                // check for a collision first
                if (HasCollision(player, xdelta, ydelta))
                {
                    return false;
                }

                // move the player
                player.Move(xdelta, ydelta);

                return true;
            }
        }

        public Type Pickup(Player player)
        {
            lock (All)
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

                        return item.GetType();
                    }
                }

                return null;
            }
        }

        public GunStateEnum Shoot(Player player)
        {
            lock (All)
            {
                var state = player.Shoot();

                // apply state change
                if (state == GunStateEnum.Fired)
                {
                    bool killShot = false;
                    bool targetDied = false; // used to change the fired state
                    bool targetHit = false;

                    // show the bullet
                    targetHit |= ApplyBulletTrajectory(player, player.Primary, player.X, player.Y, player.Angle, out killShot);
                    targetDied |= killShot;
                    if (player.Primary.Spread != 0)
                    {
                        targetHit |= ApplyBulletTrajectory(player, player.Primary, player.X, player.Y, player.Angle - (player.Primary.Spread / 2), out killShot);
                        targetDied |= killShot;
                        targetHit |= ApplyBulletTrajectory(player, player.Primary, player.X, player.Y, player.Angle + (player.Primary.Spread / 2), out killShot);
                        targetDied |= killShot;
                    }

                    // adjust state accordingly
                    if (targetDied) state = GunStateEnum.FiredAndKilled;
                    else if (targetHit) state = GunStateEnum.FiredWithContact;
                }

                return state;
            }
        }

        public Type Drop(Player player)
        {
            lock (All)
            {
                var item = player.DropPrimary();

                if (item != null)
                {
                    item.X = player.X;
                    item.Y = player.Y;
                    All.Add(item.Id, item);

                    return item.GetType();
                }

                return null;
            }
        }

        public bool IsTouching(Element elem1, Element elem2)
        {
            float x1 = (elem1.X) - (elem1.Width / 2);
            float y1 = (elem1.Y) - (elem1.Height / 2);
            float x2 = (elem1.X) + (elem1.Width / 2);
            float y2 = (elem1.Y) + (elem1.Height / 2);

            return IntersectingRectangles(x1, y1, x2, y2, elem2) != null;
        }

        #region private
        private int SpeedFactor = 2;
        private Dictionary<int, Element> All { get; set; }

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

            if (Collision.IntersectingRectangles(x11, y11, x12, y12, x21, y21, x22, y22)) return elem;
            else return null;
        }

        private float DistanceToObject(Element elem1, Element elem2)
        {
            return Collision.DistanceToObject(elem1.X, elem1.Y, elem1.Width, elem1.Height,
                elem2.X, elem2.Y, elem2.Width, elem2.Height);
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
                if (elem.IsDead) continue;
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

        private bool ApplyBulletTrajectory(Player player, Gun gun, float x, float y, float angle, out bool killShot)
        {
            float x1, y1, x2, y2;
            Collision.CalculateLineByAngle(x, y, angle, gun.Distance, out x1, out y1, out x2, out y2);

            // determine damage
            killShot = false;
            var elem = IntersectingLine(player, x1, y1, x2, y2);

            if (elem != null)
            {
                // apply damage
                if (elem.TakesDamage)
                {
                    elem.ReduceHealth(gun.Damage);

                    if (OnElementHit != null) OnElementHit(elem); 

                    if (elem.IsDead)
                    {
                        if (OnElementDied != null) OnElementDied(elem);

                        if (OnEphemerialEvent != null)
                        {
                            OnEphemerialEvent(new Message()
                            {
                                X = player.X,
                                Y = player.Y + (Height / 3) - 16,
                                Text = string.Format("player {0} killed {0}", player.Name, elem.Name),
                                Duration = Constants.EphemerialElementDuration
                            });
                        }

                        // indicate that the element died
                        killShot = true;
                    }
                }

                // reduce the visual shot on screen based on where the bullet hit
                var distance = DistanceToObject(player, elem);
                Collision.CalculateLineByAngle(x, y, angle, distance, out x1, out y1, out x2, out y2);
            }

            // add bullet effect
            if (OnEphemerialEvent != null)
            {
                OnEphemerialEvent(new BulletTrajectory()
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Damage = gun.Damage,
                    Duration = Constants.EphemerialElementDuration
                });
            }

            return elem != null;
        }
    #endregion
    }
}
