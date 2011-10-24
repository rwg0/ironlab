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

        private Axis2DCollection axisCollection;

        private MatrixTransform graphToCanvas;
        private MatrixTransform graphToAxesCanvas;

        public GridLines(Axis2DCollection axisCollection)
        {
            this.axisCollection = axisCollection;
            gridLinesGeometry = new StreamGeometry();
            this.Stroke = Brushes.LightGray;
            this.StrokeThickness = 1;
            this.StrokeLineJoin = PenLineJoin.Miter;
            graphToAxesCanvas = axisCollection[0].PlotPanel.graphToAxesCanvas;
            graphToCanvas = axisCollection[0].PlotPanel.graphToCanvas;
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                gridLinesGeometryContext = gridLinesGeometry.Open();
                Point Offset = new Point(graphToAxesCanvas.Matrix.OffsetX - graphToCanvas.Matrix.OffsetX, graphToAxesCanvas.Matrix.OffsetY - graphToCanvas.Matrix.OffsetY);
                for (int i = 0; i < axisCollection[0].Ticks.Length && i < axisCollection[1].Ticks.Length; ++i)
                {
                    Point tickStart = axisCollection[0].TickStartPosition(i);
                    tickStart.X -= Offset.X; tickStart.Y -= Offset.Y;
                    Point tickEnd = axisCollection[1].TickStartPosition(i);
                    tickEnd.X -= Offset.X; tickEnd.Y -= Offset.Y;
                    gridLinesGeometryContext.BeginFigure(tickStart, false, false);
                    gridLinesGeometryContext.LineTo(tickEnd, true, false);
                }
                gridLinesGeometryContext.Close();
                return gridLinesGeometry;
            }
        }
    }
}
