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
            var width = 500;
            var height = 300;

            if (g.Width < width || g.Height < height) throw new Exception("The title menu assumes at least " + width + "x" + height);

            g.DisableTranslation();
            {
                g.Rectangle(new RGBA() { R = 255, G = 255, B = 255, A = 200 }, top, left, width, height);
                left += 10;
                top += 10;
                if (Ranking == 1)
                {
                    g.Text(RGBA.Black, left, top, "Winner winner chicken dinner", 24);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Winner))
                    {
                        g.Text(RGBA.Black, left, top, string.Format("You placed #{0}", Ranking), 24);
                    }
                    else
                    {
                        g.Text(RGBA.Black, left, top, string.Format("You placed #{0}, {1} won!", Ranking, Winner), 24);
                    }
                }
                top += 50;
                g.Text(RGBA.Black, left, top, string.Format("You killed {0} players", Kills));
                top += 50;
                g.Text(RGBA.Black, left, top, "Top Players:");
                for (int i=0; i<7; i++)
                {
                    top += 20;
                    if (i < TopPlayers.Length)
                    {
                        g.Text(RGBA.Black, left, top, string.Format("#{0}: {1}", i+1, TopPlayers[i]));
                    }
                } 
                top += 20;
                if (Ranking != 1)
                {
                    g.Text(RGBA.Black, left, top, "[esc] to spectate");
                }
            }
            g.EnableTranslation();

            base.Draw(g);
        }
    }
}
