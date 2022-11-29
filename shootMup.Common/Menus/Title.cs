using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Title : Menu
    {
        public Title(int players) : base()
        {
            Players = players;
        }

        public override void Draw(IGraphics g)
        {
            // draw title
            var top = 100;
            var left = 100;
            var width = 1200;
            var height = 700;

            g.Rectangle(TransparentWhite, top, left, width, height);
            left += 10;
            top += 10;
            g.Text(RGBA.Black, left, top, "Welcome to shoot-M-up", 32);
            top += 100;
            g.Text(RGBA.Black, left, top, "Shortly you will be parachuting into a foreign land");
            top += 100;
            g.Text(RGBA.Black, left, top, $"along with {Players - 1} enemies... run quickly to acquire");
            top += 100;
            g.Text(RGBA.Black, left, top, "a weapon, avoid the zone, and try to survive.");
            top += 100;
            g.Image(KeyboardLayoutImage.Image, left+300, top, 320, 270);
            g.Image(MouseLayoutImage.Image, left + 650, top, 250, 300);
            top += 200;
            g.Text(RGBA.Black, left, top, "[esc] to start");
        }

        #region private
        private ImageSource KeyboardLayoutImage = new ImageSource("keyboard");
        private ImageSource MouseLayoutImage = new ImageSource("mouse");
        private int Players;

        private readonly RGBA TransparentWhite = new RGBA() { R = 255, G = 255, B = 255, A = 200 };
        #endregion
    }
}
