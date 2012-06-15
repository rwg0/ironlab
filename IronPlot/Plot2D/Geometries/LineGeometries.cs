using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IronPlot
{
    public static class LineGeometries
    {
        public static StreamGeometry StreamGeometryFromCurve(Curve curve, MatrixTransform graphToCanvas)
        {
            double[] tempX;
            double[] tempY;
            if (graphToCanvas != null)
            {
                tempX = curve.xTransformed.MultiplyBy(graphToCanvas.Matrix.M11).SumWith(graphToCanvas.Matrix.OffsetX);
                tempY = curve.yTransformed.MultiplyBy(graphToCanvas.Matrix.M22).SumWith(graphToCanvas.Matrix.OffsetY);
            }
            else
            {
                tempX = curve.xTransformed; tempY = curve.yTransformed;
            }
            StreamGeometry streamGeometry = new StreamGeometry();
            StreamGeometryContext context = streamGeometry.Open();
            int lines = 0;
            for (int i = 0; i < curve.x.Length; ++i)
            {
                if (i == 0)
                {
                    context.BeginFigure(new Point(tempX[i], tempY[i]), false, false);
                }
                else
                {
                    if (curve.includeLinePoint[i])
                    {
                        context.LineTo(new Point(tempX[i], tempY[i]), true, false);
                        lines++;
                    }
                }
            }
            context.Close();
            return streamGeometry;
        }

        public static PathGeometry PathGeometryFromCurve(Curve curve, MatrixTransform graphToCanvas)
        {
            double xScale, xOffset, yScale, yOffset;
            if (graphToCanvas != null)
            {
                xScale = graphToCanvas.Matrix.M11;
                xOffset = graphToCanvas.Matrix.OffsetX;
                yScale = graphToCanvas.Matrix.M22;
                yOffset = graphToCanvas.Matrix.OffsetY;
            }
            else
            {
                xScale = 1; xOffset = 0;
                yScale = 1; yOffset = 0;
            }

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            LineSegment lineSegment;
            double xCanvas = curve.xTransformed[0] * xScale + xOffset;
            double yCanvas = curve.yTransformed[0] * yScale + yOffset;
            pathFigure.StartPoint = new Point(xCanvas, yCanvas);
            for (int i = 1; i < curve.x.Length; ++i)
            {
                if (curve.includeLinePoint[i])
                {
                    lineSegment = new LineSegment();
                    xCanvas = curve.xTransformed[i] * xScale + xOffset;
                    yCanvas = curve.yTransformed[i] * yScale + yOffset;
                    lineSegment.Point = new Point(xCanvas, yCanvas);
                    pathFigure.Segments.Add(lineSegment);
                }
            }
            pathFigure.IsClosed = false;
            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }
    }
}
