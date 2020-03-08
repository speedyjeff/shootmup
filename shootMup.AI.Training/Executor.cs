using engine.Common;
using engine.Common.Entities;
using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public IImage CreateImage(int width, int height) { return null;  }
        public IImage CreateImage(string path) { return null; }
        public IImage CreateImage(Stream stream) { return null; }
        public void DisableTranslation(TranslationOptions options) { }
        public void Ellipse(RGBA color, float x, float y, float width, float height, bool fill, bool border, float thickness) { }
        public void EnableTranslation() { }
        public void Image(IImage img, float x, float y, float width = 0, float height = 0) { }
        public void Line(RGBA color, float x1, float y1, float x2, float y2, float thickness) { }
        public void Rectangle(RGBA color, float x, float y, float width, float height, bool fill, bool border, float thickness) { }
        public void RotateTransform(float angle) { }
        public void Text(RGBA color, float x, float y, string text, float fontsize = 16) { }
        public void Triangle(RGBA color, float x1, float y1, float x2, float y2, float x3, float y3, bool fill, bool border, float thickness) { }
        public void Polygon(RGBA color, Point[] points, bool fill, bool border, float thickness) { }
        public void SetPerspective(bool is3D, float centerX, float centerY, float centerZ, float yaw, float pitch, float roll, float cameraX, float cameraY, float cameraZ, float horizon = 0f) { }
        public void Image(IImage img, Point[] points) { }


        public void CapturePolygons() { }

        public void RenderPolygons() { }
    }

    public class VoidSound : ISounds
    {
        public void Play(string path) { }
        public void Play(string name, Stream stream) { }
        public void PlayMusic(string path, bool repeat) { }
        public void Repeat() { }
    }

    public static class Executor
    {
        public static int Run(string type)
        {
            Console.WriteLine("Starting execution for {0}...", type);

            // generate the world
            var human = new ShootMPlayer() { Name = "human" };
            var players = new Player[100];
            for(int i=0; i<players.Length; i++)
            {
                switch (type.ToLower())
                {
                    case "ml":
                        players[i] = new TrainedAI(TrainedAIModel.ML_Net) { Name = string.Format("ai{0}", i) };
                        break;
                    case "cv":
                        players[i] = new TrainedAI(TrainedAIModel.OpenCV) { Name = string.Format("ai{0}", i) };
                        break;
                    default:
                        throw new Exception("Unknown model type " + type);
                }
            }
            var world = WorldGenerator.Generate(WorldType.Random, PlayerPlacement.Borders, human, ref players);
            
            // initialize with a void UI/Sound to display too
            world.InitializeGraphics(new VoidSurface(), new VoidSound());

            // start the game
            world.KeyPress(Constants.Esc);

            // ensure there are no blocking menus
            world.OnPaused += () => { return null; };

            // wait until it is done or 1 minute has passed
            var timer = new Stopwatch();
            timer.Start();
            while (world.Alive > 1 && timer.ElapsedMilliseconds < (60 * 1024))
            {
                Console.WriteLine("Alive - {0}", world.Alive);
                System.Threading.Thread.Sleep(1000);
            }
            timer.Stop();

            // pause the game
            world.KeyPress(Constants.Esc);

            Console.WriteLine("Finished with {0} alive and {1} ms time executed", world.Alive, timer.ElapsedMilliseconds);

            return 0;
        }
    }
}
