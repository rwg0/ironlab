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
    public class Axes2D : Shape
    {
        // Canvas containing axes.
        protected Canvas canvas;

        protected MatrixTransform graphToCanvas;
        
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
        public XAxis2DCollection XAxes { get { return xAxes as XAxis2DCollection; } }
        public YAxis2DCollection YAxes { get { return yAxes as YAxis2DCollection; } }

        public Axes2D(PlotPanel plotPanel)
        {
            axesGeometry = new StreamGeometry();
            this.canvas = plotPanel.axesCanvas;
            this.graphToCanvas = plotPanel.graphToAxesCanvas;
            xAxisBottom = new XAxis(plotPanel);
            xAxisBottom.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Bottom);
            xAxisTop = new XAxis(plotPanel);
            xAxisTop.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Top);
            //
            xAxes = new XAxis2DCollection();
            xAxes.AddAxis(xAxisBottom); xAxes.AddAxis(xAxisTop);
            xAxis = xAxisBottom;
            xAxisTop.LabelsVisible = false;
            xAxisTop.TicksVisible = true;
            //
            yAxisLeft = new YAxis(plotPanel);
            yAxisLeft.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Left);
            yAxisRight = new YAxis(plotPanel);
            yAxisRight.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Right);
            //
            yAxes = new YAxis2DCollection();
            yAxes.AddAxis(yAxisLeft); yAxes.AddAxis(yAxisRight);
            yAxis = yAxisLeft;
            yAxisRight.LabelsVisible = false;
            yAxisRight.TicksVisible = true;
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
                AddTicks(axesGeometryContext);
                axesGeometryContext.Close();
                return axesGeometry;
            }
        }

        protected void AddTicks(StreamGeometryContext context)
        {
            Point tickPosition;
            bool left = (yAxisLeft.TicksVisible == true);
            bool right = (yAxisRight.TicksVisible == true);
            bool top = (xAxisTop.TicksVisible == true);
            bool bottom = (xAxisBottom.TicksVisible == true);
            for (int i = 0; i < yAxisLeft.Ticks.Length; ++i)
            {
                // Left
                if (left)
                {
                    tickPosition = yAxisLeft.TickStartPosition(i);
                    context.BeginFigure(tickPosition, false, false);
                    tickPosition.X = tickPosition.X - yAxisLeft.TickLength;
                    context.LineTo(tickPosition, true, false);
                }
                // Right
                if (right)
                {
                    tickPosition = yAxisRight.TickStartPosition(i);
                    context.BeginFigure(tickPosition, false, false);
                    tickPosition.X = tickPosition.X + yAxisRight.TickLength;
                    context.LineTo(tickPosition, true, false);
                }
            }
            for (int i = 0; i < xAxis.Ticks.Length; ++i)
            {
                // Top
                if (top)
                {
                    tickPosition = xAxisTop.TickStartPosition(i);
                    context.BeginFigure(tickPosition, false, false);
                    tickPosition.Y = tickPosition.Y - xAxisTop.TickLength;
                    context.LineTo(tickPosition, true, false);
                }
                // Bottom
                if (bottom)
                {
                    tickPosition = xAxisBottom.TickStartPosition(i);
                    context.BeginFigure(tickPosition, false, false);
                    tickPosition.Y = tickPosition.Y + xAxisBottom.TickLength;
                    context.LineTo(tickPosition, true, false);
                }
            }
        }

        internal void UpdateTicks()
        {
            xAxisBottom.DeriveTicks(); xAxisTop.DeriveTicks();
            yAxisLeft.DeriveTicks(); yAxisRight.DeriveTicks();
        }

        internal void UpdateAxisPositionsFull(Rect availableSize, bool axesEqual, Thickness minimumAxesMargin, out Rect canvasPosition, out Rect axesCanvasPosition)
        {
            Thickness requiredMargin;
            int iter = 0;
            // Perform two passes. On the first, all labels are present and are then culled if necessary.
            // On the second pass only the remaining labels are present.
            while (iter < 2)
            {
                // Required margin is the margin that would be needed if the axes lengths were all zero.
                requiredMargin = new Thickness(Math.Max(minimumAxesMargin.Left, yAxisLeft.AxisThickness()),
                Math.Max(minimumAxesMargin.Top, xAxisTop.AxisThickness()),
                Math.Max(minimumAxesMargin.Right, yAxisRight.AxisThickness()),
                Math.Max(minimumAxesMargin.Bottom, xAxisBottom.AxisThickness()));
                
                // Calculate the position of each axis assuming all labels for that axis are visible.
                AxisMargin xAxesMargin = new AxisMargin(requiredMargin.Left, requiredMargin.Right);
                xAxisBottom.SetAxisLengthFromLabels(availableSize.Width, xAxesMargin);
                xAxisTop.SetAxisLengthFromLabels(availableSize.Width, xAxesMargin);
                // Demand that axes' scales are the same and therefore take worst-case.
                if (Math.Abs(xAxisTop.scale) < Math.Abs(xAxisBottom.scale))
                    xAxisBottom.OverrideAxisScaling(xAxisTop.scale, xAxisTop.offset, xAxisTop.axisMargin);
                else xAxisTop.OverrideAxisScaling(xAxisBottom.scale, xAxisBottom.offset, xAxisBottom.axisMargin);
                //
                AxisMargin yAxesMargin = new AxisMargin(requiredMargin.Bottom, requiredMargin.Top);
                yAxisLeft.SetAxisLengthFromLabels(availableSize.Height, yAxesMargin);
                yAxisRight.SetAxisLengthFromLabels(availableSize.Height, yAxesMargin);
                if (Math.Abs(yAxisLeft.scale) < Math.Abs(yAxisRight.scale))
                    yAxisRight.OverrideAxisScaling(yAxisLeft.scale, yAxisLeft.offset, yAxisLeft.axisMargin);
                else
                    yAxisLeft.OverrideAxisScaling(yAxisRight.scale, yAxisRight.offset, yAxisRight.axisMargin);
                // At the end of the second iteration, we know which axis must be reduced in size
                // if the axes are to be constrained to be equal.
                if ((iter == 1) && axesEqual)
                {
                    if (yAxisLeft.scale > xAxisBottom.scale)
                    {
                        // Need to shorten the y axis.  
                        yAxisLeft.ScaleAxis(xAxisBottom.scale, availableSize.Height);
                        yAxisRight.ScaleAxis(xAxisBottom.scale, availableSize.Height);
                    }
                    else
                    {
                        // Need to shorten the x axis.  
                        xAxisTop.ScaleAxis(yAxisLeft.scale, availableSize.Width);
                        xAxisBottom.ScaleAxis(yAxisLeft.scale, availableSize.Width);
                    }
                }
                // Now position labels, removing as required to prevent overlap.
                xAxisBottom.yPosition = yAxisLeft.axisMax - yAxisLeft.axisMargin.MinMargin;
                xAxisTop.yPosition = yAxisLeft.axisMargin.MaxMargin;
                yAxisLeft.xPosition = xAxisBottom.axisMargin.MinMargin;
                yAxisRight.xPosition = xAxisBottom.axisMax - xAxisBottom.axisMargin.MaxMargin;
                // If axes are not equal then labels are culled on the first pass and then we re scale the graph
                // to the available space on the second.
                bool cullOverlapping = ((iter == 0) && !axesEqual) || ((iter == 1) && axesEqual);
                xAxisBottom.PositionLabels(cullOverlapping);
                xAxisTop.PositionLabels(cullOverlapping);
                yAxisLeft.PositionLabels(cullOverlapping);
                yAxisRight.PositionLabels(cullOverlapping);
                // The axis length may have to be readjusted if labels were removed, so repeat the process.
                iter++;
            }
            graphToCanvas.Matrix = new Matrix(xAxisBottom.scale, 0, 0, -yAxisLeft.scale, -xAxisBottom.offset, yAxisLeft.offset + yAxisLeft.axisMax);
            axesCanvasPosition = new Rect(0, 0,
                xAxisBottom.axisMax,
                yAxisLeft.axisMax);
            canvasPosition = new Rect(xAxisBottom.axisMargin.MinMargin, yAxisLeft.axisMargin.MaxMargin,
                xAxisBottom.axisMax - xAxisBottom.axisMargin.MinMargin - xAxisBottom.axisMargin.MaxMargin,
                yAxisLeft.axisMax - yAxisLeft.axisMargin.MinMargin - yAxisLeft.axisMargin.MaxMargin);
        }

        internal void UpdateAxisPositionsOffsetOnly(Rect availableSize, out Rect canvasPosition, out Rect axesCanvasPosition)
        {
            xAxisBottom.UpdateOffset();
            xAxisTop.UpdateOffset();
            yAxisLeft.UpdateOffset();
            yAxisRight.UpdateOffset();
            xAxisBottom.PositionLabels(true);
            xAxisTop.PositionLabels(true);
            yAxisLeft.PositionLabels(true);
            yAxisRight.PositionLabels(true);
            graphToCanvas.Matrix = new Matrix(xAxisBottom.scale, 0, 0, -yAxisLeft.scale, -xAxisBottom.offset, yAxisLeft.offset + yAxisLeft.axisMax);
            axesCanvasPosition = new Rect(0, 0,
                xAxisBottom.axisMax,
                yAxisLeft.axisMax);
            canvasPosition = new Rect(xAxisBottom.axisMargin.MinMargin, availableSize.Height - yAxisLeft.axisMax + yAxisLeft.axisMargin.MaxMargin,
                xAxisBottom.axisMax - xAxisBottom.axisMargin.MinMargin - xAxisBottom.axisMargin.MaxMargin,
                yAxisLeft.axisMax - yAxisLeft.axisMargin.MinMargin - yAxisLeft.axisMargin.MaxMargin);
        }
    }
}
