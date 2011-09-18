using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPlot
{
    public class MathHelper
    {
        /// <summary>
        /// Iterates over X MeshGrid for row-major 2D array. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="nRows"></param>
        /// <returns></returns>
        public static IEnumerable<double> MeshGridX(IEnumerable<double> x, int yLength)
        {
            // Row major:
            for (int j = 0; j < yLength; ++j)
            {
                foreach (double value in x)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Iterates over Y MeshGrid for row-major 2D array. 
        /// </summary>
        /// <param name="y"></param>
        /// <param name="nColumns"></param>
        /// <returns></returns>
        public static IEnumerable<double> MeshGridY(IEnumerable<double> y, int xLength)
        {
            // Row major:
            foreach (double value in y)
            {
                for (int i = 0; i < xLength; ++i)
                {
                    yield return value;
                }
            }
        }

        public static double[] Counter(int n)
        {
            double[] output = new double[n];
            for (int i = 0; i < n; ++i)
            {
                output[i] = i + 1;
            }
            return output;
        }

        public static double[,] Counter(int width, int height)
        {
            double[,] output = new double[width, height];
            double index = 1;
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; ++j)
                {
                    output[i, j] = index;
                    index += 1;
                }
            }
            return output;
        }

        public void Example()
        {
            var evens = Enumerable
                .Range(1, 100)
                .Where(x => (x % 2) == 0)
                .ToList();
        }

        public class Slice2D
        {
            int? Start0;
            int? Stop0;
            int? Step0;
            int? Start1;
            int? Stop1;
            int? Step1;

            public Slice2D(int? start0, int? stop0, int? step0, int? start1, int? stop1, int? step1)
            {
                this.Start0 = start0; this.Stop0 = stop0; this.Step0 = step0;
                this.Start1 = start1; this.Stop1 = stop1; this.Step1 = step1;
            }
        }
    }
}
