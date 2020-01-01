using engine.Common;
using engine.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace shootMup.Common
{
    public enum WorldType { Test, Random, HungerGames }
    public enum PlayerPlacement { Diagonal, Borders }

    public static class WorldGenerator
    {
        public static World Generate(WorldType type, PlayerPlacement placement, Player human, ref Player[] allPlayers)
        {
            // defaults
            var height = 10000;
            var width = 10000;
            var numPlayers = 100;

            // world details
            List<Element> objects = null;
            Background background = new Restriction(width, height); ;

            // generate the world
            switch (type)
            {
                case WorldType.Test:
                    // test world
                    width = 1000;
                    height = 1000;
                    numPlayers = 1;
                    background = new Background(width, height) { GroundColor = new RGBA() { R = 70, G = 169, B = 52, A = 255 } };
                    objects = WorldGenerator.Test(width, height);
                    break;

                case WorldType.Random:
                    // random gen
                    objects = WorldGenerator.Randomgen(width, height);
                    break;

                case WorldType.HungerGames:
                    // hunger games
                    objects = WorldGenerator.HungerGames(width, height);
                    break;

                default:
                    throw new Exception("Unknown world type " + type);
            }

            // create players
            if (allPlayers.Length < numPlayers) throw new Exception("Need more players");
            var players = new Player[numPlayers];
            var index = 0;
            players[(new Random()).Next() % numPlayers] = human;
            for (int i = 0; i < players.Length; i++) if (players[i] == null) players[i] = allPlayers[index++];

            // replace allPlayers with the actual players list
            allPlayers = players;

            // place the players in a diagnoal pattern
            if (placement == PlayerPlacement.Diagonal)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].X != 0 || players[i].Y != 0) continue;
                    float diag = (width / players.Length) * i;
                    if (diag < 100) throw new Exception("Too many ai players for this board size");
                    players[i].X = diag;
                    players[i].Y = diag;
                    players[i].Z = Constants.Sky;
                }
            }
            else if (placement == PlayerPlacement.Borders)
            {
                // place players around the borders
                float delta = (((width + height) * 2) / (players.Length + 5));
                if (delta < 100) throw new Exception("Too many ai players for this board size");
                float ydelta = delta;
                float xdelta = 0;
                float x = 50;
                float y = 50;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].X != 0 || players[i].Y != 0) continue;

                    x += xdelta;
                    y += ydelta;

                    if (y > height)
                    {
                        // bottom left corner
                        y -= delta;
                        xdelta = ydelta;
                        ydelta = 0;
                        x += xdelta;
                    }
                    else if (x > width)
                    {
                        // bottom right corner
                        x -= delta;
                        ydelta = xdelta * -1;
                        xdelta = 0;
                        y += ydelta;
                    }
                    else if (y < 0)
                    {
                        // top right corner
                        y += delta;
                        xdelta = ydelta;
                        ydelta = 0;
                        x += xdelta;
                    }
                    else if (x < 0)
                    {
                        throw new Exception("Failed to properly distribute all the players evenly");
                    }

                    if (x < 0 || x > width || y < 0 || y > height)
                    {
                        System.Diagnostics.Debug.WriteLine("Placing a player outside of the borders");
                    }

                    players[i].X = x;
                    players[i].Y = y;
                    players[i].Z = Constants.Sky;
                }
            }
            else
            {
                throw new Exception("Unknown placement strategy : " + placement);
            }

            // configuration
            var finish = new Finish();
            var title = new Title();
            var config = new WorldConfiguration()
            {
                Width = width,
                Height = height,
                EnableZoom = true,
                ShowCoordinates = false,
                DisplayStats = true,
                CenterIndicator = true,
                ApplyForces = false,
                EndMenu = finish,
                StartMenu = title
            };

            // setup game
            var world = new World(
                config,
                players,
                objects.ToArray(),
                background
                );

            // generate the top players list
            var playerRanking = 0;
            world.OnDeath += (elem) =>
            {
                // exit early if it was not a player
                if (!(elem is Player)) return;

                // check how many players are still alive
                var toplayers = new Dictionary<string, int>();
                Player lastAlive = null;
                foreach (var player in players)
                {
                    toplayers.Add(player.Name, player.Kills);
                    if (!player.IsDead)
                    {
                        lastAlive = player;
                    }
                }

                var winners = toplayers.OrderByDescending(kvp => kvp.Value).Select(kvp => string.Format("{0} [{1}]", kvp.Key, kvp.Value)).ToArray();

                // setup the finish screen
                finish.Kills = human.Kills;
                finish.Ranking = playerRanking > 0 ? playerRanking : world.Alive;
                finish.Winner = (world.Alive == 1) ? human.Name : "";
                finish.TopPlayers = winners;

                // display the finish menu, if this was the human's death
                if (human.IsDead)
                {
                    if (playerRanking == 0)
                    {
                        playerRanking = world.Alive;

                        // show the final screen
                        world.ShowMenu(finish);
                    }
                }
                // or if there is only 1 player alive
                if (world.Alive == 1) world.ShowMenu(finish);
            };

            return world;
        }

        #region private
        private static List<Element> Test(int width, int height)
        {
            if (width < 1000 || height < 1000) throw new Exception("Must have at least 1000 wdith & height");

            var objects = new List<Element>();

            // borders
            foreach (var elem in WorldHelper.MakeBorders(width, height, 20))
                objects.Add(elem);
            // hut
            foreach (var elem in WorldHelper.MakeHut(700, 700, (DirectionEnum.East | DirectionEnum.South | DirectionEnum.West)))
                objects.Add(elem);
            // items
            foreach (var elem in new Element[] {
                                    new Tree() { X = 125, Y = 125 },
                                    new Pistol() { X = 350, Y = 200 },
                                    new AK47() { X = 350, Y = 400 },
                                    new Shotgun() { X = 350, Y = 350 },
                                    new Ammo() { X = 350, Y = 150 },
                                    new Ammo() { X = 400, Y = 150 },
                                    new Ammo() { X = 450, Y = 150 },
                                    new Shield() { X = 200, Y = 300 },
                                    new Rock() { X = 750, Y = 250 },
                                    new Health() { X = 350, Y = 800}
            })
            {
                objects.Add(elem);
            }

            return objects;
        }

        private static List<Element> Randomgen(int width, int height)
        {
            // this size is determined by the largest element (the Hut)
            int chunkSize = 400;
            Random rand = new Random();

            if (width < chunkSize || height < chunkSize) throw new Exception("Must have at least " + chunkSize + " pixels to generate a board");

            // break the board up into 400x400 chunks, within those chunks we will then fill with a few particular patterns
            var objects = new List<Element>();

            for (int h = 0; h < height / chunkSize; h++)
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
                        objects.Add(item);
                    }
                    if (solids != null)
                    {
                        foreach (var elem in solids)
                            objects.Add(elem);
                    }
                }
            }

            // make borders
            foreach (var elem in WorldHelper.MakeBorders(width, height, 20))
                objects.Add(elem);

            return objects;
        }

        private static List<Element> HungerGames(int width, int height)
        {
            // put all the goodies in the middle - no health
            var rand = new Random();
            float x = width / 2;
            float y = height / 2;
            float dim = width / 10;
            int count = 100;
            var objects = new List<Element>();
            while (true)
            {
                float rx = x + (rand.Next() % dim) * (rand.Next() % 2 == 0 ? -1 : 1);
                float ry = y + (rand.Next() % dim) * (rand.Next() % 2 == 0 ? -1 : 1);

                var item = RandomgenItem(rx, ry, rand);

                if (item != null && !(item is Health) && !(item is Shield))
                {
                    objects.Add(item);

                    if (count-- <= 0) break;
                }
            }

            foreach (var elem in WorldHelper.MakeBorders(width, height, 20))
                objects.Add(elem);

            return objects;
        }

        private static Element RandomgenItem(float x, float y, Random rand)
        {
            switch (rand.Next() % 10)
            {
                case 0:
                    // helmet
                    return new Shield() { X = x, Y = y };
                case 1:
                    // bandage
                    return new Health() { X = x, Y = y };
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
