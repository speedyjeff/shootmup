using engine.Common;
using engine.Common.Entities;
using System;

namespace shootMup.Common
{
    internal enum WeaponStyle
    {
        Pistol,
        Shotgun,
        Rifle
    }

    internal static class ArenaArt
    {
        internal static readonly RGBA Ink = new RGBA() { R = 25, G = 31, B = 38, A = 255 };
        internal static readonly RGBA Shadow = new RGBA() { R = 20, G = 31, B = 35, A = 90 };
        internal static readonly RGBA Sand = new RGBA() { R = 224, G = 193, B = 132, A = 255 };
        internal static readonly RGBA Rust = new RGBA() { R = 173, G = 74, B = 55, A = 255 };
        internal static readonly RGBA RustLight = new RGBA() { R = 230, G = 116, B = 73, A = 255 };
        internal static readonly RGBA Steel = new RGBA() { R = 78, G = 92, B = 104, A = 255 };
        internal static readonly RGBA SteelLight = new RGBA() { R = 143, G = 164, B = 166, A = 255 };
        internal static readonly RGBA Cyan = new RGBA() { R = 53, G = 214, B = 200, A = 255 };
        internal static readonly RGBA Gold = new RGBA() { R = 247, G = 187, B = 75, A = 255 };
        internal static readonly RGBA Coral = new RGBA() { R = 241, G = 91, B = 82, A = 255 };
        internal static readonly RGBA Leaf = new RGBA() { R = 53, G = 124, B = 87, A = 255 };
        internal static readonly RGBA LeafLight = new RGBA() { R = 92, G = 171, B = 106, A = 255 };

        internal static void DrawShadow(IGraphics g, float x, float y, float width, float height)
        {
            g.Ellipse(Shadow, x - width / 2, y - height / 2, width, height, true, false);
        }

        internal static void DrawGroundWeapon(IGraphics g, float x, float y, float width, WeaponStyle style)
        {
            DrawShadow(g, x + 5, y + 9, width, 12);

            var body = style == WeaponStyle.Pistol ? Cyan : style == WeaponStyle.Shotgun ? Gold : Coral;
            var barrelLength = style == WeaponStyle.Pistol ? width * .48f : width * .65f;
            g.Rectangle(Ink, x - width * .38f, y - 7, barrelLength + 6, 14, true, false);
            g.Rectangle(body, x - width * .38f, y - 4, barrelLength, 8, true, false);
            g.Rectangle(SteelLight, x + width * .08f, y - 2, width * .28f, 4, true, false);

            if (style == WeaponStyle.Pistol)
            {
                g.Rectangle(Ink, x - width * .12f, y + 3, width * .14f, 18, true, false);
                g.Rectangle(Steel, x - width * .09f, y + 4, width * .08f, 14, true, false);
            }
            else
            {
                g.Triangle(Rust, x - width * .38f, y - 7, x - width * .52f, y, x - width * .38f, y + 7, true, true, 3);
                g.Rectangle(Ink, x - width * .02f, y + 3, width * .12f, 16, true, false);
                g.Rectangle(Steel, x, y + 4, width * .08f, 12, true, false);
            }

            if (style == WeaponStyle.Rifle)
            {
                g.Triangle(Gold, x + width * .01f, y + 4, x + width * .18f, y + 18, x + width * .22f, y + 4, true, true, 2);
            }
        }

        internal static void DrawHeldWeapon(IGraphics g, Player player, WeaponStyle style)
        {
            var length = style == WeaponStyle.Pistol ? player.Width * .9f : player.Width * 1.25f;
            Collision.CalculateLineByAngle(player.X, player.Y, player.Angle, length, out var x1, out var y1, out var x2, out var y2);
            var color = style == WeaponStyle.Pistol ? Cyan : style == WeaponStyle.Shotgun ? Gold : Coral;

            g.Line(Ink, x1, y1, x2, y2, style == WeaponStyle.Pistol ? 11 : 14);
            g.Line(color, x1, y1, x2, y2, style == WeaponStyle.Pistol ? 5 : 7);
            g.Ellipse(Gold, x2 - 4, y2 - 4, 8, 8, true, false);
        }

        internal static WeaponStyle StyleFor(Element weapon)
        {
            if (weapon is Shotgun) return WeaponStyle.Shotgun;
            if (weapon is AK47) return WeaponStyle.Rifle;
            return WeaponStyle.Pistol;
        }
    }
}
