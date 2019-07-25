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
            // Assume that the map starts at 0,0 and grows down and right

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

            // add all the elements
            foreach (var elem in elements) Add(elem.Id, elem);
        }

        public void Add(int key, Element elem)
        {
            if (elem == null) throw new Exception("Invalid element to add");

            RegionLock.EnterWriteLock();
            try
            {
                // check if this is an item that would span multiple regions
                if (IsOversized(elem))
                {
                    Oversized.Add(key, elem);
                    return;
                }

                // get region to insert into
                GetRegion(elem.X, elem.Y, out int row, out int column);

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
                // check if this is an item that would span multiple regions
                if (IsOversized(elem))
                {
                    return Oversized.Remove(key);
                }

                // get region to remove from
                GetRegion(elem.X, elem.Y, out int row, out int column);

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
                // validate
                if (src.Row < 0 || src.Row >= Rows || src.Col < 0 || src.Col >= Columns) throw new Exception("Invalid src region location");
                if (dst.Row < 0 || dst.Row >= Rows || dst.Col < 0 || dst.Col >= Columns) throw new Exception("Invalid dst region location");

                // get element
                Element elem;
                if (Oversized.TryGetValue(key, out elem))
                {
                    // this item is oversized and no work is necessary
                    return;
                }
                if (!Regions[src.Row][src.Col].TryGetValue(key, out elem)) throw new Exception("Failed to location the element to move");

                // remove
                if (!Regions[src.Row][src.Col].Remove(key)) throw new Exception("Failed to remove this element");

                // add
                Regions[dst.Row][dst.Col].Add(key, elem);
            }
            finally
            {
                RegionLock.ExitWriteLock();
            }
        }

        public IEnumerable<Element> AllValues()
        {
            // return all the values
            return Values(0, 0, Width, Height);
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
                r1 -= 1; c1 -= 1;
                r2 += 1; c2 += 1;

                // hold the lock for the duration of this call
                for (int r = (r1 >= 0 ? r1 : 0); r <= r2 && r < Regions.Length; r++)
                    for (int c = (c1 >= 0 ? c1 : 0); c <= c2 && c < Regions[r].Length; c++)
                    {
                        if (Regions[r][c].Count == 0) continue;
                        foreach (var elem in Regions[r][c].Values) yield return elem;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOversized(Element elem)
        {
            return elem.Width > RegionSize || elem.Height > RegionSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetRegion(float x, float y, out int row, out int column, bool validate = true)
        {
            row = (int)Math.Floor(y / RegionSize);
            column = (int)Math.Floor(x / RegionSize);
            if (validate && (row < 0 || row >= Rows || column < 0 || column >= Columns)) throw new Exception("Region out of range");
        }
        #endregion
    }
}
