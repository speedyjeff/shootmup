using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using shootMup.Common;

namespace shootMup
{
    public class World
    {
        public World(IGraphics surface, ISounds sounds)
        {
            Player = new Player() { X = 200, Y = 200 };

            Map = new Map(Player /* human */, null /* other players */);
            ZoomFactor = 0;

            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(Map.TranslateCoordinates);

            // sounds
            Sounds = sounds;

            // initially render all the elements
            Paint();
        }

        public void Paint()
        {
            // draw all the elements
            Surface.Clear(new RGBA() { R = 70, G = 169, B = 52, A = 255 });

            // add any bullets
            var toremove = new List<EphemerialElement>();
            foreach (var b in Map.Ephemerial)
            {
                b.Draw(Surface);
                b.Duration--;
                if (b.Duration < 0) toremove.Add(b);
            }
            foreach (var b in toremove)
            {
                Map.Ephemerial.Remove(b);
            }

            // TODO find a better way to avoid drawing all the elements
            foreach (var elem in Map.All.Values)
            {
                if (elem.Id == Player.Id) continue;
                if (elem.IsDead) continue;
                if (elem.IsTransparent)
                {
                    // if the player is intersecting with this item, then do not display it
                    if (Map.IsTouching(Player, elem)) continue;
                }
                elem.Draw(Surface);
            }
            // draw the player last
            Player.Draw(Surface);
        }

        public void KeyPress(char key)
        {
            float xdelta = 0;
            float ydelta = 0;
            float speed = Constants.Speed * SpeedFactor;

            switch (key)
            {
                // move
                case Constants.S:
                case Constants.s:
                case Constants.DownArrow:
                    ydelta = speed;
                    break;
                case Constants.A:
                case Constants.a:
                case Constants.LeftArrow:
                    xdelta = -1* speed;
                    break;
                case Constants.D:
                case Constants.d:
                case Constants.RightArrow:
                    xdelta = speed;
                    break;
                case Constants.W:
                case Constants.w:
                case Constants.UpArrow:
                    ydelta = -1* speed;
                    break;

                case Constants.x1:
                    SwitchWeapon(Player);
                    break;

                case Constants.F:
                case Constants.f:
                    Pickup(Player);
                    break;

                case Constants.Q:
                case Constants.x2:
                case Constants.q:
                case Constants.x0:
                    Drop(Player);
                    break;

                case Constants.R:
                case Constants.r:
                    Reload(Player);
                    break;

                case Constants.Space:
                case Constants.LeftMouse:
                    Shoot(Player);
                    break;

                case Constants.RightMouse:
                    // use the mouse to move in the direction of the angle
                    float r = (Player.Angle % 90) / 90f;
                    xdelta = speed * r;
                    ydelta = speed * (1 - r);
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
            ZoomFactor += delta;
        }

        public void Mousemove(float x, float y, float angle)
        {
            Turn(Player, angle);
        }

        #region private
        private IGraphics Surface;
        private Player Player;
        private int SpeedFactor = 2;
        private float ZoomFactor;
        private ISounds Sounds;
        private Map Map;

        private const string NothingSoundPath = "media/nothing.wav";
        private const string PickupSoundPath = "media/pickup.wav";

        private void SwitchWeapon(Player player)
        {
            Map.SwitchWeapon(player);
        }

        private void Pickup(Player player)
        {
            if (Map.Pickup(player))
            {
                // play sound
                Sounds.Play(PickupSoundPath);
            }
        }

        private void Drop(Player player)
        {
            Map.Drop(player);
        }

        private void Reload(Player player)
        {
            var state = Map.Reload(player);
            switch (state)
            {
                case GunStateEnum.Reloaded: Sounds.Play(player.Primary.ReloadSoundPath()); break;
                case GunStateEnum.None:
                case GunStateEnum.NoRounds: Sounds.Play(NothingSoundPath); break;
                default: throw new Exception("Unknown GunState : " + state);
            }
        }

        private void Shoot(Player player)
        {
            var state = Map.Shoot(player);

            // play sounds
            switch (state)
            {
                case GunStateEnum.Fired: Sounds.Play(player.Primary.FiredSoundPath()); break;
                case GunStateEnum.NeedsReload: Sounds.Play(player.Primary.EmptySoundPath()); break;
                case GunStateEnum.None: Sounds.Play(NothingSoundPath); break;
                default: throw new Exception("Unknown GunState : " + state);
            }
        }

        private void Move(Player player, float xdelta, float ydelta)
        {
            if (!Map.Move(player, xdelta, ydelta))
            {
                // TODO may want to move back a bit in the opposite direction
            }
        }

        private void Turn(Player player, float angle)
        {
            Map.Turn(player, angle);
        }
        #endregion
    }
}
