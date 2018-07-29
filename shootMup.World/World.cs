using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
            int width = 10000;
            int height = 10000;

            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(TranslateCoordinates);
            ZoomFactor = 1;

            // setup player
            WindowX = 200;
            WindowY = 200;
            Human = new Player() { X = WindowX, Y = WindowY, Name = "You" };

            // add all the players
            Players = new Player[20];
            Players[0] = Human;

            for(int i=1; i<Players.Length; i++)
            {
                float diag = (width / Players.Length) * i;
                if (diag < 100) throw new Exception("Too many ai players for this board size");
                Players[i] = new SimpleAI() { X = diag, Y = diag, Name = string.Format("ai{0}", i) }; // AI
            }

            // create map
            Map = new Map(width, height, Players);
            Map.OnEphemerialEvent += (item) =>
            {
                lock (Ephemerial)
                {
                    Ephemerial.Add(item);
                }
            };
            Map.OnElementHit += (item) =>
            {
                if (item is Player && item.Id == Human.Id)
                {
                    Sounds.Play(Human.HurtSoundPath);
                }
            };

            // start the players in the air
            if (true)
            {
                ParachuteTimers = new Timer[ Players.Length ];

                ZoomFactor = 0.05f;

                for (int i=0; i<Players.Length; i++)
                {
                    Players[i].Z = Constants.Sky;
                    ParachuteTimers[i] = new Timer(PlayerParachute, i, 0, 500);
                }
            }

            // startup the timer to drive the AI
            if (Players != null)
            {
                AITimers = new Timer[Players.Length];
                for (int i = 0; i < Players.Length; i++)
                {
                    if (Players[i].Id == Human.Id) continue;
                    AITimers[i] = new Timer(AIMove, i, 0, 100);
                }
            }

            // sounds
            Sounds = sounds;

            // initially render all the elements
            Paint();
        }

        public void Paint()
        {
            // draw the map
            // clear the board
            Surface.Clear(new RGBA() { R = 70, G = 169, B = 52, A = 255 });

            // draw all elements
            foreach (var elem in Map.WithinWindow(Human.X, Human.Y, Surface.Width * (1 / ZoomFactor), Surface.Height * (1 / ZoomFactor)))
            {
                if (elem is Player) continue;
                if (elem.IsDead) continue;
                if (elem.IsTransparent)
                {
                    // if the player is intersecting with this item, then do not display it
                    if (Map.IsTouching(Human, elem)) continue;
                }
                elem.Draw(Surface);
            }

            // TODO! AI players under roofs require special logic

            // draw the players
            int alive = 0;
            foreach(var othr in Players)
            {
                if (othr.IsDead) continue;
                alive++;
                othr.Draw(Surface);
            }

            // add any ephemerial elements
            lock (Ephemerial)
            {
                var toremove = new List<EphemerialElement>();
                var messageShown = false;
                foreach (var b in Ephemerial)
                {
                    if (b is Message)
                    {
                        // only show one message at a time
                        if (messageShown) continue;
                        messageShown = true;
                    }
                    b.Draw(Surface);
                    b.Duration--;
                    if (b.Duration < 0) toremove.Add(b);
                }
                foreach (var b in toremove)
                {
                    Ephemerial.Remove(b);
                }
            }

            // display the player counts
            Surface.DisableTranslation();
            {
                Surface.Text(RGBA.Black, Surface.Width - 200, 10, string.Format("Alive {0} of {1}", alive, Players.Length));
            }
            Surface.EnableTranslation();
        }

        public void KeyPress(char key)
        {
            float xdelta = 0;
            float ydelta = 0;

            switch (key)
            {
                // move
                case Constants.Down:
                case Constants.Down2:
                case Constants.DownArrow:
                    ydelta = 1;
                    break;
                case Constants.Left:
                case Constants.Left2:
                case Constants.LeftArrow:
                    xdelta = -1;
                    break;
                case Constants.Right:
                case Constants.Right2:
                case Constants.RightArrow:
                    xdelta = 1;
                    break;
                case Constants.Up:
                case Constants.Up2:
                case Constants.UpArrow:
                    ydelta = -1;
                    break;

                case Constants.Switch:
                    SwitchWeapon(Human);
                    break;

                case Constants.Pickup:
                case Constants.Pickup2:
                    Pickup(Human);
                    break;

                case Constants.Drop3:
                case Constants.Drop2:
                case Constants.Drop4:
                case Constants.Drop:
                    Drop(Human);
                    break;

                case Constants.Reload:
                case Constants.MiddleMouse:
                    Reload(Human);
                    break;

                case Constants.Space:
                case Constants.LeftMouse:
                    Shoot(Human);
                    break;

                case Constants.RightMouse:
                    // use the mouse to move in the direction of the angle
                    float r = (Human.Angle % 90) / 90f;
                    xdelta = 1 * r;
                    ydelta = 1 * (1 - r);
                    if (Human.Angle > 0 && Human.Angle < 90) ydelta *= -1;
                    else if (Human.Angle > 180 && Human.Angle <= 270) xdelta *= -1;
                    else if (Human.Angle > 270) { ydelta *= -1; xdelta *= -1; }
                    break;
            }

            // if a move command, then move
            if (xdelta != 0 || ydelta != 0) Move(Human, xdelta, ydelta);
        }

        public void Mousewheel(float delta)
        {
            if (delta < 0) ZoomFactor -= Constants.ZoomStep;
            else if (delta > 0) ZoomFactor += Constants.ZoomStep;
            if (ZoomFactor < Constants.ZoomStep) ZoomFactor = Constants.ZoomStep;
        }

        public void Mousemove(float x, float y, float angle)
        {
            Turn(Human, angle);
        }

        #region private
        private IGraphics Surface;
        private Player Human;
        private List<EphemerialElement> Ephemerial;
        private float ZoomFactor;
        private ISounds Sounds;
        private Map Map;
        private float WindowX;
        private float WindowY;
        private Timer[] ParachuteTimers;
        private Player[] Players;
        private Timer[] AITimers;

        private const string NothingSoundPath = "media/nothing.wav";
        private const string PickupSoundPath = "media/pickup.wav";

        private void PlayerParachute(object state)
        {
            int index = (int)state;

            if (Players[index].Z <= Constants.Ground)
            {
                Players[index].Z = Constants.Ground;
                ParachuteTimers[index].Dispose();

                // check if the player is touching an object, if so then move
                int count = 100;
                do
                {
                    float xdelta = 1f;
                    float ydelta = 0;
                    if (Map.Move(Players[index], ref xdelta, ref ydelta))
                    {
                        break;
                    }

                    // move over
                    Players[index].X += 10f;
                    if (Players[index].Id == Human.Id) WindowX += 10f;
                }
                while (count-- > 0);

                if (count <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to move after parachute");
                }

                return;
            }

            // decend
            Players[index].Z -= (Constants.ZoomStep/2);

            if (Players[index].Id == Human.Id)
            {
                // zoom in
                ZoomFactor += (Constants.ZoomStep / 2);
            }
        }

        private void AIMove(object state)
        {
            int index = (int)state;
            Stopwatch timer = new Stopwatch();

            timer.Start();
            if (Players[index] is AI)
            {
                AI ai = Players[index] as AI;
                float xdelta = 0;
                float ydelta = 0;
                float angle = 0;

                if (System.Threading.Interlocked.CompareExchange(ref ai.RunningState, 1, 0) != 0) return;

                if (ai.IsDead)
                {
                    // stop the timer
                    AITimers[index].Dispose();
                    return;
                }

                // TODO will likely want to translate into a copy of the list with reduced details
                List<Element> elements = Map.WithinWindow(ai.X, ai.Y, Surface.Width * (1 / ZoomFactor), Surface.Height * (1 / ZoomFactor)).ToList();

                var action = ai.Action(elements, ref xdelta, ref ydelta, ref angle);

                if (Constants.Debug_AIMoveDiag) System.Diagnostics.Debug.WriteLine("AI {0} {1} {2} {3}", action, angle, xdelta, ydelta);

                // turn
                ai.Angle = angle;

                // perform action
                Type item = null;
                switch(action)
                {
                    case AIActionEnum.Drop:
                        item = Map.Drop(ai);
                        ai.Feedback(action, item, item != null);
                        break;
                    case AIActionEnum.Pickup:
                        item = Map.Pickup(ai);
                        ai.Feedback(action, item, item != null);
                        break;
                    case AIActionEnum.Reload:
                        var reloaded = ai.Reload();
                        ai.Feedback(action, reloaded, reloaded == GunStateEnum.Reloaded);
                        break;
                    case AIActionEnum.Shoot:
                        var shoot = Map.Shoot(ai);
                        ai.Feedback(action, shoot, shoot == GunStateEnum.Fired || shoot == GunStateEnum.FiredAndKilled || shoot == GunStateEnum.FiredWithContact);
                        break;
                    case AIActionEnum.SwitchWeapon:
                        var swap = ai.SwitchWeapon();
                        ai.Feedback(action, null, swap);
                        break;
                    case AIActionEnum.Move:
                    case AIActionEnum.None:
                        break;
                    default: throw new Exception("Unknown ai action : " + action);
                }
            
                // move last
                var moved = Map.Move(ai, ref xdelta, ref ydelta);
                ai.Feedback(AIActionEnum.Move, null, moved);

                // set state back to not running
                System.Threading.Volatile.Write(ref ai.RunningState, 0);
            }
            timer.Stop();

            if (timer.ElapsedMilliseconds > 30) System.Diagnostics.Debug.WriteLine("**AIMove Duration {0} ms", timer.ElapsedMilliseconds);
        }

        // support
        private bool TranslateCoordinates(bool autoScale, float x, float y, float width, float height, float other, out float tx, out float ty, out float twidth, out float theight, out float tother)
        {
            tx = ty = twidth = theight = tother = 0;

            float zoom = (autoScale) ? ZoomFactor : 1;

            // determine scaling factor
            float scale = (1 / zoom);
            width *= zoom;
            height *= zoom;

            // Surface.Width & Surface.Height are the current windows width & height
            float windowHWidth = Surface.Width / 2.0f;
            float windowHHeight = Surface.Height / 2.0f;

            // check if in the window (do not use these as screen coordinates)
            float x1 = WindowX - (windowHWidth*scale);
            float y1 = WindowY - (windowHHeight * scale);
            float x2 = WindowX + (windowHWidth * scale);
            float y2 = WindowY + (windowHHeight * scale);

            // check if inside the window
            if (x < (x1 - width) || x > (x2 + width)) return false;
            if (y < (y1 - height) || y > (y2 + height)) return false;

            // now translate to the window
            tx = ((x - WindowX) * zoom) + windowHWidth;
            ty = ((y - WindowY) * zoom) + windowHHeight;
            twidth = width;
            theight = height;
            tother = other * zoom;

            return true;
        }

        // human movements
        private void SwitchWeapon(Player player)
        {
            if (player.IsDead) return;
            player.SwitchWeapon();
        }

        private void Pickup(Player player)
        {
            if (player.IsDead) return;
            if (Map.Pickup(player) != null)
            {
                // play sound
                Sounds.Play(PickupSoundPath);
            }
        }

        private void Drop(Player player)
        { 
            if (player.IsDead) return;
            Map.Drop(player);
        }

        private void Reload(Player player)
        {
            if (player.IsDead) return;
            var state = player.Reload();
            switch (state)
            {
                case GunStateEnum.Reloaded:
                    Sounds.Play(player.Primary.ReloadSoundPath());
                    break;
                case GunStateEnum.None:
                case GunStateEnum.NoRounds:
                    Sounds.Play(NothingSoundPath);
                    break;
                case GunStateEnum.FullyLoaded:
                    // no sound
                    break;
                default: throw new Exception("Unknown GunState : " + state);
            }
        }

        private void Shoot(Player player)
        {
            if (player.IsDead) return;
            var state = Map.Shoot(player);

            // play sounds
            switch (state)
            {
                case GunStateEnum.FiredWithContact:
                case GunStateEnum.FiredAndKilled:
                case GunStateEnum.Fired:
                    Sounds.Play(player.Primary.FiredSoundPath());
                    break;
                case GunStateEnum.NoRounds:
                case GunStateEnum.NeedsReload:
                    Sounds.Play(player.Primary.EmptySoundPath());
                    break;
                case GunStateEnum.LoadingRound:
                case GunStateEnum.None:
                    Sounds.Play(NothingSoundPath);
                    break;
                default: throw new Exception("Unknown GunState : " + state);
            }
        }

        private void Move(Player player, float xdelta, float ydelta)
        {
            if (player.IsDead) return;
            if (Map.Move(player, ref xdelta, ref ydelta))
            {
                // move the screen
                WindowX += xdelta;
                WindowY += ydelta;
            }
            else
            {
                // TODO may want to move back a bit in the opposite direction
            }
        }

        private void Turn(Player player, float angle)
        {
            if (player.IsDead) return;
            player.Angle = angle;
        }
        #endregion
    }
}
