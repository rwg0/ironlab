// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace IronPlot
{
    public enum SortedValues { X, Y }
    
    public partial class Curve
    {
        internal double[] x, y;
        internal double[] xTransformed, yTransformed;
        internal SortedValues SortedValues;
        internal double[] TransformedSorted;
        internal int[] SortedToUnsorted;

        internal bool[] includeLinePoint; // Whether or not to include the point in the line Geometry.
        internal bool[] includeMarker; // Whether or not to include the marker in the Geometry.
        protected byte[] pointRegion;
        protected int n;

        protected Matrix cachedTransform = Matrix.Identity;
        protected Rect cachedRegion = new Rect(0, 0, 0, 0);

        public double[] X
        {
            get { return x; }
        }
        
        public double[] Y
        {
            get { return y; }
        }
        
        public Curve(double[] x, double[] y)
        {
            this.x = x; this.y = y;
            Validate();
            Transform(null, null);
            PrepareLineData(x.Length);
            DetermineSorted();
        }

        public Curve(IEnumerable<double> x, IEnumerable<double> y)
        {
            int count = Math.Min(x.Count(), y.Count());
            this.x = new double[count];
            this.y = new double[count];
            for (int i = 0; i < count; ++i)
            {
                this.x[i] = x.ElementAt(i);
                this.y[i] = y.ElementAt(i);
            }
            Transform(null, null);
            PrepareLineData(count);
            DetermineSorted();
        }

        public Rect Bounds()
        {
            return new Rect(new Point(xTransformed.Min(), yTransformed.Min()), new Point(xTransformed.Max(), yTransformed.Max()));
        }

        private void PrepareLineData(int length)
        {
            n = length;
            includeLinePoint = new bool[length];
            includeMarker = new bool[length];
            pointRegion = new byte[length];
            for (int i = 0; i < length; ++i)
            {
                includeLinePoint[i] = true;
                includeMarker[i] = true;
            }
        }

        /// <summary>
        /// Determine if either x or y values are sorted; if neither, sort x.
        /// </summary>
        private void DetermineSorted()
        {
            SortedToUnsorted = Enumerable.Range(0, xTransformed.Length).ToArray();
            if (IsSorted(xTransformed))
            {
                TransformedSorted = xTransformed;
                SortedValues = SortedValues.X;
            }
            else if (IsSorted(yTransformed))
            {
                TransformedSorted = yTransformed;
                SortedValues = SortedValues.Y;
            }
            else
            {
                TransformedSorted = (double[])xTransformed.Clone();
                Array.Sort(TransformedSorted, SortedToUnsorted);
            }
        }

        private bool IsSorted(double[] array)
        {
            for (int i = 1; i < array.Length - 1; ++i)
            {
                if (array[i] < array[i - 1]) return false;
            }
            return true;
        }

        protected void Validate()
        {
            if (x.Length != y.Length)
            {
                throw new ArgumentException("Component vectors' lengths must be equal");
            }
        }

        internal void Transform(Func<double, double> graphTransformX, Func<double, double> graphTransformY)
        {
            if (graphTransformX == null)
            {
                xTransformed = x;
            }
            else
            {
                int length = x.Length;
                xTransformed = new double[length];
                for (int i = 0; i < length; ++i) xTransformed[i] = graphTransformX(x[i]);
            }
            if (graphTransformY == null)
            {
                yTransformed = y;
            }
            else
            {
                int length = y.Length;
                yTransformed = new double[length];
                for (int i = 0; i < length; ++i) yTransformed[i] = graphTransformY(y[i]);
            }
            DetermineSorted();
        }

        public void FilterMinMax(MatrixTransform canvasToGraph, Rect viewBounds)
        {
            if (xTransformed.Length <= 2) return;
            // We do not need to re-evaluate the set of lines if the view is contained by the cached region
            // and the size of the region is not significantly changed.
            double width = Math.Max(viewBounds.Width, canvasToGraph.Matrix.M11 * 500);
            double height = Math.Max(viewBounds.Height, canvasToGraph.Matrix.M22 * 500);
            double xViewMin = viewBounds.Left - width / 2;
            double xViewMax = viewBounds.Right + width / 2;
            double yViewMin = viewBounds.Top - height / 2;
            double yViewMax = viewBounds.Bottom + height / 2;
            double widthRatio = canvasToGraph.Matrix.M11 /cachedTransform.M11; 
            double heightRatio = canvasToGraph.Matrix.M22 /cachedTransform.M22; 
            if (ContainsRegion(cachedRegion, viewBounds) && (widthRatio > 0.9) && (widthRatio < 1.1)
                && (heightRatio > 0.9) && (heightRatio < 1.1)) return;
            cachedRegion = new Rect(new Point(xViewMin, yViewMin), new Point(xViewMax, yViewMax));
            cachedTransform.M11 = canvasToGraph.Matrix.M11;
            cachedTransform.M22 = canvasToGraph.Matrix.M22;

            // Exclude all line points by default: these will subsequently be added as necessary.
            // Include those marker points that are in the cached region and make note of region.
            int nPoints = includeLinePoint.Length;
            for (int j = 0; j < nPoints; ++j)
            {
                includeLinePoint[j] = false;
                includeMarker[j] = false;
                double newX = xTransformed[j]; double newY = yTransformed[j];
                if (newX < xViewMin) pointRegion[j] = 1;
                else if (newX > xViewMax) pointRegion[j] = 2;
                else if (newY < yViewMin) pointRegion[j] = 4;
                else if (newY > yViewMax) pointRegion[j] = 8;
                else
                {
                    pointRegion[j] = 0;
                    includeMarker[j] = true;
                }
            }
            double xStart, yStart;
            double xMax, xMin, yMax, yMin;
            double xMax2, xMin2, yMax2, yMin2;
            int xMaxIndex, xMinIndex, yMaxIndex, yMinIndex;
            bool withinXBound, withinYBound;
            double deltaX = Math.Abs(canvasToGraph.Matrix.M11) * 0.25; 
            double deltaY = Math.Abs(canvasToGraph.Matrix.M22) * 0.25;
            double deltaX2 = Math.Abs(canvasToGraph.Matrix.M11) * 0.75;
            double deltaY2 = Math.Abs(canvasToGraph.Matrix.M22) * 0.75;
            int i = 0;
            byte region = pointRegion[i]; byte newRegion;
            while (true)
            {
                newRegion = pointRegion[i + 1];
                // If the current point is outside the cached region, and the next point is in the same region, then we start to exclude points.
                if ((region > 0) && (region == newRegion))
                {
                    // Exclude until the current point is in a different region, or until we reach the penultimate point of the series.
                    while ((region == newRegion) && (i < nPoints - 2))
                    {
                        ++i;
                        newRegion = pointRegion[i + 1];
                    }
                    // This is the penultimate point and both this and the last point should be excluded:
                    if (region == newRegion) break;
                    // Otherwise we need to include both this and the next point.
                    includeLinePoint[i] = true;
                    includeLinePoint[i + 1] = true;
                    ++i;
                }
                else
                {
                    includeLinePoint[i] = true;
                }
                // Now do max-min filtration
                xStart = xTransformed[i]; yStart = yTransformed[i];
                ++i;
                if (i == nPoints) break;
                xMax = xStart + deltaX; xMin = xStart - deltaX;
                yMax = yStart + deltaY; yMin = yStart - deltaY;
                xMax2 = xStart + deltaX2; xMin2 = xStart - deltaX2;
                yMax2 = yStart + deltaY2; yMin2 = yStart - deltaY2;
                xMaxIndex = -1; xMinIndex = -1; yMaxIndex = -1; yMinIndex = -1;
                withinXBound = true; withinYBound = true;
                // Do max-min filtration:
                while (true)
                {
                    double newX = xTransformed[i]; double newY = yTransformed[i];
                    if (newX > xMax)
                    {
                        xMax = newX;
                        xMaxIndex = i;
                        if (newX > xMax2)
                        {
                            withinXBound = false;
                            if (!withinYBound)
                            {
                                if (yMaxIndex > -1) includeLinePoint[yMaxIndex] = true;
                                if (yMinIndex > -1) includeLinePoint[yMinIndex] = true;
                                break;
                            }
                        }
                    }
                    else if (newX < xMin)
                    {
                        xMin = newX;
                        xMinIndex = i;
                        if (newX < xMin2)
                        {
                            withinXBound = false;
                            if (!withinYBound)
                            {
                                if (yMaxIndex > -1) includeLinePoint[yMaxIndex] = true;
                                if (yMinIndex > -1) includeLinePoint[yMinIndex] = true;
                                break;
                            }
                        }
                    }
                    if (newY > yMax)
                    {
                        yMax = newY;
                        yMaxIndex = i;
                        if (newY > yMax2)
                        {
                            withinYBound = false;
                            if (!withinXBound)
                            {
                                if (xMaxIndex > -1) includeLinePoint[xMaxIndex] = true;
                                if (xMinIndex > -1) includeLinePoint[xMinIndex] = true;
                                break;
                            }
                        }
                    }
                    else if (newY < yMin)
                    {
                        yMin = newY;
                        yMinIndex = i;
                        if (newY < yMin2)
                        {
                            withinYBound = false;
                            if (!withinXBound)
                            {
                                if (xMaxIndex > -1) includeLinePoint[xMaxIndex] = true;
                                if (xMinIndex > -1) includeLinePoint[xMinIndex] = true;
                                break;
                            }
                        }
                    }
                    if (i == (nPoints - 1)) break;
                    ++i;
                }
                includeLinePoint[i] = true;
                includeLinePoint[i - 1] = true;
                if (i == (nPoints - 1)) break;
                region = pointRegion[i];
            }
            int included = 0;
            for (int j = 0; j < includeLinePoint.Length; ++j)
            {
                if (includeLinePoint[j] == true) ++included;
            }

        }

        protected bool ContainsRegion(Rect container, Rect contained)
        {
            bool contains = true;
            contains = (contained.Left >= container.Left) && (contained.Right <= container.Right)
                && (contained.Top >= container.Top) && (contained.Bottom <= container.Bottom);
            return contains;
        }

        public void FilterLinInterp(MatrixTransform canvasToGraph)
        {
            for (int j = 0; j < includeLinePoint.Length; ++j)
            {
                includeLinePoint[j] = true;
            }
            double x1, y1, x2, y2, x3, y3;
            int i = 0;
            int nExcluded = 0;
            bool newlyExcluded = true;
            x1 = x[0]; y1 = y[0];
            int toPotentiallyExclude = 0;
            double cutOffX = Math.Abs(canvasToGraph.Matrix.M11);
            double cutOffY = Math.Abs(canvasToGraph.Matrix.M22);
            while ((nExcluded < (this.X.Length - 100)) && newlyExcluded)
            {
                newlyExcluded = false;
                i = 0; x1 = x[0]; y1 = y[0];
                while (i < (this.X.Length - 4))
                {
                    ++i; // Find new point to potentially miss out
                    while (!includeLinePoint[i])
                    {
                        i += 1;
                    }
                    toPotentiallyExclude = i;
                    if (i > this.X.Length - 4) break;  
                    x2 = x[i];
                    y2 = y[i];
                    ++i; // Find new point to draw line to
                    while (!includeLinePoint[i])
                    {
                        i += 1;
                    }
                    x3 = x[i];
                    y3 = y[i];
                    if (((Math.Abs(x2 - x1) < cutOffX) || (Math.Abs(x3 - x2) < cutOffX)) ||
                        (Math.Abs((y1 + (x2 - x1) * (y3 - y1) / (x3 - x1) - y2)) < cutOffY))
                    {
                        includeLinePoint[toPotentiallyExclude] = false;
                        newlyExcluded = true;
                        nExcluded += 1;
                        x1 = x3; y1 = y3;
                    }
                    else
                    {
                        x1 = x2; y1 = y2;
                    }
                }
            }
            int totalExcluded = nExcluded;
            int remaining = this.X.Length - nExcluded;
        }

    }
}
