using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class SimpleAI : AI
    {
        // Simple rules
        //  direction rules:
        //    if right next/touching an object, move around
        //    if item, possible pickup (depending on other action)
        //    if in a crowd, move elsewhere
        //    if in the zone, move towards the center
        //    move closer to items/players of interest
        //
        //  0. if no weapn
        //    0.a go towards weapon
        //    0.b if near a player, melee
        //  1. if have a weapon
        //    1.a if less ammo than X, go towards ammo
        //    1.b if not loaded, reload
        //    1.c if primary is not an ak47, move towards and pickup
        //    1.d if a player is near and can shoot, move towards and shoot in that direction
        //  2. if low on health, go towards health
        //  3. if low on sheld, go towards sheald

        //  else move in a single direction

        public SimpleAI() : base()
        {
            Rand = new Random();
            PreviousX = PreviousY = 0;
            SameLocationCount = 0;

            // track what has attempted to be pickedup
            PreviousPickups = new Dictionary<int, int>();

            // in general keep going in the previous direction, unless there is a reason to change
            PreviousAngle = -1;

            // TODO make it configuarble that some of these rules are applied by some bots but not all
            //  for example, prefer AK47 or run away
        }

        public override ActionEnum Action(List<Element> elements, ref float xdelta, ref float ydelta, ref float angle)
        {
            var playerCount = 0;
            float playerX = 0;
            float playerY = 0;
            
            // find proximity to all types
            var closest = AITraining.ComputeProximity(this, elements);

            // gather details about if there is a crowd
            foreach (var elem in elements)
            {
                if (elem.Id == Id) continue; // found myself
                if (!(elem is Player)) continue; // only care about players

                playerCount++;
                playerX += elem.X;
                playerY += elem.Y;
            }

            // calculate the average center (if there are players near by)
            if (playerCount >= 1)
            {
                playerX /= (float)playerCount;
                playerY /= (float)playerCount;
            }
            
            // choose an action - set the rules in reverse order in order to set precedence
            var action = ActionEnum.None;
            xdelta = ydelta = angle = 0;

            if (PreviousAngle < 0)
            {
                // choose an angle at random
                PreviousAngle = Rand.Next() % 360;
            }
            angle = PreviousAngle;

            // 3) Sheld
            if (Sheld < Constants.MaxSheld)
            {
                ElementProximity helmet = null;
                if (closest.TryGetValue(typeof(Helmet), out helmet))
                {
                    // there is health either close or touching
                    if (IsTouching(helmet, Width/2))
                    {
                        // choose to pickup
                        action = ActionEnum.Pickup;
                        // set direction via another decision
                        PreviousPickupId = helmet.Id;
                    }
                    else
                    {
                        // choose action via another decision
                        angle = helmet.Angle;
                    }
                }
            }

            // 2) Health
            if (Health < Constants.MaxHealth)
            {
                ElementProximity bandage = null;
                if (closest.TryGetValue(typeof(Bandage), out bandage))
                {
                    // there is health either close or touching
                    if (IsTouching(bandage, Width/2))
                    {
                        // choose to pickup
                        action = ActionEnum.Pickup;
                        // set direction via another decision
                        PreviousPickupId = bandage.Id;
                    }
                    else
                    {
                        // choose action via another decision
                        angle = bandage.Angle;
                    }
                }
            }

            // 1) Have weapon
            if (Primary != null)
            {
                ElementProximity ammo = null;
                // need ammo
                if (Primary.Ammo < MinAmmo && closest.TryGetValue(typeof(Ammo), out ammo))
                {
                    // there is ammo either close or touching
                    if (IsTouching(ammo, Width/2))
                    {
                        // choose to pickup
                        action = ActionEnum.Pickup;
                        // set direction via another decision
                        PreviousPickupId = ammo.Id;
                    }
                    else
                    {
                        // choose action via another decision
                        angle = ammo.Angle;
                    }
                }

                // needs reload
                if (!Primary.RoundsInClip(out int rounds) && rounds == 0 && Primary.HasAmmo())
                {
                    // choose to reload
                    action = ActionEnum.Reload;
                    // choose direction via another decision
                }

                ElementProximity ak47 = null;
                // pick up ak47
                if (!(Primary is AK47) && closest.TryGetValue(typeof(AK47), out ak47))
                {
                    // there is an AK47 either close or touching
                    if (IsTouching(ak47, Width / 2))
                    {
                        // choose to pickup
                        action = ActionEnum.Pickup;
                        // set direction via another decision
                        PreviousPickupId = ak47.Id;
                    }
                    else
                    {
                        // choose action via another decision
                        angle = ak47.Angle;
                    }
                }

                ElementProximity player = null;
                // shoot a player
                if (Primary.CanShoot() && closest.TryGetValue(typeof(Player), out player))
                {
                    // choose to shoot
                    action = ActionEnum.Attack;
                    // move towards the player
                    angle = player.Angle;
                }
            }

            // 0) No weapon
            if (Primary == null)
            {
                // 0.b if near a player, melee
                ElementProximity player = null;
                if (closest.TryGetValue(typeof(Player), out player))
                {
                    if (IsTouching(player, Fists.Distance))
                    {
                        // choose to melee
                        action = ActionEnum.Attack;
                        // turn towards the player
                        angle = player.Angle;
                    }
                }

                // 0.a is there a weapon within view
                ElementProximity weapon = null;
                if (!closest.TryGetValue(typeof(AK47), out weapon))
                    if (!closest.TryGetValue(typeof(Pistol), out weapon))
                        if (!closest.TryGetValue(typeof(Shotgun), out weapon))
                        {
                        }
                if (weapon != null)
                {
                    // there is a weapon either close or touching
                    if (IsTouching(weapon, Width/2))
                    {
                        // choose to pickup
                        action = ActionEnum.Pickup;
                        // set direction via another decision
                        PreviousPickupId = weapon.Id;
                    }
                    else
                    {
                        // choose action via another decision
                        angle = weapon.Angle;
                    }
                }
            }

            // if there are too many players, then run away
            if (playerCount >= 5)
            {
                // choose an angle opposite from where the center of the other players are
                angle = Collision.CalculateAngleFromPoint(X, Y, playerX, playerY);
                // go the opposite way
                angle = (angle + 180) % 360;
            }
          
            // choose defaults
            if (action == ActionEnum.None)
            {
                // default to moving
                action = ActionEnum.Move;
            }

            // check if we are in the Zone
            if (InZone > 0)
            {
                InZone--;
                // we should be moving towards the center
                if (action == ActionEnum.Move)
                {
                    angle = Collision.CalculateAngleFromPoint(X, Y, ZoneX, ZoneY);
                }
            }

            // check if we seem to be stuck
            if (IsStuck())
            {
                // take some corrective action
                if (ShowDiagnostics) System.Diagnostics.Debug.WriteLine("AI seems stuck");
                // try something new
                angle = Rand.Next() % 360;
                CorrectiveAngle = 0;
            }

            // check if our last movement was obstructed
            float moveAngle = (angle + CorrectiveAngle) % 360;
            if (CorrectiveAngle > 0) CorrectiveAngle -= 15;
            if (CorrectiveAngle < 0) CorrectiveAngle = 0;

            // save angle for next time
            PreviousAngle = moveAngle;

            // set course
            float x1, y1, x2, y2;
            Collision.CalculateLineByAngle(X, Y, moveAngle, 1, out x1, out y1, out x2, out y2);

            xdelta = x2 - x1;
            ydelta = y2 - y1;

            // normalize
            xdelta = xdelta / (Math.Abs(xdelta) + Math.Abs(ydelta));
            ydelta = ydelta / (Math.Abs(xdelta) + Math.Abs(ydelta));
            if (Math.Abs(xdelta) + Math.Abs(ydelta) > 1)
            {
                var delta = (Math.Abs(xdelta) + Math.Abs(ydelta)) - 1;
                if (xdelta > ydelta) xdelta -= delta;
                else ydelta -= delta;
            }
            xdelta = (float)Math.Round(xdelta, 4);
            ydelta = (float)Math.Round(ydelta, 4);

            if (ShowDiagnostics) System.Diagnostics.Debug.WriteLine("AI {0} {1} {2} {3}", action, angle, xdelta, ydelta);

            return action;
        }

        public override void Feedback(ActionEnum action, object item, bool result)
        {
            // if the result was successful, then continue
            if (result)
            {
                switch (action)
                {
                    case ActionEnum.Move:
                        CorrectiveAngle = 0;
                        break;
                    case ActionEnum.Pickup:
                        // clear the previous attempts for this item (in case it is dropped later)
                        if (PreviousPickups.ContainsKey(PreviousPickupId)) PreviousPickups[PreviousPickupId] = 0;
                        break;
                }
                return;
            }

            // make corrective actions
            switch(action)
            {
                case ActionEnum.Move:
                    // do some corrective action on moving
                    CorrectiveAngle += 45;
                    break;
                case ActionEnum.Pickup:
                    // increment the attempt counter
                    if (!PreviousPickups.ContainsKey(PreviousPickupId)) PreviousPickups.Add(PreviousPickupId, 1);
                    else PreviousPickups[PreviousPickupId]++;
                    if (ShowDiagnostics) System.Diagnostics.Debug.WriteLine("Failed to pickup {0} times", PreviousPickups[PreviousPickupId]);
                    break;
                case ActionEnum.ZoneDamage:
                    // eek we are in the zone, indicate that we should be moving towards the center
                    var center = (item as Tuple<float, float>);
                    if (InZone > 0) InZone++;
                    else InZone = 5;
                    ZoneX = center.Item1;
                    ZoneY = center.Item2;
                    break;
            }
        }

        #region private
        private Random Rand;

        // movement related
        private float PreviousAngle;
        private float CorrectiveAngle;
        private float PreviousX;
        private float PreviousY;
        private int SameLocationCount;

        // zone check
        private int InZone;
        private float ZoneX;
        private float ZoneY;

        // pickup check
        private int PreviousPickupId;
        private Dictionary<int /*id*/, int/*count*/> PreviousPickups;
        private const int MaxPickupAttempts = 5;
        private const int MinAmmo = 50;

        public bool IsTouching(ElementProximity element, float radius)
        {
            return element.Distance < radius;
        }

        private bool IsStuck()
        {
            // try to determine if we have not moved in a while

            // init
            if (PreviousX == 0) PreviousX = X;
            if (PreviousY == 0) PreviousY = Y;

            // check if basically the same place
            if (Math.Abs(PreviousX - X) < 10f && Math.Abs(PreviousY - Y) < 10f)
            {
                // about the same place
                SameLocationCount++;
            }
            else
            {
                // reset
                SameLocationCount = 0;
            }
            PreviousX = X;
            PreviousY = Y;

            return (SameLocationCount > 100);
        }
        #endregion
    }
}
