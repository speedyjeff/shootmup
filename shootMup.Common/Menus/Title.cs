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
            var top = 100;
            var left = 100;
            var width = 500;
            var height = 300;

            g.DisableTranslation();
            {
                g.Rectangle(new RGBA() { R = 255, G = 255, B = 255, A = 200 }, top, left, width, height);
                left += 10;
                top += 10;
                g.Text(RGBA.Black, left, top, "Welcome to shoot-M-up", 32);
                top += 50;
                g.Text(RGBA.Black, left, top, "Shortly you will be parachuting into a foreign land");
                top += 20;
                g.Text(RGBA.Black, left, top, string.Format("along with {0} enemies... run quickly to acquire", Players-1));
                top += 20;
                g.Text(RGBA.Black, left, top, "a weapon, avoid the zone, and try to survive.");
                top += 20;
                g.Image(KeyboardLayoutPath, left, top, 190, 140);
                g.Image(MouseLayoutPath, left + 250, top, 120, 170);
                top += 150;
                g.Text(RGBA.Black, left, top, "[esc] to start");
            }
            g.EnableTranslation();

            base.Draw(g);
        }
    }
}
