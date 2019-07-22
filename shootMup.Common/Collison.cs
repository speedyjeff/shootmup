using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace shootMup.Common
{
    // x,y coordinates have y inversed
    //                (-y, 0)
    //                   |
    // (-x,0) ----------------------- (+x, 0)
    //                   |
    //                (+y, 0)

    public static class Collision
    {
        public static bool IntersectingRectangles(
            float x1, float y1, float x2, float y2,
            float x3, float y3, float x4, float y4)
        {
            // validate input
            if (x1 > x2
                || y1 > y2
                || x3 > x4
                || y3 > y4) throw new Exception("Invalid input to compare rectangles");

            if (x3 > x1 && x3 < x2)
            {
                if (y3 > y1 && y3 < y2) return true;
                if (y3 < y1 && y4 > y2) return true;
                if (y4 > y1 && y4 < y2) return true;
            }
            else if (x4 > x1 && x4 < x2)
            {
                if (y4 > y1 && y4 < y2) return true;
                if (y3 < y1 && y4 > y2) return true;
                if (y3 > y1 && y3 < y2) return true;
            }
            else if ((y3 > y1 && y3 < y2) || (y4 > y1 && y4 < y2))
            {
                if (x3 < x1 && x4 > x2) return true;
            }
            else if (y3 < y1 && x3 < x1 && y4 > y2 && x4 > x2) return true;

            return false;
        }

        public static float DistanceToObject(
            float x1, float y1, float width1, float height1,
            float x2, float y2, float width2, float height2)
        {
            // this is an approximation, consider the shortest distance between any two points in these objects
            var e1 = new Tuple<float, float>[]
            {
                new Tuple<float,float>(x1, y1),
                new Tuple<float,float>(x1 - (width1 / 2), y1 - (height1 / 2)),
                new Tuple<float,float>(x1 + (width1 / 2), y1 + (height1 / 2))
            };

            var e2 = new Tuple<float, float>[]
            {
                new Tuple<float,float>(x2, y2),
                new Tuple<float,float>(x2 - (width2 / 2), y2 - (height2 / 2)),
                new Tuple<float,float>(x2 + (width2 / 2), y2 + (height2 / 2))
            };

            var minDistance = float.MaxValue;
            for (int i = 0; i < e1.Length; i++)
                for (int j = i + 1; j < e2.Length; j++)
                    minDistance = Math.Min(Collision.DistanceBetweenPoints(e1[i].Item1, e1[i].Item2, e2[j].Item1, e2[j].Item2), minDistance);
            return minDistance;
        }

        public static float CalculateAngleFromPoint(float x1, float y1, float x2, float y2)
        {
            // normalize x,y to 0,0
            float y = -1 * (y2 - y1); // inverting Y to make Atan correct
            float x = (x2 - x1);
            float angle = (float)(Math.Atan2(x, y) * (180 / Math.PI));

            if (angle < 0) angle += 360;
            if (angle < 0) throw new Exception("Invalid negative angle : " + angle);
            if (angle > 360) throw new Exception("Invalid angle larger than 360 : " + angle);

            return angle;
        }

        public static bool CalculateLineByAngle(float x, float y, float angle, float distance, out float x1, out float y1, out float x2, out float y2)
        {
            if (angle < 0) angle *= -1;
            angle = angle % 360;

            x1 = x;
            y1 = y;
            float a = (float)Math.Cos(angle * Math.PI / 180) * distance;
            float o = (float)Math.Sin(angle * Math.PI / 180) * distance;
            x2 = x1 + o;
            y2 = y1 - a;

            return true;
        }

        public static float DistanceBetweenPoints(float x1, float y1, float x2, float y2)
        {
            // a^2 + b^2 = c^2
            //  a = |x1 - x2|
            //  b = |y1 - y2|
            //  c = result
            return (float)Math.Sqrt(
                Math.Pow(Math.Abs(x1 - x2), 2) + Math.Pow(Math.Abs(y1 - y2), 2)
                );
        }

        public static bool LineIntersectingRectangle(
            float x1, float y1, float x2, float y2, // line
            float x, float y, float width, float height // rectangle
            )
        {
            bool collision = false;

            // check if these would collide
            float x3 = x - (width / 2);
            float y3 = y - (height / 2);
            float x4 = x + (width / 2);
            float y4 = y + (height / 2);

            // https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect

            // top
            collision = Collision.IntersectingLine(x1, y1, x2, y2,
                x3, y3, x4, y3);
            // bottom
            collision |= Collision.IntersectingLine(x1, y1, x2, y2,
                x3, y4, x4, y4);
            // left
            collision |= Collision.IntersectingLine(x1, y1, x2, y2,
                x3, y3, x3, y4);
            // left
            collision |= Collision.IntersectingLine(x1, y1, x2, y2,
                x4, y3, x4, y4);

            return collision;
        }

        public static bool IntersectingLine(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            // https://stackoverflow.com/questions/3838329/how-can-i-check-if-two-segments-intersect
            if (CalcCcw(x1, y1, x3, y3, x4, y4) != CalcCcw(x2, y2, x3, y3, x4, y4)
                && CalcCcw(x1, y1, x2, y2, x3, y3) != CalcCcw(x1, y1, x2, y2, x4, y4))
                return true;
            return false;
        }

        #region private
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CalcCcw(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            return (y3 - y1) * (x2 - x1) > (y2 - y1) * (x3 - x1);
        }
        #endregion
    }
}
