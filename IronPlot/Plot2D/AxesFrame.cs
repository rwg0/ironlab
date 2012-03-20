using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPlot
{
    public class AxesFrame : ContentControl
    {
        private AxisCanvas canvas;

        private Path frame;
        public Path Frame { get { return frame; } } 
        StreamGeometry geometry = new StreamGeometry(); 

        public AxesFrame()
        {
            canvas = new AxisCanvas();
            this.Content = canvas;
            frame = new Path() { Stroke = Brushes.Black, StrokeThickness = 1, StrokeLineJoin = PenLineJoin.Miter, Data = geometry };
            canvas.Children.Add(frame);
        }

        internal void Render(Rect position)
        {
            StreamGeometryContext context = geometry.Open();
            Point contextPoint = new Point(position.X, position.Y);
            context.BeginFigure(contextPoint, false, true);
            contextPoint.Y = contextPoint.Y + position.Height; context.LineTo(contextPoint, true, false);
            contextPoint.X = contextPoint.X + position.Width; context.LineTo(contextPoint, true, false);
            contextPoint.Y = contextPoint.Y - position.Height; context.LineTo(contextPoint, true, false);
            context.Close();
        }

    }
}
