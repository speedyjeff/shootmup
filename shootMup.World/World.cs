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
            int numPlayers = 100;
            Menu = new Title() { Players = numPlayers };

            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(TranslateCoordinates);
            ZoomFactor = 1;
            Background = new Restriction(width, height);

            // sounds
            Sounds = sounds;

            // setup player
            WindowX = 50;
            WindowY = 50;
            Human = new Player() { X = WindowX, Y = WindowY, Name = "You" };

            // add all the players
            Players = new Player[numPlayers];
            Players[0] = Human;
            for(int i=1; i<Players.Length; i++) Players[i] = new SimpleAI() { Name = string.Format("ai{0}", i) };

            // create map
            Map = new Map(width, height, Players, Background, PlayerPlacement.Borders);
            Map.OnEphemerialEvent += AddEphemerialElement;
            Map.OnElementHit += HitByShoot;
            Map.OnElementDied += PlayerDied;

            // start the players in the air
            if (true)
            {
                ParachuteTimers = new Timer[ Players.Length ];

                ZoomFactor = 0.05f;

                for (int i=0; i<Players.Length; i++)
                {
                    Players[i].Z = Constants.Sky;
                    ParachuteTimers[i] = new Timer(PlayerParachute, i, 0, Constants.GlobalClock);
                }
            }

            // startup the timer to drive the AI
            AITimers = new Timer[Players.Length];
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i].Id == Human.Id) continue;
                AITimers[i] = new Timer(AIMove, i, 0, Constants.GlobalClock);
            }

            // show the title screen
            ShowMenu();

            // initially render all the elements
            Paint();
        }

        public void Paint()
        {
            // draw the map
            Background.Draw(Surface);

            // add center indicator
            var centerAngle = Collision.CalculateAngleFromPoint(Human.X, Human.Y, Background.X, Background.Y);
            float x1, y1, x2, y2;
            var distance = Math.Min(Surface.Width, Surface.Height) * 0.9f;
            Collision.CalculateLineByAngle(Surface.Width / 2, Surface.Height / 2, centerAngle, (distance / 2), out x1, out y1, out x2, out y2);
            Surface.DisableTranslation();
            {
                // draw an arrow
                var endX = x2;
                var endY = y2;
                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 180) % 360, 50, out x1, out y1, out x2, out y2);
                Surface.Line(RGBA.Black, x1, y1, x2, y2, 10);

                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 135) % 360, 25, out x1, out y1, out x2, out y2);
                Surface.Line(RGBA.Black, x1, y1, x2, y2, 10);

                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 225) % 360, 25, out x1, out y1, out x2, out y2);
                Surface.Line(RGBA.Black, x1, y1, x2, y2, 10);
            }
            Surface.EnableTranslation();

            // draw all elements
            var hidden = new bool[Players.Length];
            foreach (var elem in Map.WithinWindow(Human.X, Human.Y, Surface.Width * (1 / ZoomFactor), Surface.Height * (1 / ZoomFactor)))
            {
                if (elem is Player) continue;
                if (elem.IsDead) continue;
                if (elem.IsTransparent)
                {
                    // if the player is intersecting with this item, then do not display it
                    if (Map.IsTouching(Human, elem)) continue;

                    // check if one of the bots is hidden by this object
                    for (int i=0; i<Players.Length; i++)
                    {
                        if (Players[i].Id == Human.Id) continue;
                        hidden[i] |= Map.IsTouching(Players[i], elem);
                    }
                }
                elem.Draw(Surface);
            }

            // draw the players
            int alive = 0;
            for(int i=0; i<Players.Length; i++)
            {
                if (Players[i].IsDead) continue;
                alive++;
                if (hidden[i]) continue;
                Players[i].Draw(Surface);
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
                Surface.Text(RGBA.Black, Surface.Width - 200, 30, string.Format("Kills {0}", Human.Kills));
            }
            Surface.EnableTranslation();

            // show a menu if present
            if (Map.IsPaused)
            {
                if (Menu == null) throw new Exception("Must initalize a menu to display");
                Menu.Draw(Surface);
            }
        }

        public void KeyPress(char key)
        {
            // inputs that are accepted while a menu is displaying
            if (Map.IsPaused)
            {
                switch(key)
                {
                    // menu
                    case Constants.Esc:
                        HideMenu();
                        break;
                }

                return;
            }

            // handle the user input
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

                // menu
                case Constants.Esc:
                    ShowMenu();
                    break;
            }

            // if a move command, then move
            if (xdelta != 0 || ydelta != 0) Move(Human, xdelta, ydelta);
        }

        public void Mousewheel(float delta)
        {
            // block usage if a menu is being displayed
            if (Map.IsPaused) return;

            // only if on the ground
            if (Human.Z != Constants.Ground) return;

            // adjust the zoom
            if (delta < 0) ZoomFactor -= Constants.ZoomStep;
            else if (delta > 0) ZoomFactor += Constants.ZoomStep;

            // cap the zoom capability
            if (ZoomFactor < Constants.ZoomStep) ZoomFactor = Constants.ZoomStep;
            if (ZoomFactor > Constants.MaxZoomIn) ZoomFactor = Constants.MaxZoomIn;
        }

        public void Mousemove(float x, float y, float angle)
        {
            // block usage if a menu is being displayed
            if (Map.IsPaused) return;

            // use the angle to turn the human player
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
        private Menu Menu;
        private Background Background;
        private int PlayerRank;

        private const string NothingSoundPath = "media/nothing.wav";
        private const string PickupSoundPath = "media/pickup.wav";

        // largely used while debugging
        private void Debug_KillAll(int doNotKill)
        {
            foreach(var o in Players)
            {
                if (o.Id == Human.Id || o.Id == doNotKill) continue;
                o.Health = 0;
            }
        }

        // menu items
        private void ShowMenu()
        {
            if (Menu == null) throw new Exception("Need to initialize a menu first");
            Map.IsPaused = true;
        }

        private void HideMenu()
        {
            Map.IsPaused = false;
        }

        // callbacks to support time lapse actions
        private void PlayerParachute(object state)
        {
            // block usage if a menu is being displayed
            if (Map.IsPaused) return;

            // execute the parachute
            int index = (int)state;

            if (Players[index].Z <= Constants.Ground)
            {
                // ensure the player is on the ground
                Players[index].Z = Constants.Ground;
                ParachuteTimers[index].Dispose();

                // check if the player is touching an object, if so then move
                int count = 100;
                float xstep = 0.01f;
                float xmove = 10f;
                if (Players[index].X > Map.Width / 2)
                {
                    // move the other way
                    xstep *= -1;
                    xmove *= -1;
                }
                do
                {
                    float xdelta = xstep;
                    float ydelta = 0;
                    if (Map.Move(Players[index], ref xdelta, ref ydelta))
                    {
                        break;
                    }

                    // move over
                    Players[index].X += xmove;
                    if (Players[index].Id == Human.Id) WindowX += xmove;
                }
                while (count-- > 0);

                if (count <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to move after parachute");
                }

                return;
            }

            // decend
            Players[index].Z -= (Constants.ZoomStep/10);

            if (Players[index].Id == Human.Id)
            {
                // zoom in
                ZoomFactor += (Constants.ZoomStep / 10);
            }
        }

        private void AIMove(object state)
        {
            // block usage if a menu is being displayed
            if (Map.IsPaused) return;

            // move the AI
            int index = (int)state;
            Stopwatch timer = new Stopwatch();

            timer.Start();
            if (Players[index] is AI)
            {
                AI ai = Players[index] as AI;
                float xdelta = 0;
                float ydelta = 0;
                float angle = 0;

                // the timer is reentrant, so only allow one instance to run
                if (System.Threading.Interlocked.CompareExchange(ref ai.RunningState, 1, 0) != 0) return;

                if (ai.IsDead)
                {
                    // stop the timer
                    AITimers[index].Dispose();
                    return;
                }

                // NOTE: Do not apply the ZoomFactor (as it distorts the AI when debugging) - TODO may want to allow this while parachuting
                // TODO will likely want to translate into a copy of the list with reduced details
                List<Element> elements = Map.WithinWindow(ai.X, ai.Y, Surface.Width /** (1 / ZoomFactor)*/, Surface.Height /* (1 / ZoomFactor)*/).ToList();

                // get action from AI
                var action = ai.Action(elements, ref xdelta, ref ydelta, ref angle);

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
            
                // have the AI move
                var moved = Map.Move(ai, ref xdelta, ref ydelta);
                ai.Feedback(AIActionEnum.Move, null, moved);
               
                // ensure the player stays within the map
                if (ai.X < 0 || ai.X > Map.Width || ai.Y < 0 || ai.Y > Map.Height)
                    System.Diagnostics.Debug.WriteLine("Out of bounds");

                // set state back to not running
                System.Threading.Volatile.Write(ref ai.RunningState, 0);
            }
            timer.Stop();

            if (timer.ElapsedMilliseconds > 100) System.Diagnostics.Debug.WriteLine("**AIMove Duration {0} ms", timer.ElapsedMilliseconds);
        }

        // support
        private bool TranslateCoordinates(bool autoScale, float x, float y, float width, float height, float other, out float tx, out float ty, out float twidth, out float theight, out float tother)
        {
            // transform the world x,y coordinates into scaled and screen coordinates
            tx = ty = twidth = theight = tother = 0;

            float zoom = (autoScale) ? ZoomFactor : 1;

            // determine scaling factor
            float scale = (1 / zoom);
            width *= zoom;
            height *= zoom;

            // Surface.Width & Surface.Height are the current windows width & height
            float windowHWidth = Surface.Width / 2.0f;
            float windowHHeight = Surface.Height / 2.0f;

            // now translate to the window
            tx = ((x - WindowX) * zoom) + windowHWidth;
            ty = ((y - WindowY) * zoom) + windowHHeight;
            twidth = width;
            theight = height;
            tother = other * zoom;

            return true;
        }

        private void AddEphemerialElement(EphemerialElement element)
        {
            lock (Ephemerial)
            {
                Ephemerial.Add(element);
            }
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

        private void HitByShoot(Element element)
        {
            // play sound if the human is hit
            if (element is Player && element.Id == Human.Id)
            {
                Sounds.Play(Human.HurtSoundPath);
            }
        }

        private void PlayerDied(Element element)
        {
            // check for winner/death (element may be any element that can take damage)
            if (element is Player)
            {
                // drop the current players goodies
                var p = element as Player;
                Map.Drop(p);
                p.SwitchWeapon();
                Map.Drop(p);

                // check how many players are still alive
                int alive = 0;
                var toplayers = new Dictionary<string, int>();
                Player lastAlive = null;
                foreach (var player in Players)
                {
                    toplayers.Add(player.Name, player.Kills);
                    if (!player.IsDead)
                    {
                        alive++;
                        lastAlive = player;
                    }
                }

                if (element.Id == Human.Id || (alive == 1 && !Human.IsDead))
                {
                    PlayerRank = alive;
                    Menu = new Finish()
                    {
                        Kills = Human.Kills,
                        Ranking = PlayerRank,
                        Winner = (alive ==1) ? Human.Name : "",
                        TopPlayers = toplayers.OrderByDescending(kvp => kvp.Value).Select(kvp => string.Format("{0} [{1}]", kvp.Key, kvp.Value)).ToArray()
                    };
                    ShowMenu();
                }
                else if (alive == 1)
                {
                    Menu = new Finish()
                    {
                        Kills = Human.Kills,
                        Ranking = PlayerRank,
                        Winner = lastAlive.Name,
                        TopPlayers = toplayers.OrderByDescending(kvp => kvp.Value).Select(kvp => string.Format("{0} [{1}]", kvp.Key, kvp.Value)).ToArray()
                    };
                    ShowMenu();
                }
            }
        }
        #endregion
    }
}
