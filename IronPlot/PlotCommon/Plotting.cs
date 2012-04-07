// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    /// <summary>
    /// Static class for providing convenience methods for main Plotting classes
    /// </summary>
    public static class Plotting
    {
        /// <summary>
        /// Convenience method. Line plot of one or more lines or false colour image
        /// </summary>
        /// <param name="parameters">
        /// <para>Supports multiple lines with PyLab-like line properties, e.g. Plot2D(x1, y1, "-or", x2, y2, "--sb")</para>
        /// <para>plot2d(array) returns line plot if array is a vector (x values are craeted in this case)</para>
        /// <para>If array is a vector, line properties can be added: plot2d(array, "-or")</para>
        /// <para>plot2d(array) returns a false colour plot if array is a matrix</para>
        /// <para>plot2d(x, y, matrix) returns a false colour plot matrix using min and max values of x and y vectors to define false colour plot area</para>
        /// </param>
        /// <returns></returns>
        public static List<Plot2DCurve> Plot2D(params object[] parameters)
        {
            List<Plot2DCurve> curves = new List<Plot2DCurve>();
            Plot2D plot2D = null;
            int paramIndex = 0; // current parameter
            if (parameters.Length > 0 && parameters[0].GetType() == typeof(Plot2D))
            {
                plot2D = (Plot2D)parameters[0];
                paramIndex++;
            }
            else plot2D = new Plot2D();
            // x and y will contain lists of x and y vectors to be plotted
            List<double[]> x = new List<double[]>();
            List<double[]> y = new List<double[]>();
            double[] createdX = null;
            List<string> lineProperty = new List<string>();
            int lastLineIndex = paramIndex; // index of the end of the last line specification
            while (paramIndex < parameters.Length)
            {
                // Get the next two parameters, or up to the next string entry (line property) 
                while ((paramIndex < (lastLineIndex + 2)) && (paramIndex < parameters.Length) && (parameters[paramIndex].GetType() != Type.GetType("System.String")))
                {
                    paramIndex++;
                }
                if ((paramIndex - lastLineIndex) == 2) // 2 vectors and possibly a line property
                {
                    x.Add(Array(parameters[lastLineIndex + 0]));
                    y.Add(Array(parameters[lastLineIndex + 1]));
                    if ((paramIndex < parameters.Length) && parameters[paramIndex].GetType() == Type.GetType("System.String"))
                    {
                        lineProperty.Add((string)(parameters[paramIndex]));
                    }
                    else
                    {
                        lineProperty.Add("");
                        paramIndex--;
                    }
                }
                else if ((paramIndex - lastLineIndex) == 1) // 1 vector and possibly a line property
                {
                    y.Add(Array(parameters[lastLineIndex + 0]));
                    if (createdX != null) x.Add(createdX);
                    else
                    {
                        createdX = MathHelper.Counter(y[y.Count - 1].Length).SumWith(-1.0);
                        x.Add(createdX);
                    }
                    if ((paramIndex < parameters.Length) && parameters[paramIndex].GetType() == Type.GetType("System.String"))
                    {
                        lineProperty.Add((string)(parameters[paramIndex]));
                    }
                    else lineProperty.Add("");
                }
                else
                {
                    throw new ArgumentException("Incorrect syntax");
                }
                paramIndex++; lastLineIndex = paramIndex;
            }
            for (int i = 0; i < x.Count; ++i)
            {
                curves.Add(plot2D.AddLine(x[i], y[i], lineProperty[i]));
            }
            return curves;
        }

        static internal Window CreateNewWindow()
        {
            Window window = new Window() { Height = 640, Width = 640, Background = Brushes.White };
            window.Show();
            return window;
        }

        public static double[] Array(object convertible)
        {
            if (convertible is double[]) return convertible as double[];
            else if (convertible is DateTime[]) return (convertible as DateTime[]).Select(t => t.ToOADate()).ToArray();
            else if (convertible is IEnumerable<double>) return (convertible as IEnumerable<double>).ToArray();
            else if (convertible is IEnumerable<DateTime>) return (convertible as IEnumerable<DateTime>).Select(t => t.ToOADate()).ToArray();
            else if ((convertible is IEnumerable<object>) || (convertible is IEnumerable))
            {
                System.Array array = GeneralArray.ToDoubleArray(convertible);
                if (array.Rank == 1) return array as double[];
                else throw new Exception("Array must be one dimensional.");
            }
            else throw new Exception("Unknown array type.");
        }
    }
}
