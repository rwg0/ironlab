using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace IronPlot
{
    /// <summary>
    /// Class to deal with common non-System.Array arrays, e.g. NumpPy arrays.
    /// </summary>
    public class GeneralArray
    {
        public unsafe static int[] GetDimensions(object generalArray)
        {
            int[] dimensions = null;
            Type arrayType = generalArray.GetType();
            if (generalArray is Array) 
            {
                Array array = generalArray as Array;
                dimensions = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i) dimensions[i] = array.GetLength(i);
            }
            else if (arrayType.Name == "ndarray")
            {
                dynamic dynamicArray = generalArray;
                dimensions = new int[dynamicArray.Dims.Length];
                for (int i = 0; i < dimensions.Length; ++i) dimensions[i] = (int)dynamicArray.Dims[i];
            }
            // for jagged lists or enumerables
            else if (generalArray is IEnumerable<object>)
            {
                dimensions = DimensionsOfJaggedIEnumerable(generalArray as IEnumerable<object>);
            }
            return dimensions;
        }

        private static int[] DimensionsOfJaggedIEnumerable(IEnumerable<object> enumerable)
        {
            int rank = 0;
            IEnumerable<object> parent = enumerable;
            List<int> dimensionList = new List<int>();
            while (parent != null)
            {
                rank++;
                int count = parent.FastCount();
                dimensionList.Add(count);
                parent = count == 0 ? null : parent.First() as IEnumerable<object>;
            }
            return dimensionList.ToArray();
        }
        
        /// <summary>
        /// Converts object to a
        /// </summary>
        /// <param name="generalArray"></param>
        /// <returns></returns>
        public unsafe static Array ToDoubleArray(object generalArray)
        {
            Type arrayType = generalArray.GetType();
            Array managedArray = null;

            #region ILArray
            if (arrayType.Name == "ILArray`1")
            {
                dynamic dynamicArray = generalArray;

                if ((dynamicArray.Dimensions.NumberOfDimensions == 1) ||
                    ((dynamicArray.Dimensions.NumberOfDimensions == 2) && (dynamicArray.Dimensions[1] == 1)))
                {
                    int n = dynamicArray.Dimensions[0];
                    managedArray = new double[n];
                    double[] managedArrayCast = managedArray as double[];
                    for (int i = 0; i < n; ++i) managedArrayCast[i] = dynamicArray.GetValue(i);
                    return managedArray;
                }
                else if (dynamicArray.Dimensions.NumberOfDimensions == 2)
                {
                    int n1 = dynamicArray.Dimensions[0];
                    int n2 = dynamicArray.Dimensions[1];
                    managedArray = new double[n1, n2];
                    double[,] managedArrayCast = managedArray as double[,];
                    for (int i = 0; i < n1; ++i)
                    {
                        for (int j = 0; j < n2; ++j)
                        {
                            managedArrayCast[i, j] = dynamicArray.GetValue(i, j);
                        }
                    }
                    return managedArray;
                }
                throw new Exception("Array must be one or two dimensionsal.");
            }
            #endregion
            else if (arrayType.Name == "ndarray")
            {
                managedArray = ManagedArrayFromNumpyArray(generalArray);
            }
            else
            {
                // not Array, ILArray or Numpy array
                int[] dimensions = GeneralArray.GetDimensions(generalArray);
                if (dimensions.Length > 2) throw new Exception("More than two dimensions found.");
                if (dimensions.Length == 1)
                {
                    var enumerable = generalArray as IEnumerable<object>;
                    double[] tempArray = new double[enumerable.Count()];
                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        tempArray[index] = Convert.ToDouble(item);
                        index++;
                    }
                    managedArray = tempArray;
                }
                else
                {
                    var parent = generalArray as IEnumerable<object>;
                    int minLength = int.MaxValue; int maxLength = int.MinValue;
                    int count = parent.FastCount();
                    foreach (IEnumerable<object> child in parent)
                    {
                        if (child == null) minLength = 0;
                        else
                        {
                            minLength = Math.Min(minLength, child.FastCount());
                            maxLength = Math.Max(maxLength, child.FastCount());
                        }
                    }
                    int i = 0;
                    double[,] tempArray = new double[count, maxLength];
                    foreach (IEnumerable<object> child in parent)
                    {
                        int j = 0;
                        foreach (object element in child)
                        {
                            tempArray[i, j] = Convert.ToDouble(element);
                            j++;
                        }
                        while (j < maxLength)
                        {
                            tempArray[i, j] = Double.NaN; ++j;
                        }
                        i++;
                    }
                    managedArray = tempArray;
                }
            }
            return managedArray;
        }

        public unsafe static Array ManagedArrayFromNumpyArray(object numpyArray)
        {
            Array managedArray;
            dynamic dynamicArray = numpyArray;
            dynamic newArray = null;
            int[] dimensions = new int[dynamicArray.Dims.Length];
            int length = 1;
            for (int i = 0; i < dimensions.Length; ++i)
            {
                dimensions[i] = (int)dynamicArray.Dims[i];
                length *= dimensions[i];
            }

            // Treat double precision data as special case and keep everything fast.
            if (dynamicArray.dtype.name == "float64")
            {
                try
                {
                    // If necessary get a contiguous, C-type double precision array:
                    if (!dynamicArray.flags.contiguous)
                    {
                        newArray = dynamicArray.copy("C");
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Failed to change input array into contiguous array.");
                }
                IntPtr start;
                if (newArray == null) start = dynamicArray.UnsafeAddress;
                else start = newArray.UnsafeAddress;

                if (dimensions.Length == 1)
                {
                    managedArray = new double[dimensions[0]];
                    fixed (double* newArrayPointer = (double[])managedArray)
                    {
                        double* currentPosition = newArrayPointer;
                        double* numpyPointer = (double*)start;
                        double* endPointer = newArrayPointer + length;
                        while (currentPosition != endPointer)
                        {
                            *currentPosition = *numpyPointer;
                            currentPosition++;
                            numpyPointer++;
                        }
                    }
                }
                else if (dimensions.Length == 2)
                {
                    managedArray = new double[dimensions[0], dimensions[1]];
                    fixed (double* newArrayPointer = (double[,])managedArray)
                    {
                        double* currentPosition = newArrayPointer;
                        double* numpyPointer = (double*)start;
                        double* endPointer = newArrayPointer + length;
                        while (currentPosition != endPointer)
                        {
                            *currentPosition = *numpyPointer;
                            currentPosition++;
                            numpyPointer++;
                        }
                    }
                }
                else
                {
                    throw new Exception("Array must be one or two dimensionsal.");
                }
                if (newArray != null) newArray.Dispose();
            }
            else
            {
                managedArray = Array.CreateInstance(typeof(double), dimensions);
                IndexEnumerator enumerator = new IndexEnumerator(dimensions);
                while (enumerator.MoveNext())
                {
                    managedArray.SetValue(dynamicArray.item(enumerator.CurrentObjectIndices), enumerator.CurrentIndices);
                }
            }
            return managedArray;
        }

        /// <summary>
        /// For fast enumerations, this must be a IList of ILists, not a IList of IEnumerables
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsIListOfILists(IEnumerable<object> enumerable)
        {
            if (!(enumerable is IList)) return false; 
            IList<object> parent = enumerable as IList<object>;
            List<int> dimensionList = new List<int>();
            for (int i = 0; i < parent.Count; ++i)
            {
                if (!(parent[i] is IList<object>)) return false; 
            }
            return true;
        }

        /// <summary>
        /// Create an enumerator suitable for constructing images (i.e. travels along the first dimension,
        /// then the second). If not 2D, first element is null.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<double> ToImageEnumerator(IEnumerable<object> enumerable, out int xLength, out int yLength)
        {
            int[] dimensions = GeneralArray.GetDimensions(enumerable);
            xLength = dimensions[0];
            yLength = 0;
            if (dimensions.Length > 2) return null;

            // If this is a list of lists, then create enumerator, otherwise convert to a rectangular array first.
            // This gives better performance if not indexable or if this is a Numpy array.

            Type arrayType = enumerable.GetType();
            if (dimensions.Length == 2 && arrayType.Name != "ndarray" && IsIListOfILists(enumerable))
            {
                List<int> dimensionList = new List<int>();
                IList parent = enumerable as IList;
                int minLength = int.MaxValue; int maxLength = int.MinValue;
                int count = parent.FastCount();

                for (int i = 0; i < count; ++i)
                {
                    IList child = parent[i] as IList;
                    if (child == null) minLength = 0;
                    else
                    {
                        minLength = Math.Min(minLength, child.Count);
                        maxLength = Math.Max(maxLength, child.Count);
                    }
                }
                yLength = maxLength;
                if (minLength == 0) return null;
                else return Jagged2DImageEnumerable(parent, minLength, maxLength);
            }
            else if (dimensions.Length == 1)
            {
                return Enumerable1D(enumerable);
            }
            else
            {
                double[,] array = (double[,])GeneralArray.ToDoubleArray(enumerable);
                yLength = dimensions[1];
                return array.ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            }
        }

        private static IEnumerable<double> Enumerable1D(IEnumerable<object> enumerable)
        {
            foreach (object item in enumerable) yield return Convert.ToDouble(item);
        }

        private static IEnumerable<double> Jagged2DImageEnumerable(IList array, int minLength, int maxLength)
        {
            IList parent = array;
            int count = parent.Count;
            for (int j = 0; j < maxLength; ++j)
            {
                for (int i = 0; i < count; ++i)
                {
                    IList child = parent[i] as IList;
                    if (j > child.Count - 1) yield return Double.NaN;
                    else yield return Convert.ToDouble((parent[i] as IList)[j]);
                }
            }
        }
    }

    public class IndexEnumerator
    {
        public int[] CurrentIndices;

        public object[] CurrentObjectIndices;

        public int[] Dimensions;

        public IndexEnumerator(int[] dimensions)
        {
            Dimensions = dimensions;
            CurrentIndices = new int[dimensions.Length];
            CurrentObjectIndices = new object[dimensions.Length];
            for (int i = 0; i < dimensions.Length; ++i) CurrentObjectIndices[i] = 0;
            CurrentIndices[0] = -1;
        }

        public bool MoveNext()
        {
            int index = 0;
            CurrentIndices[index]++;
            CurrentObjectIndices[index] = CurrentIndices[index];
            while (CurrentIndices[index] == Dimensions[index])
            {
                if (index == (Dimensions.Length - 1)) return false;
                CurrentIndices[index] = 0;
                CurrentObjectIndices[index] = 0;
                index++;
                CurrentIndices[index]++;
                CurrentObjectIndices[index] = CurrentIndices[index];
            }
            return true;
        }
    }
}
