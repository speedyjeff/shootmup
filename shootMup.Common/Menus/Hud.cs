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
                g.Line(ArenaArt.Ink, x1, y1, x2, y2, 10);

                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 135) % 360, 25, out x1, out y1, out x2, out y2);
                g.Line(ArenaArt.Gold, x1, y1, x2, y2, 10);

                x1 = endX;
                y1 = endY;
                Collision.CalculateLineByAngle(x1, y1, (centerAngle + 225) % 360, 25, out x1, out y1, out x2, out y2);
                g.Line(ArenaArt.Gold, x1, y1, x2, y2, 10);
            }

            // draw stats
            var alive = OnGetAlive != null ? OnGetAlive() : 0;
            var players = OnGetPlayers != null ? OnGetPlayers() : 0;
            g.Rectangle(Panel, g.Width - 360, 16, 330, 92, true, false);
            g.Text(ArenaArt.Sand, x: g.Width - 335, y: 24, string.Format("SURVIVORS  {0}/{1}", alive, players), 18);
            g.Text(ArenaArt.Coral, x: g.Width - 335, y: 60, string.Format("ELIMINATIONS  {0}", Human.Kills), 18);

            // player hud
            // health
            g.Rectangle(Panel, g.Width / 4 - 12, g.Height - 154, g.Width / 2 + 24, 76, true, false);
            g.Text(ArenaArt.Sand, g.Width / 4, g.Height - 151, "VITALS", 13);
            g.Rectangle(ArenaArt.Coral, (g.Width / 4), g.Height - 116, (Human.Health / Constants.MaxHealth) * (g.Width / 2), 22, fill: true, border: false);
            g.Rectangle(ArenaArt.Sand, g.Width / 4, g.Height - 116, g.Width / 2, 22, false, false, 3);

            // shield
            g.Rectangle(ArenaArt.Cyan, g.Width / 4, g.Height - 88, (Human.Shield / Constants.MaxShield) * (g.Width / 2), 8, fill: true, border: false);

            // primary weapon
            g.Rectangle(Panel, x: g.Width - 300, y: (g.Height / 10), width: 250, height: 80, fill: true, border: false);
            if (Human.Primary != null && Human.Primary is RangeWeapon pgun)
            {
                g.Text(ArenaArt.Gold, g.Width - 284, (g.Height / 10) + 6, pgun.Name.ToUpperInvariant(), 16);
                g.Text(ArenaArt.Sand, g.Width - 284, (g.Height / 10) + 40, $"{pgun.Clip:00} / {pgun.Ammo:000}", 18);
            }

            // secondary weapon
            g.Rectangle(Panel, g.Width - 300, (g.Height / 10) + 94, width: 250, height: 70, true, false);
            if (Human.Secondary != null && Human.Secondary.Length >= 1 && Human.Secondary[0] is RangeWeapon sgun)
            {
                g.Text(ArenaArt.SteelLight, g.Width - 284, (g.Height / 10) + 101, sgun.Name.ToUpperInvariant(), 14);
                g.Text(ArenaArt.Sand, g.Width - 284, (g.Height / 10) + 130, $"{sgun.Clip:00} / {sgun.Ammo:000}", 15);
            }
        }

        #region private
        private Player Human;
        private float MapWidth;
        private float MapHeight;
        
        private readonly RGBA Panel = new RGBA() { R = 25, G = 31, B = 38, A = 220 };
        #endregion
    }
}
