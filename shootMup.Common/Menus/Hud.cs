using engine.Common;
using engine.Common.Entities;
using engine.Common.Entities3D;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace shootMup.Common.Menus
{
    internal class Hud : Menu
    {
        public Hud(Player human, float mapwidth, float mapheight)
        {
            Human = human;
            MapWidth = mapwidth;
            MapHeight = mapheight;
        }

        public Func<float> OnGetAlive { get; set; }
        public Func<float> OnGetPlayers { get; set; }

        public override void Draw(IGraphics g)
        {
            // draw the center indicator
            if (Human.Z == Constants.Ground)
            {
                var centerAngle = Collision.CalculateAngleFromPoint(Human.X, Human.Y, MapWidth / 2, MapHeight / 2);
                float x1, y1, x2, y2;
                var distance = Math.Min(g.Width, g.Height) * 0.9f;
                Collision.CalculateLineByAngle(g.Width / 2, g.Height / 2, centerAngle, (distance / 2), out x1, out y1, out x2, out y2);

                // draw an arrow
                var endX = x2;
                var endY = y2;
                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 180) % 360, 50, out x1, out y1, out x2, out y2);
                g.Line(RGBA.Black, x1, y1, x2, y2, 10);

                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 135) % 360, 25, out x1, out y1, out x2, out y2);
                g.Line(RGBA.Black, x1, y1, x2, y2, 10);

                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 225) % 360, 25, out x1, out y1, out x2, out y2);
                g.Line(RGBA.Black, x1, y1, x2, y2, 10);
            }

            // draw stats
            var alive = OnGetAlive != null ? OnGetAlive() : 0;
            var players = OnGetPlayers != null ? OnGetPlayers() : 0;
            g.Text(RGBA.Black, x: g.Width - 400, y: 5, string.Format("Alive {0} of {1}", alive, players));
            g.Text(RGBA.Black, x: g.Width - 400, y: 45, string.Format("Kills {0}", Human.Kills));

            // player hud
            // health
            g.Rectangle(Green, (g.Width / 4), g.Height - 120, (Human.Health / Constants.MaxHealth) * (g.Width / 2), 20, fill: true);
            g.Rectangle(RGBA.Black, g.Width / 4, g.Height - 120, g.Width / 2, 20, false);

            // shield
            g.Rectangle(Yellow, g.Width / 4, g.Height - 130, (Human.Shield / Constants.MaxShield) * (g.Width / 4), 10, fill: true);
            g.Rectangle(RGBA.Black, g.Width / 4, g.Height - 130, g.Width / 4, 10, false);

            // primary weapon
            g.Rectangle(RGBA.Black, x: g.Width - 300, y: (g.Height / 10), width: 250, height: 80, fill: false);
            if (Human.Primary != null && Human.Primary is RangeWeapon pgun)
            {
                g.Text(RGBA.Black, g.Width - 300, (g.Height / 10) - 8, pgun.Name);
                g.Text(RGBA.Black, g.Width - 300, (g.Height / 10) + 28, $"{pgun.Clip}/{pgun.Ammo}");
            }

            // secondary weapon
            g.Rectangle(RGBA.Black, g.Width - 300, (g.Height / 10) + 100, width: 250, height: 80, false);
            if (Human.Secondary != null && Human.Secondary.Length >= 1 && Human.Secondary[0] is RangeWeapon sgun)
            {
                g.Text(RGBA.Black, g.Width - 300, (g.Height / 10) + 100 - 8, sgun.Name);
                g.Text(RGBA.Black, g.Width - 300, (g.Height / 10) + 100 + 28, $"{sgun.Clip}/{sgun.Ammo}");
            }
        }

        #region private
        private Player Human;
        private float MapWidth;
        private float MapHeight;

        private readonly RGBA Green = new RGBA() { G = 255, A = 255 };
        private readonly RGBA Yellow = new RGBA() { R = 255, G = 255, A = 255 };
        #endregion
    }
}
