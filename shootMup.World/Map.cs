﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using shootMup.Bots;
using shootMup.Common;

namespace shootMup
{
    public enum PlayerPlacement { Diagnal, Borders };

    public class Map
    {
        public Map(int width, int height, Player[] players, Background background, PlayerPlacement placement)
        {
            // init
            var obstacles = new Dictionary<int, Element>();
            var items = new Dictionary<int, Element>();
            Players = players;
            Width = width;
            Height = height;
            Background = background;

            // TODO - initialize based on on disk artifact

            // create the board
            if (false)
            {
                // test world
                WorldGenerator.Test(Width, Height, obstacles, items);
            }
            else if (true)
            {
                // random gen
                WorldGenerator.Randomgen(Width, Height, obstacles, items);
            }
            else if (true)
            {
                // hunger games
                WorldGenerator.HungerGames(Width, Height, obstacles, items);
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

            // add players - after deciding on placement
            foreach (var o in players) obstacles.Add(o.Id, o);

            // put items into the collections
            Obstacles = new RegionCollection(obstacles.Values, width, height);
            Items = new RegionCollection(items.Values, width, height);

            // setup the background update timer
            BackgroundTimer = new Timer(BackgroundUpdate, null, 0, Constants.GlobalClock);
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool IsPaused { get; set; }

        public event Action<EphemerialElement> OnEphemerialEvent;
        public event Action<Element> OnElementHit;
        public event Action<Element> OnElementDied;

        public IEnumerable<Element> WithinWindow(float x, float y, float width, float height)
        {
            // do not take Z into account, as the view should be unbostructed (top down)

            // return objects that are within the window
            var x1 = x - width / 2;
            var y1 = y - height / 2;
            var x2 = x + width / 2;
            var y2 = y + height / 2;

            // iterate through all objects (obstacles + items)
            foreach (var elems in new RegionCollection[] { Items, Obstacles })
            {
                foreach (var elem in elems.Values(x1, y1, x2, y2))
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
            if (player.IsDead) return false;
            if (IsPaused) return false;

            float pace = Background.Pace(player.X, player.Y);
            if (pace < Constants.MinSpeedMultiplier) pace = Constants.MinSpeedMultiplier;
            if (pace > Constants.MaxSpeedMultiplier) pace = Constants.MaxSpeedMultiplier;
            float speed = Constants.Speed * pace;

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

            // get region before move
            var beforeRegion = Obstacles.GetRegion(player);

            // move the player
            player.Move(xdelta, ydelta);

            // get region after move
            var afterRegion = Obstacles.GetRegion(player);
            if (!beforeRegion.Equals(afterRegion))
            {
                Obstacles.Move(player.Id, beforeRegion, afterRegion);
            }

            return true;
        }

        public Type Pickup(Player player)
        {
            if (player.Z != Constants.Ground) return null;
            if (player.IsDead) return null;
            if (IsPaused) return null;

            // see if we are over an item
            Element item = IntersectingRectangles(player, true /* consider acquirable */);

            if (item != null)
            {
                // remove as an atomic operation
                if (Items.Remove(item.Id, item))
                {
                    // pickup the item
                    if (player.Take(item))
                    {
                        return item.GetType();
                    }
                }
            }

            return null;
        }

        public AttackStateEnum Attack(Player player)
        {
            if (player.Z != Constants.Ground) return AttackStateEnum.None;
            if (player.IsDead) return AttackStateEnum.None;
            if (IsPaused) return AttackStateEnum.None;

            var hit = new HashSet<Element>();
            var state = AttackStateEnum.None;
            var trajectories = new List<BulletTrajectory>();

            state = player.Attack();

            // apply state change
            if (state == AttackStateEnum.Fired)
            {
                Element elem = null;

                // apply the bullet via the trajectory
                elem = TrackAttackTrajectory(player, player.Primary, player.X, player.Y, player.Angle, trajectories);
                if (elem != null) hit.Add(elem);
                if (player.Primary.Spread != 0)
                {
                    elem = TrackAttackTrajectory(player, player.Primary, player.X, player.Y, player.Angle - (player.Primary.Spread / 2), trajectories);
                    if (elem != null) hit.Add(elem);
                    elem = TrackAttackTrajectory(player, player.Primary, player.X, player.Y, player.Angle + (player.Primary.Spread / 2), trajectories);
                    if (elem != null) hit.Add(elem);
                }
            }
            else if (state == AttackStateEnum.Melee)
            {
                // project out a short range and check if there was contact
                Element elem = null;

                // apply the bullet via the trajectory
                elem = TrackAttackTrajectory(player, player.Fists, player.X, player.Y, player.Angle, trajectories);
                if (elem != null) hit.Add(elem);

                // disregard any trajectories
                trajectories.Clear();
            }

            // send notifications
            bool targetDied = false; // used to change the fired state
            bool targetHit = false;
            foreach (var elem in hit)
            {
                targetHit = true;

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
                }
            }

            // add bullet trajectories
            foreach(var t in trajectories)
            {
                if (OnEphemerialEvent != null)
                {
                    OnEphemerialEvent(t);
                }
            }

            // adjust state accordingly
            if (state == AttackStateEnum.Melee)
            {
                // used fists
                if (targetDied) state = AttackStateEnum.MeleeAndKilled;
                else if (targetHit) state = AttackStateEnum.MeleeWithContact;
            }
            else
            {
                // used a gun
                if (targetDied) state = AttackStateEnum.FiredAndKilled;
                else if (targetHit) state = AttackStateEnum.FiredWithContact;
            }

            return state;
        }

        public Type Drop(Player player)
        {
            if (player.Z != Constants.Ground) return null;
            if (IsPaused) return null;
            // this action is allowed for a dead player

            var item = player.DropPrimary();

            if (item != null)
            {
                item.X = player.X;
                item.Y = player.Y;
                Items.Add(item.Id, item);

                return item.GetType();
            }

            return null;
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

        public bool AddItem(Element item)
        {
            if (IsPaused) return false;

            if (item != null)
            {
                if (item.CanAcquire)
                {
                    Items.Add(item.Id, item);
                }
                else
                {
                    Obstacles.Add(item.Id, item);
                }

                return true;
            }

            return false;
        }

        public bool RemoveItem(Element item)
        {
            if (IsPaused) return false;

            if (item != null)
            {
                if (item.CanAcquire)
                {
                    Items.Remove(item.Id, item);
                }
                else
                {
                    Obstacles.Remove(item.Id, item);
                }

                return true;
            }

            return false;
        }

        #region private
        private RegionCollection Obstacles;
        private RegionCollection Items;
        private Player[] Players;
        private Background Background;
        private Timer BackgroundTimer;

        private void BackgroundUpdate(object state)
        {
            if (IsPaused) return;
            var deceased = new List<Element>();

            // update the map
            Background.Update();

            // apply any necessary damage to the players
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i] == null || Players[i].IsDead) continue;

                var damage = Background.Damage(Players[i].X, Players[i].Y);
                if (damage > 0)
                {
                    Players[i].ReduceHealth(damage);

                    if (Players[i].IsDead)
                    {
                        deceased.Add(Players[i]);
                    }
                }
            }

            // notify the deceased
            foreach (var elem in deceased)
            {
                // this player has died as a result of taking damage from the zone
                if (OnElementDied != null) OnElementDied(elem);

                if (OnEphemerialEvent != null)
                {
                    OnEphemerialEvent(new Message()
                    {
                        Text = string.Format("Player {0} died in the zone", elem.Name)
                    });
                }
            }
        }

        private Element IntersectingRectangles(Player player, bool considerAquireable = false, float xdelta = 0, float ydelta = 0)
        {
            float x1 = (player.X + xdelta) - (player.Width / 2);
            float y1 = (player.Y + ydelta) - (player.Height / 2);
            float x2 = (player.X + xdelta) + (player.Width / 2);
            float y2 = (player.Y + ydelta) + (player.Height / 2);

            // either choose to iterate through solid objects (obstacles) or items
            var objects = Obstacles;
            if (considerAquireable) objects = Items;

            // check collisions
            foreach (var elem in objects.Values(x1, y1, x2, y2))
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
            foreach (var elem in Obstacles.Values(x1, y1, x2, y2))
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

        private Element TrackAttackTrajectory(Player player, Gun weapon, float x, float y, float angle, List<BulletTrajectory> trajectories)
        {
            float x1, y1, x2, y2;
            Collision.CalculateLineByAngle(x, y, angle, weapon.Distance, out x1, out y1, out x2, out y2);

            // determine damage
            var elem = IntersectingLine(player, x1, y1, x2, y2);

            if (elem != null)
            {
                // apply damage
                if (elem.TakesDamage)
                {
                    elem.ReduceHealth(weapon.Damage);
                }

                // reduce the visual shot on screen based on where the bullet hit
                var distance = DistanceToObject(player, elem);
                Collision.CalculateLineByAngle(x, y, angle, distance, out x1, out y1, out x2, out y2);
            }

            // add bullet effect
            trajectories.Add( new BulletTrajectory()
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Damage = weapon.Damage
                });
 
            return elem;
        }
        #endregion
    }
}
