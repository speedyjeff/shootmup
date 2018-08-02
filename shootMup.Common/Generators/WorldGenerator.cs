using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public static class WorldGenerator
    {
        public static void Test(int width, int height, Dictionary<int, Element> obstacles, Dictionary<int, Element> items)
        {
            if (width < 1000 || height < 1000) throw new Exception("Must have at least 1000 wdith & height");

            // borders
            foreach (var elem in WorldHelper.MakeBorders(width, height, 20))
                obstacles.Add(elem.Id, elem);
            // hut
            foreach (var elem in WorldHelper.MakeHut(700, 700, (DirectionEnum.East | DirectionEnum.South | DirectionEnum.West)))
                obstacles.Add(elem.Id, elem);
            // items
            foreach (var elem in new Element[] {
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
            })
                items.Add(elem.Id, elem);
        }

        public static void Randomgen(int width, int height, Dictionary<int, Element> obstacles, Dictionary<int, Element> items)
        {
            // this size is determined by the largest element (the Hut)
            int chunkSize = 400;
            Random rand = new Random();

            if (width < chunkSize || height < chunkSize) throw new Exception("Must have at least " + chunkSize + " pixels to generate a board");

            // break the board up into 400x400 chunks, within those chunks we will then fill with a few particular patterns

            for(int h = 0; h < height / chunkSize; h++)
            {
                for(int w = 0; w < width / chunkSize; w++)
                {
                    float x = (h * chunkSize) + (chunkSize / 2);
                    float y = (w * chunkSize) + (chunkSize / 2);

                    // potential item location
                    float ix = x;
                    float iy = y;

                    // generate random obstacle
                    var solids = RandomgenObstacle(x, y, chunkSize, rand);

                    if (solids != null && solids.Count == 1)
                    {
                        // adjust the item placement
                        if (solids[0] is Rock) ix -= (chunkSize / 2);
                        else if (solids[0] is Wall)
                        {
                            if ((solids[0] as Wall).Width > (solids[0] as Wall).Height)
                            {
                                // horizontal
                                iy += chunkSize / 2;
                            }
                            else
                            {
                                // vertical
                                ix += chunkSize / 2;
                            }
                        }
                        else if (solids[0] is Tree)
                        {
                            // pick an x and y that is not within width and height of the tree
                            var window = chunkSize - 50;
                            do
                            {
                                ix = (x - (window / 2)) + ((float)(rand.Next() % 100) / 100f) * window;
                            }
                            while (Math.Abs(ix - solids[0].X) < solids[0].Width * 2);
                            do
                            {
                                iy = (y - (window / 2)) + ((float)(rand.Next() % 100) / 100f) * window;
                            }
                            while (Math.Abs(iy - solids[0].Y) < solids[0].Height * 2);
                        }
                    }

                    // place an item
                    var item = RandomgenItem(ix, iy, rand);
                    
                    if (item != null)
                    {
                        if (item.X < 0 || item.X > width ||
                            item.Y < 0 || item.Y > height)
                            System.Diagnostics.Debug.WriteLine("Put an item outside of the wall");

                        // add items
                        items.Add(item.Id, item);
                    }
                    if (solids != null)
                    {
                        foreach (var elem in solids)
                            obstacles.Add(elem.Id, elem);
                    }
                }
            }

            // make borders
            foreach (var elem in WorldHelper.MakeBorders(width, height, 20))
                obstacles.Add(elem.Id, elem);
        }

        public static void HungerGames(int width, int height, Dictionary<int, Element> obstacles, Dictionary<int, Element> items)
        {
            // put all the goodies in the middle - no health
            var rand = new Random();
            float x = width / 2;
            float y = height / 2;
            float dim = width / 10;
            int count = 100;
            while(true)
            {
                float rx = x + (rand.Next() % dim) * (rand.Next() % 2 == 0 ? -1 : 1);
                float ry = y + (rand.Next() % dim) * (rand.Next() % 2 == 0 ? -1 : 1);

                var item = RandomgenItem(rx, ry, rand);

                if (item != null && !(item is Bandage) && !(item is Helmet))
                {
                    items.Add(item.Id, item);

                    if (count-- <= 0) break;
                }
            }

            foreach (var elem in WorldHelper.MakeBorders(width, height, 20))
                obstacles.Add(elem.Id, elem);
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
                case 5:
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
                    window -= 40;
                    x = (x - (window / 2)) + ((float)(rand.Next() % 100) / 100f) * window;
                    y = (y - (window / 2)) + ((float)(rand.Next() % 100) / 100f) * window;
                    return new List<Element>() { new Tree() { X = x, Y = y } };
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
