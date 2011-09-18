using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IronPlot
{
    /// <summary>
    /// A visual-layer line: includes line and markers.
    /// </summary>
    public class VisualLine : FrameworkElement
    {
        private Curve curve;
        private MatrixTransform graphToCanvas = new MatrixTransform(Matrix.Identity);

        public VisualLine(Curve curve, MatrixTransform graphToCanvas)
        {
            this.curve = curve;
            this.graphToCanvas = graphToCanvas;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext dc)
        {
            double xScale = graphToCanvas.Matrix.M11;
            double xOffset = graphToCanvas.Matrix.OffsetX;
            double yScale = graphToCanvas.Matrix.M11;
            double yOffset = graphToCanvas.Matrix.OffsetY;
            base.OnRender(dc);
            Pen pen = new Pen(Brushes.Black, 1.0);
            for (int i = 0; i < curve.x.Length; ++i)
            {
                double xCanvas = curve.x[i] * xScale + xOffset;
                double yCanvas = curve.y[i] * yScale + yOffset;
                dc.DrawEllipse(Brushes.Red, pen, new Point(xCanvas, yCanvas), 10.0, 10.0);
            }
        }
    }
}
