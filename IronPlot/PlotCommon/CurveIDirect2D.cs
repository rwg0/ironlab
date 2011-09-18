using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SlimDX;
using SlimDX.Direct2D;

namespace IronPlot
{
    public partial class Curve
    {
        public PathGeometry ToDirect2DPathGeometry(Factory factory, System.Windows.Media.MatrixTransform graphToCanvas)
        {
            double xScale, xOffset, yScale, yOffset;
            xScale = graphToCanvas.Matrix.M11;
            xOffset = graphToCanvas.Matrix.OffsetX;
            yScale = graphToCanvas.Matrix.M22;
            yOffset = graphToCanvas.Matrix.OffsetY;

            PathGeometry geometry = new PathGeometry(factory);

            using (GeometrySink sink = geometry.Open())
            {

                float xCanvas = (float)(x[0] * xScale + xOffset);
                float yCanvas = (float)(y[0] * yScale + yOffset);
                PointF p0 = new PointF(xCanvas, yCanvas);

                sink.BeginFigure(p0, FigureBegin.Hollow);
                for (int i = 1; i < x.Length; ++i)
                {
                    if (includeLinePoint[i])
                    {
                        xCanvas = (float)(x[i] * xScale + xOffset);
                        yCanvas = (float)(y[i] * yScale + yOffset);
                        sink.AddLine(new PointF(xCanvas, yCanvas));
                    }
                }
                sink.EndFigure(FigureEnd.Open);
                sink.Close();

            }
            return geometry;
        }
    }
}
