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
            WindowX = 200;
            WindowY = 200;
            Player = new Player() { X = WindowX, Y = WindowY };

            Map = new Map(Player /* human */, null /* other players */);
            ZoomFactor = 1; // 100%

            // graphics
            Surface = surface;
            Surface.SetTranslateCoordinates(TranslateCoordinates);

            // sounds
            Sounds = sounds;

            // initially render all the elements
            Paint();
        }

        public void Paint()
        {
            // draw the map
            Map.Paint(Player, Surface);

            // draw the players
            Player.Draw(Surface);
            // todo draw other players\
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
        private float ZoomFactor;
        private ISounds Sounds;
        private Map Map;
        private float WindowX;
        private float WindowY;

        private const string NothingSoundPath = "media/nothing.wav";
        private const string PickupSoundPath = "media/pickup.wav";

        // support
        private bool TranslateCoordinates(float x, float y, float width, float height, float other, out float tx, out float ty, out float twidth, out float theight, out float tother)
        {
            tx = ty = twidth = theight = tother = 0;

            // determine scaling factor
            float scale = (1 / ZoomFactor);
            width *= ZoomFactor;
            height *= ZoomFactor;

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
            tx = ((x - WindowX) * ZoomFactor) + windowHWidth;
            ty = ((y - WindowY) * ZoomFactor) + windowHHeight;
            twidth = width;
            theight = height;
            tother = other * ZoomFactor;

            return true;
        }

        // human movements
        private void SwitchWeapon(Player player)
        {
            player.SwitchWeapon();
        }

        private void Pickup(Player player)
        {
            if (Map.Pickup(player) != null)
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
            player.Angle = angle;
        }
        #endregion
    }
}
