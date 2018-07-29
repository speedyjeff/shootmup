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
            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(TranslateCoordinates);
            ZoomFactor = 1;

            // setup player
            WindowX = 200;
            WindowY = 200;
            Player = new Player() { X = WindowX, Y = WindowY };

            // add bots
            OtherPlayers = new Player[1]
            {
                new SimpleAI() { X = 400, Y = 400}
            };

            // create map
            Ephemerial = new List<EphemerialElement>();
            Map = new Map(Player /* human */, OtherPlayers /* other players */);
            Map.OnEphemerialEvent += (item) =>
            {
                lock (Ephemerial)
                {
                    Ephemerial.Add(item);
                }
            };
            Map.OnElementHit += (item) =>
            {
                if (item is Player && item.Id == Player.Id)
                {
                    Sounds.Play(Player.HurtSoundPath);
                }
            };

            // start the player in the air
            if (false)
            {
                Player.Z = 1;
                ZoomFactor = 0.05f;
                ParachuteTimer = new Timer(PlayerParachute, null, 0, 500);
            }

            // startup the timer to drive the AI
            if (OtherPlayers != null)
            {
                AITimers = new Timer[OtherPlayers.Length];
                for (int i = 0; i < OtherPlayers.Length; i++)
                {
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

            // add any ephemerial elements
            lock (Ephemerial)
            {
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
            }

            // draw all elements
            foreach (var elem in Map.WithinWindow(Player.X, Player.Y, Surface.Width * (1 / ZoomFactor), Surface.Height * (1 / ZoomFactor)))
            {
                if (elem is Player) continue;
                if (elem.IsDead) continue;
                if (elem.IsTransparent)
                {
                    // if the player is intersecting with this item, then do not display it
                    if (Map.IsTouching(Player, elem)) continue;
                }
                elem.Draw(Surface);
            }

            // TODO! AI players under roofs require special logic

            // draw the players
            if (!Player.IsDead) Player.Draw(Surface);
            // todo draw other players
            foreach(var othr in OtherPlayers)
            {
                if (othr.IsDead) continue;
                othr.Draw(Surface);
            }
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
                    SwitchWeapon(Player);
                    break;

                case Constants.Pickup:
                case Constants.Pickup2:
                    Pickup(Player);
                    break;

                case Constants.Drop3:
                case Constants.Drop2:
                case Constants.Drop4:
                case Constants.Drop:
                    Drop(Player);
                    break;

                case Constants.Reload:
                case Constants.MiddleMouse:
                    Reload(Player);
                    break;

                case Constants.Space:
                case Constants.LeftMouse:
                    Shoot(Player);
                    break;

                case Constants.RightMouse:
                    // use the mouse to move in the direction of the angle
                    float r = (Player.Angle % 90) / 90f;
                    xdelta = 1 * r;
                    ydelta = 1 * (1 - r);
                    if (Player.Angle > 0 && Player.Angle < 90) ydelta *= -1;
                    else if (Player.Angle > 180 && Player.Angle <= 270) xdelta *= -1;
                    else if (Player.Angle > 270) { ydelta *= -1; xdelta *= -1; }
                    break;
            }

            // if a move command, then move
            if (xdelta != 0 || ydelta != 0) Move(Player, xdelta, ydelta);
        }

        public void Mousewheel(float delta)
        {
            if (delta < 0) ZoomFactor -= Constants.ZoomStep;
            else if (delta > 0) ZoomFactor += Constants.ZoomStep;
            if (ZoomFactor < Constants.ZoomStep) ZoomFactor = Constants.ZoomStep;
        }

        public void Mousemove(float x, float y, float angle)
        {
            Turn(Player, angle);
        }

        #region private
        private IGraphics Surface;
        private Player Player;
        private List<EphemerialElement> Ephemerial;
        private float ZoomFactor;
        private ISounds Sounds;
        private Map Map;
        private float WindowX;
        private float WindowY;
        private Timer ParachuteTimer;
        private Player[] OtherPlayers;
        private Timer[] AITimers;

        private const string NothingSoundPath = "media/nothing.wav";
        private const string PickupSoundPath = "media/pickup.wav";

        private void PlayerParachute(object state)
        {
            if (Player.Z <= 0)
            {
                Player.Z = 0;
                ParachuteTimer.Dispose();
                return;
            }

            // decend
            Player.Z -= (Constants.ZoomStep/2);
            ZoomFactor += (Constants.ZoomStep/2);
        }

        private void AIMove(object state)
        {
            int index = (int)state;
            Stopwatch timer = new Stopwatch();

            timer.Start();
            if (OtherPlayers[index] is AI)
            {
                AI ai = OtherPlayers[index] as AI;
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

                System.Diagnostics.Debug.WriteLine("AI {0} {1} {2} {3}", action, angle, xdelta, ydelta);

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
