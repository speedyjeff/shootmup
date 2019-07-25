using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using shootMup.Bots;
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

            // create players
            Human = new Player() { Name = "You" };
            int playerPosition = (new Random()).Next() % numPlayers;
            Players = new Player[numPlayers];
            for (int i = 0; i < Players.Length; i++)
            {
                if (i == playerPosition)
                    Players[i] = Human;
                else
                    Players[i] = new SimpleAI() { Name = string.Format("ai{0}", i) };
            }
            Alive = Players.Length;

            // create map
            Map = new Map(width, height, Players, Background, PlayerPlacement.Borders);
            Map.OnEphemerialEvent += AddEphemerialElement;
            Map.OnElementHit += HitByAttack;
            Map.OnElementDied += PlayerDied;

            // setup window (based on placement on the map)
            WindowX = Human.X;
            WindowY = Human.Y;

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

        public event Action OnEnd;
        public int Alive { get; private set; }

        public void Paint()
        {
            // draw the map
            Background.Draw(Surface);

            // add center indicator
            if (Human.Z == Constants.Ground)
            {
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
            }

            // draw all elements
            var hidden = new HashSet<int>();
            var visiblePlayers = new List<Player>();
            foreach (var elem in Map.WithinWindow(Human.X, Human.Y, Surface.Width * (1 / ZoomFactor), Surface.Height * (1 / ZoomFactor)))
            {
                if (elem.IsDead) continue;
                if (elem is Player)
                {
                    visiblePlayers.Add(elem as Player);
                    continue;
                }
                if (elem.IsTransparent)
                {
                    // if the player is intersecting with this item, then do not display it
                    if (Map.IsTouching(Human, elem)) continue;

                    // check if one of the bots is hidden by this object
                    for (int i=0; i<Players.Length; i++)
                    {
                        if (Players[i].Id == Human.Id) continue;
                        if (Map.IsTouching(Players[i], elem))
                        {
                            hidden.Add(Players[i].Id);
                        }
                    }
                }
                elem.Draw(Surface);
            }

            // draw the players
            foreach(var player in visiblePlayers)
            {
                if (hidden.Contains(player.Id)) continue;
                player.Draw(Surface);
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
                Surface.Text(RGBA.Black, Surface.Width - 200, 10, string.Format("Alive {0} of {1}", Alive, Players.Length));
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

            // menu
            if (key == Constants.Esc)
            {
                ShowMenu();
                return;
            }

            // for training we track the human movements (as the supervised set)
            if (Human.RecordTraining)
            {
                // capture what the user sees
                List<Element> elements = Map.WithinWindow(Human.X, Human.Y, Constants.ProximityViewWidth, Constants.ProximityViewHeight).ToList();
                var angleToCenter = Collision.CalculateAngleFromPoint(Human.X, Human.Y, Background.X, Background.Y);
                var inZone = Background.Damage(Human.X, Human.Y) > 0;
                AITraining.CaptureBefore(Human, elements, angleToCenter, inZone);
            }

            // handle the user input
            ActionEnum action = ActionEnum.None;
            bool result = false;
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
                    action = ActionEnum.SwitchWeapon;
                    result = SwitchWeapon(Human);
                    break;

                case Constants.Pickup:
                case Constants.Pickup2:
                    action = ActionEnum.Pickup;
                    result = Pickup(Human);
                    break;

                case Constants.Drop3:
                case Constants.Drop2:
                case Constants.Drop4:
                case Constants.Drop:
                    action = ActionEnum.Drop;
                    result = Drop(Human);
                    break;

                case Constants.Reload:
                case Constants.MiddleMouse:
                    action = ActionEnum.Reload;
                    result = Reload(Human);
                    break;

                case Constants.Space:
                case Constants.LeftMouse:
                    action = ActionEnum.Attack;
                    result = Attack(Human);
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
            if (xdelta != 0 || ydelta != 0)
            {
                action = ActionEnum.Move;
                result = Move(Human, xdelta, ydelta);
            }

            // for training we track the human movements (as the supervised set)
            if (Human.RecordTraining)
            {
                // capture the result
                AITraining.CaptureAfter(Human, action, xdelta, ydelta, Human.Angle, result);
            }
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

                    // move over (eg. teleport)
                    Map.RemoveItem(Players[index]);
                    Players[index].X += xmove;
                    if (Players[index].Id == Human.Id) WindowX += xmove;
                    Map.AddItem(Players[index]);
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
                    // drop the current players goodies
                    Map.Drop(ai);
                    ai.SwitchWeapon();
                    Map.Drop(ai);

                    // stop the timer
                    AITimers[index].Dispose();
                    return;
                }

                // NOTE: Do not apply the ZoomFactor (as it distorts the AI when debugging) - TODO may want to allow this while parachuting
                // TODO will likely want to translate into a copy of the list with reduced details
                List<Element> elements = Map.WithinWindow(ai.X, ai.Y, Constants.ProximityViewWidth, Constants.ProximityViewHeight).ToList();
                var angleToCenter = Collision.CalculateAngleFromPoint(ai.X, ai.Y, Background.X, Background.Y);
                var inZone = Background.Damage(ai.X, ai.Y) > 0;

                if (Constants.CaptureAITrainingData)
                {
                    // capture what the ai sees
                    AITraining.CaptureBefore(ai, elements, angleToCenter, inZone);
                }

                // get action from AI

                var action = ai.Action(elements, angleToCenter, inZone, ref xdelta, ref ydelta, ref angle);

                // turn
                ai.Angle = angle;

                // perform action
                bool result = false;
                Type item = null;
                switch(action)
                {
                    case ActionEnum.Drop:
                        item = Map.Drop(ai);
                        result |= (item != null);
                        ai.Feedback(action, item, result);
                        break;
                    case ActionEnum.Pickup:
                        item = Map.Pickup(ai);
                        result |= (item != null);
                        ai.Feedback(action, item, result);
                        break;
                    case ActionEnum.Reload:
                        var reloaded = ai.Reload();
                        result |= (reloaded == AttackStateEnum.Reloaded);
                        ai.Feedback(action, reloaded, result);
                        break;
                    case ActionEnum.Attack:
                        var attack = Map.Attack(ai);
                        result |= attack == AttackStateEnum.FiredAndKilled || attack == AttackStateEnum.FiredWithContact ||
                            attack == AttackStateEnum.MeleeAndKilled || attack == AttackStateEnum.MeleeWithContact;
                        ai.Feedback(action, attack, result);
                        break;
                    case ActionEnum.SwitchWeapon:
                        var swap = ai.SwitchWeapon();
                        result |= swap;
                        ai.Feedback(action, null, result);
                        break;
                    case ActionEnum.Move:
                    case ActionEnum.None:
                        break;
                    default: throw new Exception("Unknown ai action : " + action);
                }

                // have the AI move
                float oxdelta = xdelta;
                float oydelta = ydelta;
                var moved = Map.Move(ai, ref xdelta, ref ydelta);
                ai.Feedback(ActionEnum.Move, null, moved);
               
                // ensure the player stays within the map
                if (ai.X < 0 || ai.X > Map.Width || ai.Y < 0 || ai.Y > Map.Height)
                    System.Diagnostics.Debug.WriteLine("Out of bounds");

                if (ai.RecordTraining)
                {
                    // capture what the ai sees
                    AITraining.CaptureAfter(ai, action, oxdelta, oydelta, angle,
                        action == ActionEnum.None || action == ActionEnum.Move
                        ? moved
                        : result); 
                }

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
        private bool SwitchWeapon(Player player)
        {
            if (player.IsDead) return false;
            return player.SwitchWeapon();
        }

        private bool Pickup(Player player)
        {
            if (player.IsDead) return false;
            if (Map.Pickup(player) != null)
            {
                // play sound
                Sounds.Play(PickupSoundPath);
                return true;
            }
            return false;
        }

        private bool Drop(Player player)
        { 
            if (player.IsDead) return false;
            return Map.Drop(player) != null;
        }

        private bool Reload(Player player)
        {
            if (player.IsDead) return false;
            var state = player.Reload();
            switch (state)
            {
                case AttackStateEnum.Reloaded:
                    Sounds.Play(player.Primary.ReloadSoundPath());
                    break;
                case AttackStateEnum.None:
                case AttackStateEnum.NoRounds:
                    Sounds.Play(NothingSoundPath);
                    break;
                case AttackStateEnum.FullyLoaded:
                    // no sound
                    break;
                default: throw new Exception("Unknown GunState : " + state);
            }

            return (state == AttackStateEnum.Reloaded);
        }

        private bool Attack(Player player)
        {
            if (player.IsDead) return false;
            var state = Map.Attack(player);

            // play sounds
            switch (state)
            {
                case AttackStateEnum.Melee:
                case AttackStateEnum.MeleeWithContact:
                case AttackStateEnum.MeleeAndKilled:
                    Sounds.Play(player.Fists.FiredSoundPath());
                    break;
                case AttackStateEnum.FiredWithContact:
                case AttackStateEnum.FiredAndKilled:
                case AttackStateEnum.Fired:
                    Sounds.Play(player.Primary.FiredSoundPath());
                    break;
                case AttackStateEnum.NoRounds:
                case AttackStateEnum.NeedsReload:
                    Sounds.Play(player.Primary.EmptySoundPath());
                    break;
                case AttackStateEnum.LoadingRound:
                case AttackStateEnum.None:
                    Sounds.Play(NothingSoundPath);
                    break;
                default: throw new Exception("Unknown GunState : " + state);
            }

            return (state == AttackStateEnum.MeleeAndKilled ||
                state == AttackStateEnum.MeleeWithContact ||
                state == AttackStateEnum.FiredAndKilled ||
                state == AttackStateEnum.FiredWithContact);
        }

        private bool Move(Player player, float xdelta, float ydelta)
        {
            if (player.IsDead) return false;
            if (Map.Move(player, ref xdelta, ref ydelta))
            {
                // move the screen
                WindowX += xdelta;
                WindowY += ydelta;

                return true;
            }
            else
            {
                // TODO may want to move back a bit in the opposite direction
                return false;
            }
        }

        private void Turn(Player player, float angle)
        {
            if (player.IsDead) return;
            player.Angle = angle;
        }

        // callbacks
        private void HitByAttack(Element element)
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
                Alive = alive;

                var winners = toplayers.OrderByDescending(kvp => kvp.Value).Select(kvp => string.Format("{0} [{1}]", kvp.Key, kvp.Value)).ToArray();

                if (element.Id == Human.Id || (alive == 1 && !Human.IsDead))
                {
                    if (Constants.CaptureAITrainingData)
                    {
                        AITraining.CaptureWinners(winners);
                    }

                    PlayerRank = alive;
                    Menu = new Finish()
                    {
                        Kills = Human.Kills,
                        Ranking = PlayerRank,
                        Winner = (alive ==1) ? Human.Name : "",
                        TopPlayers = winners
                    };
                    ShowMenu();

                    if (OnEnd != null) OnEnd();
                }
                else if (alive == 1)
                {
                    if (Constants.CaptureAITrainingData)
                    {
                        AITraining.CaptureWinners(winners);
                    }

                    Menu = new Finish()
                    {
                        Kills = Human.Kills,
                        Ranking = PlayerRank,
                        Winner = lastAlive.Name,
                        TopPlayers = winners
                    };
                    ShowMenu();

                    if (OnEnd != null) OnEnd();
                }
            }
        }
        #endregion
    }
}
