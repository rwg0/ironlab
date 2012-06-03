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
    public partial class Viewport3DControl : FrameworkElement
    {
        ImageBrush sceneImage;
        ViewportImage directImage;
        Grid grid;
        Canvas canvas;

        public ViewportImage Viewport3DImage { get { return directImage; } }
        public Canvas Canvas { get { return canvas; } }

        public Viewport3DControl()
        {
            grid = new Grid();
            grid.VerticalAlignment = VerticalAlignment.Stretch; grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            try
            {
                directImage = new ViewportImage();
                sceneImage = directImage.ImageBrush;
                grid.Background = sceneImage;
                sceneImage.TileMode = TileMode.None;
            }
            catch (Exception e)
            {
                // In case of error, just display message on Control
                TextBlock messageBlock = new TextBlock();
                messageBlock.Text = e.Message;
                grid.Children.Add(messageBlock);
                directImage = null;
                return;
            }
            canvas = new Canvas() { ClipToBounds = true, Background = Brushes.Transparent };
            grid.Children.Add(canvas);
            directImage.Canvas = canvas;
            // TODO change this!
            grid.SizeChanged += new SizeChangedEventHandler(directImage.OnSizeChanged);
            directImage.RenderRequested += new EventHandler(directImage_RenderRequested);
        }

        void directImage_RenderRequested(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null,
                new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            directImage.RenderScene();
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return this.grid;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            this.grid.Measure(availableSize);
            return this.grid.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);
            double width = size.Width;
            double height = size.Height;
            grid.Arrange(new Rect(0, 0, width, height));
            directImage.SetImageSize((int)width, (int)height, 96);
            return size;
        }

        /// <summary>
        /// Participates in rendering operations that are directed by the layout system.
        /// </summary>
        /// <param name="sizeInfo">
        /// The packaged parameters, which includes old and new sizes, and which
        /// dimension actually changes.
        /// </param>
        //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        //{
        //    base.OnRenderSizeChanged(sizeInfo);
        //}
    }
}

