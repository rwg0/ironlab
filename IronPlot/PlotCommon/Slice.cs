using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPlot.PlotCommon
{
    public class SliceTest
    {
        public static void Test()
        {
            var array = new double[10,10];
            var slice = new Slice2D<double>(array, new DimensionSlice(2, 2, 6), new DimensionSlice(0,1,4));
            slice.Set(u => u + 4);
        }
    }
    
    public class Slice2D<T> : IEnumerable<T>
    {
        DimensionSlice x;
        DimensionSlice y;
        T[,] array;
        int xLength;
        int yLength;

        public Slice2D(T[,] array, DimensionSlice x, DimensionSlice y)
        {
            CommonConstructor(array, x, y);
        }

        public Slice2D(T[,] array)
        {
            CommonConstructor(array, DimensionSlice.CreateBasic(array.GetLength(0)), DimensionSlice.CreateBasic(array.GetLength(0)));
        }

        public void CommonConstructor(T[,] array, DimensionSlice x, DimensionSlice y)
        {
            this.x = x; this.y = y;
            this.array = array;
            if (this.x.Stop == null) this.x.Stop = array.GetLength(0);
            if (this.y.Stop == null) this.y.Stop = array.GetLength(1);
            xLength = ((int)x.Stop - x.Start) / x.Step;
            yLength = ((int)y.Stop - y.Start) / y.Step;
        }

        public T[,] Array { get { return array; } }

        /// <summary>
        /// Indexing: likely to be slower than using Update method.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return array[index % xLength, index / xLength]; }
            set { array[index % xLength, index / xLength] = value; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (int xIndex in x)
            {
                foreach (int yIndex in y)
                {
                    yield return array[xIndex, yIndex];
                }
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            foreach (int xIndex in x)
            {
                foreach (int yIndex in y)
                {
                    yield return array[xIndex, yIndex];
                }
            }
        }

        public void Set(Func<T, T> function)
        {
            int xIndex = x.Start;
            int yIndex = y.Start;
            while (true)
            {
                array[xIndex, yIndex] = function(array[xIndex, yIndex]);
                xIndex += x.Step;
                if (xIndex > x.Stop)
                {
                    xIndex = x.Start;
                    yIndex += y.Step;
                    if (yIndex > y.Stop) break;
                }
            }
        }

        public void Set(IEnumerable<T> input1, Func<T, T, T> function)
        {
            int xIndex = x.Start;
            int yIndex = y.Start;
            foreach (T item in input1)
            {
                array[xIndex, yIndex] = function(array[xIndex, yIndex], item);
                xIndex += x.Step;
                if (xIndex > x.Stop) 
                {
                    xIndex = x.Start;
                    yIndex += y.Step;
                    if (yIndex > y.Stop) break;
                }
            }
        }

        public void Set(IEnumerable<T> input1, IEnumerable<T> input2, Func<T, T, T, T> function)
        {
            int xIndex = x.Start;
            int yIndex = y.Start;
            var item1 = input1.GetEnumerator();
            var item2 = input2.GetEnumerator();
            while (item1.MoveNext() && item2.MoveNext()) 
            {
                array[xIndex, yIndex] = function(array[xIndex, yIndex], item1.Current, item2.Current);
                xIndex += x.Step;
                if (xIndex > x.Stop)
                {
                    xIndex = x.Start;
                    yIndex += y.Step;
                    if (yIndex > y.Stop) break;
                }
            }
        }
    }

    public class DimensionSlice : IEnumerable<int>
    {
        public int Start;
        public int Step;
        public int? Stop;

        public DimensionSlice(int start, int step, int? stop)
        {
            this.Start = start;
            this.Step = step;
            this.Stop = stop;
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = Start; i < Stop; i += Step) yield return i;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = Start; i < Stop; i += Step) yield return i;
        }

        internal int this[int index]
        {
            get
            {
                return Start + index * Step;
            }
        }

        public static DimensionSlice CreateBasic(int length)
        {
            return new DimensionSlice(0, 1, length - 1);
        }
    }
}
