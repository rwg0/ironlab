// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace IronPlot
{
    public class AxisPair
    {
        public XAxis XAxis;
        public YAxis YAxis;

        public AxisPair(XAxis xAxis, YAxis yAxis)
        {
            XAxis = xAxis; YAxis = yAxis;
        }
    }

    public class Axes2D : Shape
    {
        public static readonly DependencyProperty AxisSpacingProperty =
            DependencyProperty.Register("AxisSpacingProperty",
            typeof(double), typeof(Axes2D),
            new PropertyMetadata((double)5));

        public double AxisSpacing
        {
            set { SetValue(AxisSpacingProperty, value); }
            get { return (double)GetValue(AxisSpacingProperty); }
        }

        // Canvas containing axes.
        protected Canvas canvas;

        Size minCanvasSize = new Size();
        Size maxCanvasSize = new Size(10000, 10000);
        
        // Axes geometry and context
        protected StreamGeometry axesGeometry;
        protected StreamGeometryContext axesGeometryContext;
        //
        internal XAxis xAxisBottom, xAxisTop; 
        internal YAxis yAxisLeft, yAxisRight;

        private XAxis xAxis;
        private YAxis yAxis;
        private Axis2DCollection xAxes;
        private Axis2DCollection yAxes;

        private AxisPair equalAxes;

        public XAxis2DCollection XAxes { get { return xAxes as XAxis2DCollection; } }
        public YAxis2DCollection YAxes { get { return yAxes as YAxis2DCollection; } }

        public Axes2D(PlotPanel plotPanel)
        {
            axesGeometry = new StreamGeometry();
            this.canvas = plotPanel.axesCanvas;
            xAxisBottom = new XAxis();
            xAxisBottom.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Bottom);
            xAxisTop = new XAxis();
            xAxisTop.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Top);
            //
            xAxes = new XAxis2DCollection(plotPanel);
            xAxes.Add(xAxisBottom); xAxes.Add(xAxisTop);
            xAxis = xAxisBottom;
            xAxisTop.LabelsVisible = false;
            xAxisTop.TicksVisible = true;
            xAxisTop.BindToAxis(xAxisBottom);
            //
            yAxisLeft = new YAxis();
            yAxisLeft.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Left);
            yAxisRight = new YAxis();
            yAxisRight.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Right);
            //
            yAxes = new YAxis2DCollection(plotPanel);
            yAxes.Add(yAxisLeft); yAxes.Add(yAxisRight);
            yAxis = yAxisLeft;
            yAxisRight.LabelsVisible = false;
            yAxisRight.TicksVisible = true;
            yAxisRight.BindToAxis(yAxisLeft);
            //
            UpdateTicks();
            this.Stroke = Brushes.Black;
            this.StrokeThickness = 1;
            this.StrokeLineJoin = PenLineJoin.Miter;        
        }

        protected override Geometry DefiningGeometry
        {
            get 
            {
                MatrixTransform graphToCanvas = new MatrixTransform(xAxisBottom.Scale, 0, 0, -yAxisLeft.Scale, -xAxisBottom.Offset, yAxisLeft.Offset + yAxisLeft.AxisTotalLength);
                axesGeometryContext = axesGeometry.Open();
                Lines lines = new Lines();
                Point canvasTopLeft = new Point(0, 0);
                double canvasWidth, canvasHeight;
                Point topLeftGraph = new Point(xAxis.Min, yAxis.Max);
                Point bottomRightGraph = new Point(xAxis.Max, yAxis.Min);
                Point topLeftPlot = graphToCanvas.Transform(topLeftGraph);
                Point bottomRightPlot = graphToCanvas.Transform(bottomRightGraph);
                canvasWidth = Math.Max(bottomRightPlot.X - topLeftPlot.X, 1.0);
                canvasHeight = Math.Max(bottomRightPlot.Y - topLeftPlot.Y, 1.0);

                // Add in axes lines
                Point contextPoint = new Point(topLeftPlot.X, topLeftPlot.Y);
                axesGeometryContext.BeginFigure(contextPoint, false, true);
                contextPoint.Y = contextPoint.Y + canvasHeight; axesGeometryContext.LineTo(contextPoint, true, false);
                contextPoint.X = contextPoint.X + canvasWidth; axesGeometryContext.LineTo(contextPoint, true, false);
                contextPoint.Y = contextPoint.Y - canvasHeight; axesGeometryContext.LineTo(contextPoint, true, false);
                //
                RenderEachAxis();
                //
                axesGeometryContext.Close();
                return axesGeometry;
            }
        }

        protected void RenderEachAxis()
        {
            IEnumerable<Axis2D> allAxis = xAxes.Concat(yAxes);
            foreach (Axis2D axis in allAxis) axis.RenderAxis();
        }

        internal void UpdateScales(Rect newViewedRegion)
        {
            //foreach (Axis2D axis in xAxes) axis.;
        }

        internal void UpdateTicks()
        {
            foreach (Axis2D axis in xAxes) axis.DeriveTicks();
            foreach (Axis2D axis in yAxes) axis.DeriveTicks();
        }

        /// <summary>
        /// Increase axis margins by amount necessary to ensure both that there is room
        /// for labels and that the necessary axes are aligned.
        /// </summary>
        /// <param name="alignedAxes">List of axes that need to be aligned.</param>
        internal void ExpandAxisMargins(List<Axis2D> alignedAxes)
        {
            AxisMargin margin = new AxisMargin(alignedAxes.Max(axis => axis.AxisMargin.LowerMargin), alignedAxes.Max(axis => axis.AxisMargin.UpperMargin));
            // Assume that all axes have the same total length, then set the margin and update Scale and Offset.
            foreach (Axis2D axis in alignedAxes) axis.ResetAxisMargin(margin);
            int tickIndex = 0;
            int maxTickIndex = alignedAxes.Max(axis => axis.Ticks.Length) / 2;
            int[] tickPair = new int[2];
            Axis2D limitingLowerAxis = null;
            int limitingLowerTickIndex = 0;
            double limitingLowerSemiWidth = margin.LowerMargin;
            Axis2D limitingUpperAxis = null;
            int limitingUpperTickIndex = 0;
            double limitingUpperSemiWidth = margin.UpperMargin;
            double offsetLower = 0;
            double deltaLower = alignedAxes[0].MaxTransformed - alignedAxes[0].MinTransformed;
            double deltaUpper = deltaLower;
            double axisTotalLength = alignedAxes[0].AxisTotalLength;
            double offsetUpper = 0;

            double minPlotLength = 1.0;
            double plotLength = alignedAxes.Max(axis => axis.desiredLength);
            if (plotLength > minPlotLength)
            {
                double newTotalLength = plotLength + margin.Total();
                foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = newTotalLength;
            }
            int nRescales = 0; // for diagnosic purposes only
            while (tickIndex <= maxTickIndex)
            {
                bool reset = false;
                // if a rescaling is required, start again from the beginning.
                for (int i = 0; i < alignedAxes.Count; ++i)
                {
                    Axis2D currentAxis = alignedAxes[i];
                    tickPair[0] = tickIndex;
                    tickPair[1] = currentAxis.TicksTransformed.Length - 1 - tickIndex;
                    if ((currentAxis.TicksTransformed.Length - 1 - tickIndex) < tickIndex) continue;
                    for (int j = 0; j <= 1; ++j)
                    {
                        int index = tickPair[j];
                        if (currentAxis.tickLabels[index].Text == "") continue;
                        if ((currentAxis.Scale * currentAxis.TicksTransformed[index] - currentAxis.Offset - currentAxis.LimitingTickLabelSizeForLength(index) / 2) < -0.1)
                        {
                            // need to rescale axes
                            limitingLowerAxis = currentAxis;
                            limitingLowerTickIndex = index;
                            limitingLowerSemiWidth = currentAxis.LimitingTickLabelSizeForLength(index) / 2;
                            offsetLower = currentAxis.TicksTransformed[index] - currentAxis.MinTransformed;
                            deltaLower = currentAxis.MaxTransformed - currentAxis.MinTransformed;
                        }
                        else if ((currentAxis.Scale * currentAxis.TicksTransformed[index] - currentAxis.Offset + currentAxis.LimitingTickLabelSizeForLength(index) / 2) > (currentAxis.AxisTotalLength + 0.1))
                        {
                            // need to rescale axes
                            limitingUpperAxis = currentAxis;
                            limitingUpperTickIndex = index;
                            limitingUpperSemiWidth = currentAxis.LimitingTickLabelSizeForLength(index) / 2;
                            offsetUpper = currentAxis.MaxTransformed - currentAxis.TicksTransformed[index];
                            deltaUpper = currentAxis.MaxTransformed - currentAxis.MinTransformed;
                        }
                        else continue;
                        reset = true;
                        double offsetUpperPrime = offsetUpper * deltaLower / deltaUpper;
                        
                        // scale for lower-limiting axis
                        nRescales++;
                        double newScale = (axisTotalLength - limitingLowerSemiWidth - limitingUpperSemiWidth) /
                            (deltaLower - offsetLower - offsetUpperPrime);
                        if (plotLength > minPlotLength)
                        {
                            // Axis is fixed to plotLength.
                            newScale = plotLength / deltaLower;
                            margin = new AxisMargin(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = plotLength + margin.Total();
                        }
                        if (newScale * deltaLower <= minPlotLength) 
                        {
                            // Axis is fixed to minPlotLength
                            newScale = minPlotLength / deltaLower;
                            margin = new AxisMargin(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = minPlotLength + margin.Total();
                        }
                        // otherwise, axis is unfixed.
                        margin = new AxisMargin(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                        foreach (Axis2D axis in alignedAxes) axis.RescaleAxis(newScale * deltaLower / (axis.MaxTransformed - axis.MinTransformed), margin);
                        break;
                    }
                    if (reset == true) break;
                }
                if (reset == true) tickIndex = 0;
                else tickIndex++;
            }
            if (nRescales > 2)
            {
                Console.WriteLine("Many rescales...");
            }
        }

        /// <summary>
        /// Given the available size for the plot area and axes, determine the
        /// required size and the position of the plot region within this region.
        /// Also sets the axes scales and positions and positions labels.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <param name="canvasPosition"></param>
        /// <param name="axesCanvasPositions"></param>
        internal void MeasureAxesFull(Size availableSize, out Rect canvasPosition, out Size requiredSize)
        {
            // The arrangement process is
            // 1 - Calculate axes thicknesses
            // 2 - Expand axis margins if required, taking into account alignment requirements
            // 3 - Reduce axis if equal axes is demanded
            // 4 - Cull labels
            // 5 - Repeat 1 - 3 with culled labels
            requiredSize = availableSize;
            canvasPosition = new Rect();
            int iter = 0;
            var xAxesBottom = xAxes.Where(axis => (axis as XAxis).Position == XAxisPosition.Bottom);
            var xAxesTop = xAxes.Where(axis => (axis as XAxis).Position == XAxisPosition.Top);
            var yAxesLeft = yAxes.Where(axis => (axis as YAxis).Position == YAxisPosition.Left);
            var yAxesRight = yAxes.Where(axis => (axis as YAxis).Position == YAxisPosition.Right);
            var allAxes = xAxes.Concat(yAxes);
            if (xAxesBottom.First() != null) xAxesBottom.First().IsInnermost = true;
            if (xAxesTop.First() != null) xAxesTop.First().IsInnermost = true;
            if (yAxesLeft.First() != null) yAxesLeft.First().IsInnermost = true;
            if (yAxesRight.First() != null) yAxesRight.First().IsInnermost = true;
            while (iter <= 1)
            {
                // Step 1: calculate axis thicknesses
                foreach (Axis2D axis in allAxes) axis.CalculateAxisThickness();
                double axisSpacing = AxisSpacing;
                Thickness axisSpacings = new Thickness(Math.Max((yAxesLeft.Count() - 1) * axisSpacing, 0),
                    Math.Max((xAxesTop.Count() - 1) * axisSpacing, 0), Math.Max((yAxesRight.Count() - 1) * axisSpacing, 0), Math.Max((xAxesBottom.Count() - 1) * axisSpacing, 0));
                Thickness margin = new Thickness(yAxesLeft.Sum(axis => axis.AxisThickness) + axisSpacings.Left, xAxesTop.Sum(axis => axis.AxisThickness) + axisSpacings.Top,
                    yAxesRight.Sum(axis => axis.AxisThickness) + axisSpacings.Right, xAxesBottom.Sum(axis => axis.AxisThickness) + axisSpacings.Bottom);
                foreach (Axis2D axis in xAxes)
                {
                    axis.AxisTotalLength = Math.Min(availableSize.Width, maxCanvasSize.Width + axis.AxisMargin.Total());
                    axis.AxisTotalLengthConstrained = axis.AxisTotalLength;
                    axis.ResetAxisMargin(new AxisMargin(margin.Left, margin.Right));
                }
                foreach (Axis2D axis in yAxes)
                {
                    axis.AxisTotalLength = Math.Min(availableSize.Height, maxCanvasSize.Height + axis.AxisMargin.Total());
                    axis.AxisTotalLengthConstrained = axis.AxisTotalLength;
                    axis.ResetAxisMargin(new AxisMargin(margin.Bottom, margin.Top));
                }

                // Step 2: expand axis margins if necessary
                ExpandAxisMargins(xAxes.ToList());
                ExpandAxisMargins(yAxes.ToList());
                //foreach (Axis2D axis in allAxes) axis.ExpandMarginsAsRequired();

                // Step 3: take account of equal axes
                // This is only done on the second (and final iteration)
                if ((iter == 1) && (equalAxes != null))
                {
                    // code here
                }

                double yPosition = yAxes[0].AxisTotalLength - yAxes[0].AxisMargin.LowerMargin;
                foreach (XAxis xAxis in xAxesBottom)
                {
                    xAxis.yPosition = yPosition;
                    yPosition += xAxis.AxisThickness + axisSpacing;
                }
                yPosition = yAxes[0].AxisMargin.UpperMargin;
                foreach (XAxis xAxis in xAxesTop)
                {
                    xAxis.yPosition = yPosition;
                    yPosition -= xAxis.AxisThickness + axisSpacing;
                }
                double xPosition = xAxes[0].AxisMargin.LowerMargin;
                foreach (YAxis yAxis in yAxesLeft)
                {
                    yAxis.xPosition = xPosition;
                    xPosition -= yAxis.AxisThickness + axisSpacing;
                }
                xPosition = xAxes[0].AxisTotalLength - xAxes[0].AxisMargin.UpperMargin;
                foreach (YAxis yAxis in yAxesRight)
                {
                    yAxis.xPosition = xPosition;
                    xPosition += yAxis.AxisThickness + axisSpacing;
                }

                // Step 4: cull labels
                bool cullOverlapping = ((iter == 0) && (equalAxes == null)) || ((iter == 1) && (equalAxes != null));
                foreach (Axis2D axis in allAxes) axis.PositionLabels(cullOverlapping);

                iter++;
            }
            requiredSize = new Size(xAxes[0].AxisTotalLength, yAxes[0].AxisTotalLength);
            canvasPosition = new Rect(new Point(xAxes[0].AxisMargin.LowerMargin, yAxes[0].AxisMargin.UpperMargin),
                new Point(requiredSize.Width - xAxes[0].AxisMargin.UpperMargin, requiredSize.Height - yAxes[0].AxisMargin.LowerMargin));
        }

        internal void UpdateAxisPositionsOffsetOnly(Rect availableSize, out Rect canvasPosition, out Rect axesCanvasPosition)
        {
            var allAxes = xAxes.Concat(yAxes);
            foreach (Axis2D axis in allAxes)
            {
                axis.UpdateOffset();
                axis.PositionLabels(true);
            }
            axesCanvasPosition = new Rect(0, 0,
                xAxisBottom.AxisTotalLength,
                yAxisLeft.AxisTotalLength);
            canvasPosition = new Rect(xAxisBottom.AxisMargin.LowerMargin, availableSize.Height - yAxisLeft.AxisTotalLength + yAxisLeft.AxisMargin.UpperMargin,
                xAxisBottom.AxisTotalLength - xAxisBottom.AxisMargin.LowerMargin - xAxisBottom.AxisMargin.UpperMargin,
                yAxisLeft.AxisTotalLength - yAxisLeft.AxisMargin.LowerMargin - yAxisLeft.AxisMargin.UpperMargin);
        }
    }
}
