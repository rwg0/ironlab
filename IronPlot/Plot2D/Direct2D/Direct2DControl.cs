// Copyright (c) 2010 Joe Moorhouse

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

namespace IronPlot
{
    public partial class Direct2DControl : FrameworkElement
    {
        ImageBrush sceneImage;
        Direct2DImage directImage;
        Grid grid;

        public List<DirectPath> Paths
        {
            get { return directImage.paths; }
        }

        public void AddPath(DirectPath path)
        {
            directImage.paths.Add(path);
            path.DirectImage = directImage;
            path.RecreateDisposables();
        }

        public void RemovePath(DirectPath path)
        {
            directImage.paths.Remove(path);
            path.Dispose();
        }

        public Direct2DControl()
        {
            grid = new Grid();
            grid.VerticalAlignment = VerticalAlignment.Stretch; grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            directImage = new Direct2DImage();
            sceneImage = directImage.ImageBrush;
            grid.Background = sceneImage;
            sceneImage.TileMode = TileMode.None;
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(Direct2DControl_IsVisibleChanged);
        }

        public void RequestRender()
        {
            directImage.RequestRender();
        }

        /// <summary>
        /// Gets the number of visual child elements within this element.
        /// </summary>
        /// <remarks>
        /// This will always return 1 as this control hosts a single
        /// Image control.
        /// </remarks>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>Arranges and sizes the child Image control.</summary>
        /// <param name="finalSize">The size used to arrange the control.</param>
        /// <returns>The size of the control.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);
            double width = size.Width;
            double height = size.Height;
            grid.Arrange(new Rect(0, 0, width, height));
            directImage.SetImageSize((int)width, (int)height, 96);
            return size;
        }

        /// <summary>Returns the child Image control.</summary>
        /// <param name="index">
        /// The zero-based index of the requested child element in the collection.
        /// </param>
        /// <returns>The child Image control.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than zero or greater than VisualChildrenCount.
        /// </exception>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return this.grid;
        }

        /// <summary>
        /// Updates the UIElement.DesiredSize of the child Image control.
        /// </summary>
        /// <param name="availableSize">The size that the control should not exceed.</param>
        /// <returns>The child Image's desired size.</returns>
        //protected override Size MeasureOverride(Size availableSize)
        //{
        //    this.canvas.Measure(availableSize);
        //    return this.canvas.DesiredSize;
        //}

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

        void Direct2DControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            directImage.Visible = (bool)e.NewValue;
            if (directImage.Visible == true)
            {
                foreach (DirectPath path in Paths) path.RecreateDisposables();
                if (Parent is FrameworkElement) (Parent as FrameworkElement).InvalidateMeasure();
            }
            else foreach (DirectPath path in Paths) path.DisposeDisposables();
        }
    }

    //public partial class Direct2DControl : Grid
    //{
    //    Direct2DImage directImage;
    //    ImageBrush sceneImage;
        
    //    public Direct2DControl()
    //    {
    //        directImage = new Direct2DImage();
    //        sceneImage = directImage.ImageBrush;
    //        this.Background = sceneImage;
    //        sceneImage.TileMode = TileMode.None;
    //        this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(Direct2DControl_IsVisibleChanged);
    //    }


    //    public List<DirectPath> Paths
    //    {
    //        get { return directImage.paths; }
    //    }

    //    public void AddPath(DirectPath path)
    //    {
    //        directImage.paths.Add(path);
    //        path.DirectImage = directImage;
    //        path.RecreateDisposables();
    //    }

    //    public void RemovePath(DirectPath path)
    //    {
    //        directImage.paths.Remove(path);
    //        path.Dispose();
    //    }

    //    void Direct2DControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    //    {
    //        directImage.Visible = (bool)e.NewValue;
    //        if (directImage.Visible == true)
    //        {
    //            foreach (DirectPath path in Paths) path.RecreateDisposables();
    //            if (Parent is FrameworkElement) (Parent as FrameworkElement).InvalidateMeasure();
    //        }
    //        //else foreach (DirectPath path in Paths) path.DisposeDisposables();
    //    }

    //    public void RequestRender()
    //    {
    //        directImage.RequestRender();
    //    }

    //    /// <summary>Arranges and sizes the child Image control.</summary>
    //    /// <param name="finalSize">The size used to arrange the control.</param>
    //    /// <returns>The size of the control.</returns>
    //    protected override Size ArrangeOverride(Size finalSize)
    //    {
    //        Size size = base.ArrangeOverride(finalSize);
    //        double width = size.Width;
    //        double height = size.Height;
    //        directImage.SetImageSize((int)width, (int)height, 96);
    //        return size;
    //    }
    //}
}

