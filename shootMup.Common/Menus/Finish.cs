using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public class Finish : Menu
    {
        public Finish()
        {

        }

        public int Ranking { get; set; }
        public int Kills { get; set; }
        public string[] TopPlayers { get; set; }
        public string Winner { get; set; }

        public override void Draw(IGraphics g)
        {
            var top = 100;
            var left = 100;
            var width = 1000;
            var height = 600;

            if (g.Width < width || g.Height < height) throw new Exception("The title menu assumes at least " + width + "x" + height);

            g.DisableTranslation();
            {
                g.Rectangle(Backdrop, left + 12, top + 14, width, height, true, false);
                g.Rectangle(ArenaArt.Ink, left, top, width, height, true, false);
                g.Rectangle(Ranking == 1 ? ArenaArt.Gold : ArenaArt.Coral, left, top, 18, height, true, false);
                left += 52;
                top += 34;
                if (Ranking == 1)
                {
                    g.Text(ArenaArt.Gold, left, top, "LAST ONE STANDING", 30);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Winner))
                    {
                        g.Text(ArenaArt.Coral, left, top, string.Format("EXTRACTED  #{0}", Ranking), 30);
                    }
                    else
                    {
                        g.Text(ArenaArt.Coral, left, top, string.Format("EXTRACTED  #{0}", Ranking), 30);
                        g.Text(ArenaArt.SteelLight, left, top + 46, string.Format("{0} controls the arena", Winner), 16);
                    }
                }
                top += 100;
                g.Text(ArenaArt.Sand, left, top, string.Format("ELIMINATIONS  {0}", Kills), 18);
                top += 60;
                g.Text(ArenaArt.Cyan, left, top, "ARENA LEADERS", 18);
                for (int i=0; i<7; i++)
                {
                    top += 40;
                    if (i < TopPlayers.Length)
                    {
                        g.Text(i == 0 ? ArenaArt.Gold : ArenaArt.SteelLight, left, top, string.Format("{0:00}  {1}", i+1, TopPlayers[i]));
                    }
                } 
                top += 40;
                if (Ranking != 1)
                {
                    g.Text(ArenaArt.Coral, left, top, "ESC // SPECTATE", 16);
                }
            }
            g.EnableTranslation();

            base.Draw(g);
        }

        #region private
        private readonly RGBA Backdrop = new RGBA() { R = 12, G = 17, B = 21, A = 120 };
        #endregion
    }
}
