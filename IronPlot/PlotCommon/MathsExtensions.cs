using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPlot
{
    public enum EnumerationOrder2D { RowMajor, ColumnMajor };

    public static class MathExtensions
    {
        /// <summary>
        /// Apply function to each element in the array, updating array in place.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="function"></param>
        public static void UpdateEach<T>(this T[] array, Func<T, T> function)
        {
            for (int i = 0; i < array.Length; i++) { array[i] = function(array[i]); }
        }

        public static void UpdateEach<T>(this T[,] array, Func<T, T> function)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    array[i, j] = function(array[i, j]); 
                }
            }
        }

        //public static void UpdateEach<T>(this T[] array, this T[] array2, Func<T, T, T> function)
        //{
        //    for (int i = 0; i < array.Length; i++) { array[i] = function(array[i], array2[i]); }
        //}

        public static T[,] Iter2<T>(this T[,] array, T[,] array2, Func<T, T, T> function)
        {
            if ((array.GetLength(0) != array.GetLength(0)) || (array.GetLength(0) != array.GetLength(0))) throw new ArgumentException("Arrays' dimensions must be identical.");
            T[,] output = new T[array.GetLength(0), array.GetLength(1)];
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    output[i, j] = function(array[i, j], array2[i, j]);
                }
            }
            return output;
        }

        public static T[,] ToArray2D<T>(this IEnumerable<T> enumerable, int length0, int length1)
        {
            T[,] array = new T[length0, length1];
            IEnumerator<T> enumerator = enumerable.GetEnumerator();
            for (int j = 0; j < length0; ++j)
            {
                for (int i = 0; i < length1; ++i)
                {
                    enumerator.MoveNext();
                    array[i, j] = enumerator.Current;
                }
            }
            return array;
        }


        public static IEnumerable<T> RepeatingEnumerator<T>(this IEnumerable<T> input, int repeatTimes)
        {
            for (int i = 0; i < repeatTimes; ++i)
            {
                foreach (T item in input) yield return item; 
            }
        }

        /// <summary>
        /// Get enumerator for 2D arrays.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public static IEnumerable<T> ArrayEnumerator<T>(this T[,] input, EnumerationOrder2D order)
        {
            if (order == EnumerationOrder2D.RowMajor)
            {
                for (int i = 0; i < input.GetLength(0); ++i)
                {
                    for (int j = 0; j < input.GetLength(1); ++j)
                    {
                        yield return input[i, j];
                    }
                }
            }
            else
            {
                for (int j = 0; j < input.GetLength(1); ++j)
                {
                    for (int i = 0; i < input.GetLength(0); ++i)
                    {
                        yield return input[i, j];
                    }
                }
            }
        }
        
        public static double[] MultiplyBy(this double[] input, double multiplier)
        {
            for (int i = 0; i < input.Length; ++i) input[i] *= multiplier;
            return input;
        }

        public static double[] SumWith(this double[] input, double addition)
        {
            for (int i = 0; i < input.Length; ++i) input[i] += addition;
            return input;
        }
    }

}
