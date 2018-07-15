using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public static class WorldGenerator
    {
        public static List<Element> Test(out int width, out int height)
        {
            var elements = new List<Element>();

            // width and height
            width = 1000;
            height = 1000;

            // borders
            elements.AddRange( WorldHelper.MakeBorders(width, height, 20));
            // hut
            elements.AddRange( WorldHelper.MakeHut(700, 700, (DirectionEnum.East | DirectionEnum.South | DirectionEnum.West)));
            // items
            elements.AddRange(new Element[] {
                                    new Tree() { X = 125, Y = 125 },
                                    new Pistol() { X = 350, Y = 200 },
                                    new AK47() { X = 350, Y = 400 },
                                    new Shotgun() { X = 350, Y = 350 },
                                    new Ammo() { X = 350, Y = 150 },
                                    new Ammo() { X = 400, Y = 150 },
                                    new Ammo() { X = 450, Y = 150 },
                                    new Helmet() { X = 200, Y = 300 },
                                    new Rock() { X = 750, Y = 250 },
                                    new Bandage() { X = 350, Y = 800}
            });

            return elements;
        }
    }
}
