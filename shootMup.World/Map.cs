using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using shootMup.Common;

namespace shootMup
{
    public enum PlayerPlacement { Diagnal, Borders };

    public class Map
    {
        public Map(int width, int height, Player[] players, PlayerPlacement placement)
        {
            // init
            All = new Dictionary<int, Element>();
            Width = width;
            Height = height;

            // TODO - initialize based on on disk artifact

            // add players
            foreach (var o in players) All.Add(o.Id, o);

            // create the board
            if (false)
            {
                // test world
                foreach (var elem in WorldGenerator.Test(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }

            }
            else if (true)
            {
                // random gen
                foreach (var elem in WorldGenerator.Randomgen(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }
            }
            else if (true)
            {
                // hunger games
                foreach (var elem in WorldGenerator.HungerGames(Width, Height))
                {
                    All.Add(elem.Id, elem);
                }
            }
            else
            {
                // empty
            }

            // place the players in a diagnoal pattern
            if (placement == PlayerPlacement.Diagnal)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].X != 0 || players[i].Y != 0) continue;
                    float diag = (width / players.Length) * i;
                    if (diag < 100) throw new Exception("Too many ai players for this board size");
                    players[i].X = diag;
                    players[i].Y = diag;
                }
            }
            else if (placement == PlayerPlacement.Borders)
            {
                // place players around the borders
                float delta = (((width + height) * 2) / (players.Length + 5));
                if (delta < 100) throw new Exception("Too many ai players for this board size");
                float ydelta = delta;
                float xdelta = 0;
                float x = 50;
                float y = 50;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].X != 0 || players[i].Y != 0) continue;

                    x += xdelta;
                    y += ydelta;

                    if (y > height)
                    {
                        // bottom left corner
                        y -= delta;
                        xdelta = ydelta;
                        ydelta = 0;
                        x += xdelta;
                    }
                    else if (x > width)
                    {
                        // bottom right corner
                        x -= delta;
                        ydelta = xdelta * -1;
                        xdelta = 0;
                        y += ydelta;
                    }
                    else if (y < 0)
                    {
                        // top right corner
                        y += delta;
                        xdelta = ydelta;
                        ydelta = 0;
                        x += xdelta;
                    }
                    else if (x < 0)
                    {
                        throw new Exception("Failed to properly distribute all the players evenly");
                    }

                    if (x < 0 || x > width || y < 0 || y > height)
                    {
                        System.Diagnostics.Debug.WriteLine("Placing a player outside of the borders");
                    }

                    players[i].X = x;
                    players[i].Y = y;
                }
            }
            else
            {
                throw new Exception("Unknown placement strategy : " + placement);
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public event Action<EphemerialElement> OnEphemerialEvent;
        public event Action<Element> OnElementHit;
        public event Action<Element> OnElementDied;

        public IEnumerable<Element> WithinWindow(float x, float y, float width, float height)
        {
            // do not take Z into account, as the view should be unbostructed (top down)

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
                if (IntersectingRectangles(player, false /* consider acquirable */, xdelta, ydelta) != null)
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
            if (player.Z != Constants.Ground) return null;

            lock (All)
            {
                // see if we are over an item
                Element item = IntersectingRectangles(player, true /* consider acquirable */);

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
            if (player.Z != Constants.Ground) return GunStateEnum.None;

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
            if (player.Z != Constants.Ground) return null;

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
            if (elem1.Z != elem2.Z) return false;

            float x1 = (elem1.X) - (elem1.Width / 2);
            float y1 = (elem1.Y) - (elem1.Height / 2);
            float x2 = (elem1.X) + (elem1.Width / 2);
            float y2 = (elem1.Y) + (elem1.Height / 2);

            float x3 = (elem2.X) - (elem2.Width / 2);
            float y3 = (elem2.Y) - (elem2.Height / 2);
            float x4 = (elem2.X) + (elem2.Width / 2);
            float y4 = (elem2.Y) + (elem2.Height / 2);

            return Collision.IntersectingRectangles(x1, y1, x2, y2, x3, y3, x4, y4);
        }

        #region private
        private int SpeedFactor = 2;
        private Dictionary<int, Element> All { get; set; }

        private Element IntersectingRectangles(Player player, bool considerAquireable = false, float xdelta = 0, float ydelta = 0)
        {
            float x1 = (player.X + xdelta) - (player.Width / 2);
            float y1 = (player.Y + ydelta) - (player.Height / 2);
            float x2 = (player.X + xdelta) + (player.Width / 2);
            float y2 = (player.Y + ydelta) + (player.Height / 2);

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

                // only consider items that are within the same plane
                if (elem.Z >= player.Z)
                {
                    float x3 = elem.X - (elem.Width / 2);
                    float y3 = elem.Y - (elem.Height / 2);
                    float x4 = elem.X + (elem.Width / 2);
                    float y4 = elem.Y + (elem.Height / 2);

                    // check if these collide
                    if (Collision.IntersectingRectangles(x1, y1, x2, y2, x3, y3, x4, y4)) return elem;
                }
            }

            return null;
        }

        private float DistanceToObject(Element elem1, Element elem2)
        {
            return Collision.DistanceToObject(elem1.X, elem1.Y, elem1.Width, elem1.Height,
                elem2.X, elem2.Y, elem2.Width, elem2.Height);
        }

        private Element IntersectingLine(Player player, float x1, float y1, float x2, float y2)
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
                float x3 = elem.X - (elem.Width / 2);
                float y3 = elem.Y - (elem.Height / 2);
                float x4 = elem.X + (elem.Width / 2);
                float y4 = elem.Y + (elem.Height / 2);

                // https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect

                // top
                collision = Collision.IntersectingLine(x1, y1, x2, y2,
                    x3, y3, x4, y3);
                // bottom
                collision |= Collision.IntersectingLine(x1, y1, x2, y2,
                    x3, y4, x4, y4);
                // left
                collision |= Collision.IntersectingLine(x1, y1, x2, y2,
                    x3, y3, x3, y4);
                // left
                collision |= Collision.IntersectingLine(x1, y1, x2, y2,
                    x4, y3, x4, y4);

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
                        // increment kills
                        if (elem is Player) player.Kills++;

                        if (OnElementDied != null) OnElementDied(elem);

                        if (OnEphemerialEvent != null)
                        {
                            OnEphemerialEvent(new Message()
                            {
                                Text = string.Format("Player {0} killed {1}", player.Name, elem.Name)
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
                    Damage = gun.Damage
                });
            }

            return elem != null;
        }
        #endregion
    }
}
