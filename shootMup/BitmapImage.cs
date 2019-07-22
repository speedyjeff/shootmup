using shootMup.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shootMup
{
    public class BitmapImage : IImage
    {
        public BitmapImage(int width, int height)
        {
            Width = width;
            Height = height;

            // create a bitmap and return the WritableGraphics handle
            UnderlyingImage = new Bitmap(width, height);
            var g = System.Drawing.Graphics.FromImage(UnderlyingImage);
            Graphics = new WritableGraphics(null, g, height, width);
        }

        public IGraphics Graphics { get; private set; }

        public int Height { get; private set; }

        public int Width { get; private set; }

        public void MakeTransparent(RGBA color)
        {
            UnderlyingImage.MakeTransparent(Color.FromArgb(color.A, color.R, color.G, color.B));

            // recreate the graphics after making transparent
            // marking an image transparent has a material impact to the bitmap such that we need 
            // a new graphics instance
            var g = System.Drawing.Graphics.FromImage(UnderlyingImage);
            Graphics = new WritableGraphics(null, g, Height, Width);
        }

        public void Save(string path)
        {
            UnderlyingImage.Save(path);
        }

        #region internal
        internal Bitmap UnderlyingImage;
        #endregion
    }
}
