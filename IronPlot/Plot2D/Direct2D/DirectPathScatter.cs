using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct2D;
using System.Windows;
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;
using MatrixTransform = System.Windows.Media.MatrixTransform;

namespace IronPlot
{
    /// <summary>
    /// A Direct2D Path that is plotted multiple times at different locations.
    /// In other words, this is a scatter plot.
    /// </summary>
    public class DirectPathScatter : DirectPath
    {
        private Curve curve;
        public Curve Curve { get { return curve; } set { curve = value; } }

        public double xOffsetMarker;
        public double yOffsetMarker;

        private MatrixTransform graphToCanvas;
        public MatrixTransform GraphToCanvas { get { return graphToCanvas; } set { graphToCanvas = value; } }

        public void RenderScatterGeometry(RenderTarget renderTarget)
        {
            double[] x = curve.X;
            double[] y = curve.Y;
            int length = x.Length;
            double xScale, xOffset, yScale, yOffset;
            xScale = graphToCanvas.Matrix.M11;
            xOffset = graphToCanvas.Matrix.OffsetX - this.xOffsetMarker;
            yScale = graphToCanvas.Matrix.M22;
            yOffset = graphToCanvas.Matrix.OffsetY - this.yOffsetMarker;
            bool[] include = curve.includeMarker;
            StrokeStyleProperties properties = new StrokeStyleProperties();
            properties.LineJoin = LineJoin.MiterOrBevel;
            StrokeStyle strokeStyle = new StrokeStyle(renderTarget.Factory, properties);
            for (int i = 0; i < length; ++i)
            {
                if (include[i])
                {
                    renderTarget.Transform = Matrix3x2.Translation((float)(x[i] * xScale + xOffset), (float)(y[i] * yScale + yOffset));
                    renderTarget.FillGeometry(Geometry, FillBrush);
                    renderTarget.DrawGeometry(Geometry, Brush, (float)StrokeThickness, strokeStyle);
                }
            }
            renderTarget.Transform = Matrix3x2.Identity;
        }

        public void SetGeometry(MarkersType markersType, double markersSize)
        {
            if (Geometry != null)
            {
                Geometry.Dispose();
                Geometry = null;
            }
            if (Factory == null) return;
            float width = (float)Math.Abs(markersSize);
            float height = (float)Math.Abs(markersSize);
            this.xOffsetMarker = width / 2;
            this.yOffsetMarker = height / 2;
            switch (markersType)
            {
                case MarkersType.None:
                    break;
                case MarkersType.Square:
                    this.Geometry = new RectangleGeometry(Factory, new System.Drawing.RectangleF()
                        {
                            X = 0,
                            Y = 0,
                            Width = width,
                            Height = height
                        });
                    break;
                case MarkersType.Circle:
                    this.Geometry = new EllipseGeometry(Factory, new Ellipse()
                       {
                           Center = new System.Drawing.PointF(width / 2, height / 2),
                           RadiusX = width / 2,
                           RadiusY = height / 2
                       });
                    break;
                case MarkersType.Triangle:
                    this.Geometry = new PathGeometry(Factory);
                    using (GeometrySink sink = (Geometry as PathGeometry).Open())
                    {
                        PointF p0 = new PointF(width / 2, height);
                        sink.BeginFigure(p0, FigureBegin.Hollow);
                        sink.AddLine(new PointF(width, 0f));
                        sink.AddLine(new PointF(0f, 0f));
                        sink.EndFigure(FigureEnd.Closed);
                        sink.Close();
                    }
                    break;
            }
           
        }
    }
}
