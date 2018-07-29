using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    enum ItemType { Player=0, Ammo=1, Weapon=2, Health=3, Sheld=4, WeaponAK47 = 5, LENGTH=6}
    struct ItemDetails
    {
        public float Distance;
        public float Angle;
        public bool IsValid { get { return Angle > 0; } }
        public bool IsTouching(float radius) { return Distance < radius; }
    }

    public class SimpleAI : AI
    {
        // Simple rules
        //  direction rules:
        //    if right next/touching an object, move around
        //    if item, possible pickup (depending on other action)
        //    move closer to items/players of interest
        //
        //  0. if no weapn, go towards weapon
        //  1. if have a weapon
        //    1.a if no ammo, go towards ammo
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

            // in general keep going in the previous direction, unless there is a reason to change
            PreviousAngle = -1;
        }

        public override AIActionEnum Action(List<Element> elements, ref float xdelta, ref float ydelta, ref float angle)
        {
            var distances = new ItemDetails[(int)ItemType.LENGTH];
            for (int i = 0; i < distances.Length; i++) { distances[i].Angle = -1; distances[i].Distance = float.MaxValue; }

            // find closest 
            foreach(var elem in elements)
            {
                if (elem.Id == Id) continue; // found myself

                // calculate the distance between thees
                //var distance = Collision.DistanceToObject(X, Y, Width, Height,
                //    elem.X, elem.Y, elem.Width, elem.Height);
                var distance = Collision.DistanceBetweenPoints(X, Y, elem.X, elem.Y);
                var elangle = Collision.CalculateAngleFromPoint(X, Y, elem.X, elem.Y);

                var index = (int)ItemType.LENGTH;
                if (elem is Player) index = (int)ItemType.Player;
                else if (elem is Bandage) index = (int)ItemType.Health;
                else if (elem is Helmet) index = (int)ItemType.Sheld;
                else if (elem is AK47) index = (int)ItemType.WeaponAK47;
                else if (elem is Gun) index = (int)ItemType.Weapon;
                else if (elem is Ammo) index = (int)ItemType.Ammo;

                // this is an item we can interact with
                if (index < distances.Length)
                {
                    // if this item is closet, set it for later review
                    if (distance < distances[index].Distance)
                    {
                        distances[index].Distance = distance;
                        distances[index].Angle = elangle;
                    }
                }
                else
                {
                    // the item may be solid and cannot move through
                }
            }

            // choose an action - set the rules in reverse order in order to set precedence
            var action = AIActionEnum.None;
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
                if (distances[(int)ItemType.Sheld].IsValid)
                {
                    // there is health either close or touching
                    if (distances[(int)ItemType.Sheld].IsTouching(Width/2))
                    {
                        // choose to pickup
                        action = AIActionEnum.Pickup;
                        // set direction via another decision
                    }
                    else
                    {
                        // choose action via another decision
                        angle = distances[(int)ItemType.Sheld].Angle;
                    }
                }
            }

            // 2) Health
            if (Health < Constants.MaxHealth)
            {
                if (distances[(int)ItemType.Health].IsValid)
                {
                    // there is health either close or touching
                    if (distances[(int)ItemType.Health].IsTouching(Width/2))
                    {
                        // choose to pickup
                        action = AIActionEnum.Pickup;
                        // set direction via another decision
                    }
                    else
                    {
                        // choose action via another decision
                        angle = distances[(int)ItemType.Health].Angle;
                    }
                }
            }

            // 1) Have weapon
            if (Primary != null)
            {
                // need ammo
                if (!Primary.HasAmmo() && distances[(int)ItemType.Ammo].IsValid)
                {
                    // there is ammo either close or touching
                    if (distances[(int)ItemType.Ammo].IsTouching(Width/2))
                    {
                        // choose to pickup
                        action = AIActionEnum.Pickup;
                        // set direction via another decision
                    }
                    else
                    {
                        // choose action via another decision
                        angle = distances[(int)ItemType.Ammo].Angle;
                    }
                }

                // needs reload
                if (!Primary.RoundsInClip(out int rounds) && rounds == 0 && Primary.HasAmmo())
                {
                    // choose to reload
                    action = AIActionEnum.Reload;
                    // choose direction via another decision
                }

                // pick up ak47
                if (!(Primary is AK47) && distances[(int)ItemType.WeaponAK47].IsValid)
                {
                    // there is an AK47 either close or touching
                    if (distances[(int)ItemType.WeaponAK47].IsTouching(Width / 2))
                    {
                        // choose to pickup
                        action = AIActionEnum.Pickup;
                        // set direction via another decision
                    }
                    else
                    {
                        // choose action via another decision
                        angle = distances[(int)ItemType.WeaponAK47].Angle;
                    }
                }

                // shoot a player
                if (Primary.CanShoot() && distances[(int)ItemType.Player].IsValid)
                {
                    // choose to shoot
                    action = AIActionEnum.Shoot;
                    // move towards the player
                    angle = distances[(int)ItemType.Player].Angle;
                }
            }

            // 0) No weapon
            if (Primary == null)
            {
                var weapon = distances[(int)ItemType.WeaponAK47].IsValid ? distances[(int)ItemType.WeaponAK47] : distances[(int)ItemType.Weapon];

                // is there a weapon within view
                if (weapon.IsValid)
                {
                    // there is a weapon either close or touching
                    if (weapon.IsTouching(Width/2))
                    {
                        // choose to pickup
                        action = AIActionEnum.Pickup;
                        // set direction via another decision
                    }
                    else
                    {
                        // choose action via another decision
                        angle = weapon.Angle;
                    }
                }
            }
          
            // choose defaults
            if (action == AIActionEnum.None)
            {
                // default to moving
                action = AIActionEnum.Move;
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

        public override void Feedback(AIActionEnum action, object item, bool result)
        {
            // if the result was successful, then continue
            if (result)
            {
                switch (action)
                {
                    case AIActionEnum.Move:
                        CorrectiveAngle = 0;
                        break;
                }
                return;
            }

            // make corrective actions
            switch(action)
            {
                case AIActionEnum.Move:
                    // do some corrective action on moving
                    CorrectiveAngle += 45;
                    break;
                case AIActionEnum.Pickup:
                    if (ShowDiagnostics) System.Diagnostics.Debug.WriteLine("Failed to pickup");
                    break;
            }
        }

        #region private
        private Random Rand;
        private float PreviousAngle;
        private float CorrectiveAngle;

        private float PreviousX;
        private float PreviousY;
        private int SameLocationCount;

        private bool IsStuck()
        {
            // try to determine if we have not moved in a while

            // init
            if (PreviousX == 0) PreviousX = X;
            if (PreviousY == 0) PreviousY = Y;

            // check if basically the same place
            if (Math.Abs(Math.Abs(PreviousX) - Math.Abs(X)) < 1f && Math.Abs(Math.Abs(PreviousY) - Math.Abs(Y)) < 1f)
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
