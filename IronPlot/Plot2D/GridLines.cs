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
    public class GridLines : Shape
    {
        // Grid lines geometry and context
        private StreamGeometry gridLinesGeometry;
        private StreamGeometryContext gridLinesGeometryContext;

        private Axis2D axis;

        public GridLines(Axis2D axis)
        {
            this.axis = axis;
            gridLinesGeometry = new StreamGeometry();
            this.Stroke = Brushes.LightGray;
            this.StrokeThickness = 1;
            this.StrokeLineJoin = PenLineJoin.Miter;
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                gridLinesGeometryContext = gridLinesGeometry.Open();
                for (int i = 0; i < axis.Ticks.Length; ++i)
                {
                    Point tickStart, tickEnd;
                    if (axis is XAxis)
                    {
                        tickStart = new Point(axis.GraphToCanvas(axis.Ticks[i]), 0);
                        tickEnd = new Point(axis.GraphToCanvas(axis.Ticks[i]), axis.PlotPanel.Canvas.ActualHeight);
                    }
                    else
                    {
                        tickStart = new Point(0, axis.GraphToCanvas(axis.Ticks[i]));
                        tickEnd = new Point(axis.PlotPanel.Canvas.ActualWidth, axis.GraphToCanvas(axis.Ticks[i]));
                    }
                    gridLinesGeometryContext.BeginFigure(tickStart, false, false);
                    gridLinesGeometryContext.LineTo(tickEnd, true, false);
                }
                gridLinesGeometryContext.Close();
                return gridLinesGeometry;
            }
        }
    }
}
