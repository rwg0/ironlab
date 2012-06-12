using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using SharpDX;
using SharpDX.Direct3D9;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;
using IronPlot.ManagedD3D;
using System.Windows.Threading;

namespace IronPlot.Plotting3D
{
    public partial class Viewport3DControl : DirectControl
    {
        public ViewportImage Viewport3DImage { get { return directImage as ViewportImage; } }
        
        Canvas canvas;
        public Canvas Canvas { get { return canvas; } }

        public Viewport3DControl() : base() 
        {
            canvas = new Canvas() { ClipToBounds = true, Background = Brushes.Transparent };
            if (directImage == null) return; // in the event of an exception.
            this.Children.Add(canvas);
            directImage.Canvas = canvas;
            // TODO change this!
            rectangle.SizeChanged += new SizeChangedEventHandler(directImage.OnSizeChanged);
            (directImage as ViewportImage).RenderRequested += new EventHandler(directImage_RenderRequested);
        }

        protected override void CreateDirectImage()
        {
            directImage = new ViewportImage();
        }

        void directImage_RenderRequested(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null,
                new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            if (directImage != null) directImage.RenderScene();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);
            this.canvas.Measure(availableSize);
            return this.canvas.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);
            double width = size.Width;
            double height = size.Height;
            canvas.Arrange(new Rect(0, 0, width, height));
            return size;
        }

        protected override void OnVisibleChanged_Visible()
        {
        }

        protected override void OnVisibleChanged_NotVisible()
        {
        }
    }
}

