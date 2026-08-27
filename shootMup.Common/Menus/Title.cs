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
            var left = 90f;
            var top = 80f;
            var width = Math.Min(1220, g.Width - 180);
            var height = Math.Min(760, g.Height - 160);

            g.Rectangle(Backdrop, left + 12, top + 14, width, height, true, false);
            g.Rectangle(ArenaArt.Ink, left, top, width, height, true, false);
            g.Rectangle(ArenaArt.Coral, left, top, 18, height, true, false);
            g.Rectangle(ArenaArt.Gold, left + 18, top, width - 18, 8, true, false);

            g.Text(ArenaArt.Sand, left + 60, top + 42, "SHOOT M UP", 48, "Arial");
            g.Text(ArenaArt.Cyan, left + 64, top + 112, "SALVAGE. OUTRUN. SURVIVE.", 18, "Arial");
            g.Line(ArenaArt.Steel, left + 60, top + 158, left + width - 60, top + 158, 3);

            g.Text(ArenaArt.Sand, left + 64, top + 192, $"DROP ZONE // {Players} CONTESTANTS", 19);
            g.Text(ArenaArt.SteelLight, left + 64, top + 234, "Scavenge weapons and armor. Stay inside the closing perimeter.", 14);
            g.Text(ArenaArt.SteelLight, left + 64, top + 270, "Every silhouette is hostile. Only one survivor leaves the arena.", 14);

            const float cardGap = 24;
            var cardLeft = left + 64;
            var cardAreaWidth = width - 128;
            var cardWidth = (cardAreaWidth - cardGap * 2) / 3;
            DrawControlCard(g, cardLeft, top + 340, cardWidth, "MOVE", "W A S D", null, ArenaArt.Cyan);
            DrawControlCard(g, cardLeft + cardWidth + cardGap, top + 340, cardWidth, "AIM + FIRE", "MOUSE", null, ArenaArt.Coral);
            DrawControlCard(g, cardLeft + (cardWidth + cardGap) * 2, top + 340, cardWidth, "GEAR", "F  PICKUP", "R  RELOAD", ArenaArt.Gold);

            g.Rectangle(ArenaArt.Coral, left + 64, top + height - 104, width - 128, 58, true, false);
            DrawCenteredText(g, ArenaArt.Ink, left + 64, top + height - 94, width - 128, "PRESS ESC TO DROP", 22);
        }

        #region private
        private static void DrawControlCard(IGraphics g, float x, float y, float width, string label, string control, string secondaryControl, RGBA accent)
        {
            g.Rectangle(Card, x, y, width, 150, true, false);
            g.Rectangle(accent, x, y, width, 8, true, false);
            DrawCenteredText(g, ArenaArt.SteelLight, x, y + 25, width, label, 14, 3);
            DrawCenteredText(g, ArenaArt.Sand, x, y + 72, width, control, secondaryControl == null ? 22 : 17, 3);
            if (secondaryControl != null)
            {
                DrawCenteredText(g, ArenaArt.Sand, x, y + 106, width, secondaryControl, 17, 3);
            }
        }

        private static void DrawCenteredText(IGraphics g, RGBA color, float x, float y, float width, string text, float fontSize, int leftCharacterOffset = 0)
        {
            const float averageCharacterWidth = .56f;
            var estimatedWidth = text.Length * fontSize * averageCharacterWidth;
            var leftOffset = leftCharacterOffset * fontSize * averageCharacterWidth;
            g.Text(color, x + Math.Max(8, (width - estimatedWidth) / 2 - leftOffset), y, text, fontSize);
        }

        private int Players;

        private static readonly RGBA Backdrop = new RGBA() { R = 12, G = 17, B = 21, A = 120 };
        private static readonly RGBA Card = new RGBA() { R = 43, G = 52, B = 60, A = 255 };
        #endregion
    }
}
