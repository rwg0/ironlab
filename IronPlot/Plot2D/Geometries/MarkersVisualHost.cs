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
    public class MarkersVisualHost : FrameworkElement
    {
        VisualCollection visualChildren;
        
        DrawingVisual markers = new DrawingVisual();

        public MarkersVisualHost()
        {
            visualChildren = new VisualCollection(this);
            UpdateMarkersVisual(null);
            visualChildren.Add(markers);
        }
        
        void UpdateMarkersVisual(Geometry geometry)
        {
            DrawingContext context = markers.RenderOpen();
            context.DrawRectangle(Brushes.Red, new Pen(Brushes.Red, 1), new Rect(30, 30, 30, 30));
            
            context.Close();
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return visualChildren.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= visualChildren.Count)
                throw new ArgumentOutOfRangeException("index");

            return visualChildren[index];
        }
    }
}
