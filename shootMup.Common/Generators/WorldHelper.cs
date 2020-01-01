using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public enum DirectionEnum : byte { North = 0x1, South = 0x2, East = 0x4, West = 0x8};

    public static class WorldHelper
    {
        public static List<Element> MakeHut(float x, float y, DirectionEnum openings)
        {
            var elements = new List<Element>();

            var roof = new Roof() { X = x, Y = y };

            if (((byte)openings & (byte)DirectionEnum.West) != 0)
                elements.Add(new Wall(WallDirection.Vertical, roof.Height / 2, 20) { X = roof.X - roof.Width / 2 + 40, Y = roof.Y - 80 });
            if (((byte)openings & (byte)DirectionEnum.East) != 0)
                elements.Add(new Wall(WallDirection.Vertical, roof.Height / 2, 20) { X = roof.X + roof.Width / 2 - 40, Y = roof.Y - 80 });
            if (((byte)openings & (byte)DirectionEnum.South) != 0)
                elements.Add(new Wall(WallDirection.Horiztonal, roof.Width - 40, 20) { X = roof.X, Y = roof.Y + roof.Height / 2 - 40 });
            if (((byte)openings & (byte)DirectionEnum.North) != 0)
                elements.Add(new Wall(WallDirection.Horiztonal, roof.Width - 40, 20) { X = roof.X, Y = roof.Y - roof.Height / 2 + 40 });

            elements.Add(roof);
            return elements;         
        }

        public static List<Element> MakeBorders(float width, float height, float thickness)
        {
            return new List<Element>()
            {
                new Wall(WallDirection.Horiztonal, width, thickness) { X = width / 2, Y = thickness/2, Z=float.MaxValue },
                new Wall(WallDirection.Vertical, height, thickness) { X = thickness/2, Y = height / 2, Z=float.MaxValue },
                new Wall(WallDirection.Horiztonal, width, thickness) { X = width / 2, Y = height - thickness/2, Z=float.MaxValue },
                new Wall(WallDirection.Vertical, height, thickness) { X = width - thickness/2, Y = height / 2, Z=float.MaxValue }
            };
        }
    }
}
