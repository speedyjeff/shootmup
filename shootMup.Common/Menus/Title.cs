using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Title : Menu
    {
        public Title() : base()
        {
        }

        public string KeyboardLayoutPath => "media/keyboard.png";
        public string MouseLayoutPath => "media/mouse.png";

        public int Players { get; set; }

        public override void Draw(IGraphics g)
        {
            // initialization
            if (KeyboardLayoutImage == null) KeyboardLayoutImage = g.CreateImage(KeyboardLayoutPath);
            if (MouseLayoutImage == null) MouseLayoutImage = g.CreateImage(MouseLayoutPath);

            // draw title
            var top = 100;
            var left = 100;
            var width = 1200;
            var height = 700;

            g.DisableTranslation();
            {
                g.Rectangle(new RGBA() { R = 255, G = 255, B = 255, A = 200 }, top, left, width, height);
                left += 10;
                top += 10;
                g.Text(RGBA.Black, left, top, "Welcome to shoot-M-up", 32);
                top += 100;
                g.Text(RGBA.Black, left, top, "Shortly you will be parachuting into a foreign land");
                top += 100;
                g.Text(RGBA.Black, left, top, string.Format("along with {0} enemies... run quickly to acquire", Players-1));
                top += 100;
                g.Text(RGBA.Black, left, top, "a weapon, avoid the zone, and try to survive.");
                top += 100;
                g.Image(KeyboardLayoutImage, left, top, 190, 140);
                g.Image(MouseLayoutImage, left + 250, top, 120, 170);
                top += 150;
                g.Text(RGBA.Black, left, top, "[esc] to start");
            }
            g.EnableTranslation();

            base.Draw(g);
        }

        #region private
        private IImage KeyboardLayoutImage;
        private IImage MouseLayoutImage;
        #endregion
    }
}
