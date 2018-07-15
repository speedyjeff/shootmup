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

        public static List<Element> Randomgen(int width, int height)
        {
            // this size is determined by the largest element (the Hut)
            int chunkSize = 400;
            Random rand = new Random();
            var elements = new List<Element>();

            if (width < chunkSize || height < chunkSize) throw new Exception("Must have at least " + chunkSize + " pixels to generate a board");

            // break the board up into 400x400 chunks, within those chunks we will then fill with a few particular patterns

            for(int h = 0; h < height / chunkSize; h++)
            {
                for(int w = 0; w < width / chunkSize; w++)
                {
                    if (h == 0 && w == 0) continue;

                    var x = (h * chunkSize) + (chunkSize / 2);
                    var y = (w * chunkSize) + (chunkSize / 2);

                    // potential item location
                    var ix = x;
                    var iy = y;

                    // generate random obstacle
                    var obstacles = RandomgenObstacle(x, y, chunkSize, rand);

                    if (obstacles != null && obstacles.Count == 1)
                    {
                        // adjust the item placement
                        if (obstacles[0] is Rock) ix -= (width / 2);
                        if (obstacles[0] is Wall)
                        {
                            if ((obstacles[0] as Wall).Width > (obstacles[0] as Wall).Height)
                            {
                                // horizontal
                                iy += height / 2;
                            }
                            else
                            {
                                // vertical
                                ix += width / 2;
                            }
                        }
                    }

                    // place an item
                    var item = RandomgenItem(ix, iy, rand);
                    
                    if (item != null)
                    {
                        // add the item first (so it renders correctly)
                        elements.Add(item);
                    }
                    if (obstacles != null) elements.AddRange(obstacles);
                }
            }

            // make borders
            elements.AddRange( WorldHelper.MakeBorders(width, height, 20));
            
            return elements;
        }

        public static List<Element> HungerGames(int width, int height)
        {
            var elements = new List<Element>();

            // put all the goodies in the middle - no health
            var rand = new Random();
            float x = width / 2;
            float y = height / 2;
            float dim = width / 10;
            int count = 25;
            while(true)
            {
                float rx = x + (rand.Next() % dim) * (rand.Next() % 2 == 0 ? -1 : 1);
                float ry = y + (rand.Next() % dim) * (rand.Next() % 2 == 0 ? -1 : 1);

                var item = RandomgenItem(rx, ry, rand);

                if (item != null && !(item is Bandage) && !(item is Helmet))
                {
                    elements.Add(item);

                    if (count-- <= 0) break;
                }
            }

            elements.AddRange(WorldHelper.MakeBorders(width, height, 20));

            return elements;
        }

        #region private
        private static Element RandomgenItem(float x, float y, Random rand)
        {
            switch (rand.Next() % 10)
            {
                case 0:
                    // helmet
                    return new Helmet() { X = x, Y = y };
                case 1:
                    // bandage
                    return new Bandage() { X = x, Y = y };
                case 4:
                case 2:
                    // ammo
                    return new Ammo() { X = x, Y = y };
                case 3:
                    // guns
                    switch (rand.Next() % 3)
                    {
                        case 0:
                            return new Pistol() { X = x, Y = y };
                        case 1:
                            return new AK47() { X = x, Y = y };
                        case 2:
                            return new Shotgun() { X = x, Y = y };
                    }
                    break;
                default:
                    // nothing
                    break;
            }

            return null;
        }

        private static List<Element> RandomgenObstacle(float x, float y, float window, Random rand)
        {
            // choose initial obstacle
            switch (rand.Next() % 12)
            {
                case 0:
                    // hut
                    return WorldHelper.MakeHut(x, y, (DirectionEnum)((rand.Next() % 15) + 1));
                case 10:
                case 9:
                case 8:
                case 7:
                case 6:
                case 5:
                case 4:
                case 1:
                    // tree
                    var rx = (x - (window / 2)) + ((float)(rand.Next() % 100) / 100f) * window;
                    var ry = (y - (window / 2)) + ((float)(rand.Next() % 100) / 100f) * window;
                    return new List<Element>() { new Tree() { X = rx, Y = ry } };
                case 2:
                    // rock
                    return new List<Element>() { new Rock() { X = x, Y = y } };
                case 3:
                    if (rand.Next() % 2 == 0)
                    {
                        // vertical wall
                        return new List<Element>() { new Wall(WallDirection.Vertical, window, 20) { X = x, Y = y } };
                        
                    }
                    else
                    {
                        // horizontal wall
                        return new List<Element>() { new Wall(WallDirection.Horiztonal, window, 20) { X = x, Y = y } };
                        
                    }
                default:
                    // nothing
                    break;
            }

            return null;
        }
        #endregion
    }
}
