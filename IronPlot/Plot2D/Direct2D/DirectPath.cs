using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;
using System.Windows;
using Brushes = System.Windows.Media.Brushes;

namespace IronPlot
{
    // Has managed and non-managed part. The latter comprises:
    // Brush and Geometry
    // non-managed items are disposed whenever visibility is lost.
    /// <summary>
    /// Path implemented in Direct2D
    /// </summary>
    public class DirectPath : FrameworkElement, IDisposable
    {
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill",
            typeof(System.Windows.Media.Brush), typeof(DirectPath),
            new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke",
            typeof(System.Windows.Media.Brush), typeof(DirectPath),
            new PropertyMetadata(Brushes.Black, OnStrokePropertyChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness",
            typeof(double), typeof(DirectPath),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty QuickStrokeDashProperty =
            DependencyProperty.Register("QuickStrokeDash",
            typeof(QuickStrokeDash), typeof(DirectPath),
            new PropertyMetadata(QuickStrokeDash.Solid));

        public QuickStrokeDash QuickStrokeDash
        {
            set
            {
                SetValue(QuickStrokeDashProperty, value);
            }
            get { return (QuickStrokeDash)GetValue(QuickStrokeDashProperty); }
        }

        public System.Windows.Media.Brush Fill
        {
            set
            {
                SetValue(FillProperty, value);
            }
            get { return (System.Windows.Media.Brush)GetValue(FillProperty); }
        }

        public System.Windows.Media.Brush Stroke
        {
            set
            {
                SetValue(StrokeProperty, value);
            }
            get { return (System.Windows.Media.Brush)GetValue(StrokeProperty); }
        }

        public double StrokeThickness
        {
            set
            {
                SetValue(StrokeThicknessProperty, value);
            }
            get { return (double)GetValue(StrokeThicknessProperty); }
        }

        private static void OnStrokePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            DirectPath localDirectPath = (DirectPath)obj;
            localDirectPath.SetBrush(((System.Windows.Media.SolidColorBrush)e.NewValue).Color);
        }

        DirectImage directImage;
        
        Geometry geometry;
        internal Geometry Geometry
        { 
            get { return geometry; }
            set 
            { 
                if (geometry != null) geometry.Dispose(); 
                this.geometry = value; 
            }
        }

        Brush brush;
        internal Brush Brush
        {
            get { return brush; }
            //set
            //{
            //    brush.Dispose();
            //    this.brush = value;
            //}
        }

        Brush fillBrush;
        internal Brush FillBrush
        {
            get { return fillBrush; }
        }

        public Factory Factory
        {
            get { if (directImage != null) return directImage.RenderTarget.Factory; else return null; }
        }

        public DirectImage DirectImage
        {
            get { return directImage; }
            set
            {
                directImage = value;
            }
        }

        private void SetBrush(System.Windows.Media.Color colour)
        {
            if (brush != null) brush.Dispose();
            if (directImage != null) brush = new SolidColorBrush(directImage.RenderTarget, new Color4(colour.ScR, colour.ScG, colour.ScB, colour.ScA));
        }

        private void SetFillBrush(System.Windows.Media.Color colour)
        {
            if (fillBrush != null) fillBrush.Dispose();
            if (directImage != null) fillBrush = new SolidColorBrush(directImage.RenderTarget, new Color4(colour.ScR, colour.ScG, colour.ScB, colour.ScA));
        }

        public DirectPath()
        {
            this.geometry = null;
            this.directImage = null;
        }

        internal void DisposeDisposables()
        {
            if (brush != null)
            {
                brush.Dispose();
                brush = null;
            }
            if (fillBrush != null)
            {
                fillBrush.Dispose();
                fillBrush = null;
            }
            if (geometry != null)
            {
                geometry.Dispose();
                geometry = null;
            }
        }

        internal void RecreateDisposables()
        {
            DisposeDisposables();
            SetBrush(((System.Windows.Media.SolidColorBrush)GetValue(StrokeProperty)).Color);
            SetFillBrush(((System.Windows.Media.SolidColorBrush)GetValue(FillProperty)).Color);
            // Geometry will be re-created when the containing Direct2DControl is next Arranged.
        }

        public void Dispose()
        {
            DisposeDisposables();
        }
    }
}
