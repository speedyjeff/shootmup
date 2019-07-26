using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace shootMup.Common
{
    public struct Region
    {
        public Region(int row, int col)
        {
            Row = row;
            Col = col;
        }

        #region private
        internal int Row;
        internal int Col;
        #endregion
    }

    // this collection will provide fast access to a subset of the elements
    // the subset is defined by a region

    public class RegionCollection
    {
        public RegionCollection(IEnumerable<Element> elements, int width, int height)
        {
            // gather all the element sizes
            var sizes = new List<float>();
            foreach (var o in elements) sizes.Add(o.Width > o.Height ? o.Width : o.Height);

            // get the regionSize
            if (sizes.Count == 0)
            {
                // setup only 1 region
                RegionSize = width > height ? width : height;
            }
            else
            {
                // get the 80th percentile
                sizes.Sort();
                RegionSize = (int)sizes[(int)(sizes.Count * 0.8)];
            }

            // init
            RegionLock = new ReaderWriterLockSlim();
            Columns = (width / RegionSize) + 1;
            Rows = (height / RegionSize) + 1;
            Width = width;
            Height = height;
            Oversized = new Dictionary<int, Element>();
            Regions = new Dictionary<int, Element>[Rows][];
            for (int r = 0; r < Rows; r++)
            {
                Regions[r] = new Dictionary<int, Element>[Columns];
                for (int c = 0; c < Columns; c++)
                {
                    Regions[r][c] = new Dictionary<int, Element>();
                }
            }

            // iterate through to get lower bound of x and y (as means of rebasing the regions)
            var minx = Single.MaxValue;
            var miny = Single.MaxValue;
            foreach (var elem in elements)
            {
                if (elem.X < minx) minx = elem.X;
                if (elem.Y < miny) miny = elem.Y;
            }
            // convert these into offsets to adjust the regions to a 0,0 based matrix
            if (minx < 0) XOffset = (int)Math.Floor(minx);
            else XOffset = (int)Math.Ceiling(minx);
            XOffset *= -1;
            if (miny < 0) YOffset = (int)Math.Floor(miny);
            else YOffset = (int)Math.Ceiling(miny);
            YOffset *= -1;

            // add all the elements
            foreach(var elem in elements) Add(elem.Id, elem);
        }

        public void Add(int key, Element elem)
        {
            if (elem == null) throw new Exception("Invalid element to add");

            RegionLock.EnterWriteLock();
            try
            {
                // get region to insert into
                GetRegion(elem.X, elem.Y, out int row, out int column);

                // check if this is an item that would span multiple regions
                if (IsOversized(elem) || IsOutofRange(row, column))
                {
                    Oversized.Add(key, elem);
                    return;
                }

                // add to the region specified (or span multiple regions)
                Regions[row][column].Add(key, elem);
            }
            finally
            {
                RegionLock.ExitWriteLock();
            }
        }

        public bool Remove(int key, Element elem)
        {
            if (elem == null) throw new Exception("Invalid element to remove");

            RegionLock.EnterWriteLock();
            try
            {
                // get region to remove from
                GetRegion(elem.X, elem.Y, out int row, out int column);

                // check if this is an item that would span multiple regions
                if (IsOversized(elem) || IsOutofRange(row, column))
                {
                    return Oversized.Remove(key);
                }

                // add to the region specified (or span multiple regions)
                return Regions[row][column].Remove(key);
            }
            finally
            {
                RegionLock.ExitWriteLock();
            }
        }

        public void Move(int key, Region src, Region dst)
        {
            RegionLock.EnterWriteLock();
            try
            {
                // get element (either in oversized or a Region)
                Element elem;
                if (Oversized.TryGetValue(key, out elem))
                {
                    // if elment is Oversized or the dst is out of range, keep it here
                    if (IsOversized(elem)) return;

                    // assert that this element is currently out of range
                    if (!IsOutofRange(src)) throw new Exception("Invalid state in internal datastructures");

                    // if it remains out of range, keep it here
                    if (IsOutofRange(dst)) return;

                    // remove it from the oversized, as it is moving to a region
                    if (!Oversized.Remove(key)) throw new Exception("Failed to remove this element");
                }
                else
                {
                    // find it and remove it
                    if (!Regions[src.Row][src.Col].TryGetValue(key, out elem)) throw new Exception("Failed to location the element to move");
                    if (!Regions[src.Row][src.Col].Remove(key)) throw new Exception("Failed to remove this element");
                }

                // add element (either oversized or a Region)
                if (IsOutofRange(dst)) Oversized.Add(key, elem);
                else Regions[dst.Row][dst.Col].Add(key, elem);
            }
            finally
            {
                RegionLock.ExitWriteLock();
            }
        }

        public IEnumerable<Element> Values(float x1, float y1, float x2, float y2)
        {
            RegionLock.EnterReadLock();
            try
            {
                // get the starting row,col and ending row,col
                GetRegion(x1, y1, out int r1, out int c1, validate: false);
                GetRegion(x2, y2, out int r2, out int c2, validate: false);

                // assuming we start to 0,0 in the upper left and corner and increase right and down
                // ensure r1,c1 if the upper left and corner
                if (r2 < r1)
                {
                    // swap
                    var tmp = r1;
                    r1 = r2;
                    r2 = tmp;
                }
                if (c2 < c1)
                {
                    var tmp = c1;
                    c1 = c2;
                    c2 = tmp;
                }

                // expand the region
                r1-=1; c1-=1;
                r2+=1; c2+=1;

                // hold the lock for the duration of this call
                for (int r = (r1 >= 0 ? r1 : 0); r <= r2 && r < Regions.Length; r++)
                {
                    for (int c = (c1 >= 0 ? c1 : 0); c <= c2 && c < Regions[r].Length; c++)
                    {
                        if (Regions[r][c].Count == 0) continue;
                        foreach (var elem in Regions[r][c].Values) yield return elem;
                    }
                }

                // always return the oversized items
                foreach (var elem in Oversized.Values) yield return elem;
            }
            finally
            {
                RegionLock.ExitReadLock();
            }
        }

        public Region GetRegion(Element elem)
        {
            if (elem == null) throw new Exception("Invalid element to get region");

            RegionLock.EnterReadLock();
            try
            {
                if (IsOversized(elem))
                {
                    return new Region(-1, -1);
                }

                GetRegion(elem.X, elem.Y, out int row, out int column);
                return new Region(row, column);
            }
            finally
            {
                RegionLock.ExitReadLock();
            }
        }

        #region private
        private ReaderWriterLockSlim RegionLock;
        private Dictionary<int, Element>[][] Regions;
        private Dictionary<int, Element> Oversized;
        private int RegionSize;
        private int Width;
        private int Height;
        private int Columns;
        private int Rows;
        private int XOffset;
        private int YOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOversized(Element elem)
        {
            return elem.Width > RegionSize || elem.Height > RegionSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetRegion(float x, float y, out int row, out int column, bool validate = true)
        {
            row = (int)Math.Floor((y + YOffset) / RegionSize);
            column = (int)Math.Floor((x + XOffset) / RegionSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsOutofRange(float row, float col)
        {
            return row < 0 || row >= Rows || col < 0 || col >= Columns;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsOutofRange(Region region)
        {
            return region.Row < 0 || region.Row >= Rows || region.Col < 0 || region.Col >= Columns;
        }
        #endregion
    }
}
