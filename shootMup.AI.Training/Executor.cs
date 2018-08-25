using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace shootMup.Bots.Training
{
    public class VoidSurface : IGraphics
    {
        public int Height { get { return 888; } }
        public int Width { get { return 1384; } }
        public void Clear(RGBA color) { }
        public void DisableTranslation(bool nonScaledTranslation = false) { }
        public void Ellipse(RGBA color, float x, float y, float width, float height, bool fill = true) { }
        public void EnableTranslation() { }
        public void Image(string path, float x, float y, float width = 0, float height = 0) { }
        public void Line(RGBA color, float x1, float y1, float x2, float y2, float thickness) { }
        public void Rectangle(RGBA color, float x, float y, float width, float height, bool fill = true) { }
        public void RotateTransform(float angle) { }
        public void SetTranslateCoordinates(TranslateCoordinatesDelegate callback) { }
        public void Text(RGBA color, float x, float y, string text, float fontsize = 16) { }
    }

    public class VoidSound : ISounds
    {
        public void Play(string path) { }
    }

    public static class Executor
    {
        public static int Run()
        {
            Console.WriteLine("Starting execution...");

            var world = new World(new VoidSurface(), new VoidSound());
            var done = false;

            world.OnEnd += () =>
            {
                done = true;
            };

            // start the game
            world.KeyPress(Constants.Esc);

            // wait until it is done
            int count = 0;
            while (!done)
            {
                if (count-- <= 0)
                {
                    Console.WriteLine("Alive - {0}", world.Alive);
                    count = 10;
                }
                System.Threading.Thread.Sleep(1000);
            }

            return 0;
        }
    }
}
