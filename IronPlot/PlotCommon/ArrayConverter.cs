using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    /// <summary>
    /// Class to deal with common non-System.Array arrays, e.g. NumpPy arrays.
    /// </summary>
    public class GeneralArray
    {
        public unsafe static Array ToDoubleArray(object generalArray)
        {
            Type arrayType = generalArray.GetType();
            Array managedArray = null;

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
#if ILNumerics
            if (generalArray is ILArray<double>)
            {
                ILArray<double> generalArrayIL = generalArray as ILArray<double>;
                if ((generalArrayIL.Dimensions.NumberOfDimensions == 1) || 
                    ((generalArrayIL.Dimensions.NumberOfDimensions == 2) && (generalArrayIL.Dimensions[1] == 1)))
                {
                    int n = generalArrayIL.Dimensions[0];
                    managedArray = new double[n];
                    double[] managedArrayCast = managedArray as double[];
                    for (int i = 0; i < n; ++i) managedArrayCast[i] = generalArrayIL.GetValue(i);
                    return managedArray;
                }
                else if (generalArrayIL.Dimensions.NumberOfDimensions == 2)
                {
                    int n1 = generalArrayIL.Dimensions[0];
                    int n2 = generalArrayIL.Dimensions[1];
                    managedArray = new double[n1,n2];
                    double[,] managedArrayCast = managedArray as double[,];
                    for (int i = 0; i < n1; ++i)
                    {
                        for (int j = 0; j < n2; ++j)
                        {
                            managedArrayCast[i, j] = generalArrayIL.GetValue(i, j);
                        }
                    }
                    return managedArray;
                }
                throw new Exception("Array must be one or two dimensionsal.");
            }
#endif

#if NumPy
            if (arrayType.Name == "ndarray")
            {
                dynamic dynamicArray = generalArray;
                dynamic newArray = null;
                if (dynamicArray.dtype.name != "float64")
                {
                    throw new Exception("Input array must be of type Double.");
                }
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

                int[] dimensions = new int[dynamicArray.Dims.Length];
                int length = 1;
                for (int i = 0; i < dimensions.Length; ++i)
                {
                    dimensions[i] = (int)dynamicArray.Dims[i];
                    length *= dimensions[i]; 
                }
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
                //IndexEnumerator enumerator = new IndexEnumerator(dimensions);
                //while (enumerator.MoveNext())
                //{
                //    array.SetValue(dynamicArray.item(enumerator.CurrentObjectIndices), enumerator.CurrentIndices);
                //}
            }
#endif
            return managedArray;
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
