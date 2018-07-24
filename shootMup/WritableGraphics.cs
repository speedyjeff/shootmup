using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using shootMup.Common;

namespace shootMup
{
    public class WritableGraphics : IGraphics
    {
        public WritableGraphics(BufferedGraphicsContext context)
        {
            Context = context;
            DoTranslation = true;
            ImageCache = new Dictionary<string, Image>();
        }

        // access to the Graphics implementation
        public Graphics RawGraphics => Surface.Graphics;
        public void RawRender(Graphics g) { Surface.Render(g); }
        public void RawResize(Graphics g, int height, int width)
        {
                if (Context == null) throw new Exception("Must initialize the DoublebufferContext before calling");

            // initialize the double buffer
            Width = width;
            Height = height;
            Context.MaximumBuffer = new Size(width + 1, height + 1);
            if (Surface != null)
            {
                Surface.Dispose();
            }
            Surface = Context.Allocate(g,
                new Rectangle(0, 0, width, height));
        }

        // high level access to drawing
        public void Clear(RGBA color)
        {
            Surface.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B)), 0, 0, Width, Height);
        }

        public void Ellipse(RGBA color, float x, float y, float width, float height, bool fill)
        {
            float thickness = 5f;
            float sx = x;
            float sy = y;
            float swidth = width;
            float sheight = height;
            float sthickness = thickness;
            if (Translate != null && DoTranslation && !Translate(x, y, width, height, thickness, out sx, out sy, out swidth, out sheight, out sthickness)) return;

            // safe guard accidental usage
            x = y = width = height = thickness = 0;

            // use screen coordinates
            if (fill)
            {
                Surface.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B)), sx, sy, swidth, sheight);
                Surface.Graphics.DrawEllipse(new Pen(Color.Black, sthickness), sx, sy, swidth, sheight);
            }
            else
            {
                Surface.Graphics.DrawEllipse(new Pen(Color.FromArgb(color.A, color.R, color.G, color.B), sthickness), sx, sy, swidth, sheight);
            }
        }

        public void Rectangle(RGBA color, float x, float y, float width, float height, bool fill)
        {
            float thickness = 5f;
            float sx = x;
            float sy = y;
            float swidth = width;
            float sheight = height;
            float sthickness = thickness;
            if (Translate != null && DoTranslation && !Translate(x, y, width, height, thickness, out sx, out sy, out swidth, out sheight, out sthickness)) return;

            // safe guard accidental usage
            x = y = width = height = thickness = 0;

            // use screen coordinates
            if (fill)
            {
                Surface.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B)), sx, sy, swidth, sheight);
                Surface.Graphics.DrawRectangle(new Pen(Color.Black, sthickness), sx, sy, swidth, sheight);
            }
            else
            {
                Surface.Graphics.DrawRectangle(new Pen(Color.FromArgb(color.A, color.R, color.G, color.B), sthickness), sx, sy, swidth, sheight);
            }
        }

        public void Text(RGBA color, float x, float y, string text)
        {
            float fontsize = 16;
            float sx = x;
            float sy = y;
            float swidth = 0;
            float sheight = 0;
            float sfontsize = fontsize;
            if (Translate != null && DoTranslation && !Translate(x, y, 0, 0, fontsize, out sx, out sy, out swidth, out sheight, out sfontsize)) return;

            // safe guard accidental usage
            x = y = fontsize = 0;

            // use screen coordinates
            Surface.Graphics.DrawString(text, new Font("Arial", sfontsize), new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B)), sx, sy);
        }

        public void Line(RGBA color, float x1, float y1, float x2, float y2, float thickness)
        {
            float sx1 = x1;
            float sy1 = y1;
            float width = Math.Abs(x1 - x2);
            float height = Math.Abs(y1 - y2);
            float swidth = width;
            float sheight = height;
            float sthickness = thickness;
            if (Translate != null && DoTranslation && !Translate(x1, y1, width, height, thickness, out sx1, out sy1, out swidth, out sheight, out sthickness)) return;

            // safe guard accidental usage
            x1 = y1 = thickness = 0;

            float sx2 = x2;
            float sy2 = y2;
            float sother = 0;
            if (Translate != null && DoTranslation && !Translate(x2, y2, width, height, 0, out sx2, out sy2, out swidth, out sheight, out sother)) return;

            // safe guard accidental usage
            x2 = y2 = 0;

            Surface.Graphics.DrawLine(new Pen(Color.FromArgb(color.A, color.R, color.G, color.B), sthickness), sx1, sy1, sx2, sy2);
        }

        public void Image(string path, float x, float y)
        {
            System.Drawing.Image img = null;
            if (!ImageCache.TryGetValue(path, out img))
            {
                img = System.Drawing.Image.FromFile(path);
                var bitmap = new Bitmap(img);
                bitmap.MakeTransparent(bitmap.GetPixel(0,0));
                ImageCache.Add(path, bitmap);
            }

            float sx = x;
            float sy = y;
            float swidth = 0;
            float sheight = 0;
            float sother = 0;
            if (Translate != null && DoTranslation && !Translate(x, y, img.Width, img.Height, 0, out sx, out sy, out swidth, out sheight, out sother)) return;

            // safe guard accidental usage
            x = y = 0;

            // use screen coordinates
            Surface.Graphics.DrawImage(img, sx, sy, swidth, sheight);
        }

        public void RotateTransform(float angle)
        {
            Surface.Graphics.TranslateTransform(1 * Width / 2, 1 * Height / 2);
            Surface.Graphics.RotateTransform(angle);
            Surface.Graphics.TranslateTransform(-1 * Width / 2, -1 * Height / 2);
        }

        public void EnableTranslation()
        {
            DoTranslation = true;
        }

        public void DisableTranslation()
        {
            DoTranslation = false;
        }

        public int Height { get; private set; }
        public int Width { get; private set; }

        public void SetTranslateCoordinates(TranslateCoordinatesDelegate callback)
        {
            Translate = callback;
        }

        #region private
        private BufferedGraphics Surface;
        private BufferedGraphicsContext Context;
        private TranslateCoordinatesDelegate Translate;
        private Dictionary<string, Image> ImageCache;
        private bool DoTranslation;
        // TODO! color cache
        #endregion
    }
}
