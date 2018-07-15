﻿using System;
using System.Collections.Generic;
using System.Text;

namespace shootMup.Common
{
    public struct RGBA
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public static RGBA Black = new RGBA() { R = 0, G = 0, B = 0, A = 255 };
        public static RGBA White = new RGBA() { R = 255, G = 255, B = 255, A = 255 };
    }

    public delegate bool TranslateCoordinatesDelegate(float x, float y, float width, float height, out float tx, out float ty, out float twidth, out float theight);

    public interface IGraphics
    {
        // drawing
        void Clear(RGBA color);
        void Ellipse(RGBA color, float x, float y, float width, float height, bool fill = true);
        void Rectangle(RGBA color, float x, float y, float width, float height, bool fill = true);
        void Text(RGBA color, float x, float y, string text);
        void Line(RGBA color, float x1, float y1, float x2, float y2);

        void RotateTransform(float angle);

        // details
        int Height { get; }
        int Width { get; }

        // translate the coordinates to screen
        // take into acount windowing and scalling
        void SetTranslateCoordinates(TranslateCoordinatesDelegate callback);
    }
}
